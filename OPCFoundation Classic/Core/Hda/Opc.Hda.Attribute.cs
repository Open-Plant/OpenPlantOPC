//============================================================================
// TITLE: Opc.Hda.Attribute.cs
//
// CONTENTS:
// 
// Classes used to store information related to item attributes.
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

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using Opc;

namespace Opc.Hda
{
	/// <summary>
	/// The description of an item attribute supported by the server.
	/// </summary>
	[Serializable]
	public class Attribute : ICloneable
	{
		/// <summary>
		/// A unique identifier for the attribute.
		/// </summary>
		public int ID
		{
			get { return m_id;  } 
			set { m_id = value; } 
		}

		/// <summary>
		/// The unique name for the attribute.
		/// </summary>
		public string Name
		{
			get { return m_name;  } 
			set { m_name = value; } 
		}

		/// <summary>
		/// A short description of the attribute.
		/// </summary>
		public string Description
		{
			get { return m_description;  } 
			set { m_description = value; } 
		}

		/// <summary>
		/// The data type of the attribute.
		/// </summary>
		public System.Type DataType
		{
			get { return m_datatype;  } 
			set { m_datatype = value; } 
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Name;
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a shallow copy of the object.
		/// </summary>
		public virtual object Clone() { return MemberwiseClone(); }
		#endregion
		
		#region Private Members
		private int         m_id          = 0;
		private string      m_name        = null;
		private string      m_description = null;
		private System.Type m_datatype    = null;
		#endregion
	}
	
	/// <summary>
	/// The description of an item attribute supported by the server.
	/// </summary>
	[Serializable]
	public class AttributeCollection : ICloneable, ICollection
	{
		/// <summary>
		/// Creates an empty collection.
		/// </summary>
		public AttributeCollection() {}

		/// <summary>
		/// Initializes the object with any Attributes contained in the collection.
		/// </summary>
		/// <param name="collection">A collection containing attribute descriptions.</param>
		public AttributeCollection(ICollection collection)
		{
			Init(collection);
		}

		/// <summary>
		/// Returns the attribute at the specified index.
		/// </summary>
		public Attribute this[int index]
		{
			get { return m_attributes[index];  }
			set { m_attributes[index] = value; }
		}

		/// <summary>
		/// Returns the first attribute with the specified id.
		/// </summary>
		public Attribute Find(int id)
		{
			foreach (Attribute attribute in m_attributes)
			{
				if (attribute.ID == id)
				{
					return attribute;
				}
			}

			return null;
		}

		/// <summary>
		/// Initializes the object with any attributes contained in the collection.
		/// </summary>
		/// <param name="collection">A collection containing attribute descriptions.</param>
		public void Init(ICollection collection)
		{
			Clear();

			if (collection != null)
			{
				ArrayList attributes = new ArrayList(collection.Count);

				foreach (object value in collection)
				{
					if (value.GetType() == typeof(Attribute))
					{
						attributes.Add(Opc.Convert.Clone(value));
					}
				}

				m_attributes = (Attribute[])attributes.ToArray(typeof(Attribute));
			}
		}

		/// <summary>
		/// Removes all attributes in the collection.
		/// </summary>
		public void Clear()
		{
			m_attributes = new Attribute[0];
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return new AttributeCollection(this);
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
			get { return (m_attributes != null)?m_attributes.Length:0; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_attributes != null)
			{
				m_attributes.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Attribute[] array, int index)
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
			return m_attributes.GetEnumerator();
		}
		#endregion

		#region Private Members
		private Attribute[] m_attributes = new Attribute[0];
		#endregion
	}

	/// <summary>
	/// The value of an attribute at a point in time.
	/// </summary>
	[Serializable]
	public class AttributeValue : ICloneable
	{
		/// <summary>
		/// The value of the data.
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
		
		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			AttributeValue clone = (AttributeValue)MemberwiseClone(); 
			clone.m_value = Opc.Convert.Clone(m_value);
			return clone;
		}
		#endregion
		
		#region Private Members
		private object   m_value     = null;
		private DateTime m_timestamp = DateTime.MinValue;
		#endregion
	}

	/// <summary>
	/// The set of values for an item attribute over a period of time.
	/// </summary>
	[Serializable]
	public class AttributeValueCollection : IResult, ICollection, ICloneable, IList
	{
		/// <summary>
		/// A unique identifier for the attribute.
		/// </summary>
		public int AttributeID
		{
			get { return m_attributeID;  } 
			set { m_attributeID = value; } 
		}

		/// <summary>
		/// Accessor for elements in the collection.
		/// </summary>
		public AttributeValue this[int index]
		{
			get { return (AttributeValue)m_values[index];  }
			set { m_values[index] = value; }
		}	

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		public AttributeValueCollection() {}

		/// <summary>
		/// Initializes object with the specified ItemIdentifier object.
		/// </summary>
		public AttributeValueCollection(Attribute attribute)
		{
			m_attributeID = attribute.ID;
		}

		/// <summary>
		/// Initializes object with the specified AttributeValueCollection object.
		/// </summary>
		public AttributeValueCollection(AttributeValueCollection collection)  
		{
			m_values = new ArrayList(collection.m_values.Count);

			foreach (AttributeValue value in collection.m_values)
			{
				m_values.Add(value.Clone());
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

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone()
		{
			AttributeValueCollection collection = (AttributeValueCollection)MemberwiseClone();

			collection.m_values = new ArrayList(m_values.Count);

			foreach (AttributeValue value in m_values)
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
		public void CopyTo(AttributeValue[] array, int index)
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
				if (!typeof(AttributeValue).IsInstanceOfType(value))
				{
					throw new ArgumentException("May only add AttributeValue objects into the collection.");
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
			if (!typeof(AttributeValue).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add AttributeValue objects into the collection.");
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
			if (!typeof(AttributeValue).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add AttributeValue objects into the collection.");
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
		public void Insert(int index, AttributeValue value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(AttributeValue value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(AttributeValue value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(AttributeValue value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(AttributeValue value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Members
		private int m_attributeID = 0;
		private ResultID m_resultID = ResultID.S_OK;
		private string m_diagnosticInfo = null;
		private ArrayList m_values = new ArrayList();
		#endregion
	}

	/// <summary>
	/// A collection of item attribute values passed to write or returned from a read operation.
	/// </summary>
	[Serializable]
	public class ItemAttributeCollection : ItemIdentifier, IResult, IActualTime, ICollection, IList
	{
		/// <summary>
		/// Accessor for elements in the collection.
		/// </summary>
		public AttributeValueCollection this[int index]
		{
			get { return (AttributeValueCollection)m_attributes[index];  }
			set { m_attributes[index] = value; }
		}	

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		public ItemAttributeCollection() {}

		/// <summary>
		/// Initializes object with the specified ItemIdentifier object.
		/// </summary>
		public ItemAttributeCollection(ItemIdentifier item)  : base(item) {}

		/// <summary>
		/// Initializes object with the specified ItemAttributeCollection object.
		/// </summary>
		public ItemAttributeCollection(ItemAttributeCollection item) : base(item) 
		{
			m_attributes = new ArrayList(item.m_attributes.Count);

			foreach (AttributeValueCollection value in item.m_attributes)
			{
				if (value != null)
				{
					m_attributes.Add(value.Clone());
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
			ItemAttributeCollection collection = (ItemAttributeCollection)base.Clone();

			collection.m_attributes = new ArrayList(m_attributes.Count);

			foreach (AttributeValueCollection value in m_attributes)
			{
				collection.m_attributes.Add(value.Clone());
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
			get { return (m_attributes != null)?m_attributes.Count:0; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_attributes != null)
			{
				m_attributes.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(AttributeValueCollection[] array, int index)
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
			return m_attributes.GetEnumerator();
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
			get	{ return m_attributes[index];  }
			
			set	
			{ 
				if (!typeof(AttributeValueCollection).IsInstanceOfType(value))
				{
					throw new ArgumentException("May only add AttributeValueCollection objects into the collection.");
				}
				
				m_attributes[index] = value; 
			}
		}

		/// <summary>
		/// Removes the IList item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index)
		{
			m_attributes.RemoveAt(index);
		}

		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, object value)
		{
			if (!typeof(AttributeValueCollection).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add AttributeValueCollection objects into the collection.");
			}

			m_attributes.Insert(index, value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(object value)
		{
			m_attributes.Remove(value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(object value)
		{
			return m_attributes.Contains(value);
		}

		/// <summary>
		/// Removes all items from the IList.
		/// </summary>
		public void Clear()
		{
			m_attributes.Clear();
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(object value)
		{
			return m_attributes.IndexOf(value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(object value)
		{
			if (!typeof(AttributeValueCollection).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add AttributeValueCollection objects into the collection.");
			}

			return m_attributes.Add(value);
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
		public void Insert(int index, AttributeValueCollection value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(AttributeValueCollection value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(AttributeValueCollection value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(AttributeValueCollection value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(AttributeValueCollection value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Members
		private DateTime m_startTime = DateTime.MinValue;
		private DateTime m_endTime = DateTime.MinValue;
		private ArrayList m_attributes = new ArrayList();
		private ResultID m_resultID = ResultID.S_OK;
		private string m_diagnosticInfo = null;
		#endregion
	}
	
	/// <summary>
	/// Defines constants for well-known item attributes.
	/// </summary>
	public class AttributeID
	{
		/// <remarks/>
		public const int DATA_TYPE		      = 0x01;
		/// <remarks/>
		public const int DESCRIPTION		  = 0x02;
		/// <remarks/>
		public const int ENG_UNITS		      = 0x03;
		/// <remarks/>
		public const int STEPPED		      = 0x04;
		/// <remarks/>
		public const int ARCHIVING	          = 0x05;
		/// <remarks/>
		public const int DERIVE_EQUATION      = 0x06;
		/// <remarks/>
		public const int NODE_NAME		      = 0x07;
		/// <remarks/>
		public const int PROCESS_NAME	      = 0x08;
		/// <remarks/>
		public const int SOURCE_NAME	      = 0x09;
		/// <remarks/>
		public const int SOURCE_TYPE	      = 0x0a;
		/// <remarks/>
		public const int NORMAL_MAXIMUM       = 0x0b;
		/// <remarks/>
		public const int NORMAL_MINIMUM	      = 0x0c;
		/// <remarks/>
		public const int ITEMID			      = 0x0d;
		/// <remarks/>
		public const int MAX_TIME_INT	 	  = 0x0e;
		/// <remarks/>
		public const int MIN_TIME_INT		  = 0x0f;
		/// <remarks/>
		public const int EXCEPTION_DEV	      = 0x10;
		/// <remarks/>
		public const int EXCEPTION_DEV_TYPE   = 0x11;
		/// <remarks/>
		public const int HIGH_ENTRY_LIMIT	  = 0x12;
		/// <remarks/>
		public const int LOW_ENTRY_LIMIT	  = 0x13;
	}
}
