//============================================================================
// TITLE: ItemList.cs
//
// CONTENTS:
// 
// Classes used to store lists of OPC items. 
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
using System.Collections;
using Opc.Da;

namespace OpcXml.Da
{
	/// <summary>
	/// A XML-DA specific item value result that is used for subscriptions.
	/// </summary>
	public class SubscribeItemValueResult : Opc.Da.ItemValueResult
	{
		/// <summary>
		/// The actual sampling rate used for items in a subscription.
		/// </summary>
		public int SamplingRate = 0;
		/// <remarks/>
		public bool SamplingRateSpecified = false;

		/// <summary>
		/// Initializes the object with the default values;
		/// </summary>
		public SubscribeItemValueResult() {}

		/// <summary>
		/// Initializes the object with an ItemIdentifier object.
		/// </summary>
		public SubscribeItemValueResult(Opc.ItemIdentifier item) : base(item) {}

		/// <summary>
		/// Initializes the object with an ItemValue object.
		/// </summary>
		public SubscribeItemValueResult(ItemValue item) : base(item) {}
	}

	/// <summary>
	/// Contains properties that uniquely identify an OPC item list. 
	/// </summary>
	public class List : ArrayList
	{
		/// <summary>
		/// An optional identifier for an item list.
		/// </summary>
		public string Name = null;

		/// <summary>
		/// A unique list identifier assigned by the client.
		/// </summary>
		public object ClientHandle = null;

		/// <summary>
		/// A unique list identifier assigned by the server.
		/// </summary>
		public object ServerHandle = null;
	}

	/// <summary>
	/// Contain a list of items and default values for properties used to access an OPC item.
	/// </summary>
	public class ItemList : List
	{
		/// <summary>
		/// The default item path for items in the list.
		/// </summary>
		public string ItemPath = null;

		/// <summary>
		/// The default data type to use when reading items in the list.
		/// </summary>
		public System.Type ReqType = null;

		/// <summary>
		/// The default maximum age to use when reading items in the list.
		/// </summary>
		public int MaxAge = 0;
		/// <remarks/>
		public bool MaxAgeSpecified = false;

		/// <summary>
		/// The default deadband to use when subscribing to the items in the list.
		/// </summary>
		public float Deadband = 0;
		/// <remarks/>
		public bool DeadbandSpecified = false;

		/// <summary>
		/// The default sampling rate to use when subscribing to the items in the list.
		/// </summary>
		public int SamplingRate = 0;
		/// <remarks/>
		public bool SamplingRateSpecified = false;

		/// <summary>
		/// The default buffering behavior to use when subscribing to the items in the list.
		/// </summary>
		public bool EnableBuffering = false;
		/// <remarks/>
		public bool EnableBufferingSpecified = false;

		/// <summary>
		/// Accesses the item at the specified index.
		/// </summary>
		public new Item this[int index]
		{
			get { return (Item)base[index]; }
			set { base[index] = value; }
		}
	}
	
	/// <summary>
	/// Contains a list of item results and revised default values for item access properties.
	/// </summary>
	public class ItemValueResultList : List
	{
		/// <summary>
		/// The actual sampling rate used for items in a subscription.
		/// </summary>
		public int SamplingRate = 0;
		/// <remarks/>
		public bool SamplingRateSpecified = false;

		/// <summary>
		/// Accesses the items at the specified index.
		/// </summary>
		public new Opc.Da.ItemValueResult this[int index]
		{
			get { return (Opc.Da.ItemValueResult)base[index]; }
			set { base[index] = value; }
		}
	}

	/// <summary>
	/// Contains a list of item values.
	/// </summary>
	public class ItemValueList : List
	{
		/// <summary>
		/// Accesses the items at the specified index.
		/// </summary>
		public new ItemValue this[int index]
		{
			get { return (ItemValue)base[index]; }
			set { base[index] = value; }
		}
	}
}
