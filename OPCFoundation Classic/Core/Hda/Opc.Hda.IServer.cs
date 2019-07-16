//============================================================================
// TITLE: Opc.Hda.IServer.cs
//
// CONTENTS:
// 
// The primary interface for a Historical Data Access server.
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

namespace Opc.Hda
{
	/// <summary>
	/// Defines functionality that is common to all OPC Data Access servers.
	/// </summary>
	public interface IServer : Opc.IServer
	{
		//======================================================================
		// GetStatus

		/// <summary>
		/// Returns the current server status.
		/// </summary>
		/// <returns>The current server status.</returns>
		ServerStatus GetStatus();

		//======================================================================
		// GetAttributes

		/// <summary>
		/// Returns the item attributes supported by the server.
		/// </summary>
		/// <returns>The a set of item attributes and their descriptions.</returns>
		Attribute[] GetAttributes();

		//======================================================================
		// GetAggregates

		/// <summary>
		/// Returns the aggregates supported by the server.
		/// </summary>
		/// <returns>The a set of aggregates and their descriptions.</returns>
		Aggregate[] GetAggregates();

		//======================================================================
		// CreateBrowser

		/// <summary>
		/// Creates a object used to browse the server address space.
		/// </summary>
		/// <param name="filters">The set of attribute filters to use when browsing.</param>
		/// <param name="results">A result code for each individual filter.</param>
		/// <returns>A browser object that must be released by calling Dispose().</returns>
		IBrowser CreateBrowser(BrowseFilter[] filters, out ResultID[] results);

		//======================================================================
		// CreateItems

		/// <summary>
		/// Creates a set of items.
		/// </summary>
		/// <param name="items">The identifiers for the items to create.</param>
		/// <returns>The results for each item containing the server handle and result code.</returns>
		IdentifiedResult[] CreateItems(ItemIdentifier[] items);

		//======================================================================
		// ReleaseItems

		/// <summary>
		/// Releases a set of previously created items.
		/// </summary>
		/// <param name="items">The server handles for the items to release.</param>
		/// <returns>The results for each item containing the result code.</returns>
		IdentifiedResult[] ReleaseItems(ItemIdentifier[] items);

		//======================================================================
		// ValidateItems

		/// <summary>
		/// Validates a set of items.
		/// </summary>
		/// <param name="items">The identifiers for the items to validate.</param>
		/// <returns>The results for each item containing the result code.</returns>
		IdentifiedResult[] ValidateItems(ItemIdentifier[] items);
		
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
		ItemValueCollection[] ReadRaw(
			Time             startTime,
			Time             endTime,
			int              maxValues,
			bool             includeBounds,
			ItemIdentifier[] items);

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
		IdentifiedResult[] ReadRaw(
			Time                     startTime,
			Time                     endTime,
			int                      maxValues,
			bool                     includeBounds,
			ItemIdentifier[]         items,
			object                   requestHandle,
			ReadValuesEventHandler callback,
			out IRequest             request);

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
		IdentifiedResult[] AdviseRaw(
			Time                   startTime,
			decimal                updateInterval,
			ItemIdentifier[]       items,
			object                 requestHandle,
			DataUpdateEventHandler callback,
			out IRequest           request);
		
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
		IdentifiedResult[] PlaybackRaw(
			Time                   startTime,
			Time                   endTime,
			int                    maxValues,
			decimal                updateInterval,
			decimal                playbackDuration,
			ItemIdentifier[]       items,
			object                 requestHandle,
			DataUpdateEventHandler callback,
			out IRequest           request);

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
		ItemValueCollection[] ReadProcessed(
			Time    startTime,
			Time    endTime,
			decimal resampleInterval,
			Item[]  items);

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
		IdentifiedResult[] ReadProcessed(
			Time                   startTime,
			Time                   endTime,
			decimal                resampleInterval,
			Item[]                 items,
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request);

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
		IdentifiedResult[] AdviseProcessed(
			Time                   startTime,
			decimal                resampleInterval,
			int                    numberOfIntervals,
			Item[]                 items,
			object                 requestHandle,
			DataUpdateEventHandler callback,
			out IRequest           request);
		
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
		IdentifiedResult[] PlaybackProcessed(
			Time                   startTime,
			Time                   endTime,
			decimal                resampleInterval,
			int                    numberOfIntervals,
			decimal                updateInterval,
			Item[]                 items,
			object                 requestHandle,
			DataUpdateEventHandler callback,
			out IRequest           request);

		//======================================================================
		// ReadAtTime

		/// <summary>
		/// Reads data from the historian database for a set of items at specific times.
		/// </summary>
		/// <param name="timestamps">The set of timestamps to use when reading items values.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <returns>A set of values, qualities and timestamps within the requested time range for each item.</returns>
		ItemValueCollection[] ReadAtTime(DateTime[] timestamps, ItemIdentifier[] items);

		/// <summary>
		/// Sends an asynchronous request to read item values at specific times.
		/// </summary>
		/// <param name="timestamps">The set of timestamps to use when reading items values.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		IdentifiedResult[] ReadAtTime(
			DateTime[]             timestamps,
			ItemIdentifier[]       items,
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request);

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
		ModifiedValueCollection[] ReadModified(
			Time             startTime,
			Time             endTime,
			int              maxValues,
			ItemIdentifier[] items);	

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
		IdentifiedResult[] ReadModified(
			Time                   startTime,
			Time                   endTime,
			int                    maxValues,
			ItemIdentifier[]       items,
			object                 requestHandle,
			ReadValuesEventHandler callback,
			out IRequest           request);

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
		ItemAttributeCollection ReadAttributes(
			Time           startTime,
			Time           endTime,
			ItemIdentifier item,
			int[]          attributeIDs);
		
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
		ResultCollection ReadAttributes(
			Time                       startTime,
			Time                       endTime,
			ItemIdentifier             item,
			int[]                      attributeIDs,
			object                     requestHandle,
			ReadAttributesEventHandler callback,
			out IRequest               request);

		//======================================================================
		// ReadAnnotations

		/// <summary>
		/// Reads any annotations for an item within the a time interval.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to read.</param>
		/// <param name="endTime">The end of the history period to be read.</param>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <returns>A set of annotations within the requested time range for each item.</returns>
		AnnotationValueCollection[] ReadAnnotations(
			Time             startTime,
			Time             endTime,
			ItemIdentifier[] items);

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
		IdentifiedResult[] ReadAnnotations(
			Time                        startTime,
			Time                        endTime,
			ItemIdentifier[]            items,
			object                      requestHandle,
			ReadAnnotationsEventHandler callback,
			out IRequest                request);

		//======================================================================
		// InsertAnnotations

		/// <summary>
		/// Inserts annotations for one or more items.
		/// </summary>
		/// <param name="items">A list of annotations to add for each item (must include the server handle).</param>
		/// <returns>The results of the insert operation for each annotation set.</returns>
		ResultCollection[] InsertAnnotations(AnnotationValueCollection[] items);

		/// <summary>
		/// Sends an asynchronous request to inserts annotations for one or more items.
		/// </summary>
		/// <param name="items">A list of annotations to add for each item (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		IdentifiedResult[] InsertAnnotations(
			AnnotationValueCollection[] items,
			object                      requestHandle,
			UpdateCompleteEventHandler  callback,
			out IRequest                request);

		//======================================================================
		// Insert

		/// <summary>
		/// Inserts the values into the history database for one or more items. 
		/// </summary>
		/// <param name="items">The set of values to insert.</param>
		/// <param name="replace">Whether existing values should be replaced.</param>
		/// <returns></returns>
		ResultCollection[] Insert(ItemValueCollection[] items, bool replace);
		
		/// <summary>
		/// Sends an asynchronous request to inserts values for one or more items.
		/// </summary>
		/// <param name="items">The set of values to insert.</param>
		/// <param name="replace">Whether existing values should be replaced.</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		IdentifiedResult[] Insert(
			ItemValueCollection[]      items,
			bool                       replace,
			object                     requestHandle,
			UpdateCompleteEventHandler callback,
			out IRequest               request);

		//======================================================================
		// Replace

		/// <summary>
		/// Replace the values into the history database for one or more items. 
		/// </summary>
		/// <param name="items">The set of values to replace.</param>
		/// <returns></returns>
		ResultCollection[] Replace(ItemValueCollection[] items);
		
		/// <summary>
		/// Sends an asynchronous request to replace values for one or more items.
		/// </summary>
		/// <param name="items">The set of values to replace.</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		IdentifiedResult[] Replace(
			ItemValueCollection[]      items,
			object                     requestHandle,
			UpdateCompleteEventHandler callback,
			out IRequest               request);

		//======================================================================
		// Delete

		/// <summary>
		/// Deletes the values with the specified time domain for one or more items.
		/// </summary>
		/// <param name="startTime">The beginning of the history period to delete.</param>
		/// <param name="endTime">The end of the history period to be delete.</param>
		/// <param name="items">The set of items to delete (must include the server handle).</param>
		/// <returns>The results of the delete operation for each item.</returns>
		IdentifiedResult[] Delete(
			Time             startTime,
			Time             endTime,
			ItemIdentifier[] items);
		
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
		IdentifiedResult[] Delete(
			Time                       startTime,
			Time                       endTime,
			ItemIdentifier[]           items,
			object                     requestHandle,
			UpdateCompleteEventHandler callback,
			out IRequest               request);

		//======================================================================
		// DeleteAtTime

		/// <summary>
		/// Deletes the values at the specified times for one or more items. 
		/// </summary>
		/// <param name="items">The set of timestamps to delete for one or more items.</param>
		/// <returns>The results of the operation for each timestamp.</returns>
		ResultCollection[] DeleteAtTime(ItemTimeCollection[] items);
		
		/// <summary>
		/// Sends an asynchronous request to delete values for one or more items at a specified times.
		/// </summary>
		/// <param name="items">The set of timestamps to delete for one or more items.</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		IdentifiedResult[] DeleteAtTime(
			ItemTimeCollection[]       items,
			object                     requestHandle,
			UpdateCompleteEventHandler callback,
			out IRequest               request);

		//======================================================================
		// CancelRequest

		/// <summary>
		/// Cancels an asynchronous request.
		/// </summary>
		/// <param name="request">The state object for the request to cancel.</param>
		void CancelRequest(IRequest request);
		
		/// <summary>
		/// Cancels an asynchronous request.
		/// </summary>
		/// <param name="request">The state object for the request to cancel.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		void CancelRequest(IRequest request, CancelCompleteEventHandler callback);
	}
	
	/// <summary>
	/// The set of possible server states.
	/// </summary>
	public enum ServerState
	{
		/// <summary>
		/// The historian is running.
		/// </summary>
		Up = 1,

		/// <summary>
		/// The historian is not running.
		/// </summary>
		Down = 2, 

		/// <summary>
		/// The status of the historian is indeterminate.
		/// </summary>
		Indeterminate = 3
	}
	
	/// <summary>
	/// Contains properties that describe the current status of an OPC server.
	/// </summary>
	[Serializable]
	public class ServerStatus : ICloneable
	{
		/// <summary>
		/// The vendor name and product name for the server.
		/// </summary>
		public string VendorInfo
		{
			get { return m_vendorInfo;  } 
			set { m_vendorInfo = value; }
		}

		/// <summary>
		/// A string that contains the server software version number.
		/// </summary>
		public string ProductVersion
		{
			get { return m_productVersion;  } 
			set { m_productVersion = value; }
		}

		/// <summary>
		/// The current state of the server.
		/// </summary>
		public ServerState ServerState
		{
			get { return m_serverState;  } 
			set { m_serverState = value; }
		}

		/// <summary>
		/// A string that describes the current server state.
		/// </summary>
		public string StatusInfo
		{
			get { return m_statusInfo;  } 
			set { m_statusInfo = value; }
		}

		/// <summary>
		/// The UTC time when the server started.
		/// </summary>
		public DateTime StartTime
		{
			get { return m_startTime;  } 
			set { m_startTime = value; }
		}

		/// <summary>
		/// Th current UTC time at the server.
		/// </summary>
		public DateTime CurrentTime
		{
			get { return m_currentTime;  } 
			set { m_currentTime = value; }
		}

		/// <summary>
		/// The maximum number of values that can be returned by the server on a per item basis. 
		/// </summary>
		public int MaxReturnValues
		{
			get { return m_maxReturnValues;  } 
			set { m_maxReturnValues = value; }
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deepcopy of the object.
		/// </summary>
		public virtual object Clone() { return MemberwiseClone(); }
		#endregion
        
		#region Private Members
		private string m_vendorInfo = null;
		private string m_productVersion = null;
		private DateTime m_currentTime = DateTime.MinValue;
		private DateTime m_startTime = DateTime.MinValue;
		private ServerState m_serverState = ServerState.Indeterminate;
		private string m_statusInfo = null;
		private int m_maxReturnValues = 0;		
		#endregion
	}

	//=============================================================================
	// Asynchronous Request Delegates

	/// <summary>
	/// Used to receive notifications when an exception occurs while processing a callback.
	/// </summary>
	public delegate void CallbackExceptionEventHandler(IRequest request, Exception exception);

	/// <summary>
	/// Used to receive data update notifications.
	/// </summary>
	public delegate void DataUpdateEventHandler(IRequest request, ItemValueCollection[] results);

	/// <summary>
	/// Used to receive notifications when a read values request completes.
	/// </summary>
	public delegate void ReadValuesEventHandler(IRequest request, ItemValueCollection[] results);
	
	/// <summary>
	/// Used to receive notifications when a read attributes request completes.
	/// </summary>
	public delegate void ReadAttributesEventHandler(IRequest request, ItemAttributeCollection results);

	/// <summary>
	/// Used to receive notifications when a read annotations request completes.
	/// </summary>
	public delegate void ReadAnnotationsEventHandler(IRequest request, AnnotationValueCollection[] results);

	/// <summary>
	/// Used to receive notifications when an update request completes.
	/// </summary>
	public delegate void UpdateCompleteEventHandler(IRequest request, ResultCollection[] results);

	/// <summary>
	/// Used to receive notifications when a request is cancelled.
	/// </summary>
	public delegate void CancelCompleteEventHandler(IRequest request);
}
