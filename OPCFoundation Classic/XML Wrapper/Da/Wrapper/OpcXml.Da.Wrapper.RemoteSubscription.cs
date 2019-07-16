//============================================================================
// TITLE: Server.cs
//
// CONTENTS:
// 
// An in-process wrapper for an XML-DA server. 
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
using System.Threading;
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Reflection;
using Opc;
using Opc.Da;

namespace OpcXml.Da.Wrapper
{
	/// <summary>
	/// Manages an item value cache for a subscription.
	/// </summary>
	internal class RemoteSubscription : IDisposable
	{
		/// <summary>
		/// Initializes the object.
		/// </summary>
		public RemoteSubscription() {}

		/// <summary>
		/// The item path which identifies the remote server which handles the subscription.
		/// </summary>
		public string ItemPath
		{
			get { lock(this) { return m_itemPath; }}
		}
		
		/// <summary>
		/// The update rate for the subscription.
		/// </summary>
		public int UpdateRate
		{
			get { lock(this) { return m_updateRate; }}
		}
		
		/// <summary>
		/// The nominal deadband for the subscription.
		/// </summary>
		public float Deadband
		{
			get { lock(this) { return m_deadband; }}
		}

		/// <summary>
		/// A event raised when a new data change update arrives from the remote server.
		/// </summary>
		public event DataChangedEventHandler DataChanged
		{
			add    
			{
				lock(this) 
				{ 
					m_dataChanged += value;

					if (!m_subscribed)
					{
						m_subscription.DataChanged += new DataChangedEventHandler(OnIncomingDataChanged);
						m_subscribed = true;
					}
				}
			}
			
			remove 
			{
				lock(this) 
				{ 
					m_dataChanged -= value; 
				}
			}
		}

		/// <summary>
		/// Initializes the object by creating the remote subscription. 
		/// </summary>
		public bool Initialize(
			string                     locale,
			Hashtable                  servers, 
			ItemList                   requestList, 
			SubscribeItemValueResult[] replyList,
			bool                       returnValues,
			ItemResult[]               results)
		{
			lock (this)
			{			
				// find first item that has not been assigned to a subscription.
				Item item  = null;
				int  index = -1;

				for (int ii = 0; ii < requestList.Count; ii++)
				{
					if (replyList[ii] == null)
					{
						item = (Item)requestList[ii];

						m_itemPath   = (item.ItemPath != null)?item.ItemPath:requestList.ItemPath;
						m_updateRate = (item.SamplingRateSpecified)?item.SamplingRate:requestList.SamplingRate;
						m_deadband   = (item.DeadbandSpecified)?item.Deadband:requestList.Deadband;

						index = ii;
						break;
					}
				}

				// no items found.
				if (item == null)
				{
					return false;
				}

				// look up the remote server.
				m_server = null;

				if (m_itemPath != null)
				{
					m_server = (Opc.Da.IServer)servers[m_itemPath];
				}

				// check for invalid item path. 
				if (m_server == null)
				{
					// create error result.
					results[index] = new ItemResult(item, ResultID.Da.E_UNKNOWN_ITEM_PATH);

					// create subscribe response.
					SubscribeItemValueResult result = new SubscribeItemValueResult(item);

					result.ItemPath = m_itemPath;
					result.ResultID = ResultID.Da.E_UNKNOWN_ITEM_PATH;

					replyList[index] = result;
					return false;
				}

				// create the subscription.
				try
				{
					SubscriptionState state = new SubscriptionState();

					state.Name       = null;
					state.Active     = true;
					state.UpdateRate = m_updateRate;
					state.Deadband   = m_deadband;
					state.Locale     = locale;
					state.KeepAlive  = 0;

					m_subscription = (Opc.Da.ISubscription)m_server.CreateSubscription(state);
				}
				catch (Exception e)
				{
					if (m_subscription != null)
					{
						m_subscription.Dispose();
						m_subscription = null;
					}

					m_server = null;

					throw e;
				}

				// add items.
				AddItems(requestList, replyList, returnValues, results);
				return true;
			}
		}

		/// <summary>
		/// Adds items to the subscription.
		/// </summary>
		public void AddItems(
			ItemList                   requestList, 
			SubscribeItemValueResult[] replyList,
			bool                       returnValues,
			ItemResult[]               additemResults)
		{
			lock (this)
			{		
				ArrayList indexes = new ArrayList();
				ArrayList items   = new ArrayList();

				for (int ii = 0; ii < requestList.Count; ii++)
				{
					Item item = new Item(requestList[ii]);

					// check item path.
					if ((item.ItemPath != null && item.ItemPath != ItemPath) || (item.ItemPath == null && requestList.ItemPath != ItemPath))
					{
						continue;
					}

					// check sampling rate.
					if ((item.SamplingRateSpecified && item.SamplingRate != UpdateRate) || (!item.SamplingRateSpecified && requestList.SamplingRate != UpdateRate))
					{
						continue;
					}

					// check deadband.
					if ((item.DeadbandSpecified && item.Deadband != Deadband) || (!item.DeadbandSpecified && requestList.Deadband != Deadband))
					{
						continue;
					}

					// assign globally unique client handle.
					item.ClientHandle = item.ItemName + Guid.NewGuid().ToString();

					if (item.ReqType == null && requestList.ReqType != null)
					{
						item.ReqType = requestList.ReqType;
					}

					// item can be handled by this subscription.
					indexes.Add(ii); 

					// copy the item and clear the item path.
					Item copy = new Item(item);
					copy.ItemPath = null;
					items.Add(copy);
				}

				// check if no suitable items found.
				if (indexes.Count == 0)
				{
					return;
				}

				// add items to subscription.
				ItemResult[] results = m_subscription.AddItems((Item[])items.ToArray(typeof(Item)));

				ItemValueResult[] values = null;

				if (returnValues)
				{
					values = m_subscription.Read(results);
				}

				// fill-in result list.
				for (int ii = 0; ii < items.Count; ii++)
				{
					int index = (int)indexes[ii];

					// return the add item result.
					additemResults[index] = results[ii];

					Item item = (Item)items[ii];

					SubscribeItemValueResult result = new SubscribeItemValueResult();

					result.ItemName              = item.ItemName;
					result.ItemPath              = ItemPath;
					result.ClientHandle          = requestList[index].ClientHandle;
					result.ServerHandle          = results[ii].ServerHandle;
					result.SamplingRate          = 0;
					result.SamplingRateSpecified = false;
					result.Value                 = null;
					result.Quality               = Quality.Bad;
					result.QualitySpecified      = false;
					result.Timestamp             = DateTime.MinValue;
					result.TimestampSpecified    = false;
					result.ResultID              = results[ii].ResultID;
					result.DiagnosticInfo        = results[ii].DiagnosticInfo;

					if (results[ii].SamplingRateSpecified && results[ii].SamplingRate != item.SamplingRate)
					{
						result.SamplingRate          = results[ii].SamplingRate;
						result.SamplingRateSpecified = results[ii].SamplingRateSpecified;
					}

					if (values != null)
					{
						if (result.ResultID.Succeeded())
						{
							result.Value                 = values[ii].Value;
							result.Quality               = values[ii].Quality;
							result.QualitySpecified      = values[ii].QualitySpecified;
							result.Timestamp             = values[ii].Timestamp;
							result.TimestampSpecified    = values[ii].TimestampSpecified;
							result.ResultID              = values[ii].ResultID;
							result.DiagnosticInfo        = values[ii].DiagnosticInfo;
						}
					}
					
					// return the value as part of the subscribe response.
					replyList[index] = result;

					// save server handle locally.
					if (result.ResultID.Succeeded())
					{
						m_items[(string)item.ClientHandle] = new ItemIdentifier(results[ii]);
					}
				}
			}
		}

		/// <summary>
		/// Removes items from the subscription.
		/// </summary>
		public bool RemoveItems(ItemIdentifier[] itemIDs)
		{
			lock (this)
			{
				// find items that apply to this subscription.
				ArrayList items = new ArrayList();

				foreach (ItemIdentifier itemID in itemIDs)
				{
					object item = m_items[(string)itemID.ClientHandle];

					if (item != null)
					{
						m_items.Remove((string)itemID.ClientHandle);
						items.Add(item);
					}
				}

				// remove items from remote subscription.
				if (items.Count > 0)
				{
					m_subscription.RemoveItems((ItemIdentifier[])items.ToArray(typeof(ItemIdentifier)));
				}

				// return false if no items left in subscription.
				return (m_items.Count > 0);
			}
		}

		#region IDisposable Members
		/// <summary>
		/// Disposes of the subscription and disposes all contained cache objects.
		/// </summary>
		public void Dispose()
		{
			lock (this)
			{
				if (m_subscription != null)
				{
					m_server.CancelSubscription(m_subscription);
				}

				m_server = null;
				m_subscription = null;
				m_items.Clear();
			}
		}
		#endregion

		#region Private Members
		private Opc.Da.IServer m_server = null;
		private Opc.Da.ISubscription m_subscription = null;
		private Hashtable m_items = new Hashtable();
		private string m_itemPath = null;
		private int m_updateRate = 0;
		private float m_deadband = 0;
		private DataChangedEventHandler m_dataChanged = null;
		private bool m_subscribed = false;

		/// <summary>
		/// Handles data changed events from the remote server.
		/// </summary>
		private void OnIncomingDataChanged(object subscriptionHandle, object requestHandle, ItemValueResult[] values)
		{
			try
			{
				if (m_dataChanged != null)
				{
					m_dataChanged(m_itemPath, requestHandle, values);
				}
			}
			catch
			{
				// do nothing on error.
			}
		}
		#endregion			
	}
}
