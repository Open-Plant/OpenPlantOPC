//============================================================================
// TITLE: Opc.Da.Subscription.cs
//
// CONTENTS:
// 
// A class which is an in-process object used to access subscriptions on OPC Data Access servers.
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
	/// An in-process object used to access subscriptions on OPC Data Access servers.
	/// </summary>
	[Serializable]
	public class Subscription : ISubscription, IDisposable, ISerializable, ICloneable
	{	
		//======================================================================
		// Construction

		/// <summary>
		/// Initializes object with default values.
		/// </summary>
		public Subscription(Server server, ISubscription subscription)
		{
			if (server == null)       throw new ArgumentNullException("server");
			if (subscription == null) throw new ArgumentNullException("subscription");

			m_server       = server;
			m_subscription = subscription;

			GetResultFilters();
			GetState();
		}

		//======================================================================
		// IDisposable

		/// <summary>
		/// This must be called explicitly by clients to ensure the remote server is released.
		/// </summary>
		public void Dispose() 
		{
			if (m_subscription != null)
			{
				m_subscription.Dispose();

				m_server       = null;
				m_subscription = null;
				m_items        = null;
			}
		}

		//======================================================================
		// ISerializable

		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string STATE   = "State";
			internal const string FILTERS = "Filters";
			internal const string ITEMS   = "Items";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected Subscription(SerializationInfo info, StreamingContext context)
		{
			m_state   = (SubscriptionState)info.GetValue(Names.STATE, typeof(SubscriptionState));
			m_filters = (int)info.GetValue(Names.FILTERS, typeof(int));
			m_items   = (Item[])info.GetValue(Names.ITEMS, typeof(Item[]));
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(Names.STATE, m_state);
			info.AddValue(Names.FILTERS, m_filters);
			info.AddValue(Names.ITEMS, m_items);
		}

		//======================================================================
		// ICloneable

		/// <summary>
		/// Returns an unconnected copy of the subscription with the same items.
		/// </summary>
		public virtual object Clone()
		{
			// do a memberwise clone.
			Subscription clone = (Subscription)MemberwiseClone();
			
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

			// return clone.
			return clone;
		}

		//======================================================================
		// Public Properties

		/// <summary>
		/// The server that the subscription is attached to.
		/// </summary>
		public Opc.Da.Server Server {get{ return m_server; }}

		/// <summary>
		/// The name assigned to the subscription by the client.
		/// </summary>
		public string Name {get{ return m_state.Name; }}

		/// <summary>
		/// The handle assigned to the group by the client.
		/// </summary>
		public object ClientHandle {get{ return m_state.ClientHandle; }}
		
		/// <summary>
		/// The handle assigned to the subscription by the server.
		/// </summary>
		public object ServerHandle {get{ return m_state.ServerHandle; }}

		/// <summary>
		/// Whether the subscription is active.
		/// </summary>
		public bool Active {get{ return m_state.Active; }}
		
		/// <summary>
		/// Whether data callbacks are enabled.
		/// </summary>
		public bool Enabled {get{ return m_enabled; }}
		
		/// <summary>
		/// The current locale used by the subscription.
		/// </summary>
		public string Locale {get{ return m_state.Locale; }}	

		/// <summary>
		/// The current result filters applied by the subscription.
		/// </summary>
		public int Filters {get{ return m_filters; }}	

		/// <summary>
		/// Returns a copy of the current subscription state.
		/// </summary>
		public SubscriptionState State {get{ return (SubscriptionState)m_state.Clone(); }}

		/// <summary>
		/// The items belonging to the subscription.
		/// </summary>
		public Item[] Items 
		{
			get
			{
				if (m_items == null) return new Item[0];
				Item[] items = new Item[m_items.Length];
				for (int ii = 0; ii < m_items.Length; ii++) items[ii] = (Item)m_items[ii].Clone();
				return items;
			}
		}

		//======================================================================
		// ISubscription

		/// <summary>
		/// An event to receive data change updates.
		/// </summary>
		public event DataChangedEventHandler DataChanged
		{ 
			add    { m_subscription.DataChanged += value; }
			remove { m_subscription.DataChanged -= value; }
		}

		//======================================================================
		// Result Filters

		/// <summary>
		/// Gets default result filters for the server.
		/// </summary>
		public int GetResultFilters()
		{
			m_filters = m_subscription.GetResultFilters();
			return m_filters;
		}
		
		/// <summary>
		/// Sets default result filters for the server.
		/// </summary>
		public void SetResultFilters(int filters)
		{ 
			m_subscription.SetResultFilters(filters); 
			m_filters = filters;
		}
		
		//======================================================================
		// State Management

		/// <summary>
		/// Returns the current subscription state.
		/// </summary>
		public SubscriptionState GetState() 
		{ 
			m_state = m_subscription.GetState();
			return m_state; 
		}
		
		/// <summary>
		/// Updates the current subscription state.
		/// </summary>
		public SubscriptionState ModifyState(int masks, SubscriptionState state) 
		{
			m_state = m_subscription.ModifyState(masks, state); 
			return m_state;
		}
		
		//======================================================================
		// Item Management

		/// <summary>
		/// Adds items to the subscription.
		/// </summary>
		public virtual ItemResult[] AddItems(Item[] items)
		{
			if (items == null) throw new ArgumentNullException("items");

			// check if there is nothing to do.
			if (items.Length == 0)
			{
				return new ItemResult[0];
			}
			
			// add items.
			ItemResult[] results = m_subscription.AddItems(items);

			if (results == null || results.Length == 0) 
			{
				throw new InvalidResponseException();
			}

			// update locale item list.
			ArrayList itemList = new ArrayList();
			if (m_items != null) itemList.AddRange(m_items);

			for (int ii = 0; ii < results.Length; ii++)
			{
				// check for failure.
				if (results[ii].ResultID.Failed())
				{
					continue;
				}

				// create locale copy of the item.
				Item item = new Item(results[ii]);

				// item name, item path and client handle may not be returned by server.
				item.ItemName     = items[ii].ItemName;
				item.ItemPath     = items[ii].ItemPath;
				item.ClientHandle = items[ii].ClientHandle;

				itemList.Add(item);
			}
			
			// save the new item list.
			m_items = (Item[])itemList.ToArray(typeof(Item));

			// update the local state.
			GetState();
		
			// return results.
			return results;
		}

		/// <summary>
		/// Modifies items that are already part of the subscription.
		/// </summary>
		public virtual ItemResult[] ModifyItems(int masks, Item[] items)
		{
			if (items == null) throw new ArgumentNullException("items");

			// check if there is nothing to do.
			if (items.Length == 0)
			{
				return new ItemResult[0];
			}

			// modify items.
			ItemResult[] results = m_subscription.ModifyItems(masks, items);
			
			if (results == null || results.Length == 0) 
			{
				throw new InvalidResponseException();
			}

			// update local item - modify item success means all fields were updated successfully.
			for (int ii = 0; ii < results.Length; ii++)
			{
				// check for failure.
				if (results[ii].ResultID.Failed())
				{
					continue;
				}

				// search local item list.
				for (int jj = 0; jj < m_items.Length; jj++)
				{
					if (m_items[jj].ServerHandle.Equals(items[ii].ServerHandle))
					{
						// update locale copy of the item.
						Item item = new Item(results[ii]);

						// item name, item path and client handle may not be returned by server.
						item.ItemName     = m_items[jj].ItemName;
						item.ItemPath     = m_items[jj].ItemPath;
						item.ClientHandle = m_items[jj].ClientHandle;

						m_items[jj] = item;
						break;
					}
				}
			}

			// update the local state.
			GetState();
			
			// return results.
			return results;
		}

		/// <summary>
		/// Removes items from a subsription.
		/// </summary>
		public virtual IdentifiedResult[] RemoveItems(ItemIdentifier[] items)
		{
			if (items == null) throw new ArgumentNullException("items");

			// check if there is nothing to do.
			if (items.Length == 0)
			{
				return new IdentifiedResult[0];
			}

			// remove items from server.
			IdentifiedResult[] results = m_subscription.RemoveItems(items);

			if (results == null || results.Length == 0) 
			{
				throw new InvalidResponseException();
			}

			// remove items from local list if successful.
			ArrayList itemList = new ArrayList();

			foreach (Item item in m_items)
			{
				bool removed = false;

				for (int ii = 0; ii < results.Length; ii++)
				{
					if (item.ServerHandle.Equals(items[ii].ServerHandle))
					{
						removed = results[ii].ResultID.Succeeded();
						break;
					}
				}

				if (!removed) itemList.Add(item);
			}

			// update local list.
			m_items = (Item[])itemList.ToArray(typeof(Item));

			// update the local state.
			GetState();
			
			// return results.
			return results;
		}

		//======================================================================
		// Synchronous I/O

		/// <summary>
		/// Reads a set of subscription items.
		/// </summary>
		public ItemValueResult[] Read(Item[] items) { return m_subscription.Read(items); }

		/// <summary>
		/// Writes a set of subscription items.
		/// </summary>
		public IdentifiedResult[] Write(ItemValue[] items) { return m_subscription.Write(items); }

		//======================================================================
		// Asynchronous I/O

		/// <summary>
		/// Begins an asynchronous read operation for a set of items.
		/// </summary>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] Read(
			Item[]                   items,
			object                   requestHandle,
			ReadCompleteEventHandler callback,
			out IRequest             request)
		{ 
			return m_subscription.Read(items, requestHandle, callback, out request); 
		}

		/// <summary>
		/// Begins an asynchronous write operation for a set of items.
		/// </summary>
		/// <param name="items">The set of item values to write (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] Write(
			ItemValue[]               items,
			object                    requestHandle,
			WriteCompleteEventHandler callback,
			out IRequest              request)
		{ 
			return m_subscription.Write(items, requestHandle, callback, out request); 
		}
		
		/// <summary>
		/// Cancels an asynchronous request.
		/// </summary>
		public void Cancel(IRequest request, CancelCompleteEventHandler callback) 
		{ 
			m_subscription.Cancel(request, callback); 
		}

		/// <summary>
		/// Tells the server to send an data change update for all subscription items. 
		/// </summary>
		public void Refresh() { m_subscription.Refresh(); }
		
		/// <summary>
		/// Causes the server to send a data changed notification for all active items. 
		/// </summary>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public void Refresh(
			object       requestHandle,
			out IRequest request)
		{
			m_subscription.Refresh(requestHandle, out request); 
		}

		/// <summary>
		/// Sets whether data change callbacks are enabled.
		/// </summary>
		public void SetEnabled(bool enabled)
		{
			m_subscription.SetEnabled(enabled);
			m_enabled = enabled;
		}

		/// <summary>
		/// Gets whether data change callbacks are enabled.
		/// </summary>
		public bool GetEnabled()
		{
			m_enabled = m_subscription.GetEnabled();
			return m_enabled;
		}

		//======================================================================
		// Private Members

		/// <summary>
		/// The containing server object.
		/// </summary>
		internal Server m_server = null;

		/// <summary>
		/// The remote subscription object.
		/// </summary>
		internal ISubscription m_subscription = null;

		/// <summary>
		/// The local copy of the subscription state.
		/// </summary>
		private SubscriptionState m_state = new SubscriptionState();

		/// <summary>
		/// The local copy of all subscription items.
		/// </summary>
		private Item[] m_items = null;
		
		/// <summary>
		/// Whether data callbacks are enabled.
		/// </summary>
		private bool m_enabled = true;

		/// <summary>
		/// The local copy of the result filters.
		/// </summary>
		int m_filters = (int)ResultFilter.Minimal;
	}
}
