//============================================================================
// TITLE: ISubscription.cs
//
// CONTENTS:
// 
// A .NET wrapper for a COM server that implements the DA group interfaces.
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
using System.Runtime.InteropServices;
using Opc;
using Opc.Da;
using OpcRcw.Da;

namespace OpcCom.Da
{
	/// <summary>
	/// A .NET wrapper for a COM server that implements the DA group interfaces.
	/// </summary>
	public class Subscription : ISubscription
	{	
		#region Constructors
		/// <summary>
		/// Initializes a new instance of a subscription.
		/// </summary>
		internal Subscription(object group, SubscriptionState state, int filters)
		{
			if (group == null) throw new ArgumentNullException("group");
			if (state == null) throw new ArgumentNullException("state");

			m_group    = group;
			m_name     = state.Name;
			m_handle   = state.ClientHandle;
			m_filters  = filters;
			m_callback = new Callback(state.ClientHandle, m_filters, m_items);
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
                lock (this)
                {
                    if (disposing)
                    {
                        // Free other state (managed objects).
                        
                        if (m_group != null)
                        {
                            // close all connections.
                            if (m_connection != null)
                            {
                                m_connection.Dispose();
                                m_connection = null;
                            }
                        }
                    }

                    // Free your own state (unmanaged objects).
                    // Set large fields to null.

                    if (m_group != null)
                    {
                        // release group object.
                        OpcCom.Interop.ReleaseServer(m_group);
                        m_group = null;
                    }
                }

                m_disposed = true;
            }
        }

        private bool m_disposed = false;
		#endregion

		#region Private Members
		/// <summary>
		/// The COM server for the group object.
		/// </summary>
		protected object m_group = null;
		
		/// <summary>
		/// A connect point with the COM server.
		/// </summary>
		protected ConnectionPoint m_connection = null;

		/// <summary>
		/// The internal object that implements the IOPCDataCallback interface.
		/// </summary>
		private Callback m_callback = null;
		
		/// <summary>
		/// The name of the group on the server.
		/// </summary>
		protected string m_name = null;

		/// <summary>
		/// A handle assigned by the client for the subscription.
		/// </summary>
		protected object m_handle = null;

		/// <summary>
		/// The default result filters for the subscription.
		/// </summary>
		protected int m_filters = (int)ResultFilter.Minimal;

		/// <summary>
		/// A table of all item identifers which are indexed by internal handle.
		/// </summary>
		private ItemTable m_items = new ItemTable();

		/// <summary>
		/// A counter used to assign unique internal client handles.
		/// </summary>
		protected int m_counter = 0;
		#endregion

		#region Opc.Da.ISubscription Members
		/// <summary>
		/// An event to receive data change updates.
		/// </summary>
		public event DataChangedEventHandler DataChanged
		{
			add    {lock (this){ m_callback.DataChanged += value; Advise();   }}
			remove {lock (this){ m_callback.DataChanged -= value; Unadvise(); }}
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
			lock (this) 
			{ 
				m_filters = filters; 

				// update the callback object.
				m_callback.SetFilters(m_handle, m_filters);
			}
		}

		//======================================================================
		// State Management

		/// <summary>
		/// Returns the current state of the subscription.
		/// </summary>
		/// <returns>The current state of the subscription.</returns>
		public SubscriptionState GetState() 
		{
			lock (this)
			{ 
				SubscriptionState state = new SubscriptionState();

				state.ClientHandle = m_handle;

				try
				{
					string name         = null;
					int    active       = 0;
					int    updateRate   = 0;
					float  deadband     = 0;
					int    timebias     = 0;
					int    localeID     = 0;
					int    clientHandle = 0;
					int    serverHandle = 0;

					((IOPCGroupStateMgt)m_group).GetState(
						out updateRate,
						out active,
						out name,
						out timebias,
						out deadband,
						out localeID,
						out clientHandle,
						out serverHandle);

					state.Name         = name;
					state.ServerHandle = serverHandle;
					state.Active       = active != 0;
					state.UpdateRate   = updateRate;
					state.Deadband     = deadband;
					state.Locale       = OpcCom.Interop.GetLocale(localeID);

					// cache the name separately.
					m_name = state.Name;

					try
					{
						int keepAlive = 0;
						((IOPCGroupStateMgt2)m_group).GetKeepAlive(out keepAlive);
						state.KeepAlive = keepAlive;
					}
					catch
					{
						state.KeepAlive = 0;
					}
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCGroupStateMgt.GetState", e);
				}

				return state;
			}
		}

		/// <summary>
		/// Changes the state of a subscription.
		/// </summary>
		/// <param name="masks">A bit mask that indicates which elements of the subscription state are changing.</param>
		/// <param name="state">The new subscription state.</param>
		/// <returns>The actual subscption state after applying the changes.</returns>
		public Opc.Da.SubscriptionState ModifyState(int masks, Opc.Da.SubscriptionState state)
		{
			if (state == null) throw new ArgumentNullException("state");

			lock (this)
			{
				// update the group name.
				if ((masks & (int)StateMask.Name) != 0 && state.Name != m_name)
				{
					try
					{
						((IOPCGroupStateMgt)m_group).SetName(state.Name);
						m_name = state.Name;
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCGroupStateMgt.SetName", e);
					}
				}

				// update the client handle.
				if ((masks & (int)StateMask.ClientHandle) != 0)
				{
					m_handle = state.ClientHandle;

					// update the callback object.
					m_callback.SetFilters(m_handle, m_filters);
				}

				// update the group state.
				int active   = (state.Active)?1:0;
				int localeID = ((masks & (int)StateMask.Locale) != 0)?OpcCom.Interop.GetLocale(state.Locale):0;

				GCHandle hActive     = GCHandle.Alloc(active,               GCHandleType.Pinned);
				GCHandle hLocale     = GCHandle.Alloc(localeID,             GCHandleType.Pinned);
				GCHandle hUpdateRate = GCHandle.Alloc(state.UpdateRate, GCHandleType.Pinned);
				GCHandle hDeadband   = GCHandle.Alloc(state.Deadband,   GCHandleType.Pinned);
				
				int updateRate = 0;

				try
				{
					((IOPCGroupStateMgt)m_group).SetState(
						((masks & (int)StateMask.UpdateRate) != 0)?hUpdateRate.AddrOfPinnedObject():IntPtr.Zero,
						out updateRate,
						((masks & (int)StateMask.Active) != 0)?hActive.AddrOfPinnedObject():IntPtr.Zero,
						IntPtr.Zero,
						((masks & (int)StateMask.Deadband) != 0)?hDeadband.AddrOfPinnedObject():IntPtr.Zero,
						((masks & (int)StateMask.Locale) != 0)?hLocale.AddrOfPinnedObject():IntPtr.Zero,
						IntPtr.Zero);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCGroupStateMgt.SetState", e);
				}
				finally
				{
					if (hActive.IsAllocated)     hActive.Free();
					if (hLocale.IsAllocated)     hLocale.Free();
					if (hUpdateRate.IsAllocated) hUpdateRate.Free();
					if (hDeadband.IsAllocated)   hDeadband.Free();
				}

				// set keep alive, if supported.
				if ((masks & (int)StateMask.KeepAlive) != 0)
				{
					int keepAlive = 0;

					try
					{
						((IOPCGroupStateMgt2)m_group).SetKeepAlive(state.KeepAlive, out keepAlive);
					}
					catch 
					{
						state.KeepAlive = 0;
					}
				}

				// return the current state.
				return GetState();
			}
		}
						
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
				if (m_group == null) throw new NotConnectedException();

				// marshal input parameters.
				int count = items.Length;

				OpcRcw.Da.OPCITEMDEF[] definitions = OpcCom.Da.Interop.GetOPCITEMDEFs(items);
                ItemResult[] results = null;

                lock (m_items)
                {
                    for (int ii = 0; ii < count; ii++)
                    {
                        definitions[ii].hClient = ++m_counter;
                    }
     
				    // initialize output parameters.
				    IntPtr pResults = IntPtr.Zero;
				    IntPtr pErrors  = IntPtr.Zero;

				    try
				    {
					    ((IOPCItemMgt)m_group).AddItems(
						    count,
						    definitions,
						    out pResults,
						    out pErrors);
				    }
				    catch (Exception e)
				    {
					    throw OpcCom.Interop.CreateException("IOPCItemMgt.AddItems", e);
				    }

				    // unmarshal output parameters.
				    int[] serverHandles = OpcCom.Da.Interop.GetItemResults(ref pResults, count, true);
				    int[] errors        = OpcCom.Interop.GetInt32s(ref pErrors,  count, true);
    				
				    // construct result list.
				    results = new ItemResult[count];

				    for (int ii = 0; ii < count; ii++)
				    {
					    // create a new ResultIDs.
					    results[ii] = new ItemResult(items[ii]);

					    // save server handles.
					    results[ii].ServerHandle = serverHandles[ii];
					    results[ii].ClientHandle = definitions[ii].hClient;

					    // items created active by default.
					    if (!results[ii].ActiveSpecified)
					    {
						    results[ii].Active = true;
						    results[ii].ActiveSpecified = true;
					    }

					    // update result id.
					    results[ii].ResultID       = OpcCom.Interop.GetResultID(errors[ii]);
					    results[ii].DiagnosticInfo = null;

                        // add new item table.
                        if (results[ii].ResultID.Succeeded())
                        {
                            // save client handle.
                            results[ii].ClientHandle = items[ii].ClientHandle;
		                    m_items[definitions[ii].hClient] = new ItemIdentifier(results[ii]);

	                        // restore internal handle.
	                        results[ii].ClientHandle = definitions[ii].hClient;
                        }
				    }
                }

			    // set non-critical item parameters - these methods all update the item result objects. 
			    UpdateDeadbands(results);
			    UpdateSamplingRates(results);
		        SetEnableBuffering(results);

                lock (m_items)
                {
			        // return results.
				    ItemResult[] filteredResults = (ItemResult[])m_items.ApplyFilters(m_filters, results);
                    
                    // need to return the client handle for failed items.
                    if ((m_filters & (int)ResultFilter.ClientHandle) != 0)
                    {
			            for (int ii = 0; ii < count; ii++)
                        {				    
                            if (filteredResults[ii].ResultID.Failed())
                            {
                                filteredResults[ii].ClientHandle = items[ii].ClientHandle;
                            }
                        }
                    }

                    return filteredResults;
                }
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
				if (m_group == null) throw new NotConnectedException();

				// initialize result list.
				ItemResult[] results = null;
				
				lock (m_items)
				{
					results = m_items.CreateItems(items);
				}

				if ((masks & (int)StateMask.ReqType) != 0)         SetReqTypes(results);
				if ((masks & (int)StateMask.Active) != 0)          UpdateActive(results);
				if ((masks & (int)StateMask.Deadband) != 0)        UpdateDeadbands(results);
				if ((masks & (int)StateMask.SamplingRate) != 0)    UpdateSamplingRates(results);
				if ((masks & (int)StateMask.EnableBuffering) != 0) SetEnableBuffering(results);

				// return results.
				lock (m_items)
				{
					return (ItemResult[])m_items.ApplyFilters(m_filters, results);
				}
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
				if (m_group == null) throw new NotConnectedException();

				// get item ids.
				ItemIdentifier[] itemIDs = null;

				lock (m_items)
				{
					itemIDs = m_items.GetItemIDs(items);
				}			

				// fetch server handles.
				int[] serverHandles = new int[itemIDs.Length];

				for (int ii = 0; ii < itemIDs.Length; ii++) 
				{	
					serverHandles[ii] = (int)itemIDs[ii].ServerHandle;
				}

				// initialize output parameters.
				IntPtr pErrors = IntPtr.Zero;

				try
				{
					((IOPCItemMgt)m_group).RemoveItems(itemIDs.Length, serverHandles, out pErrors);
				}
				catch (Exception e)
				{					
					throw OpcCom.Interop.CreateException("IOPCItemMgt.RemoveItems", e);
				}

				// unmarshal output parameters.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, itemIDs.Length, true);

				// process results.
				IdentifiedResult[] results = new IdentifiedResult[itemIDs.Length];

                ArrayList itemsToRemove = new ArrayList(itemIDs.Length);

				for (int ii = 0; ii < itemIDs.Length; ii++)
				{
					results[ii] = new IdentifiedResult(itemIDs[ii]);

					results[ii].ResultID       = OpcCom.Interop.GetResultID(errors[ii]);
					results[ii].DiagnosticInfo = null;

					// flag item for removal from local list.
					if (results[ii].ResultID.Succeeded())
					{
                        itemsToRemove.Add(results[ii].ClientHandle);
					}
				}

				// apply filter to results.
				lock (m_items)
				{
					results = (IdentifiedResult[])m_items.ApplyFilters(m_filters, results);

				    // remove item from local list.
                    foreach (int clientHandle in itemsToRemove)
                    {
					    m_items[clientHandle] = null;
                    }

                    return results;
				}                
			}
		}	
		
		/// <summary>
		/// Reads the values for a set of items in the subscription.
		/// </summary>
		/// <param name="items">The identifiers (i.e. server handles) for the items being read.</param>
		/// <returns>The value for each of items.</returns>
		public ItemValueResult[] Read(Item[] items)
		{
			if (items == null) throw new ArgumentNullException("items");

			// check if nothing to do.
			if (items.Length == 0)
			{
				return new ItemValueResult[0];
			}

			lock (this)
			{
				if (m_group == null) throw new NotConnectedException();

				// get item ids.
				ItemIdentifier[] itemIDs = null; 
								
				lock (m_items)
				{
					itemIDs = m_items.GetItemIDs(items);
				}

				// read from the server.
				ItemValueResult[] results = Read(itemIDs, items);	

				// return results.
				lock (m_items)
				{
					return (ItemValueResult[])m_items.ApplyFilters(m_filters, results);
				}
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

			// check if nothing to do.
			if (items.Length == 0)
			{
				return new IdentifiedResult[0];
			}

			lock (this)
			{
				if (m_group == null) throw new NotConnectedException();

				// get item ids.
				ItemIdentifier[] itemIDs = null; 
								
				lock (m_items)
				{
					itemIDs = m_items.GetItemIDs(items);
				}

				// write to the server.
				IdentifiedResult[] results = Write(itemIDs, items);	

				// return results.
				lock (m_items)
				{
					return (IdentifiedResult[])m_items.ApplyFilters(m_filters, results);
				}	
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
			if (items == null)    throw new ArgumentNullException("items");
			if (callback == null) throw new ArgumentNullException("callback");

			request = null;

			// check if nothing to do.
			if (items.Length == 0)
			{
				return new IdentifiedResult[0];
			}

			lock (this)
			{
				if (m_group == null) throw new NotConnectedException();

				// ensure a callback connection is established with the server.
				if (m_connection == null)
				{
					Advise();
				}

				// get item ids.
				ItemIdentifier[] itemIDs = null; 
								
				lock (m_items)
				{
					itemIDs = m_items.GetItemIDs(items);
				}

				// create request object.
				OpcCom.Da.Request internalRequest = new OpcCom.Da.Request(
					this, 
					requestHandle, 
					m_filters,
					m_counter++,
					callback);

				// register request with callback object.
				m_callback.BeginRequest(internalRequest);
				request = internalRequest;

				// begin read request.
				IdentifiedResult[] results = null;
				int cancelID = 0;

				try
				{
					results = BeginRead(itemIDs, items, internalRequest.RequestID, out cancelID);
				}
				catch (Exception e)
				{
					m_callback.EndRequest(internalRequest);
					throw e;
				}

				// apply request options.
				lock (m_items)
				{
					m_items.ApplyFilters(m_filters | (int)ResultFilter.ClientHandle, results);
				}

				lock (internalRequest)
				{
					// check if all results have already arrived - this invokes the callback if this is the case.
					if (internalRequest.BeginRead(cancelID, results))
					{
						m_callback.EndRequest(internalRequest);
                        request = null;
					}
				}

				// return initial results.
				return results;
			}
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
			
			if (items == null)    throw new ArgumentNullException("items");
			if (callback == null) throw new ArgumentNullException("callback");

			request = null;

			// check if nothing to do.
			if (items.Length == 0)
			{
				return new IdentifiedResult[0];
			}

			lock (this)
			{
				if (m_group == null) throw new NotConnectedException();

				// ensure a callback connection is established with the server.
				if (m_connection == null)
				{
					Advise();
				}

				// get item ids.
				ItemIdentifier[] itemIDs = null; 
								
				lock (m_items)
				{
					itemIDs = m_items.GetItemIDs(items);
				}

				// create request object.
				OpcCom.Da.Request internalRequest = new OpcCom.Da.Request(
					this, 
					requestHandle, 
					m_filters,
					m_counter++,
					callback);

				// register request with callback object.
				m_callback.BeginRequest(internalRequest);
				request = internalRequest;

				// begin write request.
				IdentifiedResult[] results = null;
				int cancelID = 0;

				try
				{
					results = BeginWrite(itemIDs, items, internalRequest.RequestID, out cancelID);
				}
				catch (Exception e)
				{
					m_callback.EndRequest(internalRequest);
					throw e;
				}

				// apply request options.
				lock (m_items)
				{
					m_items.ApplyFilters(m_filters | (int)ResultFilter.ClientHandle, results);
				}

				lock (internalRequest)
				{
					// check if all results have already arrived - this invokes the callback if this is the case.
					if (internalRequest.BeginWrite(cancelID, results))
					{
						m_callback.EndRequest(internalRequest);
						request = null;
					}
				}

				// return initial results.
				return results;
			}
		}
	
		/// <summary>
		/// Cancels an asynchronous read or write operation.
		/// </summary>
		/// <param name="request">The object returned from the BeginRead or BeginWrite request.</param>
		/// <param name="callback">The function to invoke when the cancel completes.</param>
		public void Cancel(IRequest request, CancelCompleteEventHandler callback) 
		{ 
			if (request == null) throw new ArgumentNullException("request");

			lock (this)
			{
				lock (request)
				{
					// check if request can still be cancelled.
					if (!m_callback.CancelRequest((OpcCom.Da.Request)request))
					{
						return;
					}

					// update the callback.
					((OpcCom.Da.Request)request).Callback = callback;

					// send a cancel request to the server.
					try
					{
						((IOPCAsyncIO2)m_group).Cancel2(((OpcCom.Da.Request)request).CancelID);
					}
					catch (Exception e)
					{	
						throw OpcCom.Interop.CreateException("IOPCAsyncIO2.Cancel2", e);
					}
				}
			}
		}

		/// <summary>
		/// Causes the server to send a data changed notification for all active items. 
		/// </summary>
		public virtual void Refresh()
		{
			lock (this)
			{
				if (m_group == null) throw new NotConnectedException();

				try
				{
					int cancelID = 0;
					((IOPCAsyncIO3)m_group).RefreshMaxAge(Int32.MaxValue, ++m_counter, out cancelID);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCAsyncIO3.RefreshMaxAge", e);
				}
			}
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
				if (m_group == null) throw new NotConnectedException();

				// ensure a callback connection is established with the server.
				if (m_connection == null)
				{
					Advise();
				}

				// create request object.
				OpcCom.Da.Request internalRequest = new OpcCom.Da.Request(
					this, 
					requestHandle, 
					m_filters,
					m_counter++,
					null);

				int cancelID = 0;

				try
				{
					((IOPCAsyncIO3)m_group).RefreshMaxAge(0, (int)internalRequest.RequestID, out cancelID);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCAsyncIO3.RefreshMaxAge", e);
				}

				request = internalRequest;

				// save the cancel id.
				lock (request)
				{
					internalRequest.BeginRefresh(cancelID);
				}
			}
		}

		/// <summary>
		/// Enables or disables data change notifications from the server.
		/// </summary>
		/// <param name="enabled">Whether data change notifications are enabled.</param>
		public virtual void SetEnabled(bool enabled)
		{
			lock (this)
			{
				if (m_group == null) throw new NotConnectedException();

				try
				{
					((IOPCAsyncIO3)m_group).SetEnable((enabled)?1:0);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCAsyncIO3.SetEnable", e);
				}
			}
		}

		/// <summary>
		/// Checks whether data change notifications from the server are enabled.
		/// </summary>
		/// <returns>Whether data change notifications are enabled.</returns>
		public virtual bool GetEnabled()
		{
			lock (this)
			{
				if (m_group == null) throw new NotConnectedException();

				try
				{
					int enabled = 0;
					((IOPCAsyncIO3)m_group).GetEnable(out enabled);
					return enabled != 0;
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCAsyncIO3.GetEnable", e);
				}
			}
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Reads a set of items using DA3.0 interfaces.
		/// </summary>
		protected virtual ItemValueResult[] Read(ItemIdentifier[] itemIDs, Item[] items)
		{
			try
			{
				// marshal input parameters.
				int[] serverHandles = new int[itemIDs.Length];
				int[] maxAges       = new int[itemIDs.Length];

				for (int ii = 0; ii < itemIDs.Length; ii++) 
				{	
					serverHandles[ii] = (int)itemIDs[ii].ServerHandle;
					maxAges[ii]       = (items[ii].MaxAgeSpecified)?items[ii].MaxAge:0;
				}

				// initialize output parameters.
				IntPtr pValues     = IntPtr.Zero;
				IntPtr pQualities  = IntPtr.Zero;
				IntPtr pTimestamps = IntPtr.Zero;
				IntPtr pErrors     = IntPtr.Zero;

				((IOPCSyncIO2)m_group).ReadMaxAge(
					itemIDs.Length,
					serverHandles,
					maxAges,
					out pValues,
					out pQualities,
					out pTimestamps,
					out pErrors);

				// unmarshal output parameters.
				object[]   values     = OpcCom.Interop.GetVARIANTs(ref pValues, itemIDs.Length, true);
				short[]    qualities  = OpcCom.Interop.GetInt16s(ref pQualities, itemIDs.Length, true);
				DateTime[] timestamps = OpcCom.Interop.GetFILETIMEs(ref pTimestamps, itemIDs.Length, true);
				int[]      errors     = OpcCom.Interop.GetInt32s(ref pErrors, itemIDs.Length, true);

				// create item results.
				ItemValueResult[] results = new ItemValueResult[itemIDs.Length];

				for (int ii = 0; ii < itemIDs.Length; ii++)
				{
					results[ii] = new ItemValueResult(itemIDs[ii]);

					results[ii].Value              = values[ii];
					results[ii].Quality            = new Quality(qualities[ii]);
					results[ii].QualitySpecified   = values[ii] != null;
					results[ii].Timestamp          = timestamps[ii];
					results[ii].TimestampSpecified = values[ii] != null;
					results[ii].ResultID           = OpcCom.Interop.GetResultID(errors[ii]);
					results[ii].DiagnosticInfo     = null;

					// convert COM code to unified DA code.
					if (errors[ii] == ResultIDs.E_BADRIGHTS) { results[ii].ResultID = new ResultID(ResultID.Da.E_WRITEONLY, ResultIDs.E_BADRIGHTS); }
				}

				// return results.
				return results;
			}
			catch (Exception e)
			{					
				throw OpcCom.Interop.CreateException("IOPCSyncIO2.ReadMaxAge", e);
			}
		}
		
		/// <summary>
		/// Writes a set of items using DA3.0 interfaces.
		/// </summary>
		protected virtual IdentifiedResult[] Write(ItemIdentifier[] itemIDs, ItemValue[] items)
		{
			try
			{
				// initialize input parameters.
				int[] serverHandles = new int[itemIDs.Length];

				for (int ii = 0; ii < itemIDs.Length; ii++) 
				{	
					serverHandles[ii] = (int)itemIDs[ii].ServerHandle;
				}

				OpcRcw.Da.OPCITEMVQT[] values = OpcCom.Da.Interop.GetOPCITEMVQTs(items);

				// write to sever.
				IntPtr pErrors = IntPtr.Zero;

				((IOPCSyncIO2)m_group).WriteVQT(
					itemIDs.Length,
					serverHandles,
					values,
					out pErrors);

				// unmarshal results.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, itemIDs.Length, true);

				// create result list.
				IdentifiedResult[] results = new IdentifiedResult[itemIDs.Length];

				for (int ii = 0; ii < itemIDs.Length; ii++)
				{
					results[ii] = new IdentifiedResult(itemIDs[ii]);

					results[ii].ResultID       = OpcCom.Interop.GetResultID(errors[ii]);
					results[ii].DiagnosticInfo = null;

					// convert COM code to unified DA code.
					if (errors[ii] == ResultIDs.E_BADRIGHTS) { results[ii].ResultID = new ResultID(ResultID.Da.E_READONLY, ResultIDs.E_BADRIGHTS); }
				}

				// return results.
				return results;
			}
			catch (Exception e)
			{
				throw OpcCom.Interop.CreateException("IOPCSyncIO2.WriteVQT", e);
			}
		}

		/// <summary>
		/// Begins an asynchronous read of a set of items using DA3.0 interfaces.
		/// </summary>
		protected virtual IdentifiedResult[] BeginRead(
			ItemIdentifier[] itemIDs, 
			Item[]           items,
			int              requestID,
			out int          cancelID)
		{
			try
			{
				// marshal input parameters.
				int[] serverHandles = new int[itemIDs.Length];
				int[] maxAges       = new int[itemIDs.Length];

				for (int ii = 0; ii < itemIDs.Length; ii++) 
				{	
					serverHandles[ii] = (int)itemIDs[ii].ServerHandle;
					maxAges[ii]       = (items[ii].MaxAgeSpecified)?items[ii].MaxAge:0;
				}

				// initialize output parameters.
				IntPtr pErrors     = IntPtr.Zero;

				((IOPCAsyncIO3)m_group).ReadMaxAge(
					itemIDs.Length,
					serverHandles,
					maxAges,
					requestID,
					out cancelID,
					out pErrors);

				// unmarshal output parameters.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, itemIDs.Length, true);

				// create item results.
				IdentifiedResult[] results = new IdentifiedResult[itemIDs.Length];

				for (int ii = 0; ii < itemIDs.Length; ii++)
				{
					results[ii] = new IdentifiedResult(itemIDs[ii]);
					
					results[ii].ResultID       = OpcCom.Interop.GetResultID(errors[ii]);
					results[ii].DiagnosticInfo = null;

					// convert COM code to unified DA code.
					if (errors[ii] == ResultIDs.E_BADRIGHTS) { results[ii].ResultID = new ResultID(ResultID.Da.E_WRITEONLY, ResultIDs.E_BADRIGHTS); }
				}

				// return results.
				return results;
			}
			catch (Exception e)
			{					
				throw OpcCom.Interop.CreateException("IOPCAsyncIO3.ReadMaxAge", e);
			}
		}
		
		/// <summary>
		/// Begins an asynchronous write for a set of items using DA3.0 interfaces.
		/// </summary>
		protected virtual IdentifiedResult[] BeginWrite(
			ItemIdentifier[] itemIDs, 
			ItemValue[]      items,
			int              requestID,
			out int          cancelID)
		{
			try
			{
				// initialize input parameters.
				int[] serverHandles = new int[itemIDs.Length];

				for (int ii = 0; ii < itemIDs.Length; ii++) 
				{	
					serverHandles[ii] = (int)itemIDs[ii].ServerHandle;
				}

				OpcRcw.Da.OPCITEMVQT[] values = OpcCom.Da.Interop.GetOPCITEMVQTs(items);

				// write to sever.
				IntPtr pErrors = IntPtr.Zero;

				((IOPCAsyncIO3)m_group).WriteVQT(
					itemIDs.Length,
					serverHandles,
					values,
					requestID,
					out cancelID,
					out pErrors);

				// unmarshal results.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, itemIDs.Length, true);

				// create result list.
				IdentifiedResult[] results = new IdentifiedResult[itemIDs.Length];

				for (int ii = 0; ii < itemIDs.Length; ii++)
				{
					results[ii] = new IdentifiedResult(itemIDs[ii]);

					results[ii].ResultID       = OpcCom.Interop.GetResultID(errors[ii]);
					results[ii].DiagnosticInfo = null;

					// convert COM code to unified DA code.
					if (errors[ii] == ResultIDs.E_BADRIGHTS) { results[ii].ResultID = new ResultID(ResultID.Da.E_READONLY, ResultIDs.E_BADRIGHTS); }
				}

				// return results.
				return results;
			}
			catch (Exception e)
			{
				throw OpcCom.Interop.CreateException("IOPCAsyncIO3.WriteVQT", e);
			}
		}

		/// <summary>
		/// Sets the requested data type for the specified items.
		/// </summary>
		private void SetReqTypes(ItemResult[] items)
		{
			// check if there is nothing to do.
			if (items == null || items.Length == 0) return;

			// clients must explicitly set the ReqType to typeof(object) in order to set it to VT_EMPTY.
			ArrayList changedItems = new ArrayList();

			foreach (ItemResult item in items)
			{
				if (item.ResultID.Succeeded())
				{
					if (item.ReqType != null) changedItems.Add(item);
				}
			}	

			// check if there is nothing to do.
			if (changedItems.Count == 0) return;

			// invoke method.
			try
			{
				// initialize input parameters.
				int[]   handles   = new int[changedItems.Count];
				short[] datatypes = new short[changedItems.Count];
			
				for (int ii = 0; ii < changedItems.Count; ii++)
				{
					ItemResult item = (ItemResult)changedItems[ii];
					handles[ii]     = System.Convert.ToInt32(item.ServerHandle);
					datatypes[ii]   = (short)OpcCom.Interop.GetType(item.ReqType);
				}

				// initialize output parameters.
				IntPtr pErrors = IntPtr.Zero;

				((IOPCItemMgt)m_group).SetDatatypes(
					changedItems.Count,
					handles,
					datatypes,
					out pErrors);

				// check for individual item errors.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, handles.Length, true);

				for (int ii = 0; ii < errors.Length; ii++)
				{
					if (OpcCom.Interop.GetResultID(errors[ii]).Failed())
					{
						ItemResult item     = (ItemResult)changedItems[ii];
						item.ResultID       = ResultID.Da.E_BADTYPE;
						item.DiagnosticInfo = null;
					}
				}
			}

			// treat any general failure to mean the item is deactivated.
			catch
			{
				for (int ii = 0; ii < changedItems.Count; ii++)
				{
					ItemResult item     = (ItemResult)changedItems[ii];
					item.ResultID       = ResultID.Da.E_BADTYPE;
					item.DiagnosticInfo = null;
				}
			}
		}

		/// <summary>
		/// Sets the active state for the specified items.
		/// </summary>
		private void SetActive(ItemResult[] items, bool active)
		{
			// check if there is nothing to do.
			if (items == null || items.Length == 0) return;

			// invoke method.
			try
			{
				// initialize input parameters.
				int[] handles = new int[items.Length];
			
				for (int ii = 0; ii < items.Length; ii++)
				{
					handles[ii] = System.Convert.ToInt32(items[ii].ServerHandle);
				}

				// initialize output parameters.
				IntPtr pErrors = IntPtr.Zero;

				((IOPCItemMgt)m_group).SetActiveState(
					items.Length,
					handles,
					(active)?1:0,
					out pErrors);

				// check for individual item errors.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, handles.Length, true);

				for (int ii = 0; ii < errors.Length; ii++)
				{
					if (OpcCom.Interop.GetResultID(errors[ii]).Failed())
					{
						items[ii].Active = false;
						items[ii].ActiveSpecified = true;
					}
				}
			}

			// treat any general failure to mean the item is deactivated.
			catch
			{
				for (int ii = 0; ii < items.Length; ii++)
				{
					items[ii].Active = false;
					items[ii].ActiveSpecified = true;
				}
			}
		}

		/// <summary>
		/// Update the active state for the specified items.
		/// </summary>
		private void UpdateActive(ItemResult[] items)
		{
			if (items == null || items.Length == 0) return;

			// seperate items in two groups depending on whether the deadband is being set or cleared.
			ArrayList activatedItems   = new ArrayList();
			ArrayList deactivatedItems = new ArrayList();

			foreach (ItemResult item in items)
			{
				if (item.ResultID.Succeeded() && item.ActiveSpecified)
				{
					if (item.Active) 
					{ 
						activatedItems.Add(item);   
					}
					else
					{ 
						deactivatedItems.Add(item); 
					}
				}
			}

			// activate items.
			SetActive((ItemResult[])activatedItems.ToArray(typeof(ItemResult)), true);

			// de-activate items.
			SetActive((ItemResult[])deactivatedItems.ToArray(typeof(ItemResult)), false);
		}
		
		/// <summary>
		/// Sets the deadbands for the specified items.
		/// </summary>
		private void SetDeadbands(ItemResult[] items)
		{
			// check if there is nothing to do.
			if (items == null || items.Length == 0) return;

			// invoke method.
			try
			{
				// initialize input parameters.
				int[]   handles   = new int[items.Length];
				float[] deadbands = new float[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					handles[ii]   = System.Convert.ToInt32(items[ii].ServerHandle);
					deadbands[ii] = items[ii].Deadband;
				}

				// initialize output parameters.
				IntPtr pErrors = IntPtr.Zero;

				((IOPCItemDeadbandMgt)m_group).SetItemDeadband(
					handles.Length,
					handles,
					deadbands,
					out pErrors);

				// check for individual item errors.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, handles.Length, true);

				for (int ii = 0; ii < errors.Length; ii++)
				{
					if (OpcCom.Interop.GetResultID(errors[ii]).Failed())
					{
						items[ii].Deadband          = 0;
						items[ii].DeadbandSpecified = false;
					}
				}
			}

			// treat any general failure as an indication that deadband is not supported.
			catch
			{
				for (int ii = 0; ii < items.Length; ii++)
				{
					items[ii].Deadband          = 0;
					items[ii].DeadbandSpecified = false;
				}
			}
		}

		/// <summary>
		/// Clears the deadbands for the specified items.
		/// </summary>
		private void ClearDeadbands(ItemResult[] items)
		{
			// check if there is nothing to do.
			if (items == null || items.Length == 0) return;

			// invoke method.
			try
			{
				// initialize input parameters.
				int[] handles = new int[items.Length];
			
				for (int ii = 0; ii < items.Length; ii++)
				{
					handles[ii] = System.Convert.ToInt32(items[ii].ServerHandle);
				}

				// initialize output parameters.
				IntPtr pErrors = IntPtr.Zero;

				((IOPCItemDeadbandMgt)m_group).ClearItemDeadband(
					handles.Length,
					handles,
					out pErrors);

				// check for individual item errors.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, handles.Length, true);

				for (int ii = 0; ii < errors.Length; ii++)
				{
					if (OpcCom.Interop.GetResultID(errors[ii]).Failed())
					{
						items[ii].Deadband          = 0;
						items[ii].DeadbandSpecified = false;
					}
				}
			}

			// treat any general failure as an indication that deadband is not supported.
			catch
			{
				for (int ii = 0; ii < items.Length; ii++)
				{
					items[ii].Deadband          = 0;
					items[ii].DeadbandSpecified = false;
				}
			}
		}

		/// <summary>
		/// Update the deadbands for the specified items.
		/// </summary>
		private void UpdateDeadbands(ItemResult[] items)
		{
			if (items == null || items.Length == 0) return;

			// seperate items in two groups depending on whether the deadband is being set or cleared.
			ArrayList changedItems = new ArrayList();
			ArrayList clearedItems = new ArrayList();

			foreach (ItemResult item in items)
			{
				if (item.ResultID.Succeeded())
				{
					if (item.DeadbandSpecified) 
					{
						changedItems.Add(item);
					}
					else
					{
						clearedItems.Add(item);
					}
				}
			}

			// set deadbands.
			SetDeadbands((ItemResult[])changedItems.ToArray(typeof(ItemResult)));

			// clear deadbands.
			ClearDeadbands((ItemResult[])clearedItems.ToArray(typeof(ItemResult)));
		}

		/// <summary>
		/// Sets the sampling rates for the specified items.
		/// </summary>
		private void SetSamplingRates(ItemResult[] items)
		{
			// check if there is nothing to do.
			if (items == null || items.Length == 0) return;

			// invoke method.
			try
			{
				// initialize input parameters.
				int[] handles      = new int[items.Length];
				int[] samplingRate = new int[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					handles[ii]      = System.Convert.ToInt32(items[ii].ServerHandle);
					samplingRate[ii] = items[ii].SamplingRate;
				}

				// initialize output parameters.
				IntPtr pResults = IntPtr.Zero;
				IntPtr pErrors  = IntPtr.Zero;

				((IOPCItemSamplingMgt)m_group).SetItemSamplingRate(
					handles.Length,
					handles,
					samplingRate,
					out pResults,
					out pErrors);

				// check for individual item errors.
				int[] results = OpcCom.Interop.GetInt32s(ref pResults, handles.Length, true);
				int[] errors  = OpcCom.Interop.GetInt32s(ref pErrors, handles.Length, true);

				for (int ii = 0; ii < errors.Length; ii++)
				{
					if (items[ii].SamplingRate != results[ii])
					{
						items[ii].SamplingRate          = results[ii];
						items[ii].SamplingRateSpecified = true;
						continue;
					}

					if (OpcCom.Interop.GetResultID(errors[ii]).Failed())
					{
						items[ii].SamplingRate          = 0;
						items[ii].SamplingRateSpecified = false;
						continue;
					}
				}
			}

			// treat any general failure as an indication that sampling rate is not supported.
			catch
			{
				for (int ii = 0; ii < items.Length; ii++)
				{
					items[ii].SamplingRate          = 0;
					items[ii].SamplingRateSpecified = false;
				}
			}
		}

		/// <summary>
		/// Clears the sampling rates for the specified items.
		/// </summary>
		private void ClearSamplingRates(ItemResult[] items)
		{
			// check if there is nothing to do.
			if (items == null || items.Length == 0) return;

			// invoke method.
			try
			{
				// initialize input parameters.
				int[] handles = new int[items.Length];
			
				for (int ii = 0; ii < items.Length; ii++)
				{
					handles[ii] = System.Convert.ToInt32(items[ii].ServerHandle);
				}

				// initialize output parameters.
				IntPtr pErrors = IntPtr.Zero;

				((IOPCItemSamplingMgt)m_group).ClearItemSamplingRate(
					handles.Length,
					handles,
					out pErrors);

				// check for individual item errors.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, handles.Length, true);

				for (int ii = 0; ii < errors.Length; ii++)
				{
					if (OpcCom.Interop.GetResultID(errors[ii]).Failed())
					{
						items[ii].SamplingRate          = 0;
						items[ii].SamplingRateSpecified = false;
					}
				}
			}

			// treat any general failure as an indication that sampling rate is not supported.
			catch
			{
				for (int ii = 0; ii < items.Length; ii++)
				{
					items[ii].SamplingRate          = 0;
					items[ii].SamplingRateSpecified = false;
				}
			}
		}

		/// <summary>
		/// Update the sampling rates for the specified items.
		/// </summary>
		private void UpdateSamplingRates(ItemResult[] items)
		{
			if (items == null || items.Length == 0) return;

			// seperate items in two groups depending on whether the sampling rate is being set or cleared.
			ArrayList changedItems = new ArrayList();
			ArrayList clearedItems = new ArrayList();

			foreach (ItemResult item in items)
			{
				if (item.ResultID.Succeeded())
				{
					if (item.SamplingRateSpecified) 
					{
						changedItems.Add(item);
					}
					else
					{
						clearedItems.Add(item);
					}
				}
			}

			// set sampling rates.
			SetSamplingRates((ItemResult[])changedItems.ToArray(typeof(ItemResult)));

			// clear sampling rates.
			ClearSamplingRates((ItemResult[])clearedItems.ToArray(typeof(ItemResult)));
		}

		/// <summary>
		/// Sets the enable buffering flags.
		/// </summary>
		private void SetEnableBuffering(ItemResult[] items)
		{
			// check if there is nothing to do.
			if (items == null || items.Length == 0) return;
			
			ArrayList changedItems = new ArrayList();

			foreach (ItemResult item in items)
			{
				if (item.ResultID.Succeeded()) 
				{
					changedItems.Add(item);
				}
			}

			// check if there is nothing to do.
			if (changedItems.Count == 0) return;
			
			// invoke method.
			try
			{
				// initialize input parameters.
				int[] handles = new int[changedItems.Count];
				int[] enabled = new int[changedItems.Count];
			
				for (int ii = 0; ii < changedItems.Count; ii++)
				{
					ItemResult item = (ItemResult)changedItems[ii];
					handles[ii] = System.Convert.ToInt32(item.ServerHandle);
					enabled[ii] = (item.EnableBufferingSpecified && item.EnableBuffering)?1:0;
				}

				// initialize output parameters.
				IntPtr pErrors = IntPtr.Zero;

				((IOPCItemSamplingMgt)m_group).SetItemBufferEnable(
					handles.Length,
					handles,
					enabled,
					out pErrors);

				// check for individual item errors.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, handles.Length, true);

				for (int ii = 0; ii < errors.Length; ii++)
				{
					ItemResult item = (ItemResult)changedItems[ii];

					if (OpcCom.Interop.GetResultID(errors[ii]).Failed())
					{
						item.EnableBuffering          = false;
						item.EnableBufferingSpecified = true;
					}
				}
			}

			// treat any general failure as an indication that enable buffering is not supported.
			catch
			{
				foreach (ItemResult item in changedItems)
				{
					item.EnableBuffering          = false;
					item.EnableBufferingSpecified = true;
				}
			}
		}
		
		/// <summary>
		/// Establishes a connection point callback with the COM server.
		/// </summary>
		private void Advise()
		{
			if (m_connection == null)
			{
				m_connection = new ConnectionPoint(m_group, typeof(OpcRcw.Da.IOPCDataCallback).GUID);
				m_connection.Advise(m_callback);
			}
		}

		/// <summary>
		/// Closes a connection point callback with the COM server.
		/// </summary>
		private void Unadvise()
		{
			if (m_connection != null)
			{
				if (m_connection.Unadvise() == 0)
				{
					m_connection.Dispose();
					m_connection = null;
				}
			}
		}
		#endregion

		#region ItemTable Class
		/// <summary>
		/// A table of item identifiers indexed by internal handle.
		/// </summary>
		private class ItemTable
		{
			/// <summary>
			/// Looks up an item identifier for the specified internal handle.
			/// </summary>
			public ItemIdentifier this[object handle]
			{
				get 
				{ 
					if (handle != null) 
					{ 
						return (ItemIdentifier)m_items[handle]; 
					} 
					
					return null; 
				}
				
				set 
				{ 
					if (handle != null) 
					{ 
						if (value ==  null) 
						{ 
							m_items.Remove(handle); 
							return; 
						}

						m_items[handle] = value;
					} 
				}
			}			
			
			/// <summary>
			/// Returns a server handle that must be treated as invalid by the server,
			/// </summary>
			/// <returns></returns>
			private int GetInvalidHandle()
			{
				int invalidHandle = 0;

				foreach (Opc.ItemIdentifier item in m_items.Values)
				{
					if (item.ServerHandle != null && item.ServerHandle.GetType() == typeof(int))
					{
						if (invalidHandle < (int)item.ServerHandle)
						{
							invalidHandle = (int)item.ServerHandle + 1;
						}
					}
				}

				return invalidHandle;
			}

			/// <summary>
			/// Copies a set of items an substitutes the client and server handles for use by the server.
			/// </summary>
			public ItemIdentifier[] GetItemIDs(ItemIdentifier[] items)
			{
				// create an invalid server handle.
				int invalidHandle = GetInvalidHandle();

				// copy the items.
				ItemIdentifier[] itemIDs = new ItemIdentifier[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					// lookup server handle.
					ItemIdentifier itemID = this[items[ii].ServerHandle];

					// copy the item id.
					if (itemID != null)
					{
						itemIDs[ii] = (ItemIdentifier)itemID.Clone();
					}

					// create an invalid item id.
					else
					{
						itemIDs[ii]              = new ItemIdentifier();
						itemIDs[ii].ServerHandle = invalidHandle;
					}

					// store the internal handle as the client handle.
					itemIDs[ii].ClientHandle = items[ii].ServerHandle;
				}

				// return the item copies.
				return itemIDs;
			}

			/// <summary>
			/// Creates a item result list from a set of items and sets the handles for use by the server.
			/// </summary>
			public ItemResult[] CreateItems(Item[] items)
			{
				if (items == null) { return null; }

				ItemResult[] results = new ItemResult[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{		
					// initialize result with the item
					results[ii] = new ItemResult((Item)items[ii]);
			
					// lookup the cached identifier.
					ItemIdentifier itemID = this[items[ii].ServerHandle];
					
					if (itemID != null)
					{
						results[ii].ItemName     = itemID.ItemName;
						results[ii].ItemPath     = itemID.ItemName;
						results[ii].ServerHandle = itemID.ServerHandle;

						// update the client handle.
						itemID.ClientHandle = items[ii].ClientHandle;
					}

					// check if handle not found.
					if (results[ii].ServerHandle == null)
					{
						results[ii].ResultID       = ResultID.Da.E_INVALIDHANDLE;
						results[ii].DiagnosticInfo = null;
						continue;
					}

					// replace client handle with internal handle.
					results[ii].ClientHandle = items[ii].ServerHandle;
				}

				return results;
			}

			/// <summary>
			/// Updates a result list based on the request options and sets the handles for use by the client.
			/// </summary>
			public ItemIdentifier[] ApplyFilters(int filters, ItemIdentifier[] results)
			{
				if (results == null) { return null; }

				foreach (ItemIdentifier result in results)
				{
					ItemIdentifier itemID = this[result.ClientHandle];

					if (itemID != null)
					{
						result.ItemName     = ((filters & (int)ResultFilter.ItemName) != 0)?itemID.ItemName:null;
						result.ItemPath     = ((filters & (int)ResultFilter.ItemPath) != 0)?itemID.ItemPath:null;
						result.ServerHandle = result.ClientHandle;
						result.ClientHandle = ((filters & (int)ResultFilter.ClientHandle) != 0)?itemID.ClientHandle:null;
					}

					if ((filters & (int)ResultFilter.ItemTime) == 0)
					{
						if (result.GetType() == typeof(ItemValueResult))
						{
							((ItemValueResult)result).Timestamp = DateTime.MinValue;
							((ItemValueResult)result).TimestampSpecified = false;
						}
					}
				}

				return results;
			}

			/// <summary>
			/// The table of known item identifiers.
			/// </summary>
			private Hashtable m_items = new Hashtable();
		}
		#endregion

		#region IOPCDataCallback Members
		/// <summary>
		/// A class that implements the IOPCDataCallback interface.
		/// </summary>
		private class Callback : OpcRcw.Da.IOPCDataCallback
		{
			/// <summary>
			/// Initializes the object with the containing subscription object.
			/// </summary>
			public Callback(object handle, int filters, ItemTable items) 
			{ 
				m_handle  = handle; 
				m_filters = filters;
				m_items   = items;
			}
			
			/// <summary>
			/// Updates the result filters and subscription handle.
			/// </summary>
			public void SetFilters(object handle, int filters)
			{
				lock (this)
				{
					m_handle  = handle; 
					m_filters = filters;
				}
			}

			/// <summary>
			/// Adds an asynchrounous request.
			/// </summary>
			public void BeginRequest(OpcCom.Da.Request request)
			{
				lock (this)
				{
					m_requests[request.RequestID] = request;
				}
			}

			/// <summary>
			/// Returns true is an asynchrounous request can be cancelled.
			/// </summary>
			public bool CancelRequest(OpcCom.Da.Request request)
			{
				lock (this)
				{
					return m_requests.ContainsKey(request.RequestID);
				}
			}

			/// <summary>
			/// Remvoes an asynchrounous request.
			/// </summary>
			public void EndRequest(OpcCom.Da.Request request)
			{
				lock (this)
				{
					m_requests.Remove(request.RequestID);
				}
			}

			/// <summary>
			/// The handle to return with any callbacks. 
			/// </summary>
			private object m_handle = null;

			/// <summary>
			/// The current request options for the subscription.
			/// </summary>
			private int m_filters = (int)ResultFilter.Minimal;

			/// <summary>
			/// A table of item identifiers indexed by internal handle.
			/// </summary>
			private ItemTable m_items = null;
			
			/// <summary>
			/// A table of autstanding asynchronous requests.
			/// </summary>
			private Hashtable m_requests = new Hashtable();

			/// <summary>
			/// Raised when data changed callbacks arrive.
			/// </summary>
			public event DataChangedEventHandler DataChanged
			{
				add    {lock (this){ m_dataChanged += value; }}
				remove {lock (this){ m_dataChanged -= value; }}
			}
			/// <remarks/>
			private event DataChangedEventHandler m_dataChanged = null;

			/// <summary>
			/// Called when a data changed event is received.
			/// </summary>
			public void OnDataChange(
				int                  dwTransid,
				int                  hGroup,
				int                  hrMasterquality,
				int                  hrMastererror,
				int                  dwCount,
				int[]                phClientItems,
				object[]             pvValues,
				short[]              pwQualities,
				OpcRcw.Da.FILETIME[] pftTimeStamps,
				int[]                pErrors)
			{
				try
				{
					OpcCom.Da.Request request = null;

					lock (this)
					{
						// check for an outstanding request.
						if (dwTransid != 0)
						{
							request = (OpcCom.Da.Request)m_requests[dwTransid];

							if (request != null)
							{
								// remove the request.
								m_requests.Remove(dwTransid);  
							}
						}

						// do nothing if no connections.
						if (m_dataChanged == null) return;

						// unmarshal item values.
						ItemValueResult[] values = UnmarshalValues(
							dwCount, 
							phClientItems, 
							pvValues, 
							pwQualities, 
							pftTimeStamps, 
							pErrors);

						// apply request options.
						lock (m_items)
						{
							m_items.ApplyFilters(m_filters | (int)ResultFilter.ClientHandle, values);
						}

						// invoke the callback.
						m_dataChanged(m_handle, (request != null)?request.Handle:null, values);
					}
				}
				catch (Exception e) 
				{ 
					string stack = e.StackTrace;
				}
			}

			// sends read complete notifications.
			public void OnReadComplete(
				int                  dwTransid,
				int                  hGroup,
				int                  hrMasterquality,
				int                  hrMastererror,
				int                  dwCount,
				int[]                phClientItems,
				object[]             pvValues,
				short[]              pwQualities,
				OpcRcw.Da.FILETIME[] pftTimeStamps,
				int[]                pErrors)
			{
				try
				{
					OpcCom.Da.Request request = null;
					ItemValueResult[] values  = null;

					lock (this)
					{
						// do nothing if no outstanding requests.
						request = (OpcCom.Da.Request)m_requests[dwTransid];

						if (request == null)
						{
							return;
						}

						// remove the request.
						m_requests.Remove(dwTransid);              
						
						// unmarshal item values.
						values = UnmarshalValues(
							dwCount, 
							phClientItems, 
							pvValues, 
							pwQualities, 
							pftTimeStamps, 
							pErrors);

						// apply request options.
						lock (m_items)
						{
							m_items.ApplyFilters(m_filters | (int)ResultFilter.ClientHandle, values);
						}
					}

					// end the request.
					lock (request)
					{
						request.EndRequest(values);
					}
				}
				catch (Exception e) 
				{ 
					string stack = e.StackTrace;
				}
			}

			// handles asynchronous write complete events.
			public void OnWriteComplete(
				int   dwTransid,
				int   hGroup,
				int   hrMastererror,
				int   dwCount,
				int[] phClientItems,
				int[] pErrors)
			{
				try
				{
					OpcCom.Da.Request  request = null;
					IdentifiedResult[] results = null;

					lock (this)
					{
						// do nothing if no outstanding requests.
						request = (OpcCom.Da.Request)m_requests[dwTransid];

						if (request == null)
						{
							return;
						}

						// remove the request.
						m_requests.Remove(dwTransid);              
						
						// contruct the item results.
						results = new IdentifiedResult[dwCount];

						for (int ii = 0; ii < results.Length; ii++)
						{
							// lookup the external client handle.
							ItemIdentifier itemID = (ItemIdentifier)m_items[phClientItems[ii]];

							results[ii]                = new IdentifiedResult(itemID);
							results[ii].ClientHandle   = phClientItems[ii];
							results[ii].ResultID       = OpcCom.Interop.GetResultID(pErrors[ii]);
							results[ii].DiagnosticInfo = null;

							// convert COM code to unified DA code.
							if (pErrors[ii] == ResultIDs.E_BADRIGHTS) { results[ii].ResultID = new ResultID(ResultID.Da.E_READONLY, ResultIDs.E_BADRIGHTS); }
						}
					
						// apply request options.
						lock (m_items)
						{
							m_items.ApplyFilters(m_filters | (int)ResultFilter.ClientHandle, results);
						}
					}

					// end the request.
					lock (request)
					{
						request.EndRequest(results);
					}
				}
				catch (Exception e) 
				{ 
					string stack = e.StackTrace;
				}
			}

			// handles asynchronous request cancel events.
			public void OnCancelComplete(
				int dwTransid,
				int hGroup)
			{
				try
				{
					OpcCom.Da.Request request = null;

					lock (this)
					{
						// do nothing if no outstanding requests.
						request = (OpcCom.Da.Request)m_requests[dwTransid];

						if (request == null)
						{
							return;
						}

						// remove the request.
						m_requests.Remove(dwTransid);
					}

					// end the request.
					lock (request)
					{
						request.EndRequest();
					}
				}
				catch (Exception e) 
				{ 
					string stack = e.StackTrace;
				}
			}

			/// <summary>
			/// Creates an array of item value result objects from the callback data.
			/// </summary>
			private ItemValueResult[] UnmarshalValues(
				int                  dwCount,
				int[]                phClientItems,
				object[]             pvValues,
				short[]              pwQualities,
				OpcRcw.Da.FILETIME[] pftTimeStamps,
				int[]                pErrors)
			{
				// contruct the item value results.
				ItemValueResult[] values = new ItemValueResult[dwCount];

				for (int ii = 0; ii < values.Length; ii++)
				{
					// lookup the external client handle.
					ItemIdentifier itemID = (ItemIdentifier)m_items[phClientItems[ii]];

					values[ii]                    = new ItemValueResult(itemID);
					values[ii].ClientHandle       = phClientItems[ii];
					values[ii].Value              = pvValues[ii];
					values[ii].Quality            = new Quality(pwQualities[ii]);
					values[ii].QualitySpecified   = true;
					values[ii].Timestamp          = OpcCom.Interop.GetFILETIME(OpcCom.Da.Interop.Convert(pftTimeStamps[ii]));
					values[ii].TimestampSpecified = values[ii].Timestamp != DateTime.MinValue;
					values[ii].ResultID           = OpcCom.Interop.GetResultID(pErrors[ii]);
					values[ii].DiagnosticInfo     = null;

					// convert COM code to unified DA code.
					if (pErrors[ii] == ResultIDs.E_BADRIGHTS) { values[ii].ResultID = new ResultID(ResultID.Da.E_WRITEONLY, ResultIDs.E_BADRIGHTS); }
				}

				// return results
				return values;
			}
		}
		#endregion
	}

	#region OpcCom.Da.Request Class
	/// <summary>
	/// Contains the state of an asynchronous request to a COM server.
	/// </summary>
	[Serializable]
	public class Request : Opc.Da.Request
	{	
		/// <summary>
		/// The unique id assigned by the subscription.
		/// </summary>
		internal int RequestID = 0;

		/// <summary>
		/// The unique id assigned by the server.
		/// </summary>
		internal int CancelID = 0;

		/// <summary>
		/// The callback used when the request completes.
		/// </summary>
		internal Delegate Callback = null;

		/// <summary>
		/// The result filters to use for the request.
		/// </summary>
		internal int Filters = 0;

		/// <summary>
		/// The set of initial results.
		/// </summary>
		internal ItemIdentifier[] InitialResults = null;

		/// <summary>
		/// Initializes the object with a subscription and a unique id.
		/// </summary>
		public Request(
			ISubscription  subscription, 
			object         clientHandle,
			int            filters,
			int            requestID,
			Delegate       callback) 
		: 
			base(subscription, clientHandle)
		{
			Filters        = filters;
			RequestID      = requestID;
			Callback       = callback;
			CancelID       = 0;
			InitialResults = null;
		}

		/// <summary>
		/// Begins a read request by storing the initial results.
		/// </summary>
		public bool BeginRead(int cancelID, IdentifiedResult[] results)
		{
			CancelID = cancelID;

			ItemValueResult[] values = null;

			// check if results have already arrived.
			if (InitialResults != null)
			{
				if (InitialResults.GetType() == typeof(ItemValueResult[]))
				{
					values = (ItemValueResult[])InitialResults;
					InitialResults = results;
					EndRequest(values);
					return true;
				}
			}

			// check that at least one valid item existed.
			foreach (IdentifiedResult result in results)
			{
				if (result.ResultID.Succeeded())
				{
					InitialResults = results;
					return false;
				}
			}

			// request complete - all items had errors.
			return true;
		}

		/// <summary>
		/// Begins a write request by storing the initial results.
		/// </summary>
		public bool BeginWrite(int cancelID, IdentifiedResult[] results)
		{
			CancelID = cancelID;

			// check if results have already arrived.
			if (InitialResults != null)
			{
				if (InitialResults.GetType() == typeof(IdentifiedResult[]))
				{
					IdentifiedResult[] callbackResults = (IdentifiedResult[])InitialResults;
					InitialResults = results;
					EndRequest(callbackResults);
					return true;
				}
			}

			// check that at least one valid item existed.
			foreach (IdentifiedResult result in results)
			{
				if (result.ResultID.Succeeded())
				{
					InitialResults = results;
					return false;
				}
			}

			// apply filters.		
			for (int ii = 0; ii < results.Length; ii++)
			{
				if ((Filters & (int)ResultFilter.ItemName) == 0)     results[ii].ItemName = null; 
				if ((Filters & (int)ResultFilter.ItemPath) == 0)     results[ii].ItemPath = null; 
				if ((Filters & (int)ResultFilter.ClientHandle) == 0) results[ii].ClientHandle = null; 
			}

			// invoke callback.
			((WriteCompleteEventHandler)Callback)(Handle, results);
			
			return true;
		}

		/// <summary>
		/// Begins a refersh request by saving the cancel id.
		/// </summary>
		public bool BeginRefresh(int cancelID)
		{
			// save cancel id.
			CancelID = cancelID;

			// request not complete.
			return false;
		}

		/// <summary>
		/// Completes a read request by processing the values and invoking the callback.
		/// </summary>
		public void EndRequest()
		{			
			// check for cancelled request.
			if (typeof(CancelCompleteEventHandler).IsInstanceOfType(Callback))
			{
				((CancelCompleteEventHandler)Callback)(Handle);
				return;
			}
		}

		/// <summary>
		/// Completes a read request by processing the values and invoking the callback.
		/// </summary>
		public void EndRequest(ItemValueResult[] results)
		{
			// check if the begin request has not completed yet.
			if (InitialResults == null)
			{
				InitialResults = results;
				return;
			}		

			// check for cancelled request.
			if (typeof(CancelCompleteEventHandler).IsInstanceOfType(Callback))
			{
				((CancelCompleteEventHandler)Callback)(Handle);
				return;
			}

			// apply filters.
			for (int ii = 0; ii < results.Length; ii++)
			{
				if ((Filters & (int)ResultFilter.ItemName) == 0)     results[ii].ItemName = null; 
				if ((Filters & (int)ResultFilter.ItemPath) == 0)     results[ii].ItemPath = null; 
				if ((Filters & (int)ResultFilter.ClientHandle) == 0) results[ii].ClientHandle = null; 
				
				if ((Filters & (int)ResultFilter.ItemTime) == 0) 
				{
					results[ii].Timestamp = DateTime.MinValue;
					results[ii].TimestampSpecified = false; 
				}
			}

			// invoke callback.
			if (typeof(ReadCompleteEventHandler).IsInstanceOfType(Callback))
			{
				((ReadCompleteEventHandler)Callback)(Handle, results);
			}
		}

		/// <summary>
		/// Completes a write request by processing the values and invoking the callback.
		/// </summary>
		public void EndRequest(IdentifiedResult[] callbackResults)
		{
			// check if the begin request has not completed yet.
			if (InitialResults == null)
			{
				InitialResults = callbackResults;
				return;
			}		

			// check for cancelled request.
			if (Callback != null && Callback.GetType() == typeof(CancelCompleteEventHandler))
			{
				((CancelCompleteEventHandler)Callback)(Handle);
				return;
			}

			// update initial results with callback results.
			IdentifiedResult[] results = (IdentifiedResult[])InitialResults;

			// insert matching value by checking client handle.
			int index = 0;

			for (int ii = 0; ii < results.Length; ii++)
			{
				while (index < callbackResults.Length)
				{
					// the initial results have the internal handle stores as the server handle.
					if (callbackResults[ii].ServerHandle.Equals(results[index].ServerHandle))
					{
						results[index++] = callbackResults[ii];
						break;
					}

					index++;
				}
			}

			// apply filters.
			for (int ii = 0; ii < results.Length; ii++)
			{
				if ((Filters & (int)ResultFilter.ItemName) == 0)     results[ii].ItemName = null; 
				if ((Filters & (int)ResultFilter.ItemPath) == 0)     results[ii].ItemPath = null; 
				if ((Filters & (int)ResultFilter.ClientHandle) == 0) results[ii].ClientHandle = null; 
			}

			// invoke callback.
			if (Callback != null && Callback.GetType() == typeof(WriteCompleteEventHandler))
			{
				((WriteCompleteEventHandler)Callback)(Handle, results);
			}
		}
	}
	#endregion
}
