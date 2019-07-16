//============================================================================
// TITLE: Opc.Dx.Server.cs
//
// CONTENTS:
// 
// A class which is an in-process object used to access OPC Data eXchange servers.
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

namespace Opc.Dx
{
	/// <summary>
	/// An in-process object used to access OPC Data eXchange servers.
	/// </summary>
	[Serializable]
	public class Server : Opc.Da.Server, Opc.Dx.IServer, ISerializable
	{
		/// <summary>
		/// Initializes the object with a factory and a default URL.
		/// </summary>
		/// <param name="factory">The Opc.Factory used to connect to remote servers.</param>
		/// <param name="url">The network address of a remote server.</param>
		public Server(Factory factory, URL url) : base(factory, url) 
		{
		}
	
		/// <summary>
		/// The last configuration version returned from the server.
		/// </summary>
		public string Version
		{
			get { return m_version; }
		}

		/// <summary>
		/// The set of source servers in the configuration.
		/// </summary>
		public SourceServerCollection SourceServers
		{
			get { return m_sourceServers; }
		}

		/// <summary>
		/// The set of DX connection queries 
		/// </summary>
		public DXConnectionQueryCollection Queries
		{
			get { return m_connectionQueries; }
		}
		

		/// <summary>
		/// Adds a single source server to the configuration.
		/// </summary>
		/// <param name="server">The source server to add.</param>
		/// <returns>Returns the new source server object.</returns>
		public SourceServer AddSourceServer(SourceServer server)
		{
			GeneralResponse response = AddSourceServers(new SourceServer[] { server });

			if (response == null || response.Count != 1)
			{
				throw new Opc.InvalidResponseException();
			}

			if (response[0].ResultID.Failed())
			{
				throw new Opc.ResultIDException(response[0].ResultID);
			}

			SourceServer result = new SourceServer(server);

			result.ItemName = response[0].ItemName;
			result.ItemPath = response[0].ItemPath;
			result.Version  = response[0].Version;

			return result;
		}

		/// <summary>
		/// Modifies a single source server in the configuration.
		/// </summary>
		/// <param name="server">The source server to modify.</param>
		/// <returns>Returns the new source server object.</returns>
		public SourceServer ModifySourceServer(SourceServer server)
		{
			GeneralResponse response = ModifySourceServers(new SourceServer[] { server });

			if (response == null || response.Count != 1)
			{
				throw new Opc.InvalidResponseException();
			}

			if (response[0].ResultID.Failed())
			{
				throw new Opc.ResultIDException(response[0].ResultID);
			}

			SourceServer result = new SourceServer(server);

			result.ItemName = response[0].ItemName;
			result.ItemPath = response[0].ItemPath;
			result.Version  = response[0].Version;

			return result;
		}

		/// <summary>
		/// Deletes a single source server from the configuration.
		/// </summary>
		/// <param name="server">The source server to delete.</param>
		public void DeleteSourceServer(SourceServer server)
		{
			GeneralResponse response = DeleteSourceServers(new Opc.Dx.ItemIdentifier[] { server });

			if (response == null || response.Count != 1)
			{
				throw new Opc.InvalidResponseException();
			}

			if (response[0].ResultID.Failed())
			{
				throw new Opc.ResultIDException(response[0].ResultID);
			}
		}

		/// <summary>
		/// Adds a single DX connection to the configuration.
		/// </summary>
		/// <param name="connection">The DX connection to add.</param>
		/// <returns>Returns the new DX connection object.</returns>
		public DXConnection AddDXConnection(DXConnection connection)
		{
			GeneralResponse response = AddDXConnections(new DXConnection[] { connection });

			if (response == null || response.Count != 1)
			{
				throw new Opc.InvalidResponseException();
			}

			if (response[0].ResultID.Failed())
			{
				throw new Opc.ResultIDException(response[0].ResultID);
			}

			DXConnection result = new DXConnection(connection);

			result.ItemName = response[0].ItemName;
			result.ItemPath = response[0].ItemPath;
			result.Version  = response[0].Version;

			return result;
		}

		/// <summary>
		/// Modifies a single DX connection in the configuration.
		/// </summary>
		/// <param name="connection">The DX connection to modify.</param>
		public DXConnection ModifyDXConnection(DXConnection connection)
		{
			GeneralResponse response = ModifyDXConnections(new DXConnection[] { connection });

			if (response == null || response.Count != 1)
			{
				throw new Opc.InvalidResponseException();
			}

			if (response[0].ResultID.Failed())
			{
				throw new Opc.ResultIDException(response[0].ResultID);
			}

			DXConnection result = new DXConnection(connection);

			result.ItemName = response[0].ItemName;
			result.ItemPath = response[0].ItemPath;
			result.Version  = response[0].Version;

			return result;
		}

		/// <summary>
		/// Deletes a single DX connection from the configuration.
		/// </summary>
		/// <param name="connection">The DX connection to delete.</param>
		public void DeleteDXConnections(DXConnection connection)
		{
			ResultID[] errors = null;

			GeneralResponse response = DeleteDXConnections(
				null,
				new DXConnection[] { connection },
				true,
				out errors);

			if (errors != null && errors.Length > 0 && errors[0].Failed())
			{
				throw new Opc.ResultIDException(errors[0]);
			}

			if (response == null || response.Count != 1)
			{
				throw new Opc.InvalidResponseException();
			}

			if (response[0].ResultID.Failed())
			{
				throw new Opc.ResultIDException(response[0].ResultID);
			}
		}

		#region ISerializable Members
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string QUERIES = "Queries";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected Server(SerializationInfo info, StreamingContext context)
			:
			base(info, context)
		{		
			DXConnectionQuery[] queries = (DXConnectionQuery[])info.GetValue(Names.QUERIES, typeof(DXConnectionQuery[]));

			if (queries != null)
			{
				foreach (DXConnectionQuery subscription in queries)
				{
					m_connectionQueries.Add(subscription);
				}
			}
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			DXConnectionQuery[] queries = null;

			if (m_connectionQueries.Count > 0)
			{
				queries = new DXConnectionQuery[m_connectionQueries.Count];

				for (int ii = 0; ii < queries.Length; ii++)
				{
					queries[ii] = m_connectionQueries[ii];
				}
			}

			info.AddValue(Names.QUERIES, queries);
		}
		#endregion

		#region Opc.Dx.IServer Members
		/// <summary>
		/// Fetches all source servers in the current configuration.
		/// </summary>
		/// <returns>The list of source servers.</returns>
		public SourceServer[] GetSourceServers()
		{
			if (m_server == null) throw new NotConnectedException();

			// fetch source servers.
			SourceServer[] sourceServers = ((IServer)m_server).GetSourceServers();

			// update local cache.
			m_sourceServers.Initialize(sourceServers);

			// return results.
			return sourceServers;
		}

		/// <summary>
		/// Adds a new set of source servers to the current configuration.
		/// </summary>
		/// <param name="servers">The list of source servers to add.</param>
		/// <returns>The results of the operation for each source server.</returns>
		public GeneralResponse AddSourceServers(SourceServer[] servers)
		{
			if (m_server == null) throw new NotConnectedException();

			GeneralResponse response = ((IServer)m_server).AddSourceServers(servers);

			if (response != null)
			{
				// update cached source servers.
				GetSourceServers();

				// save configuration version.
				m_version = response.Version;
			}

			return response;
		}

		/// <summary>
		/// Modifies a set of source servers in the current configuration.
		/// </summary>
		/// <param name="servers">A list source source attributes.</param>
		/// <returns>The results of the operation for each source server.</returns>
		public GeneralResponse ModifySourceServers(SourceServer[] servers)
		{
			if (m_server == null) throw new NotConnectedException();

			GeneralResponse response = ((IServer)m_server).ModifySourceServers(servers);

			if (response != null)
			{
				// update cached source servers.
				GetSourceServers();

				// save configuration version.
				m_version = response.Version;
			}

			return response;
		}

		/// <summary>
		/// Deletes a set of source servers in the current configuration.
		/// </summary>
		/// <param name="servers">A list of source servers to delete.</param>
		/// <returns>The results of the operation for each source server.</returns>
		public GeneralResponse DeleteSourceServers(ItemIdentifier[] servers)
		{
			if (m_server == null) throw new NotConnectedException();

			GeneralResponse response = ((IServer)m_server).DeleteSourceServers(servers);

			if (response != null)
			{
				// update cached source servers.
				GetSourceServers();

				// save configuration version.
				m_version = response.Version;
			}

			return response;
		}

		/// <summary>
		/// Copies the default or runtime attributes for a set of source servers. 
		/// </summary>
		/// <param name="configToStatus">Whether the default attributes are copied to or copied from the runtime attributes.</param>
		/// <param name="servers">The set of source servers to modify.</param>
		/// <returns>The results of the operation for each source server.</returns>
		public GeneralResponse CopyDefaultSourceServerAttributes(
			bool             configToStatus, 
			ItemIdentifier[] servers)
		{
			if (m_server == null) throw new NotConnectedException();
			
			GeneralResponse response = ((IServer)m_server).CopyDefaultSourceServerAttributes(configToStatus, servers);

			if (response != null)
			{
				// update cached source servers if configuration changed.
				if (!configToStatus)
				{
					GetSourceServers();
				}

				// save configuration version.
				m_version = response.Version;
			}

			return response;
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
			if (m_server == null) throw new NotConnectedException();

			return ((IServer)m_server).QueryDXConnections(browsePath, connectionMasks, recursive, out errors);
		}

		/// <summary>
		/// Add a set of connections to the configuration.
		/// </summary>
		/// <param name="connections">The set of connections to add.</param>
		/// <returns>The results of the operation for each connection.</returns>
		public GeneralResponse AddDXConnections(DXConnection[] connections)
		{
			if (m_server == null) throw new NotConnectedException();

			GeneralResponse response = ((IServer)m_server).AddDXConnections(connections);

			// save configuration version.
			if (response != null)
			{
				m_version = response.Version;
			}

			return response;
		}
    	
		/// <summary>
		/// Modify a set of connections in the configuration.
		/// </summary>
		/// <param name="connections">The set of connections to modify.</param>
		/// <returns>The results of the operation for each connection.</returns>
		/// <remarks>Only explicitly specified attributes in the connection objects are changed.</remarks>
		public GeneralResponse ModifyDXConnections(DXConnection[] connections)
		{
			if (m_server == null) throw new NotConnectedException();

			GeneralResponse response = ((IServer)m_server).ModifyDXConnections(connections);

			// save configuration version.
			if (response != null)
			{
				m_version = response.Version;
			}

			return response;
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
			if (m_server == null) throw new NotConnectedException();

			GeneralResponse response = ((IServer)m_server).UpdateDXConnections(browsePath, connectionMasks, recursive, connectionDefinition, out errors);

			// save configuration version.
			if (response != null)
			{
				m_version = response.Version;
			}

			return response;
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
			DXConnection[] connectionMasks, 
			bool           recursive,
			out ResultID[] errors)
		{
			if (m_server == null) throw new NotConnectedException();

			GeneralResponse response = ((IServer)m_server).DeleteDXConnections(browsePath, connectionMasks, recursive, out errors);

			// save configuration version.
			if (response != null)
			{
				m_version = response.Version;
			}

			return response;
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
			if (m_server == null) throw new NotConnectedException();

			GeneralResponse response = ((IServer)m_server).CopyDXConnectionDefaultAttributes(configToStatus, browsePath, connectionMasks, recursive, out errors);

			// save configuration version.
			if (response != null)
			{
				m_version = response.Version;
			}

			return response;
		}

		/// <summary>
		/// Resets the current configuration,
		/// </summary>
		/// <param name="configurationVersion">The current configuration version.</param>
		/// <returns>The new configuration version.</returns>
		public string ResetConfiguration(string configurationVersion)
		{
			if (m_server == null) throw new NotConnectedException();

			m_version = ((IServer)m_server).ResetConfiguration((configurationVersion == null)?m_version:configurationVersion);

			return m_version;
		}
		#endregion

		#region Private Members
		private string m_version = null;
		private SourceServerCollection m_sourceServers = new SourceServerCollection();
		private DXConnectionQueryCollection m_connectionQueries = new DXConnectionQueryCollection();
		#endregion
	}
}
