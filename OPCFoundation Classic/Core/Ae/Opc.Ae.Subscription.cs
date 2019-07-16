//============================================================================
// TITLE: Opc.Ae.Subscription.cs
//
// CONTENTS:
// 
// An in-process object which provides access to AE subscription objects.
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

namespace Opc.Ae
{
	#region Opc.Ae.Subscription Class
	/// <summary>
	/// An in-process object which provides access to AE subscription objects.
	/// </summary>
	[Serializable]
	public class Subscription : ISubscription, ISerializable, ICloneable
	{	
		#region Constructors
		/// <summary>
		/// Initializes object with default values.
		/// </summary>
		public Subscription(Server server, ISubscription subscription, SubscriptionState state)
		{
			if (server == null)       throw new ArgumentNullException("server");
			if (subscription == null) throw new ArgumentNullException("subscription");

			m_server       = server;
			m_subscription = subscription;
			m_state        = (SubscriptionState)state.Clone();
			m_name         = state.Name;
		}
		#endregion
        
        #region IDisposable Members
        /// <summary>
        /// The finalizer.
        /// </summary>
        ~Subscription()
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
                    if (m_subscription != null)
                    {
                        m_server.SubscriptionDisposed(this);
                        m_subscription.Dispose();
                    }
                }

                // Free your own state (unmanaged objects).
                // Set large fields to null.
                m_disposed = true;
            }
        }

        private bool m_disposed = false;
		#endregion

		#region ISerializable Members
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string STATE      = "ST";
			internal const string FILTERS    = "FT";
			internal const string ATTRIBUTES = "AT";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected Subscription(SerializationInfo info, StreamingContext context)
		{
			m_state      = (SubscriptionState)info.GetValue(Names.STATE, typeof(SubscriptionState));
			m_filters    = (SubscriptionFilters)info.GetValue(Names.FILTERS, typeof(SubscriptionFilters));
			m_attributes = (AttributeDictionary)info.GetValue(Names.ATTRIBUTES, typeof(AttributeDictionary));

			m_name = m_state.Name;

			m_categories = new CategoryCollection(m_filters.Categories.ToArray());
			m_areas      = new StringCollection(m_filters.Areas.ToArray());
			m_sources    = new StringCollection(m_filters.Sources.ToArray());
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(Names.STATE, m_state);
			info.AddValue(Names.FILTERS, m_filters);
			info.AddValue(Names.ATTRIBUTES, m_attributes);
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Returns an unconnected copy of the subscription with the same items.
		/// </summary>
		public virtual object Clone()
		{
			// do a memberwise clone.
			Subscription clone = (Subscription)MemberwiseClone();
			
			/*
			// place clone in disconnected state.
			clone.m_server       = null;
			clone.m_subscription = null;
			clone.m_state        = (SubscriptionState)m_state.Clone();

			// clear server handles.
			clone.m_state.ServerHandle = null;

			// always make cloned subscriptions inactive.
			clone.m_state.Active = false;

			// clone items.
			if (clone.m_items != null)
			{
				ArrayList items = new ArrayList();

				foreach (Item item in clone.m_items)
				{
					items.Add(item.Clone());
				}

				clone.m_items = (Item[])items.ToArray(typeof(Item));
			}
			*/
			
			// return clone.
			return clone;
		}
		#endregion

		#region Public Interface
		//======================================================================
		// Public Properties

		/// <summary>
		/// The server that the subscription object belongs to.
		/// </summary>
		public Opc.Ae.Server Server
		{
			get { return m_server; }
		}

		/// <summary>
		/// A descriptive name for the subscription.
		/// </summary>
		public string Name
		{
			get { return m_state.Name; }
		}

		/// <summary>
		/// A unique identifier for the subscription assigned by the client.
		/// </summary>
		public object ClientHandle
		{
			get { return m_state.ClientHandle; }
		}

		/// <summary>
		/// Whether the subscription is monitoring for events to send to the client.
		/// </summary>
		public bool Active
		{
			get { return m_state.Active; }
		}

		/// <summary>
		/// The maximum rate at which the server send event notifications.
		/// </summary>
		public int BufferTime
		{
			get { return m_state.BufferTime; }
		}

		/// <summary>
		/// The requested maximum number of events that will be sent in a single callback.
		/// </summary>
		public int MaxSize
		{
			get { return m_state.MaxSize; }
		}

		/// <summary>
		/// The maximum period between updates sent to the client.
		/// </summary>
		public int KeepAlive
		{
			get { return m_state.KeepAlive; }
		}

		/// <summary>
		/// A mask indicating which event types should be sent to the client.
		/// </summary>
		public int EventTypes
		{
			get { return m_filters.EventTypes; }
		}

		/// <summary>
		/// The highest severity for the events that should be sent to the client.
		/// </summary>
		public int HighSeverity
		{
			get { return m_filters.HighSeverity; }
		}

		/// <summary>
		/// The lowest severity for the events that should be sent to the client.
		/// </summary>
		public int LowSeverity
		{
			get { return m_filters.LowSeverity; }
		}

		/// <summary>
		/// The event category ids monitored by this subscription.
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
			get { return m_areas;  }
		}
		
		/// <summary>
		/// A list of full-qualified ids for sources of interest - only events or conditions from these soucres will be reported.
		/// </summary>
		public StringCollection Sources
		{
			get { return m_sources;  }
		}
		
		/// <summary>
		/// The list of attributes returned for each event category.
		/// </summary>
		public AttributeDictionary Attributes
		{
			get { return m_attributes; }
		}

		/// <summary>
		/// Returns a writeable copy of the current attributes.
		/// </summary>
		public Opc.Ae.AttributeDictionary GetAttributes()
		{
			Opc.Ae.AttributeDictionary attributes = new Opc.Ae.AttributeDictionary();

			IDictionaryEnumerator enumerator = m_attributes.GetEnumerator();

			while (enumerator.MoveNext())
			{
				int categoryID = (int)enumerator.Key;
				AttributeCollection attributeIDs = (AttributeCollection)enumerator.Value;

				attributes.Add(categoryID, attributeIDs.ToArray());
			}

			return attributes;
		}
		
		#region CategoryCollection Class
		/// <summary>
		/// Contains a read-only collection category ids.
		/// </summary>
		public class CategoryCollection : ReadOnlyCollection
		{			
			/// <summary>
			/// An indexer for the collection.
			/// </summary>
			public new int this[int index]
			{
				get	{ return (int)Array.GetValue(index); }
			}

			/// <summary>
			/// Returns a copy of the collection as an array.
			/// </summary>
			public new int[] ToArray()
			{
				return (int[])Opc.Convert.Clone(Array);
			}

			/// <summary>
			/// Creates an empty collection.
			/// </summary>
			internal CategoryCollection() : base(new int[0]) {}

			/// <summary>
			/// Creates a collection containing the list of category ids.
			/// </summary>
			internal CategoryCollection(int[] categoryIDs) : base(categoryIDs) {}
		}
		#endregion

		#region StringCollection Class
		/// <summary>
		/// Contains a read-only collection of strings.
		/// </summary>
		public class StringCollection : ReadOnlyCollection
		{			
			/// <summary>
			/// An indexer for the collection.
			/// </summary>
			public new string this[int index]
			{
				get	{ return (string)Array.GetValue(index); }
			}

			/// <summary>
			/// Returns a copy of the collection as an array.
			/// </summary>
			public new string[] ToArray()
			{
				return (string[])Opc.Convert.Clone(Array);
			}

			/// <summary>
			/// Creates an empty collection.
			/// </summary>
			internal StringCollection() : base(new string[0]) {}

			/// <summary>
			/// Creates a collection containing the specified strings.
			/// </summary>
			internal StringCollection(string[] strings) : base(strings) {}
		}
		#endregion

		#region AttributeDictionary Class
		/// <summary>
		/// Contains a read-only dictionary of attribute lists indexed by category id..
		/// </summary>
		[Serializable]
		public class AttributeDictionary : ReadOnlyDictionary
		{			
			/// <summary>
			/// Gets or sets the atrtibute collection for the specified category. 
			/// </summary>
			public AttributeCollection this[int categoryID]
			{
				get { return (AttributeCollection)base[categoryID]; }
			}

			/// <summary>
			/// Creates an empty collection.
			/// </summary>
			internal AttributeDictionary() : base(null) {}

			/// <summary>
			/// Constructs an dictionary from a set of category ids.
			/// </summary>
			internal AttributeDictionary(Hashtable dictionary) : base(dictionary) {}

			/// <summary>
			/// Adds or replaces the set of attributes associated with the category.
			/// </summary>
			internal void Update(int categoryID, int[] attributeIDs)
			{
				Dictionary[categoryID] = new AttributeCollection(attributeIDs);
			}

			#region ISerializable Members
			/// <summary>
			/// Contructs an object by deserializing it from a stream.
			/// </summary>
			protected AttributeDictionary(SerializationInfo info, StreamingContext context) : base(info, context) {}
			#endregion
		}

		#region AttributeCollection Class
		/// <summary>
		/// Contains a read-only collection attribute ids.
		/// </summary>
		[Serializable]
		public class AttributeCollection : ReadOnlyCollection
		{			
			/// <summary>
			/// An indexer for the collection.
			/// </summary>
			public new int this[int index]
			{
				get	{ return (int)Array.GetValue(index); }
			}

			/// <summary>
			/// Returns a copy of the collection as an array.
			/// </summary>
			public new int[] ToArray()
			{
				return (int[])Opc.Convert.Clone(Array);
			}

			/// <summary>
			/// Creates an empty collection.
			/// </summary>
			internal AttributeCollection() : base(new int[0]) {}

			/// <summary>
			/// Creates a collection containing the specified attribute ids.
			/// </summary>
			internal AttributeCollection(int[] attributeIDs) : base(attributeIDs) {}
			
			#region ISerializable Members
			/// <summary>
			/// Contructs an object by deserializing it from a stream.
			/// </summary>
			protected AttributeCollection(SerializationInfo info, StreamingContext context) : base(info, context) {}
			#endregion
		}
		#endregion
		#endregion
		
		#endregion

		#region Opc.Ae.ISubscription Members
		/// <summary>
		/// An event to receive data change updates.
		/// </summary>
		public event EventChangedEventHandler EventChanged
		{ 
			add    { m_subscription.EventChanged += value; }
			remove { m_subscription.EventChanged -= value; }
		}
		
		//======================================================================
		// State Management

		/// <summary>
		/// Returns the current state of the subscription.
		/// </summary>
		/// <returns>The current state of the subscription.</returns>
		public SubscriptionState GetState()
		{
			if (m_subscription == null) throw new NotConnectedException();

			m_state = m_subscription.GetState(); 
			m_state.Name = m_name;

			return (SubscriptionState)m_state.Clone();
		}
		
		/// <summary>
		/// Changes the state of a subscription.
		/// </summary>
		/// <param name="masks">A bit mask that indicates which elements of the subscription state are changing.</param>
		/// <param name="state">The new subscription state.</param>
		/// <returns>The actual subscription state after applying the changes.</returns>
		public SubscriptionState ModifyState(int masks, SubscriptionState state)
		{
			if (m_subscription == null) throw new NotConnectedException();

			m_state = m_subscription.ModifyState(masks, state); 

			if ((masks & (int)StateMask.Name) != 0)
			{
				m_state.Name = m_name = state.Name;
			}
			else
			{
				m_state.Name = m_name;
			}

			return (SubscriptionState)m_state.Clone();
		}

		//======================================================================
		// Filter Management

		/// <summary>
		/// Returns the current filters for the subscription.
		/// </summary>
		/// <returns>The current filters for the subscription.</returns>
		public SubscriptionFilters GetFilters()
		{
			if (m_subscription == null) throw new NotConnectedException();

			m_filters    = m_subscription.GetFilters(); 			
			m_categories = new CategoryCollection(m_filters.Categories.ToArray());
			m_areas      = new StringCollection(m_filters.Areas.ToArray());
			m_sources    = new StringCollection(m_filters.Sources.ToArray());

			return (SubscriptionFilters)m_filters.Clone();
		}

		/// <summary>
		/// Sets the current filters for the subscription.
		/// </summary>
		/// <param name="filters">The new filters to use for the subscription.</param>
		public void SetFilters(SubscriptionFilters filters)
		{
			if (m_subscription == null) throw new NotConnectedException();

			m_subscription.SetFilters(filters); 

			GetFilters();
		}

		//======================================================================
		// Attribute Management

		/// <summary>
		/// Returns the set of attributes to return with event notifications.
		/// </summary>
		/// <returns>The set of attributes to returned with event notifications.</returns>
		public int[] GetReturnedAttributes(int eventCategory)
		{
			if (m_subscription == null) throw new NotConnectedException();

			int[] attributeIDs = m_subscription.GetReturnedAttributes(eventCategory);

			m_attributes.Update(eventCategory, (int[])Opc.Convert.Clone(attributeIDs));

			return attributeIDs;
		}

		/// <summary>
		/// Selects the set of attributes to return with event notifications.
		/// </summary>
		/// <param name="eventCategory">The specific event category for which the attributes apply.</param>
		/// <param name="attributeIDs">The list of attribute ids to return.</param>
		public void SelectReturnedAttributes(int eventCategory, int[] attributeIDs)
		{
			if (m_subscription == null) throw new NotConnectedException();
		
			m_subscription.SelectReturnedAttributes(eventCategory, attributeIDs);

			m_attributes.Update(eventCategory, (int[])Opc.Convert.Clone(attributeIDs));
		}

		//======================================================================
		// Refresh

		/// <summary>
		/// Force a refresh for all active conditions and inactive, unacknowledged conditions whose event notifications match the filter of the event subscription.
		/// </summary>
		public void Refresh()
		{
			if (m_subscription == null) throw new NotConnectedException();
		
			m_subscription.Refresh();
		}

		/// <summary>
		/// Cancels an outstanding refresh request.
		/// </summary>
		public void CancelRefresh()
		{
			if (m_subscription == null) throw new NotConnectedException();

			m_subscription.CancelRefresh();
		}
		#endregion
		
		#region Private Methods
		/// <summary>
		/// The current state.
		/// </summary>
		internal SubscriptionState State
		{
			get { return m_state; }
		}

		/// <summary>
		/// The current filters.
		/// </summary>
		internal SubscriptionFilters Filters
		{
			get { return m_filters; }
		}
		#endregion

		#region Private Members
		private Opc.Ae.Server m_server = null;
		private Opc.Ae.ISubscription m_subscription = null;

		// state.
		private SubscriptionState m_state = new SubscriptionState();
		private string m_name = null;

		// filters.
		private SubscriptionFilters m_filters = new SubscriptionFilters();
		private CategoryCollection m_categories = new CategoryCollection();
		private StringCollection m_areas = new StringCollection();
		private StringCollection m_sources = new StringCollection();

		// returned attributes
		private AttributeDictionary m_attributes = new AttributeDictionary();
		#endregion
	}
	#endregion
}
