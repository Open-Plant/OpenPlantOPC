//============================================================================
// TITLE: EnumString.cs
//
// CONTENTS:
// 
// A wrapper for the COM IEnumString interface.
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

using System;
using System.Collections;
using System.Runtime.InteropServices;
using OpcRcw.Comn;

namespace OpcCom
{
	/// <summary>
	/// A wrapper for the COM IEnumString interface.
	/// </summary>
	public class EnumString : IDisposable
	{	
		/// <summary>
		/// A reference to the remote COM object.
		/// </summary>
		private IEnumString m_enumerator = null;

		/// <summary>
		/// Initializes the object with an enumerator.
		/// </summary>
		public EnumString(object enumerator)
		{
			m_enumerator = (IEnumString)enumerator;
		}
		
		/// <summary>
		/// Releases the remote COM object.
		/// </summary>
		public void Dispose()
		{
			OpcCom.Interop.ReleaseServer(m_enumerator);
			m_enumerator = null;
		}

		//=====================================================================
		// IEnumString

		/// <summary>
		/// Fetches the next group of strings. 
		/// </summary>
		public string[] Next(int count)
		{
			try
			{
				// create buffer.
                IntPtr buffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(IntPtr))*count);

                try
                {
                    // fetch next group of strings.
                    int fetched = 0;

                    m_enumerator.RemoteNext(
                        count,
                        buffer,
                        out fetched);

                    // return empty array if at end of list.
                    if (fetched == 0)
                    {
                        return new string[0];
                    }

                    return Interop.GetUnicodeStrings(ref buffer, fetched, true);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(buffer);
                }
			}

			// return null on any error.
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Skips a number of strings.
		/// </summary>
		public void Skip(int count)
		{
			m_enumerator.Skip(count);
		}

		/// <summary>
		/// Sets pointer to the start of the list.
		/// </summary>
		public void Reset()
		{
			m_enumerator.Reset();
		}

		/// <summary>
		/// Clones the enumerator.
		/// </summary>
		public EnumString Clone()
		{
			IEnumString enumerator = null;
			m_enumerator.Clone(out enumerator);
			return new EnumString(enumerator);
		}
	}
}
