//============================================================================
// TITLE: Opc.Hda.Annotation.cs
//
// CONTENTS:
// 
// Classes used to represent item annotations.
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
// 2004/01/26 RSA   Initial implementation.

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace Opc.Hda
{	
	/// <summary>
	/// An annotation associated with an item.
	/// </summary>
	[Serializable]
	public class AnnotationValue : ICloneable
	{
		/// <summary>
		/// The timestamp for the annotation.
		/// </summary>
		public DateTime Timestamp
		{
			get { return m_timestamp;  } 
			set { m_timestamp = value; }
		}
		
		/// <summary>
		/// The text of the annotation.
		/// </summary>
		public string Annotation
		{
			get { return m_annotation;  } 
			set { m_annotation = value; }
		}		
		
		/// <summary>
		/// The time when the annotation was created.
		/// </summary>
		public DateTime CreationTime
		{
			get { return m_creationTime;  } 
			set { m_creationTime = value; }
		}

		/// <summary>
		/// The user who created the annotation.
		/// </summary>
		public string User
		{
			get { return m_user;  } 
			set { m_user = value; }
		}
			
		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone()
		{
			return MemberwiseClone();
		}
		#endregion

		#region Private Members
		private DateTime m_timestamp = DateTime.MinValue;
		private string m_annotation = null;
		private DateTime m_creationTime = DateTime.MinValue;
		private string m_user = null;
		#endregion
	}

	/// <summary>
	/// A collection of item values passed to write or returned from a read operation.
	/// </summary>
	[Serializable]
	public class AnnotationValueCollection : Item, IResult, IActualTime, ICollection, ICloneable, IList
	{
		/// <summary>
		/// Accessor for elements in the collection.
		/// </summary>
		public AnnotationValue this[int index]
		{
			get { return (AnnotationValue)m_values[index];  }
			set { m_values[index] = value; }
		}	

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		public AnnotationValueCollection() {}

		/// <summary>
		/// Initializes object with the specified ItemIdentifier object.
		/// </summary>
		public AnnotationValueCollection(ItemIdentifier item)  : base(item) {}

		/// <summary>
		/// Initializes object with the specified Item object.
		/// </summary>
		public AnnotationValueCollection(Item item) : base(item) {}

		/// <summary>
		/// Initializes object with the specified ItemValueCollection object.
		/// </summary>
		public AnnotationValueCollection(AnnotationValueCollection item) : base(item) 
		{
			m_values = new ArrayList(item.m_values.Count);

			foreach (ItemValue value in item.m_values)
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
			AnnotationValueCollection collection = (AnnotationValueCollection)base.Clone();

			collection.m_values = new ArrayList(m_values.Count);

			foreach (AnnotationValue value in m_values)
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
		public void CopyTo(AnnotationValue[] array, int index)
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
				if (!typeof(AnnotationValue).IsInstanceOfType(value))
				{
					throw new ArgumentException("May only add AnnotationValue objects into the collection.");
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
			if (!typeof(AnnotationValue).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add AnnotationValue objects into the collection.");
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
			if (!typeof(AnnotationValue).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add AnnotationValue objects into the collection.");
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
		public void Insert(int index, AnnotationValue value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(AnnotationValue value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(AnnotationValue value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(AnnotationValue value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(AnnotationValue value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Members
		private ArrayList m_values = new ArrayList();
		private DateTime m_startTime = DateTime.MinValue;
		private DateTime m_endTime = DateTime.MinValue;
		private ResultID m_resultID = ResultID.S_OK;
		private string m_diagnosticInfo = null;
		#endregion
	}
}
