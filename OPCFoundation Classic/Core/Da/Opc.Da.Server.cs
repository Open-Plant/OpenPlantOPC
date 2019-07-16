//============================================================================
// TITLE: Opc.Da.Server.cs
//
// CONTENTS:
// 
// A class which is an in-process object used to access OPC Data Access servers.
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

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace Opc.Da
{
	/// <summary>
	/// An in-process object used to access OPC Data Access servers.
	/// </summary>
	[Serializable]
	public class Server : Opc.Server, IServer 
	{
		//======================================================================
		// Construction

		/// <summary>
		/// Initializes the object with a factory and a default URL.
		/// </summary>
		/// <param name="factory">The Opc.Factory used to connect to remote servers.</param>
		/// <param name="url">The network address of a remote server.</param>
		public Server(Factory factory, URL url) 
		:
			base(factory, url)
		{
		}

		//======================================================================
		// ISerializable

		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string FILTERS       = "Filters";
			internal const string SUBSCRIPTIONS = "Subscription";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected Server(SerializationInfo info, StreamingContext context)
		:
			base(info, context)
		{
			m_filters = (int)info.GetValue(Names.FILTERS, typeof(int));
		
			Subscription[] subscriptions = (Subscription[])info.GetValue(Names.SUBSCRIPTIONS, typeof(Subscription[]));

			if (subscriptions != null)
			{
				foreach (Subscription subscription in subscriptions)
				{
					m_subscriptions.Add(subscription);
				}
			}
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue(Names.FILTERS, m_filters);

			Subscription[] subscriptions = null;

			if (m_subscriptions.Count > 0)
			{
				subscriptions = new Subscription[m_subscriptions.Count];

				for (int ii = 0; ii < subscriptions.Length; ii++)
				{
					subscriptions[ii] = m_subscriptions[ii];
				}
			}

			info.AddValue(Names.SUBSCRIPTIONS, subscriptions);
		}

		//======================================================================
		// ICloneable

		/// <summary>
		/// Returns an unconnected copy of the server with the same URL. 
		/// </summary>
		public override object Clone()
		{
			// clone the base object.
			Server clone = (Server)base.Clone();
			
			// clone subscriptions.
			if (clone.m_subscriptions != null)
			{
				SubscriptionCollection subscriptions = new SubscriptionCollection();

				foreach (Subscription subscription in clone.m_subscriptions)
				{
					subscriptions.Add(subscription.Clone());
				}

				clone.m_subscriptions = subscriptions;
			}

			// return clone.
			return clone;
		}

		//======================================================================
		// Public Properties		
		
		/// <summary>
		/// Returns an array of all subscriptions for the server.
		/// </summary>
		public SubscriptionCollection Subscriptions 
		{
			get	{ return m_subscriptions; }
		}

		/// <summary>
		/// The current result filters applied by the server.
		/// </summary>
		public int Filters {get{ return m_filters; }}	
		
		//======================================================================
		// Connection Management	

		/// <summary>
		/// Connects to the server with the specified URL and credentials.
		/// </summary>
		public override void Connect(URL url, ConnectData connectData)
		{ 
			// connect to server.
			base.Connect(url, connectData);

			// all done if no subscriptions.
			if (m_subscriptions == null)
			{
				return;
			}

			// create subscriptions (should only happen if server has been deserialized).
			SubscriptionCollection subscriptions = new SubscriptionCollection();

			foreach (Subscription template in m_subscriptions)
			{
				// create subscription for template.
				try   { subscriptions.Add(EstablishSubscription(template)); }
				catch {}
			}

			// save new set of subscriptions.
			m_subscriptions = subscriptions;
		}

		/// <summary>
		/// Disconnects from the server and releases all network resources.
		/// </summary>
		public override void Disconnect() 
		{
			if (m_server == null) throw new NotConnectedException();

			// dispose of all subscriptions first.
			if (m_subscriptions != null)
			{
				foreach (Subscription subscription in m_subscriptions)
				{
					subscription.Dispose();
				}

				m_subscriptions = null;
			}

			// disconnect from server.
			base.Disconnect();
		}

		//======================================================================
		// Result Filters

		/// <summary>
		/// Returns the filters applied by the server to any item results returned to the client.
		/// </summary>
		/// <returns>A bit mask indicating which fields should be returned in any item results.</returns>
		public int GetResultFilters()
		{
			if (m_server == null) throw new NotConnectedException();

			// update local cache.
			m_filters = ((IServer)m_server).GetResultFilters();

			// return filters.
			return m_filters;
		}
		
		/// <summary>
		/// Sets the filters applied by the server to any item results returned to the client.
		/// </summary>
		/// <param name="filters">A bit mask indicating which fields should be returned in any item results.</param>
		public void SetResultFilters(int filters)
		{ 
			if (m_server == null) throw new NotConnectedException();

			// set filters on server.
			((IServer)m_server).SetResultFilters(filters); 

			// cache updated filters.
			m_filters = filters;
		}
		
		//======================================================================
		// GetStatus

		/// <summary>
		/// Returns the current server status.
		/// </summary>
		/// <returns>The current server status.</returns>
		public ServerStatus GetStatus()
		{ 
			if (m_server == null) throw new NotConnectedException();

			ServerStatus status = ((IServer)m_server).GetStatus(); 

			if (status.StatusInfo == null)
			{
				status.StatusInfo = GetString("serverState." + status.ServerState.ToString());
			}

			return status;
		}

		//======================================================================
		// Read

		/// <summary>
		/// Reads the current values for a set of items. 
		/// </summary>
		/// <param name="items">The set of items to read.</param>
		/// <returns>The results of the read operation for each item.</returns>
		public ItemValueResult[] Read(Item[] items)
		{ 
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).Read(items); 
		}

		//======================================================================
		// Write

		/// <summary>
		/// Writes the value, quality and timestamp for a set of items.
		/// </summary>
		/// <param name="items">The set of item values to write.</param>
		/// <returns>The results of the write operation for each item.</returns>
		public IdentifiedResult[] Write(ItemValue[] items) 
		{ 
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).Write(items);
		}

		//======================================================================
		// CreateSubscription

		/// <summary>
		/// Creates a new subscription.
		/// </summary>
		/// <param name="state">The initial state of the subscription.</param>
		/// <returns>The new subscription object.</returns>
		public virtual ISubscription CreateSubscription(SubscriptionState state) 
		{ 
			if (state == null)    throw new ArgumentNullException("state");
			if (m_server == null) throw new NotConnectedException();
			
			// create subscription on server.
			ISubscription subscription = ((IServer)m_server).CreateSubscription(state); 
			
			// set filters.
			subscription.SetResultFilters(m_filters);

			// append new subscription to existing list.
			SubscriptionCollection subscriptions = new SubscriptionCollection();
			
			if (m_subscriptions != null) 
			{
				foreach (Subscription value in m_subscriptions)
				{
					subscriptions.Add(value);
				}
			}

			subscriptions.Add(CreateSubscription(subscription));

			// save new subscription list.
			m_subscriptions = subscriptions;

			// return new subscription.
			return m_subscriptions[m_subscriptions.Count-1];
		}
		
		/// <summary>
		/// Creates a new instance of the appropriate subcription object.
		/// </summary>
		/// <param name="subscription">The remote subscription object.</param>
		protected virtual Subscription CreateSubscription(ISubscription subscription) 
		{
			return new Subscription(this, subscription);
		}

		//======================================================================
		// CancelSubscription

		/// <summary>
		/// Cancels a subscription and releases all resources allocated for it.
		/// </summary>
		/// <param name="subscription">The subscription to cancel.</param>
		public virtual void CancelSubscription(ISubscription subscription) 
		{ 
			if (subscription == null) throw new ArgumentNullException("subscription");
			if (m_server == null)     throw new NotConnectedException();
			
			// validate argument.
			if (!typeof(Opc.Da.Subscription).IsInstanceOfType(subscription))
			{
				throw new ArgumentException("Incorrect object type.", "subscription");	
			}

			if (!this.Equals(((Opc.Da.Subscription)subscription).Server))
			{
				throw new ArgumentException("Unknown subscription.", "subscription");	
			}

			// search for subscription in list of subscriptions.
			SubscriptionCollection subscriptions = new SubscriptionCollection();
			
			foreach (Subscription current in m_subscriptions) 
			{
				if (!subscription.Equals(current))
				{
					subscriptions.Add(current);
					continue;
				}
			}

			// check if subscription was not found.
			if (subscriptions.Count == m_subscriptions.Count)
			{
				throw new ArgumentException("Subscription not found.", "subscription");
			}

			// remove subscription from list of subscriptions.
			m_subscriptions = subscriptions;

			// cancel subscription on server.
			((IServer)m_server).CancelSubscription(((Subscription)subscription).m_subscription);
		}

		//======================================================================
		// Browse

		/// <summary>
		/// Fetches the children of a branch that meet the filter criteria.
		/// </summary>
		/// <param name="itemID">The identifier of branch which is the target of the search.</param>
		/// <param name="filters">The filters to use to limit the set of child elements returned.</param>
		/// <param name="position">An object used to continue a browse that could not be completed.</param>
		/// <returns>The set of elements found.</returns>
		public BrowseElement[] Browse(
			ItemIdentifier     itemID,
			BrowseFilters      filters, 
			out BrowsePosition position)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).Browse(itemID, filters, out position); 
		}

		//======================================================================
		// BrowseNext

		/// <summary>
		/// Continues a browse operation with previously specified search criteria.
		/// </summary>
		/// <param name="position">An object containing the browse operation state information.</param>
		/// <returns>The set of elements found.</returns>
		public BrowseElement[] BrowseNext(ref BrowsePosition position)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).BrowseNext(ref position);
		}

		//======================================================================
		// GetProperties

		/// <summary>
		/// Returns the item properties for a set of items.
		/// </summary>
		/// <param name="itemIDs">A list of item identifiers.</param>
		/// <param name="propertyIDs">A list of properties to fetch for each item.</param>
		/// <param name="returnValues">Whether the property values should be returned with the properties.</param>
		/// <returns>A list of properties for each item.</returns>
		public ItemPropertyCollection[] GetProperties(
			ItemIdentifier[] itemIDs,
			PropertyID[]     propertyIDs,
			bool             returnValues)
		{			
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).GetProperties(itemIDs, propertyIDs, returnValues);
		}

		//======================================================================
		// Private Members
		
		/// <summary>
		/// A list of subscriptions for the server.
		/// </summary>
		private SubscriptionCollection m_subscriptions = new SubscriptionCollection();

		/// <summary>
		/// The local copy of the result filters.
		/// </summary>
		int m_filters = (int)ResultFilter.Minimal;
		
		//======================================================================
		// Private Methods

		/// <summary>
		/// Establishes a subscription based on the template provided.
		/// </summary>
		private Subscription EstablishSubscription(Subscription template)
		{
			// create subscription.
			Subscription subscription = new Subscription(this, ((IServer)m_server).CreateSubscription(template.State));
			
			// set filters.
			subscription.SetResultFilters(template.Filters);

			// add items.
			try
			{
				subscription.AddItems(template.Items);
			}
			catch
			{
				subscription.Dispose();
				subscription = null;
			}
			
			// return new subscription.
			return subscription;
		}
	}

	/// <summary>
	/// A collection of subscriptions.
	/// </summary>
	[Serializable]
	public class SubscriptionCollection : ICollection, ICloneable, IList
	{
		/// <summary>
		///  Gets the item at the specified index.
		/// </summary>
		public Subscription this[int index]
		{
			get { return (Subscription)m_subscriptions[index];  }
			set { m_subscriptions[index] = value; }
		}	

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		public SubscriptionCollection() {}

		/// <summary>
		/// Initializes object with the specified SubscriptionCollection object.
		/// </summary>
		public SubscriptionCollection(SubscriptionCollection subscriptions)
		{
			if (subscriptions != null)
			{
				foreach (Subscription subscription in subscriptions)
				{
					Add(subscription);
				}
			}
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone()
		{
			SubscriptionCollection clone = (SubscriptionCollection)MemberwiseClone();

			clone.m_subscriptions = new ArrayList();

			foreach (Subscription subscription in m_subscriptions)
			{
				clone.m_subscriptions.Add(subscription.Clone());
			}

			return clone;
		}
		#endregion
		
		#region ICollection Members
		/// <summary>
		/// Indicates whether access to the ICollection is synchronized (thread-safe).
		/// </summary>
		public bool IsSynchronized
		{
			get	{ return false; }
		}

		/// <summary>
		/// Gets the number of objects in the collection.
		/// </summary>
		public int Count
		{
			get { return (m_subscriptions != null)?m_subscriptions.Count:0; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_subscriptions != null)
			{
				m_subscriptions.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Subscription[] array, int index)
		{
			CopyTo((Array)array, index);
		}

		/// <summary>
		/// Indicates whether access to the ICollection is synchronized (thread-safe).
		/// </summary>
		public object SyncRoot
		{
			get	{ return this; }
		}
		#endregion

		#region IEnumerable Members
		/// <summary>
		/// Returns an enumerator that can iterate through a collection.
		/// </summary>
		/// <returns>An IEnumerator that can be used to iterate through the collection.</returns>
		public IEnumerator GetEnumerator()
		{
			return m_subscriptions.GetEnumerator();
		}
		#endregion

		#region IList Members
		/// <summary>
		/// Gets a value indicating whether the IList is read-only.
		/// </summary>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// Gets or sets the element at the specified index.
		/// </summary>
		object System.Collections.IList.this[int index]
		{
			get	{ return m_subscriptions[index];  }
			
			set	
			{ 
				if (!typeof(Subscription).IsInstanceOfType(value))
				{
					throw new ArgumentException("May only add Subscription objects into the collection.");
				}
				
				m_subscriptions[index] = value; 
			}
		}

		/// <summary>
		/// Removes the IList subscription at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the subscription to remove.</param>
		public void RemoveAt(int index)
		{
			m_subscriptions.RemoveAt(index);
		}

		/// <summary>
		/// Inserts an subscription to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, object value)
		{
			if (!typeof(Subscription).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add Subscription objects into the collection.");
			}

			m_subscriptions.Insert(index, value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(object value)
		{
			m_subscriptions.Remove(value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(object value)
		{
			return m_subscriptions.Contains(value);
		}

		/// <summary>
		/// Removes all subscriptions from the IList.
		/// </summary>
		public void Clear()
		{
			m_subscriptions.Clear();
		}

		/// <summary>
		/// Determines the index of a specific subscription in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(object value)
		{
			return m_subscriptions.IndexOf(value);
		}

		/// <summary>
		/// Adds an subscription to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(object value)
		{
			if (!typeof(Subscription).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add Subscription objects into the collection.");
			}

			return m_subscriptions.Add(value);
		}

		/// <summary>
		/// Indicates whether the IList has a fixed size.
		/// </summary>
		public bool IsFixedSize
		{
			get	{ return false; }
		}

		/// <summary>
		/// Inserts an subscription to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, Subscription value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(Subscription value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(Subscription value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific subscription in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(Subscription value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an subscription to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(Subscription value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Members
		private ArrayList m_subscriptions = new ArrayList();
		#endregion
	}

	//=============================================================================
	// Asynchronous Delegates

	/// <summary>
	/// The asynchronous delegate for GetResultFilters.
	/// </summary>
	public delegate int GetResultFiltersAsyncDelegate();

	/// <summary>
	/// The asynchronous delegate for SetResultFilters.
	/// </summary>
	public delegate void SetResultFiltersAsyncDelegate(int filters);

	/// <summary>
	/// The asynchronous delegate for GetStatus.
	/// </summary>
	public delegate ServerStatus GetStatusAsyncDelegate();

	/// <summary>
	/// The asynchronous delegate for Read.
	/// </summary>
	public delegate ItemValueResult[] ReadAsyncDelegate(Item[] items);

	/// <summary>
	/// The asynchronous delegate for Write.
	/// </summary>
	public delegate IdentifiedResult[] WriteAsyncDelegate(ItemValue[] items);

	/// <summary>
	/// The asynchronous delegate for CreateSubscription.
	/// </summary>
	public delegate ISubscription CreateSubscriptionAsyncDelegate(SubscriptionState state);
		
	/// <summary>
	/// The asynchronous delegate for CancelSubscription.
	/// </summary>
	public delegate void CancelSubscriptionAsyncDelegate(ISubscription subscription);
		
	/// <summary>
	/// The asynchronous delegate for Browse.
	/// </summary>
	public delegate BrowseElement[] BrowseAsyncDelegate(ItemIdentifier itemID, BrowseFilters filters, out BrowsePosition position);

	/// <summary>
	/// The asynchronous delegate for BrowseNext.
	/// </summary>
	public delegate BrowseElement[] BrowseNextAsyncDelegate(ref BrowsePosition position);

	/// <summary>
	/// The asynchronous delegate for GetProperties.
	/// </summary>
	public delegate ItemPropertyCollection[] GetPropertiesAsyncDelegate(ItemIdentifier[] itemIDs, PropertyID[] propertyIDs, string itemPath, bool returnValues);
}
