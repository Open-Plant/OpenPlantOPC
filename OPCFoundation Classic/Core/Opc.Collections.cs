//============================================================================
// TITLE: Opc.Collections.cs
//
// CONTENTS:
// 
// Contains collections classes that can be used as object properties.
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
// 2004/11/13 RSA   Initial implementation.

using System;
using System.Xml;
using System.Collections;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Runtime.Serialization;

namespace Opc
{
	#region ReadOnlyCollection Class
	/// <summary>
	/// A read only collection class which can be used to expose arrays as properties of classes.
	/// </summary>
	[Serializable]
	public class ReadOnlyCollection : ICollection, ICloneable, ISerializable
	{	
		#region Public Interface
		/// <summary>
		/// An indexer for the collection.
		/// </summary>
		public virtual object this[int index]
		{
			get	{ return m_array.GetValue(index); }
		}

		/// <summary>
		/// Returns a copy of the collection as an array.
		/// </summary>
		public virtual Array ToArray()
		{
			return (Array)Opc.Convert.Clone(m_array);
		}
		#endregion
		
		#region Protected Interface
		/// <summary>
		///Creates a collection that wraps the specified array instance.
		/// </summary>
		protected ReadOnlyCollection(Array array)
		{
			Array = array;
		}

		/// <summary>
		/// The array instance exposed by the collection.
		/// </summary>
		protected virtual Array Array
		{
			get { return m_array;  }
			
			set 
			{ 
				m_array = value; 

				if (m_array == null)
				{
					m_array = new object[0];
				}
			}
		}
		#endregion

		#region ISerializable Members
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string ARRAY = "AR";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected ReadOnlyCollection(SerializationInfo info, StreamingContext context)
		{	
			m_array = (Array)info.GetValue(Names.ARRAY, typeof(Array));
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{			
			info.AddValue(Names.ARRAY, m_array);
		}
		#endregion

		#region ICollection Members
		/// <summary>
		/// Indicates whether access to the ICollection is synchronized (thread-safe).
		/// </summary>
		public virtual bool IsSynchronized
		{
			get	{ return false; }
		}

		/// <summary>
		/// Gets the number of objects in the collection.
		/// </summary>
		public virtual int Count
		{
			get { return m_array.Length; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public virtual void CopyTo(Array array, int index)
		{
			if (m_array != null)
			{
				m_array.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Indicates whether access to the ICollection is synchronized (thread-safe).
		/// </summary>
		public virtual object SyncRoot
		{
			get	{ return this; }
		}
		#endregion

		#region IEnumerable Members
		/// <summary>
		/// Returns an enumerator that can iterate through a collection.
		/// </summary>
		/// <returns>An IEnumerator that can be used to iterate through the collection.</returns>
		public virtual IEnumerator GetEnumerator()
		{
			return m_array.GetEnumerator();
		}
		#endregion
		
		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the collection.
		/// </summary>
		public virtual object Clone()
		{
			ReadOnlyCollection clone = (ReadOnlyCollection)this.MemberwiseClone();
	
			ArrayList array = new ArrayList(m_array.Length);

			// clone the elements and determine the element type.
			System.Type elementType = null;

			for (int ii = 0; ii < m_array.Length; ii++)
			{
				object element = m_array.GetValue(ii);

				if (elementType == null)
				{
					elementType = element.GetType();
				}
				else if (elementType != typeof(object))
				{
					while (!elementType.IsInstanceOfType(element))
					{
						elementType = elementType.BaseType;
					}
				}

				array.Add(Opc.Convert.Clone(element));
			}

			// convert array list to an array.
			clone.Array = array.ToArray(elementType);

			return clone;
		}
		#endregion

		#region Private Members
		private Array m_array = null;
		#endregion
	}
	#endregion

	#region WriteableCollection Class
	/// <summary>
	/// A writeable collection class which can be used to expose arrays as properties of classes.
	/// </summary>
	[Serializable]
	public class WriteableCollection : ICollection, IList, ICloneable, ISerializable
	{	
		#region Public Interface
		/// <summary>
		/// An indexer for the collection.
		/// </summary>
		public virtual object this[int index]
		{
			get	{ return m_array[index];  }
			set	{ m_array[index] = value; }
		}

		/// <summary>
		/// Returns a copy of the collection as an array.
		/// </summary>
		public virtual Array ToArray()
		{
			return m_array.ToArray(m_elementType);
		}

		/// <summary>
		/// Adds a list of values to the collection.
		/// </summary>
		public virtual void AddRange(ICollection collection)
		{
			if (collection != null)
			{
				foreach (object element in collection)
				{
					ValidateElement(element);
				}

				m_array.AddRange(collection);
			}
		}
		#endregion
		
		#region Protected Interface
		/// <summary>
		/// Creates a collection that wraps the specified array instance.
		/// </summary>
		protected WriteableCollection(ICollection array, System.Type elementType)
		{
			// copy array.
			if (array != null)
			{			
				m_array = new ArrayList(array);
			}
			else
			{
				m_array = new ArrayList();
			}

			// set default element type.
			m_elementType = typeof(object);

			// verify that current contents of the array are the correct type.
			if (elementType != null)
			{
				foreach (object element in m_array)
				{
					ValidateElement(element);
				}

				m_elementType = elementType;
			}
		}

		/// <summary>
		/// The array instance exposed by the collection.
		/// </summary>
		protected virtual ArrayList Array
		{
			get { return m_array;  }
			
			set 
			{ 
				m_array = value; 

				if (m_array == null)
				{
					m_array = new ArrayList();
				}
			}
		}

		/// <summary>
		/// The type of objects allowed in the collection.
		/// </summary>
		protected virtual System.Type ElementType
		{
			get { return m_elementType; }
			
			set 
			{
				// verify that current contents of the array are the correct type.
				foreach (object element in m_array)
				{
					ValidateElement(element);
				}

				m_elementType = value; 
			}
		}
		
		/// <summary>
		/// Throws an exception if the element is not valid for the collection.
		/// </summary>
		protected virtual void ValidateElement(object element)
		{
			if (element == null)
			{
				throw new ArgumentException(String.Format(INVALID_VALUE, element));
			}

			if (!m_elementType.IsInstanceOfType(element))
			{
				throw new ArgumentException(String.Format(INVALID_TYPE, element.GetType()));
			}
		}

		/// <remarks/>
		protected const string INVALID_VALUE = "The value '{0}' cannot be added to the collection.";
		/// <remarks/>
		protected const string INVALID_TYPE  = "A value with type '{0}' cannot be added to the collection.";
		#endregion

		#region ISerializable Members
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string COUNT        = "CT";
			internal const string ELEMENT      = "EL";
			internal const string ELEMENT_TYPE = "ET";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected WriteableCollection(SerializationInfo info, StreamingContext context)
		{	
			m_elementType = (System.Type)info.GetValue(Names.ELEMENT_TYPE, typeof(System.Type));

			int count = (int)info.GetValue(Names.COUNT, typeof(int));

			m_array = new ArrayList(count);

			for (int ii = 0; ii < count; ii++)
			{
				m_array.Add(info.GetValue(Names.ELEMENT + ii.ToString(), typeof(object)));
			}
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(Names.ELEMENT_TYPE, m_elementType);
			info.AddValue(Names.COUNT, m_array.Count);

			for (int ii = 0; ii < m_array.Count; ii++)
			{
				info.AddValue(Names.ELEMENT + ii.ToString(), m_array[ii]);
			}		
		}
		#endregion

		#region ICollection Members
		/// <summary>
		/// Indicates whether access to the ICollection is synchronized (thread-safe).
		/// </summary>
		public virtual bool IsSynchronized
		{
			get	{ return false; }
		}

		/// <summary>
		/// Gets the number of objects in the collection.
		/// </summary>
		public virtual int Count
		{
			get { return m_array.Count; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public virtual void CopyTo(Array array, int index)
		{
			if (m_array != null)
			{
				m_array.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Indicates whether access to the ICollection is synchronized (thread-safe).
		/// </summary>
		public virtual object SyncRoot
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
			return m_array.GetEnumerator();
		}
		#endregion
		
		#region IList Members
		/// <summary>
		/// Gets a value indicating whether the collection is read-only.
		/// </summary>
		public virtual bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// Gets or sets the element at the specified index.
		/// </summary>
		object System.Collections.IList.this[int index]
		{
			get	{ return this[index];  }			
			set	{ this[index] = value; }
		}
        
		/// <summary>
		/// Removes the IList item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public virtual void RemoveAt(int index)
		{
			m_array.RemoveAt(index);
		}

		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public virtual void Insert(int index, object value)
		{
			ValidateElement(value);
			m_array.Insert(index, value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public virtual void Remove(object value)
		{
			m_array.Remove(value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public virtual  bool Contains(object value)
		{
			return m_array.Contains(value);
		}

		/// <summary>
		/// Removes all items from the IList.
		/// </summary>
		public virtual void Clear()
		{
			m_array.Clear();
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public virtual int IndexOf(object value)
		{
			return m_array.IndexOf(value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public virtual int Add(object value)
		{
			ValidateElement(value);
			return m_array.Add(value);
		}

		/// <summary>
		/// Indicates whether the IList has a fixed size.
		/// </summary>
		public virtual bool IsFixedSize
		{
			get	{ return false; }
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the collection.
		/// </summary>
		public virtual object Clone()
		{
			WriteableCollection clone = (WriteableCollection)this.MemberwiseClone();

			clone.m_array = new ArrayList();

			for (int ii = 0; ii < m_array.Count; ii++)
			{
				clone.Add(Opc.Convert.Clone(m_array[ii]));
			}

			return clone;
		}
		#endregion

		#region Private Members
		private ArrayList m_array = null;
		private System.Type m_elementType = null;
		#endregion
	}
	#endregion

	#region ReadOnlyDictionary Class
	/// <summary>
	/// A read only dictionary class which can be used to expose arrays as properties of classes.
	/// </summary>
	[Serializable]
	public class ReadOnlyDictionary : IDictionary, ISerializable
	{		
		#region Protected Interface
		/// <summary>
		///Creates a collection that wraps the specified array instance.
		/// </summary>
		protected ReadOnlyDictionary(Hashtable dictionary)
		{
			Dictionary = dictionary;
		}

		/// <summary>
		/// The array instance exposed by the collection.
		/// </summary>
		protected virtual Hashtable Dictionary
		{
			get { return m_dictionary;  }
			
			set 
			{ 
				m_dictionary = value; 

				if (m_dictionary == null)
				{
					m_dictionary = new Hashtable();
				}
			}
		}
		#endregion

		#region ISerializable Members
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string COUNT = "CT";
			internal const string KEY   = "KY";
			internal const string VALUE = "VA";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected ReadOnlyDictionary(SerializationInfo info, StreamingContext context)
		{	
			int count = (int)info.GetValue(Names.COUNT, typeof(int));

			m_dictionary = new Hashtable();

			for (int ii = 0; ii < count; ii++)
			{
				object key   = info.GetValue(Names.KEY + ii.ToString(), typeof(object));
				object value = info.GetValue(Names.VALUE + ii.ToString(), typeof(object));

				if (key != null)
				{
					m_dictionary[key] = value;
				}
			}
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{			
			info.AddValue(Names.COUNT, m_dictionary.Count);

			int ii = 0;

			IDictionaryEnumerator enumerator = m_dictionary.GetEnumerator();

			while (enumerator.MoveNext())
			{
				info.AddValue(Names.KEY + ii.ToString(), enumerator.Key);
				info.AddValue(Names.VALUE + ii.ToString(), enumerator.Value);

				ii++;
			}			
		}
		#endregion

		#region IDictionary Members
		/// <summary>
		/// Gets a value indicating whether the IDictionary is read-only.
		/// </summary>
		public virtual bool IsReadOnly
		{
			get	{ return true; }
		}

		/// <summary>
		/// Returns an IDictionaryEnumerator for the IDictionary.
		/// </summary>
		public virtual IDictionaryEnumerator GetEnumerator()
		{
			return m_dictionary.GetEnumerator();
		}		

		/// <summary>
		/// Gets or sets the element with the specified key. 
		/// </summary>
		public virtual object this[object key]
		{
			get
			{
				return m_dictionary[key];
			}

			set
			{
				throw new InvalidOperationException(READ_ONLY_DICTIONARY);
			}
		}

		/// <summary>
		/// Removes the element with the specified key from the IDictionary.
		/// </summary>
		public virtual void Remove(object key)
		{
			throw new InvalidOperationException(READ_ONLY_DICTIONARY);
		}

		/// <summary>
		/// Determines whether the IDictionary contains an element with the specified key.
		/// </summary>
		public virtual bool Contains(object key)
		{
			return m_dictionary.Contains(key);
		}

		/// <summary>
		/// Removes all elements from the IDictionary.
		/// </summary>
		public virtual void Clear()
		{
			throw new InvalidOperationException(READ_ONLY_DICTIONARY);
		}

		/// <summary>
		/// Gets an ICollection containing the values in the IDictionary.
		/// </summary>
		public virtual ICollection Values
		{
			get	{ return m_dictionary.Values; }
		}

		/// <summary>
		/// Adds an element with the provided key and value to the IDictionary.
		/// </summary>
		public void Add(object key, object value)
		{
			throw new InvalidOperationException(READ_ONLY_DICTIONARY);
		}

		/// <summary>
		/// Gets an ICollection containing the keys of the IDictionary.
		/// </summary>
		public virtual ICollection Keys
		{
			get	{ return m_dictionary.Keys; }
		}

		/// <summary>
		/// Gets a value indicating whether the IDictionary has a fixed size.
		/// </summary>
		public virtual bool IsFixedSize
		{
			get	{ return false; }
		}
		#endregion

		#region ICollection Members
		/// <summary>
		/// Indicates whether access to the ICollection is synchronized (thread-safe).
		/// </summary>
		public virtual bool IsSynchronized
		{
			get	{ return false; }
		}

		/// <summary>
		/// Gets the number of objects in the collection.
		/// </summary>
		public virtual int Count
		{
			get { return m_dictionary.Count; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public virtual void CopyTo(Array array, int index)
		{
			if (m_dictionary != null)
			{
				m_dictionary.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Indicates whether access to the ICollection is synchronized (thread-safe).
		/// </summary>
		public virtual object SyncRoot
		{
			get	{ return this; }
		}
		#endregion

		#region IEnumerable Members
		/// <summary>
		/// Returns an enumerator that can iterate through a collection.
		/// </summary>
		/// <returns>An IEnumerator that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion
		
		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the collection.
		/// </summary>
		public virtual object Clone()
		{
			ReadOnlyDictionary clone = (ReadOnlyDictionary)this.MemberwiseClone();
	
			// clone contents of hashtable.
			Hashtable dictionary = new Hashtable();

			IDictionaryEnumerator enumerator = m_dictionary.GetEnumerator();

			while (enumerator.MoveNext())
			{
				dictionary.Add(Opc.Convert.Clone(enumerator.Key), Opc.Convert.Clone(enumerator.Value));
			}

			clone.m_dictionary = dictionary;

			// return clone.
			return clone;
		}
		#endregion

		#region Private Members
		private Hashtable m_dictionary = new Hashtable();
		private const string READ_ONLY_DICTIONARY = "Cannot change the contents of a read-only dictionary";
		#endregion
	}
	#endregion

	#region WriteableDictionary Class
	/// <summary>
	/// A read only dictionary class which can be used to expose arrays as properties of classes.
	/// </summary>
	[Serializable]
	public class WriteableDictionary : IDictionary, ISerializable
	{				
		#region Protected Interface
		/// <summary>
		/// Creates a collection that wraps the specified array instance.
		/// </summary>
		protected WriteableDictionary(IDictionary dictionary, System.Type keyType, System.Type valueType)
		{
			// set default key/value types.
			m_keyType   = (keyType == null)?typeof(object):keyType;
			m_valueType = (valueType == null)?typeof(object):valueType;

			// copy dictionary.
			Dictionary = dictionary;
		}

		/// <summary>
		/// The dictionary instance exposed by the collection.
		/// </summary>
		protected virtual IDictionary Dictionary
		{
			get { return m_dictionary;  }
			
			set 
			{ 
				// copy dictionary.
				if (value != null)
				{			
					// verify that current keys of the dictionary are the correct type.
					if (m_keyType != null)
					{
						foreach (object element in value.Keys)
						{
							ValidateKey(element, m_keyType);
						}
					}

					// verify that current values of the dictionary are the correct type.
					if (m_valueType != null)
					{
						foreach (object element in value.Values)
						{
							ValidateValue(element, m_valueType);
						}
					}

					m_dictionary = new Hashtable(value);
				}
				else
				{
					m_dictionary = new Hashtable();
				}
			}
		}

		/// <summary>
		/// The type of objects allowed as keys in the dictionary.
		/// </summary>
		protected System.Type KeyType
		{
			get { return m_keyType; }
			
			set 
			{
				// verify that current keys of the dictionary are the correct type.
				foreach (object element in m_dictionary.Keys)
				{
					ValidateKey(element, value);
				}

				m_keyType = value; 
			}
		}

		/// <summary>
		/// The type of objects allowed as values in the dictionary.
		/// </summary>
		protected System.Type ValueType
		{
			get { return m_valueType; }
			
			set 
			{
				// verify that current values of the dictionary are the correct type.
				foreach (object element in m_dictionary.Values)
				{
					ValidateValue(element, value);
				}

				m_valueType = value; 
			}
		}
				
		/// <summary>
		/// Throws an exception if the key is not valid for the dictionary.
		/// </summary>
		protected virtual void ValidateKey(object element, System.Type type)
		{
			if (element == null)
			{
				throw new ArgumentException(String.Format(INVALID_VALUE, element, "key"));
			}

			if (!type.IsInstanceOfType(element))
			{
				throw new ArgumentException(String.Format(INVALID_TYPE, element.GetType(), "key"));
			}
		}
				
		/// <summary>
		/// Throws an exception if the value is not valid for the dictionary.
		/// </summary>
		protected virtual void ValidateValue(object element, System.Type type)
		{
			if (element != null)
			{
				if (!type.IsInstanceOfType(element))
				{
					throw new ArgumentException(String.Format(INVALID_TYPE, element.GetType(), "value"));
				}
			}
		}

		/// <remarks/>
		protected const string INVALID_VALUE = "The {1} '{0}' cannot be added to the dictionary.";
		/// <remarks/>
		protected const string INVALID_TYPE  = "A {1} with type '{0}' cannot be added to the dictionary.";
		#endregion

		#region ISerializable Members
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{  
			internal const string COUNT       = "CT";
			internal const string KEY         = "KY";
			internal const string VALUE       = "VA";
			internal const string KEY_TYPE    = "KT";
			internal const string VALUE_VALUE = "VT";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected WriteableDictionary(SerializationInfo info, StreamingContext context)
		{	
			m_keyType   = (System.Type)info.GetValue(Names.KEY_TYPE, typeof(System.Type));
			m_valueType = (System.Type)info.GetValue(Names.VALUE_VALUE, typeof(System.Type));

			int count = (int)info.GetValue(Names.COUNT, typeof(int));

			m_dictionary = new Hashtable();

			for (int ii = 0; ii < count; ii++)
			{
				object key   = info.GetValue(Names.KEY + ii.ToString(), typeof(object));
				object value = info.GetValue(Names.VALUE + ii.ToString(), typeof(object));

				if (key != null)
				{
					m_dictionary[key] = value;
				}
			}
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{			
			info.AddValue(Names.KEY_TYPE, m_keyType);
			info.AddValue(Names.VALUE_VALUE, m_valueType);
			info.AddValue(Names.COUNT, m_dictionary.Count);

			int ii = 0;

			IDictionaryEnumerator enumerator = m_dictionary.GetEnumerator();

			while (enumerator.MoveNext())
			{
				info.AddValue(Names.KEY + ii.ToString(), enumerator.Key);
				info.AddValue(Names.VALUE + ii.ToString(), enumerator.Value);

				ii++;
			}		
		}
		#endregion

		#region IDictionary Members
		/// <summary>
		/// Gets a value indicating whether the IDictionary is read-only.
		/// </summary>
		public virtual bool IsReadOnly
		{
			get	{ return false; }
		}

		/// <summary>
		/// Returns an IDictionaryEnumerator for the IDictionary.
		/// </summary>
		public virtual IDictionaryEnumerator GetEnumerator()
		{
			return m_dictionary.GetEnumerator();
		}		

		/// <summary>
		/// Gets or sets the element with the specified key. 
		/// </summary>
		public virtual object this[object key]
		{
			get
			{
				return m_dictionary[key];
			}

			set
			{
				ValidateKey(key, m_keyType);
				ValidateValue(value, m_valueType);
				m_dictionary[key] = value;
			}
		}

		/// <summary>
		/// Removes the element with the specified key from the IDictionary.
		/// </summary>
		public virtual void Remove(object key)
		{
			m_dictionary.Remove(key);
		}

		/// <summary>
		/// Determines whether the IDictionary contains an element with the specified key.
		/// </summary>
		public virtual bool Contains(object key)
		{
			return m_dictionary.Contains(key);
		}

		/// <summary>
		/// Removes all elements from the IDictionary.
		/// </summary>
		public virtual void Clear()
		{
			m_dictionary.Clear();
		}

		/// <summary>
		/// Gets an ICollection containing the values in the IDictionary.
		/// </summary>
		public virtual ICollection Values
		{
			get	{ return m_dictionary.Values; }
		}

		/// <summary>
		/// Adds an element with the provided key and value to the IDictionary.
		/// </summary>
		public virtual void Add(object key, object value)
		{
			ValidateKey(key, m_keyType);
			ValidateValue(value, m_valueType);
			m_dictionary.Add(key, value);
		}

		/// <summary>
		/// Gets an ICollection containing the keys of the IDictionary.
		/// </summary>
		public virtual ICollection Keys
		{
			get	{ return m_dictionary.Keys; }
		}

		/// <summary>
		/// Gets a value indicating whether the IDictionary has a fixed size.
		/// </summary>
		public virtual bool IsFixedSize
		{
			get	{ return false; }
		}
		#endregion

		#region ICollection Members
		/// <summary>
		/// Indicates whether access to the ICollection is synchronized (thread-safe).
		/// </summary>
		public virtual bool IsSynchronized
		{
			get	{ return false; }
		}

		/// <summary>
		/// Gets the number of objects in the collection.
		/// </summary>
		public virtual int Count
		{
			get { return m_dictionary.Count; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public virtual void CopyTo(Array array, int index)
		{
			if (m_dictionary != null)
			{
				m_dictionary.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Indicates whether access to the ICollection is synchronized (thread-safe).
		/// </summary>
		public virtual object SyncRoot
		{
			get	{ return this; }
		}
		#endregion

		#region IEnumerable Members
		/// <summary>
		/// Returns an enumerator that can iterate through a collection.
		/// </summary>
		/// <returns>An IEnumerator that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		#endregion
		
		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the collection.
		/// </summary>
		public virtual object Clone()
		{
			WriteableDictionary clone = (WriteableDictionary)this.MemberwiseClone();
	
			// clone contents of hashtable.
			Hashtable dictionary = new Hashtable();

			IDictionaryEnumerator enumerator = m_dictionary.GetEnumerator();

			while (enumerator.MoveNext())
			{
				dictionary.Add(Opc.Convert.Clone(enumerator.Key), Opc.Convert.Clone(enumerator.Value));
			}

			clone.m_dictionary = dictionary;

			// return clone.
			return clone;
		}
		#endregion

		#region Private Members
		private Hashtable m_dictionary = new Hashtable();	
		private System.Type m_keyType = null;
		private System.Type m_valueType = null;
		#endregion
	}
	#endregion
}
