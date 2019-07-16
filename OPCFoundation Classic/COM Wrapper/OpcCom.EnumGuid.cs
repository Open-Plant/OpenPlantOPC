//============================================================================
// TITLE: EnumGuid.cs
//
// CONTENTS:
// 
// An enumerator for a set of GUIDs (obsolete - replaced by OpcEnumGuid).
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
	/// Enumerates a set of GUIDs.
	/// </summary>
	public class EnumGuid
	{	
		private IEnumGUID m_enumerator = null;

		/// <summary>
		/// Saves a reference to the COM server.
		/// </summary>
		public EnumGuid(object server)
		{
			m_enumerator = (IEnumGUID)server;
		}
		
		/// <summary>
		/// releases the COM server.
		/// </summary>
		public void Release()
		{
			OpcCom.Interop.ReleaseServer(m_enumerator);
			m_enumerator = null;
		}

		/// <summary>
		/// returns a reference to the COM server.
		/// </summary>
		public object GetEnumerator()
		{
			return m_enumerator;
		}

		/// <summary>
		/// fetches all GUIDs. 
		/// </summary>
		public Guid[] GetAll()
		{
			Reset();

			ArrayList guids = new ArrayList();

			while (true)
			{
				Guid[] results = Next(1);

				if (results == null)
				{
					break;
				}

				guids.AddRange(results);
			}

			return (Guid[])guids.ToArray(typeof(Guid));
		}

		//=====================================================================
		// IEnumGUID

		/// <summary>
		/// fetches next group of GUIDs. 
		/// </summary>
		public Guid[] Next(int count)
		{
            IntPtr buffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Guid))*count);

            try
            {
                int fetched = 0;

                try
                {
                    m_enumerator.Next(
                        count,
                        buffer,
                        out fetched);
                }
                catch (Exception)
                {
                    return null;
                }

                if (fetched == 0)
                {
                    return null;
                }

                IntPtr pPos = buffer;
                Guid[] results = new Guid[fetched];

                for (int ii = 0; ii < fetched; ii++)
                {
                    results[ii] = (Guid)Marshal.PtrToStructure(pPos, typeof(Guid));
                    pPos = (IntPtr)(pPos.ToInt64() + Marshal.SizeOf(typeof(Guid)));
                }

                return results;
            }
            finally
            {
                Marshal.FreeCoTaskMem(buffer);
            }
		}

		/// <summary>
		/// skips elements.
		/// </summary>
		public void Skip(int count)
		{
			m_enumerator.Skip(count);
		}

		/// <summary>
		/// sets pointer to the start of the list.
		/// </summary>
		public void Reset()
		{
			m_enumerator.Reset();
		}

		/// <summary>
		/// clones the enumerator.
		/// </summary>
		public EnumGuid Clone()
		{
			IEnumGUID enumerator = null;
			m_enumerator.Clone(out enumerator);
			return new EnumGuid(enumerator);
		}
	}
}
