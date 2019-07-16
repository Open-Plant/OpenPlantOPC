//============================================================================
// TITLE: Opc.Ae.ItemUrl.cs
//
// CONTENTS:
// 
// Classes used to store information related to item item urls.
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
using Opc;

namespace Opc.Ae
{
	#region ItemUrl Class
	/// <summary>
	/// The item id and network location of a DA item associated with an event source.
	/// </summary>
	[Serializable]
	public class ItemUrl : Opc.ItemIdentifier
	{
		#region Public Interface
		/// <summary>
		/// The url of the server that contains the item.
		/// </summary>
		public Opc.URL Url 
		{
			get { return m_url;  }
			set { m_url = value; }
		}	
		#endregion
	
		#region Constructors
		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		public ItemUrl() {}

		/// <summary>
		/// Initializes the object with an ItemIdentifier object.
		/// </summary>
		public ItemUrl(ItemIdentifier item) : base(item) {}
		
		/// <summary>
		/// Initializes the object with an ItemIdentifier object and url.
		/// </summary>
		public ItemUrl(ItemIdentifier item, Opc.URL url) : base(item)
		{			
			Url = url;
		}

		/// <summary>
		/// Initializes object with the specified ItemResult object.
		/// </summary>
		public ItemUrl(ItemUrl item) : base(item)
		{
			if (item != null)
			{
				Url = item.Url;
			}
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public override object Clone() 
		{ 
			return new ItemUrl(this);
		}
		#endregion
		
		#region Private Members
		private Opc.URL m_url = new URL(); 
		#endregion
	}
	#endregion

	#region ItemUrlCollection Class
	/// <summary>
	/// Contains a collection of item urls.
	/// </summary>
	public class ItemUrlCollection : ReadOnlyCollection
	{
		#region Public Interface		
		/// <summary>
		/// An indexer for the collection.
		/// </summary>
		public new ItemUrl this[int index]
		{
			get	{ return (ItemUrl)Array.GetValue(index); }
		}

		/// <summary>
		/// Returns a copy of the collection as an array.
		/// </summary>
		public new ItemUrl[] ToArray()
		{
			return (ItemUrl[])Opc.Convert.Clone(Array);
		}

		/// <summary>
		/// Constructs an empty collection.
		/// </summary>
		public ItemUrlCollection() : base(new ItemUrl[0]) {}

		/// <summary>
		/// Constructs a collection from an array of item urls.
		/// </summary>
		public ItemUrlCollection(ItemUrl[] itemUrls) : base(itemUrls) {}
		#endregion
	}
	#endregion
}
