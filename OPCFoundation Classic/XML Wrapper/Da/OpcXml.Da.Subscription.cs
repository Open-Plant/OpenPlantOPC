//============================================================================
// TITLE: ISubscription.cs
//
// CONTENTS:
// 
// The interface for a subscription for a set of items on an OPC server.
//
// (c) Copyright 2003 The OPC Foundation
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
using System.Globalization;
using System.Collections;
using System.Threading;
using Opc;
using Opc.Da;

namespace OpcXml.Da
{

	/// <summary>
	/// A subscription for a set of items on a single XML-DA server.
	/// </summary>
	public class Subscription : ISubscription
	{				
		//======================================================================
		// Constructor

		/// <summary>
		/// Initializes a new instance of a subscription.
		/// </summary>
		internal Subscription(
			Server              server, 
			OpcXml.Da10.Service proxy, 
			SubscriptionState   state,
			int                 filters)
		{
			if (server == null) throw new ArgumentNullException("server");
			if (proxy == null)  throw new ArgumentNullException("proxy");

			m_server  = server;
			m_proxy   = proxy;
			m_filters = filters;
			m_state   = (state != null)?(SubscriptionState)state.Clone():new SubscriptionState();

			if (m_state.Name == null || m_state.Name == "")
			{
				lock (typeof(Subscription))
				{
					m_state.Name = String.Format("Subscription{0,3:000}", ++m_counter);
				}
			}
		}

		//======================================================================
		// IDisposable

		/// <summary>
		/// Releases any unmanaged resources used by the subscription.
		/// </summary>
		public void Dispose() 
		{
			if (m_proxy != null)
			{
				// stop the timer - if running.
				if (m_pollTimer != null)
				{
					m_pollTimer.Dispose();
					m_pollTimer = null;
				}

				// cancel the subscription.
				Unsubscribe();

				// release the server.
				m_proxy = null;
			}
		}

		//======================================================================
		// Public Properties

		/// <summary>
		/// The server that the subscription is attached to.
		/// </summary>
		public Opc.Da.IServer Server {get{lock (this){ return m_server; }}}

		//======================================================================
		// ISubscription

		/// <summary>
		/// An event to receive data change updates.
		/// </summary>
		public event DataChangedEventHandler DataChanged
		{
			add    {lock (this){ m_callback += value; }}
			remove {lock (this){ m_callback -= value; }}
		}

		//======================================================================
		// Result Filters

		/// <summary>
		/// Returns the filters applied by the server to any item results returned to the client.
		/// </summary>
		/// <returns>A bit mask indicating which fields should be returned in any item results.</returns>
		public int GetResultFilters()
		{
			lock (this) { return m_filters; }
		}
		
		/// <summary>
		/// Sets the filters applied by the server to any item results returned to the client.
		/// </summary>
		/// <param name="filters">A bit mask indicating which fields should be returned in any item results.</param>
		public void SetResultFilters(int filters)
		{ 
			lock (this) { m_filters = filters; }
		}
		
		//======================================================================
		// State Management

		/// <summary>
		/// Returns the current state of the subscription.
		/// </summary>
		/// <returns>The current state of the subscription.</returns>
		public Opc.Da.SubscriptionState GetState() 
		{
			lock (this)
			{ 
				return (Opc.Da.SubscriptionState)m_state.Clone(); 
			}
		}

		/// <summary>
		/// Changes the state of a subscription.
		/// </summary>
		/// <param name="masks">A bit mask that indicates which elements of the subscription state are changing.</param>
		/// <param name="state">The new subscription state.</param>
		/// <returns>The actual subscption state after applying the changes.</returns>
		public SubscriptionState ModifyState(int masks, SubscriptionState state)
		{
			if (state == null) throw new ArgumentNullException("state");

			lock (this)
			{
				// save copy of current state.
				SubscriptionState modifiedState = (SubscriptionState)m_state.Clone();

				// update subscription defaults.
				if ((masks & (int)StateMask.Name) != 0)         { modifiedState.Name         = state.Name;         } 
				if ((masks & (int)StateMask.ClientHandle) != 0) { modifiedState.ClientHandle = state.ClientHandle; }
				if ((masks & (int)StateMask.Active) != 0)       { modifiedState.Active       = state.Active;       }
				if ((masks & (int)StateMask.UpdateRate) != 0)   { modifiedState.UpdateRate   = state.UpdateRate;   }
				if ((masks & (int)StateMask.Locale) != 0)       { modifiedState.Locale       = state.Locale;       }
				if ((masks & (int)StateMask.KeepAlive) != 0) 	{ modifiedState.KeepAlive    = state.KeepAlive;    }
				if ((masks & (int)StateMask.Deadband) != 0)  	{ modifiedState.Deadband     = state.Deadband;     }

				bool resubscribe = false;

				// check for changes that require new subscription.
				if (modifiedState.Active     != m_state.Active     ||
					modifiedState.UpdateRate != m_state.UpdateRate ||
					modifiedState.KeepAlive  != m_state.KeepAlive  ||
					modifiedState.Deadband   != m_state.Deadband)
				{
					resubscribe = true;
				}
				
				// save new state.
				m_state = modifiedState;

				if (resubscribe)
				{
					// cancel any existing subscription.
					Unsubscribe();

					// create a new subscription if active.
					if (m_state.Active) 
					{
						OnDataChange(Subscribe(m_items));
					}
				}

				// return new state.
				return GetState();
			}
		}
		
		//======================================================================
		// Item Management

		/// <summary>
		/// Adds items to the subscription.
		/// </summary>
		/// <param name="items">The set of items to add to the subscription.</param>
		/// <returns>The results of the add item operation for each item.</returns>
		public ItemResult[] AddItems(Item[] items)
		{
			if (items == null) throw new ArgumentNullException("items");

			// check if nothing to do.
			if (items.Length == 0)
			{
				return new ItemResult[0];
			}

			lock (this)
			{
				if (m_proxy == null) throw new NotConnectedException();

				// create complete item list.
				ItemList itemList = new ItemList();
				if (m_items != null) itemList.AddRange(m_items);

				// add new items.
				for (int ii = 0; ii < items.Length; ii++)
				{	
					Item clone = (Item)items[ii].Clone();

					// generate a unique item handle.
					clone.ServerHandle = Guid.NewGuid().ToString();

					// items must be active when first added.
					clone.Active = true;
					clone.ActiveSpecified = true;

					itemList.Add(clone);
				}

				// save the index of the first new item.
				int start = (m_items != null)?m_items.Count:0;

				// (re)establishes a subscription.
				ItemValueResultList resultList = Subscribe(itemList);

				if (resultList == null || resultList.Count != itemList.Count)
				{
					throw new InvalidResponseException();
				}

				// clear existing item list.
				m_items.Clear();

				// process results.
				ItemResult[] results = new ItemResult[items.Length];

				for (int ii = 0; ii < resultList.Count; ii++)
				{
					// the current item value result object.
					SubscribeItemValueResult result = (SubscribeItemValueResult)resultList[ii];

					// the matching item object.
					Item item = itemList[ii];

					// save revised sampling rates.
					item.SamplingRate          = result.SamplingRate;
					item.SamplingRateSpecified = result.SamplingRateSpecified;

					// create item result object for new items.
					if (ii >= start)
					{
						results[ii-start]                = new ItemResult(item);
						results[ii-start].ResultID       = result.ResultID;
						results[ii-start].DiagnosticInfo = result.DiagnosticInfo;
					}

					// existing items stay in the list; only add new items if successful. 
					if (ii < start || result.ResultID.Succeeded())
					{
						m_items.Add(item);
					}
				}
			
				// cancel subscription if subscription is not active.
				if (!m_state.Active)
				{
					Unsubscribe();
				}
					
				// send data change notifications if subscription is active.
				else
				{
					OnDataChange(resultList);
				}
				
				// return the results for the new items.
				return results;
			}
		}

		/// <summary>
		/// Modifies the state of items in the subscription
		/// </summary>
		/// <param name="masks">Specifies which item state parameters are being modified.</param>
		/// <param name="items">The new state for each item.</param>
		/// <returns>The results of the modify item operation for each item.</returns>
		public ItemResult[] ModifyItems(int masks, Item[] items)
		{
			if (items == null) throw new ArgumentNullException("items");

			// check if nothing to do.
			if (items.Length == 0)
			{
				return new ItemResult[0];
			}

			lock (this)
			{
				if (m_proxy == null) throw new NotConnectedException();

				// build the modified list of items.
				ArrayList itemList = new ArrayList(items.Length);

				foreach (Item item in m_items)
				{
					// search the list of masks for a matching server handle.
					bool found = false;

					foreach (Item changes in items)
					{
						// apply the updates to an item copy.
						if (item.ServerHandle.Equals(changes.ServerHandle))
						{
							Item clone = (Item)item.Clone();

							// change requested data type.
							if ((masks & (int)StateMask.ReqType) != 0)        
							{
								clone.ReqType = changes.ReqType;
							}

							// deactivating items not supported at ths time.
							if ((masks & (int)StateMask.Active) != 0)     
							{
								clone.Active          = true;
								clone.ActiveSpecified = true;
							}

							// change deadband.
							if ((masks & (int)StateMask.Deadband) != 0)
							{
								clone.Deadband          = changes.Deadband;
								clone.DeadbandSpecified = changes.DeadbandSpecified;
							}

							// change sampling rate
							if ((masks & (int)StateMask.SamplingRate) != 0)
							{
								clone.SamplingRate          = changes.SamplingRate;
								clone.SamplingRateSpecified = changes.SamplingRateSpecified;
							}

							// change buffering.
							if ((masks & (int)StateMask.SamplingRate) != 0)
							{
								clone.EnableBuffering          = changes.EnableBuffering;
								clone.EnableBufferingSpecified = changes.EnableBufferingSpecified;
							}								

							itemList.Add(clone);
							found = true;
							break;
						}
					}

					// original to the item list.
					if (!found) itemList.Add(item); 
				}

				// create a new subscription.
				ItemValueResultList resultList = Subscribe(itemList);

				// construct result for each mask provided.
				ItemResult[] results = new ItemResult[items.Length];

				for (int ii = 0; ii < resultList.Count; ii++)
				{
					// the current item value result object.
					SubscribeItemValueResult result = (SubscribeItemValueResult)resultList[ii];

					// the matching item object.
					Item item = (Item)itemList[ii];

					// save revised sampling rates.
					item.SamplingRate          = result.SamplingRate;
					item.SamplingRateSpecified = result.SamplingRateSpecified;

					// search list of masks for a matching server handle.
					for (int jj = 0; jj < items.Length; jj++)
					{
						if (result.ServerHandle.Equals(items[jj].ServerHandle))
						{
							results[jj]                = new ItemResult(item);
							results[jj].ResultID       = result.ResultID;
							results[jj].DiagnosticInfo = result.DiagnosticInfo;
							break;
						}
					}
				}

				// look for masks without a result - must have an invalid or duplicate server handles.
				for (int ii = 0; ii < results.Length; ii++)
				{
					if (results[ii] == null)
					{
						results[ii]                = new ItemResult(items[ii]);
						results[ii].ResultID       = ResultID.Da.E_INVALIDHANDLE;
						results[ii].DiagnosticInfo = null;
					}
				}

				// save modified list of items.
				m_items = itemList;

				// send data change notifications.
				OnDataChange(resultList);

				// return results.
				return results;
			}
		}

		/// <summary>
		/// Removes items from the subscription.
		/// </summary>
		/// <param name="items">The identifiers (i.e. server handles) for the items being removed.</param>
		/// <returns>The results of the remove item operation for each item.</returns>
		public IdentifiedResult[] RemoveItems(ItemIdentifier[] items)
		{
			if (items == null) throw new ArgumentNullException("items");

			// check if nothing to do.
			if (items.Length == 0)
			{
				return new IdentifiedResult[0];
			}

			lock (this)
			{
				if (m_proxy == null) throw new NotConnectedException();

				// initialize results list.
				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				// build the remaining list of items.
				ArrayList itemList = new ArrayList(items.Length);

				foreach (Item item in m_items)
				{
					// search the list of items to remove for a matching server handle.
					bool found = false;

					for (int ii = 0; ii < items.Length; ii++)
					{
						if (item.ServerHandle.Equals(items[ii].ServerHandle))
						{
							results[ii] = new IdentifiedResult(items[ii]);
							found = true;
							break;
						}
					}

					// add copy to the item list.
					if (!found) itemList.Add(item);
				}

				// create a new subscription.
				ItemValueResultList resultList = Subscribe(itemList);

				// update remaining items.
				for (int ii = 0; ii < resultList.Count; ii++)
				{
					// the current item value result object.
					SubscribeItemValueResult result = (SubscribeItemValueResult)resultList[ii];

					// the matching item object.
					Item item = (Item)itemList[ii];

					// save revised sampling rates.
					item.SamplingRate          = result.SamplingRate;
					item.SamplingRateSpecified = result.SamplingRateSpecified;
				}

				// look for uninitialized results - must have an invalid or duplicate server handles.
				for (int ii = 0; ii < results.Length; ii++)
				{
					if (results[ii] == null)
					{
						results[ii]                = new IdentifiedResult(items[ii]);
						results[ii].ResultID       = ResultID.Da.E_INVALIDHANDLE;
						results[ii].DiagnosticInfo = null;
					}
				}

				// save modified list of items.
				m_items = itemList;

				// send data change notifications.
				OnDataChange(resultList);

				// return results.
				return results;
			}		
		}

		//======================================================================
		// Synchronous I/O
		
		/// <summary>
		/// Reads the values for a set of items in the subscription.
		/// </summary>
		/// <param name="items">The identifiers (i.e. server handles) for the items being read.</param>
		/// <returns>The value for each of items.</returns>
		public Opc.Da.ItemValueResult[] Read(Item[] items)
		{
			if (items == null)   throw new ArgumentNullException("items");	

			if (items.Length == 0)
			{
				return new Opc.Da.ItemValueResult[0];
			}

			lock (this)
			{
				if (m_proxy == null) throw new NotConnectedException();

				ItemList list = new ItemList();
				list.AddRange(items);

				OpcXml.Da10.RequestOptions      options     = OpcXml.Da10.Request.GetRequestOptions(m_state.Locale, m_filters);
				OpcXml.Da10.ReadRequestItemList requestList = OpcXml.Da10.Request.GetItemList(list);
				OpcXml.Da10.ReplyItemList       replyList   = null;
				OpcXml.Da10.OPCError[]          errors      = null;
				
				OpcXml.Da10.ReplyBase reply = m_proxy.Read(
					options,
					requestList,
					out replyList,
					out errors);

				m_server.CacheResponse(m_state.Locale, reply, errors);

				ItemValueResultList valueList = OpcXml.Da10.Request.GetResultList(replyList);
				
				if (valueList == null)
				{
					throw new InvalidResponseException();
				}

				return (ItemValueResult[])valueList.ToArray(typeof(ItemValueResult));
			}
		}

		/// <summary>
		/// Writes the value, quality and timestamp for a set of items in the subscription.
		/// </summary>
		/// <param name="items">The item values to write.</param>
		/// <returns>The results of the write operation for each item.</returns>
		public IdentifiedResult[] Write(ItemValue[] items)
		{
			if (items == null) throw new ArgumentNullException("items");	

			if (items.Length == 0)
			{
				return new Opc.IdentifiedResult[0];
			}

			lock (this)
			{
				if (m_proxy == null) throw new NotConnectedException();

				ItemValueList list = new ItemValueList();
				list.AddRange(items);

				OpcXml.Da10.RequestOptions       options     = OpcXml.Da10.Request.GetRequestOptions(m_state.Locale, m_filters);
				OpcXml.Da10.WriteRequestItemList requestList = OpcXml.Da10.Request.GetItemValueList(list);
				OpcXml.Da10.ReplyItemList        replyList   = null;
				OpcXml.Da10.OPCError[]           errors      = null;
				
				OpcXml.Da10.ReplyBase reply = m_proxy.Write(
					options,
					requestList,
					false,
					out replyList,
					out errors);

				m_server.CacheResponse(m_state.Locale, reply, errors);

				ItemValueResultList valueList = OpcXml.Da10.Request.GetResultList(replyList);

				if (valueList == null)
				{
					throw new InvalidResponseException();
				}

				IdentifiedResult[] results = new IdentifiedResult[valueList.Count];

				for (int ii = 0; ii < valueList.Count; ii++)
				{
					ItemValueResult valueResult = valueList[ii];

					results[ii]                 = new IdentifiedResult(valueResult);
					results[ii].ResultID        = valueResult.ResultID;
					results[ii].DiagnosticInfo  = valueResult.DiagnosticInfo;
				}

				return results;
			}
		}

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
			throw new NotImplementedException("BeginRead");
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
			throw new NotImplementedException("BeginRead");
		}

		/// <summary>
		/// Cancels an asynchronous read or write operation.
		/// </summary>
		/// <param name="request">The object returned from the BeginRead or BeginWrite request.</param>
		/// <param name="callback">The function to invoke when the cancel completes.</param>
		public void Cancel(IRequest request, CancelCompleteEventHandler callback) 
		{ 
			throw new NotImplementedException("Cancel");
		}

		/// <summary>
		/// Causes the server to send a data changed notification for all active items. 
		/// </summary>
		public void Refresh()
		{
			IRequest request = null;
			Refresh(null, out request);
		}

		/// <summary>
		/// Causes the server to send a data changed notification for all active items. 
		/// </summary>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public virtual void Refresh(
			object       requestHandle,
			out IRequest request)
		{
			lock (this)
			{
				request = null;

				// do nothing is subscription is no longer valid.
				if (m_state.ServerHandle == null) return;	

				// begin the polled refresh.
				OpcXml.Da10.RequestOptions options = OpcXml.Da10.Request.GetRequestOptions(m_state.Locale, m_filters);

				// send a polled refresh that requests all items.
				m_proxy.BeginSubscriptionPolledRefresh(
					options,
					new string [] { (string)m_state.ServerHandle },
					m_server.ServerTime,
					true,
					0,
					true,
					new AsyncCallback(OnPollCompleted),
					new string[] { (string)m_state.ServerHandle, options.LocaleID, null } );
			}
		}

		/// <summary>
		/// Enables or disables data change notifications from the server.
		/// </summary>
		/// <param name="enabled">Whether data change notifications are enabled.</param> 
		public void SetEnabled(bool enabled)
		{
			lock (this)
			{
				m_enabled = enabled;
			}
		}

		/// <summary>
		/// Checks whether data change notifications from the server are enabled.
		/// </summary>
		/// <returns>Whether data change notifications are enabled.</returns>
		public bool GetEnabled()
		{
			lock (this)
			{
				return m_enabled;
			}
		}

		//======================================================================
		// Private Members

		/// <summary>
		/// The maximum time between polled refreshes.
		/// </summary>
		private int m_pingRate = 0;

		/// <summary>
		/// Whether the subscription is currently enabled.
		/// </summary>
		private bool m_enabled = true;

		/// <summary>
		/// An ordered list of all items which are part of the subscription.
		/// </summary>
		private ArrayList m_items = new ArrayList();

		/// <summary>
		/// A timer used to schedule polled refreshes.
		/// </summary>
		private Timer m_pollTimer = null;

		/// <summary>
		/// A counter used to create unique subscription names.
		/// </summary>
		private static int m_counter = 0;

		/// <summary>
		/// The containing server object.
		/// </summary>
		private Server m_server = null;

		/// <summary>
		/// The autogenerated proxy object for the XML-DA 1.0 web service.
		/// </summary>
		private OpcXml.Da10.Service m_proxy = null;
		
		/// <summary>
		/// The event raised when data change events occur.
		/// </summary>
		private event DataChangedEventHandler m_callback = null;

		/// <summary>
		/// The current subscription result filters options.
		/// </summary>
		private int m_filters = (int)ResultFilter.Minimal;

		/// <summary>
		/// The current subscription state.
		/// </summary>
		private SubscriptionState m_state = null;

		//======================================================================
		// Private Methods

		/// <summary>
		/// Sends data change notifications for all active items.
		/// </summary>
		private void OnDataChange(ItemValueResultList items)
		{	
			lock (this)
			{
				if (m_callback != null)
				{
					m_callback(m_state.ClientHandle, null, (ItemValueResult[])items.ToArray(typeof(ItemValueResult)));
				}
			}
		}

		/// <summary>
		/// Establishes a subscription for the current set of items.
		/// </summary>
		private ItemValueResultList Subscribe(ArrayList items)
		{
			lock (this)
			{
				// cancel any current subscription.
				Unsubscribe();
            
				// check if there is nothing to do.
				if (items == null || items.Count == 0) 
				{ 
					return new ItemValueResultList();
				}

				// create a single item list and use the subscription state to set list level parameters.
				ItemList itemList = new ItemList();

				itemList.ClientHandle             = Guid.NewGuid().ToString();
				itemList.ItemPath                 = null;
				itemList.ReqType                  = null;
				itemList.Deadband                 = m_state.Deadband;
				itemList.DeadbandSpecified        = true;
				itemList.SamplingRate             = m_state.UpdateRate;
				itemList.SamplingRateSpecified    = true;
				itemList.EnableBuffering          = false;
				itemList.EnableBufferingSpecified = false;

				// set the ping rate based on the keep alive (if specified) or five times update rate. 
				m_pingRate = (m_state.KeepAlive != 0)?m_state.KeepAlive:m_state.UpdateRate*5;

				// stop any existing timer.
				if (m_pollTimer != null)
				{
					m_pollTimer.Dispose();
					m_pollTimer = null;
				}
			
				// create copies of each and replace the client handle with the server handle.
				foreach (Item item in items) 
				{
					Item clone = (Item)item.Clone();
					clone.ClientHandle = clone.ServerHandle;
					itemList.Add(clone);
				}
			
				string subscription = null;

				// establish the subscription on the server.		
				OpcXml.Da10.RequestOptions         options   = OpcXml.Da10.Request.GetRequestOptions(m_state.Locale, m_filters);
				OpcXml.Da10.SubscribeReplyItemList replyList = null;
				OpcXml.Da10.OPCError[]             errors    = null;     

				OpcXml.Da10.ReplyBase reply = m_proxy.Subscribe(
					options, 
					OpcXml.Da10.Request.GetSubscribeList(itemList),
					true,
					m_pingRate*2,
					out replyList,
					out errors,
					out subscription);

				// cache results with the server object.
				m_server.CacheResponse(m_state.Locale, reply, errors);

				// save subscription handle.
				m_state.ServerHandle = subscription;

				// check for valid response.
				if (replyList == null)
				{
					throw new InvalidResponseException();
				}

				// save the revised update rate.
				if (replyList.RevisedSamplingRateSpecified)
				{
					m_state.UpdateRate = (int)replyList.RevisedSamplingRate;
				}

				// update items.
				ItemValueResultList resultList = OpcXml.Da10.Request.GetSubscribeResultList(replyList);

				for (int ii = 0; ii < itemList.Count; ii++)
				{
					SubscribeItemValueResult resultItem = (SubscribeItemValueResult)resultList[ii];

					// restore the client/server handles in the result.
					resultItem.ServerHandle = resultItem.ClientHandle;
					resultItem.ClientHandle = ((Item)items[ii]).ClientHandle;

					// check if the requested sampling rate was accepted.
					if (!resultItem.SamplingRateSpecified)
					{
						resultItem.SamplingRate          = itemList[ii].SamplingRate;
						resultItem.SamplingRateSpecified = itemList[ii].SamplingRateSpecified;
					}
				}

				// schedule polling.
				SchedulePoll();

				// return result list.
				return resultList;
			}
		}

		/// <summary>
		/// Closes the current subscription with the server.
		/// </summary>
		private void Unsubscribe()
		{
			if (m_state.ServerHandle != null)
			{
				string requestHandle = null;

				try	  { m_proxy.SubscriptionCancel(m_state.ServerHandle.ToString(), ref requestHandle); }
				catch {}

				m_state.ServerHandle = null;
			}
		}

		/// <summary>
		/// Called when a poll completes.
		/// </summary>
		private void  OnPollCompleted(IAsyncResult handle)
		{
			// restore async state parameters.
			string[] args = (string[])handle.AsyncState;

			try
			{
				lock (this)
				{
					// check if object has been disposed.
					if (m_proxy == null)
					{
						return;
					}

					// fetch the poll results.
					string[] invalidHandles = null;
					bool     bufferOverflow = false;

					OpcXml.Da10.SubscribePolledRefreshReplyItemList[] replyLists = null;
					OpcXml.Da10.OPCError[]                            errors     = null;
					OpcXml.Da10.ReplyBase                             reply      = null;

					reply = m_proxy.EndSubscriptionPolledRefresh(
						handle, 
						out invalidHandles, 
						out replyLists, 
						out errors, 
						out bufferOverflow);

					// cache results with the server object.
					m_server.PollCompleted((string)args[1], reply, errors);

					// check if the server handle has changed - ignore results in this case.
					if (!args[0].Equals(m_state.ServerHandle)) return;

					ItemValueResultList resultList = null;
				
					// restore subscription if it has been dropped by the server for some reason.
					if (invalidHandles != null && invalidHandles.Length > 0)
					{
						resultList = Subscribe(m_items);

						if (resultList != null && resultList.Count == m_items.Count)
						{
							// replace the client handles.
							for (int ii = 0; ii < m_items.Count; ii++)
							{
								SubscribeItemValueResult result = (SubscribeItemValueResult)resultList[ii];
							
								if (result.ResultID.Succeeded())
								{
									Item item = (Item)m_items[ii];

									// save revised sampling rate.
									item.SamplingRate          = result.SamplingRate;
									item.SamplingRateSpecified = result.SamplingRateSpecified;
								}
							}
						}
					}

					// get the list of changed items.
					else
					{
						ItemValueResultList[] resultLists = OpcXml.Da10.Request.GetSubscribeRefreshLists(replyLists);

						// validate items and replace the client handles.
						resultList = new ItemValueResultList();

						if (resultLists != null && resultLists.Length == 1)
						{
							foreach (ItemValueResult result in resultLists[0])
							{
								foreach (Item item in m_items)
								{
									if (item.ServerHandle.Equals(result.ClientHandle))
									{
										result.ServerHandle = item.ServerHandle;
										result.ClientHandle = item.ClientHandle;
										resultList.Add(result);
										break;
									}
								}
							}
						}
					}

					// send the data change notifications.
					OnDataChange(resultList);		
					
					// check if a new poll should be scheduled.
					if (args[2] != null)
					{
						SchedulePoll();
					}						
				}
			}
			catch (Exception e)
			{
				string message = e.Message;

				// schedule a new poll on exception.
				if (args[2] != null)
				{
					SchedulePoll();
				}
			}
		}

		/// <summary>
		/// Starts polling for the current subscription.
		/// </summary>
		private void SchedulePoll()
		{
			lock (this)
			{
				// use simple client side polling if not enabled.
				if (!m_enabled)
				{
					if (m_pollTimer == null)
					{
						m_pollTimer = new Timer(
							new TimerCallback(Poll), 
							null, 
							(long)m_pingRate,
							m_pingRate);
					}
				}

				// clear the timer an start server side polling.
				else
				{
					if (m_pollTimer != null)
					{
						m_pollTimer.Dispose();
						m_pollTimer = null;
					}

					Poll(null);
				}
			}
		}
		
		/// <summary>
		/// Starts polling for the current subscription.
		/// </summary>
		private void Poll(object state)
		{
			lock (this)
			{
				// do nothing is subscription is no longer valid.
				if (m_state.ServerHandle == null) return;

				TimeSpan holdTime = TimeSpan.Zero;
				TimeSpan waitTime = TimeSpan.Zero;

				// use simple client side polling if not enabled.
				if (m_enabled)
				{
					// hold time is the ping rate and the wait time is zero.
					if (m_pingRate < m_state.UpdateRate)
					{
						holdTime = new TimeSpan(m_pingRate*10000);
						waitTime = TimeSpan.Zero;
					}

						// hold time is the update rate and the wait time is ping rate minus the update rate.
					else
					{
						holdTime = new TimeSpan(m_state.UpdateRate*10000);
						waitTime = new TimeSpan((m_pingRate-m_state.UpdateRate)*10000);
					}
				}

				// begin the polled refresh.
				OpcXml.Da10.RequestOptions options = OpcXml.Da10.Request.GetRequestOptions(m_state.Locale, m_filters);

				m_proxy.BeginSubscriptionPolledRefresh(
					options,
					new string [] { (string)m_state.ServerHandle },
					m_server.ServerTime.Add(holdTime),
					true,
					(int)waitTime.TotalMilliseconds,
					false,
					new AsyncCallback(OnPollCompleted),
					new string[] { (string)m_state.ServerHandle, options.LocaleID, "" } );
			}
		}
	}
}
