//============================================================================
// TITLE: Opc.Da.Browse.cs
//
// CONTENTS:
// 
// Contains classes used to browse the address space of an OPC server.
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
// 2004/11/11 RSA   Added a base interfaces for BrowsePosition.

using System;
using System.Xml;
using System.Collections;

namespace Opc.Da
{
	/// <summary>
	/// Stores the state of a browse operation.
	/// </summary>
	[Serializable]
	public class BrowsePosition : IBrowsePosition
	{
		/// <summary>
		/// The item identifier of the branch being browsed.
		/// </summary>
		public ItemIdentifier ItemID 
		{ 
			get{ return m_itemID; }
		}

		/// <summary>
		/// The filters applied during the browse operation.
		/// </summary>
		public BrowseFilters Filters 
		{
			get { return (BrowseFilters)m_filters.Clone(); }
		}

		/// <summary>
		/// The maximum number of elements that may be returned in a single browse.
		/// </summary>
		public int MaxElementsReturned 
		{
			get { return m_filters.MaxElementsReturned;  }
			set { m_filters.MaxElementsReturned = value; }
		}

		#region Constructors
		/// <summary>
		/// Saves the parameters for an incomplete browse information.
		/// </summary>
		public BrowsePosition(ItemIdentifier itemID, BrowseFilters filters)
		{
			if (filters == null) throw new ArgumentNullException("filters");

			m_itemID  = (itemID != null)?(ItemIdentifier)itemID.Clone():null;
			m_filters = (BrowseFilters)filters.Clone();
		}
		#endregion
        
        #region IDisposable Members
        /// <summary>
        /// The finalizer.
        /// </summary>
        ~BrowsePosition()
        {
            Dispose (false);
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
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return (BrowsePosition)MemberwiseClone();
		}
		#endregion

		#region Private Members
		private BrowseFilters m_filters = null;
		private ItemIdentifier m_itemID = null;
		#endregion
	}

	/// <summary>
	/// Contains a description of a single item property.
	/// </summary>
	[Serializable]
	public class ItemProperty : ICloneable, IResult	
	{
		/// <summary>
		/// The property identifier.
		/// </summary>
		public PropertyID ID
		{
			get { return m_id;  } 
			set { m_id = value; } 
		}

		/// <summary>
		/// A short description of the property.
		/// </summary>
		public string Description
		{
			get { return m_description;  } 
			set { m_description = value; } 
		}

		/// <summary>
		/// The data type of the property.
		/// </summary>
		public System.Type DataType
		{
			get { return m_datatype;  } 
			set { m_datatype = value; } 
		}

		/// <summary>
		/// The value of the property.
		/// </summary>
		public object Value
		{
			get { return m_value;  } 
			set { m_value = value; } 
		}

		/// <summary>
		/// The primary identifier for the property if it is directly accessible as an item.
		/// </summary>
		public string ItemName
		{
			get { return m_itemName;  } 
			set { m_itemName = value; } 
		}

		/// <summary>
		/// The secondary identifier for the property if it is directly accessible as an item.
		/// </summary>
		public string ItemPath
		{
			get { return m_itemPath;  } 
			set { m_itemPath = value; } 
		}

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

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			ItemProperty clone = (ItemProperty)MemberwiseClone();
			clone.Value = Opc.Convert.Clone(Value);
			return clone;
		}
		#endregion

		#region Private Members
		private PropertyID m_id;
		private string m_description = null;
		private System.Type m_datatype = null;
		private object m_value = null; 
		private string m_itemName = null;
		private string m_itemPath = null; 
		private ResultID m_resultID = ResultID.S_OK; 
		private string m_diagnosticInfo = null;
		#endregion
	}

	/// <summary>
	/// Contains a description of an element in the server address space.
	/// </summary>
	[Serializable]
	public class BrowseElement : ICloneable
	{
		/// <summary>
		/// A descriptive name for element that is unique within a branch.
		/// </summary>
		public string Name
		{
			get { return m_name;  } 
			set { m_name = value; } 
		}

		/// <summary>
		/// The primary identifier for the element within the server namespace.
		/// </summary>
		public string ItemName
		{
			get { return m_itemName;  } 
			set { m_itemName = value; } 
		}

		/// <summary>
		/// An secondary identifier for the element within the server namespace.
		/// </summary>
		public string ItemPath
		{
			get { return m_itemPath;  } 
			set { m_itemPath = value; } 
		}

		/// <summary>
		/// Whether the element refers to an item with data that can be accessed.
		/// </summary>
		public bool IsItem
		{
			get { return m_isItem;  } 
			set { m_isItem = value; } 
		}

		/// <summary>
		/// Whether the element has children.
		/// </summary>
		public bool HasChildren
		{
			get { return m_hasChildren;  } 
			set { m_hasChildren = value; } 
		}

		/// <summary>
		/// The set of properties for the element.
		/// </summary>
		public ItemProperty[] Properties
		{
			get { return m_properties;  } 
			set { m_properties = value; } 
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			BrowseElement clone = (BrowseElement)MemberwiseClone();
			clone.m_properties = (ItemProperty[])Opc.Convert.Clone(m_properties);
			return clone;
		}
		#endregion

		#region Private Members
		private string m_name = null;
		private string m_itemName = null;
		private string m_itemPath = null; 
		private bool m_isItem = false;
		private bool m_hasChildren = false; 
		private ItemProperty[] m_properties = new ItemProperty[0];
		#endregion
	};

	/// <summary>
	/// The type of browse elements to return during a browse.
	/// </summary>
	public enum browseFilter 
	{       
		/// <summary>
		/// Return all types of browse elements.
		/// </summary>
		all,
        
		/// <summary>
		/// Return only elements that contain other elements.
		/// </summary>
		branch,
        
		/// <summary>
		/// Return only elements that represent items.
		/// </summary>
		item
	}

	/// <summary>
	/// Defines a set of filters to apply when browsing.
	/// </summary>
	[Serializable]
	public class BrowseFilters : ICloneable
	{
		/// <summary>
		/// The maximum number of elements to return. Zero means no limit.
		/// </summary>
		public int MaxElementsReturned
		{
			get { return m_maxElementsReturned;  } 
			set { m_maxElementsReturned = value; } 
		} 
    
		/// <summary>
		/// The type of element to return.
		/// </summary>
		public browseFilter BrowseFilter
		{
			get { return m_browseFilter;  } 
			set { m_browseFilter = value; } 
		}
 
		/// <summary>
		/// An expression used to match the name of the element.
		/// </summary>
		public string ElementNameFilter
		{
			get { return m_elementNameFilter;  } 
			set { m_elementNameFilter = value; } 
		}

		/// <summary>
		/// A filter which has semantics that defined by the server.
		/// </summary>
		public string VendorFilter
		{
			get { return m_vendorFilter;  } 
			set { m_vendorFilter = value; } 
		}

		/// <summary>
		/// Whether all supported properties to return with each element.
		/// </summary>
		public bool ReturnAllProperties
		{
			get { return m_returnAllProperties;  } 
			set { m_returnAllProperties = value; } 
		}

		/// <summary>
		/// A list of names of the properties to return with each element.
		/// </summary>
		public PropertyID[] PropertyIDs
		{
			get { return m_propertyIDs;  } 
			set { m_propertyIDs = value; } 
		}

		/// <summary>
		/// Whether property values should be returned with the properties.
		/// </summary>
		public bool ReturnPropertyValues
		{
			get { return m_returnPropertyValues;  } 
			set { m_returnPropertyValues = value; } 
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			BrowseFilters clone = (BrowseFilters)MemberwiseClone();
			clone.PropertyIDs = (PropertyID[])((PropertyIDs != null)?PropertyIDs.Clone():null);
			return clone;
		}
		#endregion

		#region Private Members
		private int m_maxElementsReturned = 0; 
		private browseFilter m_browseFilter = browseFilter.all; 
		private string m_elementNameFilter = null; 
		private string m_vendorFilter = null; 
		private bool m_returnAllProperties = false; 
		private PropertyID[] m_propertyIDs = null;
		private bool m_returnPropertyValues = false; 
		#endregion
	}

	/// <summary>
	/// A list of properties for a single item.
	/// </summary>
	[Serializable]
	public class ItemPropertyCollection : ArrayList, IResult
	{
		/// <summary>
		/// The primary identifier for the item within the server namespace.
		/// </summary>
		public string ItemName
		{
			get { return m_itemName;  } 
			set { m_itemName = value; } 
		}

		/// <summary>
		/// An secondary identifier for the item within the server namespace.
		/// </summary>
		public string ItemPath
		{
			get { return m_itemPath;  } 
			set { m_itemPath = value; } 
		}

		/// <summary>
		/// Accesses the items at the specified index.
		/// </summary>
		public new ItemProperty this[int index]
		{
			get { return (ItemProperty)base[index]; }
			set { base[index] = value; }
		}

		/// <summary>
		/// Initializes the object with its default values.
		/// </summary>
		public ItemPropertyCollection() 
		{
		}

		/// <summary>
		/// Initializes the object with the specified item identifier.
		/// </summary>
		public ItemPropertyCollection(ItemIdentifier itemID) 
		{
			if (itemID != null)
			{
				m_itemName = itemID.ItemName;
				m_itemPath = itemID.ItemPath;
			}
		}

		/// <summary>
		/// Initializes the object with the specified item identifier and result id.
		/// </summary>
		public ItemPropertyCollection(ItemIdentifier itemID, ResultID resultID) 
		{
			if (itemID != null)
			{
				m_itemName = itemID.ItemName;
				m_itemPath = itemID.ItemPath;
			}

			ResultID = resultID;
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
		
		#region ICollection Members
		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(ItemProperty[] array, int index)
		{
			CopyTo((Array)array, index);
		}
		#endregion

		#region IList Members
		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, ItemProperty value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(ItemProperty value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(ItemProperty value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(ItemProperty value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(ItemProperty value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Members
		private string m_itemName = null; 
		private string m_itemPath = null; 
		private ResultID m_resultID = ResultID.S_OK; 
		private string m_diagnosticInfo = null;
		#endregion
	}
}
