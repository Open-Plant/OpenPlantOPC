//============================================================================
// TITLE: Opc.Ae.Server.cs
//
// CONTENTS:
// 
// An in-process object which provides access to AE server objects.
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

namespace Opc.Ae
{
	#region Opc.Ae.Server Class
	/// <summary>
	/// An in-process object which provides access to AE server objects.
	/// </summary>
	[Serializable]
	public class Server : Opc.Server, Opc.Ae.IServer, ISerializable
	{
		#region Constructors
		/// <summary>
		/// Initializes the object with a factory and a default URL.
		/// </summary>
		/// <param name="factory">The Opc.Factory used to connect to remote servers.</param>
		/// <param name="url">The network address of a remote server.</param>
		public Server(Factory factory, URL url) : base(factory, url) 
		{
		}
		#endregion
	
		#region ISerializable Members
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string COUNT        = "CT";
			internal const string SUBSCRIPTION = "SU";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected Server(SerializationInfo info, StreamingContext context)
		:
			base(info, context)
		{	
			int count = (int)info.GetValue(Names.COUNT, typeof(int));

			m_subscriptions = new SubscriptionCollection();

			for (int ii = 0; ii < count; ii++)
			{
				Subscription subscription = (Subscription)info.GetValue(Names.SUBSCRIPTION + ii.ToString(), typeof(Subscription));
				m_subscriptions.Add(subscription);
			}
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			
			info.AddValue(Names.COUNT, m_subscriptions.Count);

			for (int ii = 0; ii < m_subscriptions.Count; ii++)
			{
				info.AddValue(Names.SUBSCRIPTION + ii.ToString(), m_subscriptions[ii]);
			}		
		}
		#endregion

		#region Public Interface
		/// <summary>
		/// The filters supported by the server.
		/// </summary>
		public int AvailableFilters
		{
			get { return m_filters; }
		}

		/// <summary>
		/// The outstanding subscriptions for the server.
		/// </summary>
		public SubscriptionCollection Subscriptions
		{
			get { return m_subscriptions; }			
		}

		#region SubscriptionCollection Class
		/// <summary>
		/// A read-only collection of subscriptions.
		/// </summary>
		public class SubscriptionCollection : ReadOnlyCollection
		{
			/// <summary>
			/// An indexer for the collection.
			/// </summary>
			public new Opc.Ae.Subscription this[int index]
			{
				get	{ return (Opc.Ae.Subscription)Array.GetValue(index); }
			}

			/// <summary>
			/// Returns a copy of the collection as an array.
			/// </summary>
			public new Opc.Ae.Subscription[] ToArray()
			{
				return (Opc.Ae.Subscription[])Array;
			}

			/// <summary>
			/// Adds a subscription to the end of the collection.
			/// </summary>
			internal void Add(Opc.Ae.Subscription subscription)
			{
				Opc.Ae.Subscription[] array = new Opc.Ae.Subscription[Count+1];
				
				Array.CopyTo(array, 0);
				array[Count] = subscription;
			
				Array = array;
			}

			/// <summary>
			/// Removes a subscription to the from the collection.
			/// </summary>
			internal void Remove(Opc.Ae.Subscription subscription)
			{
				Opc.Ae.Subscription[] array = new Opc.Ae.Subscription[Count-1];

				int index = 0;

				for (int ii = 0; ii < Array.Length; ii++)
				{
					Opc.Ae.Subscription element = (Opc.Ae.Subscription)Array.GetValue(ii);

					if (subscription != element)
					{
						array[index++] = element;
					}
				}

				Array = array;
			}

			/// <summary>
			/// Creates an empty collection.
			/// </summary>
			internal SubscriptionCollection() : base(new Opc.Ae.Subscription[0]) {}
		}
		#endregion

		//======================================================================
		// Connection Management	

		/// <summary>
		/// Connects to the server with the specified URL and credentials.
		/// </summary>
		public override void Connect(URL url, ConnectData connectData)
		{ 
			// connect to server.
			base.Connect(url, connectData);

			// all done if no subscriptions.
			if (m_subscriptions.Count == 0)
			{
				return;
			}

			// create subscriptions (should only happen if server has been deserialized).
			SubscriptionCollection subscriptions = new SubscriptionCollection();

			foreach (Subscription template in m_subscriptions)
			{
				// create subscription for template.
				try   { subscriptions.Add(EstablishSubscription(template)); }
				catch {}
			}

			// save new set of subscriptions.
			m_subscriptions = subscriptions;
		}

		/// <summary>
		/// Disconnects from the server and releases all network resources.
		/// </summary>
		public override void Disconnect() 
		{
			if (m_server == null) throw new NotConnectedException();

			// dispose of all subscriptions first.
			m_disposing = true;

			foreach (Subscription subscription in m_subscriptions)
			{
				subscription.Dispose();
			}
		
			m_disposing = false;

			// disconnect from server.
			base.Disconnect();
		}
		#endregion

		#region Opc.Ae.IServer Members
		//======================================================================
		// Get Status

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
		// Event Subscription

		/// <summary>
		/// Creates a new event subscription.
		/// </summary>
		/// <param name="state">The initial state for the subscription.</param>
		/// <returns>The new subscription object.</returns>
		public ISubscription CreateSubscription(SubscriptionState state)
		{
			if (m_server == null) throw new NotConnectedException();

			// create remote object.
			ISubscription subscription = ((IServer)m_server).CreateSubscription(state); 

			if (subscription != null)
			{
				// create wrapper.
				Subscription wrapper = new Subscription(this, subscription, state);
				m_subscriptions.Add(wrapper);
				return wrapper;
			}

			// should never happen.
			return null;
		}

		//======================================================================
		// QueryAvailableFilters

		/// <summary>
		/// Returns the event filters supported by the server.
		/// </summary>
		/// <returns>A bit mask of all event filters supported by the server.</returns>
		public int QueryAvailableFilters()
		{
			if (m_server == null) throw new NotConnectedException();

			m_filters = ((IServer)m_server).QueryAvailableFilters(); 

			return m_filters;
		}

		//======================================================================
		// QueryEventCategories

		/// <summary>		
		/// Returns the event categories supported by the server for the specified event types.
		/// </summary>
		/// <param name="eventType">A bit mask for the event types of interest.</param>
		/// <returns>A collection of event categories.</returns>
		public Category[] QueryEventCategories(int eventType)
		{
			if (m_server == null) throw new NotConnectedException();

			// fetch categories from server.
			Category[] categories = ((IServer)m_server).QueryEventCategories(eventType); 

			// return result.
			return categories;
		}

		//======================================================================
		// QueryConditionNames

		/// <summary>
		/// Returns the condition names supported by the server for the specified event categories.
		/// </summary>
		/// <param name="eventCategory">A bit mask for the event categories of interest.</param>
		/// <returns>A list of condition names.</returns>
		public string[] QueryConditionNames(int eventCategory)
		{
			if (m_server == null) throw new NotConnectedException();

			// fetch condition names from the server.
			string[] conditions = ((IServer)m_server).QueryConditionNames(eventCategory); 

			// return result.
			return conditions;
		}

		//======================================================================
		// QuerySubConditionNames

		/// <summary>
		/// Returns the sub-condition names supported by the server for the specified event condition.
		/// </summary>
		/// <param name="conditionName">The name of the condition.</param>
		/// <returns>A list of sub-condition names.</returns>
		public string[] QuerySubConditionNames(string conditionName)
		{
			if (m_server == null) throw new NotConnectedException();

			// fetch sub-condition names from the server.
			string[] subconditions = ((IServer)m_server).QuerySubConditionNames(conditionName); 

			// return result.
			return subconditions;
		}

		//======================================================================
		// QuerySourceConditions

		/// <summary>
		/// Returns the condition names supported by the server for the specified event source.
		/// </summary>
		/// <param name="sourceName">The name of the event source.</param>
		/// <returns>A list of condition names.</returns>
		public string[] QueryConditionNames(string sourceName)
		{
			if (m_server == null) throw new NotConnectedException();

			// fetch condition names from the server.
			string[] conditions = ((IServer)m_server).QueryConditionNames(sourceName); 

			// return result.
			return conditions;
		}

		//======================================================================
		// QueryEventAttributes

		/// <summary>		
		/// Returns the event attributes supported by the server for the specified event categories.
		/// </summary>
		/// <param name="eventCategory">A bit mask for the event categories of interest.</param>
		/// <returns>A collection of event attributes.</returns>
		public Attribute[] QueryEventAttributes(int eventCategory)
		{
			if (m_server == null) throw new NotConnectedException();

			// fetch attributes from server.
			Attribute[] attributes = ((IServer)m_server).QueryEventAttributes(eventCategory); 

			// return result.
			return attributes;
		}

		//======================================================================
		// TranslateToItemIDs

		/// <summary>
		/// Returns the DA item ids for a set of attribute ids belonging to events which meet the specified filter criteria.
		/// </summary>
		/// <param name="sourceName">The event source of interest.</param>
		/// <param name="eventCategory">The id of the event category for the events of interest.</param>
		/// <param name="conditionName">The name of a condition within the event category.</param>
		/// <param name="subConditionName">The name of a sub-condition within a multi-state condition.</param>
		/// <param name="attributeIDs">The ids of the attributes to return item ids for.</param>
		/// <returns>A list of item urls for each specified attribute.</returns>
		public ItemUrl[] TranslateToItemIDs(
			string sourceName,
			int    eventCategory,
			string conditionName,
			string subConditionName,
			int[]  attributeIDs)
		{
			if (m_server == null) throw new NotConnectedException();

			ItemUrl[] itemUrls = ((IServer)m_server).TranslateToItemIDs(
				sourceName, 
				eventCategory,
				conditionName, 
				subConditionName,
				attributeIDs); 

			return itemUrls;
		}

		//======================================================================
		// GetConditionState

		/// <summary>
		/// Returns the current state information for the condition instance corresponding to the source and condition name.
		/// </summary>
		/// <param name="sourceName">The source name</param>
		/// <param name="conditionName">A condition name for the source.</param>
		/// <param name="attributeIDs">The list of attributes to return with the condition state.</param>
		/// <returns>The current state of the connection.</returns>
		public Condition GetConditionState(
			string sourceName,
			string conditionName,
			int[]  attributeIDs)
		{
			if (m_server == null) throw new NotConnectedException();

			Condition condition = ((IServer)m_server).GetConditionState(sourceName, conditionName, attributeIDs); 

			return condition;
		}

		//======================================================================
		// EnableConditionByArea

		/// <summary>
		/// Places the specified process areas into the enabled state.
		/// </summary>
		/// <param name="areas">A list of fully qualified area names.</param>
		/// <returns>The results of the operation for each area.</returns>
		public ResultID[] EnableConditionByArea(string[] areas)
		{
			if (m_server == null) throw new NotConnectedException();

			ResultID[] results = ((IServer)m_server).EnableConditionByArea(areas); 

			return results;
		}
		
		//======================================================================
		// DisableConditionByArea

		/// <summary>
		/// Places the specified process areas into the disabled state.
		/// </summary>
		/// <param name="areas">A list of fully qualified area names.</param>
		/// <returns>The results of the operation for each area.</returns>
		public ResultID[] DisableConditionByArea(string[] areas)
		{
			if (m_server == null) throw new NotConnectedException();

			ResultID[] results = ((IServer)m_server).DisableConditionByArea(areas); 

			return results;
		}

		//======================================================================
		// EnableConditionBySource

		/// <summary>
		/// Places the specified process areas into the enabled state.
		/// </summary>
		/// <param name="sources">A list of fully qualified source names.</param>
		/// <returns>The results of the operation for each area.</returns>
		public ResultID[] EnableConditionBySource(string[] sources)
		{
			if (m_server == null) throw new NotConnectedException();

			ResultID[] results = ((IServer)m_server).EnableConditionBySource(sources); 

			return results;
		}

		//======================================================================
		// DisableConditionBySource

		/// <summary>
		/// Places the specified process areas into the disabled state.
		/// </summary>
		/// <param name="sources">A list of fully qualified source names.</param>
		/// <returns>The results of the operation for each area.</returns>
		public ResultID[] DisableConditionBySource(string[] sources)
		{
			if (m_server == null) throw new NotConnectedException();

			ResultID[] results = ((IServer)m_server).DisableConditionBySource(sources); 

			return results;
		}

		//======================================================================
		// GetEnableStateByArea

		/// <summary>
		/// Returns the enabled state for the specified process areas. 
		/// </summary>
		/// <param name="areas">A list of fully qualified area names.</param>
		public EnabledStateResult[] GetEnableStateByArea(string[] areas)
		{
			if (m_server == null) throw new NotConnectedException();

			EnabledStateResult[] results = ((IServer)m_server).GetEnableStateByArea(areas); 

			return results;
		}

		//======================================================================
		// GetEnableStateBySource

		/// <summary>
		/// Returns the enabled state for the specified event sources. 
		/// </summary>
		/// <param name="sources">A list of fully qualified source names.</param>
		public EnabledStateResult[] GetEnableStateBySource(string[] sources)
		{
			if (m_server == null) throw new NotConnectedException();

			EnabledStateResult[] results = ((IServer)m_server).GetEnableStateBySource(sources); 

			return results;
		}

		//======================================================================
		// AcknowledgeCondition

		/// <summary>
		/// Used to acknowledge one or more conditions in the event server.
		/// </summary>
		/// <param name="acknowledgerID">The identifier for who is acknowledging the condition.</param>
		/// <param name="comment">A comment associated with the acknowledgment.</param>
		/// <param name="conditions">The conditions being acknowledged.</param>
		/// <returns>A list of result id indictaing whether each condition was successfully acknowledged.</returns>
		public ResultID[] AcknowledgeCondition(
			string                 acknowledgerID,
			string                 comment,
			EventAcknowledgement[] conditions)
		{
			if (m_server == null) throw new NotConnectedException();

			return ((IServer)m_server).AcknowledgeCondition(acknowledgerID, comment, conditions); 
		}

		//======================================================================
		// Browse

		/// <summary>
		/// Browses for all children of the specified area that meet the filter criteria.
		/// </summary>
		/// <param name="areaID">The full-qualified id for the area.</param>
		/// <param name="browseType">The type of children to return.</param>
		/// <param name="browseFilter">The expression used to filter the names of children returned.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		public BrowseElement[] Browse(
			string     areaID,
			BrowseType browseType, 
			string     browseFilter)
		{
			if (m_server == null) throw new NotConnectedException();

			return ((IServer)m_server).Browse(areaID, browseType, browseFilter); 
		}

		/// <summary>
		/// Browses for all children of the specified area that meet the filter criteria.
		/// </summary>
		/// <param name="areaID">The full-qualified id for the area.</param>
		/// <param name="browseType">The type of children to return.</param>
		/// <param name="browseFilter">The expression used to filter the names of children returned.</param>
		/// <param name="maxElements">The maximum number of elements to return.</param>
		/// <param name="position">The object used to continue the browse if the number nodes exceeds the maximum specified.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		public BrowseElement[]  Browse(
			string              areaID,
			BrowseType          browseType, 
			string              browseFilter, 
			int                 maxElements,
			out IBrowsePosition position)
		{
			if (m_server == null) throw new NotConnectedException();

			return ((IServer)m_server).Browse(areaID, browseType, browseFilter, maxElements, out position); 
		}

		//======================================================================
		// BrowseNext

		/// <summary>
		/// Continues browsing the server's address space at the specified position.
		/// </summary>
		/// <param name="maxElements">The maximum number of elements to return.</param>
		/// <param name="position">The position object used to continue a browse operation.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		public BrowseElement[] BrowseNext(int maxElements, ref IBrowsePosition position)
		{
			if (m_server == null) throw new NotConnectedException();

			return ((IServer)m_server).BrowseNext(maxElements, ref position); 
		}	
		#endregion

		#region Private Members
		/// <summary>
		/// Called when a subscription object is disposed.
		/// </summary>
		/// <param name="subscription"></param>
		internal void SubscriptionDisposed(Subscription subscription)
		{
			if (!m_disposing)
			{
				m_subscriptions.Remove(subscription);
			}
		}

		/// <summary>
		/// Establishes a subscription based on the template provided.
		/// </summary>
		private Subscription EstablishSubscription(Subscription template)
		{
			ISubscription remoteServer = null;

			try
			{
				// create remote object.
				remoteServer = ((IServer)m_server).CreateSubscription(template.State); 

				if (remoteServer == null)
				{
					return null;
				}

				// create wrapper.
				Subscription subscription = new Subscription(this, remoteServer, template.State);

				// set filters.
				subscription.SetFilters(template.Filters);

				// set attributes.
				IDictionaryEnumerator enumerator = template.Attributes.GetEnumerator();

				while (enumerator.MoveNext())
				{
					subscription.SelectReturnedAttributes(
						(int)enumerator.Key,
						((Subscription.AttributeCollection)enumerator.Value).ToArray());
				}

				// return new subscription
				return subscription;
			}
			catch
			{
				if (remoteServer != null)
				{
					remoteServer.Dispose();
					remoteServer = null;
				}
			}
			
			// return null.
			return null;
		}
		#endregion

		#region Private Members
		private int m_filters = 0;
		private bool m_disposing = false;
		private SubscriptionCollection m_subscriptions = new SubscriptionCollection();
		#endregion
	}
	#endregion

	#region Asynchronous Delegates
	/// <summary>
	/// The asynchronous delegate for GetStatus.
	/// </summary>
	public delegate ServerStatus GetStatusAsyncDelegate();
	#endregion
}
