using System;
using System.Collections;
using System.Runtime.Serialization;

namespace Opc.Hda
{
	/// <summary>
	/// Manages a set of items and a set of read, update, subscribe or playback request parameters. 
	/// </summary>
	[Serializable]
	public class Trend : ISerializable, ICloneable
	{
		/// <summary>
		/// Initializes the object with the specified server.
		/// </summary>
		public Trend(Opc.Hda.Server server)
		{
			if (server == null) throw new ArgumentNullException("server");

			// save a reference to a server.
			m_server = server;

			// create a default name.
			do
			{
				Name = String.Format("Trend{0,2:00}", ++m_count);
			}
			while (m_server.Trends[Name] != null);
		}

		#region Public Properties
		/// <summary>
		/// The server containing the data in the trend.
		/// </summary>
		public Opc.Hda.Server Server
		{
			get { return m_server;  }
		}

		/// <summary>
		/// A name for the trend used to display to the user.
		/// </summary>
		public string Name
		{
			get { return m_name;  }
			set { m_name = value; }
		}

		/// <summary>
		/// The default aggregate to use for the trend.
		/// </summary>
		public int AggregateID
		{
			get { return m_aggregateID;  }
			set { m_aggregateID = value; }
		}

		/// <summary>
		/// The start time for the trend.
		/// </summary>
		public Opc.Hda.Time StartTime
		{
			get { return m_startTime;  }
			set { m_startTime = value; }
		}

		/// <summary>
		/// The end time for the trend.
		/// </summary>
		public Opc.Hda.Time EndTime
		{
			get { return m_endTime;  }
			set { m_endTime = value; }
		}

		/// <summary>
		/// The maximum number of data points per item in the trend.
		/// </summary>
		public int MaxValues
		{
			get { return m_maxValues;  }
			set { m_maxValues = value; }
		}

		/// <summary>
		/// Whether the trend includes the bounding values.
		/// </summary>
		public bool IncludeBounds
		{
			get { return m_includeBounds;  }
			set { m_includeBounds = value; }
		}

		/// <summary>
		/// The resampling interval (in seconds) to use for processed reads.
		/// </summary>
		public decimal ResampleInterval
		{
			get { return m_resampleInterval;  }
			set { m_resampleInterval = value; }
		}

		/// <summary>
		/// The discrete set of timestamps for the trend.
		/// </summary>
		public ItemTimeCollection Timestamps
		{
			get { return m_timestamps;  }

			set 
			{ 
				if (value == null) throw new ArgumentNullException("value");
                m_timestamps = value; 
			}
		}

		/// <summary>
		/// The interval between updates from the server when subscribing to new data.
		/// </summary>
		/// <remarks>This specifies a number of seconds for raw data or the number of resample intervals for processed data.</remarks>
		public decimal UpdateInterval
		{
			get { return m_updateInterval;  }
			set { m_updateInterval = value; }
		}

		/// <summary>
		/// Whether the server is currently sending updates for the trend.
		/// </summary>
		public bool SubscriptionActive
		{
			get { return m_subscription != null; }
		}

		/// <summary>
		/// The interval between updates from the server when playing back existing data. 
		/// </summary>
		/// <remarks>This specifies a number of seconds for raw data and for processed data.</remarks>
		public decimal PlaybackInterval
		{
			get { return m_playbackInterval;  }
			set { m_playbackInterval = value; }
		}

		/// <summary>
		/// The amount of data that should be returned with each update when playing back existing data.
		/// </summary>
		/// <remarks>This specifies a number of seconds for raw data or the number of resample intervals for processed data.</remarks>
		public decimal PlaybackDuration
		{
			get { return m_playbackDuration;  }
			set { m_playbackDuration = value; }
		}
	
		/// <summary>
		/// Whether the server is currently playing data back for the trend.
		/// </summary>
		public bool PlaybackActive
		{
			get { return m_playback != null; }
		}

		/// <summary>
		/// The items
		/// </summary>
		public Opc.Hda.ItemCollection Items
		{
			get { return m_items; }
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Returns the items in a trend as an array.
		/// </summary>
		public Item[] GetItems()
		{
			Item[] items = new Item[m_items.Count];

			for (int ii = 0; ii < m_items.Count; ii++)
			{
				items[ii] = m_items[ii];
			}

			return items;
		}
		
		/// <summary>
		/// Creates a handle for an item and adds it to the trend.
		/// </summary>
		public Item AddItem(ItemIdentifier itemID)
		{ 
			if (itemID == null) throw new ArgumentNullException("itemID");

			// assign client handle.
			if (itemID.ClientHandle == null)
			{
				itemID.ClientHandle = Guid.NewGuid().ToString();
			}

			// create server handle.
			IdentifiedResult[] results = m_server.CreateItems(new ItemIdentifier[] { itemID });

			// check for valid results.
			if (results == null || results.Length != 1)
			{				
				throw new InvalidResponseException();
			}

			// check result code.
			if (results[0].ResultID.Failed())
			{
				throw new ResultIDException(results[0].ResultID, "Could not add item to trend.");
			}

			// add new item.
			Item item = new Item(results[0]);
			m_items.Add(item);

			// return new item.
			return item;
		}

		/// <summary>
		/// Removes an item from the trend.
		/// </summary>
		public void RemoveItem(Item item)
		{ 
			if (item == null) throw new ArgumentNullException("item");

			for (int ii = 0; ii < m_items.Count; ii++)
			{
				if (item.Equals(m_items[ii]))
				{
					m_server.ReleaseItems(new ItemIdentifier[] { item });
					m_items.RemoveAt(ii);
					return;
				}
			}

			throw new ArgumentOutOfRangeException("item", item.Key, "Item not found in collection.");
		}

		/// <summary>
		/// Removes all items from the trend.
		/// </summary>
		public void ClearItems()
		{
			m_server.ReleaseItems(GetItems());
			m_items.Clear();
		}

		//======================================================================
		// Read

		/// <summary>
		/// Reads the values for a for all items in the trend.
		/// </summary>
		public ItemValueCollection[] Read()
		{		
			return Read(GetItems());
		}

		/// <summary>
		/// Reads the values for a for a set of items. 
		/// </summary>
		public ItemValueCollection[] Read(Item[] items)
		{
			// read raw data.
			if (AggregateID == Opc.Hda.AggregateID.NOAGGREGATE)
			{
				return ReadRaw(items);
			}

			// read processed data.
			else
			{
				return ReadProcessed(items);
			}
		}

		/// <summary>
		/// Starts an asynchronous read request for all items in the trend. 
		/// </summary>
		public IdentifiedResult[] Read(
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request)
		{		
			return Read(GetItems(), requestHandle, callback, out request);
		}

		/// <summary>
		/// Starts an asynchronous read request for a set of items. 
		/// </summary>
		public IdentifiedResult[] Read(
			Item[]                 items,
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request)
		{
			// read raw data.
			if (AggregateID == Opc.Hda.AggregateID.NOAGGREGATE)
			{
				return ReadRaw(items, requestHandle, callback, out request);
			}

			// read processed data.
			else
			{
				return ReadProcessed(items, requestHandle, callback, out request);
			}
		}

		//======================================================================
		// ReadRaw

		/// <summary>
		/// Reads the raw values for a for all items in the trend.
		/// </summary>
		public ItemValueCollection[] ReadRaw()
		{		
			return ReadRaw(GetItems());
		}

		/// <summary>
		/// Reads the raw values for a for a set of items. 
		/// </summary>
		public ItemValueCollection[] ReadRaw(Item[] items)
		{
			ItemValueCollection[] results = m_server.ReadRaw(
				StartTime,
				EndTime,
				MaxValues,
				IncludeBounds,
				items);

			return results;
		}

		/// <summary>
		/// Starts an asynchronous read raw request for all items in the trend. 
		/// </summary>
		public IdentifiedResult[] ReadRaw(
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request)
		{		
			return Read(GetItems(), requestHandle, callback, out request);
		}

		/// <summary>
		/// Starts an asynchronous read raw request for a set of items. 
		/// </summary>
		public IdentifiedResult[] ReadRaw(
			ItemIdentifier[]       items,
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request)
		{
			IdentifiedResult[] results = m_server.ReadRaw(
				StartTime,
				EndTime,
				MaxValues,
				IncludeBounds,
				items,
				requestHandle,
				callback,
				out request);
			
			return results;
		}

		//======================================================================
		// ReadProcessed

		/// <summary>
		/// Reads the processed values for a for all items in the trend.
		/// </summary>
		public ItemValueCollection[] ReadProcessed()
		{		
			return ReadProcessed(GetItems());
		}

		/// <summary>
		/// Reads the processed values for a for a set of items. 
		/// </summary>
		public ItemValueCollection[] ReadProcessed(Item[] items)
		{
			Item[] localItems = ApplyDefaultAggregate(items);

			ItemValueCollection[] results = m_server.ReadProcessed(
				StartTime,
				EndTime,
				ResampleInterval,
				localItems);

			return results;
		}

		/// <summary>
		/// Starts an asynchronous read processed request for all items in the trend. 
		/// </summary>
		public IdentifiedResult[] ReadProcessed(
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request)
		{		
			return ReadProcessed(GetItems(), requestHandle, callback, out request);
		}

		/// <summary>
		/// Starts an asynchronous read processed request for a set of items. 
		/// </summary>
		public IdentifiedResult[] ReadProcessed(
			Item[]                 items,
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request)
		{
			Item[] localItems = ApplyDefaultAggregate(items);

			IdentifiedResult[] results = m_server.ReadProcessed(
				StartTime,
				EndTime,
				ResampleInterval,
				localItems,
				requestHandle,
				callback,
				out request);

			return results;
		}

		//======================================================================
		// Subscribe

		/// <summary>
		/// Establishes a subscription for the trend.
		/// </summary>
		public IdentifiedResult[] Subscribe(
			object                 subscriptionHandle,
			DataUpdateEventHandler callback)
		{
			IdentifiedResult[] results = null;

			// subscribe to raw data.
			if (AggregateID == Opc.Hda.AggregateID.NOAGGREGATE)
			{
				results = m_server.AdviseRaw(
					StartTime,
					UpdateInterval,
					GetItems(),
					subscriptionHandle,
					callback,
					out m_subscription);
			}

			// subscribe processed data.
			else
			{
				Item[] localItems = ApplyDefaultAggregate(GetItems());

				results = m_server.AdviseProcessed(
					StartTime,
					ResampleInterval,
					(int)UpdateInterval,
					localItems,
					subscriptionHandle,
					callback,
					out m_subscription);
			}

			return results;
		}

		/// <summary>
		/// Cancels an existing subscription.
		/// </summary>
		public void SubscribeCancel()
		{
			if (m_subscription != null)
			{
				m_server.CancelRequest(m_subscription);
				m_subscription = null;
			}
		}

		//======================================================================
		// Playback

		/// <summary>
		/// Begins playback of data for a trend.
		/// </summary>
		public IdentifiedResult[] Playback(
			object                 playbackHandle,
			DataUpdateEventHandler callback)
		{
			IdentifiedResult[] results = null;

			// playback raw data.
			if (AggregateID == Opc.Hda.AggregateID.NOAGGREGATE)
			{
				results = m_server.PlaybackRaw(
					StartTime,
					EndTime,
					MaxValues,
					PlaybackInterval,
					PlaybackDuration,
					GetItems(),
					playbackHandle,
					callback,
					out m_playback);
			}

			// playback processed data.
			else
			{
				Item[] localItems = ApplyDefaultAggregate(GetItems());

				results = m_server.PlaybackProcessed(
					StartTime,
					EndTime,
					ResampleInterval,
					(int)PlaybackDuration,
					PlaybackInterval,
					localItems,
					playbackHandle,
					callback,
					out m_playback);
			}

			return results;
		}

		/// <summary>
		/// Cancels an existing playback operation.
		/// </summary>
		public void PlaybackCancel()
		{
			if (m_playback != null)
			{
				m_server.CancelRequest(m_playback);
				m_playback = null;
			}
		}

		//======================================================================
		// ReadModified

		/// <summary>
		/// Reads the modified values for all items in the trend.
		/// </summary>
		public ModifiedValueCollection[] ReadModified()
		{		
			return ReadModified(GetItems());
		}

		/// <summary>
		/// Reads the modified values for a for a set of items. 
		/// </summary>
		public ModifiedValueCollection[] ReadModified(Item[] items)
		{
			ModifiedValueCollection[] results = m_server.ReadModified(
				StartTime,
				EndTime,
				MaxValues,
				items);

			return results;
		}
		
		/// <summary>
		/// Starts an asynchronous read modified request for all items in the trend.
		/// </summary>
		public IdentifiedResult[] ReadModified(
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request)
		{
			return ReadModified(GetItems(), requestHandle, callback, out request);
		}

		/// <summary>
		/// Starts an asynchronous read modified request for a set of items. 
		/// </summary>
		public IdentifiedResult[] ReadModified(
			Item[]                 items,
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request)
		{
			IdentifiedResult[] results = m_server.ReadModified(
				StartTime,
				EndTime,
				MaxValues,
				items,
				requestHandle,
				callback,
				out request);

			return results;
		}

		//======================================================================
		// ReadAtTime

		/// <summary>
		/// Reads the values at specific times for a for all items in the trend.
		/// </summary>
		public ItemValueCollection[] ReadAtTime()
		{		
			return ReadAtTime(GetItems());
		}

		/// <summary>
		/// Reads the values at specific times for a for a set of items. 
		/// </summary>
		public ItemValueCollection[] ReadAtTime(Item[] items)
		{
			DateTime[] timestamps = new DateTime[Timestamps.Count];

			for (int ii = 0; ii < Timestamps.Count; ii++)
			{
				timestamps[ii] = Timestamps[ii];
			}

			return m_server.ReadAtTime(timestamps,	items);
		}
		
		/// <summary>
		/// Starts an asynchronous read values at specific times request for all items in the trend. 
		/// </summary>
		public IdentifiedResult[] ReadAtTime(
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request)
		{		
			return ReadAtTime(GetItems(), requestHandle, callback, out request);
		}

		/// <summary>
		/// Starts an asynchronous read values at specific times request for a set of items.
		/// </summary>
		public IdentifiedResult[] ReadAtTime(
			Item[]                 items,
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request)
		{		
			DateTime[] timestamps = new DateTime[Timestamps.Count];

			for (int ii = 0; ii < Timestamps.Count; ii++)
			{
				timestamps[ii] = Timestamps[ii];
			}

			return m_server.ReadAtTime(timestamps, items, requestHandle, callback, out request);
		}

		//======================================================================
		// ReadAttributes

		/// <summary>
		/// Reads the attributes at specific times for a for an item. 
		/// </summary>
		public ItemAttributeCollection ReadAttributes(ItemIdentifier item, int[] attributeIDs)
		{
			return m_server.ReadAttributes(StartTime, EndTime, item, attributeIDs);
		}
		
		/// <summary>
		/// Starts an asynchronous read attributes at specific times request for an item. 
		/// </summary>
		public ResultCollection ReadAttributes(
			ItemIdentifier             item,
			int[]                      attributeIDs,
			object                     requestHandle,
			ReadAttributesEventHandler callback,
			out IRequest               request)
		{		
			ResultCollection results = m_server.ReadAttributes(
				StartTime,
				EndTime,
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
		/// Reads the annotations for a for all items in the trend.
		/// </summary>
		public AnnotationValueCollection[] ReadAnnotations()
		{		
			return ReadAnnotations(GetItems());
		}

		/// <summary>
		/// Reads the annotations for a for a set of items. 
		/// </summary>
		public AnnotationValueCollection[] ReadAnnotations(Item[] items)
		{
			AnnotationValueCollection[] results = m_server.ReadAnnotations(
				StartTime,
				EndTime,
				items);

			return results;
		}

		/// <summary>
		/// Starts an asynchronous read annotations request for all items in the trend.
		/// </summary>
		public IdentifiedResult[] ReadAnnotations(
			object                      requestHandle,
			ReadAnnotationsEventHandler callback,
			out IRequest                request)
		{
			return ReadAnnotations(GetItems(), requestHandle, callback, out request);
		}

		/// <summary>
		/// Starts an asynchronous read annotations request for a set of items. 
		/// </summary>
		public IdentifiedResult[] ReadAnnotations(
			Item[]                      items,
			object                      requestHandle,
			ReadAnnotationsEventHandler callback,
			out IRequest                request)
		{
			IdentifiedResult[] results = m_server.ReadAnnotations(
				StartTime,
				EndTime,
				items,
				requestHandle,
				callback,
				out request);

			return results;
		}

		//======================================================================
		// Delete

		/// <summary>
		/// Deletes the raw values for a for all items in the trend.
		/// </summary>
		public IdentifiedResult[] Delete()
		{		
			return Delete(GetItems());
		}

		/// <summary>
		/// Deletes the raw values for a for a set of items. 
		/// </summary>
		public IdentifiedResult[] Delete(Item[] items)
		{
			IdentifiedResult[] results = m_server.Delete(
				StartTime,
				EndTime,
				items);

			return results;
		}

		/// <summary>
		/// Starts an asynchronous delete raw request for all items in the trend. 
		/// </summary>
		public IdentifiedResult[] Delete(
			object                     requestHandle,
			UpdateCompleteEventHandler callback,
			out IRequest               request)
		{		
			return Delete(GetItems(), requestHandle, callback, out request);
		}

		/// <summary>
		/// Starts an asynchronous delete raw request for a set of items. 
		/// </summary>
		public IdentifiedResult[] Delete(
			ItemIdentifier[]           items,
			object                     requestHandle,
			UpdateCompleteEventHandler callback,
			out IRequest               request)
		{ 
			IdentifiedResult[] results = m_server.Delete(
				StartTime,
				EndTime,
				items,
				requestHandle,
				callback,
				out request);
			
			return results;
		}

		//======================================================================
		// DeleteAtTime

		/// <summary>
		/// Deletes the values at specific times for a for all items in the trend.
		/// </summary>
		public ResultCollection[] DeleteAtTime()
		{		
			return DeleteAtTime(GetItems());
		}

		/// <summary>
		/// Deletes the values at specific times for a for a set of items. 
		/// </summary>
		public ResultCollection[] DeleteAtTime(Item[] items)
		{
			ItemTimeCollection[] times = new ItemTimeCollection[items.Length];

			for (int ii = 0; ii < items.Length; ii++)
			{
				times[ii] = (ItemTimeCollection)Timestamps.Clone();

				times[ii].ItemName     = items[ii].ItemName;
				times[ii].ItemPath     = items[ii].ItemPath;
				times[ii].ClientHandle = items[ii].ClientHandle;
				times[ii].ServerHandle = items[ii].ServerHandle;
			}

			return m_server.DeleteAtTime(times);
		}
		
		/// <summary>
		/// Starts an asynchronous delete values at specific times request for all items in the trend. 
		/// </summary>
		public IdentifiedResult[] DeleteAtTime(
			object                     requestHandle,
			UpdateCompleteEventHandler callback,
			out IRequest               request)
		{		
			return DeleteAtTime(GetItems(), requestHandle, callback, out request);
		}

		/// <summary>
		/// Starts an asynchronous delete values at specific times request for a set of items.
		/// </summary>
		public IdentifiedResult[] DeleteAtTime(
			Item[]                     items,
			object                     requestHandle,
			UpdateCompleteEventHandler callback,
			out IRequest               request)
		{		
			ItemTimeCollection[] times = new ItemTimeCollection[items.Length];

			for (int ii = 0; ii < items.Length; ii++)
			{
				times[ii] = (ItemTimeCollection)Timestamps.Clone();

				times[ii].ItemName     = items[ii].ItemName;
				times[ii].ItemPath     = items[ii].ItemPath;
				times[ii].ClientHandle = items[ii].ClientHandle;
				times[ii].ServerHandle = items[ii].ServerHandle;
			}

			return m_server.DeleteAtTime(times, requestHandle, callback, out request);
		}
		#endregion

		#region ISerializable Members
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string NAME              = "Name";
			internal const string AGGREGATE_ID      = "AggregateID";
			internal const string START_TIME        = "StartTime";
			internal const string END_TIME          = "EndTime";
			internal const string MAX_VALUES        = "MaxValues";
			internal const string INCLUDE_BOUNDS    = "IncludeBounds";
			internal const string RESAMPLE_INTERVAL = "ResampleInterval";
			internal const string UPDATE_INTERVAL   = "UpdateInterval";
			internal const string PLAYBACK_INTERVAL = "PlaybackInterval";
			internal const string PLAYBACK_DURATION = "PlaybackDuration";
			internal const string TIMESTAMPS        = "Timestamps";
			internal const string ITEMS             = "Items";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected Trend(SerializationInfo info, StreamingContext context)
		{
			// deserialize basic parameters.
			m_name             = (string)info.GetValue(Names.NAME, typeof(string));
			m_aggregateID      = (int)info.GetValue(Names.AGGREGATE_ID, typeof(int));
			m_startTime        = (Opc.Hda.Time)info.GetValue(Names.START_TIME, typeof(Opc.Hda.Time));
			m_endTime          = (Opc.Hda.Time)info.GetValue(Names.END_TIME, typeof(Opc.Hda.Time)); 
			m_maxValues        = (int)info.GetValue(Names.MAX_VALUES, typeof(int));
			m_includeBounds    = (bool)info.GetValue(Names.INCLUDE_BOUNDS, typeof(bool));
			m_resampleInterval = (decimal)info.GetValue(Names.RESAMPLE_INTERVAL, typeof(decimal));
			m_updateInterval   = (decimal)info.GetValue(Names.UPDATE_INTERVAL, typeof(decimal));
			m_playbackInterval = (decimal)info.GetValue(Names.PLAYBACK_INTERVAL, typeof(decimal));
			m_playbackDuration = (decimal)info.GetValue(Names.PLAYBACK_DURATION, typeof(decimal));

			// deserialize timestamps.
			DateTime[] timestamps = (DateTime[])info.GetValue(Names.TIMESTAMPS, typeof(DateTime[]));

			if (timestamps != null)
			{
				foreach (DateTime timestamp in timestamps)
				{
					m_timestamps.Add(timestamp);
				}
			}

			// deserialize items.
			Item[] items = (Opc.Hda.Item[])info.GetValue(Names.ITEMS, typeof(Opc.Hda.Item[]));

			if (items != null)
			{
				foreach (Item item in items)
				{
					m_items.Add(item);
				}
			}
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			// serialize basic parameters.
			info.AddValue(Names.NAME, m_name);
			info.AddValue(Names.AGGREGATE_ID, m_aggregateID);
			info.AddValue(Names.START_TIME, m_startTime);
			info.AddValue(Names.END_TIME, m_endTime);
			info.AddValue(Names.MAX_VALUES, m_maxValues);
			info.AddValue(Names.INCLUDE_BOUNDS, m_includeBounds);
			info.AddValue(Names.RESAMPLE_INTERVAL, m_resampleInterval);
			info.AddValue(Names.UPDATE_INTERVAL, m_updateInterval);
			info.AddValue(Names.PLAYBACK_INTERVAL, m_playbackInterval);
			info.AddValue(Names.PLAYBACK_DURATION, m_playbackDuration);

			// serialize timestamps.
			DateTime[] timestamps = null;

			if (m_timestamps.Count > 0)
			{
				timestamps = new DateTime[m_timestamps.Count];

				for (int ii = 0; ii < timestamps.Length; ii++)
				{
					timestamps[ii] = m_timestamps[ii];
				}
			}

			info.AddValue(Names.TIMESTAMPS, timestamps);

			// serialize items.
			Item[] items = null;

			if (m_items.Count > 0)
			{
				items = new Item[m_items.Count];

				for (int ii = 0; ii < items.Length; ii++)
				{
					items[ii] = m_items[ii];
				}
			}

			info.AddValue(Names.ITEMS, items);
		}

		/// <summary>
		/// Used to set the server after the object is deserialized.
		/// </summary>
		internal void SetServer(Opc.Hda.Server server)
		{
			m_server = server;
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone()
		{
			// clone simple properies.
			Trend clone = (Trend)MemberwiseClone();

			// clone items.
			clone.m_items = new ItemCollection();

			foreach (Item item in m_items)
			{
				clone.m_items.Add(item.Clone());
			}
			
			// clone timestamps.
			clone.m_timestamps = new ItemTimeCollection();

			foreach (DateTime timestamp in m_timestamps)
			{
				clone.m_timestamps.Add(timestamp);
			}

			// clear dynamic state information.
			clone.m_subscription = null;
			clone.m_playback     = null;

			return clone;
		}
		#endregion

		#region Private Members
		/// <summary>
		/// Creates a copy of the items that have a valid aggregate set.
		/// </summary>
		private Item[] ApplyDefaultAggregate(Item[] items)
		{
			// use interpolative aggregate if none specified for the trend.
			int defaultID = AggregateID;
			
			if (defaultID == Opc.Hda.AggregateID.NOAGGREGATE)
			{
				defaultID = Opc.Hda.AggregateID.INTERPOLATIVE;
			}

			// apply default aggregate to items that have no aggregate specified.
			Item[] localItems = new Item[items.Length];

			for (int ii = 0; ii < items.Length; ii++)
			{
				localItems[ii] = new Item(items[ii]);

				if (localItems[ii].AggregateID == Opc.Hda.AggregateID.NOAGGREGATE)
				{
					localItems[ii].AggregateID = defaultID;
				}
			}

			// return updated items.
			return localItems;
		}

		private static int m_count = 0;
		
		private Opc.Hda.Server m_server = null;

		private string m_name = null;
		private int m_aggregateID = Opc.Hda.AggregateID.NOAGGREGATE; 
		private Opc.Hda.Time m_startTime = null;  
		private Opc.Hda.Time m_endTime = null;  
		private int m_maxValues = 0;
		private bool m_includeBounds = false;
		private decimal m_resampleInterval = 0;
		private ItemTimeCollection m_timestamps = new ItemTimeCollection();
		private ItemCollection m_items = new ItemCollection();
		private decimal m_updateInterval = 0;
		private decimal m_playbackInterval = 0;
		private decimal m_playbackDuration = 0;

		private IRequest m_subscription = null;
		private IRequest m_playback = null;
		#endregion
	}


	/// <summary>
	/// A collection of items.
	/// </summary>
	[Serializable]
	public class TrendCollection : ICollection, ICloneable, IList
	{
		/// <summary>
		/// Gets the trend at the specified index.
		/// </summary>
		public Trend this[int index]
		{
			get { return (Trend)m_trends[index]; }
		}
	
		/// <summary>
		/// Gets the first trend with the specified name.
		/// </summary>
		public Trend this[string name]
		{
			get 
			{
				foreach (Trend trend in m_trends)
				{
					if (trend.Name == name)
					{
						return trend;
					}
				}

				return null;
			}
		}	

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		public TrendCollection() {}

		/// <summary>
		/// Initializes object with the specified TrendValueCollection object.
		/// </summary>
		public TrendCollection(TrendCollection items)
		{
			if (items != null)
			{
				foreach (Trend item in items)
				{
					Add(item);
				}
			}
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone()
		{
			TrendCollection clone = (TrendCollection)MemberwiseClone();

			clone.m_trends = new ArrayList();

			foreach (Trend trend in m_trends)
			{
				clone.m_trends.Add(trend.Clone());
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
			get { return (m_trends != null)?m_trends.Count:0; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_trends != null)
			{
				m_trends.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Trend[] array, int index)
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
			return m_trends.GetEnumerator();
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
			get	{ return m_trends[index];  }
			
			set	
			{ 
				if (!typeof(Trend).IsInstanceOfType(value))
				{
					throw new ArgumentException("May only add Trend objects into the collection.");
				}
				
				m_trends[index] = value; 
			}
		}
        
		/// <summary>
		/// Removes the IList item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index)
		{
			m_trends.RemoveAt(index);
		}

		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, object value)
		{
			if (!typeof(Trend).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add Trend objects into the collection.");
			}

			m_trends.Insert(index, value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(object value)
		{
			m_trends.Remove(value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(object value)
		{
			return m_trends.Contains(value);
		}

		/// <summary>
		/// Removes all items from the IList.
		/// </summary>
		public void Clear()
		{
			m_trends.Clear();
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(object value)
		{
			return m_trends.IndexOf(value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(object value)
		{
			if (!typeof(Trend).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add Trend objects into the collection.");
			}

			return m_trends.Add(value);
		}

		/// <summary>
		/// Indicates whether the IList has a fixed size.
		/// </summary>
		public bool IsFixedSize
		{
			get	{ return false; }
		}
		
		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, Trend value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(Trend value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(Trend value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(Trend value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(Trend value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Members
		private ArrayList m_trends = new ArrayList();
		#endregion
	}

}
