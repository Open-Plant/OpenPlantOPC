//============================================================================
// TITLE: Server.cs
//
// CONTENTS:
// 
// An in-process wrapper for a remote OPC XML-DA server (not thread safe).
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
using System.Threading;
using System.Collections;
using System.Globalization;
using Opc;
using Opc.Da;

namespace OpcXml.Da
{
	/// <summary>
	/// An in-process wrapper for a remote OPC XML-DA server (not thread safe).
	/// </summary>
	public class  Server : Opc.Da.IServer
	{	
		//======================================================================
		// Construction

		/// <summary>
		/// Initializes the object.
		/// </summary>
		internal Server() {}
		
		//======================================================================
		// IDisposable

		/// <summary>
		/// This must be called explicitly by clients to ensure the COM server is released.
		/// </summary>
		public void Dispose() {}

		//======================================================================
		// Initialization

        /// <summary>
		/// Connects to the server with the specified URL and credentials.
		/// </summary>
		public virtual void Initialize(URL url, ConnectData connectData)
		{
			if (url == null) throw new ArgumentNullException("url");

			lock (this)
			{
				try
				{
					m_proxy             = new OpcXml.Da10.Service();
					m_proxy.Credentials = (connectData != null)?connectData.GetCredential(null, null):null;
					m_proxy.Url         = url.ToString();
					m_proxy.Proxy       = (connectData != null)?connectData.GetProxy():new WebProxy();

					m_proxy.UnsafeAuthenticatedConnectionSharing = true;
				}
       			catch (Exception e)
				{
					string msg = e.Message;
				}

				try
				{
					GetStatus();
				}
				catch (WebException e)
				{
					if (e.Status == WebExceptionStatus.ConnectFailure)
					{
						throw new ConnectFailedException(e);
					}

					throw new AccessDeniedException(e);
				}
                
				m_url = (URL)url.Clone();
			}
		}

		/// <summary>
		/// Disconnects from the server and releases all network resources.
		/// </summary>
		public void Uninitialize()
		{
			lock (this)
			{
				m_proxy = null;
			}
		}

		//======================================================================
		// IServer
		
		//======================================================================
		// Events

		/// <summary>
		/// An event to receive server shutdown notifications.
		/// </summary>
		public virtual event ServerShutdownEventHandler ServerShutdown
		{
			add    {}
			remove {}
		}

		//======================================================================
		// Localization

		/// <summary>
		/// The locale used in any error messages or results returned to the client.
		/// </summary>
		/// <returns>The locale name in the format "[languagecode]-[country/regioncode]".</returns>
		public string GetLocale()
		{
			lock (this)
			{
				if (m_proxy == null) throw new NotConnectedException();

				return m_options.Locale;
			}
		}

		/// <summary>
		/// Sets the locale used in any error messages or results returned to the client.
		/// </summary>
		/// <param name="locale">The locale name in the format "[languagecode]-[country/regioncode]".</param>
		/// <returns>A locale that the server supports and is the best match for the requested locale.</returns>
		public string SetLocale(string locale)
		{
			lock (this)
			{
				if (m_proxy == null) throw new NotConnectedException();
				
				m_options.Locale = locale;
				return m_options.Locale;
			}
		}

		/// <summary>
		/// Returns the locales supported by the server
		/// </summary>
		/// <remarks>The first element in the array must be the default locale for the server.</remarks>
		/// <returns>An array of locales with the format "[languagecode]-[country/regioncode]".</returns>
		public string[] GetSupportedLocales() 
		{ 
			lock (this)
			{
				if (m_proxy == null) throw new NotConnectedException();

				OpcXml.Da10.ServerStatus status = null;
				
				OpcXml.Da10.ReplyBase reply = m_proxy.GetStatus(
					m_options.Locale,
					null,
					out status);

				if (status != null && status.SupportedLocaleIDs != null) 
				{
					ArrayList locales = new ArrayList();

					foreach (string locale in status.SupportedLocaleIDs)
					{
						if (locale != null) locales.Add(locale);
					}

					return (string[])locales.ToArray(typeof(string));
				}

				return null;
			}
		}

		/// <summary>
		/// Returns the localized text for the specified result code.
		/// </summary>
		/// <param name="locale">The locale name in the format "[languagecode]-[country/regioncode]".</param>
		/// <param name="resultID">The result code identifier.</param>
		/// <returns>A message localized for the best match for the requested locale.</returns>
		public string GetErrorText(string locale, ResultID resultID)
		{ 
			lock (this)
			{
				if (m_proxy == null) throw new NotConnectedException();

				// fetch list of known locales.
				ArrayList knownLocales = new ArrayList();
				knownLocales.AddRange(m_messageTables.Keys);

				// select best matching locale. 
				string revisedLocale = Opc.Da.Server.FindBestLocale(locale, (string[])knownLocales.ToArray(typeof(string)));

				// find the message table for the locale.
				Hashtable messageTable = (Hashtable)m_messageTables[revisedLocale];

				// lookup the error message.
				if (messageTable != null)
				{
					return (string)messageTable[resultID];
				}

				// no matching message found.
				return null;
			}
		}	

		//======================================================================
		// Result Filters

		/// <summary>
		/// Returns the filters applied by the server to any item results returned to the client.
		/// </summary>
		/// <returns>A bit mask indicating which fields should be returned in any item results.</returns>
		public int GetResultFilters()
		{
			lock (this)
			{
				if (m_proxy == null) throw new NotConnectedException();

				return m_options.Filters;
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
				if (m_proxy == null) throw new NotConnectedException();

				m_options.Filters = filters;
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
				if (m_proxy == null) throw new NotConnectedException();

				OpcXml.Da10.RequestOptions options      = OpcXml.Da10.Request.GetRequestOptions(m_options.Locale, m_options.Filters);
				OpcXml.Da10.ServerStatus   remoteStatus = null;
				
				OpcXml.Da10.ReplyBase reply = m_proxy.GetStatus(
					options.LocaleID,
					options.ClientRequestHandle,
					out remoteStatus);

				CacheResponse(m_options.Locale, reply, null);

				// fill in the last update time.
				ServerStatus status = OpcXml.Da10.Request.GetServerStatus(reply, remoteStatus);
				
				status.LastUpdateTime = m_lastUpdateTime;
				
				return status;
			}
		}

		//======================================================================
		// Read

		/// <summary>
		/// Reads the current values for a set of items. 
		/// </summary>
		/// <param name="items">The set of items to read.</param>
		/// <returns>The results of the read operation for each item.</returns>
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

				OpcXml.Da10.RequestOptions      options     = OpcXml.Da10.Request.GetRequestOptions(m_options.Locale, m_options.Filters);
				OpcXml.Da10.ReadRequestItemList requestList = OpcXml.Da10.Request.GetItemList(list);
				OpcXml.Da10.ReplyItemList       replyList   = null;
				OpcXml.Da10.OPCError[]          errors      = null;
				
				OpcXml.Da10.ReplyBase reply = m_proxy.Read(
					options,
					requestList,
					out replyList,
					out errors);

				CacheResponse(m_options.Locale, reply, errors);

				ItemValueResultList valueList = OpcXml.Da10.Request.GetResultList(replyList);
				
				if (valueList == null)
				{
					throw new InvalidResponseException();
				}

				return (ItemValueResult[])valueList.ToArray(typeof(ItemValueResult));
			}
		}

		//======================================================================
		// Write

		/// <summary>
		/// Writes the value, quality and timestamp for a set of items.
		/// </summary>
		/// <param name="items">The set of item values to write.</param>
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

				OpcXml.Da10.RequestOptions       options     = OpcXml.Da10.Request.GetRequestOptions(m_options.Locale, m_options.Filters);
				OpcXml.Da10.WriteRequestItemList requestList = OpcXml.Da10.Request.GetItemValueList(list);
				OpcXml.Da10.ReplyItemList        replyList   = null;
				OpcXml.Da10.OPCError[]           errors      = null;
				
				OpcXml.Da10.ReplyBase reply = m_proxy.Write(
					options,
					requestList,
					false,
					out replyList,
					out errors);

				CacheResponse(m_options.Locale, reply, errors);

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
		// CreateSubscription

		/// <summary>
		/// Creates a new subscription.
		/// </summary>
		/// <param name="state">The initial state of the subscription.</param>
		/// <returns>The new subscription object.</returns>
		public ISubscription CreateSubscription(Opc.Da.SubscriptionState state)
		{
			if (state == null)   throw new ArgumentNullException("state");	
			if (m_proxy == null) throw new NotConnectedException();

			lock (this)
			{					
				return new Subscription(this, m_proxy, state, m_options.Filters);
			}
		}
		
		//======================================================================
		// CancelSubscription

		/// <summary>
		/// Creates a new instance of the appropriate subcription object.
		/// </summary>
		/// <param name="subscription">The remote subscription object.</param>
		public void CancelSubscription(ISubscription subscription)
		{
			if (subscription == null) throw new ArgumentNullException("subscription");	

			lock (this)
			{	
				if (m_proxy == null) throw new NotConnectedException();

				if (!typeof(OpcXml.Da.Subscription).IsInstanceOfType(subscription))
				{
					throw new ArgumentException("Incorrect object type.", "subscription");	
				}

				if (!this.Equals(((OpcXml.Da.Subscription)subscription).Server))
				{
					throw new ArgumentException("Unknown subscription.", "subscription");	
				}

				subscription.Dispose();
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
		public BrowseElement[] Browse(
			ItemIdentifier            itemID,
			BrowseFilters             filters, 
			out Opc.Da.BrowsePosition position)
		{	
			position = null;

			lock (this)
			{
				if (m_proxy == null) throw new NotConnectedException();

				// use default filters if none specified.
				if (filters == null) filters = new BrowseFilters();

				BrowsePosition pos = new BrowsePosition(itemID, filters, m_options.Locale, ((m_options.Filters & (int)ResultFilter.ErrorText) != 0));

				OpcXml.Da10.BrowseElement[] elements = null;
				OpcXml.Da10.OPCError[]      errors   = null;
				
				OpcXml.Da10.ReplyBase reply = m_proxy.Browse(
					OpcXml.Da10.Request.GetPropertyNames(filters.PropertyIDs),
					pos.Locale,
					"",
					(itemID != null)?itemID.ItemPath:null,
					(itemID != null)?itemID.ItemName:null,
					ref pos.ContinuationPoint,
					filters.MaxElementsReturned,
					OpcXml.Da10.Request.GetBrowseFilter(filters.BrowseFilter),
					filters.ElementNameFilter,
					filters.VendorFilter,
					filters.ReturnAllProperties,
					filters.ReturnPropertyValues,
					pos.ReturnErrorText,
					out elements,
					out errors,
					out pos.MoreElements);

				CacheResponse(pos.Locale, reply, errors);

				if (pos.MoreElements || (pos.ContinuationPoint != null &&  pos.ContinuationPoint != ""))
				{
					position = pos;
				}

				return OpcXml.Da10.Request.GetBrowseElements(elements);
			}
		}

		//======================================================================
		// BrowseNext

		/// <summary>
		/// Continues a browse operation with previously specified search criteria.
		/// </summary>
		/// <param name="position">An object containing the browse operation state information.</param>
		/// <returns>The set of elements found.</returns>
		public BrowseElement[] BrowseNext(ref Opc.Da.BrowsePosition position)
		{
			if (position == null) throw new ArgumentNullException("position");
			if (m_proxy == null)  throw new NotConnectedException();			

			lock (this)
			{
				BrowsePosition pos = (BrowsePosition)position;

				if (pos.ContinuationPoint == null || pos.ContinuationPoint == "")
				{
					throw new BrowseCannotContinueException();
				}

				OpcXml.Da10.BrowseElement[] elements = null;
				OpcXml.Da10.OPCError[]      errors   = null;
				
				OpcXml.Da10.ReplyBase reply = m_proxy.Browse(
					OpcXml.Da10.Request.GetPropertyNames(pos.Filters.PropertyIDs),
					pos.Locale,
					"",
					(pos.ItemID != null)?pos.ItemID.ItemPath:null,
					(pos.ItemID != null)?pos.ItemID.ItemName:null,
					ref pos.ContinuationPoint,
					pos.Filters.MaxElementsReturned,
					OpcXml.Da10.Request.GetBrowseFilter(pos.Filters.BrowseFilter),
					pos.Filters.ElementNameFilter,
					pos.Filters.VendorFilter,
					pos.Filters.ReturnAllProperties,
					pos.Filters.ReturnPropertyValues,
					pos.ReturnErrorText,
					out elements,
					out errors,
					out pos.MoreElements);

				CacheResponse(pos.Locale, reply, errors);

				position = null;

				if (pos.MoreElements || (pos.ContinuationPoint != null &&  pos.ContinuationPoint != ""))
				{
					position = pos;
				}

				return OpcXml.Da10.Request.GetBrowseElements(elements);
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
		public ItemPropertyCollection[] GetProperties(
			ItemIdentifier[] itemIDs,
			PropertyID[]     propertyIDs,
			bool             returnValues)
		{
			if (itemIDs == null) throw new ArgumentNullException("itemIDs");
			if (m_proxy == null) throw new NotConnectedException();			

			lock (this)
			{
				OpcXml.Da10.RequestOptions      options    = OpcXml.Da10.Request.GetRequestOptions(m_options.Locale, m_options.Filters);
				OpcXml.Da10.PropertyReplyList[] properties = null;
				OpcXml.Da10.OPCError[]          errors     = null;
				
				OpcXml.Da10.ReplyBase reply = m_proxy.GetProperties(
					OpcXml.Da10.Request.GetItemIdentifiers(itemIDs),
					OpcXml.Da10.Request.GetPropertyNames(propertyIDs),
					options.LocaleID,
					options.ClientRequestHandle,
					null,
					(propertyIDs == null),
					returnValues,
					options.ReturnErrorText,
					out properties,
					out errors);

				CacheResponse(options.LocaleID, reply, errors);

				return OpcXml.Da10.Request.GetItemPropertyCollections(properties);
			}
		}

		//======================================================================
		// Private Members

		/// <summary>
		/// The autogenerated proxy object for the XML-DA 1.0 web service.
		/// </summary>
		private OpcXml.Da10.Service m_proxy = null;
	
		/// <summary>
		/// The URL referencing the web service.
		/// </summary>
		private URL m_url = null;

		/// <summary>
		/// The default reqeust options for the server.
		/// </summary>
		private RequestOptions m_options = new RequestOptions();	
					
		/// <summary>
		/// Keeps track of the difference between the server clock and the local clock.
		/// </summary>
		private TimeSpan m_timebias = TimeSpan.Zero;

		/// <summary>
		/// The last time an update was received from the server.
		/// </summary>
		private DateTime m_lastUpdateTime = DateTime.MinValue;

		/// <summary>
		/// Returns an estimate of the UTC time at the server.
		/// </summary>
		internal DateTime ServerTime {get{lock (this){ return DateTime.Now.Add(m_timebias); }}}

		/// <summary>
		/// Stores tables of error messages indexed by locale.
		/// </summary>
		private Hashtable m_messageTables = new Hashtable();

		//======================================================================
		// Private Methods

		/// <summary>
		/// Caches error messages and request statistics after each request.
		/// </summary>
		internal void CacheResponse(
			string                 locale, 
			OpcXml.Da10.ReplyBase  reply, 
			OpcXml.Da10.OPCError[] errors)
		{
			lock (this)
			{			
				if (reply != null)
				{
					// check for revised locale id.
					if (reply.RevisedLocaleID != null)
					{
						locale = reply.RevisedLocaleID;
					}

					// calculate the bias to use when calculating request timeouts.
					m_timebias = reply.ReplyTime.Subtract(DateTime.Now);
				}

				if (errors != null && errors.Length > 0)
				{
					// check for null locale.
					if (locale == null) { locale = ""; }

					// find the message table for the locale.
					Hashtable messageTable = (Hashtable)m_messageTables[locale];

					if (messageTable == null)
					{
						m_messageTables[locale] = messageTable = new Hashtable();
					}

					// index message texts by message id.
					foreach (OpcXml.Da10.OPCError error in errors)
					{
						messageTable[OpcXml.Da10.Request.GetResultID(error.ID)] = error.Text;
					}
				}
			}
		}

		/// <summary>
		/// Called when a subscription receives a polled refresh response.
		/// </summary>
		internal void PollCompleted(
			string                 localeID, 
			OpcXml.Da10.ReplyBase  reply, 
			OpcXml.Da10.OPCError[] errors)
		{
			lock (this)
			{ 
				CacheResponse(localeID, reply, errors);
				m_lastUpdateTime = reply.ReplyTime;
			}
		}
	}
}
