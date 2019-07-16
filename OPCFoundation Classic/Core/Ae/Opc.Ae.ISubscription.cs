//============================================================================
// TITLE: Opc.Ae.ISubscription.cs
//
// CONTENTS:
// 
// An interface to an object which implements a AE event subscription.
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
// Aete       By    Notes
// ---------- ---   -----
// 2004/11/08 RSA   Initial implementation.

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using Opc;

namespace Opc.Ae
{
	#region Opc.Ae.ISubscription Interface
	/// <summary>
	/// An interface to an object which implements a AE event subscription.
	/// </summary>
	public interface ISubscription : IDisposable
	{
		//======================================================================
		// Events

		/// <summary>
		/// An event to receive event change updates.
		/// </summary>
		event EventChangedEventHandler EventChanged;
		
		//======================================================================
		// State Management

		/// <summary>
		/// Returns the current state of the subscription.
		/// </summary>
		/// <returns>The current state of the subscription.</returns>
		SubscriptionState GetState();
		
		/// <summary>
		/// Changes the state of a subscription.
		/// </summary>
		/// <param name="masks">A bit mask that indicates which elements of the subscription state are changing.</param>
		/// <param name="state">The new subscription state.</param>
		/// <returns>The actual subscption state after applying the changes.</returns>
		SubscriptionState ModifyState(int masks, SubscriptionState state);

		//======================================================================
		// Filter Management

		/// <summary>
		/// Returns the current filters for the subscription.
		/// </summary>
		/// <returns>The current filters for the subscription.</returns>
		SubscriptionFilters GetFilters();

		/// <summary>
		/// Sets the current filters for the subscription.
		/// </summary>
		/// <param name="filters">The new filters to use for the subscription.</param>
		void SetFilters(SubscriptionFilters filters);

		//======================================================================
		// Attribute Management

		/// <summary>
		/// Returns the set of attributes to return with event notifications.
		/// </summary>		
		/// <param name="eventCategory">The specific event category for which the attributes apply.</param>
		/// <returns>The set of attribute ids which returned with event notifications.</returns>
		int[] GetReturnedAttributes(int eventCategory);

		/// <summary>
		/// Selects the set of attributes to return with event notifications.
		/// </summary>
		/// <param name="eventCategory">The specific event category for which the attributes apply.</param>
		/// <param name="attributeIDs">The list of attribute ids to return.</param>
		void SelectReturnedAttributes(int eventCategory, int[] attributeIDs);

		//======================================================================
		// Refresh

		/// <summary>
		/// Force a refresh for all active conditions and inactive, unacknowledged conditions whose event notifications match the filter of the event subscription.
		/// </summary>
		void Refresh();

		/// <summary>
		/// Cancels an outstanding refresh request.
		/// </summary>
		void CancelRefresh();
	}
	#endregion
 
	#region Delegate Declarations
	/// <summary>
	/// A delegate to receive data change updates from the server.
	/// </summary>
	public delegate void EventChangedEventHandler(EventNotification[] notifications, bool refresh, bool lastRefresh);
	#endregion 

	#region StateMask Enumeration
	/// <summary>
	/// Defines masks to be used when modifying the subscription or item state.
	/// </summary>
	[Flags]
	public enum StateMask
	{		
		/// <summary>
		/// A name assigned to subscription.
		/// </summary>
		Name = 0x0001,

		/// <summary>
		/// The client assigned handle for the item or subscription.
		/// </summary>
		ClientHandle = 0x0002,

		/// <summary>
		/// Whether the subscription is active.
		/// </summary>
		Active = 0x0004,

		/// <summary>
		/// The maximum rate at which the server send event notifications.
		/// </summary>
		BufferTime = 0x0008,

		/// <summary>
		/// The requested maximum number of events that will be sent in a single callback.
		/// </summary>
		MaxSize = 0x0010,

		/// <summary>
		/// The maximum period between updates sent to the client.
		/// </summary>
		KeepAlive = 0x0020,

		/// <summary>
		/// All fields are valid.
		/// </summary>
		All = 0xFFFF
	}
	#endregion

	#region EventType Enumeration
	/// <summary>
	/// The types of events that could be generated by a server.
	/// </summary>
	[Flags]
	public enum EventType
	{
		/// <summary>
		/// Events that are not tracking or condition events.
		/// </summary>
		Simple = 0x0001,

		/// <summary>
		/// Events that represent occurrences which involve the interaction of the client with a target within the server.
		/// </summary>
		Tracking = 0x0002,
		
		/// <summary>
		/// Events that are associated with transitions in and out states defined by the server.
		/// </summary>
		Condition = 0x0004,

		/// <summary>
		/// All events generated by the server.
		/// </summary>
		All = 0xFFFF
	}
	#endregion

	#region FilterType Enumeration
	/// <summary>
	/// The types of event filters that the server could support.
	/// </summary>
	[Flags]
	public enum FilterType
	{
		/// <summary>
		/// The server supports filtering by event type.
		/// </summary>
		Event = 0x0001,

		/// <summary>
		/// The server supports filtering by event categories.
		/// </summary>
		Category = 0x0002,

		/// <summary>
		/// The server supports filtering by severity levels.
		/// </summary>
		Severity = 0x0004,

		/// <summary>
		/// The server supports filtering by process area.
		/// </summary>
		Area = 0x0008,
		
		/// <summary>
		/// The server supports filtering by event sources.
		/// </summary>
		Source = 0x0010,
		
		/// <summary>
		/// All filters supported by the server.
		/// </summary>
		All = 0xFFFF
	}
	#endregion

	#region SubscriptionState Class
	/// <summary>
	/// Describes the state of a subscription.
	/// </summary>
	[Serializable]
	public class SubscriptionState : ICloneable
	{		
		#region Public Interface
		/// <summary>
		/// A descriptive name for the subscription.
		/// </summary>
		public string Name
		{
			get { return m_name;  }
			set { m_name = value; }
		}

		/// <summary>
		/// A unique identifier for the subscription assigned by the client.
		/// </summary>
		public object ClientHandle
		{
			get { return m_clientHandle;  }
			set { m_clientHandle = value; }
		}

		/// <summary>
		/// Whether the subscription is monitoring for events to send to the client.
		/// </summary>
		public bool Active
		{
			get { return m_active;  }
			set { m_active = value; }
		}

		/// <summary>
		/// The maximum rate at which the server send event notifications.
		/// </summary>
		public int BufferTime
		{
			get { return m_bufferTime;  }
			set { m_bufferTime = value; }
		}

		/// <summary>
		/// The requested maximum number of events that will be sent in a single callback.
		/// </summary>
		public int MaxSize
		{
			get { return m_maxSize;  }
			set { m_maxSize = value; }
		}

		/// <summary>
		/// The maximum period between updates sent to the client.
		/// </summary>
		public int KeepAlive
		{
			get { return m_keepAlive;  }
			set { m_keepAlive = value; }
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes object with default values.
		/// </summary>
		public SubscriptionState() 
		{
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a shallow copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return MemberwiseClone(); 
		}
		#endregion
		
		#region Private Members
		private string m_name = null;
		private object m_clientHandle = null;
		private bool m_active = true;
		private int m_bufferTime = 0;
		private int m_maxSize = 0;
		private int m_keepAlive = 0;
		#endregion
	}	
	#endregion

	#region SubscriptionFilters Class
	/// <summary>
	/// Describes the event filters for a subscription.
	/// </summary>
	[Serializable]
	public class SubscriptionFilters : ICloneable, ISerializable
	{		
		#region Public Interface
		/// <summary>
		/// A mask indicating which event types should be sent to the client.
		/// </summary>
		public int EventTypes
		{
			get { return m_eventTypes;  }
			set { m_eventTypes = value; }
		}

		/// <summary>
		/// The highest severity for the events that should be sent to the client.
		/// </summary>
		public int HighSeverity
		{
			get { return m_highSeverity;  }
			set { m_highSeverity = value; }
		}

		/// <summary>
		/// The lowest severity for the events that should be sent to the client.
		/// </summary>
		public int LowSeverity
		{
			get { return m_lowSeverity;  }
			set { m_lowSeverity = value; }
		}

		/// <summary>
		/// The category ids for the events that should be sent to the client.
		/// </summary>
		public CategoryCollection Categories
		{
			get { return m_categories; }
		}

		/// <summary>
		/// A list of full-qualified ids for process areas of interest - only events or conditions in these areas will be reported.
		/// </summary>
		public StringCollection Areas
		{
			get { return m_areas; }
		}
		
		/// <summary>
		/// A list of full-qualified ids for sources of interest - only events or conditions from these soucres will be reported.
		/// </summary>
		public StringCollection Sources
		{
			get { return m_sources; }
		}

		#region CategoryCollection Class
		/// <summary>
		/// Contains a writeable collection category ids.
		/// </summary>
		[Serializable]
		public class CategoryCollection : WriteableCollection
		{			
			/// <summary>
			/// An indexer for the collection.
			/// </summary>
			public new int this[int index]
			{
				get	{ return (int)Array[index]; }
			}

			/// <summary>
			/// Returns a copy of the collection as an array.
			/// </summary>
			public new int[] ToArray()
			{
				return (int[])Array.ToArray(typeof(int));
			}

			/// <summary>
			/// Creates an empty collection.
			/// </summary>
			internal CategoryCollection() : base(null, typeof(int)) {}

			#region ISerializable Members
			/// <summary>
			/// Contructs an object by deserializing it from a stream.
			/// </summary>
			protected CategoryCollection(SerializationInfo info, StreamingContext context) : base(info, context) {}
			#endregion
		}
		#endregion

		#region StringCollection Class
		/// <summary>
		/// Contains a writeable collection of strings.
		/// </summary>
		[Serializable]
		public class StringCollection : WriteableCollection
		{			
			/// <summary>
			/// An indexer for the collection.
			/// </summary>
			public new string this[int index]
			{
				get	{ return (string)Array[index]; }
			}

			/// <summary>
			/// Returns a copy of the collection as an array.
			/// </summary>
			public new string[] ToArray()
			{
				return (string[])Array.ToArray(typeof(string));
			}

			/// <summary>
			/// Creates an empty collection.
			/// </summary>
			internal StringCollection() : base(null, typeof(string)) {}

			#region ISerializable Members
			/// <summary>
			/// Contructs an object by deserializing it from a stream.
			/// </summary>
			protected StringCollection(SerializationInfo info, StreamingContext context) : base(info, context) {}
			#endregion
		}
		#endregion

		#endregion

		#region Constructors
		/// <summary>
		/// Initializes object with default values.
		/// </summary>
		public SubscriptionFilters() {}
		#endregion

		#region ISerializable Members
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string EVENT_TYPES   = "ET";
			internal const string CATEGORIES    = "CT";
			internal const string HIGH_SEVERITY = "HS";
			internal const string LOW_SEVERITY  = "LS";
			internal const string AREAS         = "AR";
			internal const string SOURCES       = "SR";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected SubscriptionFilters(SerializationInfo info, StreamingContext context)
		{
			m_eventTypes   = (int)info.GetValue(Names.EVENT_TYPES, typeof(int));
			m_categories   = (CategoryCollection)info.GetValue(Names.CATEGORIES, typeof(CategoryCollection));
			m_highSeverity = (int)info.GetValue(Names.HIGH_SEVERITY, typeof(int));
			m_lowSeverity  = (int)info.GetValue(Names.LOW_SEVERITY, typeof(int));
			m_areas        = (StringCollection)info.GetValue(Names.AREAS, typeof(StringCollection));
			m_sources      = (StringCollection)info.GetValue(Names.SOURCES, typeof(StringCollection));
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(Names.EVENT_TYPES, m_eventTypes);
			info.AddValue(Names.CATEGORIES, m_categories);
			info.AddValue(Names.HIGH_SEVERITY, m_highSeverity);
			info.AddValue(Names.LOW_SEVERITY, m_lowSeverity);
			info.AddValue(Names.AREAS, m_areas);
			info.AddValue(Names.SOURCES, m_sources);
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			SubscriptionFilters filters = (SubscriptionFilters)MemberwiseClone();

			filters.m_categories = (CategoryCollection)m_categories.Clone();
			filters.m_areas      = (StringCollection)m_areas.Clone();
			filters.m_sources    = (StringCollection)m_sources.Clone();

			return filters;
		}
		#endregion
		
		#region Private Members
		private int m_eventTypes = (int)EventType.All;
		private CategoryCollection m_categories = new CategoryCollection();
		private int m_highSeverity = 1000;
		private int m_lowSeverity = 1;
		private StringCollection m_areas = new StringCollection();
		private StringCollection m_sources = new StringCollection();
		#endregion
	}
	#endregion

	#region ChangeMask Enumeration
	/// <summary>
	/// The bits indicating what changes generated an event notification.
	/// </summary>
	[Flags]
	public enum ChangeMask
	{
		/// <summary>
		/// The condition’s active state has changed.
		/// </summary>
		ActiveState = 0x0001,

		/// <summary>
		/// The condition’s acknowledgment state has changed.
		/// </summary>
		AcknowledgeState = 0x0002,

		/// <summary>
		/// The condition’s enabled state has changed.
		/// </summary>
		EnableState = 0x0004,

		/// <summary>
		/// The condition quality has changed.
		/// </summary>
		Quality = 0x0008,

		/// <summary>
		/// The severity level has changed.
		/// </summary>
		Severity = 0x0010,

		/// <summary>
		/// The condition has transitioned into a new sub-condition.
		/// </summary>
		SubCondition = 0x0020,

		/// <summary>
		/// The event message has changed.
		/// </summary>
		Message = 0x0040,

		/// <summary>
		/// One or more event attributes have changed.
		/// </summary>
		Attribute = 0x0080
	}
	#endregion

	#region EventNotification Class
	/// <summary>
	/// A notification sent by the server when a event change occurs.
	/// </summary>
	[Serializable]
	public class EventNotification : ICloneable	
	{
		#region Public Interface
		/// <summary>
		/// The handle of the subscription that requested the notification
		/// </summary>
		public object ClientHandle
		{
			get { return m_clientHandle;  } 
			set { m_clientHandle = value; } 
		}

		/// <summary>
		/// The identifier for the source that generated the event.
		/// </summary>
		public string SourceID
		{
			get { return m_sourceID;  } 
			set { m_sourceID = value; } 
		}

		/// <summary>
		/// The time of the event occurrence.
		/// </summary>
		public DateTime Time
		{
			get { return m_time;  } 
			set { m_time = value; } 
		}

		/// <summary>
		/// Event notification message describing the event.
		/// </summary>
		public string Message
		{
			get { return m_message;  } 
			set { m_message = value; } 
		}

		/// <summary>
		/// The type of event that generated the notification.
		/// </summary>
		public EventType EventType
		{
			get { return m_eventType;  } 
			set { m_eventType = value; } 
		}

		/// <summary>
		/// The vendor defined category id for the event.
		/// </summary>
		public int EventCategory
		{
			get { return m_eventCategory;  } 
			set { m_eventCategory = value; } 
		}

		/// <summary>
		/// The severity of the event (1..1000).
		/// </summary>
		public int Severity
		{
			get { return m_severity;  } 
			set { m_severity = value; } 
		}

		/// <summary>
		/// The name of the condition related to this event notification.
		/// </summary>
		public string ConditionName
		{
			get { return m_conditionName;  } 
			set { m_conditionName = value; } 
		}

		/// <summary>
		/// The name of the current sub-condition, for multi-state conditions.
		/// For a single-state condition, this contains the condition name.
		/// </summary>
		public string SubConditionName
		{
			get { return m_subConditionName;  } 
			set { m_subConditionName = value; } 
		}

		/// <summary>
		/// The values of the attributes selected for the event subscription. 
		/// </summary>
		public AttributeCollection Attributes
		{
			get { return m_attributes; } 
		}

		/// <summary>
		/// Indicates which properties of the condition have changed, to have caused the server to send the event notification.
		/// </summary>
		public int ChangeMask
		{
			get { return m_changeMask;  } 
			set { m_changeMask = value; } 
		}

		/// <summary>
		/// A bit mask specifying the new state of the condition.
		/// </summary>
		public int NewState
		{
			get { return m_newState;  } 
			set { m_newState = value; } 
		}

		/// <summary>
		/// The quality associated with the condition state.
		/// </summary>
		public Opc.Da.Quality Quality
		{
			get { return m_quality;  } 
			set { m_quality = value; } 
		}

		/// <summary>
		/// Whether the related condition requires acknowledgment of this event.
		/// </summary>
		public bool AckRequired
		{
			get { return m_ackRequired;  } 
			set { m_ackRequired = value; } 
		}

		/// <summary>
		/// The time that the condition became active (for single-state conditions), or 
		/// the time of the transition into the current sub-condition (for multi-state conditions).
		/// </summary>
		public DateTime ActiveTime
		{
			get { return m_activeTime;  } 
			set { m_activeTime = value; } 
		}

		/// <summary>
		/// A server defined cookie associated with the event notification.
		/// </summary>
		public int Cookie
		{
			get { return m_cookie;  } 
			set { m_cookie = value; } 
		}

		/// <summary>
		/// For tracking events, this is the actor id for the event notification. 
		/// For condition-related events, this is the acknowledger id passed by the client.
		/// </summary>
		public string ActorID
		{
			get { return m_actorID;  } 
			set { m_actorID = value; } 
		}

		#region AttributeCollection Class
		/// <summary>
		/// Contains a read-only collection of AttributeValues.
		/// </summary>
		[Serializable]
		public class AttributeCollection : ReadOnlyCollection
		{			
			/// <summary>
			/// Creates an empty collection.
			/// </summary>
			internal AttributeCollection() : base(new object[0]) {}

			/// <summary>
			/// Creates a collection from an array of objects.
			/// </summary>
			internal AttributeCollection(object[] attributes) : base(attributes) {}
		}

		/// <summary>
		/// Sets the list of attribute values.
		/// </summary>
		public void SetAttributes(object[] attributes)
		{
			if (attributes == null)
			{
				m_attributes = new AttributeCollection();
			}
			else
			{
				m_attributes = new AttributeCollection(attributes);
			}
		}
		#endregion

		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			EventNotification clone = (EventNotification)MemberwiseClone();
			
			clone.m_attributes = (AttributeCollection)m_attributes.Clone();
			
			return clone;
		}
		#endregion
		
		#region Private Members
		private object m_clientHandle = null;
		private string m_sourceID = null;
		private DateTime m_time = DateTime.MinValue;
		private string m_message = null;
		private EventType m_eventType = EventType.Condition;
		private int m_eventCategory = 0;
		private int m_severity = 1;
		private string m_conditionName = null;
		private string m_subConditionName = null;
		private AttributeCollection m_attributes = new AttributeCollection();
		private int m_changeMask = 0;
		private int m_newState = 0;
		private Opc.Da.Quality m_quality = Opc.Da.Quality.Bad;
		private bool m_ackRequired = false;
		private DateTime m_activeTime = DateTime.MinValue;
		private int m_cookie = 0;
		private string m_actorID = null;
		#endregion
	}
	#endregion
}
