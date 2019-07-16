//============================================================================
// TITLE: Opc.Hda.Server.cs
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
// 2003/12/20 RSA   Initial implementation.

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace Opc.Hda
{
	/// <summary>
	/// An in-process object used to access OPC Data Access servers.
	/// </summary>
	[Serializable]
	public class Server : Opc.Server, Opc.Hda.IServer 
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
		// Public Properties		
		
		/// <summary>
		/// Returns a collection of item attributes supported by the server.
		/// </summary>
		public AttributeCollection Attributes 
		{
			get	{ return m_attributes; }
		}
		
		/// <summary>
		/// Returns a collection of aggregates supported by the server.
		/// </summary>
		public AggregateCollection Aggregates 
		{
			get	{ return m_aggregates; }
		}

		/// <summary>
		/// Returns a collection of items with server handles assigned to them.
		/// </summary>
		public ItemIdentifierCollection Items 
		{
			get	{ return new ItemIdentifierCollection(m_items.Values); }
		}

		/// <summary>
		/// Returns a collection of trends created for the server.
		/// </summary>
		public TrendCollection Trends 
		{
			get	{ return m_trends; }
		}

		//======================================================================
		// Connection Management	

		/// <summary>
		/// Connects to the server with the specified URL and credentials.
		/// </summary>
		public override void Connect(URL url, ConnectData connectData)
		{ 
			// connect to server.
			base.Connect(url, connectData);

			// fetch supported attributes.
			GetAttributes();

			// fetch supported aggregates.
			GetAggregates();

			// create items for trends.
			foreach (Trend trend in m_trends)
			{
				ArrayList itemIDs = new ArrayList();

				foreach (Item item in trend.Items)
				{
					itemIDs.Add(new ItemIdentifier(item));
				}

				// save server handles for each item.
				IdentifiedResult[] results = CreateItems((ItemIdentifier[])itemIDs.ToArray(typeof(ItemIdentifier)));

				if (results != null)
				{
					for (int ii = 0; ii < results.Length; ii++)
					{
						trend.Items[ii].ServerHandle = null;

						if (results[ii].ResultID.Succeeded())
						{
							trend.Items[ii].ServerHandle = results[ii].ServerHandle;
						}
					}
				}
			}
		}

		/// <summary>
		/// Disconnects from the server and releases all network resources.
		/// </summary>
		public override void Disconnect() 
		{
			if (m_server == null) throw new NotConnectedException();

			// dispose of all items first.
			if (m_items.Count > 0)
			{

				try
				{
					ArrayList items = new ArrayList(m_items.Count);
					items.AddRange(m_items);

					((IServer)m_server).ReleaseItems((ItemIdentifier[])items.ToArray(typeof(ItemIdentifier)));
				}
				catch
				{
					// ignore errors.
				}
				
				m_items.Clear();
			}

			// invalidate server handles for trends.
			foreach (Trend trend in m_trends)
			{
				foreach (Item item in trend.Items)
				{
					item.ServerHandle = null;
				}
			}

			// disconnect from server.
			base.Disconnect();
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
		// GetAttributes

		/// <summary>
		/// Returns the item attributes supported by the server.
		/// </summary>
		/// <returns>The a set of item attributes and their descriptions.</returns>
		public Attribute[] GetAttributes()
		{
			if (m_server == null) throw new NotConnectedException();

			// clear existing cached list.
			m_attributes.Clear();

			Attribute[] attributes = ((IServer)m_server).GetAttributes();

			// save a locale copy.
			if (attributes != null)
			{
				m_attributes.Init(attributes);
			}

			return attributes;
		}

		//======================================================================
		// GetAggregates

		/// <summary>
		/// Returns the aggregates supported by the server.
		/// </summary>
		/// <returns>The a set of aggregates and their descriptions.</returns>
		public Aggregate[] GetAggregates()
		{
			if (m_server == null) throw new NotConnectedException();

			// discard existing cached list.
			m_aggregates.Clear();

			Aggregate[] aggregates = ((IServer)m_server).GetAggregates();

			// save a locale copy.
			if (aggregates != null)
			{
				m_aggregates.Init(aggregates);
			}

			return aggregates;
		}

		//======================================================================
		// CreateBrowser

		/// <summary>
		/// Creates a object used to browse the server address space.
		/// </summary>
		/// <param name="filters">The set of attribute filters to use when browsing.</param>
		/// <param name="results">A result code for each individual filter.</param>
		/// <returns>A browser object that must be released by calling Dispose().</returns>
		public IBrowser CreateBrowser(BrowseFilter[] filters, out ResultID[] results)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).CreateBrowser(filters, out results);
		}

		//======================================================================
		// CreateItems

		/// <summary>
		/// Creates a set of items.
		/// </summary>
		/// <param name="items">The identifiers for the items to create.</param>
		/// <returns>The results for each item containing the server handle and result code.</returns>
		public IdentifiedResult[] CreateItems(ItemIdentifier[] items)
		{
			if (m_server == null) throw new NotConnectedException();

			IdentifiedResult[] results = ((IServer)m_server).CreateItems(items);

			// save items for future reference.
			if (results != null)
			{
				foreach (IdentifiedResult result in results)
				{
					if (result.ResultID.Succeeded())
					{
						m_items.Add(result.ServerHandle, new ItemIdentifier(result));
					}
				}
			}

			return results;
		}

		//======================================================================
		// ReleaseItems

		/// <summary>
		/// Releases a set of previously created items.
		/// </summary>
		/// <param name="items">The server handles for the items to release.</param>
		/// <returns>The results for each item containing the result code.</returns>
		public IdentifiedResult[] ReleaseItems(ItemIdentifier[] items)
		{
			if (m_server == null) throw new NotConnectedException();
			
			IdentifiedResult[] results = ((IServer)m_server).ReleaseItems(items);

			// remove items from local cache.
			if (results != null)
			{
				foreach (IdentifiedResult result in results)
				{
					if (result.ResultID.Succeeded())
					{
						m_items.Remove(result.ServerHandle);
					}
				}
			}

			return results;
		}

		//======================================================================
		// ValidateItems

		/// <summary>
		/// Validates a set of items.
		/// </summary>
		/// <param name="items">The identifiers for the items to validate.</param>
		/// <returns>The results for each item containing the result code.</returns>
		public IdentifiedResult[] ValidateItems(ItemIdentifier[] items)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).ValidateItems(items);
		}

		//======================================================================
		// ReadRaw

		/// <summary>
		/// Reads raw (unprocessed) data from the historian database for a set of items.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="endTime">The end of the history period to be read.</param>
		/// <param name="maxValues">The number of values to be read for each item.</param>
		/// <param name="includeBounds">Whether the bounding item values should be returned.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <returns>A set of values, qualities and timestamps within the requested time range for each item.</returns>
		public ItemValueCollection[] ReadRaw(
			Time             startTime,
			Time             endTime,
			int              maxValues,
			bool             includeBounds,
			ItemIdentifier[] items)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).ReadRaw(startTime, endTime, maxValues, includeBounds, items);
		}

		/// <summary>
		/// Sends an asynchronous request to read raw data from the historian database for a set of items.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="endTime">The end of the history period to be read.</param>
		/// <param name="maxValues">The number of values to be read for each item.</param>
		/// <param name="includeBounds">Whether the bounding item values should be returned.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] ReadRaw(
			Time                   startTime,
			Time                   endTime,
			int                    maxValues,
			bool                   includeBounds,
			ItemIdentifier[]       items,
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).ReadRaw(startTime, endTime, maxValues, includeBounds, items, requestHandle, callback, out request);
		}

		/// <summary>
		/// Requests that the server periodically send notifications when new data becomes available for a set of items.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="updateInterval">The frequency, in seconds, that the server should check for new data.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] AdviseRaw(
			Time                   startTime,
			decimal                updateInterval,
			ItemIdentifier[]       items,
			object                 requestHandle,
			DataUpdateEventHandler callback,
			out IRequest           request)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).AdviseRaw(startTime, updateInterval, items, requestHandle, callback, out request);
		}
		
		/// <summary>
		/// Begins the playback raw data from the historian database for a set of items.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="endTime">The end of the history period to be read.</param>
		/// <param name="maxValues">The number of values to be read for each item.</param>		
		/// <param name="updateInterval">The frequency, in seconds, that the server send data.</param>
		/// <param name="playbackDuration">The duration, in seconds, of the timespan returned with each update.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] PlaybackRaw(
			Time                   startTime,
			Time                   endTime,
			int                    maxValues,
			decimal                updateInterval,
			decimal                playbackDuration,
			ItemIdentifier[]       items,
			object                 requestHandle,
			DataUpdateEventHandler callback,
			out IRequest           request)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).PlaybackRaw(startTime, endTime, maxValues, updateInterval, playbackDuration, items, requestHandle, callback, out request);
		}

		//======================================================================
		// ReadProcessed

		/// <summary>
		/// Reads processed data from the historian database for a set of items.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="endTime">The end of the history period to be read.</param>
		/// <param name="resampleInterval">The interval between returned values.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <returns>A set of values, qualities and timestamps within the requested time range for each item.</returns>
		public ItemValueCollection[] ReadProcessed(
			Time    startTime,
			Time    endTime,
			decimal resampleInterval,
			Item[]  items)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).ReadProcessed(startTime, endTime, resampleInterval, items);
		}

		/// <summary>
		/// Sends an asynchronous request to read processed data from the historian database for a set of items.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="endTime">The end of the history period to be read.</param>
		/// <param name="resampleInterval">The interval between returned values.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] ReadProcessed(
			Time                     startTime,
			Time                     endTime,
			decimal                  resampleInterval,
			Item[]                   items,
			object                   requestHandle,
			ReadValuesEventHandler callback,
			out IRequest             request)
		{
			if (m_server == null) throw new NotConnectedException();

			IdentifiedResult[] results = ((IServer)m_server).ReadProcessed(
				startTime, 
				endTime,
				resampleInterval, 
				items, 
				requestHandle,
				callback,
				out request);

			return results;
		}

		/// <summary>
		/// Requests that the server periodically send notifications when new data becomes available for a set of items.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="resampleInterval">The interval between returned values.</param>
		/// <param name="numberOfIntervals">The number of resample intervals that the server should return in each callback.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] AdviseProcessed(
			Time                   startTime,
			decimal                resampleInterval,
			int                    numberOfIntervals,
			Item[]                 items,
			object                 requestHandle,
			DataUpdateEventHandler callback,
			out IRequest           request)
		{
			if (m_server == null) throw new NotConnectedException();

			IdentifiedResult[] results = ((IServer)m_server).AdviseProcessed(
				startTime, 
				resampleInterval, 
				numberOfIntervals,
				items, 
				requestHandle,
				callback,
				out request);

			return results;
		}
		
		/// <summary>
		/// Begins the playback of processed data from the historian database for a set of items.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="endTime">The end of the history period to be read.</param>
		/// <param name="resampleInterval">The interval between returned values.</param>
		/// <param name="numberOfIntervals">The number of resample intervals that the server should return in each callback.</param>
		/// <param name="updateInterval">The frequency, in seconds, that the server send data.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] PlaybackProcessed(
			Time                   startTime,
			Time                   endTime,
			decimal                resampleInterval,
			int                    numberOfIntervals,
			decimal                updateInterval,
			Item[]                 items,
			object                 requestHandle,
			DataUpdateEventHandler callback,
			out IRequest           request)
		{
			if (m_server == null) throw new NotConnectedException();

			IdentifiedResult[] results = ((IServer)m_server).PlaybackProcessed(
				startTime, 
				endTime, 
				resampleInterval, 
				numberOfIntervals,
				updateInterval,
				items, 
				requestHandle, 
				callback, 
				out request);

			return results;
		}

		//======================================================================
		// ReadAtTime

		/// <summary>
		/// Reads data from the historian database for a set of items at specific times.
		/// </summary>
		/// <param name="timestamps">The set of timestamps to use when reading items values.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <returns>A set of values, qualities and timestamps within the requested time range for each item.</returns>
		public ItemValueCollection[] ReadAtTime(DateTime[] timestamps, ItemIdentifier[] items)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).ReadAtTime(timestamps, items);
		}

		/// <summary>
		/// Sends an asynchronous request to read item values at specific times.
		/// </summary>
		/// <param name="timestamps">The set of timestamps to use when reading items values.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] ReadAtTime(
			DateTime[]             timestamps,
			ItemIdentifier[]       items,
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request)
		{
			if (m_server == null) throw new NotConnectedException();

			IdentifiedResult[] results = ((IServer)m_server).ReadAtTime(
				timestamps,
				items, 
				requestHandle, 
				callback, 
				out request);

			return results;
		}

		//======================================================================
		// ReadModified

		/// <summary>
		/// Reads item values that have been deleted or replaced.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="endTime">The end of the history period to be read.</param>
		/// <param name="maxValues">The number of values to be read for each item.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <returns>A set of values, qualities and timestamps within the requested time range for each item.</returns>
		public ModifiedValueCollection[] ReadModified(
			Time             startTime,
			Time             endTime,
			int              maxValues,
			ItemIdentifier[] items)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).ReadModified(startTime, endTime, maxValues, items);
		}

		/// <summary>
		/// Sends an asynchronous request to read item values that have been deleted or replaced.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="endTime">The end of the history period to be read.</param>
		/// <param name="maxValues">The number of values to be read for each item.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] ReadModified(
			Time                   startTime,
			Time                   endTime,
			int                    maxValues,
			ItemIdentifier[]       items,
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request)
		{
			if (m_server == null) throw new NotConnectedException();

			IdentifiedResult[] results = ((IServer)m_server).ReadModified(
				startTime,
				endTime,
				maxValues,
				items, 
				requestHandle, 
				callback, 
				out request);

			return results;
		}

		//======================================================================
		// ReadAttributes

		/// <summary>
		/// Reads the current or historical values for the attributes of an item.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="endTime">The end of the history period to be read.</param>
		/// <param name="item">The item to read (must include the server handle).</param>
		/// <param name="attributeIDs">The attributes to read.</param>
		/// <returns>A set of attribute values for each requested attribute.</returns>
		public ItemAttributeCollection ReadAttributes(
			Time             startTime,
			Time             endTime,
			ItemIdentifier   item,
			int[]            attributeIDs)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).ReadAttributes(startTime, endTime, item, attributeIDs);
		}

		/// <summary>
		/// Sends an asynchronous request to read the attributes of an item.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="endTime">The end of the history period to be read.</param>
		/// <param name="item">The item to read (must include the server handle).</param>
		/// <param name="attributeIDs">The attributes to read.</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the attribute ids.</returns>
		public ResultCollection ReadAttributes(
			Time                       startTime,
			Time                       endTime,
			ItemIdentifier             item,
			int[]                      attributeIDs,
			object                     requestHandle,
			ReadAttributesEventHandler callback,
			out IRequest               request)
		{
			if (m_server == null) throw new NotConnectedException();

			ResultCollection results = ((IServer)m_server).ReadAttributes(
				startTime,
				endTime,
				item, 
				attributeIDs,
				requestHandle, 
				callback, 
				out request);

			return results;
		}

		//======================================================================
		// ReadAnnotations

		/// <summary>
		/// Reads any annotations for an item within the a time interval.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="endTime">The end of the history period to be read.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <returns>A set of annotations within the requested time range for each item.</returns>
		public AnnotationValueCollection[] ReadAnnotations(
			Time             startTime,
			Time             endTime,
			ItemIdentifier[] items)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).ReadAnnotations(startTime, endTime, items);
		}
		
		/// <summary>
		/// Sends an asynchronous request to read the annotations for a set of items.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="endTime">The end of the history period to be read.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] ReadAnnotations(
			Time                        startTime,
			Time                        endTime,
			ItemIdentifier[]            items,
			object                      requestHandle,
			ReadAnnotationsEventHandler callback,
			out IRequest                request)
		{
			if (m_server == null) throw new NotConnectedException();

			IdentifiedResult[] results = ((IServer)m_server).ReadAnnotations(
				startTime,
				endTime,
				items, 
				requestHandle, 
				callback, 
				out request);

			return results;
		}

		//======================================================================
		// InsertAnnotations

		/// <summary>
		/// Inserts annotations for one or more items.
		/// </summary>
		/// <param name="items">A list of annotations to add for each item (must include the server handle).</param>
		/// <returns>The results of the insert operation for each annotation set.</returns>
		public ResultCollection[] InsertAnnotations(AnnotationValueCollection[] items)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).InsertAnnotations(items);
		}

		/// <summary>
		/// Sends an asynchronous request to inserts annotations for one or more items.
		/// </summary>
		/// <param name="items">A list of annotations to add for each item (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] InsertAnnotations(
			AnnotationValueCollection[] items,
			object                      requestHandle,
			UpdateCompleteEventHandler  callback,
			out IRequest                request)
		{
			if (m_server == null) throw new NotConnectedException();

			IdentifiedResult[] results = ((IServer)m_server).InsertAnnotations(
				items, 
				requestHandle, 
				callback, 
				out request);

			return results;
		}
		
		//======================================================================
		// Insert

		/// <summary>
		/// Inserts the values into the history database for one or more items. 
		/// </summary>
		/// <param name="items">The set of values to insert.</param>
		/// <param name="replace">Whether existing values should be replaced.</param>
		/// <returns></returns>
		public ResultCollection[] Insert(ItemValueCollection[] items, bool replace)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).Insert(items, replace);
		}
				
		/// <summary>
		/// Sends an asynchronous request to inserts values for one or more items.
		/// </summary>
		/// <param name="items">The set of values to insert.</param>
		/// <param name="replace">Whether existing values should be replaced.</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] Insert(
			ItemValueCollection[]      items,
			bool                       replace,
			object                     requestHandle,
			UpdateCompleteEventHandler callback,
			out IRequest               request)
		{
			if (m_server == null) throw new NotConnectedException();

			IdentifiedResult[] results = ((IServer)m_server).Insert(
				items, 
				replace,
				requestHandle, 
				callback, 
				out request);

			return results;
		}

		//======================================================================
		// Replace

		/// <summary>
		/// Replace the values into the history database for one or more items. 
		/// </summary>
		/// <param name="items">The set of values to replace.</param>
		/// <returns></returns>
		public ResultCollection[] Replace(ItemValueCollection[] items)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).Replace(items);
		}
				
		/// <summary>
		/// Sends an asynchronous request to replace values for one or more items.
		/// </summary>
		/// <param name="items">The set of values to replace.</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] Replace(
			ItemValueCollection[]      items,
			object                     requestHandle,
			UpdateCompleteEventHandler callback,
			out IRequest               request)
		{
			if (m_server == null) throw new NotConnectedException();

			IdentifiedResult[] results = ((IServer)m_server).Replace(
				items, 
				requestHandle, 
				callback, 
				out request);

			return results;
		}

		//======================================================================
		// Delete

		/// <summary>
		/// Deletes the values with the specified time domain for one or more items.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to delete.</param>
		/// <param name="endTime">The end of the history period to be delete.</param>
		/// <param name="items">The set of items to delete (must include the server handle).</param>
		/// <returns>The results of the delete operation for each item.</returns>
		public IdentifiedResult[] Delete(
			Time             startTime,
			Time             endTime,
			ItemIdentifier[] items)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).Delete(startTime, endTime, items);
		}

		/// <summary>
		/// Sends an asynchronous request to delete values for one or more items.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to delete.</param>
		/// <param name="endTime">The end of the history period to be delete.</param>
		/// <param name="items">The set of items to delete (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] Delete(
			Time                       startTime,
			Time                       endTime,
			ItemIdentifier[]           items,
			object                     requestHandle,
			UpdateCompleteEventHandler callback,
			out IRequest               request)
		{
			if (m_server == null) throw new NotConnectedException();

			IdentifiedResult[] results = ((IServer)m_server).Delete(
				startTime,
				endTime,
				items, 
				requestHandle, 
				callback, 
				out request);

			return results;
		}

		//======================================================================
		// DeleteAtTime

		/// <summary>
		/// Deletes the values at the specified times for one or more items. 
		/// </summary>
		/// <param name="items">The set of timestamps to delete for one or more items.</param>
		/// <returns>The results of the operation for each timestamp.</returns>
		public ResultCollection[] DeleteAtTime(ItemTimeCollection[] items)
		{
			if (m_server == null) throw new NotConnectedException();
			return ((IServer)m_server).DeleteAtTime(items);
		}

		/// <summary>
		/// Sends an asynchronous request to delete values for one or more items at a specified times.
		/// </summary>
		/// <param name="items">The set of timestamps to delete for one or more items.</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		public IdentifiedResult[] DeleteAtTime(
			ItemTimeCollection[]       items,
			object                     requestHandle,
			UpdateCompleteEventHandler callback,
			out IRequest               request)
		{
			if (m_server == null) throw new NotConnectedException();

			IdentifiedResult[] results = ((IServer)m_server).DeleteAtTime(
				items, 
				requestHandle, 
				callback, 
				out request);

			return results;
		}

		//======================================================================
		// CancelRequest

		/// <summary>
		/// Cancels an asynchronous request.
		/// </summary>
		/// <param name="request">The state object for the request to cancel.</param>
		public void CancelRequest(IRequest request)
		{
			if (m_server == null) throw new NotConnectedException();
			((IServer)m_server).CancelRequest(request);
		}
		
		/// <summary>
		/// Cancels an asynchronous request.
		/// </summary>
		/// <param name="request">The state object for the request to cancel.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		public void CancelRequest(IRequest request, CancelCompleteEventHandler callback)
		{
			if (m_server == null) throw new NotConnectedException();
			((IServer)m_server).CancelRequest(request, callback);
		}

		#region ISerializable Members
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string TRENDS = "Trends";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected Server(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			Trend[] trends = (Opc.Hda.Trend[])info.GetValue(Names.TRENDS, typeof(Opc.Hda.Trend[]));

			if (trends != null)
			{
				foreach (Trend trend in trends)
				{
					trend.SetServer(this);
					m_trends.Add(trend);
				}
			}
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			Trend[] trends = null;

			if (m_trends.Count > 0)
			{
				trends = new Trend[m_trends.Count];

				for (int ii = 0; ii < trends.Length; ii++)
				{
					trends[ii] = m_trends[ii];
				}
			}

			info.AddValue(Names.TRENDS, trends);
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Returns an unconnected copy of the server with the same URL. 
		/// </summary>
		public override object Clone()
		{
			// clone the base object.
			Server clone = (Server)base.Clone();
			
			// return clone.
			return clone;
		}
		#endregion

		#region Private Members
		private Hashtable m_items = new Hashtable();	
		private AttributeCollection m_attributes = new AttributeCollection();	
		private AggregateCollection m_aggregates = new AggregateCollection();	
		private TrendCollection m_trends = new TrendCollection();
		#endregion	
	}

	//=============================================================================
	// Asynchronous Delegates

	/// <summary>
	/// The asynchronous delegate for GetStatus.
	/// </summary>
	public delegate ServerStatus GetStatusDelegate();
}
