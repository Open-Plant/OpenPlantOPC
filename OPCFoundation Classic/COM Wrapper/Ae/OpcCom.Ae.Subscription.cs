//============================================================================
// TITLE: OpcCom.Ae.Subscription.cs
//
// CONTENTS:
// 
// A .NET wrapper for a COM server that implements the AE subscription interfaces.
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
using System.Runtime.InteropServices;
using Opc;
using Opc.Ae;
using OpcRcw.Ae;
using OpcRcw.Comn;

namespace OpcCom.Ae
{
	/// <summary>
	/// A .NET wrapper for a COM server that implements the AE subscription interfaces.
	/// </summary>
	[Serializable]
	public class Subscription : Opc.Ae.ISubscription
	{
		#region Constructors
		/// <summary>
		/// Initializes the object with the specified URL and COM server.
		/// </summary>
		internal Subscription(SubscriptionState state, object subscription)
		{
			m_subscription = subscription;
			m_clientHandle = Opc.Convert.Clone(state.ClientHandle);
			m_supportsAE11 = true;
			m_callback     = new Callback(state.ClientHandle);

			// check if the V1.1 interfaces are supported.
			try
			{
				IOPCEventSubscriptionMgt2 server = (IOPCEventSubscriptionMgt2)m_subscription;
			}
			catch
			{
				m_supportsAE11 = false;
			}
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

                        // close connections.
                        if (m_connection != null)
                        {
                            m_connection.Dispose();
                            m_connection = null;
                        }
                    }

                    // Free your own state (unmanaged objects).
                    // Set large fields to null.

                    // release the COM server.
                    if (m_subscription != null)
                    {
                        OpcCom.Interop.ReleaseServer(m_subscription);
                        m_subscription = null;
                    }
                }

                m_disposed = true;
            }
        }

        private bool m_disposed = false;
		#endregion

		#region Opc.Ae.ISubscription Members
		/// <summary>
		/// An event to receive data change updates.
		/// </summary>
		public event EventChangedEventHandler EventChanged
		{ 
			add    {lock (this){ Advise(); m_callback.EventChanged += value;   }}
			remove {lock (this){ m_callback.EventChanged -= value; Unadvise(); }}
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
				// verify state and arguments.
				if (m_subscription == null) throw new NotConnectedException();

				// initialize arguments.
				int pbActive = 0;
				int pdwBufferTime = 0;
				int pdwMaxSize = 0;
				int phClientSubscription = 0;
				int pdwKeepAliveTime = 0;

				// invoke COM method.
				try
				{
					((IOPCEventSubscriptionMgt)m_subscription).GetState(
						out pbActive,
						out pdwBufferTime,
						out pdwMaxSize,
						out phClientSubscription);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventSubscriptionMgt.GetState", e);
				}		
	
				// get keep alive.
				if (m_supportsAE11)
				{
					try
					{
						((IOPCEventSubscriptionMgt2)m_subscription).GetKeepAlive(out pdwKeepAliveTime);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCEventSubscriptionMgt2.GetKeepAlive", e);
					}
				}

				// build results 
				SubscriptionState state = new SubscriptionState();

				state.Active       = pbActive != 0;
				state.ClientHandle = m_clientHandle;
				state.BufferTime   = pdwBufferTime;
				state.MaxSize      = pdwMaxSize;
				state.KeepAlive    = pdwKeepAliveTime;
				
				// return results.
				return state;
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
			lock (this)
			{
				// verify state and arguments.
				if (m_subscription == null) throw new NotConnectedException();

				// initialize arguments.
				int active = (state.Active)?1:0;

				GCHandle hActive     = GCHandle.Alloc(active, GCHandleType.Pinned);
				GCHandle hBufferTime = GCHandle.Alloc(state.BufferTime, GCHandleType.Pinned);
				GCHandle hMaxSize    = GCHandle.Alloc(state.MaxSize, GCHandleType.Pinned);

				IntPtr pbActive      = ((masks & (int)StateMask.Active) != 0)?hActive.AddrOfPinnedObject():IntPtr.Zero;
				IntPtr pdwBufferTime = ((masks & (int)StateMask.BufferTime) != 0)?hBufferTime.AddrOfPinnedObject():IntPtr.Zero;
				IntPtr pdwMaxSize    = ((masks & (int)StateMask.MaxSize) != 0)?hMaxSize.AddrOfPinnedObject():IntPtr.Zero;

				int phClientSubscription = 0;
				int pdwRevisedBufferTime = 0;
				int pdwRevisedMaxSize = 0;

				// invoke COM method.
				try
				{
					((IOPCEventSubscriptionMgt)m_subscription).SetState(
						pbActive,
						pdwBufferTime,
						pdwMaxSize,
						phClientSubscription,
						out pdwRevisedBufferTime,
						out pdwRevisedMaxSize);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventSubscriptionMgt.SetState", e);
				}		
				finally
				{
					if (hActive.IsAllocated)     hActive.Free();
					if (hBufferTime.IsAllocated) hBufferTime.Free();
					if (hMaxSize.IsAllocated)    hMaxSize.Free();
				}

				// update keep alive.
				if (((masks & (int)StateMask.KeepAlive) != 0) && m_supportsAE11)
				{
					int pdwRevisedKeepAliveTime = 0;

					try
					{
						((IOPCEventSubscriptionMgt2)m_subscription).SetKeepAlive(
							state.KeepAlive,
							out pdwRevisedKeepAliveTime);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCEventSubscriptionMgt2.SetKeepAlive", e);
					}
				}
					
				// return current state.
				return GetState();
			}
		}

		//======================================================================
		// Filter Management

		/// <summary>
		/// Returns the current filters for the subscription.
		/// </summary>
		/// <returns>The current filters for the subscription.</returns>
		public SubscriptionFilters GetFilters()
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_subscription == null) throw new NotConnectedException();

				// initialize arguments.
				int pdwEventType = 0;
				int pdwNumCategories = 0;
				IntPtr ppdwEventCategories = IntPtr.Zero;
				int pdwLowSeverity = 0;
				int pdwHighSeverity = 0;
				int pdwNumAreas = 0;
				IntPtr ppszAreaList = IntPtr.Zero;
				int pdwNumSources = 0;
				IntPtr ppszSourceList = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCEventSubscriptionMgt)m_subscription).GetFilter(
						out pdwEventType,
						out pdwNumCategories,
						out ppdwEventCategories,
						out pdwLowSeverity,
						out pdwHighSeverity,
						out pdwNumAreas,
						out ppszAreaList,
						out pdwNumSources,
						out ppszSourceList);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventSubscriptionMgt.GetFilter", e);
				}		

				// unmarshal results 
				int[]    categoryIDs = OpcCom.Interop.GetInt32s(ref ppdwEventCategories, pdwNumCategories, true);
				string[] areaIDs     = OpcCom.Interop.GetUnicodeStrings(ref ppszAreaList, pdwNumAreas, true);
				string[] sourceIDs   = OpcCom.Interop.GetUnicodeStrings(ref ppszSourceList, pdwNumSources, true);

				// build results.
				SubscriptionFilters filters = new SubscriptionFilters();

				filters.EventTypes   = pdwEventType;
				filters.LowSeverity  = pdwLowSeverity;
				filters.HighSeverity = pdwHighSeverity;

				filters.Categories.AddRange(categoryIDs);
				filters.Areas.AddRange(areaIDs);
				filters.Sources.AddRange(sourceIDs);

				// return results.
				return filters;
			}
		}

		/// <summary>
		/// Sets the current filters for the subscription.
		/// </summary>
		/// <param name="filters">The new filters to use for the subscription.</param>
		public void SetFilters(SubscriptionFilters filters)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_subscription == null) throw new NotConnectedException();

				// invoke COM method.
				try
				{
					((IOPCEventSubscriptionMgt)m_subscription).SetFilter(
						filters.EventTypes,
						filters.Categories.Count,
						filters.Categories.ToArray(),
						filters.LowSeverity,
						filters.HighSeverity,
						filters.Areas.Count,
						filters.Areas.ToArray(),
						filters.Sources.Count,
						filters.Sources.ToArray());
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventSubscriptionMgt.SetFilter", e);
				}		
			}
		}

		//======================================================================
		// Attribute Management

		/// <summary>
		/// Returns the set of attributes to return with event notifications.
		/// </summary>
		/// <returns>The set of attributes to returned with event notifications.</returns>
		public int[] GetReturnedAttributes(int eventCategory)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_subscription == null) throw new NotConnectedException();

				// initialize arguments.
				int pdwCount = 0;
				IntPtr ppdwAttributeIDs = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCEventSubscriptionMgt)m_subscription).GetReturnedAttributes(
						eventCategory,
						out pdwCount,
						out ppdwAttributeIDs);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventSubscriptionMgt.GetReturnedAttributes", e);
				}		

				// unmarshal results 
				int[] attributeIDs = OpcCom.Interop.GetInt32s(ref ppdwAttributeIDs, pdwCount, true);

				// return results.
				return attributeIDs;
			}
		}

		/// <summary>
		/// Selects the set of attributes to return with event notifications.
		/// </summary>
		/// <param name="eventCategory">The specific event category for which the attributes apply.</param>
		/// <param name="attributeIDs">The list of attribute ids to return.</param>
		public void SelectReturnedAttributes(int eventCategory, int[] attributeIDs)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_subscription == null) throw new NotConnectedException();

				// invoke COM method.
				try
				{
					((IOPCEventSubscriptionMgt)m_subscription).SelectReturnedAttributes(
						eventCategory,
						(attributeIDs != null)?attributeIDs.Length:0,
						(attributeIDs != null)?attributeIDs:new int[0]);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventSubscriptionMgt.SelectReturnedAttributes", e);
				}		
			}
		}

		//======================================================================
		// Refresh

		/// <summary>
		/// Force a refresh for all active conditions and inactive, unacknowledged conditions whose event notifications match the filter of the event subscription.
		/// </summary>
		public void Refresh()
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_subscription == null) throw new NotConnectedException();

				// invoke COM method.
				try
				{
                   ((IOPCEventSubscriptionMgt)m_subscription).Refresh(m_connection.Cookie); 
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventSubscriptionMgt.Refresh", e);
				}		
			}
		}

		/// <summary>
		/// Cancels an outstanding refresh request.
		/// </summary>
		public void CancelRefresh()
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_subscription == null) throw new NotConnectedException();

                // invoke COM method. 
                try 
                { 
                   ((IOPCEventSubscriptionMgt)m_subscription).CancelRefresh(m_connection.Cookie); 
                } 
                catch (Exception e) 
                { 
                   throw OpcCom.Interop.CreateException("IOPCEventSubscriptionMgt.Refresh", e); 
                }		
			}
		}
		#endregion

		#region IOPCEventSink Members
		/// <summary>
		/// A class that implements the IOPCEventSink interface.
		/// </summary>
		private class Callback : OpcRcw.Ae.IOPCEventSink
		{
			/// <summary>
			/// Initializes the object with the containing subscription object.
			/// </summary>
			public Callback(object clientHandle) 
			{ 
				m_clientHandle  = clientHandle;
			}
 
			/// <summary>
			/// Raised when data changed callbacks arrive.
			/// </summary>
			public event EventChangedEventHandler EventChanged
			{
				add    {lock (this){ m_EventChanged += value; }}
				remove {lock (this){ m_EventChanged -= value; }}
			}

			/// <summary>
			/// Called when a data changed event is received.
			/// </summary>
			public void OnEvent(
				int                       hClientSubscription,
				int                       bRefresh,
				int                       bLastRefresh,
				int                       dwCount,
				OpcRcw.Ae.ONEVENTSTRUCT[] pEvents)
			{
				try
				{
					lock (this)
					{
						// do nothing if no connections.
						if (m_EventChanged == null) return;

						// unmarshal item values.
						EventNotification[] notifications = Interop.GetEventNotifications(pEvents);

						for (int ii = 0; ii < notifications.Length; ii++)
						{
							notifications[ii].ClientHandle = m_clientHandle;
						}

						// invoke the callback.
						m_EventChanged(notifications, bRefresh != 0, bLastRefresh != 0);
					}
				}
				catch (Exception e) 
				{ 
					string stack = e.StackTrace;
				}
			}

			#region Private Members
			private object m_clientHandle = null;
			private event EventChangedEventHandler m_EventChanged = null;
			#endregion
		}
		#endregion

		#region Private Methods

		/// <summary>
		/// Establishes a connection point callback with the COM server.
		/// </summary>
		private void Advise()
		{
			if (m_connection == null)
			{
				m_connection = new ConnectionPoint(m_subscription, typeof(OpcRcw.Ae.IOPCEventSink).GUID);
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

		#region Private Members
		private object m_subscription = null;
		private object m_clientHandle = null;
		private bool m_supportsAE11 = true;
		private ConnectionPoint m_connection = null;
		private Callback m_callback = null;
		#endregion
	}
}
