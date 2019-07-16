//============================================================================
// TITLE: Opc.Da.Item.cs
//
// CONTENTS:
// 
// Classes used to store information related to an OPC item.
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
// 2003/09/24 RSA   Defined an equality operator for ItemIdentifier
// 2004/02/18 RSA   Updated to conform with the .NET design guidelines.

using System;
using System.Text;
using System.Xml;
using System.Collections;

namespace Opc.Da
{
	/// <summary>
	/// Describes how an item in the server address space should be accessed. 
	/// </summary>
	[Serializable]
	public class Item : Opc.ItemIdentifier
	{
		/// <summary>
		/// The data type to use when returning the item value.
		/// </summary>
		public System.Type ReqType
		{
			get { return m_reqType;  }
			set { m_reqType = value; }
		}

		/// <summary>
		/// The oldest (in milliseconds) acceptable cached value when reading an item.
		/// </summary>
		public int MaxAge
		{
			get { return m_maxAge;  }
			set { m_maxAge = value; }
		}

		/// <summary>
		/// Whether the Max Age is specified.
		/// </summary>
		public bool MaxAgeSpecified
		{
			get { return m_maxAgeSpecified;  }
			set { m_maxAgeSpecified = value; }
		}


		/// <summary>
		/// Whether the server should send data change updates. 
		/// </summary>
		public bool Active
		{
			get { return m_active;  }
			set { m_active = value; }
		}

		/// <summary>
		/// Whether the Active state is specified.
		/// </summary>
		public bool ActiveSpecified
		{
			get { return m_activeSpecified;  }
			set { m_activeSpecified = value; }
		}

		/// <summary>
		/// The minimum percentage change required to trigger a data update for an item. 
		/// </summary>
		public float Deadband
		{
			get { return m_deadband;  }
			set { m_deadband = value; }
		}

		/// <summary>
		/// Whether the Deadband is specified.
		/// </summary>
		public bool DeadbandSpecified
		{
			get { return m_deadbandSpecified;  }
			set { m_deadbandSpecified = value; }
		}

		/// <summary>
		/// How frequently the server should sample the item value.
		/// </summary>
		public int SamplingRate
		{
			get { return m_samplingRate;  }
			set { m_samplingRate = value; }
		}

		/// <summary>
		/// Whether the Sampling Rate is specified.
		/// </summary>
		public bool SamplingRateSpecified
		{
			get { return m_samplingRateSpecified;  }
			set { m_samplingRateSpecified = value; }
		}

		/// <summary>
		/// Whether the server should buffer multiple data changes between data updates.
		/// </summary>
		public bool EnableBuffering
		{
			get { return m_enableBuffering;  }
			set { m_enableBuffering = value; }
		}

		/// <summary>
		/// Whether the Enable Buffering is specified.
		/// </summary>
		public bool EnableBufferingSpecified
		{
			get { return m_enableBufferingSpecified;  }
			set { m_enableBufferingSpecified = value; }
		}

		#region Constructors
		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		public Item() {}

		/// <summary>
		/// Initializes object with the specified ItemIdentifier object.
		/// </summary>
		public Item(ItemIdentifier item) 
		{
			if (item != null)
			{
				ItemName     = item.ItemName;
				ItemPath     = item.ItemPath;
				ClientHandle = item.ClientHandle;
				ServerHandle = item.ServerHandle;
			}
		}

		/// <summary>
		/// Initializes object with the specified Item object.
		/// </summary>
		public Item(Item item) : base(item)
		{
			if (item != null)
			{
				ReqType                  = item.ReqType;
				MaxAge                   = item.MaxAge;
				MaxAgeSpecified          = item.MaxAgeSpecified;
				Active                   = item.Active;
				ActiveSpecified          = item.ActiveSpecified;
				Deadband                 = item.Deadband;
				DeadbandSpecified        = item.DeadbandSpecified;
				SamplingRate             = item.SamplingRate;
				SamplingRateSpecified    = item.SamplingRateSpecified;
				EnableBuffering          = item.EnableBuffering;
				EnableBufferingSpecified = item.EnableBufferingSpecified;
			}
		}
		#endregion
		
		#region Private Members
		private System.Type m_reqType = null;
		private int m_maxAge = 0;
		private bool m_maxAgeSpecified = false;
		private bool m_active = true;
		private bool m_activeSpecified = false;
		private float m_deadband = 0;
		private bool m_deadbandSpecified = false;
		private int m_samplingRate = 0;
		private bool m_samplingRateSpecified = false;
		private bool m_enableBuffering = false;
		private bool m_enableBufferingSpecified = false;
		#endregion
	}

	/// <summary>
	/// A collection of items.
	/// </summary>
	[Serializable]
	public class ItemCollection : ICollection, ICloneable, IList
	{
		/// <summary>
		///  Gets the item at the specified index.
		/// </summary>
		public Item this[int index]
		{
			get { return (Item)m_items[index];  }
			set { m_items[index] = value; }
		}	

		/// <summary>
		/// Gets the first item with the specified item id.
		/// </summary>
		public Item this[ItemIdentifier itemID]
		{
			get 
			{
				foreach (Item item in m_items)
				{
					if (itemID.Key == item.Key)
					{
						return item;
					}
				}

				return null;
			}
		}	

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		public ItemCollection() {}

		/// <summary>
		/// Initializes object with the specified ResultCollection object.
		/// </summary>
		public ItemCollection(ItemCollection items)
		{
			if (items != null)
			{
				foreach (Item item in items)
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
			ItemCollection clone = (ItemCollection)MemberwiseClone();

			clone.m_items = new ArrayList();

			foreach (Item item in m_items)
			{
				clone.m_items.Add(item.Clone());
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
			get { return (m_items != null)?m_items.Count:0; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_items != null)
			{
				m_items.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Item[] array, int index)
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
			return m_items.GetEnumerator();
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
			get	{ return m_items[index];  }
			
			set	
			{ 
				if (!typeof(Item).IsInstanceOfType(value))
				{
					throw new ArgumentException("May only add Item objects into the collection.");
				}
				
				m_items[index] = value; 
			}
		}

		/// <summary>
		/// Removes the IList item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index)
		{
			m_items.RemoveAt(index);
		}

		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, object value)
		{
			if (!typeof(Item).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add Item objects into the collection.");
			}

			m_items.Insert(index, value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(object value)
		{
			m_items.Remove(value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(object value)
		{
			return m_items.Contains(value);
		}

		/// <summary>
		/// Removes all items from the IList.
		/// </summary>
		public void Clear()
		{
			m_items.Clear();
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(object value)
		{
			return m_items.IndexOf(value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(object value)
		{
			if (!typeof(Item).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add Item objects into the collection.");
			}

			return m_items.Add(value);
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
		public void Insert(int index, Item value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(Item value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(Item value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(Item value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(Item value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Members
		private ArrayList m_items = new ArrayList();
		#endregion
	}

	/// <summary>
	/// The results of an operation on a uniquely identifiable item.
	/// </summary>
	[Serializable]
	public class ItemResult : Item, IResult
	{
		#region Constructors
		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		public ItemResult() {}

		/// <summary>
		/// Initializes the object with an ItemIdentifier object.
		/// </summary>
		public ItemResult(ItemIdentifier item) : base(item) {}
		
		/// <summary>
		/// Initializes the object with an ItemIdentifier object and ResultID.
		/// </summary>
		public ItemResult(ItemIdentifier item, ResultID resultID) : base(item)
		{			
			ResultID = ResultID;
		}

		/// <summary>
		/// Initializes the object with an Item object.
		/// </summary>
		public ItemResult(Item item) : base(item) {}

		/// <summary>
		/// Initializes the object with an Item object and ResultID.
		/// </summary>
		public ItemResult(Item item, ResultID resultID) : base(item)
		{			
			ResultID = resultID;
		}

		/// <summary>
		/// Initializes object with the specified ItemResult object.
		/// </summary>
		public ItemResult(ItemResult item) : base(item)
		{
			if (item != null)
			{
				ResultID       = item.ResultID;
				DiagnosticInfo = item.DiagnosticInfo;
			}
		}
		#endregion

		#region IResult Members
		/// <summary>
		/// The error id for the result of an operation on an property.
		/// </summary>
		public ResultID ResultID 
		{
			get { return m_resultID;  }
			set { m_resultID = value; }
		}	

		/// <summary>
		/// Vendor specific diagnostic information (not the localized error text).
		/// </summary>
		public string DiagnosticInfo
		{
			get { return m_diagnosticInfo;  }
			set { m_diagnosticInfo = value; }
		}
		#endregion

		#region Private Members
		private ResultID m_resultID = ResultID.S_OK; 
		private string m_diagnosticInfo = null;
		#endregion
	}
	
	/// <summary>
	/// Contains the value for a single item.
	/// </summary>
	[Serializable]
	public class ItemValue : ItemIdentifier
	{
		/// <summary>
		/// The item value.
		/// </summary>
		public object Value
		{
			get { return m_value;  }
			set { m_value = value; }
		}

		/// <summary>
		/// The quality of the item value.
		/// </summary>
		public Quality Quality
		{
			get { return m_quality;  }
			set { m_quality = value; }
		}
	
		/// <summary>
		/// Whether the quality is specified.
		/// </summary>
		public bool QualitySpecified
		{
			get { return m_qualitySpecified;  }
			set { m_qualitySpecified = value; }
		}	

		/// <summary>
		/// The UTC timestamp for the item value.
		/// </summary>
		public DateTime Timestamp
		{
			get { return m_timestamp;  }
			set { m_timestamp = value; }
		}	
	
		/// <summary>
		/// Whether the timestamp is specified.
		/// </summary>
		public bool TimestampSpecified
		{
			get { return m_timestampSpecified;  }
			set { m_timestampSpecified = value; }
		}	

		#region Constructors
		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		public ItemValue() {}

		/// <summary>
		/// Initializes the object with and ItemIdentifier object.
		/// </summary>
		public ItemValue(ItemIdentifier item) 
		{
			if (item != null)
			{
				ItemName     = item.ItemName;
				ItemPath     = item.ItemPath;
				ClientHandle = item.ClientHandle;
				ServerHandle = item.ServerHandle;
			}
		}

		/// <summary>
		/// Initializes the object with the specified item name.
		/// </summary>
		public ItemValue(string itemName) : base(itemName)
		{
		}

		/// <summary>
		/// Initializes object with the specified ItemValue object.
		/// </summary>
		public ItemValue(ItemValue item) : base(item)
		{
			if (item != null)
			{
				Value              = Opc.Convert.Clone(item.Value);
				Quality            = item.Quality;
				QualitySpecified   = item.QualitySpecified;
				Timestamp          = item.Timestamp;
				TimestampSpecified = item.TimestampSpecified;
			}
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public override object Clone() 
		{ 
			ItemValue clone = (ItemValue)MemberwiseClone();
			clone.Value = Opc.Convert.Clone(Value);
			return clone;
		}
		#endregion

		#region Private Members
		private object m_value = null;
		private Quality m_quality = Quality.Bad;
		private bool m_qualitySpecified = false;
		private DateTime m_timestamp = DateTime.MinValue;
		private bool m_timestampSpecified = false;
		#endregion
	}

	/// <summary>
	/// The results of an operation on a uniquely identifiable item value.
	/// </summary>
	[Serializable]
	public class ItemValueResult : ItemValue, IResult
	{
		#region Constructors
		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		public ItemValueResult() {}

		/// <summary>
		/// Initializes the object with an ItemIdentifier object.
		/// </summary>
		public ItemValueResult(ItemIdentifier item) : base(item) {}

		/// <summary>
		/// Initializes the object with an ItemValue object.
		/// </summary>
		public ItemValueResult(ItemValue item) : base(item) {}

		/// <summary>
		/// Initializes object with the specified ItemValueResult object.
		/// </summary>
		public ItemValueResult(ItemValueResult item) : base(item)
		{
			if (item != null)
			{
				ResultID       = item.ResultID;
				DiagnosticInfo = item.DiagnosticInfo;
			}
		}

		/// <summary>
		/// Initializes the object with the specified item name and result code.
		/// </summary>
		public ItemValueResult(string itemName, ResultID resultID) : base(itemName)
		{
			ResultID = resultID;
		}

		/// <summary>
		/// Initializes the object with the specified item name, result code and diagnostic info.
		/// </summary>
		public ItemValueResult(string itemName, ResultID resultID, string diagnosticInfo) : base(itemName)
		{
			ResultID       = resultID;
			DiagnosticInfo = diagnosticInfo;
		}

		/// <summary>
		/// Initialize object with the specified ItemIdentifier and result code.
		/// </summary>
		public ItemValueResult(ItemIdentifier item, ResultID resultID) : base(item)
		{
			ResultID = resultID;
		}

		/// <summary>
		/// Initializes the object with the specified ItemIdentifier, result code and diagnostic info.
		/// </summary>
		public ItemValueResult(ItemIdentifier item, ResultID resultID, string diagnosticInfo) : base(item)
		{
			ResultID       = resultID;
			DiagnosticInfo = diagnosticInfo;
		}
		#endregion 

		#region IResult Members
		/// <summary>
		/// The error id for the result of an operation on an property.
		/// </summary>
		public ResultID ResultID 
		{
			get { return m_resultID;  }
			set { m_resultID = value; }
		}	

		/// <summary>
		/// Vendor specific diagnostic information (not the localized error text).
		/// </summary>
		public string DiagnosticInfo
		{
			get { return m_diagnosticInfo;  }
			set { m_diagnosticInfo = value; }
		}
		#endregion

		#region Private Members
		private ResultID m_resultID = ResultID.S_OK; 
		private string m_diagnosticInfo = null;
		#endregion
	}
}
