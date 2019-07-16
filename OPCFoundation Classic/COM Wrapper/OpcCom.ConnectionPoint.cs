//============================================================================
// TITLE: ConnectionPoint.cs
//
// CONTENTS:
// 
// Encapulates a COM server that supports IConnectionPoint.
//
// (c) Copyright 2002-2003 The OPC Foundation
// ALL RIGHTS RESERVED.
//
// DISCLAIMER:
//  This code is provided by the OPC Foundation solely to assist in 
//  understanding and use of the appropriate OPC Specification(s) and may be 
//  used as set forth in the License Grant section of the OPC Specification.
//  This code is provided as-is and without warranty or support of any sort
//  and is subject to the Warranty and Liability Disclaimers which appear
//  in the printed OPC Specification.
//
// MODIFICATION LOG:
//
// Date       By    Notes
// ---------- ---   -----
// 2002/09/03 RSA   First release.
// 2002/11/16 RSA   Second release.

using System;
using System.Runtime.InteropServices;
using OpcRcw.Comn;

namespace OpcCom
{
	/// <summary>
	/// Adds and removes a connection point to a server.
	/// </summary>
	public class ConnectionPoint : IDisposable
	{
		/// <summary>
		/// The COM server that supports connection points.
		/// </summary>
		private IConnectionPoint m_server = null;

		/// <summary>
		/// The id assigned to the connection by the COM server.
		/// </summary>
		private int m_cookie = 0;
		
		/// <summary>
		/// The number of times Advise() has been called without a matching Unadvise(). 
		/// </summary>
		private int m_refs = 0;
		
		/// <summary>
		/// Initializes the object by finding the specified connection point.
		/// </summary>
		public ConnectionPoint(object server, Guid iid)
		{
			((IConnectionPointContainer)server).FindConnectionPoint(ref iid, out m_server);
		}

		/// <summary>
		/// Releases the COM server.
		/// </summary>
		public void Dispose()
		{
			if (m_server != null)
			{
				while (Unadvise() > 0);				
				OpcCom.Interop.ReleaseServer(m_server);
				m_server = null;
			}
		}

        /// <summary> 
        /// The cookie returned in the advise call. 
        /// </summary> 
        public int Cookie 
        { 
            get { return m_cookie; } 
        }
		
        //=====================================================================
		// IConnectionPoint

		/// <summary>
		/// Establishes a connection, if necessary and increments the reference count.
		/// </summary>
		public int Advise(object callback)
		{
			if (m_refs++ == 0) m_server.Advise(callback, out m_cookie);
			return m_refs;
		}

		/// <summary>
		/// Decrements the reference count and closes the connection if no more references.
		/// </summary>
		public int Unadvise()
		{
			if (--m_refs == 0) m_server.Unadvise(m_cookie);
			return m_refs;
		}
	}
}
