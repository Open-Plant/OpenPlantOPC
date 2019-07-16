//============================================================================
// TITLE: Opc.Da.IServer.cs
//
// CONTENTS:
// 
// An interface that defines functionality that is common to all OPC Data Access servers.
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
// 2003/03/26 RSA   Initial implementation.
// 2004/02/18 RSA   Updated to conform with the .NET design guidelines.
// 2004/11/11 RSA   Added a base interfaces for BrowsePosition.

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace Opc.Da
{
	/// <summary>
	/// Defines functionality that is common to all OPC Data Access servers.
	/// </summary>
	public interface IServer : Opc.IServer
	{
		//======================================================================
		// Result Filters

		/// <summary>
		/// Returns the filters applied by the server to any item results returned to the client.
		/// </summary>
		/// <returns>A bit mask indicating which fields should be returned in any item results.</returns>
		int GetResultFilters();
		
		/// <summary>
		/// Sets the filters applied by the server to any item results returned to the client.
		/// </summary>
		/// <param name="filters">A bit mask indicating which fields should be returned in any item results.</param>
		void SetResultFilters(int filters);
		
		//======================================================================
		// GetStatus

		/// <summary>
		/// Returns the current server status.
		/// </summary>
		/// <returns>The current server status.</returns>
		ServerStatus GetStatus();

		//======================================================================
		// Read

		/// <summary>
		/// Reads the current values for a set of items. 
		/// </summary>
		/// <param name="items">The set of items to read.</param>
		/// <returns>The results of the read operation for each item.</returns>
		ItemValueResult[] Read(Item[] items);

		//======================================================================
		// Write

		/// <summary>
		/// Writes the value, quality and timestamp for a set of items.
		/// </summary>
		/// <param name="values">The set of item values to write.</param>
		/// <returns>The results of the write operation for each item.</returns>
		IdentifiedResult[] Write(ItemValue[] values);

		//======================================================================
		// CreateSubscription

		/// <summary>
		/// Creates a new subscription.
		/// </summary>
		/// <param name="state">The initial state of the subscription.</param>
		/// <returns>The new subscription object.</returns>
		ISubscription CreateSubscription(SubscriptionState state);
		
		//======================================================================
		// CancelSubscription

		/// <summary>
		/// Cancels a subscription and releases all resources allocated for it.
		/// </summary>
		/// <param name="subscription">The subscription to cancel.</param>
		void CancelSubscription(ISubscription subscription);

		//======================================================================
		// Browse

		/// <summary>
		/// Fetches the children of a branch that meet the filter criteria.
		/// </summary>
		/// <param name="itemID">The identifier of branch which is the target of the search.</param>
		/// <param name="filters">The filters to use to limit the set of child elements returned.</param>
		/// <param name="position">An object used to continue a browse that could not be completed.</param>
		/// <returns>The set of elements found.</returns>
		BrowseElement[] Browse(
			ItemIdentifier     itemID,
			BrowseFilters      filters, 
			out BrowsePosition position);

		//======================================================================
		// BrowseNext

		/// <summary>
		/// Continues a browse operation with previously specified search criteria.
		/// </summary>
		/// <param name="position">An object containing the browse operation state information.</param>
		/// <returns>The set of elements found.</returns>
		BrowseElement[] BrowseNext(ref BrowsePosition position);

		//======================================================================
		// GetProperties

		/// <summary>
		/// Returns the item properties for a set of items.
		/// </summary>
		/// <param name="itemIDs">A list of item identifiers.</param>
		/// <param name="propertyIDs">A list of properties to fetch for each item.</param>
		/// <param name="returnValues">Whether the property values should be returned with the properties.</param>
		/// <returns>A list of properties for each item.</returns>
		ItemPropertyCollection[] GetProperties(
			ItemIdentifier[] itemIDs,
			PropertyID[]     propertyIDs,
			bool             returnValues);
	}

	/// <summary>
	/// Filters applied by the server before returning item results.
	/// </summary>
	[Flags]
	public enum ResultFilter
	{		
		/// <summary>
		/// Include the ItemName in the ItemIdentifier if bit is set.
		/// </summary>
		ItemName = 0x01,

		/// <summary>
		/// Include the ItemPath in the ItemIdentifier if bit is set.
		/// </summary>
		ItemPath = 0x02,
				
		/// <summary>
		/// Include the ClientHandle in the ItemIdentifier if bit is set.
		/// </summary>
		ClientHandle = 0x04,
		
		/// <summary>
		/// Include the Timestamp in the ItemValue if bit is set.
		/// </summary>
		ItemTime = 0x08,
		
		/// <summary>
		/// Include verbose, localized error text with result if bit is set. 
		/// </summary>
		ErrorText = 0x10,

		/// <summary>
		/// Include additional diagnostic information with result if bit is set.
		/// </summary>
		DiagnosticInfo = 0x20,

		/// <summary>
		/// Include the ItemName and Timestamp if bit is set.
		/// </summary>
		Minimal = ItemName | ItemTime,

		/// <summary>
		/// Include all information in the results if bit is set.
		/// </summary>
		All = 0x3F
	}
	
	/// <summary>
	/// The set of possible server states.
	/// </summary>
	public enum serverState
	{
		/// <summary>
		/// The server state is not known.
		/// </summary>
		unknown,

		/// <summary>
		/// The server is running normally.
		/// </summary>
		running, 

		/// <summary>
		/// The server is not functioning due to a fatal error.
		/// </summary>
		failed, 

		/// <summary>
		/// The server cannot load its configuration information.
		/// </summary>
		noConfig, 

		/// <summary>
		/// The server has halted all communication with the underlying hardware.
		/// </summary>
		suspended, 

		/// <summary>
		/// The server is disconnected from the underlying hardware.
		/// </summary>
		test,

		/// <summary>
		/// The server cannot communicate with the underlying hardware.
		/// </summary>
		commFault
	}

	/// <summary>
	/// Contains properties that describe the current status of an OPC server.
	/// </summary>
	[Serializable]
	public class ServerStatus : ICloneable
	{
		/// <summary>
		/// The vendor name and product name for the server.
		/// </summary>
		public string VendorInfo
		{
			get { return m_vendorInfo;  }
			set { m_vendorInfo = value; }
		}

		/// <summary>
		/// A string that contains the server software version number.
		/// </summary>
		public string ProductVersion
		{
			get { return m_productVersion;  }
			set { m_productVersion = value; }
		}

		/// <summary>
		/// The current state of the server.
		/// </summary>
		public serverState ServerState
		{
			get { return m_serverState;  }
			set { m_serverState = value; }
		}

		/// <summary>
		/// A string that describes the current server state.
		/// </summary>
		public string StatusInfo
		{
			get { return m_statusInfo;  }
			set { m_statusInfo = value; }
		}

		/// <summary>
		/// The UTC time when the server started.
		/// </summary>
		public DateTime StartTime
		{
			get { return m_startTime;  }
			set { m_startTime = value; }
		}

		/// <summary>
		/// Th current UTC time at the server.
		/// </summary>
		public DateTime CurrentTime
		{
			get { return m_currentTime;  }
			set { m_currentTime = value; }
		}

		/// <summary>
		/// The last time the server sent an data update to the client.
		/// </summary>
		public DateTime LastUpdateTime
		{
			get { return m_lastUpdateTime;  }
			set { m_lastUpdateTime = value; }
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deepcopy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return MemberwiseClone(); 
		}
		#endregion

		#region Private Members
		private string m_vendorInfo = null;
		private string m_productVersion = null;
		private serverState m_serverState = serverState.unknown;
		private string m_statusInfo = null;
		private DateTime m_startTime = DateTime.MinValue;
		private DateTime m_currentTime = DateTime.MinValue;
		private DateTime m_lastUpdateTime = DateTime.MinValue;
		#endregion
	}
}
