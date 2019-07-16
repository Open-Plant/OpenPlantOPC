//============================================================================
// TITLE: Cache.cs
//
// CONTENTS:
// 
// An object that maintains a cache for a single XML-DA subscription.
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
	internal class Subscription
	{
		/// <summary>
		/// The unique handle assigned to the subscription.
		/// </summary>
		public string Handle
		{
			get { return m_handle; }
		}

		/// <summary>
		/// The ping time used for the subscription.
		/// </summary>
		public TimeSpan PingTime
		{
			get { return m_pingTime;  }
			set { m_pingTime = value; }
		}

		/// <summary>
		/// Initializes the cache with a table of servers, a handle and a ping time.
		/// </summary>
		public Subscription(string handle, TimeSpan pingTime)
		{
			m_handle       = handle;
			m_pingTime     = pingTime;
			m_lastPollTime = DateTime.MinValue;

			// use a default ping time of ten seconds. 
			if (m_pingTime == TimeSpan.Zero)
			{
				m_pingTime = new TimeSpan(0, 0, 10);
			}
		}

		private class CacheItem
		{
			public ItemIdentifier  ItemID           = null;
			public ItemValueResult LatestValue      = null;
			public bool            ReturnedToClient = false;
			public ArrayList       BufferedValues   = null;
		}

		/// <summary>
		/// Initializes the cache with the initial set of results. 
		/// </summary>
		public void Initialize(ItemResult[] items, SubscribeItemValueResult[] results)
		{
			m_items.Clear();

			for (int ii = 0; ii < items.Length; ii++)
			{
				if (items[ii].ResultID.Succeeded())
				{
					CacheItem item = new CacheItem();

					// save the remote client handle and the remote server handle.
					item.ItemID           = new ItemIdentifier(results[ii]);
					item.LatestValue      = new ItemValueResult(results[ii]);
					item.ReturnedToClient = true;
					item.BufferedValues   = null;

					// replace the local client handle.
					item.LatestValue.ClientHandle = item.ItemID.ClientHandle;

					// the item result object has the internal client handle.
					m_items[items[ii].ClientHandle] = item;
				}
			}
		}
		
		/// <summary>
		/// Checks if the subcription has expired.
		/// </summary>
		public bool HasExpired()
		{
			DateTime now = DateTime.Now;

            if (m_lastPollTime < now.Subtract(m_pingTime) && m_lastPollTime != DateTime.MinValue)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns the current set of item values.
		/// </summary>
		public ItemIdentifier[] GetItems()
		{
			ArrayList items = new ArrayList();
			
			IDictionaryEnumerator enumerator = m_items.GetEnumerator();

			while (enumerator.MoveNext())
			{
				ItemIdentifier itemID = new ItemIdentifier(((CacheItem)enumerator.Value).ItemID);
				itemID.ClientHandle = enumerator.Key;
				items.Add(itemID);
			}

			return (ItemIdentifier[])items.ToArray(typeof(ItemIdentifier));
		}

		/// <summary>
		/// Returns the current set of item values.
		/// </summary>
		public ItemValueResultList GetItemValues(bool returnAllItems)
		{
			ItemValueResultList results = new ItemValueResultList();

			foreach (CacheItem item in m_items.Values)
			{
				// skip old items.
				if (!returnAllItems && item.ReturnedToClient)
				{
					continue;
				}
				
				// add latest value.
				results.Add(item.LatestValue);
				ItemValueResult latestValue = item.LatestValue;

				// add any buffered values.
				if (item.BufferedValues != null)
				{
					foreach (ItemValueResult bufferedValue in item.BufferedValues)
					{
						latestValue = bufferedValue;
						results.Add(bufferedValue);
					}

					// clear the buffer.
					item.BufferedValues.Clear();
				}

				// flag item as returned to client.
				item.ReturnedToClient = true;

				// save the last value returned.
				item.LatestValue = new ItemValueResult(latestValue);
			}

			// update last poll time.
			m_lastPollTime = DateTime.Now;

			return results;
		}

		/// <summary>
		/// Called when data updates are received from the server.
		/// </summary>
		public void OnDataChanged(ItemValueResult[] values)
		{
			foreach (ItemValueResult value in values)
			{
				// skip invalid handles.
				if (value.ClientHandle == null)
				{
					continue;
				}

				// lookup item.
				CacheItem item = (CacheItem)m_items[value.ClientHandle];

				if (item == null)
				{
					continue;
				}

				// copy value
				ItemValueResult result = new ItemValueResult(value);

				// insertitem path and remote client handle.
				result.ItemPath     = item.ItemID.ItemPath;
				result.ClientHandle = item.ItemID.ClientHandle;

				// no previous value.
				if (item.ReturnedToClient)
				{
					item.ReturnedToClient = true;

					if (item.LatestValue.Quality != result.Quality || !Opc.Convert.Compare(item.LatestValue.Value, result.Value))
					{
						item.ReturnedToClient = false;
					}

					item.LatestValue = result;
				}

				// append to buffer.
				else
				{
					if (item.BufferedValues == null)
					{
						item.BufferedValues = new ArrayList();
					}

					item.BufferedValues.Add(result);
				}
			}
		}

		#region Private Members
		private string m_handle = null;
		private TimeSpan m_pingTime = TimeSpan.Zero;
		private DateTime m_lastPollTime = DateTime.MinValue;
		private Hashtable m_items = new Hashtable();
		#endregion
	}
}
