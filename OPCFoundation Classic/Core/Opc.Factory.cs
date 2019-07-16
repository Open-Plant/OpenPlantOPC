//============================================================================
// TITLE: Opc.IFactory.cs
//
// CONTENTS:
// 
// A interface and a class used to instantiate server objects.
//
// (c) Copyright 2003-2004 The OPC Foundation
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
// 2003/08/18 RSA   Initial implementation.

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace Opc
{
	/// <summary>
	/// A interface to a factory used to instantiate servers.
	/// </summary>
	public interface IFactory : IDisposable
	{
		/// <summary>
		/// Creates a new instance of the server at the specified URL.
		/// </summary>
		IServer CreateInstance(URL url, ConnectData connectData);
	}

	/// <summary>
	/// The default class used to instantiate server objects.
	/// </summary>
	[Serializable]
	public class Factory : IFactory, ISerializable, ICloneable
	{
		//======================================================================
		// Construction

		/// <summary>
		/// Initializes the object with the type of the servers it can instantiate.
		/// </summary>
		/// <param name="systemType">The System.Type of the server object that the factory can create.</param>
		/// <param name="useRemoting">Whether the factory should use .NET Remoting to instantiate the servers.</param>
		public Factory(System.Type systemType, bool useRemoting)
		{
			m_systemType  = systemType;
			m_useRemoting = useRemoting;
		}

		//======================================================================
		// IDisposable
        
        #region IDisposable Members
        /// <summary>
        /// The finalizer.
        /// </summary>
        ~Factory()
        {
            Dispose (false);
        }

        /// <summary>
        /// Releases unmanaged resources held by the object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged resources held by the object.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                }

                // Free your own state (unmanaged objects).
                // Set large fields to null.
                m_disposed = true;
            }
        }

        private bool m_disposed = false;
		#endregion

		//======================================================================
		// ISerializable

		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string USE_REMOTING  = "UseRemoting";
			internal const string SYSTEM_TYPE   = "SystemType";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected Factory(SerializationInfo info, StreamingContext context)
		{
			m_useRemoting = info.GetBoolean(Names.USE_REMOTING);
			m_systemType  = (System.Type)info.GetValue(Names.SYSTEM_TYPE, typeof(System.Type));
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(Names.USE_REMOTING, m_useRemoting);
			info.AddValue(Names.SYSTEM_TYPE,  m_systemType);
		}
	
		//======================================================================
		// ICloneable

		/// <summary>
		/// Returns a clone of the factory.
		/// </summary>
		public virtual object Clone()
		{
			return MemberwiseClone();
		}
		
		//======================================================================
		// IFactory

		/// <summary>
		/// Creates a new instance of the server.
		/// </summary>
		public virtual IServer CreateInstance(URL url, ConnectData connectData)
		{
			IServer server = null;

			// instantiate the object locally.
			if (!m_useRemoting)
			{
				server = (IServer)Activator.CreateInstance(m_systemType, new object[] { url, connectData });			
			}

			// instantiate the object remotely using .NET remoting.
			else
			{
				server = (IServer)Activator.GetObject(m_systemType, url.ToString());
			}

			return server;
		}

		//======================================================================
		// Protected Properties

		/// <summary>
		/// The system type used to instantiate the remote server object.
		/// </summary>
		protected System.Type SystemType
		{
			get { return m_systemType;  }
			set { m_systemType = value; }
		}
		
		/// <summary>
		/// Whether the system type is a default system type for an OPC specification.
		/// </summary>
		protected bool UseRemoting 
		{
			get { return m_useRemoting;  }
			set { m_useRemoting = value; }
		}

		//======================================================================
		// Private Members

		/// <summary>
		/// The system type used to instantiate the remote server object.
		/// </summary>
		private System.Type m_systemType = null;
		
		/// <summary>
		/// Whether the system type is a default system type for an OPC specification.
		/// </summary>
		private bool m_useRemoting = false;
	}
}
