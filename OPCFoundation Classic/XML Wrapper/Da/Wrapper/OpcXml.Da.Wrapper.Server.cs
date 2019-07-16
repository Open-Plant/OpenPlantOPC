//============================================================================
// TITLE: Server.cs
//
// CONTENTS:
// 
// An in-process wrapper for an XML-DA server. 
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
using System.Resources;
using System.Reflection;
using System.Web.Services.Protocols;
using System.Text;
using System.Configuration;
using Opc;
using Opc.Da;

namespace OpcXml.Da.Wrapper
{
	/// <summary>
	/// A XML-DA server implementation that wraps a COM-DA server.
	/// </summary>
	public class Server : IServer
	{	
		//======================================================================
		// Construction

		/// <summary>
		/// Initializes the XML-DA server.
		/// </summary>
		public Server() 
		{
			// initialize server status.

			// fetch vendor info from application configuration.
			try
			{
				m_status.VendorInfo = (string)(new AppSettingsReader().GetValue("OpcXml.Da.Wrapper.VendorInfo", typeof(string)));
			}
			catch
			{
				m_status.VendorInfo = "OPC XML Data Access 1.00 Sample Server";
			}

			// fetch product version from application configuration.
			try
			{
				m_status.ProductVersion = (string)(new AppSettingsReader().GetValue("OpcXml.Da.Wrapper.ProductVersion", typeof(string)));
			}
			catch
			{
				m_status.ProductVersion = "1.00";
			}

			m_status.ServerState    = serverState.running;
			m_status.StatusInfo     = null;
			m_status.CurrentTime    = DateTime.Now;
			m_status.StartTime      = DateTime.Now;
			m_status.LastUpdateTime = DateTime.MinValue;

			// set the supported locales.
			m_supportedLocales = new string[] { "en", "fr", "de" };

			// create the reosurce manager.
			m_resourceManager  = new ResourceManager("OpcXml.Resources.Strings", Assembly.GetExecutingAssembly());
		}

		//======================================================================
		// IDisposable

		/// <summary>
		/// Releases any unmanaged resources used by the server.
		/// </summary>
		public void Dispose()
		{
			lock (this)
			{
				// dispose of all servers.
				foreach (Opc.Da.IServer server in m_servers.Values)
				{
					try   { server.Dispose(); }
					catch {}
				} 

				// clear server table.
				m_servers.Clear();

				// update state.
				m_status.ServerState = serverState.unknown;
			}
		}
		
		//======================================================================
		// Public Properties

		/// <summary>
		/// The names of the locales supported by the server.
		/// </summary>
		public string[] SupportedLocales {get{lock (this){ return (m_supportedLocales != null)?(string[])m_supportedLocales.Clone():null; }}}

		//======================================================================
		// OpcXml.Da.IServer

		/// <summary>
		/// Connects to the server with the specified address.
		/// </summary>
		public void WrapServer(string itemPath, Opc.Da.IServer server, bool primaryServer)
		{
			lock (this)
			{
				try
				{					
					// check the status.
					Opc.Da.ServerStatus status = server.GetStatus();

					// set default result filters.
					server.SetResultFilters((int)ResultFilter.All);

					// set default locale.
					server.SetLocale("en-US");

					// index server by item path.
					m_servers.Add(itemPath, server);

					// the primary server is used for get status requests.
					if (primaryServer)
					{
						m_primaryServer = itemPath;
					}
				}
				catch (Exception e)
				{
					m_status.ServerState = serverState.commFault;
					m_status.StatusInfo  = e.Message;
					throw e;
				}
			}
		}

		/// <summary>
		/// Returns the current server status.
		/// </summary>
		public ReplyBase GetStatus(
			string           locale,
			string           clientRequestHandle,
			out ServerStatus status)
		{
			// initialize reply.
			ReplyBase reply = CreateReply(locale, clientRequestHandle);

			try
			{
				lock (this)
				{
					// copy the current status of the wrapper.
					if (m_primaryServer == null)
					{
						status             = (ServerStatus)m_status.Clone();
						status.CurrentTime = DateTime.Now;
					}
					
					// get the status from the primary server.
					else
					{
						Opc.Da.IServer server = (Opc.Da.IServer)m_servers[m_primaryServer];
						status = server.GetStatus();
					}
								
					// update reply and return.
					reply.ReplyTime = DateTime.Now;
					return reply;
				}
			}
			catch (Exception e)
			{
				throw CreateException(reply.RevisedLocaleID, e);
			}
		}

		/// <summary>
		/// Reads a set of items.
		/// </summary>
		public ReplyBase Read(
			RequestOptions          options, 
			ItemList                requestList,
			out ItemValueResultList replyList,
			out Error[]             errors)
		{
			// initialize reply.
			ReplyBase reply = CreateReply(options.Locale, options.RequestHandle);

			try
			{
				lock (this)
				{
					// ensure server can process the request.
					CheckState(reply.RevisedLocaleID, true);

					// check deadline.
					replyList = CheckDeadline(reply.RevisedLocaleID, options.RequestDeadline, requestList);

					if (replyList != null)
					{
						errors = ApplyOptions(reply.RevisedLocaleID, options, replyList);
						reply.ReplyTime = DateTime.Now;
						return reply;
					}

					// create reply list.
					replyList = new ItemValueResultList();

					// apply list level parameters.
					ApplyItemListDefaults(requestList);

					Item[]            requestItems = (Item[])requestList.ToArray(typeof(Item));
					ItemValueResult[] replyItems   = new ItemValueResult[requestList.Count];

					// process items with recognized paths.
					foreach (string itemPath in m_servers.Keys)
					{
						Read(reply.RevisedLocaleID, itemPath, requestItems, replyItems);
					}

					// set error for items with unrecognized paths.
					for (int ii = 0; ii < replyItems.Length; ii++)
					{
						// check if item has not been processed.
						if (replyItems[ii] == null)
						{
							replyItems[ii] = new ItemValueResult(requestItems[ii], ResultID.Da.E_UNKNOWN_ITEM_PATH);
						}

						// check for bad values.
						CheckStringValue(replyItems[ii]);
					}			

					// copy result items into reply list.
					replyList.AddRange(replyItems);

					// apply request options.
					errors = ApplyOptions(reply.RevisedLocaleID, options, replyList);

					// update reply and return.
					reply.ReplyTime = DateTime.Now;
					return reply;
				}
			}
			catch (Exception e)
			{
				throw CreateException(reply.RevisedLocaleID, e);
			}
		}


		/// <summary>
		/// Writes a set of items and, if requested, returns the current values.
		/// </summary>
		public ReplyBase Write(
			RequestOptions          options, 
			ItemValueList           requestList, 
			bool                    returnValues,
			out ItemValueResultList replyList,
			out Error[]             errors)
		{
			// initialize reply.
			ReplyBase reply = CreateReply(options.Locale, options.RequestHandle);

			try
			{
				lock (this)
				{
					// ensure server can process the request.
					CheckState(reply.RevisedLocaleID, true);
				
					// check deadline.
					replyList = CheckDeadline(reply.RevisedLocaleID, options.RequestDeadline, requestList);

					if (replyList != null)
					{
						errors = ApplyOptions(reply.RevisedLocaleID, options, replyList);
						reply.ReplyTime = DateTime.Now;
						return reply;
					}

					// create reply list.
					replyList = new ItemValueResultList();

					ItemValue[]       requestItems = (ItemValue[])requestList.ToArray(typeof(ItemValue));
					ItemValueResult[] replyItems = new ItemValueResult[requestList.Count];

					// process items with recognized paths.
					foreach (string itemPath in m_servers.Keys)
					{
						Write(reply.RevisedLocaleID, itemPath, requestItems, returnValues, replyItems);
					}

					// set error for items with unrecognized paths.
					for (int ii = 0; ii < replyItems.Length; ii++)
					{
						// check if item has not been processed.
						if (replyItems[ii] == null)
						{
							replyItems[ii] = new ItemValueResult(requestItems[ii], ResultID.Da.E_UNKNOWN_ITEM_PATH);
						}

						// check for bad values.
						CheckStringValue(replyItems[ii]);
					}			

					// add items to list.
					replyList.AddRange(replyItems);

					// apply request options.
					errors = ApplyOptions(reply.RevisedLocaleID, options, replyList);
				
					// update reply and return.
					reply.ReplyTime = DateTime.Now;
					return reply;
				}
			}
			catch (Exception e)
			{
				throw CreateException(reply.RevisedLocaleID, e);
			}
		}

		#region Subscription Management
		/// <summary>
		/// Establishes a subscription for the set of items.
		/// </summary>
		public ReplyBase Subscribe(
			RequestOptions          options, 
			ItemList                requestList, 
			TimeSpan                pingTime,
			bool                    returnValues,
			out string              subscriptionID,
			out ItemValueResultList replyList,
			out Error[]             errors)
		{
			subscriptionID = null;

			// initialize reply.
			ReplyBase reply = CreateReply(options.Locale, options.RequestHandle);

			try
			{
				lock (this)
				{
					// ensure server can process the request.
					CheckState(reply.RevisedLocaleID, true);

					// check deadline.
					replyList = CheckDeadline(reply.RevisedLocaleID, options.RequestDeadline, requestList);

					if (replyList != null)
					{
						errors = ApplyOptions(reply.RevisedLocaleID, options, replyList);
						reply.ReplyTime = DateTime.Now;
						return reply;
					}

					SubscribeItemValueResult[] replyItems = new SubscribeItemValueResult[requestList.Count];
		
					// assign items to existing remote subscriptions.
					ItemResult[] results = new ItemResult[replyItems.Length];

					foreach (RemoteSubscription remoteSubscription in m_remoteSubscriptions)
					{
						remoteSubscription.AddItems(requestList, replyItems, returnValues, results);
					}

					// assign items to new remote subscriptions.
					for (int ii = 0; ii < results.Length; ii++)
					{
						RemoteSubscription remoteSubscription = new RemoteSubscription();

						bool initialized = remoteSubscription.Initialize(reply.RevisedLocaleID, m_servers, requestList, replyItems, returnValues, results);
				
						if (initialized)
						{
							m_remoteSubscriptions.Add(remoteSubscription);
							remoteSubscription.DataChanged += new DataChangedEventHandler(DataChanged);
						}
						else
						{
							remoteSubscription.Dispose();
							remoteSubscription = null;
						}
					}

					// count the number of valid items and items with information to return.
					int validItems = 0;
					int itemsToReturn = 0;

					for (int ii = 0; ii < replyItems.Length; ii++)
					{
						if (replyItems[ii].ResultID.Succeeded())
						{
							validItems++;
						}

						if (returnValues || replyItems[ii].ResultID != ResultID.S_OK || replyItems[ii].SamplingRateSpecified)
						{
							itemsToReturn++;
						}
					}

					// create reply list.
					replyList = new ItemValueResultList();

					if (itemsToReturn > 0)
					{
						// add reply items to reply list.
						replyList.AddRange(replyItems);
					}

					if (validItems > 0)
					{
						// create xml-da subscription.
						Subscription subscription = new Subscription(Guid.NewGuid().ToString(), pingTime);

						subscription.Initialize(results, replyItems);
		            
						// set a default ping time if not provided by client.
						if (pingTime == TimeSpan.Zero)
						{			
							// find the longest sampling rate.
							int samplingRate = 0;

							for (int ii = 0; ii < replyItems.Length; ii++)
							{
								// check for the revised sampling rate at the item level.
								if (replyItems[ii].SamplingRateSpecified)
								{
									if (samplingRate < replyItems[ii].SamplingRate)
									{
										samplingRate = replyItems[ii].SamplingRate;
									}
								}

								// check for the requested sampling rate at the item level.
								else if (requestList[ii].SamplingRateSpecified)
								{
									if (samplingRate < requestList[ii].SamplingRate)
									{
										samplingRate = requestList[ii].SamplingRate;
									}
								}							

								// check for the requested sampling rate at the list level.
								else if (requestList.SamplingRateSpecified)
								{
									if (samplingRate < requestList.SamplingRate)
									{
										samplingRate = requestList.SamplingRate;
									}
								}
							}
						
							// set default ping time.
							if (samplingRate < 3000)
							{
								subscription.PingTime = new TimeSpan(0, 0, 0, 0, 10000);
							}
							else
							{
								subscription.PingTime = new TimeSpan(0, 0, 0, 0, samplingRate*3);
							}
						}

						// assign a unique subscription handle and save subscription list.
						subscriptionID = subscription.Handle;
						m_subscriptions[subscriptionID] = subscription;
					}
									
					// start the ping timer - checks for expired subscriptions once per second.
					if (m_pingTimer == null)
					{
						m_pingTimer = new Timer(
							new TimerCallback(CleanupSubscriptions), 
							null, 
							1000,
							Timeout.Infinite);
					}

					// check for bad values in data stream.
					if (returnValues)
					{
						if (replyList != null)
						{
							for (int ii = 0; ii < replyList.Count; ii++)
							{
								CheckStringValue(replyList[ii]);
							}
						}
					}

					// apply request options.
					errors = ApplyOptions(reply.RevisedLocaleID, options, replyList);
			
					// update reply and return.
					reply.ReplyTime = DateTime.Now;
					return reply;
				}			
			}
			catch (Exception e)
			{
				throw CreateException(reply.RevisedLocaleID, e);
			}
		}

		/// <summary>
		/// Polls the server for the any item changes for one or more subscriptions.
		/// </summary>
		public ReplyBase PolledRefresh(
			RequestOptions            options, 
			string[]                  subscriptionIDs,
			TimeSpan                  holdTime,
			TimeSpan                  waitTime,
			bool                      returnAllValues,
			out string[]              invalidSubscriptionIDs,
			out ItemValueResultList[] replyLists,
			out Error[]               errors,
			out bool                  dataBufferOverflow)
		{
			// initialize reply.
			ReplyBase reply = CreateReply(options.Locale, options.RequestHandle);

			try
			{
				// ensure server can process the request.
				lock (this)	{ CheckState(reply.RevisedLocaleID, true); }

				// check deadline.
                CheckDeadline(reply.RevisedLocaleID, options.RequestDeadline);
                

				// check for excessive hold times.
				if (holdTime.TotalSeconds > 60)
				{
					throw CreateException(reply.RevisedLocaleID, Error.E_INVALIDHOLDTIME);
				}

				// wait for the hold time to expire.
				if (holdTime.Ticks > 0) Thread.Sleep(holdTime);

				ArrayList invalidHandles = new ArrayList();
				ArrayList changedLists   = new ArrayList();

				DateTime waitUntil = DateTime.Now.Add(waitTime);
			
				// check for data changes until the wait time expires. 
				do
				{
					lock (this)
					{
						foreach (string subscriptionID in subscriptionIDs)
						{
							// lookup subscription handle.
							Subscription subscription = (Subscription)m_subscriptions[subscriptionID];

							if (subscription == null)
							{
								invalidHandles.Add(subscriptionID);
								continue;
							}

							// fetch items from the cache for the subscription.
							ItemValueResultList changedItems = subscription.GetItemValues(returnAllValues);
						
							if (changedItems != null && changedItems.Count > 0)
							{
								changedItems.ServerHandle = subscriptionID;
								changedLists.Add(changedItems);
							}
						}
					}

					// exit loop if changes found or all handles are invalid.
					if (changedLists.Count > 0 || invalidHandles.Count == subscriptionIDs.Length)
					{
						break;
					}

					// poll subcription caches for changes every 100ms until wait time exceeded.
					Thread.Sleep(100);
				}
				while (DateTime.Now < waitUntil);

				// initialize output parameters.
				invalidSubscriptionIDs = (invalidHandles.Count > 0)?(string[])invalidHandles.ToArray(typeof(string)):null;
				replyLists             = (ItemValueResultList[])changedLists.ToArray(typeof(ItemValueResultList));
				dataBufferOverflow     = false;
			
				// check for bad values in data stream.
				for (int ii = 0; ii < replyLists.Length; ii++)
				{
					for (int jj = 0; jj < replyLists[ii].Count; jj++)
					{
						CheckStringValue(replyLists[ii][jj]);
					}
				}

				// apply request options.
				errors = ApplyOptions(reply.RevisedLocaleID, options, replyLists);

				// update reply and return.
				reply.ReplyTime = DateTime.Now;
				return reply;
			}
			catch (Exception e)
			{
				throw CreateException(reply.RevisedLocaleID, e);
			}
		}

		/// <summary>
		/// Terminates one or more subscriptions.
		/// </summary>
		public void Unsubscribe(string subscriptionID)
		{
			try
			{
				lock (this)
				{
					if (subscriptionID == null)
					{
						throw CreateException(SupportedLocales[0], Error.E_NOSUBSCRIPTION);
					}

					Subscription subscription = (Subscription)m_subscriptions[subscriptionID];

					if (subscription == null)
					{
						throw CreateException(SupportedLocales[0], Error.E_NOSUBSCRIPTION);
					}

					CancelSubscription(subscription);
				}
			}
			catch (Exception e)
			{
				throw CreateException(SupportedLocales[0], e);
			}
		}
		#endregion

		/// <summary>
		/// Returns a set of elements at the specified position and that meet the filter criteria.
		/// </summary>
		public ReplyBase Browse(
			string              locale,
			string              clientRequestHandle,
			bool                returnErrorText,
			ItemIdentifier      itemID,
			BrowseFilters       filters,
			ref string          continuationPoint,
			out bool            moreElements,
			out BrowseElement[] elements,
			out Error[]         errors)
		{
			moreElements = false;
			
			// initialize reply.
			ReplyBase reply = CreateReply(locale, clientRequestHandle);

			try
			{
				lock (this)
				{
					// ensure server can process the request.
					CheckState(reply.RevisedLocaleID, false);

					// lookup position used to continue browse.
					Opc.Da.BrowsePosition position = null;
				
					if (continuationPoint != null && continuationPoint.Length > 0)
					{
						position = (Opc.Da.BrowsePosition)m_positions[continuationPoint];

						// cannot continue previous browse.
						if (position == null)
						{
							throw CreateException(reply.RevisedLocaleID, Error.E_INVALIDCONTINUATIONPOINT);
						}
					}

					// lookup the item path.
					string itemPath = "";

					if (itemID != null && itemID.ItemPath != null)
					{
						itemPath = itemID.ItemPath; 
					}

					// lookup the server.
					Opc.Da.IServer server = (Opc.Da.IServer)m_servers[itemPath];

					if (server == null)
					{
						throw CreateException(reply.RevisedLocaleID, Error.E_INVALIDITEMPATH);
					}

					// check for root of server address space.
					bool root = false;

					if (itemID != null && itemID.ItemPath == itemID.ItemName)
					{
						itemID.ItemName = "";
						root = true;
					}

					// continue previous browse.
					if (position != null)
					{
						m_positions.Remove(continuationPoint);

						// browse wrapped servers.
						if (position is RootBrowsePosition)
						{
							elements = BrowseRoot(
								null,
								itemID,
								filters,
								locale,
								returnErrorText,
								ref position);
						}
						else
						{
							try
							{
								elements = server.BrowseNext(ref position);
							}
							catch (Exception e)
							{
								position.Dispose();
								throw CreateException(reply.RevisedLocaleID, e);
							}

							// browse wrapped servers.
							if (root && itemPath.Length == 0 && position == null)
							{
								elements = BrowseRoot(
									elements,
									itemID,
									filters,
									locale,
									returnErrorText,
									ref position);
							}
						}
					}

					// begin new browse.
					else
					{
						try
						{
							elements = server.Browse(itemID, filters, out position);
						}
						catch (Exception e)
						{
							throw CreateException(reply.RevisedLocaleID, e);
						}

						// browse wrapped servers.
						if (root && itemPath.Length == 0 && position == null)
						{
							elements = BrowseRoot(
								elements,
								itemID,
								filters,
								locale,
								returnErrorText,
								ref position);
						}
					}

					// update item path in elements.
					if (elements != null)
					{
						foreach (BrowseElement element in elements)
						{
							// skip elements which already have an item path defined.
							if (element.ItemPath != null)
							{
								continue;
							}

							element.ItemPath = itemPath;

							// update item path for any properties for the browse element.
							if (element.Properties != null)
							{
								foreach (ItemProperty property in element.Properties)
								{
									if (property.ItemName != null)
									{
										property.ItemPath = itemPath;
									}						
								
									// check for property values.
									CheckStringValue(property);
								}
							}
						}
					}

					// save new continuation point.
					continuationPoint = null;

					if (position != null)
					{
						// dispose of any previous browse positions.
						foreach (Opc.Da.BrowsePosition current in m_positions.Values) { current.Dispose(); }
						m_positions.Clear();

						moreElements = true;
						continuationPoint = Guid.NewGuid().ToString();
						m_positions[continuationPoint] = position;
					}

					errors = (returnErrorText)?GetErrors(reply.RevisedLocaleID, elements):null;
				
					// update reply and return.
					reply.ReplyTime = DateTime.Now;
					return reply;
				}
			}
			catch (Exception e)
			{
				throw CreateException(reply.RevisedLocaleID, e);
			}
		}

		/// <summary>
		/// Returns the names of the wrapped servers.
		/// </summary>
		private BrowseElement[] BrowseRoot(
			BrowseElement[]           elements, 
			ItemIdentifier            itemID,
			BrowseFilters             filters, 
			string                    locale,
			bool                      returnErrorText,
			ref Opc.Da.BrowsePosition position)
		{
			int index = 0;
			string[] names = null;

			ArrayList elementList = new ArrayList();

			// add already fetched elements.
			if (elements != null)
			{
				elementList.AddRange(elements);
			}

			// fetch list of names for wrapped servers.
			if (position is RootBrowsePosition)
			{
				index   = ((RootBrowsePosition)position).Index;
				names   = ((RootBrowsePosition)position).Names;
			}

			// fetch list of names for wrapped servers.
			if (names == null)
			{
				ArrayList nameList = new ArrayList();

				foreach (string name in m_servers.Keys)
				{
					if (name.Length > 0)
					{
						nameList.Add(name);
					}
				}
				
				names = (string[])nameList.ToArray(typeof(string));
			}

			// release existing continuation point.
			if (position != null)
			{
				itemID  = position.ItemID;
				filters = position.Filters;

				if (position is BrowsePosition)
				{
					locale          = ((BrowsePosition)position).Locale;
					returnErrorText = ((BrowsePosition)position).ReturnErrorText;
				}

				position.Dispose();
				position = null;
			}

			for (int ii = index; ii < names.Length; ii++)
			{
				// check for max elements.
				if (filters.MaxElementsReturned > 0 && filters.MaxElementsReturned == elementList.Count)
				{
					position = new RootBrowsePosition(itemID, filters, locale, returnErrorText);

					((RootBrowsePosition)position).Names = names;
					((RootBrowsePosition)position).Index = ii;
					break;
				}

				// nothing to do for item browses.
				if (filters.BrowseFilter == browseFilter.item)
				{
					continue;
				}

				// check element name filter.
				if (!Opc.Convert.Match(names[ii], filters.ElementNameFilter, false))
				{
					continue;
				}

				// create browse element.
				BrowseElement element = new BrowseElement();

				element.Name        = names[ii];
				element.ItemName    = names[ii];
				element.ItemPath    = names[ii];
				element.HasChildren = true;
				element.IsItem      = false;
				element.Properties  = null;

				elementList.Add(element);
			}

			// return browse elements.
			return (BrowseElement[])elementList.ToArray(typeof(BrowseElement));
		}

		/// <summary>
		/// Returns the specified properties for a set of items.
		/// </summary>
		public ReplyBase GetProperties(
			string                       locale,
			string                       clientRequestHandle,
			bool                         returnErrorText,
			ItemIdentifier[]             itemIDs,
			PropertyID[]                 propertyIDs,
			string                       itemPath,
			bool                         returnValues,
			out ItemPropertyCollection[] properties,
			out Error[]                  errors)
		{
			// initialize reply.
			ReplyBase reply = CreateReply(locale, clientRequestHandle);

			try
			{
				lock (this)
				{
					// get the properties.
					try
					{				
						// ensure server can process the request.
						CheckState(reply.RevisedLocaleID, false);

						// copy default item path into individual items.
						foreach (ItemIdentifier itemID in itemIDs)
						{
							if (itemID.ItemPath == null)
							{
								itemID.ItemPath = itemPath;
							}
						}

						// create property lists.
						properties = new ItemPropertyCollection[itemIDs.Length];

						// process items with recognized paths.
						foreach (string serverPath in m_servers.Keys)
						{
							GetProperties(reply.RevisedLocaleID, serverPath, itemIDs, propertyIDs, returnValues, properties);
						}

						// set error for items with unrecognized paths.
						for (int ii = 0; ii < properties.Length; ii++)
						{
							// check if item has not been processed.
							if (properties[ii] == null)
							{
								properties[ii] = new ItemPropertyCollection(itemIDs[ii], ResultID.Da.E_UNKNOWN_ITEM_PATH);
							}

							// check for property values.
							for (int jj = 0; jj < properties[ii].Count; jj++)
							{
								CheckStringValue(properties[ii][jj]);
							}
						}			
					}
					catch (Exception e)
					{
						throw CreateException(reply.RevisedLocaleID, e);
					}

					errors = (returnErrorText)?GetErrors(reply.RevisedLocaleID, properties):null;

					// update reply and return.
					reply.ReplyTime = DateTime.Now;
					return reply;
				}
			}
			catch (Exception e)
			{
				throw CreateException(reply.RevisedLocaleID, e);
			}
		}
		
		#region Private Members
		/// <summary>
		/// The set of COM servers being wrapped by the XML-DA server.
		/// </summary>
		private Hashtable m_servers = new Hashtable();

		/// <summary>
		/// The item path for the wrapped server that should be used for get status requests.
		/// </summary>
		private string m_primaryServer = null;

		/// <summary>
		/// The current status of the XML-DA server (distinct from the status of the COM-DA server).
		/// </summary>
		private ServerStatus m_status = new ServerStatus();

		/// <summary>
		/// A timer that clears out expired subscriptions.
		/// </summary>
		private Timer m_pingTimer = null;

		/// <summary>
		/// A table of last pool times indexed by subscription handle.
		/// </summary>
		private Hashtable m_subscriptionPollTimes = new Hashtable();

		/// <summary>
		/// The names of the locales supported by the server.
		/// </summary>
		private string[] m_supportedLocales = null;

		/// <summary>
		/// The resource manager used to access localized resources.
		/// </summary>
		protected ResourceManager m_resourceManager = null;

		/// <summary>
		/// Stores browse positions for incomplete browse operations.
		/// </summary>
		Hashtable m_positions = new Hashtable();

		/// <summary>
		/// Currently active client subscriptions.
		/// </summary>
		private Hashtable m_subscriptions = new Hashtable();

		/// <summary>
		/// Current remote subscriptions.
		/// </summary>
		private ArrayList m_remoteSubscriptions = new ArrayList();

		/// <summary>
		/// Returns a localized string with the specified name.
		/// </summary>
		private string GetString(string name, string locale)
		{
			// create a culture object.
			CultureInfo culture = null;
			
			try   { culture = new CultureInfo(locale); }
			catch {	culture = new CultureInfo(""); }

			// lookup resource string.
			try   { return m_resourceManager.GetString(name, culture); }
			catch {	return null; }
		}

		/// <summary>
		/// Initializes a reply object.
		/// </summary>
		private ReplyBase CreateReply(string locale, string clientRequestHandle)
		{
			ReplyBase reply = new ReplyBase();

			reply.ClientRequestHandle = clientRequestHandle;
			reply.RcvTime             = DateTime.Now;
			reply.ReplyTime           = DateTime.MinValue;
			reply.ServerState         = m_status.ServerState;
			reply.RevisedLocaleID     = Opc.Da.Server.FindBestLocale(locale, m_supportedLocales);

			return reply;
		}

		/// <summary>
		/// Checks that the server is aply to process requests.
		/// </summary>
		private void CheckState(string locale, bool isDataRequest)
		{
			if (isDataRequest)
			{
				if (m_status.ServerState != serverState.running && m_status.ServerState != serverState.test)
				{
					throw CreateException(locale, Error.E_SERVERSTATE);
				}
			}
			else
			{
				if (m_status.ServerState == serverState.failed)
				{
					throw CreateException(locale, Error.E_SERVERSTATE);
				}
			}
		}

		/// <summary>
		/// Creates a SOAP exception for the specified error.
		/// </summary>
		private Exception CreateException(string locale, XmlQualifiedName error)
		{
			return new SoapException(GetString(error.Name, locale), error);
		}

		/// <summary>
		/// Creates a SOAP exception for the specified exception.
		/// </summary>
		private Exception CreateException(string locale, Exception e)
		{
			// no change if already a soap exception.
			if (typeof(SoapException).IsInstanceOfType(e))
			{
				return e;
			}

			// convert to a soap exception with a generic error code.
			if (!typeof(ResultIDException).IsInstanceOfType(e))
			{
				return new SoapException(e.Message, Error.E_FAIL, e);
			}

			// map unified DA results onto XML-DA errors.
			ResultIDException exception = (ResultIDException)e;
				
			if (exception.Result == ResultID.E_FAIL)                        return CreateException(locale, Error.E_FAIL);
			if (exception.Result == ResultID.Da.E_INVALID_ITEM_NAME)        return CreateException(locale, Error.E_INVALIDITEMNAME);
			if (exception.Result == ResultID.Da.E_INVALID_ITEM_PATH)        return CreateException(locale, Error.E_INVALIDITEMPATH);
			if (exception.Result == ResultID.Da.E_UNKNOWN_ITEM_NAME)        return CreateException(locale, Error.E_UNKNOWNITEMNAME);
			if (exception.Result == ResultID.Da.E_UNKNOWN_ITEM_PATH)        return CreateException(locale, Error.E_UNKNOWNITEMPATH);
			if (exception.Result == ResultID.Da.E_INVALID_FILTER)           return CreateException(locale, Error.E_INVALIDFILTER);
			if (exception.Result == ResultID.Da.E_INVALIDCONTINUATIONPOINT) return CreateException(locale, Error.E_INVALIDCONTINUATIONPOINT);
			if (exception.Result == ResultID.E_TIMEDOUT)                    return CreateException(locale, Error.E_TIMEDOUT);
			if (exception.Result == ResultID.E_OUTOFMEMORY)                 return CreateException(locale, Error.E_OUTOFMEMORY);

			// throw a generic error exception.
			return new SoapException(exception.Result.ToString(), Error.E_FAIL, e);
		}
        
        /// <summary>
        /// Verifies that the request deadline has been met.
        /// </summary>
        private TimeSpan CheckDeadline(string locale, DateTime deadline)
        {
            if (deadline > DateTime.MinValue)
            { 
                if (deadline.Kind != DateTimeKind.Utc)
                {
                    deadline = deadline.ToUniversalTime();
                }

                DateTime now = DateTime.UtcNow;
                
                if (deadline < now)
                {
					throw CreateException(locale, Error.E_TIMEDOUT);
                }

                return deadline - now;
            }
                
            return TimeSpan.MaxValue;
        }

		/// <summary>
		/// Updates the item objects with values specified at the list level.
		/// </summary>
		private void ApplyItemListDefaults(ItemList list)
		{
			foreach (Item item in list)
			{
				if (item.ReqType == null)
				{
					item.ReqType = list.ReqType;
				}
				
				if (!item.MaxAgeSpecified)
				{
					item.MaxAge          = list.MaxAge;
					item.MaxAgeSpecified = list.MaxAgeSpecified;
				}

				if (!item.DeadbandSpecified)
				{
					item.Deadband          = list.Deadband;
					item.DeadbandSpecified = list.DeadbandSpecified;
				}

				if (!item.SamplingRateSpecified)
				{
					item.SamplingRate          = list.SamplingRate;
					item.SamplingRateSpecified = list.SamplingRateSpecified;
				}

				if (!item.EnableBufferingSpecified)
				{
					item.EnableBuffering = list.EnableBuffering;
					item.EnableBufferingSpecified = list.EnableBufferingSpecified;
				}
			}
		}

		/// <summary>
		/// Generates a list of error results if the deadline has already passed.
		/// </summary>
		private ItemValueResultList CheckDeadline(string locale, DateTime deadline, object requestList)
		{
			// check for trivial case.
			if (deadline == DateTime.MinValue || requestList == null)
			{
				return null;
			}

			// check if deadline has already passed.
            TimeSpan timeleft = CheckDeadline(locale, deadline);

			// check if there is enough time left to complete the request.
			// the value used here is arbitraty - picked to demonstrate how to use the deadline.
			if (timeleft.TotalSeconds > 1)
			{
				return null;
			}

			// create result list.
			ItemValueResultList replyList = new ItemValueResultList();

			foreach (ItemIdentifier requestItem in (ICollection)requestList)
			{
				ItemValueResult replyItem = new ItemValueResult(requestItem);

				replyItem.Value              = null;
				replyItem.Quality            = Quality.Bad;
				replyItem.QualitySpecified   = false;
				replyItem.Timestamp          = DateTime.MinValue;
				replyItem.TimestampSpecified = false;
				replyItem.ResultID           = ResultID.E_TIMEDOUT;
				replyItem.DiagnosticInfo     = null;

				replyList.Add(replyItem);
			}

			return replyList;
		}

		/// <summary>
		/// Applies the request objects to the results.
		/// </summary>
		private Error[] ApplyOptions(string locale, RequestOptions options, object replyLists)
		{
			// check for null.
			if (replyLists == null)
			{
				return null;
			}
			
			// return verbose error texts, if required.
			Error[] errors = ((options.Filters & (int)ResultFilter.ErrorText) != 0)?GetErrors(locale, replyLists):null;

			// process single result list.
			if (replyLists.GetType() == typeof(ItemValueResultList))
			{
				foreach (ItemValueResult replyItem in (ItemValueResultList)replyLists)
				{
					// remove fields that are not requested by the client.
					if ((options.Filters & (int)ResultFilter.ItemName) == 0) replyItem.ItemName = null;
					if ((options.Filters & (int)ResultFilter.ItemPath) == 0) replyItem.ItemPath = null;
					
					if ((options.Filters & (int)ResultFilter.DiagnosticInfo) == 0) 
					{
						replyItem.DiagnosticInfo = null;
					}
					else if (replyItem.DiagnosticInfo == null)
					{
						replyItem.DiagnosticInfo = String.Format("ResultID = {0}", replyItem.ResultID);
					}

					if ((options.Filters & (int)ResultFilter.ItemTime) == 0) 
					{
						replyItem.Timestamp = DateTime.MinValue;
						replyItem.TimestampSpecified = false;
					}
				}
			}
			
			// process multiple result lists.
			else if (replyLists.GetType() == typeof(ItemValueResultList[]))
			{
				foreach (ItemValueResultList replyList in (ItemValueResultList[])replyLists)
				{
					// remove fields that are not requested by the client.
					foreach (ItemValueResult replyItem in replyList)
					{
						if ((options.Filters & (int)ResultFilter.ItemName) == 0)       replyItem.ItemName       = null;
						if ((options.Filters & (int)ResultFilter.ItemPath) == 0)       replyItem.ItemPath       = null;
						if ((options.Filters & (int)ResultFilter.DiagnosticInfo) == 0) replyItem.DiagnosticInfo = null;

						if ((options.Filters & (int)ResultFilter.ItemTime) == 0) 
						{
							replyItem.Timestamp = DateTime.MinValue;
							replyItem.TimestampSpecified = false;
						}
					}
				}
			}

			// return verbose error texts, if required.
			return errors;
		}

		/// <summary>
		/// Constructs an error object for a result identifier.
		/// </summary>
		private OpcXml.Da.Error GetError(string locale, string itemPath, ResultID resultID)
		{
			OpcXml.Da.Error error = new OpcXml.Da.Error();

			error.ID = OpcXml.Da10.Request.GetResultID(resultID);

			if (resultID.Name != null)
			{
				error.Text = GetString(resultID.Name.Name, locale);
			}

			if (error.Text == null || error.Text == "")
			{
				Opc.Da.IServer server = (Opc.Da.IServer)m_servers[itemPath];

				if (server != null)
				{
					try   { error.Text = server.GetErrorText(locale, resultID);   }
					catch {	error.Text = String.Format("0x{0,8:X}", resultID.Code); }
				}
			}
			
			return error;
		}

		/// <summary>
		/// Constructs an error object for a result identifier.
		/// </summary>
		private OpcXml.Da.Error[] GetErrors(string locale, object resultLists)
		{
			Hashtable resultIDs = new Hashtable();

			// check if there is nothing to do.
			if (resultLists == null) { return null; }

			// search item value result lists.
			if (resultLists.GetType() == typeof(ItemValueResultList))
			{
				foreach (ItemValueResult resultItem in (ItemValueResultList)resultLists)
				{
					if (resultItem.ResultID != ResultID.S_OK)
					{
						if (!resultIDs.Contains(resultItem.ResultID))
						{
							resultIDs[resultItem.ResultID] = GetError(locale, resultItem.ItemPath, resultItem.ResultID);
						}
					}
				}
			}

            // search item value result lists.
			else if (resultLists.GetType() == typeof(ItemValueResultList[]))
			{
				foreach (ItemValueResultList resultList in (ItemValueResultList[])resultLists)
				{
					foreach (ItemValueResult resultItem in resultList)
					{
						if (resultItem.ResultID != ResultID.S_OK)
						{
							if (!resultIDs.Contains(resultItem.ResultID))
							{
								resultIDs[resultItem.ResultID] = GetError(locale, resultItem.ItemPath, resultItem.ResultID);
							}
						}
					}
				}
			}

			// search browse elements.
			else if (resultLists.GetType().GetElementType() == typeof(BrowseElement))
			{
				foreach (BrowseElement element in (Array)resultLists)
				{
					if (element.Properties == null) { continue; }

					foreach (ItemProperty property in element.Properties)
					{
						if (property.ResultID != ResultID.S_OK)
						{
							if (!resultIDs.Contains(property.ResultID))
							{
								resultIDs[property.ResultID] = GetError(locale, element.ItemPath, property.ResultID);
							}
						}
					}
				}
			}

			// search item property lists.
			else if (resultLists.GetType().GetElementType() == typeof(ItemPropertyCollection))
			{
				foreach (ItemPropertyCollection propertyList in (Array)resultLists)
				{
					if (propertyList == null) { continue; }

					if (propertyList.ResultID != ResultID.S_OK)
					{
						if (!resultIDs.Contains(propertyList.ResultID))
						{

							resultIDs[propertyList.ResultID] = GetError(locale, propertyList.ItemPath, propertyList.ResultID);
						}
					}

					foreach (ItemProperty property in propertyList)
					{
						if (property.ResultID != ResultID.S_OK)
						{
							if (!resultIDs.Contains(property.ResultID))
							{
								resultIDs[property.ResultID] = GetError(locale, propertyList.ItemPath, property.ResultID);
							}
						}
					}
				}
			}

			// construct array of unique errors.
			ArrayList errors = new ArrayList();

			foreach (OpcXml.Da.Error error in resultIDs.Values)
			{
				errors.Add(error);
			}

			return (OpcXml.Da.Error[])errors.ToArray(typeof(OpcXml.Da.Error));
		}

		/// <summary>
		/// Cleans up any inactive subscriptions.
		/// </summary>
		private void CleanupSubscriptions(object state)
		{
			lock (this)
			{
				// collect list of expired subscriptions.
				ArrayList expiredSubscriptions = new ArrayList();

				foreach (Subscription subscription in m_subscriptions.Values)
				{
					if (subscription.HasExpired())
					{
						expiredSubscriptions.Add(subscription);
					}
				}

				// remove expired subscriptions.
				foreach (Subscription subscription in expiredSubscriptions)
				{
					CancelSubscription(subscription);
				}

				// cancel timer if no more subscriptions.
				if (m_subscriptions.Count > 0)
				{
					m_pingTimer = new Timer(
						new TimerCallback(CleanupSubscriptions), 
						null, 
						1000,
						Timeout.Infinite);
				}
			}
		}

		/// <summary>
		/// Cancels a subscription and releases all resources.
		/// </summary>
		private void CancelSubscription(Subscription subscription)
		{
			lock (this)
			{
				// remove from table of subscriptions.
				m_subscriptions.Remove(subscription.Handle);

				// get list of items.
				ItemIdentifier[] items = subscription.GetItems();

				ArrayList remoteSubscriptions = new ArrayList();

				foreach (RemoteSubscription remoteSubscription in m_remoteSubscriptions)
				{
					// discard empty remote subscriptions.
					if (remoteSubscription.RemoveItems(items))
					{
						remoteSubscriptions.Add(remoteSubscription);
					}
					else
					{
						remoteSubscription.Dispose();
					}
				}

				// update set of remote subscriptions.
				m_remoteSubscriptions = remoteSubscriptions;
			}
		}

		/// <summary>
		/// Notify each subscription object that new data has arrived.
		/// </summary>
		private void DataChanged(object subscriptionHandle, object requestHandle, ItemValueResult[] values)
		{
			lock (this)
			{
				foreach (Subscription subcription in m_subscriptions.Values)
				{
					subcription.OnDataChanged(values);
				}
			}
		}

		/// <summary>
		/// Reads all items with the specifed item path.
		/// </summary>
		private void Read(
			string            locale,
			string            itemPath,
			Item[]            items,
			ItemValueResult[] results)
		{
			// lookup from server.
			Opc.Da.IServer server = (Opc.Da.IServer)m_servers[(itemPath != null)?itemPath:""];

			// find all items with a matching path.
			ArrayList readItems = new ArrayList(items.Length);

			for (int ii = 0; ii < items.Length; ii++)
			{
				if (itemPath == items[ii].ItemPath)
				{					
					if (server != null)
					{
						Item item = new Item(items[ii]);
						item.ItemPath = null;
						readItems.Add(item);
					}
					else
					{
						results[ii] = new ItemValueResult(items[ii], ResultID.Da.E_UNKNOWN_ITEM_PATH);
					}
				}
			}

			// check if nothing more to do.
			if (readItems.Count == 0)
			{
				return;
			}

			ItemValueResult[] readResults = null;
			ResultID result = ResultID.S_OK;

			// do read.
			try
			{
				server.SetLocale(locale);

				readResults = server.Read((Item[])readItems.ToArray(typeof(Item)));

				if (readResults == null || readResults.Length != readItems.Count)
				{
					result = ResultID.E_FAIL;
				}
			}
			catch (ResultIDException e)
			{
				result = e.Result;
			}
			catch (Exception)
			{
				result = ResultID.E_FAIL;
			}

			// create result items.
			int index = 0;
					
			for (int ii = 0; ii < items.Length; ii++)
			{
				if (itemPath == items[ii].ItemPath)
				{
					if (result.Succeeded())
					{
						results[ii] = readResults[index++];
						results[ii].ItemPath = itemPath;
					}
					else
					{
						results[ii] = new ItemValueResult(items[ii], result);
					}
				}
			}
		}

		/// <summary>
		/// Writes all items with the specifed item path.
		/// </summary>
		private void Write(
			string            locale,
			string            itemPath,
			ItemValue[]       values,
			bool              returnValues,
			ItemValueResult[] results)
		{
			// lookup from server.
			Opc.Da.IServer server = (Opc.Da.IServer)m_servers[(itemPath != null)?itemPath:""];

			// get canonical data type for all items to write.
			ItemPropertyCollection[] properties = null;
			
			if (server != null)
			{
				properties = server.GetProperties(
					values, 
					new PropertyID[] { Opc.Da.Property.DATATYPE },
					true);
			}

			// initialize results.
			SortedList writableIndexes = new SortedList();

			for (int ii = 0; ii < results.Length; ii++)
			{
				// ignore values for other item paths.
				if (itemPath != values[ii].ItemPath)
				{					
					continue;
				}

				// check for valid path.
				if (server == null)
				{
					results[ii] = new ItemValueResult(values[ii], ResultID.Da.E_UNKNOWN_ITEM_PATH);		
					continue;
				}

				// initialize result.
				results[ii] = new ItemValueResult(values[ii]);

				results[ii].Value              = null;
				results[ii].Quality            = Quality.Bad;
				results[ii].QualitySpecified   = false;
				results[ii].Timestamp          = DateTime.MinValue;
				results[ii].TimestampSpecified = false;
				results[ii].ResultID           = ResultID.S_OK;
				results[ii].DiagnosticInfo     = null;

				// writing strings only allowed if canonical data type is a string.
				if (typeof(string).IsInstanceOfType(values[ii].Value))
				{
					// item may not exist.
					if (properties[ii].ResultID.Failed())
					{
						results[ii].ResultID       = properties[ii].ResultID;
						results[ii].DiagnosticInfo = null;
					}

					// return bad type error.
					else if (typeof(string) != (System.Type)properties[ii][0].Value)
					{
						results[ii].ResultID       = Opc.ResultID.Da.E_BADTYPE;
						results[ii].DiagnosticInfo = null;
					}
				}

				// build list of items to write.
				if (results[ii].ResultID.Succeeded())
				{
					ItemValue value = new ItemValue(values[ii]);
					value.ItemPath = null;
					writableIndexes[ii] = value;
				}
			}

			// check if nothing more to do.
			if (writableIndexes.Count == 0)
			{
				return;
			}

			// do write.
			ResultID result = ResultID.S_OK;

			try
			{
				// ensure correct locale.
				server.SetLocale(locale);

				// write initial results.
				ItemValue[] writableValues = new ItemValue[writableIndexes.Count];

				for (int ii = 0; ii < writableValues.Length; ii++)
				{
					writableValues[ii] = (ItemValue)writableIndexes.GetByIndex(ii);
				}

				IdentifiedResult[] writeResults = server.Write(writableValues);

				if (writeResults == null || writeResults.Length != writableValues.Length)
				{
					result = ResultID.E_FAIL;
				}

				// copy the results.
				for (int ii = 0; ii < writeResults.Length; ii++)
				{
					int index = (int)writableIndexes.GetKey(ii);

					results[index].ResultID       = writeResults[ii].ResultID;
					results[index].DiagnosticInfo = writeResults[ii].DiagnosticInfo;
				}

				// check for server cannot handle data conversion.
				SortedList conversionErrors = new SortedList();

				for (int ii = 0; ii < writeResults.Length; ii++)
				{
					if (writeResults[ii].ResultID == ResultID.Da.E_BADTYPE && writableValues[ii].Value != null)
					{
						conversionErrors.Add(writableIndexes.GetKey(ii), writableIndexes.GetByIndex(ii));
					}	
				}

				// re-write results in canonical datatype.
				if (conversionErrors.Count > 0)
				{
					// convert to canonical type.
					writableValues = new ItemValue[conversionErrors.Count];

					for (int ii = 0; ii < writableValues.Length; ii++)
					{
						writableValues[ii] = (ItemValue)((ICloneable)conversionErrors.GetByIndex(ii)).Clone();

						try
						{
							int index = (int)conversionErrors.GetKey(ii);

							ItemValue canonicalValue = (ItemValue)((ItemValue)conversionErrors.GetByIndex(ii)).Clone();						
							canonicalValue.Value = Opc.Convert.ChangeType(canonicalValue.Value, (System.Type)properties[index][0].Value);
							writableValues[ii] = canonicalValue;
						}
						catch
						{
							// ignore errors.
						}
					}

					// write again.
					writeResults = server.Write(writableValues);

					if (writeResults == null || writeResults.Length != writableValues.Length)
					{
						result = ResultID.E_FAIL;
					}

					// copy the results.
					for (int ii = 0; ii < writeResults.Length; ii++)
					{
						int index = (int)conversionErrors.GetKey(ii);

						results[index].ResultID       = writeResults[ii].ResultID;
						results[index].DiagnosticInfo = writeResults[ii].DiagnosticInfo;
					}
				}
			}
			catch (ResultIDException e)
			{
				result = e.Result;
			}
			catch (Exception)
			{
				result = ResultID.E_FAIL;
			}

			// check for general failure.
			if (result.Failed())
			{			
				for (int ii = 0; ii < results.Length; ii++)
				{
					results[ii].ResultID       = result;
					results[ii].DiagnosticInfo = null;
				}

				return;
			}

			// readback values if requested.
			if (returnValues)
			{
				// build list of successful writes.
				SortedList readableIndexes = new SortedList();
				
				for (int ii = 0; ii < writableIndexes.Count; ii++)
				{
					int index = (int)writableIndexes.GetKey(ii);

					if (results[index] != null && results[index].ResultID.Succeeded())
					{
						readableIndexes.Add(index, values[index]);
					}
				}

				if (readableIndexes.Count > 0)
				{
					// create item list to read.
					Item[] readableValues = new Item[readableIndexes.Count];

					for (int ii = 0; ii < readableValues.Length; ii++)
					{
						ItemValue value = (ItemValue)readableIndexes.GetByIndex(ii);

						readableValues[ii] = new Item(value);

						readableValues[ii].ReqType         = (value.Value != null)?value.Value.GetType():null;
						readableValues[ii].MaxAge          = 0;
						readableValues[ii].MaxAgeSpecified = true;
					}

					// readback values.
					ItemValueResult[] readResults = server.Read(readableValues);

					// copy the values into the results.
					for (int ii = 0; ii < readResults.Length; ii++)
					{
						int index = (int)readableIndexes.GetKey(ii);

						results[index].Value              = readResults[ii].Value;
						results[index].Quality            = readResults[ii].Quality;
						results[index].QualitySpecified   = readResults[ii].QualitySpecified;
						results[index].Timestamp          = readResults[ii].Timestamp;
						results[index].TimestampSpecified = readResults[ii].TimestampSpecified;
						results[index].ResultID           = readResults[ii].ResultID;
						results[index].DiagnosticInfo     = readResults[ii].DiagnosticInfo;
					}
				}
			}
		}
		
		/// <summary>
		/// Gets the properties with for specified items with the specifed item path.
		/// </summary>
		private void GetProperties(
			string                   locale,
			string                   itemPath,
			ItemIdentifier[]         itemIDs,
			PropertyID[]             propertyIDs,
			bool                     returnValues,
			ItemPropertyCollection[] properties)
		{
			// lookup from server.
			Opc.Da.IServer server = (Opc.Da.IServer)m_servers[(itemPath != null)?itemPath:""];

			// find all items with a matching path.
			ArrayList items = new ArrayList(itemIDs.Length);

			for (int ii = 0; ii < itemIDs.Length; ii++)
			{
				if (itemPath == itemIDs[ii].ItemPath)
				{					
					if (server != null)
					{
						items.Add(itemIDs[ii]);
					}
					else
					{
						properties[ii] = new ItemPropertyCollection(itemIDs[ii], ResultID.Da.E_UNKNOWN_ITEM_PATH);
					}
				}
			}

			// check if nothing more to do.
			if (items.Count == 0)
			{
				return;
			}

			ResultID result = ResultID.S_OK;

			ItemPropertyCollection[] results = null;

			// do get properties.
			try
			{				
				server.SetLocale(locale);

				results = server.GetProperties(
					(ItemIdentifier[])items.ToArray(typeof(ItemIdentifier)),
					propertyIDs,
					returnValues);

				if (results == null || results.Length != items.Count)
				{
					result = ResultID.E_FAIL;
				}
			}
			catch (ResultIDException e)
			{
				result = e.Result;
			}
			catch (Exception)
			{
				result = ResultID.E_FAIL;
			}

			// copy property lists.		
			int index = 0;

			for (int ii = 0; ii < itemIDs.Length; ii++)
			{
				if (itemPath == itemIDs[ii].ItemPath)
				{
					if (result.Succeeded())
					{
						properties[ii] = results[index++];

						// ensure result has correct item path.
						properties[ii].ItemPath = itemPath;

						// update item path for property items.
						foreach (ItemProperty property in properties[ii])
						{
							if (property.ItemName != null)
							{
								property.ItemPath = itemPath;
							}
						}
					}
					else
					{
						properties[ii] = new ItemPropertyCollection(itemIDs[ii], result);
					}
				}
			}
		}

		/// <summary>
		/// Ensures that the item value contains valid data.
		/// </summary>
		private void CheckStringValue(ItemValue result)
		{
			// check all string values for control characters.
			if (typeof(string).IsInstanceOfType(result.Value))
			{
				string value = (string)result.Value;

				for (int ii = 0; ii < value.Length; ii++)
				{
					// the .NET framework cannot encode control characters properly so
					// strings containing these characters must be removed from the response
					// in order to ensure the XML document is valid.
					if (Char.IsControl(value[ii]) && !Char.IsWhiteSpace(value[ii]))
					{
						result.Value = null;

						qualityBits quality = result.Quality.QualityBits;

						if (!result.QualitySpecified || ((uint)quality & (uint)qualityBits.good) != 0 || ((uint)quality & (uint)qualityBits.uncertain) != 0)
						{
							result.Quality          = Quality.Bad;
							result.QualitySpecified = true;
						}

						break;
					}
				}
			}
		}

		/// <summary>
		/// Ensures that the item property contains valid data.
		/// </summary>
		private void CheckStringValue(ItemProperty property)
		{
			// check all string values for control characters.
			if (typeof(string).IsInstanceOfType(property.Value))
			{
				string value = (string)property.Value;

				for (int ii = 0; ii < value.Length; ii++)
				{
					// the .NET framework cannot encode control characters properly so
					// strings containing these characters must be removed from the response
					// in order to ensure the XML document is valid.
					if (Char.IsControl(value[ii]) && !Char.IsWhiteSpace(value[ii]))
					{
						property.Value = null;
						break;
					}
				}
			}
		}
		#endregion
	}
}
