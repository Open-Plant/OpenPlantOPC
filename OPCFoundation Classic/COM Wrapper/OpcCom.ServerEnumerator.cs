//============================================================================
// TITLE: ResultIDs.cs
//
// CONTENTS:
// 
// Defines static information for well known error/success codes.
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
// 2003/04/04 RSA   Initial implementation.

using System;
using System.Net;
using System.Collections;
using System.Runtime.InteropServices;
using Opc;
using Opc.Da;
using OpcRcw.Comn;

namespace OpcCom
{
	/// <summary>
	/// A unique identifier for the result of an operation of an item.
	/// </summary>
	public class ServerEnumerator : IDiscovery
	{
		//======================================================================
		// IDisposable

		/// <summary>
		/// Frees all unmanaged resources
		/// </summary>
		public void Dispose() {}

		//======================================================================
		// IDiscovery

		/// <summary>
		/// Enumerates hosts that may be accessed for server discovery.
		/// </summary>
		public string[] EnumerateHosts()
		{
			return OpcCom.Interop.EnumComputers();
		}

		/// <summary>
		/// Returns a list of servers that support the specified interface specification.
		/// </summary>
		public Opc.Server[] GetAvailableServers(Specification specification)
		{
			return GetAvailableServers(specification, null, null);
		}

		/// <summary>
		/// Returns a list of servers that support the specified specification on the specified host.
		/// </summary>
		public Opc.Server[] GetAvailableServers(Specification specification, string host, ConnectData connectData)
		{
			lock (this)
			{
				NetworkCredential credentials = (connectData != null)?connectData.GetCredential(null, null):null;

				// connect to the server.				
				m_server = (IOPCServerList2)OpcCom.Interop.CreateInstance(CLSID, host, credentials);
				m_host   = host;

				try
				{
					ArrayList servers = new ArrayList();
					
					// convert the interface version to a guid.
					Guid catid = new Guid(specification.ID);
			
					// get list of servers in the specified specification.
					IOPCEnumGUID enumerator = null;

					m_server.EnumClassesOfCategories(
						1,
						new Guid[] { catid },
						0,
						null,
						out enumerator);

					// read clsids.
					Guid[] clsids = ReadClasses(enumerator);

					// release enumerator object.					
					OpcCom.Interop.ReleaseServer(enumerator);
					enumerator = null;

					// fetch class descriptions.
					foreach (Guid clsid in clsids)
					{
						Factory factory = new OpcCom.Factory();

						try
						{
							URL url = CreateUrl(specification, clsid);

							Opc.Server server = null;

							if (specification == Specification.COM_DA_30)
							{							
								server = new Opc.Da.Server(factory, url);
							}

							else if (specification == Specification.COM_DA_20)
							{
								server = new Opc.Da.Server(factory, url);
							}
						
							else if (specification == Specification.COM_AE_10)
							{
								server = new Opc.Ae.Server(factory, url);
							}

							else if (specification == Specification.COM_HDA_10)
							{
								server = new Opc.Hda.Server(factory, url);
							}
						
							else if (specification == Specification.COM_DX_10)
							{
								server = new Opc.Dx.Server(factory, url);
							}

							servers.Add(server);
						}
						catch (Exception)
						{
							// ignore bad clsids.
						}
					}

					return (Opc.Server[])servers.ToArray(typeof(Opc.Server));
				}
				finally
				{
					// free the server.
					OpcCom.Interop.ReleaseServer(m_server);
					m_server = null;
				}
			}
		}
		
		/// <summary>
		/// Looks up the CLSID for the specified prog id on a remote host.
		/// </summary>
		public Guid CLSIDFromProgID(string progID, string host, ConnectData connectData)
		{
			lock (this)
			{
				NetworkCredential credentials = (connectData != null)?connectData.GetCredential(null, null):null;

				// connect to the server.				
				m_server = (IOPCServerList2)OpcCom.Interop.CreateInstance(CLSID, host, credentials);
				m_host   = host;

				// lookup prog id.
				Guid clsid;

				try
				{
					m_server.CLSIDFromProgID(progID, out clsid);
				}
				catch
				{
					clsid = Guid.Empty;
				}
				finally
				{
					OpcCom.Interop.ReleaseServer(m_server);
					m_server = null;
				}

				// return empty guid if prog id not found.
				return clsid;
			}
		}

		//======================================================================
		// Private Members

		/// <summary>
		/// The server enumerator COM server.
		/// </summary>
		private IOPCServerList2 m_server = null;

		/// <summary>
		/// The host where the servers are being enumerated.
		/// </summary>
		private string m_host = null;

		/// <summary>
		/// The ProgID for the OPC Server Enumerator.
		/// </summary>
		private const string ProgID = "OPC.ServerList.1";
		
		/// <summary>
		/// The CLSID for the OPC Server Enumerator.
		/// </summary>
		private static readonly Guid CLSID = new Guid("13486D51-4821-11D2-A494-3CB306C10000");

		//======================================================================
		// Private Methods

		/// <summary>
		/// Reads the guids from the enumerator.
		/// </summary>
		private Guid[] ReadClasses(IOPCEnumGUID enumerator)
		{
			ArrayList guids = new ArrayList();

            int fetched = 0;
            int count = 10;
            
            // create buffer.
            IntPtr buffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Guid))*count);

            try
            {
			    do
			    {
				    try
				    {
                        enumerator.Next(count, buffer, out fetched);

                        IntPtr pPos = buffer;
    					
					    for (int ii = 0; ii < fetched; ii++)
					    {
                            Guid guid = (Guid)Marshal.PtrToStructure(pPos, typeof(Guid));
                            guids.Add(guid);
                            pPos = (IntPtr)(pPos.ToInt64() + Marshal.SizeOf(typeof(Guid)));
					    }
				    }
				    catch
				    {
					    break;
				    }
			    }
			    while (fetched > 0);

			    return (Guid[])guids.ToArray(typeof(Guid));
            }
            finally
            {
                Marshal.FreeCoTaskMem(buffer);
            }
		}

		/// <summary>
		/// Reads the server details from the enumerator.
		/// </summary>
		URL CreateUrl(Specification specification, Guid clsid)
		{
			// initialize the server url.
			URL url = new URL();
		
			url.HostName = m_host;
			url.Port     = 0;
			url.Path     = null;

			if      (specification == Specification.COM_DA_30)    { url.Scheme = UrlScheme.DA;    }
			else if (specification == Specification.COM_DA_20)    { url.Scheme = UrlScheme.DA;    }
			else if (specification == Specification.COM_DA_10)    { url.Scheme = UrlScheme.DA;    }
			else if (specification == Specification.COM_DX_10)    { url.Scheme = UrlScheme.DX;    }
			else if (specification == Specification.COM_AE_10)    { url.Scheme = UrlScheme.AE;    }
			else if (specification == Specification.COM_HDA_10)   { url.Scheme = UrlScheme.HDA;   }
			else if (specification == Specification.COM_BATCH_10) { url.Scheme = UrlScheme.BATCH; }
			else if (specification == Specification.COM_BATCH_20) { url.Scheme = UrlScheme.BATCH; }

			try
			{
				// fetch class details from the enumerator.
				string progID       = null;
				string description  = null;
				string verIndProgID = null;

				m_server.GetClassDetails(
					ref clsid, 
					out progID, 
					out description, 
					out verIndProgID);
				
				// create the server URL path.
				if (verIndProgID != null)
				{
					url.Path = String.Format("{0}/{1}", verIndProgID, "{" + clsid.ToString() + "}");
				}
				else if (progID != null)
				{
					url.Path = String.Format("{0}/{1}", progID, "{" + clsid.ToString() + "}");
				}
			}
			catch (Exception)
			{
				// bad value in registry.
			}
			finally
			{
				// default to the clsid if the prog is not known.
				if (url.Path == null)
				{
					url.Path = String.Format("{0}", "{" + clsid.ToString() + "}");
				}
			}

			// return the server url.
			return url;
		}
	}
}
