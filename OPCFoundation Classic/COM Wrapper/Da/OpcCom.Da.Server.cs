//============================================================================
// TITLE: Opc.Da.Server.cs
//
// CONTENTS:
// 
//  A .NET wrapper for a COM server that implements the DA group interfaces.
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
// 2004/11/11 RSA   Added a base interfaces for BrowsePosition.

using System;
using System.Xml;
using System.Net;
using System.Threading;
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Runtime.InteropServices;
using Opc;
using Opc.Da;
using OpcRcw.Da;
using OpcRcw.Comn;

namespace OpcCom.Da
{
	/// <summary>
	/// A .NET wrapper for a COM server that implements the DA server interfaces.
	/// </summary>
	public class  Server : OpcCom.Server, Opc.Da.IServer
	{	
		#region Constructors
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
                        if (m_server != null)
                        {
                            // release all groups.
                            foreach (Subscription subscription in m_subscriptions.Values)
                            {
                                // dispose of the subscription object (disconnects all group connections).
                                subscription.Dispose();

                                // remove group from server.
                                try
                                {
                                    SubscriptionState state = subscription.GetState();
                                    ((IOPCServer)m_server).RemoveGroup((int)state.ServerHandle, 0);
                                }
                                catch { }
                            }

                            // clear subscription table.
                            m_subscriptions.Clear();
                        }
                    }

                    // Release unmanaged resources.
                    // Set large fields to null.

                    if (m_server != null)
                    {
                        // release the COM server.
                        OpcCom.Interop.ReleaseServer(m_server);
                        m_server = null;
                    }
                }

                // Call Dispose on your base class.
                m_disposed = true;
            }

            base.Dispose(disposing);
        }

        private bool m_disposed = false;
        #endregion

		#region OpcCom.Server Overrides
		/// <summary>
		/// Returns the localized text for the specified result code.
		/// </summary>
		/// <param name="locale">The locale name in the format "[languagecode]-[country/regioncode]".</param>
		/// <param name="resultID">The result code identifier.</param>
		/// <returns>A message localized for the best match for the requested locale.</returns>
		public override string GetErrorText(string locale, ResultID resultID)
		{ 
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					string errorText = null;

					((IOPCServer)m_server).GetErrorString(
						resultID.Code, 
						OpcCom.Interop.GetLocale(locale),
						out errorText);
					
					return errorText;
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCServer.GetErrorString", e);
				}
			}
		}	
		#endregion

		#region Opc.Da.IServer Members
		/// <summary>
		/// Returns the filters applied by the server to any item results returned to the client.
		/// </summary>
		/// <returns>A bit mask indicating which fields should be returned in any item results.</returns>
		public int GetResultFilters()
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();
				return m_filters;
			}
		}
		
		/// <summary>
		/// Sets the filters applied by the server to any item results returned to the client.
		/// </summary>
		/// <param name="filters">A bit mask indicating which fields should be returned in any item results.</param>
		public void SetResultFilters(int filters)
		{ 
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();
				m_filters = filters;
			}
		}

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

				// invoke COM method.
				try
				{
					((IOPCServer)m_server).GetStatus(out pStatus);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCServer.GetStatus", e);
				}		
				
				// return status.
				return OpcCom.Da.Interop.GetServerStatus(ref pStatus, true);
			}
		}

		//======================================================================
		// Read

		/// <summary>
		/// Reads the current values for a set of items. 
		/// </summary>
		/// <param name="items">The set of items to read.</param>
		/// <returns>The results of the read operation for each item.</returns>
		public virtual ItemValueResult[] Read(Item[] items)
		{
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				int count = items.Length;
				if (count == 0) throw new ArgumentOutOfRangeException("items.Length", "0");

				// initialize arguments.
				string[] itemIDs = new string[count];
				int[]    maxAges = new int[count];

				for (int ii = 0; ii < count; ii++)
				{
					itemIDs[ii] = items[ii].ItemName;
					maxAges[ii] = (items[ii].MaxAgeSpecified)?items[ii].MaxAge:0;
				}

				IntPtr pValues     = IntPtr.Zero;
				IntPtr pQualities  = IntPtr.Zero;
				IntPtr pTimestamps = IntPtr.Zero;
				IntPtr pErrors     = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCItemIO)m_server).Read(
						count,
						itemIDs,
						maxAges,
						out pValues,
						out pQualities,
						out pTimestamps,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCItemIO.Read", e);
				}

				// unmarshal results.
				object[]   values     = OpcCom.Interop.GetVARIANTs(ref pValues, count, true);
				short[]    qualities  = OpcCom.Interop.GetInt16s(ref pQualities, count, true);
				DateTime[] timestamps = OpcCom.Interop.GetFILETIMEs(ref pTimestamps, count, true);
				int[]      errors     = OpcCom.Interop.GetInt32s(ref pErrors, count, true);

				// pre-fetch the current locale to use for data conversions.
				string locale = GetLocale();

				// construct result array.
				ItemValueResult[] results = new ItemValueResult[count];

				for (int ii = 0; ii < results.Length; ii++)
				{
					results[ii] = new ItemValueResult(items[ii]);
 
					results[ii].ServerHandle       = null;
					results[ii].Value              = values[ii];                  
					results[ii].Quality            = new Opc.Da.Quality(qualities[ii]);
					results[ii].QualitySpecified   = true;
					results[ii].Timestamp          = timestamps[ii];
					results[ii].TimestampSpecified = timestamps[ii] != DateTime.MinValue;
					results[ii].ResultID           = OpcCom.Interop.GetResultID(errors[ii]);
					results[ii].DiagnosticInfo     = null;

					// convert COM code to unified DA code.
					if (errors[ii] == ResultIDs.E_BADRIGHTS) { results[ii].ResultID = new ResultID(ResultID.Da.E_WRITEONLY, ResultIDs.E_BADRIGHTS); }

					// convert the data type since the server does not support the feature.
					if (results[ii].Value != null && items[ii].ReqType != null)
					{
						try
						{
							results[ii].Value = ChangeType(values[ii], items[ii].ReqType, locale);
						}
						catch (Exception e)
						{
							results[ii].Value              = null;
							results[ii].Quality            = Quality.Bad;
							results[ii].QualitySpecified   = true;
							results[ii].Timestamp          = DateTime.MinValue;
							results[ii].TimestampSpecified = false;

							if (e.GetType() == typeof(System.OverflowException))
							{
								results[ii].ResultID = OpcCom.Interop.GetResultID(ResultIDs.E_RANGE);
							}
							else
							{
								results[ii].ResultID = OpcCom.Interop.GetResultID(ResultIDs.E_BADTYPE);
							}
						}
					}

					// apply request options.
					if ((m_filters & (int)ResultFilter.ItemName) == 0)     results[ii].ItemName     = null;
					if ((m_filters & (int)ResultFilter.ItemPath) == 0)     results[ii].ItemPath     = null;
					if ((m_filters & (int)ResultFilter.ClientHandle) == 0) results[ii].ClientHandle = null;
					
					if ((m_filters & (int)ResultFilter.ItemTime) == 0) 
					{
						results[ii].Timestamp = DateTime.MinValue;
						results[ii].TimestampSpecified = false;
					}       
				}

				// return results.
				return results;
			}
		}

		//======================================================================
		// Write

		/// <summary>
		/// Writes the value, quality and timestamp for a set of items.
		/// </summary>
		/// <param name="items">The set of item values to write.</param>
		/// <returns>The results of the write operation for each item.</returns>
		public virtual IdentifiedResult[] Write(ItemValue[] items)
		{
			if (items == null) throw new ArgumentNullException("items");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				int count = items.Length;
				if (count == 0) throw new ArgumentOutOfRangeException("items.Length", "0");

				// initialize arguments.
				string[] itemIDs = new string[count];

				for (int ii = 0; ii < count; ii++)
				{
					itemIDs[ii] = items[ii].ItemName;
				}

				OpcRcw.Da.OPCITEMVQT[] values = OpcCom.Da.Interop.GetOPCITEMVQTs(items);

				IntPtr pErrors = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCItemIO)m_server).WriteVQT(
						count,
						itemIDs,
						values,
						out pErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCItemIO.Read", e);
				}

				// unmarshal results.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, count, true);

				// construct result array.
				IdentifiedResult[] results = new IdentifiedResult[count];

				for (int ii = 0; ii < count; ii++)
				{
					results[ii] = new IdentifiedResult(items[ii]);
					
					results[ii].ServerHandle   = null;
					results[ii].ResultID       = OpcCom.Interop.GetResultID(errors[ii]);
					results[ii].DiagnosticInfo = null;

					// convert COM code to unified DA code.
					if (errors[ii] == ResultIDs.E_BADRIGHTS) { results[ii].ResultID = new ResultID(ResultID.Da.E_READONLY, ResultIDs.E_BADRIGHTS); }

					// apply request options.
					if ((m_filters & (int)ResultFilter.ItemName) == 0)     results[ii].ItemName     = null;
					if ((m_filters & (int)ResultFilter.ItemPath) == 0)     results[ii].ItemPath     = null;
					if ((m_filters & (int)ResultFilter.ClientHandle) == 0) results[ii].ClientHandle = null;
				}

				// return results.
				return results;				
			}
		}

		//======================================================================
		// CreateSubscription

		/// <summary>
		/// Creates a new subscription.
		/// </summary>
		/// <param name="state">The initial state of the subscription.</param>
		/// <returns>The new subscription object.</returns>
		public ISubscription CreateSubscription(SubscriptionState state)
		{
			if (state == null) throw new ArgumentNullException("state");

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// copy the subscription state.
				SubscriptionState result = (Opc.Da.SubscriptionState)state.Clone();

				// initialize arguments.
				Guid   iid   = typeof(IOPCItemMgt).GUID;
				object group = null;
			
				int serverHandle      = 0;
				int revisedUpdateRate = 0;

				GCHandle hDeadband = GCHandle.Alloc(result.Deadband, GCHandleType.Pinned);

				// invoke COM method.
				try
				{
					((IOPCServer)m_server).AddGroup(
						(result.Name != null)?result.Name:"",
						(result.Active)?1:0,
						result.UpdateRate,
						0,
						IntPtr.Zero,
						hDeadband.AddrOfPinnedObject(),
						OpcCom.Interop.GetLocale(result.Locale),
						out serverHandle,
						out revisedUpdateRate,
						ref iid,
						out group);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCServer.AddGroup", e);
				}
				finally
				{
					if (hDeadband.IsAllocated) hDeadband.Free();
				}

				// set the keep alive rate if requested.
				try
				{
					int keepAlive = 0;
					((IOPCGroupStateMgt2)group).SetKeepAlive(result.KeepAlive, out keepAlive);
					result.KeepAlive = keepAlive;
				}
				catch
				{
					result.KeepAlive = 0;
				}

				// save server handle.
				result.ServerHandle = serverHandle;

				// set the revised update rate.
				if (revisedUpdateRate > result.UpdateRate)
				{
					result.UpdateRate = revisedUpdateRate;
				}

				// create the subscription object.
				OpcCom.Da.Subscription subscription = CreateSubscription(group, result, m_filters);

				// index by server handle.
				m_subscriptions[serverHandle] = subscription;

				// return subscription.
				return subscription;
			}
		}
		
		//======================================================================
		// CancelSubscription

		/// <summary>
		/// Cancels a subscription and releases all resources allocated for it.
		/// </summary>
		/// <param name="subscription">The subscription to cancel.</param>
		public void CancelSubscription(ISubscription subscription)
		{
			if (subscription == null) throw new ArgumentNullException("subscription");	

			lock (this)
			{	
				if (m_server == null) throw new NotConnectedException();

				// validate argument.
				if (!typeof(OpcCom.Da.Subscription).IsInstanceOfType(subscription))
				{
					throw new ArgumentException("Incorrect object type.", "subscription");	
				}

				// get the subscription state.
				SubscriptionState state = subscription.GetState();

				if (!m_subscriptions.ContainsKey(state.ServerHandle))
				{
					throw new ArgumentException("Handle not found.", "subscription");	
				}

				m_subscriptions.Remove(state.ServerHandle);

				// release all subscription resources.
				subscription.Dispose();

				// invoke COM method.
				try
				{
					((IOPCServer)m_server).RemoveGroup((int)state.ServerHandle, 0);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCServer.RemoveGroup", e);
				}
			}
		}
		
		//======================================================================
		// Browse

		/// <summary>
		/// Fetches the children of a branch that meet the filter criteria.
		/// </summary>
		/// <param name="itemID">The identifier of branch which is the target of the search.</param>
		/// <param name="filters">The filters to use to limit the set of child elements returned.</param>
		/// <param name="position">An object used to continue a browse that could not be completed.</param>
		/// <returns>The set of elements found.</returns>
		public virtual BrowseElement[] Browse(
			ItemIdentifier            itemID,
			BrowseFilters             filters, 
			out Opc.Da.BrowsePosition position)
		{		
			if (filters == null) throw new ArgumentNullException("filters");	

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();	
				
				position = null;

				// initialize arguments.
				int count = 0;
				int moreElements = 0;

				IntPtr pContinuationPoint = IntPtr.Zero;
				IntPtr pElements          = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCBrowse)m_server).Browse(
						(itemID != null && itemID.ItemName != null)?itemID.ItemName:"",
						ref pContinuationPoint,
						filters.MaxElementsReturned,
						OpcCom.Da.Interop.GetBrowseFilter(filters.BrowseFilter),
						(filters.ElementNameFilter != null)?filters.ElementNameFilter:"",
						(filters.VendorFilter != null)?filters.VendorFilter:"",
						(filters.ReturnAllProperties)?1:0,
						(filters.ReturnPropertyValues)?1:0,
						(filters.PropertyIDs != null)?filters.PropertyIDs.Length:0,
						OpcCom.Da.Interop.GetPropertyIDs(filters.PropertyIDs),
						out moreElements,
						out count,
						out pElements);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCBrowse.Browse", e);
				}		

				// unmarshal results.
				BrowseElement[] elements = OpcCom.Da.Interop.GetBrowseElements(ref pElements, count, true);

				string continuationPoint = Marshal.PtrToStringUni(pContinuationPoint);
				Marshal.FreeCoTaskMem(pContinuationPoint);

				// check if more results exist.
				if (moreElements != 0 || (continuationPoint != null && continuationPoint != ""))
				{
					// allocate new browse position object.
					position = new OpcCom.Da.BrowsePosition(itemID, filters, continuationPoint);
				}

				// process results.
				ProcessResults(elements, filters.PropertyIDs);

				return elements;
			}
		}

		//======================================================================
		// BrowseNext

		/// <summary>
		/// Continues a browse operation with previously specified search criteria.
		/// </summary>
		/// <param name="position">An object containing the browse operation state information.</param>
		/// <returns>The set of elements found.</returns>
		public virtual BrowseElement[] BrowseNext(ref Opc.Da.BrowsePosition position)
		{		
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// check for valid position object.
				if (position == null || position.GetType() != typeof(OpcCom.Da.BrowsePosition))
				{
					throw new BrowseCannotContinueException();
				}
	
				OpcCom.Da.BrowsePosition pos = (OpcCom.Da.BrowsePosition)position;
			
				// check for valid continuation point.
				if (pos == null || pos.ContinuationPoint == null || pos.ContinuationPoint == "")
				{
					throw new BrowseCannotContinueException();
				}

				// initialize arguments.
				int count = 0;
				int moreElements = 0;

				ItemIdentifier itemID  = ((BrowsePosition)position).ItemID;
				BrowseFilters  filters = ((BrowsePosition)position).Filters;

				IntPtr pContinuationPoint = Marshal.StringToCoTaskMemUni(pos.ContinuationPoint);
				IntPtr pElements          = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCBrowse)m_server).Browse(
						(itemID != null && itemID.ItemName != null)?itemID.ItemName:"",
						ref pContinuationPoint,
						filters.MaxElementsReturned,
						OpcCom.Da.Interop.GetBrowseFilter(filters.BrowseFilter),
						(filters.ElementNameFilter != null)?filters.ElementNameFilter:"",
						(filters.VendorFilter != null)?filters.VendorFilter:"",
						(filters.ReturnAllProperties)?1:0,
						(filters.ReturnPropertyValues)?1:0,
						(filters.PropertyIDs != null)?filters.PropertyIDs.Length:0,
						OpcCom.Da.Interop.GetPropertyIDs(filters.PropertyIDs),
						out moreElements,
						out count,
						out pElements);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCBrowse.BrowseNext", e);
				}		

				// unmarshal results.
				BrowseElement[] elements = OpcCom.Da.Interop.GetBrowseElements(ref pElements, count, true);

				pos.ContinuationPoint = Marshal.PtrToStringUni(pContinuationPoint);
				Marshal.FreeCoTaskMem(pContinuationPoint);

				// check if more no results exist.
				if (moreElements == 0 && (pos.ContinuationPoint == null || pos.ContinuationPoint == ""))
				{
					position = null;
				}	

				// process results.
				ProcessResults(elements, filters.PropertyIDs);

				return elements;
			}
		}

		//======================================================================
		// GetProperties

		/// <summary>
		/// Returns the item properties for a set of items.
		/// </summary>
		/// <param name="itemIDs">A list of item identifiers.</param>
		/// <param name="propertyIDs">A list of properties to fetch for each item.</param>
		/// <param name="returnValues">Whether the property values should be returned with the properties.</param>
		/// <returns>A list of properties for each item.</returns>
		public virtual ItemPropertyCollection[] GetProperties(
			ItemIdentifier[] itemIDs,
			PropertyID[]     propertyIDs,
			bool             returnValues)
		{		
			if (itemIDs == null) throw new ArgumentNullException("itemIDs");	

			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				string[] pItemIDs = new string[itemIDs.Length];

				for (int ii = 0; ii < itemIDs.Length; ii++)
				{
					pItemIDs[ii] = itemIDs[ii].ItemName;
				}

				IntPtr pPropertyLists = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCBrowse)m_server).GetProperties(
						itemIDs.Length,
						pItemIDs,
						(returnValues)?1:0,
						(propertyIDs != null)?propertyIDs.Length:0,
						OpcCom.Da.Interop.GetPropertyIDs(propertyIDs),
						out pPropertyLists);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCBrowse.GetProperties", e);
				}		

				// unmarshal results.
				ItemPropertyCollection[] resultLists = OpcCom.Da.Interop.GetItemPropertyCollections(ref pPropertyLists, itemIDs.Length, true);

				// replace integer codes with qnames passed in.
				if (propertyIDs != null && propertyIDs.Length > 0)
				{
					foreach (ItemPropertyCollection resultList in resultLists)
					{
						for (int ii = 0; ii < resultList.Count; ii++)
						{
							resultList[ii].ID = propertyIDs[ii];
						}
					}
				}

				// return the results.
				return resultLists;
			}
		}
		#endregion
	
		#region Private Methods
		/// <summary>
		/// Converts a value to the specified type using the specified locale.
		/// </summary>
		protected object ChangeType(object source, System.Type type, string locale)
		{
			CultureInfo culture = Thread.CurrentThread.CurrentCulture;

			// override the current thread culture to ensure conversions happen correctly.
			try
			{
				Thread.CurrentThread.CurrentCulture = new CultureInfo(locale);
			}
			catch
			{
				Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			}

			try
			{
				object result = Opc.Convert.ChangeType(source, type);

				// check for overflow converting to float.
				if (typeof(Single) == type)
				{
					if (Single.IsInfinity(System.Convert.ToSingle(result)))
					{
						throw new System.OverflowException();
					}
				}

				return result;
			}

				// restore the current thread culture after conversion.
			finally
			{
				Thread.CurrentThread.CurrentCulture = culture;
			}
		}

		/// <summary>
		/// Creates a new instance of a subscription.
		/// </summary>
		protected virtual OpcCom.Da.Subscription CreateSubscription(
			object            group, 
			SubscriptionState state, 
			int               filters)
		{
			return new OpcCom.Da.Subscription(group, state, filters);
		}

		/// <summary>
		/// Updates the properties to convert COM values to OPC .NET API results.
		/// </summary>
		private void ProcessResults(BrowseElement[] elements, PropertyID[] propertyIDs)
		{
			// check for null.
			if (elements == null)
			{
				return;
			}

			// process each element.
			foreach (BrowseElement element in elements)
			{
				// check if no properties.
				if (element.Properties == null)
				{
					continue;
				}

				// process each property.
				foreach (ItemProperty property in element.Properties)
				{
					// replace the property ids which on contain the codes with the proper qualified names passed in.
					if (propertyIDs != null)
					{
						foreach (PropertyID propertyID in propertyIDs)
						{
							if (property.ID.Code == propertyID.Code)
							{
								property.ID = propertyID;
								break;
							}
						}
					}
				}
			}
		}		
		#endregion

		#region Private Members
		/// <summary>
		/// The default result filters for the server.
		/// </summary>
		protected int m_filters = (int)ResultFilter.Minimal;

		/// <summary>
		/// A table of active subscriptions for the server.
		/// </summary>
		private Hashtable m_subscriptions = new Hashtable();
		#endregion
	}
}
