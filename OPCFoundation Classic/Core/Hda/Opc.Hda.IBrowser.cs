//============================================================================
// TITLE: Opc.Hda.IBrowser.cs
//
// CONTENTS:
// 
// An interface used to browse the address space of a Historical Data Access server.
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
// 2004/11/11 RSA   Added a base interfaces for BrowsePosition.

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace Opc.Hda
{
	/// <summary>
	/// Defines functionality that is common to all OPC Data Access servers.
	/// </summary>
	public interface IBrowser : IDisposable
	{
		//======================================================================
		// Filters

		/// <summary>
		/// Returns the set of attribute filters used by the browser. 
		/// </summary>
		BrowseFilterCollection Filters { get; }

		//======================================================================
		// Browse

		/// <summary>
		/// Browses the server's address space at the specified branch.
		/// </summary>
		/// <param name="itemID">The item id of the branch to search.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		BrowseElement[] Browse(ItemIdentifier itemID);
		
		/// <summary>
		/// Begins a browsing the server's address space at the specified branch.
		/// </summary>
		/// <param name="itemID">The item id of the branch to search.</param>
		/// <param name="maxElements">The maximum number of elements to return.</param>
		/// <param name="position">The position object used to continue a browse operation.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		BrowseElement[] Browse(ItemIdentifier itemID, int maxElements, out IBrowsePosition position);
		
		//======================================================================
		// BrowseNext

		/// <summary>
		/// Continues browsing the server's address space at the specified position.
		/// </summary>
		/// <param name="maxElements">The maximum number of elements to return.</param>
		/// <param name="position">The position object used to continue a browse operation.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		BrowseElement[] BrowseNext(int maxElements, ref IBrowsePosition position);
	}
	
	/// <summary>
	/// Stores the state of a browse operation.
	/// </summary>
	[Serializable]
	public class BrowsePosition : IBrowsePosition
	{
        #region IDisposable Members
        /// <summary>
        /// The finalizer.
        /// </summary>
        ~BrowsePosition()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases unmanaged resources held by the object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged resources held by the object.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                }

                // Free your own state (unmanaged objects).
                // Set large fields to null.
                m_disposed = true;
            }
        }

        private bool m_disposed = false;
        #endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a shallow copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return (BrowsePosition)MemberwiseClone();
		}
		#endregion
	}

	/// <summary>
	/// Contains the description of an element in the server's address space.
	/// </summary>
	public class BrowseElement : ItemIdentifier
	{
		/// <summary>
		/// The name of element within its branch.
		/// </summary>
		public string Name
		{
			get { return m_name;  } 
			set { m_name = value; }
		}

		/// <summary>
		/// Whether the element is an item with associated data in the archive.
		/// </summary>
		public bool IsItem
		{
			get { return m_isItem;  } 
			set { m_isItem = value; }
		}

		/// <summary>
		/// Whether the element has child elements.
		/// </summary>
		public bool HasChildren
		{
			get { return m_hasChildren;  } 
			set { m_hasChildren = value; }
		}

		/// <summary>
		/// The current values of any attributes associated with the item.
		/// </summary>
		public AttributeValueCollection Attributes
		{
			get { return m_attributes;  } 
			set { m_attributes = value; }
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deepcopy of the object.
		/// </summary>
		public override object Clone() 
		{
			BrowseElement element = (BrowseElement)MemberwiseClone();
			element.Attributes = (AttributeValueCollection)m_attributes.Clone();
			return element; 
		}
		#endregion

		#region Private Members
		private string m_name = null;
		private bool m_isItem = false;
		private bool m_hasChildren = false;
		private AttributeValueCollection m_attributes = new AttributeValueCollection();
		#endregion
	}

	/// <summary>
	/// The set of possible operators to use when applying an item attribute filter.
	/// </summary>
	public enum Operator
	{
		/// <summary>
		/// The attribute value is equal (or matches) to the filter.
		/// </summary>
		Equal = 1,
		
		/// <summary>
		/// The attribute value is less than the filter.
		/// </summary>
		Less,

		/// <summary>
		/// The attribute value is less than or equal to the filter.
		/// </summary>
		LessEqual,

		/// <summary>
		/// The attribute value is greater than the filter.
		/// </summary>
		Greater,

		/// <summary>
		/// The attribute value is greater than or equal to the filter.
		/// </summary>
		GreaterEqual,

		/// <summary>
		/// The attribute value is not equal (or does not match)to the filter.
		/// </summary>
		NotEqual
	}

	/// <summary>
	/// Defines a filter to apply to an item attribute when browsing.
	/// </summary>
	[Serializable]
	public class BrowseFilter : ICloneable
	{
		/// <summary>
		/// The attribute id to use when filtering.
		/// </summary>
		public int AttributeID
		{
			get { return m_attributeID;  } 
			set { m_attributeID = value; }
		}

		/// <summary>
		/// The operator to use when testing if the filter condition is met.
		/// </summary>
		public Operator Operator
		{
			get { return m_operator;  } 
			set { m_operator = value; }
		}

		/// <summary>
		/// The value of the filter. The '*' and '?' wildcard characters are permitted. 
		/// </summary>
		public object FilterValue
		{
			get { return m_filterValue;  } 
			set { m_filterValue = value; }
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deepcopy of the object.
		/// </summary>
		public virtual object Clone() 
		{
			BrowseFilter filter = (BrowseFilter)MemberwiseClone();
			filter.FilterValue = Opc.Convert.Clone(FilterValue);
			return filter; 
		}
		#endregion
        
		#region Private Members
		private int m_attributeID = 0;
		private Operator m_operator = Operator.Equal;
		private object m_filterValue = null;
		#endregion
	}

	/// <summary>
	/// A collection of attribute filters used when browsing the server address space.
	/// </summary>
	[Serializable]
	public class BrowseFilterCollection : Opc.ItemIdentifier, ICollection
	{
		/// <summary>
		/// Creates an empty collection.
		/// </summary>
		public BrowseFilterCollection()
		{
			// do nothing.
		}

		/// <summary>
		/// Initializes the object with any BrowseFilter contained in the collection.
		/// </summary>
		/// <param name="collection">A collection containing browse filters.</param>
		public BrowseFilterCollection(ICollection collection)
		{
			Init(collection);
		}

		/// <summary>
		/// Returns the browse filter at the specified index.
		/// </summary>
		public BrowseFilter this[int index]
		{
			get { return m_filters[index];  }
			set { m_filters[index] = value; }
		}

		/// <summary>
		/// Returns the browse filter for the specified attribute id.
		/// </summary>
		public BrowseFilter Find(int id)
		{
			foreach (BrowseFilter filter in m_filters)
			{
				if (filter.AttributeID == id)
				{
					return filter;
				}
			}

			return null;
		}

		/// <summary>
		/// Initializes the object with any attribute values contained in the collection.
		/// </summary>
		/// <param name="collection">A collection containing attribute values.</param>
		public void Init(ICollection collection)
		{
			Clear();

			if (collection != null)
			{
				ArrayList values = new ArrayList(collection.Count);

				foreach (object value in collection)
				{
					if (value.GetType() == typeof(BrowseFilter))
					{
						values.Add(Opc.Convert.Clone(value));
					}
				}

				m_filters = (BrowseFilter[])values.ToArray(typeof(BrowseFilter));
			}
		}

		/// <summary>
		/// Removes all attribute values in the collection.
		/// </summary>
		public void Clear()
		{
			m_filters = new BrowseFilter[0];
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public override object Clone() 
		{ 
			return new BrowseFilterCollection(this);
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
			get { return (m_filters != null)?m_filters.Length:0; }
		}

		/// <summary>
		/// Copies the objects in to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_filters != null)
			{
				m_filters.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(BrowseFilter[] array, int index)
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
			return m_filters.GetEnumerator();
		}
		#endregion
		
		#region Private Members
		private BrowseFilter[] m_filters = new BrowseFilter[0];
		#endregion
	}
}
