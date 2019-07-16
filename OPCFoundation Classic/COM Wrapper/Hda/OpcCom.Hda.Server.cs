//============================================================================
// TITLE: OpcCom.Hda.Server.cs
//
// CONTENTS:
// 
// An in-process wrapper for a remote OPC COM-HDA 1.20 server.
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
// 2003/12/31 RSA   Initial implementation.
// 2004/11/22 RSA   Removed support for writing HDA quality values.

using System;
using System.Xml;
using System.Net;
using System.Threading;
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Runtime.InteropServices;
using Opc;
using Opc.Hda;
using OpcRcw.Hda;
using OpcRcw.Comn;

namespace OpcCom.Hda
{
	/// <summary>
	/// An in-process wrapper for a remote OPC COM-HDA server (thread-safe).
	/// </summary>
	public class  Server : OpcCom.Server, Opc.Hda.IServer
	{	
		#region Constructor
		//======================================================================
		// Construction

		/// <summary>
		/// Initializes the object.
		/// </summary>
		internal Server() {}
		
		/// <summary>
		/// Initializes the object with the specifed COM server.
		/// </summary>
        public Server(URL url, object server) 
		{
			if (url == null) throw new ArgumentNullException("url");

			m_url    = (URL)url.Clone();
			m_server = server;

			// establish the callback.
			Advise();
		}
		#endregion

        #region IDisposable Members
        /// <summary>
        /// Releases unmanaged resources held by the object.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                lock (this)
                {
                    if (disposing)
                    {
                        // Release managed resources.
                        // close the callback.
                        Unadvise();
                    }

                    // Release unmanaged resources.
                    // Set large fields to null.

                    m_disposed = true;
                }
            }

            base.Dispose(disposing);
        }

        private bool m_disposed = false;
        #endregion

		#region Server Info
		//======================================================================
		// GetStatus

		/// <summary>
		/// Returns the current server status.
		/// </summary>
		/// <returns>The current server status.</returns>
		public ServerStatus GetStatus()
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				IntPtr pStatus = IntPtr.Zero;

				OPCHDA_SERVERSTATUS wStatus = OPCHDA_SERVERSTATUS.OPCHDA_INDETERMINATE;
				
				IntPtr pftCurrentTime    = IntPtr.Zero;
				IntPtr pftStartTime      = IntPtr.Zero;
				short  wMajorVersion     = 0;
				short  wMinorVersion     = 0;
				short  wBuildNumber      = 0;
				int    dwMaxReturnValues = 0;
				string szStatusString    = null;
				string szVendorInfo      = null;

				// invoke COM method.
				try
				{
					((IOPCHDA_Server)m_server).GetHistorianStatus(
						out wStatus,
						out pftCurrentTime,
						out pftStartTime,
						out wMajorVersion,
						out wMinorVersion,
						out wBuildNumber,
						out dwMaxReturnValues,
						out szStatusString,
						out szVendorInfo);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_Server.GetHistorianStatus", e);
				}

				// unmarshal return parameters and free memory.
				ServerStatus status = new ServerStatus();

				status.VendorInfo      = szVendorInfo;
				status.ProductVersion  = String.Format("{0}.{1}.{2}", wMajorVersion, wMinorVersion, wBuildNumber);
				status.ServerState     = (ServerState)wStatus;
				status.StatusInfo      = szStatusString;					
				status.StartTime       = DateTime.MinValue;			
				status.CurrentTime     = DateTime.MinValue;
				status.MaxReturnValues = dwMaxReturnValues;
				
				if (pftStartTime != IntPtr.Zero)
				{
					status.StartTime = OpcCom.Interop.GetFILETIME(pftStartTime);
					Marshal.FreeCoTaskMem(pftStartTime);
				}

				if (pftCurrentTime != IntPtr.Zero)
				{
					status.CurrentTime = OpcCom.Interop.GetFILETIME(pftCurrentTime);
					Marshal.FreeCoTaskMem(pftCurrentTime);
				}

				return status;
			}
		}	
		
		//======================================================================
		// GetAttributes

		/// <summary>
		/// Returns the item attributes supported by the server.
		/// </summary>
		/// <returns>The a set of item attributes and their descriptions.</returns>
		public Opc.Hda.Attribute[] GetAttributes()
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				int count = 0;

				IntPtr pIDs          = IntPtr.Zero;
				IntPtr pNames        = IntPtr.Zero;
				IntPtr pDescriptions = IntPtr.Zero;
				IntPtr pDataTypes    = IntPtr.Zero;

				try
				{
					((IOPCHDA_Server)m_server).GetItemAttributes(
						out count,
						out pIDs,
						out pNames,
						out pDescriptions,
						out pDataTypes);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_Server.GetItemAttributes", e);
				}

				// check if no attributes supported.
				if (count == 0)
				{
					return new Opc.Hda.Attribute[0];
				}

				// unmarshal return parameters and free memory.
				int[]    ids          = OpcCom.Interop.GetInt32s(ref pIDs, count, true);
				string[] names        = OpcCom.Interop.GetUnicodeStrings(ref pNames, count, true);
				string[] descriptions = OpcCom.Interop.GetUnicodeStrings(ref pDescriptions, count, true);
				short[]  datatypes    = OpcCom.Interop.GetInt16s(ref pDataTypes, count, true);

				// verify return parameters.
				if (ids == null || names == null || descriptions == null || datatypes == null)
				{
					throw new InvalidResponseException();
				}

				Opc.Hda.Attribute[] attributes = new Opc.Hda.Attribute[count];

				for (int ii = 0; ii < count; ii++)
				{
					attributes[ii] = new Opc.Hda.Attribute();

					attributes[ii].ID          = ids[ii];
					attributes[ii].Name        = names[ii];
					attributes[ii].Description = descriptions[ii];
					attributes[ii].DataType    = OpcCom.Interop.GetType((VarEnum)Enum.ToObject(typeof(VarEnum), datatypes[ii]));
				}

				// return results.
				return attributes;
			}
		}
	
		//======================================================================
		// GetAggregates

		/// <summary>
		/// Returns the aggregates supported by the server.
		/// </summary>
		/// <returns>The a set of aggregates and their descriptions.</returns>
		public Opc.Hda.Aggregate[] GetAggregates()
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				int count = 0;

				IntPtr pIDs          = IntPtr.Zero;
				IntPtr pNames        = IntPtr.Zero;
				IntPtr pDescriptions = IntPtr.Zero;

				try
				{
					((IOPCHDA_Server)m_server).GetAggregates(
						out count,
						out pIDs,
						out pNames,
						out pDescriptions);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_Server.GetAggregates", e);
				}

				// check if no aggregates supported.
				if (count == 0)
				{
					return new Opc.Hda.Aggregate[0];
				}

				// unmarshal return parameters and free memory.
				int[]    ids          = OpcCom.Interop.GetInt32s(ref pIDs, count, true);
				string[] names        = OpcCom.Interop.GetUnicodeStrings(ref pNames, count, true);
				string[] descriptions = OpcCom.Interop.GetUnicodeStrings(ref pDescriptions, count, true);

				// verify return parameters.
				if (ids == null || names == null || descriptions == null)
				{
					throw new InvalidResponseException();
				}

				Opc.Hda.Aggregate[] aggregates = new Opc.Hda.Aggregate[count];

				for (int ii = 0; ii < count; ii++)
				{
					aggregates[ii] = new Opc.Hda.Aggregate();

					aggregates[ii].ID          = ids[ii];
					aggregates[ii].Name        = names[ii];
					aggregates[ii].Description = descriptions[ii];
				}

				// return results.
				return aggregates;
			}
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
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				int count = (filters != null)?filters.Length:0;

				// marshal input parameters.
				int[]                  ids       = new int[count];
				object[]               values    = new object[count];
				OPCHDA_OPERATORCODES[] operators = new OPCHDA_OPERATORCODES[count];

				for (int ii = 0; ii < count; ii++)
				{
					ids[ii]       = filters[ii].AttributeID;
					operators[ii] = (OPCHDA_OPERATORCODES)Enum.ToObject(typeof(OPCHDA_OPERATORCODES), filters[ii].Operator);
					values[ii]    = OpcCom.Interop.GetVARIANT(filters[ii].FilterValue);
				}

				// initialize output parameners
				IOPCHDA_Browser pBrowser = null;
				IntPtr          pErrors  = IntPtr.Zero;

				// call COM server.
				try
				{
					((IOPCHDA_Server)m_server).CreateBrowse(
						count,
						ids,
						operators,
						values,
						out pBrowser,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_Server.CreateBrowse", e);
				}

				// unmarshal return parameters and free memory.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, count, true);

				// verify return parameters.
				if ((count > 0 && errors == null) || pBrowser == null)
				{
					throw new InvalidResponseException();
				}

				results = new ResultID[count];

				for (int ii = 0; ii < count; ii++)
				{
					results[ii] = OpcCom.Interop.GetResultID(errors[ii]);
				}

				// return browser.
				return new OpcCom.Hda.Browser(this, pBrowser, filters, results);
			}
		}
		#endregion

		#region Item Management
		//======================================================================
		// CreateItems

		/// <summary>
		/// Creates a set of items.
		/// </summary>
		/// <param name="items">The identifiers for the items to create.</param>
		/// <returns>The results for each item containing the server handle and result code.</returns>
		public IdentifiedResult[] CreateItems(ItemIdentifier[] items)
		{
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				// initialize input parameters.
				string[] itemIDs       = new string[items.Length];
				int[]    clientHandles = new int[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					if (items[ii] != null)
					{
						itemIDs[ii]       = items[ii].ItemName;
						clientHandles[ii] = CreateHandle();
					}
				}

				// initialize output arguments.
				IntPtr pServerHandles = IntPtr.Zero;
				IntPtr pErrors        = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_Server)m_server).GetItemHandles(
						items.Length,
						itemIDs,
						clientHandles,
						out pServerHandles,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_Server.GetItemHandles", e);
				}

				// unmarshal return parameters and free memory.
				int[] serverHandles = OpcCom.Interop.GetInt32s(ref pServerHandles, items.Length, true);
				int[] errors        = OpcCom.Interop.GetInt32s(ref pErrors, items.Length, true);
			
				// verify return parameters.
				if (serverHandles == null || errors == null)
				{
					throw new InvalidResponseException();
				}

				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < results.Length; ii++)
				{
					results[ii]              = new IdentifiedResult(items[ii]);
					results[ii].ResultID     = OpcCom.Interop.GetResultID(errors[ii]);

					if (results[ii].ResultID.Succeeded())
					{
						// cache item id locally to store remote server handle/local client handle mapping.
						ItemIdentifier itemID = new ItemIdentifier();

						itemID.ItemName     = items[ii].ItemName;
						itemID.ItemPath     = items[ii].ItemPath;
						itemID.ServerHandle = serverHandles[ii];
						itemID.ClientHandle = items[ii].ClientHandle;

						m_items.Add(clientHandles[ii], itemID);

						// return correct handles in result.
						results[ii].ServerHandle = clientHandles[ii];
						results[ii].ClientHandle = items[ii].ClientHandle;
					}
				}

				return results;
			}
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
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				// initialize input parameters.
				int[] serverHandles = GetServerHandles(items);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_Server)m_server).ReleaseItemHandles(
						items.Length,
						serverHandles,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_Server.ReleaseItemHandles", e);
				}

				// unmarshal return parameters and free memory.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, items.Length, true);
			
				// verify return parameters.
				if (errors == null)
				{
					throw new InvalidResponseException();
				}

				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < results.Length; ii++)
				{
					results[ii]          = new IdentifiedResult(items[ii]);
					results[ii].ResultID = OpcCom.Interop.GetResultID(errors[ii]);

					if (results[ii].ResultID.Succeeded() && items[ii].ServerHandle != null)
					{
						// lookup locally cached item id.
						ItemIdentifier itemID = (ItemIdentifier)m_items[items[ii].ServerHandle];

						// remove the locally cached item.
						if (itemID != null)
						{
							results[ii].ItemName     = itemID.ItemName;
							results[ii].ItemPath     = itemID.ItemPath;
							results[ii].ClientHandle = itemID.ClientHandle;

							m_items.Remove(items[ii].ServerHandle);
						}
					}
				}

				// return results.
				return results;
			}
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
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				// initialize input parameters.
				string[] itemIDs = new string[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					if (items[ii] != null)
					{
						itemIDs[ii] = items[ii].ItemName;
					}
				}

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_Server)m_server).ValidateItemIDs(
						items.Length,
						itemIDs,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_Server.ValidateItemIDs", e);
				}

				// unmarshal return parameters and free memory.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, items.Length, true);
				
				// verify return parameters.
				if (errors == null)
				{
					throw new InvalidResponseException();
				}

				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < results.Length; ii++)
				{
					results[ii] = new IdentifiedResult(items[ii]);
					results[ii].ResultID = OpcCom.Interop.GetResultID(errors[ii]);
				}

				return results;
			}
		}
		#endregion

		#region Read Raw
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
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new ItemValueCollection[0];
				}

				// initialize input parameters.
				int[] serverHandles = GetServerHandles(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime   = Interop.GetTime(endTime);

				// initialize output arguments.
				IntPtr pValues = IntPtr.Zero;
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_SyncRead)m_server).ReadRaw(
						ref pStartTime,
						ref pEndTime,
						maxValues,
						(includeBounds)?1:0,
                        serverHandles.Length,
						serverHandles,
						out pValues,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_SyncRead.ReadRaw", e);
				}
				
				// unmarhal modified item structures.
				ItemValueCollection[] results = Interop.GetItemValueCollections(ref pValues, items.Length, true);

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// store actual items in result.
				UpdateActualTimes(results, pStartTime, pEndTime);

				// completed successfully.
				return results;
			}
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
			Time                     startTime,
			Time                     endTime,
			int                      maxValues,
			bool                     includeBounds,
			ItemIdentifier[]         items,
			object                   requestHandle,
			ReadValuesEventHandler callback,
			out IRequest             request)
		{
			if (items == null)    throw new ArgumentNullException("items");
			if (callback == null) throw new ArgumentNullException("callback");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize input parameters.
				int requestID = internalRequest.RequestID;
				int cancelID  = 0;

				int[] serverHandles = GetServerHandles(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime   = Interop.GetTime(endTime);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_AsyncRead)m_server).ReadRaw(
						internalRequest.RequestID,
						ref pStartTime,
						ref pEndTime,
						maxValues,
						(includeBounds)?1:0,
						serverHandles.Length,
						serverHandles,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_AsyncRead.ReadRaw", e);
				}

				// create result objects.
				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					results[ii] = new IdentifiedResult();
				}

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// check if request has already completed.
				if (internalRequest.Update(cancelID, results))
				{
					// discard the request.
					request = null;
					m_callback.CancelRequest(internalRequest, null);
					
					// return results.
					return results;
				}

				// store actual items in request object.
				UpdateActualTimes(new IActualTime[] { internalRequest }, pStartTime, pEndTime);
			
				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return results;
			}
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
			Time                    startTime,
			decimal                 updateInterval,
			ItemIdentifier[]        items,
			object                  requestHandle,
			DataUpdateEventHandler callback,
			out IRequest            request)
		{
			if (items == null)    throw new ArgumentNullException("items");
			if (callback == null) throw new ArgumentNullException("callback");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize input parameters.
				int requestID = internalRequest.RequestID;
				int cancelID  = 0;

				int[] serverHandles = GetServerHandles(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime       = Interop.GetTime(startTime);
				OPCHDA_FILETIME        ftUpdateInterval = Interop.GetFILETIME(updateInterval);	

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_AsyncRead)m_server).AdviseRaw(
						internalRequest.RequestID,
						ref pStartTime,
						ftUpdateInterval,
						serverHandles.Length,
						serverHandles,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_AsyncRead.AdviseRaw", e);
				}

				// create result objects.
				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					results[ii] = new IdentifiedResult();
				}

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// send callbacks for any data that has already arrived.
				internalRequest.Update(cancelID, results);
			
				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return results;
			}
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
			Time                       startTime,
			Time                       endTime,
			int                        maxValues,
			decimal                    updateInterval,
			decimal                    playbackDuration,
			ItemIdentifier[]           items,
			object                     requestHandle,
			DataUpdateEventHandler callback,
			out IRequest               request)
		{
			if (items == null)    throw new ArgumentNullException("items");
			if (callback == null) throw new ArgumentNullException("callback");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize input parameters.
				int requestID = internalRequest.RequestID;
				int cancelID  = 0;

				int[] serverHandles = GetServerHandles(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime       = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime         = Interop.GetTime(endTime);
				OPCHDA_FILETIME        ftUpdateInterval = Interop.GetFILETIME(updateInterval);	
				OPCHDA_FILETIME        ftUpdateDuration = Interop.GetFILETIME(playbackDuration);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_Playback)m_server).ReadRawWithUpdate(
						internalRequest.RequestID,
						ref pStartTime,
						ref pEndTime,
						maxValues,
						ftUpdateDuration,
						ftUpdateInterval,
						serverHandles.Length,
						serverHandles,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_Playback.ReadRawWithUpdate", e);
				}

				// create result objects.
				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					results[ii] = new IdentifiedResult();
				}

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// send callbacks for any data that has already arrived.
				internalRequest.Update(cancelID, results);
			
				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return results;
			}
		}
		#endregion

		#region Read Processed
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
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new ItemValueCollection[0];
				}

				// initialize input parameters.
				int[] serverHandles = GetServerHandles(items);
				int[] aggregateIDs  = GetAggregateIDs(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime  = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime    = Interop.GetTime(endTime);
				OPCHDA_FILETIME ftResampleInterval = Interop.GetFILETIME(resampleInterval);	

				// initialize output arguments.
				IntPtr pValues = IntPtr.Zero;
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_SyncRead)m_server).ReadProcessed(
						ref pStartTime,
						ref pEndTime,
						ftResampleInterval,
						serverHandles.Length,
						serverHandles,
						aggregateIDs,
						out pValues,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_SyncRead.ReadProcessed", e);
				}

				// unmarhal modified item structures.
				ItemValueCollection[] results = Interop.GetItemValueCollections(ref pValues, items.Length, true);

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// store actual items in result.
				UpdateActualTimes(results, pStartTime, pEndTime);
	
				// completed successfully.
				return results;
			}
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
			if (items == null)    throw new ArgumentNullException("items");
			if (callback == null) throw new ArgumentNullException("callback");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize input parameters.
				int requestID = internalRequest.RequestID;
				int cancelID  = 0;

				int[] serverHandles = GetServerHandles(items);
				int[] aggregateIDs  = GetAggregateIDs(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime  = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime    = Interop.GetTime(endTime);
				OPCHDA_FILETIME ftResampleInterval = Interop.GetFILETIME(resampleInterval);	


				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_AsyncRead)m_server).ReadProcessed(
						internalRequest.RequestID,
						ref pStartTime,
						ref pEndTime,
						ftResampleInterval,
						serverHandles.Length,
						serverHandles,
						aggregateIDs,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_AsyncRead.ReadProcessed", e);
				}

				// create result objects.
				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					results[ii] = new IdentifiedResult();
				}

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// check if request has already completed.
				if (internalRequest.Update(cancelID, results))
				{
					// discard the request.
					request = null;
					m_callback.CancelRequest(internalRequest, null);
					
					// return results.
					return results;
				}

				// store actual items in request object.
				UpdateActualTimes(new IActualTime[] { internalRequest }, pStartTime, pEndTime);
			
				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return results;
			}
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
			Time                    startTime,
			decimal                 resampleInterval,
			int                     numberOfIntervals,
			Item[]                  items,
			object                  requestHandle,
			DataUpdateEventHandler callback,
			out IRequest            request)
		{
			if (items == null)    throw new ArgumentNullException("items");
			if (callback == null) throw new ArgumentNullException("callback");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize input parameters.
				int requestID = internalRequest.RequestID;
				int cancelID  = 0;

				int[] serverHandles = GetServerHandles(items);
				int[] aggregateIDs  = GetAggregateIDs(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime  = Interop.GetTime(startTime);
				OPCHDA_FILETIME ftResampleInterval = Interop.GetFILETIME(resampleInterval);	

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_AsyncRead)m_server).AdviseProcessed(
						internalRequest.RequestID,
						ref pStartTime,
						ftResampleInterval,
						serverHandles.Length,
						serverHandles,
						aggregateIDs,
						numberOfIntervals,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_AsyncRead.AdviseProcessed", e);
				}

				// create result objects.
				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					results[ii] = new IdentifiedResult();
				}

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// send callbacks for any data that has already arrived.
				internalRequest.Update(cancelID, results);
			
				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return results;
			}
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
			Time                       startTime,
			Time                       endTime,
			decimal                    resampleInterval,
			int                        numberOfIntervals,
			decimal                    updateInterval,
			Item[]                     items,
			object                     requestHandle,
			DataUpdateEventHandler callback,
			out IRequest               request)
		{
			if (items == null)    throw new ArgumentNullException("items");
			if (callback == null) throw new ArgumentNullException("callback");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize input parameters.
				int requestID = internalRequest.RequestID;
				int cancelID  = 0;

				int[] serverHandles = GetServerHandles(items);
				int[] aggregateIDs  = GetAggregateIDs(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime  = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime    = Interop.GetTime(endTime);
				OPCHDA_FILETIME ftResampleInterval = Interop.GetFILETIME(resampleInterval);	
				OPCHDA_FILETIME ftUpdateInterval   = Interop.GetFILETIME(updateInterval);	

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_Playback)m_server).ReadProcessedWithUpdate(
						internalRequest.RequestID,
						ref pStartTime,
						ref pEndTime,
						ftResampleInterval,
						numberOfIntervals,
						ftUpdateInterval,
						serverHandles.Length,
						serverHandles,
						aggregateIDs,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_Playback.ReadProcessedWithUpdate", e);
				}

				// create result objects.
				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					results[ii] = new IdentifiedResult();
				}

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// send callbacks for any data that has already arrived.
				internalRequest.Update(cancelID, results);
			
				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return results;
			}
		}
		#endregion

		#region Read At Time
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
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new ItemValueCollection[0];
				}

				// initialize input parameters.
				int[] serverHandles = GetServerHandles(items);
				OPCHDA_FILETIME[] ftTimestamps = Interop.GetFILETIMEs(timestamps);	

				// initialize output arguments.
				IntPtr pValues = IntPtr.Zero;
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_SyncRead)m_server).ReadAtTime(
						ftTimestamps.Length,
						ftTimestamps,
						serverHandles.Length,
						serverHandles,
						out pValues,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_SyncRead.ReadAtTime", e);
				}

				// unmarhal modified item structures.
				ItemValueCollection[] results = Interop.GetItemValueCollections(ref pValues, items.Length, true);

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// completed successfully.
				return results;
			}
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
			if (items == null)    throw new ArgumentNullException("items");
			if (callback == null) throw new ArgumentNullException("callback");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize input parameters.
				int requestID = internalRequest.RequestID;
				int cancelID  = 0;

				int[] serverHandles = GetServerHandles(items);
				OPCHDA_FILETIME[] ftTimestamps = Interop.GetFILETIMEs(timestamps);	

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_AsyncRead)m_server).ReadAtTime(
						internalRequest.RequestID,
						ftTimestamps.Length,
						ftTimestamps,
						serverHandles.Length,
						serverHandles,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_AsyncRead.ReadAtTime", e);
				}

				// create result objects.
				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					results[ii] = new IdentifiedResult();
				}

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// check if request has already completed.
				if (internalRequest.Update(cancelID, results))
				{
					// discard the request.
					request = null;
					m_callback.CancelRequest(internalRequest, null);
					
					// return results.
					return results;
				}

				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return results;
			}
		}
		#endregion

		#region Read Modified
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
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new ModifiedValueCollection[0];
				}

				// initialize input parameters.
				int[] serverHandles = GetServerHandles(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime   = Interop.GetTime(endTime);

				// initialize output arguments.
				IntPtr pValues = IntPtr.Zero;
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_SyncRead)m_server).ReadModified(
						ref pStartTime,
						ref pEndTime,
						maxValues,
						serverHandles.Length,
						serverHandles,
						out pValues,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_SyncRead.ReadModified", e);
				}

				// unmarhal modified item structures.
				ModifiedValueCollection[] results = Interop.GetModifiedValueCollections(ref pValues, items.Length, true);

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// store actual items in result.
				UpdateActualTimes(results, pStartTime, pEndTime);

				// completed successfully.
				return results;
			}
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
			if (items == null)    throw new ArgumentNullException("items");
			if (callback == null) throw new ArgumentNullException("callback");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize input parameters.
				int requestID = internalRequest.RequestID;
				int cancelID  = 0;

				int[] serverHandles = GetServerHandles(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime   = Interop.GetTime(endTime);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_AsyncRead)m_server).ReadModified(
						internalRequest.RequestID,
						ref pStartTime,
						ref pEndTime,
						maxValues,
						serverHandles.Length,
						serverHandles,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_AsyncRead.ReadModified", e);
				}

				// create result objects.
				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					results[ii] = new IdentifiedResult();
				}

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// check if request has already completed.
				if (internalRequest.Update(cancelID, results))
				{
					// discard the request.
					request = null;
					m_callback.CancelRequest(internalRequest, null);
					
					// return results.
					return results;
				}

				// store actual items in request object.
				UpdateActualTimes(new IActualTime[] { internalRequest }, pStartTime, pEndTime);
			
				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return results;
			}
		}
		#endregion

		#region Read Attributes
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
			if (item == null)         throw new ArgumentNullException("item");
			if (attributeIDs == null) throw new ArgumentNullException("attributeIDs");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (attributeIDs.Length == 0)
				{
					return new ItemAttributeCollection(item);
				}

				// initialize input parameters.
				int[] serverHandles = GetServerHandles(new ItemIdentifier[] { item });

				OpcRcw.Hda.OPCHDA_TIME pStartTime = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime   = Interop.GetTime(endTime);

				// initialize output arguments.
				IntPtr pValues = IntPtr.Zero;
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_SyncRead)m_server).ReadAttribute(
						ref pStartTime,
						ref pEndTime,
						serverHandles[0],
						attributeIDs.Length,
						attributeIDs,
						out pValues,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_SyncRead.ReadAttribute", e);
				}

				// unmarhal item attribute structures.
				AttributeValueCollection[] attributes = Interop.GetAttributeValueCollections(ref pValues, attributeIDs.Length, true);
				
				// create item level result collection. 
				ItemAttributeCollection result = UpdateResults(item, attributes, ref pErrors);

				// store actual items in result.
				UpdateActualTimes(new IActualTime[] { result }, pStartTime, pEndTime);

				// completed successfully.
				return result;
			}
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
			if (item == null)         throw new ArgumentNullException("item");
			if (attributeIDs == null) throw new ArgumentNullException("attributeIDs");
			if (callback == null)     throw new ArgumentNullException("callback");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (attributeIDs.Length == 0)
				{
					return new ResultCollection();
				}

				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize input parameters.
				int requestID = internalRequest.RequestID;
				int cancelID  = 0;

				int[] serverHandles = GetServerHandles(new ItemIdentifier[] { item });

				OpcRcw.Hda.OPCHDA_TIME pStartTime = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime   = Interop.GetTime(endTime);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_AsyncRead)m_server).ReadAttribute(
						internalRequest.RequestID,
						ref pStartTime,
						ref pEndTime,
						serverHandles[0],
						attributeIDs.Length,
						attributeIDs,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_AsyncRead.ReadAttribute", e);
				}

				// create result objects.
				ResultCollection results = new ResultCollection(item);
                
				// update result with error code and info from the item argument.
				UpdateResult(item, results, 0);

				// unmarshal return parameters and free memory.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, attributeIDs.Length, true);

				// verify return parameters.
				if (errors == null)
				{
					throw new InvalidResponseException();
				}

				// add results for each attribute.
				foreach (int error in errors)
				{
					Result result = new Result(OpcCom.Interop.GetResultID(error));
					results.Add(result);
				}

				// check if request has already completed.
				if (internalRequest.Update(cancelID, new ResultCollection[] { results }))
				{
					// discard the request.
					request = null;
					m_callback.CancelRequest(internalRequest, null);
					
					// return results.
					return results;
				}

				// store actual items in request object.
				UpdateActualTimes(new IActualTime[] { internalRequest }, pStartTime, pEndTime);

				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return results;
			}
		}
		#endregion

		#region Annotations
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
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new AnnotationValueCollection[0];
				}

				// initialize input parameters.
				int[] serverHandles = GetServerHandles(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime   = Interop.GetTime(endTime);

				// initialize output arguments.
				IntPtr pValues = IntPtr.Zero;
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_SyncAnnotations)m_server).Read(
						ref pStartTime,
						ref pEndTime,
						serverHandles.Length,
						serverHandles,
						out pValues,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_SyncAnnotations.Read", e);
				}

				// unmarhal modified item structures.
				AnnotationValueCollection[] results = Interop.GetAnnotationValueCollections(ref pValues, items.Length, true);

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// store actual items in result.
				UpdateActualTimes(results, pStartTime, pEndTime);

				// completed successfully.
				return results;
			}
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
			if (items == null)    throw new ArgumentNullException("items");
			if (callback == null) throw new ArgumentNullException("callback");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize input parameters.
				int requestID = internalRequest.RequestID;
				int cancelID  = 0;

				int[] serverHandles = GetServerHandles(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime   = Interop.GetTime(endTime);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_AsyncAnnotations)m_server).Read(
						internalRequest.RequestID,
						ref pStartTime,
						ref pEndTime,
						serverHandles.Length,
						serverHandles,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_AsyncAnnotations.Read", e);
				}

				// create result objects.
				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					results[ii] = new IdentifiedResult();
				}

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// check if request has already completed.
				if (internalRequest.Update(cancelID, results))
				{
					// discard the request.
					request = null;
					m_callback.CancelRequest(internalRequest, null);
					
					// return results.
					return results;
				}

				// store actual items in request object.
				UpdateActualTimes(new IActualTime[] { internalRequest }, pStartTime, pEndTime);
			
				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return results;
			}
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
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new ResultCollection[0];
				}

				// create empty set of result collections.
				ResultCollection[] results = CreateResultCollections(items);

				// initialize input parameters.
				int[]               serverHandles = null;	
				OPCHDA_ANNOTATION[] pAnnotations  = null;	
				OPCHDA_FILETIME[]   pTimestamps   = null;	

				// flatten out list of collections into a set of single arrays.
				int count = MarshalAnnotatations(
					items,
					ref serverHandles,
					ref pTimestamps,
					ref pAnnotations);

				// handle trivial case.
				if (count == 0)
				{
					return results;
				}
			
				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_SyncAnnotations)m_server).Insert(
						serverHandles.Length,
						serverHandles,
						pTimestamps,
						pAnnotations,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_SyncAnnotations.Insert", e);
				}

				// free memory allocated for input arguments.
				for (int ii = 0; ii < pAnnotations.Length; ii++)
				{
					OpcCom.Interop.GetFILETIMEs(ref pAnnotations[ii].ftTimeStamps, 1, true);
					OpcCom.Interop.GetUnicodeStrings(ref pAnnotations[ii].szAnnotation, 1, true);
					OpcCom.Interop.GetFILETIMEs(ref pAnnotations[ii].ftAnnotationTime, 1, true);
					OpcCom.Interop.GetUnicodeStrings(ref pAnnotations[ii].szUser, 1, true);
				}
				
				// unmarshal return parameters and free memory.
				UpdateResults(items, results, count, ref pErrors);

				// completed successfully.
				return results;
			}
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
			if (items == null) throw new ArgumentNullException("items");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				// create empty set of result collections.
				ResultCollection[] results = CreateResultCollections(items);

				// initialize input parameters.
				int[]               serverHandles = null;	
				OPCHDA_ANNOTATION[] pAnnotations  = null;	
				OPCHDA_FILETIME[]   pTimestamps   = null;	

				// flatten out list of collections into a set of single arrays.
				int count = MarshalAnnotatations(
					items,
					ref serverHandles,
					ref pTimestamps,
					ref pAnnotations);

				// handle trivial case.
				if (count == 0)
				{
					return GetIdentifiedResults(results);
				}

				// create request.
				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				int cancelID = 0;

				// invoke COM method.
				try
				{
					((IOPCHDA_AsyncAnnotations)m_server).Insert(
						internalRequest.RequestID,
						serverHandles.Length,
						serverHandles,
						pTimestamps,
						pAnnotations,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_AsyncAnnotations.Insert", e);
				}

				// free memory allocated for input arguments.
				for (int ii = 0; ii < pAnnotations.Length; ii++)
				{
					OpcCom.Interop.GetFILETIMEs(ref pAnnotations[ii].ftTimeStamps, 1, true);
					OpcCom.Interop.GetUnicodeStrings(ref pAnnotations[ii].szAnnotation, 1, true);
					OpcCom.Interop.GetFILETIMEs(ref pAnnotations[ii].ftAnnotationTime, 1, true);
					OpcCom.Interop.GetUnicodeStrings(ref pAnnotations[ii].szUser, 1, true);
				}
				
				// unmarshal return parameters and free memory.
				UpdateResults(items, results, count, ref pErrors);

				// check if request has already completed.
				if (internalRequest.Update(cancelID, results))
				{
					// discard the request.
					request = null;
					m_callback.CancelRequest(internalRequest, null);
					
					// return results.
					return GetIdentifiedResults(results);
				}
			
				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return GetIdentifiedResults(results);
			}
		}
		#endregion

		#region Insert/Replace
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
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new ResultCollection[0];
				}

				// create empty set of result collections.
				ResultCollection[] results = CreateResultCollections(items);

				// initialize input parameters.
				int[]      serverHandles = null;	
				object[]   values        = null;	
				int[]      qualities     = null;	
				DateTime[] timestamps    = null;	

				// flatten out list of collections into a set of single arrays.
				int count = MarshalValues(
					items,
					ref serverHandles,
					ref values,
					ref qualities,
					ref timestamps);

				// handle trivial case.
				if (count == 0)
				{
					return results;
				}

				OPCHDA_FILETIME[] ftTimestamps = OpcCom.Hda.Interop.GetFILETIMEs(timestamps);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				if (replace)
				{
					try
					{
						((IOPCHDA_SyncUpdate)m_server).InsertReplace(
							serverHandles.Length,
							serverHandles,
							ftTimestamps,
							values,
							qualities,
							out pErrors);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCHDA_SyncUpdate.InsertReplace", e);
					}
				}
				else
				{
					try
					{
						((IOPCHDA_SyncUpdate)m_server).Insert(
							serverHandles.Length,
							serverHandles,
							ftTimestamps,
							values,
							qualities,
							out pErrors);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCHDA_SyncUpdate.Insert", e);
					}
				}

				// unmarshal return parameters and free memory.
				UpdateResults(items, results, count, ref pErrors);

				// completed successfully.
				return results;
			}
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
			if (items == null) throw new ArgumentNullException("items");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				// create empty set of result collections.
				ResultCollection[] results = CreateResultCollections(items);

				// initialize input parameters.
				int[]      serverHandles = null;	
				object[]   values        = null;	
				int[]      qualities     = null;	
				DateTime[] timestamps    = null;	

				// flatten out list of collections into a set of single arrays.
				int count = MarshalValues(
					items,
					ref serverHandles,
					ref values,
					ref qualities,
					ref timestamps);

				// handle trivial case.
				if (count == 0)
				{
					return GetIdentifiedResults(results);
				}

				OPCHDA_FILETIME[] ftTimestamps = OpcCom.Hda.Interop.GetFILETIMEs(timestamps);

				// create request.
				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				int cancelID = 0;

				// invoke COM method.
				if (replace)
				{
					try
					{
						((IOPCHDA_AsyncUpdate)m_server).InsertReplace(
							internalRequest.RequestID,
							serverHandles.Length,
							serverHandles,
							ftTimestamps,
							values,
							qualities,
							out cancelID,
							out pErrors);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCHDA_AsyncUpdate.InsertReplace", e);
					}
				}
				else
				{
					try
					{
						((IOPCHDA_AsyncUpdate)m_server).Insert(
							internalRequest.RequestID,
							serverHandles.Length,
							serverHandles,
							ftTimestamps,
							values,
							qualities,
							out cancelID,
							out pErrors);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCHDA_AsyncUpdate.Insert", e);
					}
				}

				// unmarshal return parameters and free memory.
				UpdateResults(items, results, count, ref pErrors);

				// check if request has already completed.
				if (internalRequest.Update(cancelID, results))
				{
					// discard the request.
					request = null;
					m_callback.CancelRequest(internalRequest, null);
					
					// return results.
					return GetIdentifiedResults(results);
				}
			
				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return GetIdentifiedResults(results);
			}
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
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new ResultCollection[0];
				}

				// create empty set of result collections.
				ResultCollection[] results = CreateResultCollections(items);

				// initialize input parameters.
				int[]      serverHandles = null;	
				object[]   values        = null;	
				int[]      qualities     = null;	
				DateTime[] timestamps    = null;	

				// flatten out list of collections into a set of single arrays.
				int count = MarshalValues(
					items,
					ref serverHandles,
					ref values,
					ref qualities,
					ref timestamps);

				// handle trivial case.
				if (count == 0)
				{
					return results;
				}

				OPCHDA_FILETIME[] ftTimestamps = OpcCom.Hda.Interop.GetFILETIMEs(timestamps);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_SyncUpdate)m_server).Replace(
						serverHandles.Length,
						serverHandles,
						ftTimestamps,
						values,
						qualities,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_SyncUpdate.Replace", e);
				}

				// unmarshal return parameters and free memory.
				UpdateResults(items, results, count, ref pErrors);

				// completed successfully.
				return results;
			}
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
			if (items == null) throw new ArgumentNullException("items");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				// create empty set of result collections.
				ResultCollection[] results = CreateResultCollections(items);

				// initialize input parameters.
				int[]      serverHandles = null;	
				object[]   values        = null;	
				int[]      qualities     = null;	
				DateTime[] timestamps    = null;	

				// flatten out list of collections into a set of single arrays.
				int count = MarshalValues(
					items,
					ref serverHandles,
					ref values,
					ref qualities,
					ref timestamps);

				// handle trivial case.
				if (count == 0)
				{
					return GetIdentifiedResults(results);
				}

				OPCHDA_FILETIME[] ftTimestamps = OpcCom.Hda.Interop.GetFILETIMEs(timestamps);

				// create request.
				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				int cancelID = 0;

				try
				{
					((IOPCHDA_AsyncUpdate)m_server).Replace(
						internalRequest.RequestID,
						serverHandles.Length,
						serverHandles,
						ftTimestamps,
						values,
						qualities,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_AsyncUpdate.Replace", e);
				}

				// unmarshal return parameters and free memory.
				UpdateResults(items, results, count, ref pErrors);

				// check if request has already completed.
				if (internalRequest.Update(cancelID, results))
				{
					// discard the request.
					request = null;
					m_callback.CancelRequest(internalRequest, null);
					
					// return results.
					return GetIdentifiedResults(results);
				}
			
				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return GetIdentifiedResults(results);
			}
		}
		#endregion

		#region Delete
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
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				// initialize input parameters.
				int[] serverHandles = GetServerHandles(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime   = Interop.GetTime(endTime);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_SyncUpdate)m_server).DeleteRaw(
						ref pStartTime,
						ref pEndTime,
						serverHandles.Length,
						serverHandles,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_SyncUpdate.DeleteRaw", e);
				}

				// create result objects.
				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					results[ii] = new IdentifiedResult();
				}

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// completed successfully.
				return results;
			}
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
			if (items == null)    throw new ArgumentNullException("items");
			if (callback == null) throw new ArgumentNullException("callback");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize input parameters.
				int requestID = internalRequest.RequestID;
				int cancelID  = 0;

				int[] serverHandles = GetServerHandles(items);

				OpcRcw.Hda.OPCHDA_TIME pStartTime = Interop.GetTime(startTime);
				OpcRcw.Hda.OPCHDA_TIME pEndTime   = Interop.GetTime(endTime);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_AsyncUpdate)m_server).DeleteRaw(
						internalRequest.RequestID,
						ref pStartTime,
						ref pEndTime,
						serverHandles.Length,
						serverHandles,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_AsyncUpdate.DeleteRaw", e);
				}

				// create result objects.
				IdentifiedResult[] results = new IdentifiedResult[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					results[ii] = new IdentifiedResult();
				}

				// update result with error code and info from the item argument.
				UpdateResults(items, results, ref pErrors);

				// check if request has already completed.
				if (internalRequest.Update(cancelID, results))
				{
					// discard the request.
					request = null;
					m_callback.CancelRequest(internalRequest, null);
					
					// return results.
					return results;
				}

				// store actual items in request object.
				UpdateActualTimes(new IActualTime[] { internalRequest }, pStartTime, pEndTime);
			
				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return results;
			}
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
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new ResultCollection[0];
				}

				// create empty set of result collections.
				ResultCollection[] results = CreateResultCollections(items);

				// initialize input parameters.
				int[]      serverHandles = null;	
				DateTime[] timestamps    = null;	

				// flatten out list of collections into a set of single arrays.
				int count = MarshalTimestamps(
					items,
					ref serverHandles,
					ref timestamps);

				// handle trivial case.
				if (count == 0)
				{
					return results;
				}

				OPCHDA_FILETIME[] ftTimestamps = OpcCom.Hda.Interop.GetFILETIMEs(timestamps);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCHDA_SyncUpdate)m_server).DeleteAtTime(
						serverHandles.Length,
						serverHandles,
						ftTimestamps,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_SyncUpdate.DeleteAtTime", e);
				}

				// unmarshal return parameters and free memory.
				UpdateResults(items, results, count, ref pErrors);

				// completed successfully.
				return results;
			}
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
			if (items == null) throw new ArgumentNullException("items");

			request = null;

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// handle trivial case.
				if (items.Length == 0)
				{
					return new IdentifiedResult[0];
				}

				// create empty set of result collections.
				ResultCollection[] results = CreateResultCollections(items);

				// initialize input parameters.
				int[]      serverHandles = null;	
				DateTime[] timestamps    = null;	

				// flatten out list of collections into a set of single arrays.
				int count = MarshalTimestamps(
					items,
					ref serverHandles,
					ref timestamps);

				// handle trivial case.
				if (count == 0)
				{
					return GetIdentifiedResults(results);
				}

				OPCHDA_FILETIME[] ftTimestamps = OpcCom.Hda.Interop.GetFILETIMEs(timestamps);

				// create request.
				Request internalRequest = m_callback.CreateRequest(requestHandle, callback);

				// initialize output arguments.
				IntPtr pErrors = IntPtr.Zero;

				int cancelID = 0;

				try
				{
					((IOPCHDA_AsyncUpdate)m_server).DeleteAtTime(
						internalRequest.RequestID,
						serverHandles.Length,
						serverHandles,
						ftTimestamps,
						out cancelID,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_AsyncUpdate.DeleteAtTime", e);
				}

				// unmarshal return parameters and free memory.
				UpdateResults(items, results, count, ref pErrors);

				// check if request has already completed.
				if (internalRequest.Update(cancelID, results))
				{
					// discard the request.
					request = null;
					m_callback.CancelRequest(internalRequest, null);
					
					// return results.
					return GetIdentifiedResults(results);
				}
			
				// return request object.
				request = internalRequest;
				
				// completed successfully.
				return GetIdentifiedResults(results);
			}
		}
		#endregion

		#region Cancel
		//======================================================================
		// CancelRequest

		/// <summary>
		/// Cancels an asynchronous request.
		/// </summary>
		/// <param name="request">The state object for the request to cancel.</param>
		public void CancelRequest(IRequest request)
		{
			CancelRequest(request, null);
		}
		
		/// <summary>
		/// Cancels an asynchronous request.
		/// </summary>
		/// <param name="request">The state object for the request to cancel.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		public void CancelRequest(IRequest request, CancelCompleteEventHandler callback)
		{
			if (request == null) throw new ArgumentNullException("request");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				Request internalRequest = (Request)request;

				// register the cancel request callback.
				m_callback.CancelRequest(internalRequest, callback);

				// invoke COM method.
				try
				{
					((IOPCHDA_AsyncRead)m_server).Cancel(internalRequest.CancelID);
				}
				catch (Exception e)
				{
					// a return code of E_FAIL indicates the request does not exist or can't be cancelled.
					if (OpcCom.Da.ResultIDs.E_FAIL != Marshal.GetHRForException(e))
					{
						throw OpcCom.Interop.CreateException("IOPCHDA_AsyncRead.Cancel", e);
					}
				}
			}
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
				try
				{
					m_connection = new ConnectionPoint(m_server, typeof(OpcRcw.Hda.IOPCHDA_DataCallback).GUID);
					m_connection.Advise(m_callback);
				}
				catch 
				{
					m_connection = null;
				}
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

		/// <summary>
		/// Creates a unique handle for an item.
		/// </summary>
		private int CreateHandle()
		{
			return Server.NextHandle++;
		}
		
		/// <summary>
		/// Finds an invalid server handle.
		/// </summary>
		private int GetInvalidHandle()
		{
			int max = 0;

			foreach (ItemIdentifier item in m_items.Values)
			{
				int handle = (int)item.ServerHandle;

				if (max < handle)
				{
					max = handle;
				}
			}

			return max+1;
		}

		/// <summary>
		/// Gets the total count for multiple collections.
		/// </summary>
		private int GetCount(ICollection[] collections)
		{
			int count = 0;

			if (collections != null)
			{
				foreach (ICollection collection in collections)
				{
					if (collection != null)
					{
						count += collection.Count;
					}
				}
			}

			return count;
		}

		/// <summary>
		/// Initializes a set of result collections from a set of item ids.
		/// </summary>
		ResultCollection[] CreateResultCollections(ItemIdentifier[] items)
		{
			ResultCollection[] results = null;

			if (items != null)
			{
				results = new ResultCollection[items.Length];

				for (int ii = 0; ii < items.Length; ii++)
				{
					results[ii] = new ResultCollection();

					if (items[ii] != null)
					{
						UpdateResult(items[ii], results[ii], 0);
					}
				}
			}

			return results;
		}

		/// <summary>
		/// Returns an array of item server handles.
		/// </summary>
		private int[] GetServerHandles(ItemIdentifier[] items)
		{
			// use this if the client passes an unrecognized server handle.
			int invalidHandle = GetInvalidHandle();

			// create server handle array.
			int[] serverHandles = new int[items.Length];

			for (int ii = 0; ii < items.Length; ii++)
			{
				serverHandles[ii] = invalidHandle;

				if (items[ii] != null && items[ii].ServerHandle != null)
				{
					// lookup cached handle.
					ItemIdentifier item = (ItemIdentifier)m_items[items[ii].ServerHandle];

					if (item != null)
					{
						serverHandles[ii] = (int)item.ServerHandle;
					}
				}
			}

			// return handles.
			return serverHandles;
		}

		/// <summary>
		/// Returns an array of item aggregate ids.
		/// </summary>
		private int[] GetAggregateIDs(Item[] items)
		{
			int[] aggregateIDs = new int[items.Length];

			for (int ii = 0; ii < items.Length; ii++)
			{
				aggregateIDs[ii] = 0;

				if (items[ii].AggregateID != Opc.Hda.AggregateID.NOAGGREGATE)
				{
					aggregateIDs[ii] = items[ii].AggregateID;
				}
			}

			return aggregateIDs;
		}

		/// <summary>
		/// Updates the result with locally cached item information.
		/// </summary>
		void UpdateResult(ItemIdentifier item, ItemIdentifier result, int error)
		{
			result.ItemName     = item.ItemName;
			result.ItemPath     = item.ItemPath;
			result.ClientHandle = item.ClientHandle;
			result.ServerHandle = item.ServerHandle;

			if (error >= 0 && item.ServerHandle != null)
			{
				// lookup locally cached item id.
				ItemIdentifier itemID = (ItemIdentifier)m_items[item.ServerHandle];

				// update result with locally cached information.
				if (itemID != null)
				{
					result.ItemName     = itemID.ItemName;
					result.ItemPath     = itemID.ItemPath;
					result.ClientHandle = itemID.ClientHandle;
				}
			}
		}

		/// <summary>
		/// Adds the actual start/end times to a result collection. 
		/// </summary>
		void UpdateActualTimes(
			IActualTime[] results,
			OPCHDA_TIME   pStartTime, 
			OPCHDA_TIME   pEndTime)
		{
			// unmarshal actual times from input arguments.
			DateTime startTime = OpcCom.Interop.GetFILETIME(OpcCom.Hda.Interop.Convert(pStartTime.ftTime));
			DateTime endTime   = OpcCom.Interop.GetFILETIME(OpcCom.Hda.Interop.Convert(pEndTime.ftTime));

			foreach (IActualTime result in results)
			{
				result.StartTime = startTime;
				result.EndTime   = endTime;
			}
		}

		/// <summary>
		/// Updates the attribute value objects before returing them to the client.
		/// </summary>
		ItemAttributeCollection UpdateResults(
			ItemIdentifier             item, 
			AttributeValueCollection[] attributes,
			ref IntPtr                 pErrors)
		{
			// unmarshal return parameters and free memory.
			int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, attributes.Length, true);

			// verify return parameters.
			if (attributes == null || errors == null)
			{
				throw new InvalidResponseException();
			}
			
			// set attribute level errors.
			for (int ii = 0; ii < attributes.Length; ii++)
			{	
				attributes[ii].ResultID  = OpcCom.Interop.GetResultID(errors[ii]);
			}

			// create item level collection. 
			ItemAttributeCollection result = new ItemAttributeCollection();

			foreach (AttributeValueCollection attribute in attributes)
			{
				result.Add(attribute);
			}

			// add locally cached item information.
			UpdateResult(item, result, 0);

			// all done.
			return result;
		}
		
		/// <summary>
		/// Updates the annotation value objects before returing them to the client.
		/// </summary>
		void UpdateResults(
			ItemIdentifier[]       items, 
			ItemIdentifier[]       results,
			ref IntPtr             pErrors)
		{
			// unmarshal return parameters and free memory.
			int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, items.Length, true);

			// verify return parameters.
			if (results == null || errors == null)
			{
				throw new InvalidResponseException();
			}
			
			for (int ii = 0; ii < results.Length; ii++)
			{
				// get cached item information.
				UpdateResult(items[ii], results[ii], errors[ii]);

				// lookup the error code.
				if (typeof(IResult).IsInstanceOfType(results[ii]))
				{
					((IResult)results[ii]).ResultID = OpcCom.Interop.GetResultID(errors[ii]);
				}
			}
		}

		/// <summary>
		/// Unmarshals the errors array and updates the result objects.
		/// </summary>
		void UpdateResults(ICollection[] items, ResultCollection[] results, int count, ref IntPtr pErrors)
		{
			// unmarshal return parameters and free memory.
			int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, count, true);

			// verify return parameters.
			if (errors == null)
			{
				throw new InvalidResponseException();
			}
			
			// create result object and lookup error code.
			int index = 0;

			for (int ii = 0; ii < items.Length; ii++)
			{
				for (int jj = 0; jj < items[ii].Count; jj++)
				{
					if (index >= count)
					{
						break;
					}

					Result result = new Result(OpcCom.Interop.GetResultID(errors[index++]));
					results[ii].Add(result);
				}
			}
		}

		/// <summary>
		/// Flattens a set of item value collections into an set of single arrays.
		/// </summary>
		private int MarshalValues(
			ItemValueCollection[] items,
			ref int[]             handles,
			ref object[]          values,
			ref int[]             qualities,
			ref DateTime[]        timestamps)
		{

			// determine the total length.
			int count = GetCount(items);

			// flatten out list of collections into a set of single arrays. 
			handles    = new int[count];
			timestamps = new DateTime[count];
			values     = new object[count];
			qualities  = new int[count];

			// initialize input parameters.
			int[] serverHandles = GetServerHandles(items);

			int index = 0;

			for (int ii = 0; ii < items.Length; ii++)
			{
				foreach (ItemValue value in items[ii])
				{
					handles[index]    = serverHandles[ii];
					timestamps[index] = value.Timestamp;
					values[index]     = OpcCom.Interop.GetVARIANT(value.Value);
					qualities[index]  = value.Quality.GetCode();

					index++;
				}
			}

			// return the total count.
			return count;
		}

		/// <summary>
		/// Flattens a set of item time collections into an set of single arrays.
		/// </summary>
		private int MarshalTimestamps(
			ItemTimeCollection[] items,
			ref int[]            handles,
			ref DateTime[]       timestamps)
		{

			// determine the total length.
			int count = GetCount(items);

			// flatten out list of collections into a set of single arrays. 
			handles    = new int[count];
			timestamps = new DateTime[count];

			// initialize input parameters.
			int[] serverHandles = GetServerHandles(items);

			int index = 0;

			for (int ii = 0; ii < items.Length; ii++)
			{
				foreach (DateTime value in items[ii])
				{
					handles[index]    = serverHandles[ii];
					timestamps[index] = value;

					index++;
				}
			}

			// return the total count.
			return count;
		}

		/// <summary>
		/// Marshals a set of annotation collections into an set of arrays.
		/// </summary>
		private int MarshalAnnotatations(
			AnnotationValueCollection[] items,
			ref int[]                   serverHandles,
			ref OPCHDA_FILETIME[]       ftTimestamps,
			ref OPCHDA_ANNOTATION[]     annotations)
		{
			// determine the total length.
			int count = GetCount(items);

			// fetch item server handles.
			int[] remoteHandles = GetServerHandles(items);

			// allocate input arrays.
			serverHandles = new int[count];
			annotations   = new OPCHDA_ANNOTATION[count];

			DateTime[] timestamps = new DateTime[count];
			
			// flatten array of collections into a single array.
			int index = 0;

			for (int ii = 0; ii < items.Length; ii++)
			{
				for (int jj = 0; jj < items[ii].Count; jj++)
				{
					serverHandles[index] = remoteHandles[ii];
					timestamps[index]    = items[ii][jj].Timestamp;

					annotations[index]   = new OPCHDA_ANNOTATION();

					annotations[index].dwNumValues      = 1;
					annotations[index].ftTimeStamps     = OpcCom.Interop.GetFILETIMEs(new DateTime[] { timestamps[jj] });
					annotations[index].szAnnotation     = OpcCom.Interop.GetUnicodeStrings(new string[] { items[ii][jj].Annotation });
					annotations[index].ftAnnotationTime = OpcCom.Interop.GetFILETIMEs(new DateTime[] { items[ii][jj].CreationTime });
					annotations[index].szUser           = OpcCom.Interop.GetUnicodeStrings(new string[] { items[ii][jj].User });

					index++;
				}
			}

			ftTimestamps = Interop.GetFILETIMEs(timestamps);	

			// return the total number of annotations.
			return count;
		}

		/// <summary>
		/// Collapses a set of result collections into a single result code.
		/// </summary>
		private IdentifiedResult[] GetIdentifiedResults(ResultCollection[] results)
		{
			// handle trival case.
			if (results == null || results.Length == 0)
			{
				return new IdentifiedResult[0];
			}

			// fetch the results from each collection.
			IdentifiedResult[] items = new IdentifiedResult[results.Length];

			for (int ii = 0; ii < results.Length; ii++)
			{
				items[ii] = new IdentifiedResult(results[ii]);

				// check if data actually exists.
				if (results[ii] == null || results[ii].Count == 0)
				{
					items[ii].ResultID = ResultID.Hda.S_NODATA;
					continue;
				}

				// start with the first result code.
				ResultID resultID = results[ii][0].ResultID;

				foreach (Result result in results[ii])
				{
					// all result in the collection should have the same error. 
					if (resultID.Code != result.ResultID.Code)
					{
						resultID = ResultID.E_FAIL;
						break;
					}
				}
			}

			// all done.
			return items;
		}
		#endregion

		#region Private Members
		private static int NextHandle = 1;
		private Hashtable m_items = new Hashtable();
		private DataCallback m_callback = new DataCallback();
		private ConnectionPoint m_connection = null;
		#endregion
	}
}
