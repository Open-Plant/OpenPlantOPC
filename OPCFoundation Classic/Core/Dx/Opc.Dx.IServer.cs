//============================================================================
// TITLE: Opc.Dx.IServer.cs
//
// CONTENTS:
// 
// The primary interface for a Data eXchange server.
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
	/// Defines functionality that is common to all OPC Data Access servers.
	/// </summary>
	public interface IServer : Opc.Da.IServer
	{		
		/// <summary>
		/// Fetches all source servers in the current configuration.
		/// </summary>
		/// <returns>The list of source servers.</returns>
		SourceServer[] GetSourceServers();

		/// <summary>
		/// Adds a new set of source servers to the current configuration.
		/// </summary>
		/// <param name="servers">The list of source servers to add.</param>
		/// <returns>The results of the operation for each source server.</returns>
		GeneralResponse AddSourceServers(SourceServer[] servers);

		/// <summary>
		/// Modifies a set of source servers in the current configuration.
		/// </summary>
		/// <param name="servers">A list source source attributes.</param>
		/// <returns>The results of the operation for each source server.</returns>
		GeneralResponse ModifySourceServers(SourceServer[] servers);

		/// <summary>
		/// Deletes a set of source servers in the current configuration.
		/// </summary>
		/// <param name="servers">A list of source servers to delete.</param>
		/// <returns>The results of the operation for each source server.</returns>
		GeneralResponse DeleteSourceServers(ItemIdentifier[] servers);

		/// <summary>
		/// Copies the default or runtime attributes for a set of source servers. 
		/// </summary>
		/// <param name="configToStatus">Whether the default attributes are copied to or copied from the runtime attributes.</param>
		/// <param name="servers">The set of source servers to modify.</param>
		/// <returns>The results of the operation for each source server.</returns>
		GeneralResponse CopyDefaultSourceServerAttributes(bool configToStatus, ItemIdentifier[] servers);

		/// <summary>
		/// Returns a list of connections that meet the specified criteria.
		/// </summary>
		/// <param name="browsePath">The browse path where the search begins.</param>
		/// <param name="connectionMasks">The masks that define the query criteria.</param>
		/// <param name="recursive">Whether the folders under the browse path are searched as well.</param>
		/// <param name="errors">Any errors associated with individual query masks.</param>
		/// <returns>The list of connections that meet the criteria.</returns>
		DXConnection[] QueryDXConnections(
			string         browsePath, 
			DXConnection[] connectionMasks, 
			bool           recursive,
			out ResultID[] errors
		);

		/// <summary>
		/// Add a set of connections to the configuration.
		/// </summary>
		/// <param name="connections">The set of connections to add.</param>
		/// <returns>The results of the operation for each connection.</returns>
		GeneralResponse AddDXConnections(DXConnection[] connections);
    	
		/// <summary>
		/// Modify a set of connections in the configuration.
		/// </summary>
		/// <param name="connections">The set of connections to modify.</param>
		/// <returns>The results of the operation for each connection.</returns>
		/// <remarks>Only explicitly specified attributes in the connection objects are changed.</remarks>
		GeneralResponse ModifyDXConnections(DXConnection[] connections);

		/// <summary>
		/// Updates a set of connections which meet the specified query criteria.
		/// </summary>
		/// <param name="browsePath">The browse path where the search begins.</param>
		/// <param name="connectionMasks">The masks that define the query criteria.</param>
		/// <param name="recursive">Whether the folders under the browse path are searched as well.</param>
		/// <param name="connectionDefinition">The changes that will be applied to all connections meeting the criteria.</param>
		/// <param name="errors">Any errors associated with individual query masks.</param>
		/// <returns>The list of connections that met the criteria and were updated.</returns>
		GeneralResponse UpdateDXConnections(
			string         browsePath, 
			DXConnection[] connectionMasks, 
			bool           recursive,
			DXConnection   connectionDefinition,
			out ResultID[] errors);

		/// <summary>
		/// Deletes a set of connections which meet the specified query criteria.
		/// </summary>
		/// <param name="browsePath">The browse path where the search begins.</param>
		/// <param name="connectionMasks">The masks that define the query criteria.</param>
		/// <param name="recursive">Whether the folders under the browse path are searched as well.</param>
		/// <param name="errors">Any errors associated with individual query masks.</param>
		/// <returns>The list of connections that met the criteria and were deleted.</returns>
		GeneralResponse DeleteDXConnections(
			string         browsePath, 
			DXConnection[]   connectionMasks, 
			bool           recursive,
			out ResultID[] errors);

		/// <summary>
		/// Changes the default or runtime attributes for a set of connections. 
		/// </summary>
		/// <param name="configToStatus">Whether the default attributes are copied to or copied from the runtime attributes.</param>
		/// <param name="browsePath">The browse path where the search begins.</param>
		/// <param name="connectionMasks">The masks that define the query criteria.</param>
		/// <param name="recursive">Whether the folders under the browse path are searched as well.</param>
		/// <param name="errors">Any errors associated with individual query masks.</param>
		/// <returns>The list of connections that met the criteria and were modified.</returns>
		GeneralResponse CopyDXConnectionDefaultAttributes(
			bool		   configToStatus,
			string         browsePath, 
			DXConnection[] connectionMasks, 
			bool           recursive,
			out ResultID[] errors);

		/// <summary>
		/// Resets the current configuration,
		/// </summary>
		/// <param name="configurationVersion">The current configuration version.</param>
		/// <returns>The new configuration version.</returns>
		string ResetConfiguration(string configurationVersion);
	}
}
