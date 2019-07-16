//============================================================================
// TITLE: Opc.Da.ISubscription.cs
//
// CONTENTS:
// 
// An interface that 
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
// 2004/02/18 RSA   Updated to conform with the .NET design guidelines.

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;

namespace Opc.Da
{
	//=============================================================================
	// SubscriptionState

	/// <summary>
	/// A subscription for a set of items on a single OPC server.
	/// </summary>
	public interface ISubscription : IDisposable
	{
		//======================================================================
		// Events

		/// <summary>
		/// An event to receive data change updates.
		/// </summary>
		event DataChangedEventHandler DataChanged;
		
		//======================================================================
		// Result Filters

		/// <summary>
		/// Returns the filters applied by the server to any item results returned to the client.
		/// </summary>
		/// <returns>A bit mask indicating which fields should be returned in any item results.</returns>
		int GetResultFilters();
		
		/// <summary>
		/// Sets the filters applied by the server to any item results returned to the client.
		/// </summary>
		/// <param name="filters">A bit mask indicating which fields should be returned in any item results.</param>
		void SetResultFilters(int filters);

		//======================================================================
		// State Management

		/// <summary>
		/// Returns the current state of the subscription.
		/// </summary>
		/// <returns>The current state of the subscription.</returns>
		SubscriptionState GetState();
		
		/// <summary>
		/// Changes the state of a subscription.
		/// </summary>
		/// <param name="masks">A bit mask that indicates which elements of the subscription state are changing.</param>
		/// <param name="state">The new subscription state.</param>
		/// <returns>The actual subscption state after applying the changes.</returns>
		SubscriptionState ModifyState(int masks, SubscriptionState state);

		//======================================================================
		// Item Management

		/// <summary>
		/// Adds items to the subscription.
		/// </summary>
		/// <param name="items">The set of items to add to the subscription.</param>
		/// <returns>The results of the add item operation for each item.</returns>
		ItemResult[] AddItems(Item[] items);

		/// <summary>
		/// Modifies the state of items in the subscription
		/// </summary>
		/// <param name="masks">Specifies which item state parameters are being modified.</param>
		/// <param name="items">The new state for each item.</param>
		/// <returns>The results of the modify item operation for each item.</returns>
		ItemResult[] ModifyItems(int masks, Item[] items);

		/// <summary>
		/// Removes items from the subscription.
		/// </summary>
		/// <param name="items">The identifiers (i.e. server handles) for the items being removed.</param>
		/// <returns>The results of the remove item operation for each item.</returns>
		IdentifiedResult[] RemoveItems(ItemIdentifier[] items);

		//======================================================================
		// Synchronous I/O

		/// <summary>
		/// Reads the values for a set of items in the subscription.
		/// </summary>
		/// <param name="items">The identifiers (i.e. server handles) for the items being read.</param>
		/// <returns>The value for each of items.</returns>
		ItemValueResult[] Read(Item[] items);

		/// <summary>
		/// Writes the value, quality and timestamp for a set of items in the subscription.
		/// </summary>
		/// <param name="items">The item values to write.</param>
		/// <returns>The results of the write operation for each item.</returns>
		IdentifiedResult[] Write(ItemValue[] items);

		//======================================================================
		// Asynchronous I/O

		/// <summary>
		/// Begins an asynchronous read operation for a set of items.
		/// </summary>
		/// <param name="items">The set of items to read (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		IdentifiedResult[] Read(
			Item[]                   items,
			object                   requestHandle,
			ReadCompleteEventHandler callback,
			out IRequest             request);

		/// <summary>
		/// Begins an asynchronous write operation for a set of items.
		/// </summary>
		/// <param name="items">The set of item values to write (must include the server handle).</param>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="callback">A delegate used to receive notifications when the request completes.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		IdentifiedResult[] Write(
			ItemValue[]               items,
			object                    requestHandle,
			WriteCompleteEventHandler callback,
			out IRequest              request);
		
		/// <summary>
		/// Cancels an asynchronous read or write operation.
		/// </summary>
		/// <param name="request">The object returned from the BeginRead or BeginWrite request.</param>
		/// <param name="callback">The function to invoke when the cancel completes.</param>
		void Cancel(IRequest request, CancelCompleteEventHandler callback);

		/// <summary>
		/// Causes the server to send a data changed notification for all active items. 
		/// </summary>
		void Refresh();

		/// <summary>
		/// Causes the server to send a data changed notification for all active items. 
		/// </summary>
		/// <param name="requestHandle">An identifier for the request assigned by the caller.</param>
		/// <param name="request">An object that contains the state of the request (used to cancel the request).</param>
		/// <returns>A set of results containing any errors encountered when the server validated the items.</returns>
		void Refresh(
			object       requestHandle,
			out IRequest request);
		
		/// <summary>
		/// Enables or disables data change notifications from the server.
		/// </summary>
		/// <param name="enabled">Whether data change notifications are enabled.</param>
		void SetEnabled(bool enabled);

		/// <summary>
		/// Checks whether data change notifications from the server are enabled.
		/// </summary>
		/// <returns>Whether data change notifications are enabled.</returns>
		bool GetEnabled();
	}

	//=============================================================================
	// Delegates

	/// <summary>
	/// A delegate to receive data change updates from the server.
	/// </summary>
	public delegate void DataChangedEventHandler(object subscriptionHandle, object requestHandle, ItemValueResult[] values);

	/// <summary>
	/// A delegate to receive asynchronous read completed notifications.
	/// </summary>
	public delegate void ReadCompleteEventHandler(object requestHandle, ItemValueResult[] results);

	/// <summary>
	/// A delegate to receive asynchronous write completed notifications.
	/// </summary>
	public delegate void WriteCompleteEventHandler(object requestHandle, IdentifiedResult[] results);

	/// <summary>
	/// A delegate to receive asynchronous cancel completed notifications.
	/// </summary>
	public delegate void CancelCompleteEventHandler(object requestHandle);
	
	//=============================================================================
	// Request

	/// <summary>
	/// Describes the state of a subscription.
	/// </summary>
	[Serializable]
	public class Request : IRequest 
	{	
		/// <summary>
		/// The subscription processing the request.
		/// </summary>
		public ISubscription Subscription 
		{
			get { return m_subscription; }
		}
		
		/// <summary>
		/// An unique identifier, assigned by the client, for the request.
		/// </summary>
		public object Handle 
		{
			get { return m_handle; }
		}
		
		/// <summary>
		/// Cancels the request, if possible.
		/// </summary>
		public void Cancel(CancelCompleteEventHandler callback) { m_subscription.Cancel(this, callback); }

		#region Constructors
		/// <summary>
		/// Initializes the object with a subscription and a unique id.
		/// </summary>
		public Request(ISubscription subscription, object handle)
		{
			m_subscription = subscription;
			m_handle       = handle;
		}
		#endregion
	
		#region Private Members
		private ISubscription m_subscription = null;
		private object m_handle = null;
		#endregion
	}

	//=============================================================================
	// StateMask

	/// <summary>
	/// Defines masks to be used when modifying the subscription or item state.
	/// </summary>
	[Flags]
	public enum StateMask
	{		
		/// <summary>
		/// The name of the subscription.
		/// </summary>
		Name = 0x0001,

		/// <summary>
		/// The client assigned handle for the item or subscription.
		/// </summary>
		ClientHandle = 0x0002,

		/// <summary>
		/// The locale to use for results returned to the client from the subscription.
		/// </summary>
		Locale = 0x0004,

		/// <summary>
		/// Whether the item or subscription is active.
		/// </summary>
		Active = 0x0008,

		/// <summary>
		/// The maximum rate at which data update notifications are sent.
		/// </summary>
		UpdateRate = 0x0010,

		/// <summary>
		/// The longest period between data update notifications.
		/// </summary>
		KeepAlive = 0x0020,

		/// <summary>
		/// The requested data type for the item.
		/// </summary>
		ReqType = 0x0040,

		/// <summary>
		/// The deadband for the item or subscription.
		/// </summary>
		Deadband = 0x0080,

		/// <summary>
		/// The rate at which the server should check for changes to an item value.
		/// </summary>
		SamplingRate = 0x0100,

		/// <summary>
		/// Whether the server should buffer multiple changes to a single item.
		/// </summary>
		EnableBuffering = 0x0200,

		/// <summary>
		/// All fields are valid.
		/// </summary>
		All = 0xFFFF
	}

	/// <summary>
	/// Describes the state of a subscription.
	/// </summary>
	[Serializable]
	public class SubscriptionState : ICloneable
	{		
		/// <summary>
		/// A unique name for the subscription controlled by the client.
		/// </summary>
		public string Name
		{
			get { return m_name;  }
			set { m_name = value; }
		}

		/// <summary>
		/// A unique identifier for the subscription assigned by the client.
		/// </summary>
		public object ClientHandle
		{
			get { return m_clientHandle;  }
			set { m_clientHandle = value; }
		}

		/// <summary>
		/// A unique subscription identifier assigned by the server.
		/// </summary>
		public object ServerHandle
		{
			get { return m_serverHandle;  }
			set { m_serverHandle = value; }
		}

		/// <summary>
		/// The locale used for any error messages or results returned to the client.
		/// </summary>
		public string Locale
		{
			get { return m_locale;  }
			set { m_locale = value; }
		}

		/// <summary>
		/// Whether the subscription is scanning for updates to send to the client.
		/// </summary>
		public bool Active
		{
			get { return m_active;  }
			set { m_active = value; }
		}

		/// <summary>
		/// The rate at which the server checks of updates to send to the client.
		/// </summary>
		public int UpdateRate
		{
			get { return m_updateRate;  }
			set { m_updateRate = value; }
		}

		/// <summary>
		/// The maximum period between updates sent to the client.
		/// </summary>
		public int KeepAlive
		{
			get { return m_keepAlive;  }
			set { m_keepAlive = value; }
		}

		/// <summary>
		/// The minimum percentage change required to trigger a data update for an item.
		/// </summary>
		public float Deadband
		{
			get { return m_deadband;  }
			set { m_deadband = value; }
		}

		#region Constructors
		/// <summary>
		/// Initializes object with default values.
		/// </summary>
		public SubscriptionState() 
		{
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a shallow copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return MemberwiseClone(); 
		}
		#endregion
		
		#region Private Members
		private string m_name = null;
		private object m_clientHandle = null;
		private object m_serverHandle = null;
		private string m_locale = null;
		private bool m_active = true;
		private int m_updateRate = 0;
		private int m_keepAlive = 0;
		private float m_deadband = 0;
		#endregion
	}
}
