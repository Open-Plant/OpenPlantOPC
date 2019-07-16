//============================================================================
// TITLE: Opc.Hda.Item.cs
//
// CONTENTS:
// 
// Classes used to represent items and item values.
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
// 2004/01/04 RSA   Initial implementation.

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace Opc.Hda
{
	/// <summary>
	/// Describes an item used in a request for processed or raw data.
	/// </summary>
	[Serializable]
	public class Item : ItemIdentifier
	{
		/// <summary>
		/// The aggregate to use to process the data.
		/// </summary>
		public int AggregateID
		{
			get { return m_aggregateID;  } 
			set { m_aggregateID = value; }
		}

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		public Item() {}

		/// <summary>
		/// Initializes object with the specified ItemIdentifier object.
		/// </summary>
		public Item(ItemIdentifier item)  : base(item) {}

		/// <summary>
		/// Initializes object with the specified Item object.
		/// </summary>
		public Item(Item item) : base(item)
		{
			if (item != null)
			{
				AggregateID = item.AggregateID;
			}
		}
		
		#region Private Members
		private int m_aggregateID = Opc.Hda.AggregateID.NOAGGREGATE;
		#endregion
	}

	/// <summary>
	/// Describes the results for an item used in a read processed or raw data request.
	/// </summary>
	[Serializable]
	public class ItemResult : Item, IResult
	{
		/// <summary>
		/// Initialize object with default values.
		/// </summary>
		public ItemResult() {}

		/// <summary>
		/// Initialize object with the specified ItemIdentifier object.
		/// </summary>
		public ItemResult(ItemIdentifier item) : base(item) {}

		/// <summary>
		/// Initializes object with the specified Item object.
		/// </summary>
		public ItemResult(Item item)  : base(item) {}

		/// <summary>
		/// Initialize object with the specified ItemResult object.
		/// </summary>
		public ItemResult(ItemResult item) : base(item)
		{
			if (item != null)
			{
				ResultID       = item.ResultID;
				DiagnosticInfo = item.DiagnosticInfo;
			}
		}

		#region IResult Members
		/// <summary>
		/// The error id for the result of an operation on an item.
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
	/// Defines possible HDA quality codes.
	/// </summary>
	[Flags]
	public enum Quality
	{
		/// <summary>
		/// More than one piece of data that may be hidden exists at same timestamp.
		/// </summary>
		ExtraData = 0x00010000,

		/// <summary>
		/// Interpolated data value.
		/// </summary>
		Interpolated = 0x00020000,

		/// <summary>
		/// Raw data
		/// </summary>
		Raw	= 0x00040000,

		/// <summary>
		/// Calculated data value, as would be returned from a ReadProcessed call.
		/// </summary>
		Calculated = 0x00080000,

		/// <summary>
		/// No data found to provide upper or lower bound value. 
		/// </summary>
		NoBound = 0x00100000,

		/// <summary>
		/// Bad No data collected. Archiving not active (for item or all items).
		/// </summary>
		NoData = 0x00200000,
		
		/// <summary>
		/// Collection started/stopped/lost.
		/// </summary>
		DataLost = 0x00400000,

		/// <summary>
		/// Scaling or conversion error. 
		/// </summary>
		Conversion = 0x00800000,

		/// <summary>
		/// Aggregate value is for an incomplete interval. 
		/// </summary>
		Partial = 0x01000000
	}

	/// <summary>
	/// A value of an item at in instant of time.
	/// </summary>
	[Serializable]
	public class ItemValue : ICloneable
	{
		/// <summary>
		/// The value of the item.
		/// </summary>
		public object Value
		{
			get { return m_value;  } 
			set { m_value = value; }
		}

		/// <summary>
		/// The timestamp associated with the value.
		/// </summary>
		public DateTime Timestamp
		{
			get { return m_timestamp;  } 
			set { m_timestamp = value; }
		}

		/// <summary>
		/// The quality associated with the value when it was acquired from the data source.
		/// </summary>
		public Opc.Da.Quality Quality
		{
			get { return m_quality;  } 
			set { m_quality = value; }
		}		
		
		/// <summary>
		/// The quality associated with the value when it was retrieved from the hiatorian database.
		/// </summary>
		public Opc.Hda.Quality HistorianQuality
		{
			get { return m_historianQuality;  } 
			set { m_historianQuality = value; }
		}
		
		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public object Clone()
		{
			ItemValue value = (ItemValue)MemberwiseClone();
			value.Value = Opc.Convert.Clone(Value);
			return value;
		}
		#endregion
		
		#region Private Members
		private object m_value = null;
		private DateTime m_timestamp = DateTime.MinValue;
		private Opc.Da.Quality m_quality = Opc.Da.Quality.Bad;
		private Opc.Hda.Quality m_historianQuality = Opc.Hda.Quality.NoData;
		#endregion
	}

	/// <summary>
	/// A interface used to actual time information associated with a result.
	/// </summary>
	public interface IActualTime
	{
		/// <summary>
		/// The actual start time used by a server while processing a request.
		/// </summary>
		DateTime StartTime { get; set; }
		
		/// <summary>
		/// The actual end time used by a server while processing a request.
		/// </summary>
		DateTime EndTime { get; set; }
	}

	/// <summary>
	/// A collection of item values passed to write or returned from a read operation.
	/// </summary>
	[Serializable]
	public class ItemValueCollection : Item, IResult, IActualTime, ICollection, ICloneable, IList
	{
		/// <summary>
		/// Accessor for elements in the collection.
		/// </summary>
		public ItemValue this[int index]
		{
			get { return (ItemValue)m_values[index];  }
			set { m_values[index] = value; }
		}	

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		public ItemValueCollection() {}

		/// <summary>
		/// Initializes object with the specified ItemIdentifier object.
		/// </summary>
		public ItemValueCollection(ItemIdentifier item)  : base(item) {}

		/// <summary>
		/// Initializes object with the specified Item object.
		/// </summary>
		public ItemValueCollection(Item item) : base(item) {}

		/// <summary>
		/// Initializes object with the specified ItemValueCollection object.
		/// </summary>
		public ItemValueCollection(ItemValueCollection item) : base(item) 
		{
			m_values = new ArrayList(item.m_values.Count);

			foreach (ItemValue value in item.m_values)
			{
				if (value != null)
				{
					m_values.Add(value.Clone());
				}
			}
		}

		/// <summary>
		/// Appends another value collection to the collection.
		/// </summary>
		public void AddRange(ItemValueCollection collection)
		{
			if (collection != null)
			{
				foreach (ItemValue value in collection)
				{
					if (value != null)
					{
						m_values.Add(value.Clone());
					}
				}
			}
		}

		#region IResult Members
		/// <summary>
		/// The error id for the result of an operation on an item.
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
		
		#region IActualTime Members
		/// <summary>
		/// The actual start time used by a server while processing a request.
		/// </summary>
		public DateTime StartTime
		{
			get { return m_startTime;  } 
			set { m_startTime = value; }
		}

		/// <summary>
		/// The actual end time used by a server while processing a request.
		/// </summary>
		public DateTime EndTime
		{
			get { return m_endTime;  } 
			set { m_endTime = value; }
		}
		#endregion
        
		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public override object Clone()
		{
			ItemValueCollection collection = (ItemValueCollection)base.Clone();

			collection.m_values = new ArrayList(m_values.Count);

			foreach (ItemValue value in m_values)
			{
				collection.m_values.Add(value.Clone());
			}

			return collection;
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
			get { return (m_values != null)?m_values.Count:0; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_values != null)
			{
				m_values.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(ItemValue[] array, int index)
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
			return m_values.GetEnumerator();
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
			get	{ return m_values[index];  }
			
			set	
			{ 
				if (!typeof(ItemValue).IsInstanceOfType(value))
				{
					throw new ArgumentException("May only add ItemValue objects into the collection.");
				}
				
				m_values[index] = value; 
			}
		}

		/// <summary>
		/// Removes the IList item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index)
		{
			m_values.RemoveAt(index);
		}

		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, object value)
		{
			if (!typeof(ItemValue).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add ItemValue objects into the collection.");
			}

			m_values.Insert(index, value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(object value)
		{
			m_values.Remove(value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(object value)
		{
			return m_values.Contains(value);
		}

		/// <summary>
		/// Removes all items from the IList.
		/// </summary>
		public void Clear()
		{
			m_values.Clear();
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(object value)
		{
			return m_values.IndexOf(value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(object value)
		{
			if (!typeof(ItemValue).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add ItemValue objects into the collection.");
			}

			return m_values.Add(value);
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
		public void Insert(int index, ItemValue value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(ItemValue value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(ItemValue value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(ItemValue value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(ItemValue value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Members
		private DateTime m_startTime = DateTime.MinValue;
		private DateTime m_endTime = DateTime.MinValue;
		private ArrayList m_values = new ArrayList();
		private ResultID m_resultID = ResultID.S_OK;
		private string m_diagnosticInfo = null;
		#endregion
	}
	
	/// <summary>
	/// A value of an item at in instant of time that has be deleted or replaced.
	/// </summary>
	[Serializable]
	public class ModifiedValue : ItemValue
	{
		/// <summary>
		/// The time when the value was deleted or replaced.
		/// </summary>
		public DateTime ModificationTime
		{
			get { return m_modificationTime;  } 
			set { m_modificationTime = value; }
		}

		/// <summary>
		/// Whether the value was deleted or replaced.
		/// </summary>
		public EditType EditType
		{
			get { return m_editType;  } 
			set { m_editType = value; }
		}		
		
		/// <summary>
		/// The user who modified the item value.
		/// </summary>
		public string User
		{
			get { return m_user;  } 
			set { m_user = value; }
		}
			
		#region Private Members
		private DateTime m_modificationTime = DateTime.MinValue;
		private EditType m_editType = EditType.Insert;
		private string m_user = null;
		#endregion
	}
	
	/// <summary>
	/// A collection of modified item values with a result code indicating the results of a read operation.
	/// </summary>
	[Serializable]
	public class ModifiedValueCollection : ItemValueCollection
	{
		/// <summary>
		/// Accessor for elements in the collection.
		/// </summary>
		public new ModifiedValue this[int index]
		{
			get { return (ModifiedValue)this[index];  }
			set { this[index] = value; }
		}	

		/// <summary>
		/// Initialize object with default values.
		/// </summary>
		public ModifiedValueCollection() {}

		/// <summary>
		/// Initialize object with the specified ItemIdentifier object.
		/// </summary>
		public ModifiedValueCollection(ItemIdentifier item) : base(item) {}

		/// <summary>
		/// Initializes object with the specified Item object.
		/// </summary>
		public ModifiedValueCollection(Item item)  : base(item) {}

		/// <summary>
		/// Initializes object with the specified ItemValueCollection object.
		/// </summary>
		public ModifiedValueCollection(ItemValueCollection item)  : base(item) {}
	}

	/// <summary>
	/// The types of modifications that can be applied to an item.
	/// </summary>
	public enum EditType
	{
		/// <summary>
		/// The item was inserted.
		/// </summary>
		Insert = 1,

		/// <summary>
		/// The item was replaced.
		/// </summary>
		Replace = 2,

		/// <summary>
		/// The item was inserted or replaced during an insert/replace operation.
		/// </summary>
		InsertReplace = 3,

		/// <summary>
		/// The item was deleted.
		/// </summary>
		Delete = 4
	}

	/// <summary>
	/// A result associated with a single item value.
	/// </summary>
	[Serializable]
	public class Result : ICloneable, IResult
	{		
		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		public Result() {}

		/// <summary>
		/// Initializes the object with the specified result id.
		/// </summary>
		public Result(ResultID resultID)
		{
			ResultID       = resultID;
			DiagnosticInfo = null;
		}

		/// <summary>
		/// Initializes the object with the specified result object.
		/// </summary>
		public Result(IResult result)
		{
			ResultID       = result.ResultID;
			DiagnosticInfo = result.DiagnosticInfo;
		}

		#region IResult Members
		/// <summary>
		/// The error id for the result of an operation on an item.
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

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public object Clone()
		{
			return MemberwiseClone();
		}
		#endregion
		
		#region Private Members
		private ResultID m_resultID = ResultID.S_OK;
		private string m_diagnosticInfo = null;
		#endregion
	}

	/// <summary>
	/// A collection of results passed to write or returned from an insert, replace or delete operation.
	/// </summary>
	[Serializable]
	public class ResultCollection : ItemIdentifier, ICollection, ICloneable, IList
	{
		/// <summary>
		/// Accessor for elements in the collection.
		/// </summary>
		public Result this[int index]
		{
			get { return (Result)m_results[index];  }
			set { m_results[index] = value; }
		}	

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		public ResultCollection() {}

		/// <summary>
		/// Initializes object with the specified ItemIdentifier object.
		/// </summary>
		public ResultCollection(ItemIdentifier item)  : base(item) {}

		/// <summary>
		/// Initializes object with the specified ResultCollection object.
		/// </summary>
		public ResultCollection(ResultCollection item) : base(item) 
		{
			m_results = new ArrayList(item.m_results.Count);

			foreach (Result value in item.m_results)
			{
				m_results.Add(value.Clone());
			}
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public override object Clone()
		{
			ResultCollection collection = (ResultCollection)base.Clone();

			collection.m_results = new ArrayList(m_results.Count);

			foreach (ResultCollection value in m_results)
			{
				collection.m_results.Add(value.Clone());
			}

			return collection;
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
			get { return (m_results != null)?m_results.Count:0; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_results != null)
			{
				m_results.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Result[] array, int index)
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
			return m_results.GetEnumerator();
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
			get	{ return m_results[index];  }
			
			set	
			{ 
				if (!typeof(Result).IsInstanceOfType(value))
				{
					throw new ArgumentException("May only add Result objects into the collection.");
				}
				
				m_results[index] = value; 
			}
		}

		/// <summary>
		/// Removes the IList item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index)
		{
			m_results.RemoveAt(index);
		}

		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, object value)
		{
			if (!typeof(Result).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add Result objects into the collection.");
			}

			m_results.Insert(index, value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(object value)
		{
			m_results.Remove(value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(object value)
		{
			return m_results.Contains(value);
		}

		/// <summary>
		/// Removes all items from the IList.
		/// </summary>
		public void Clear()
		{
			m_results.Clear();
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(object value)
		{
			return m_results.IndexOf(value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(object value)
		{
			if (!typeof(Result).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add Result objects into the collection.");
			}

			return m_results.Add(value);
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
		public void Insert(int index, Result value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(Result value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(Result value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(Result value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(Result value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Members
		private ArrayList m_results = new ArrayList();
		#endregion
	}

	/// <summary>
	/// A collection of results passed to write or returned from an insert, replace or delete operation.
	/// </summary>
	[Serializable]
	public class ItemTimeCollection : ItemIdentifier, ICollection, ICloneable, IList
	{
		/// <summary>
		/// Accessor for elements in the collection.
		/// </summary>
		public DateTime this[int index]
		{
			get { return (DateTime)m_times[index];  }
			set { m_times[index] = value; }
		}	

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		public ItemTimeCollection() {}

		/// <summary>
		/// Initializes object with the specified ItemIdentifier object.
		/// </summary>
		public ItemTimeCollection(ItemIdentifier item)  : base(item) {}

		/// <summary>
		/// Initializes object with the specified ItemTimeCollection object.
		/// </summary>
		public ItemTimeCollection(ItemTimeCollection item) : base(item) 
		{
			m_times = new ArrayList(item.m_times.Count);

			foreach (DateTime value in item.m_times)
			{
				m_times.Add(value);
			}
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public override object Clone()
		{
			ItemTimeCollection collection = (ItemTimeCollection)base.Clone();

			collection.m_times = new ArrayList(m_times.Count);

			foreach (DateTime value in m_times)
			{
				collection.m_times.Add(value);
			}

			return collection;
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
			get { return (m_times != null)?m_times.Count:0; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_times != null)
			{
				m_times.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(DateTime[] array, int index)
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
			return m_times.GetEnumerator();
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
			get	{ return m_times[index];  }
			
			set	
			{ 
				if (!typeof(DateTime).IsInstanceOfType(value))
				{
					throw new ArgumentException("May only add DateTime objects into the collection.");
				}
				
				m_times[index] = value; 
			}
		}

		/// <summary>
		/// Removes the IList item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index)
		{
			m_times.RemoveAt(index);
		}

		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, object value)
		{
			if (!typeof(DateTime).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add DateTime objects into the collection.");
			}

			m_times.Insert(index, value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(object value)
		{
			m_times.Remove(value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(object value)
		{
			return m_times.Contains(value);
		}

		/// <summary>
		/// Removes all items from the IList.
		/// </summary>
		public void Clear()
		{
			m_times.Clear();
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(object value)
		{
			return m_times.IndexOf(value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(object value)
		{
			if (!typeof(DateTime).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add DateTime objects into the collection.");
			}

			return m_times.Add(value);
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
		public void Insert(int index, DateTime value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(DateTime value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(DateTime value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(DateTime value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(DateTime value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Members
		private ArrayList m_times = new ArrayList();
		#endregion
	}
}
