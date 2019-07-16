//============================================================================
// TITLE: OpcCom.Dx.Server.cs
//
// CONTENTS:
// 
// A class that wraps a COM-DX server.
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
// 2004/05/17 RSA   Initial implementation.

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using Opc;
using Opc.Dx;

namespace OpcCom.Dx
{
	/// <summary>
	/// An in-process object used to access OPC Data eXchange servers.
	/// </summary>
	[Serializable]
	public class Server : OpcCom.Da.Server, Opc.Dx.IServer
	{
		/// <summary>
		/// Initializes the object with the specified URL and COM server.
		/// </summary>
        public Server(URL url, object server) : base(url, server) { }
	
		#region Opc.Dx.IServer Members
		/// <summary>
		/// Fetches all source servers in the current configuration.
		/// </summary>
		/// <returns>The list of source servers.</returns>
		public SourceServer[] GetSourceServers()
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					int    count    = 0;
					IntPtr pServers = IntPtr.Zero;

					((OpcRcw.Dx.IOPCConfiguration)m_server).GetServers(out count, out pServers);

					return Interop.GetSourceServers(ref pServers, count, true);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCConfiguration.GetServers", e);
				}		
			}
		}

		/// <summary>
		/// Adds a new set of source servers to the current configuration.
		/// </summary>
		/// <param name="servers">The list of source servers to add.</param>
		/// <returns>The results of the operation for each source server.</returns>
		public GeneralResponse AddSourceServers(SourceServer[] servers)
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					OpcRcw.Dx.SourceServer[]    input = Interop.GetSourceServers(servers);
					OpcRcw.Dx.DXGeneralResponse pResponse;

					((OpcRcw.Dx.IOPCConfiguration)m_server).AddServers(
						input.Length, 
						input,
						out pResponse);

					return Interop.GetGeneralResponse(pResponse, true);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCConfiguration.AddServers", e);
				}		
			}
		}

		/// <summary>
		/// Modifies a set of source servers in the current configuration.
		/// </summary>
		/// <param name="servers">A list source source attributes.</param>
		/// <returns>The results of the operation for each source server.</returns>
		public GeneralResponse ModifySourceServers(SourceServer[] servers)
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					OpcRcw.Dx.SourceServer[]    input = Interop.GetSourceServers(servers);
					OpcRcw.Dx.DXGeneralResponse pResponse;

					((OpcRcw.Dx.IOPCConfiguration)m_server).ModifyServers(
						input.Length, 
						input,
						out pResponse);

					return Interop.GetGeneralResponse(pResponse, true);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCConfiguration.ModifyServers", e);
				}		
			}
		}

		/// <summary>
		/// Deletes a set of source servers in the current configuration.
		/// </summary>
		/// <param name="servers">A list of source servers to delete.</param>
		/// <returns>The results of the operation for each source server.</returns>
		public GeneralResponse DeleteSourceServers(Opc.Dx.ItemIdentifier[] servers)
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					OpcRcw.Dx.ItemIdentifier[]  input = Interop.GetItemIdentifiers(servers);
					OpcRcw.Dx.DXGeneralResponse pResponse;

					((OpcRcw.Dx.IOPCConfiguration)m_server).DeleteServers(
						input.Length, 
						input,
						out pResponse);

					return Interop.GetGeneralResponse(pResponse, true);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCConfiguration.DeleteServers", e);
				}		
			}
		}

		/// <summary>
		/// Copies the default or runtime attributes for a set of source servers. 
		/// </summary>
		/// <param name="configToStatus">Whether the default attributes are copied to or copied from the runtime attributes.</param>
		/// <param name="servers">The set of source servers to modify.</param>
		/// <returns>The results of the operation for each source server.</returns>
		public GeneralResponse CopyDefaultSourceServerAttributes(
			bool                    configToStatus, 
			Opc.Dx.ItemIdentifier[] servers)
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					OpcRcw.Dx.ItemIdentifier[]  input = Interop.GetItemIdentifiers(servers);
					OpcRcw.Dx.DXGeneralResponse pResponse;

					((OpcRcw.Dx.IOPCConfiguration)m_server).CopyDefaultServerAttributes(
						(configToStatus)?1:0,
						input.Length, 
						input,
						out pResponse);

					return Interop.GetGeneralResponse(pResponse, true);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCConfiguration.CopyDefaultServerAttributes", e);
				}		
			}
		}

		/// <summary>
		/// Returns a list of connections that meet the specified criteria.
		/// </summary>
		/// <param name="browsePath">The browse path where the search begins.</param>
		/// <param name="connectionMasks">The masks that define the query criteria.</param>
		/// <param name="recursive">Whether the folders under the browse path are searched as well.</param>
		/// <param name="errors">Any errors associated with individual query masks.</param>
		/// <returns>The list of connections that meet the criteria.</returns>
		public DXConnection[] QueryDXConnections(
			string         browsePath, 
			DXConnection[] connectionMasks, 
			bool           recursive,
			out ResultID[] errors)
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					OpcRcw.Dx.DXConnection[] input = Interop.GetDXConnections(connectionMasks);

					if (input == null)
					{
						input = new OpcRcw.Dx.DXConnection[0]; 
					}

					int count = 0;

					IntPtr pErrors      = IntPtr.Zero;
					IntPtr pConnections = IntPtr.Zero;

					((OpcRcw.Dx.IOPCConfiguration)m_server).QueryDXConnections(
						(browsePath != null)?browsePath:"",
						input.Length,
						input,
						(recursive)?1:0,
						out pErrors,
						out count,
						out pConnections);

					errors = Interop.GetResultIDs(ref pErrors, input.Length, true);

					return Interop.GetDXConnections(ref pConnections, count, true);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCConfiguration.QueryDXConnections", e);
				}		
			}		
		}

		/// <summary>
		/// Add a set of connections to the configuration.
		/// </summary>
		/// <param name="connections">The set of connections to add.</param>
		/// <returns>The results of the operation for each connection.</returns>
		public GeneralResponse AddDXConnections(DXConnection[] connections)
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					OpcRcw.Dx.DXConnection[] input = Interop.GetDXConnections(connections);
					
					if (input == null)
					{
						input = new OpcRcw.Dx.DXConnection[0]; 
					}

					OpcRcw.Dx.DXGeneralResponse pResponse;

					((OpcRcw.Dx.IOPCConfiguration)m_server).AddDXConnections(
						input.Length,
						input,
						out pResponse);

					return Interop.GetGeneralResponse(pResponse, true);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCConfiguration.AddDXConnections", e);
				}		
			}		
		}
    	
		/// <summary>
		/// Modify a set of connections in the configuration.
		/// </summary>
		/// <param name="connections">The set of connections to modify.</param>
		/// <returns>The results of the operation for each connection.</returns>
		/// <remarks>Only explicitly specified attributes in the connection objects are changed.</remarks>
		public GeneralResponse ModifyDXConnections(DXConnection[] connections)
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					OpcRcw.Dx.DXConnection[] input = Interop.GetDXConnections(connections);
					
					if (input == null)
					{
						input = new OpcRcw.Dx.DXConnection[0]; 
					}

					OpcRcw.Dx.DXGeneralResponse pResponse;

					((OpcRcw.Dx.IOPCConfiguration)m_server).ModifyDXConnections(
						input.Length,
						input,
						out pResponse);

					return Interop.GetGeneralResponse(pResponse, true);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCConfiguration.ModifyDXConnections", e);
				}		
			}	
		}

		/// <summary>
		/// Updates a set of connections which meet the specified query criteria.
		/// </summary>
		/// <param name="browsePath">The browse path where the search begins.</param>
		/// <param name="connectionMasks">The masks that define the query criteria.</param>
		/// <param name="recursive">Whether the folders under the browse path are searched as well.</param>
		/// <param name="connectionDefinition">The changes that will be applied to all connections meeting the criteria.</param>
		/// <param name="errors">Any errors associated with individual query masks.</param>
		/// <returns>The list of connections that met the criteria and were updated.</returns>
		public GeneralResponse UpdateDXConnections(
			string         browsePath, 
			DXConnection[] connectionMasks, 
			bool           recursive,
			DXConnection   connectionDefinition,
			out ResultID[] errors)
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					OpcRcw.Dx.DXConnection[] input = Interop.GetDXConnections(connectionMasks);
					
					if (input == null)
					{
						input = new OpcRcw.Dx.DXConnection[0]; 
					} 

					OpcRcw.Dx.DXConnection definition = Interop.GetDXConnection(connectionDefinition);

					OpcRcw.Dx.DXGeneralResponse pResponse;
					IntPtr pErrors = IntPtr.Zero;

					((OpcRcw.Dx.IOPCConfiguration)m_server).UpdateDXConnections(
						(browsePath != null)?browsePath:"",
						input.Length,
						input,
						(recursive)?1:0,
						ref definition,
						out pErrors,
						out pResponse);

					errors = Interop.GetResultIDs(ref pErrors, input.Length, true);

					return Interop.GetGeneralResponse(pResponse, true);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCConfiguration.UpdateDXConnections", e);
				}		
			}			
		}

		/// <summary>
		/// Deletes a set of connections which meet the specified query criteria.
		/// </summary>
		/// <param name="browsePath">The browse path where the search begins.</param>
		/// <param name="connectionMasks">The masks that define the query criteria.</param>
		/// <param name="recursive">Whether the folders under the browse path are searched as well.</param>
		/// <param name="errors">Any errors associated with individual query masks.</param>
		/// <returns>The list of connections that met the criteria and were deleted.</returns>
		public GeneralResponse DeleteDXConnections(
			string         browsePath, 
			DXConnection[]   connectionMasks, 
			bool           recursive,
			out ResultID[] errors)
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					OpcRcw.Dx.DXConnection[] input = Interop.GetDXConnections(connectionMasks);
					
					if (input == null)
					{
						input = new OpcRcw.Dx.DXConnection[0]; 
					}

					OpcRcw.Dx.DXGeneralResponse pResponse;

					IntPtr pErrors = IntPtr.Zero;

					((OpcRcw.Dx.IOPCConfiguration)m_server).DeleteDXConnections(
						(browsePath != null)?browsePath:"",
						input.Length,
						input,
						(recursive)?1:0,
						out pErrors,
						out pResponse);

					errors = Interop.GetResultIDs(ref pErrors, input.Length, true);

					return Interop.GetGeneralResponse(pResponse, true);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCConfiguration.DeleteDXConnections", e);
				}		
			}				
		}

		/// <summary>
		/// Changes the default or runtime attributes for a set of connections. 
		/// </summary>
		/// <param name="configToStatus">Whether the default attributes are copied to or copied from the runtime attributes.</param>
		/// <param name="browsePath">The browse path where the search begins.</param>
		/// <param name="connectionMasks">The masks that define the query criteria.</param>
		/// <param name="recursive">Whether the folders under the browse path are searched as well.</param>
		/// <param name="errors">Any errors associated with individual query masks.</param>
		/// <returns>The list of connections that met the criteria and were modified.</returns>
		public GeneralResponse CopyDXConnectionDefaultAttributes(
			bool		   configToStatus,
			string         browsePath, 
			DXConnection[] connectionMasks, 
			bool           recursive,
			out ResultID[] errors)
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					OpcRcw.Dx.DXConnection[] input = Interop.GetDXConnections(connectionMasks);
					
					if (input == null)
					{
						input = new OpcRcw.Dx.DXConnection[0]; 
					}

					OpcRcw.Dx.DXGeneralResponse pResponse;

					IntPtr pErrors = IntPtr.Zero;

					((OpcRcw.Dx.IOPCConfiguration)m_server).CopyDXConnectionDefaultAttributes(
						(configToStatus)?1:0,
						(browsePath != null)?browsePath:"",
						input.Length,
						input,
						(recursive)?1:0,
						out pErrors,
						out pResponse);

					errors = Interop.GetResultIDs(ref pErrors, input.Length, true);

					return Interop.GetGeneralResponse(pResponse, true);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCConfiguration.CopyDXConnectionDefaultAttributes", e);
				}		
			}			
		}

		/// <summary>
		/// Resets the current configuration,
		/// </summary>
		/// <param name="configurationVersion">The current configuration version.</param>
		/// <returns>The new configuration version.</returns>
		public string ResetConfiguration(string configurationVersion)
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					string resetConfiguration = null;

					((OpcRcw.Dx.IOPCConfiguration)m_server).ResetConfiguration(configurationVersion, out resetConfiguration);

					return resetConfiguration;
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCConfiguration.ResetConfiguration", e);
				}		
			}
		}
		#endregion

		#region Private Members
		#endregion
	}
}
