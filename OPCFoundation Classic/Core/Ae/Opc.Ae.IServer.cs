//============================================================================
// TITLE: Opc.Ae.IServer.cs
//
// CONTENTS:
// 
// The primary interface for an Alarms and Events server.
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
// 2004/11/08 RSA   Initial implementation.

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace Opc.Ae
{
	#region Opc.Ae.IServer Interface
	/// <summary>
	/// Defines functionality that is common to all OPC Data Access servers.
	/// </summary>
	public interface IServer : Opc.IServer
	{		
		//======================================================================
		// Get Status

		/// <summary>
		/// Returns the current server status.
		/// </summary>
		/// <returns>The current server status.</returns>
		ServerStatus GetStatus();

		//======================================================================
		// Event Subscription

		/// <summary>
		/// Creates a new event subscription.
		/// </summary>
		/// <param name="state">The initial state for the subscription.</param>
		/// <returns>The new subscription object.</returns>
		ISubscription CreateSubscription(SubscriptionState state);

		//======================================================================
		// QueryAvailableFilters

		/// <summary>
		/// Returns the event filters supported by the server.
		/// </summary>
		/// <returns>A bit mask of all event filters supported by the server.</returns>
		int QueryAvailableFilters();

		//======================================================================
		// QueryEventCategories

		/// <summary>		
		/// Returns the event categories supported by the server for the specified event types.
		/// </summary>
		/// <param name="eventType">A bit mask for the event types of interest.</param>
		/// <returns>A collection of event categories.</returns>
		Category[] QueryEventCategories(int eventType);

		//======================================================================
		// QueryConditionNames

		/// <summary>
		/// Returns the condition names supported by the server for the specified event categories.
		/// </summary>
		/// <param name="eventCategory">A bit mask for the event categories of interest.</param>
		/// <returns>A list of condition names.</returns>
		string[] QueryConditionNames(int eventCategory);

		//======================================================================
		// QuerySubConditionNames

		/// <summary>
		/// Returns the sub-condition names supported by the server for the specified event condition.
		/// </summary>
		/// <param name="conditionName">The name of the condition.</param>
		/// <returns>A list of sub-condition names.</returns>
		string[] QuerySubConditionNames(string conditionName);

		//======================================================================
		// QuerySourceConditions

		/// <summary>
		/// Returns the condition names supported by the server for the specified event source.
		/// </summary>
		/// <param name="sourceName">The name of the event source.</param>
		/// <returns>A list of condition names.</returns>
		string[] QueryConditionNames(string sourceName);

		//======================================================================
		// QueryEventAttributes

		/// <summary>		
		/// Returns the event attributes supported by the server for the specified event categories.
		/// </summary>
		/// <param name="eventCategory">The event category of interest.</param>
		/// <returns>A collection of event attributes.</returns>
		Attribute[] QueryEventAttributes(int eventCategory);

		//======================================================================
		// TranslateToItemIDs

		/// <summary>
		/// Returns the DA item ids for a set of attribute ids belonging to events which meet the specified filter criteria.
		/// </summary>
		/// <param name="sourceName">The event source of interest.</param>
		/// <param name="eventCategory">The id of the event category for the events of interest.</param>
		/// <param name="conditionName">The name of a condition within the event category.</param>
		/// <param name="subConditionName">The name of a sub-condition within a multi-state condition.</param>
		/// <param name="attributeIDs">The ids of the attributes to return item ids for.</param>
		/// <returns>A list of item urls for each specified attribute.</returns>
		ItemUrl[] TranslateToItemIDs(
			string sourceName,
			int    eventCategory,
			string conditionName,
			string subConditionName,
			int[]  attributeIDs);

		//======================================================================
		// GetConditionState

		/// <summary>
		/// Returns the current state information for the condition instance corresponding to the source and condition name.
		/// </summary>
		/// <param name="sourceName">The source name</param>
		/// <param name="conditionName">A condition name for the source.</param>
		/// <param name="attributeIDs">The list of attributes to return with the condition state.</param>
		/// <returns>The current state of the connection.</returns>
		Condition GetConditionState(
			string sourceName,
			string conditionName,
			int[]  attributeIDs);

		//======================================================================
		// EnableConditionByArea

		/// <summary>
		/// Places the specified process areas into the enabled state.
		/// </summary>
		/// <param name="areas">A list of fully qualified area names.</param>
		/// <returns>The results of the operation for each area.</returns>
		ResultID[] EnableConditionByArea(string[] areas);
		
		//======================================================================
		// DisableConditionByArea

		/// <summary>
		/// Places the specified process areas into the disabled state.
		/// </summary>
		/// <param name="areas">A list of fully qualified area names.</param>
		/// <returns>The results of the operation for each area.</returns>
		ResultID[] DisableConditionByArea(string[] areas);

		//======================================================================
		// EnableConditionBySource

		/// <summary>
		/// Places the specified process areas into the enabled state.
		/// </summary>
		/// <param name="sources">A list of fully qualified source names.</param>
		/// <returns>The results of the operation for each area.</returns>
		ResultID[] EnableConditionBySource(string[] sources);

		//======================================================================
		// DisableConditionBySource

		/// <summary>
		/// Places the specified process areas into the disabled state.
		/// </summary>
		/// <param name="sources">A list of fully qualified source names.</param>
		/// <returns>The results of the operation for each area.</returns>
		ResultID[] DisableConditionBySource(string[] sources);

		//======================================================================
		// GetEnableStateByArea

		/// <summary>
		/// Returns the enabled state for the specified process areas. 
		/// </summary>
		/// <param name="areas">A list of fully qualified area names.</param>
		EnabledStateResult[] GetEnableStateByArea(string[] areas);

		//======================================================================
		// GetEnableStateBySource

		/// <summary>
		/// Returns the enabled state for the specified event sources. 
		/// </summary>
		/// <param name="sources">A list of fully qualified source names.</param>
		EnabledStateResult[] GetEnableStateBySource(string[] sources);

		//======================================================================
		// AcknowledgeCondition

		/// <summary>
		/// Used to acknowledge one or more conditions in the event server.
		/// </summary>
		/// <param name="acknowledgerID">The identifier for who is acknowledging the condition.</param>
		/// <param name="comment">A comment associated with the acknowledgment.</param>
		/// <param name="conditions">The conditions being acknowledged.</param>
		/// <returns>A list of result id indictaing whether each condition was successfully acknowledged.</returns>
		ResultID[] AcknowledgeCondition(
			string                 acknowledgerID,
			string                 comment,
			EventAcknowledgement[] conditions);

		//======================================================================
		// Browse

		/// <summary>
		/// Browses for all children of the specified area that meet the filter criteria.
		/// </summary>
		/// <param name="areaID">The full-qualified id for the area.</param>
		/// <param name="browseType">The type of children to return.</param>
		/// <param name="browseFilter">The expression used to filter the names of children returned.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		BrowseElement[] Browse(
			string     areaID,
			BrowseType browseType, 
			string     browseFilter);

		/// <summary>
		/// Browses for all children of the specified area that meet the filter criteria.
		/// </summary>
		/// <param name="areaID">The full-qualified id for the area.</param>
		/// <param name="browseType">The type of children to return.</param>
		/// <param name="browseFilter">The expression used to filter the names of children returned.</param>
		/// <param name="maxElements">The maximum number of elements to return.</param>
		/// <param name="position">The object used to continue the browse if the number nodes exceeds the maximum specified.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		BrowseElement[] Browse(
			string              areaID,
			BrowseType          browseType, 
			string              browseFilter, 
			int                 maxElements,
			out IBrowsePosition position);

		//======================================================================
		// BrowseNext

		/// <summary>
		/// Continues browsing the server's address space at the specified position.
		/// </summary>
		/// <param name="maxElements">The maximum number of elements to return.</param>
		/// <param name="position">The position object used to continue a browse operation.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		BrowseElement[] BrowseNext(int maxElements, ref IBrowsePosition position);	
	}
	#endregion

	#region ServerState Enumeration
	/// <summary>
	/// The set of possible server states.
	/// </summary>
	public enum ServerState
	{
		/// <summary>
		/// The server state is not known.
		/// </summary>
		Unknown,

		/// <summary>
		/// The server is running normally.
		/// </summary>
		Running, 

		/// <summary>
		/// The server is not functioning due to a fatal error.
		/// </summary>
		Failed, 

		/// <summary>
		/// The server cannot load its configuration information.
		/// </summary>
		NoConfig, 

		/// <summary>
		/// The server has halted all communication with the underlying hardware.
		/// </summary>
		Suspended, 

		/// <summary>
		/// The server is disconnected from the underlying hardware.
		/// </summary>
		Test,

		/// <summary>
		/// The server cannot communicate with the underlying hardware.
		/// </summary>
		CommFault
	}
	#endregion

	#region ServerStatus Class
	/// <summary>
	/// Contains properties that describe the current status of an OPC server.
	/// </summary>
	[Serializable]
	public class ServerStatus : ICloneable
	{
		#region Public Interface
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
		public ServerState ServerState
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
		#endregion

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
		private ServerState m_serverState = ServerState.Unknown;
		private string m_statusInfo = null;
		private DateTime m_startTime = DateTime.MinValue;
		private DateTime m_currentTime = DateTime.MinValue;
		private DateTime m_lastUpdateTime = DateTime.MinValue;
		#endregion
	}
	#endregion

	#region EventAcknowledgement Class
	/// <summary>
	/// Specifies the information required to acknowledge an event.
	/// </summary>
	[Serializable]
	public class EventAcknowledgement : ICloneable	
	{
		#region Public Interface
		/// <summary>
		/// The name of the source that generated the event.
		/// </summary>
		public string SourceName
		{
			get { return m_sourceName;  } 
			set { m_sourceName = value; } 
		}

		/// <summary>
		/// The name of the condition that is being acknowledged.
		/// </summary>
		public string ConditionName
		{
			get { return m_conditionName;  } 
			set { m_conditionName = value; } 
		}

		/// <summary>
		/// The time that the condition or sub-condition became active.
		/// </summary>
		public DateTime ActiveTime
		{
			get { return m_activeTime;  } 
			set { m_activeTime = value; } 
		}

		/// <summary>
		/// The cookie for the condition passed to client during the event notification.
		/// </summary>
		public int Cookie
		{
			get { return m_cookie;  } 
			set { m_cookie = value; } 
		}

		/// <summary>
		/// Constructs an acknowledgment with its default values.
		/// </summary>
		public EventAcknowledgement() {}

		/// <summary>
		/// Constructs an acknowledgment from an event notification.
		/// </summary>
		public EventAcknowledgement(EventNotification notification)
		{
			m_sourceName    = notification.SourceID;
			m_conditionName = notification.ConditionName;
			m_activeTime    = notification.ActiveTime;
			m_cookie        = notification.Cookie;
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return MemberwiseClone();
		}
		#endregion
		
		#region Private Members
		private string m_sourceName = null;
		private string m_conditionName = null;
		private DateTime m_activeTime = DateTime.MinValue;
		private int m_cookie = 0;
		#endregion
	}
	#endregion

	#region EnabledStateResult Class
	/// <summary>
	/// The current state of a process area or an event source.
	/// </summary>
	public class EnabledStateResult : Opc.IResult
	{
		#region Public Interface
		/// <summary>
		/// Whether if the area or source is enabled.
		/// </summary>
		public bool Enabled
		{
			get { return m_enabled;  }
			set { m_enabled = value; }
		}

		/// <summary>
		/// Whether the area or source is enabled and all areas within the hierarchy of its containing areas are enabled. 
		/// </summary>
		public bool EffectivelyEnabled
		{
			get { return m_effectivelyEnabled;  }
			set { m_effectivelyEnabled = value; }
		}
		#endregion
		
		#region IResult Members
		/// <summary>
		/// The error id for the result of an operation on an item.
		/// </summary>
		public ResultID ResultID 
		{
			get { return m_resultID;  }
			set { m_resultID = value; }
		}	

		/// <summary>
		/// Vendor specific diagnostic information (not the localized error text).
		/// </summary>
		public string DiagnosticInfo
		{
			get { return m_diagnosticInfo;  }
			set { m_diagnosticInfo = value; }
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		public EnabledStateResult() {}

		/// <summary>
		/// Initializes the object with an qualified name.
		/// </summary>
		public EnabledStateResult(string qualifiedName)
		{
			m_qualifiedName = qualifiedName;
		}
		
		/// <summary>
		/// Initializes the object with an qualified name and ResultID.
		/// </summary>
		public EnabledStateResult(string qualifiedName, ResultID resultID)
		{			
			m_qualifiedName = qualifiedName;
			m_resultID      = ResultID;
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return MemberwiseClone();
		}
		#endregion

		#region Private Members
		private string m_qualifiedName = null;
		private bool m_enabled = false;
		private bool m_effectivelyEnabled = false;
		private ResultID m_resultID = ResultID.S_OK;
		private string m_diagnosticInfo = null;
		#endregion
	}
	#endregion
}
