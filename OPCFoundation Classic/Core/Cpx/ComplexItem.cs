//============================================================================
// TITLE: ComplexItem.cs
//
// CONTENTS:
// 
// A class that contains complex data related properties for an item.
//
// (c) Copyright 2002-2003 The OPC Foundation
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
// 2002/11/16 RSA   Initial implementation.

using System;
using System.Text;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Opc;
using Opc.Da;

namespace Opc.Cpx
{	   
	/// <summary>
	/// A class that contains complex data related properties for an item.
	/// </summary>
	public class ComplexItem : ItemIdentifier
	{
		//======================================================================
		// Constants

		/// <summary>
		/// The reserved name for complex data branch in the server namespace.
		/// </summary>
		public const string CPX_BRANCH = "CPX";

		/// <summary>
		/// The reserved name for the data filters branch in the CPX namespace.
		/// </summary>
		public const string CPX_DATA_FILTERS = "DataFilters";

		/// <summary>
		/// The set of all complex data item properties.
		/// </summary>
		public static readonly PropertyID[] CPX_PROPERTIES = new PropertyID[]
		{
			Property.TYPE_SYSTEM_ID, 
			Property.DICTIONARY_ID, 
			Property.TYPE_ID, 
			Property.UNCONVERTED_ITEM_ID, 
			Property.UNFILTERED_ITEM_ID, 
			Property.DATA_FILTER_VALUE
		};
	
		//======================================================================
		// Properties

		/// <summary>
		/// The name of the item in the server address space.
		/// </summary>
		public string Name
		{
			get { return m_name; } 
		}

		/// <summary>
		/// The type system id for the complex item.
		/// </summary>
		public string TypeSystemID
		{
			get { return m_typeSystemID; } 
		}
		
		/// <summary>
		/// The dictionary id for the complex item.
		/// </summary>
		public string DictionaryID
		{
			get { return m_dictionaryID; } 
		}
	
		/// <summary>
		/// The type id for the complex item.
		/// </summary>
		public string TypeID
		{
			get { return m_typeID; } 
		}

	
		/// <summary>
		/// The id of the item containing the dictionary for the item.
		/// </summary>
		public ItemIdentifier DictionaryItemID
		{
			get { return m_dictionaryItemID; } 
		}

		/// <summary>
		/// The id of the item containing the type description for the item.
		/// </summary>
		public ItemIdentifier TypeItemID
		{
			get { return m_typeItemID; } 
		}

		/// <summary>
		/// The id of the unconverted version of the item. Only valid for items which apply type conversions to the item. 
		/// </summary>
		public ItemIdentifier UnconvertedItemID
		{
			get { return m_unconvertedItemID; } 
		}

		/// <summary>
		/// The id of the unfiltered version of the item. Only valid for items apply data filters to the item. 
		/// </summary>
		public ItemIdentifier UnfilteredItemID
		{
			get { return m_unfilteredItemID; } 
		}

		/// <summary>
		/// The item used to create new data filters for the complex data item (null is item does not support it). 
		/// </summary>
		public ItemIdentifier DataFilterItem
		{
			get { return m_filterItem;  } 
		}

		/// <summary>
		/// The current data filter value. Only valid for items apply data filters to the item.
		/// </summary>
		public string DataFilterValue
		{
			get { return m_filterValue;  } 
			set { m_filterValue = value; } 
		}

		//======================================================================
		// Initialization

		/// <summary>
		/// Initializes the object with the default values.
		/// </summary>
		public ComplexItem() {}

		/// <summary>
		/// Initializes the object from an item identifier.
		/// </summary>
		public ComplexItem(ItemIdentifier itemID)
		{	
			ItemPath = itemID.ItemPath;
			ItemName = itemID.ItemName;
		}

		//======================================================================
		// Public Methods

		/// <summary>
		/// Returns an appropriate string representation of the object.
		/// </summary>
		public override string ToString()
		{
			if (m_name != null || m_name.Length != 0)
			{
				return m_name;
			}

			return ItemName;
		}

		/// <summary>
		/// Returns the root complex data item for the object.
		/// </summary>
		public ComplexItem GetRootItem()
		{
			if (m_unconvertedItemID != null)
			{
				return ComplexTypeCache.GetComplexItem(m_unconvertedItemID);
			}

			if (m_unfilteredItemID != null)
			{
				return ComplexTypeCache.GetComplexItem(m_unfilteredItemID);
			}

			return this;
		}	
		
		/// <summary>
		/// Reads the current complex data item properties from the server.
		/// </summary>
		public void Update(Opc.Da.Server server)
		{
			// clear the existing state.
			Clear();

			// check if the item supports any of the complex data properties. 
			ItemPropertyCollection[] results = server.GetProperties(
				new ItemIdentifier[] { this },
				CPX_PROPERTIES,
				true);

			// unexpected return value.
			if (results == null || results.Length != 1)
			{
				throw new ApplicationException("Unexpected results returned from server.");
			}

			// update object.
			if (!Init((ItemProperty[])results[0].ToArray(typeof(ItemProperty))))
			{
				throw new ApplicationException("Not a valid complex item.");
			}		

			// check if data filters are suppported for the item.
			GetDataFilterItem(server);
		}

		/// <summary>
		/// Fetches the set of type conversions from the server.
		/// </summary>
		public ComplexItem[] GetTypeConversions(Opc.Da.Server server)
		{
			// only the root item can have type conversions.
			if (m_unconvertedItemID != null || m_unfilteredItemID != null)
			{
				return null;
			}

			BrowsePosition position = null;
			
			try
			{
				// look for the 'CPX' branch.
				BrowseFilters filters = new BrowseFilters();

				filters.ElementNameFilter    = CPX_BRANCH;
				filters.BrowseFilter         = browseFilter.branch;
				filters.ReturnAllProperties  = false;
				filters.PropertyIDs          = null;
				filters.ReturnPropertyValues = false;

				BrowseElement[] elements = server.Browse(this, filters, out position);

				// nothing found.
				if (elements == null || elements.Length == 0)
				{
					return null;
				}

				// release the browse position object.
				if (position != null)
				{	
					position.Dispose();
					position = null;
				}

				// browse for type conversions.
				ItemIdentifier itemID = new ItemIdentifier(elements[0].ItemPath, elements[0].ItemName);

				filters.ElementNameFilter    = null;
				filters.BrowseFilter         = browseFilter.item;
				filters.ReturnAllProperties  = false;
				filters.PropertyIDs          = CPX_PROPERTIES;
				filters.ReturnPropertyValues = true;

				elements = server.Browse(itemID, filters, out position);

				// nothing found.
				if (elements == null || elements.Length == 0)
				{
					return new ComplexItem[0];
				}

				// contruct an array of complex data items for each available conversion.
				ArrayList conversions = new ArrayList(elements.Length);

				foreach (BrowseElement element in elements)
				{
					if (element.Name != CPX_DATA_FILTERS)
					{						
						ComplexItem item = new ComplexItem();

						if (item.Init(element))
						{
							// check if data filters supported for type conversion.
							item.GetDataFilterItem(server);

							conversions.Add(item);
						}
					}
				}

				// return the set of available conversions.
				return (ComplexItem[])conversions.ToArray(typeof(ComplexItem));
			}
			finally
			{
				if (position != null)
				{	
					position.Dispose();
					position = null;
				}
			}
		}	

		/// <summary>
		/// Fetches the set of data filters from the server.
		/// </summary>
		public ComplexItem[] GetDataFilters(Opc.Da.Server server)
		{
			// not a valid operation for data filter items. 
			if (m_unfilteredItemID != null)
			{
				return null;
			}

			// data filters not supported by the item.
			if (m_filterItem == null)
			{
				return null;
			}

			BrowsePosition position = null;
			
			try
			{
				// browse any existing filter instances.
				BrowseFilters filters = new BrowseFilters();

				filters.ElementNameFilter    = null;
				filters.BrowseFilter         = browseFilter.item;
				filters.ReturnAllProperties  = false;
				filters.PropertyIDs          = CPX_PROPERTIES;
				filters.ReturnPropertyValues = true;

				BrowseElement[] elements = server.Browse(m_filterItem, filters, out position);

				// nothing found.
				if (elements == null || elements.Length == 0)
				{
					return new ComplexItem[0];
				}

				// contruct an array of complex data items for each available data filter.
				ArrayList dataFilters = new ArrayList(elements.Length);

				foreach (BrowseElement element in elements)
				{			
					ComplexItem item = new ComplexItem();

					if (item.Init(element))
					{
						dataFilters.Add(item);
					}
				}

				// return the set of available data filters.
				return (ComplexItem[])dataFilters.ToArray(typeof(ComplexItem));
			}
			finally
			{
				if (position != null)
				{	
					position.Dispose();
					position = null;
				}
			}
		}

		/// <summary>
		/// Creates a new data filter.
		/// </summary>
		public ComplexItem CreateDataFilter(Opc.Da.Server server, string filterName, string filterValue)
		{
			// not a valid operation for data filter items. 
			if (m_unfilteredItemID != null)
			{
				return null;
			}

			// data filters not supported by the item.
			if (m_filterItem == null)
			{
				return null;
			}

			BrowsePosition position = null;
			
			try
			{
				// write the desired filter to the server.
				ItemValue item = new ItemValue(m_filterItem);

				// create the filter parameters document.
				StringWriter  ostrm  = new StringWriter();
				XmlTextWriter writer = new XmlTextWriter(ostrm);

				writer.WriteStartElement("DataFilters");
				writer.WriteAttributeString("Name", filterName);
				writer.WriteString(filterValue);
				writer.WriteEndElement();

				writer.Close();

				// create the value to write.
				item.Value              = ostrm.ToString();
				item.Quality            = Quality.Bad;
				item.QualitySpecified   = false;
				item.Timestamp          = DateTime.MinValue;
				item.TimestampSpecified = false;

				// write the value.
				IdentifiedResult[] result = server.Write(new ItemValue[] { item });

				if (result == null || result.Length == 0)
				{
					throw new ApplicationException("Unexpected result from server.");
				}

				if (result[0].ResultID.Failed())
				{
					throw new ApplicationException("Could not create new data filter.");
				}

				// browse for new data filter item.
				BrowseFilters filters = new BrowseFilters(); 

				filters.ElementNameFilter    = filterName;
				filters.BrowseFilter         = browseFilter.item;
				filters.ReturnAllProperties  = false;
				filters.PropertyIDs          = CPX_PROPERTIES;
				filters.ReturnPropertyValues = true;

				BrowseElement[] elements = server.Browse(m_filterItem, filters, out position);

				// nothing found.
				if (elements == null || elements.Length == 0)
				{
					throw new ApplicationException("Could not browse to new data filter.");
				}

				ComplexItem filterItem = new ComplexItem();

				if (!filterItem.Init(elements[0]))
				{
					throw new ApplicationException("Could not initialize to new data filter.");
				}
				
				// return the new data filter.
				return filterItem;
			}
			finally
			{
				if (position != null)
				{	
					position.Dispose();
					position = null;
				}
			}
		}

		/// <summary>
		/// Updates a data filter.
		/// </summary>
		public void UpdateDataFilter(Opc.Da.Server server, string filterValue)
		{
			// not a valid operation for non data filter items. 
			if (m_unfilteredItemID == null)
			{
				throw new ApplicationException("Cannot update the data filter for this item.");
			}

			ItemValue item = new ItemValue(this);

			// create the value to write.
			item.Value              = filterValue;
			item.Quality            = Quality.Bad;
			item.QualitySpecified   = false;
			item.Timestamp          = DateTime.MinValue;
			item.TimestampSpecified = false;

			// write the value.
			IdentifiedResult[] result = server.Write(new ItemValue[] { item });

			if (result == null || result.Length == 0)
			{
				throw new ApplicationException("Unexpected result from server.");
			}

			if (result[0].ResultID.Failed())
			{
				throw new ApplicationException("Could not update data filter.");
			}

			// update locale copy of the filter value.
			m_filterValue = filterValue;
		}

		/// <summary>
		/// Fetches the type dictionary for the item.
		/// </summary>
		public string GetTypeDictionary(Opc.Da.Server server)
		{
			ItemPropertyCollection[] results = server.GetProperties(
				new ItemIdentifier[] { m_dictionaryItemID },
				new PropertyID[] { Property.DICTIONARY },
				true);

			if (results == null || results.Length == 0 || results[0].Count == 0)
			{
				return null;
			}

			ItemProperty property = results[0][0];

			if (!property.ResultID.Succeeded())
			{
				return null;
			}

			return (string)property.Value;
		}

		/// <summary>
		/// Fetches the type description for the item.
		/// </summary>
		public string GetTypeDescription(Opc.Da.Server server)
		{
			ItemPropertyCollection[] results = server.GetProperties(
				new ItemIdentifier[] { m_typeItemID },
				new PropertyID[] { Property.TYPE_DESCRIPTION },
				true);

			if (results == null || results.Length == 0 || results[0].Count == 0)
			{
				return null;
			}

			ItemProperty property = results[0][0];

			if (!property.ResultID.Succeeded())
			{
				return null;
			}

			return (string)property.Value;
		}

		/// <summary>
		/// Fetches the item id for the data filters items.
		/// </summary>
		public void GetDataFilterItem(Opc.Da.Server server)
		{
			m_filterItem = null;

			// not a valid operation for data filter items. 
			if (m_unfilteredItemID != null)
			{
				return;
			}

			BrowsePosition position = null;
			
			try
			{
				ItemIdentifier itemID = new ItemIdentifier(this);

				// browse any existing filter instances.
				BrowseFilters filters = new BrowseFilters();

				filters.ElementNameFilter    = CPX_DATA_FILTERS;
				filters.BrowseFilter         = browseFilter.all;
				filters.ReturnAllProperties  = false;
				filters.PropertyIDs          = null;
				filters.ReturnPropertyValues = false;

				BrowseElement[] elements = null;

				// browse for the 'CPX' branch first.
				if (m_unconvertedItemID == null)
				{
					filters.ElementNameFilter = CPX_BRANCH;

					elements = server.Browse(itemID, filters, out position);

					// nothing found.
					if (elements == null || elements.Length == 0)
					{
						return;
					}

					// release the position object.
					if (position != null)
					{	
						position.Dispose();
						position = null;
					}
					
					// update the item for the next browse operation.
					itemID = new ItemIdentifier(elements[0].ItemPath, elements[0].ItemName);

					filters.ElementNameFilter = CPX_DATA_FILTERS;
				}
				
				// browse for the 'DataFilters' branch.
				elements = server.Browse(itemID, filters, out position);

				// nothing found.
				if (elements == null || elements.Length == 0)
				{
					return;
				}

				m_filterItem = new ItemIdentifier(elements[0].ItemPath, elements[0].ItemName);
			}
			finally
			{
				if (position != null)
				{	
					position.Dispose();
					position = null;
				}
			}
		}

		#region Private Members
		/// <summary>
		/// Sets all object properties to their default values.
		/// </summary>
		private void Clear()
		{
			m_typeSystemID      = null;
			m_dictionaryID      = null;
			m_typeID            = null;
			m_dictionaryItemID  = null;
			m_typeItemID        = null;
			m_unconvertedItemID = null;
			m_unfilteredItemID  = null;
			m_filterItem        = null;
			m_filterValue       = null;
		}

		/// <summary>
		/// Initializes the object from a browse element.
		/// </summary>
		private bool Init(BrowseElement element)
		{	
			// update the item id.
			ItemPath = element.ItemPath;
			ItemName = element.ItemName;
			m_name   = element.Name;

			return Init(element.Properties);
		}

		/// <summary>
		/// Initializes the object from a list of properties.
		/// </summary>
		private bool Init(ItemProperty[] properties)
		{	
			// put the object into default state.
			Clear();

			// must have at least three properties defined.
			if (properties == null || properties.Length < 3)
			{
				return false;
			}

			foreach (ItemProperty property in properties)
			{
				// continue - ignore invalid properties.
				if (!property.ResultID.Succeeded())
				{
					continue;
				}

				// type system id.
				if (property.ID == Property.TYPE_SYSTEM_ID)
				{
					m_typeSystemID = (string)property.Value;
					continue;
				}

				// dictionary id
				if (property.ID == Property.DICTIONARY_ID)
				{
					m_dictionaryID     = (string)property.Value;
					m_dictionaryItemID = new ItemIdentifier(property.ItemPath, property.ItemName);
					continue;
				}

				// type id
				if (property.ID == Property.TYPE_ID)
				{
					m_typeID     = (string)property.Value;
					m_typeItemID = new ItemIdentifier(property.ItemPath, property.ItemName);
					continue;
				}
	
				// unconverted item id
				if (property.ID == Property.UNCONVERTED_ITEM_ID)
				{
					m_unconvertedItemID = new ItemIdentifier(ItemPath, (string)property.Value);
					continue;
				}

				// unfiltered item id
				if (property.ID == Property.UNFILTERED_ITEM_ID)
				{
					m_unfilteredItemID = new ItemIdentifier(ItemPath, (string)property.Value);
					continue;
				}

				// data filter value.
				if (property.ID == Property.DATA_FILTER_VALUE)
				{
					m_filterValue = (string)property.Value;
					continue;
				}
			}

			// validate object.
			if (m_typeSystemID == null || m_dictionaryID == null || m_typeID == null)
			{
				return false;
			}

			return true;
		}
		#endregion

		#region Private Members
		private string m_name = null;
		private string m_typeSystemID = null;
		private string m_dictionaryID = null;
		private string m_typeID = null;
		private ItemIdentifier m_dictionaryItemID = null;
		private ItemIdentifier m_typeItemID = null;
		private ItemIdentifier m_unconvertedItemID = null;
		private ItemIdentifier m_unfilteredItemID = null;
		private ItemIdentifier m_filterItem = null;
		private string m_filterValue = null;
		#endregion
	}

	/// <summary>
	/// A class that caches properties of complex data items.
	/// </summary>
	public class ComplexTypeCache
	{
		/// <summary>
		/// Initializes the complex type cache with defaults.
		/// </summary>
		public ComplexTypeCache() {}

		/// <summary>
		/// Get or sets the server to use for the cache.
		/// </summary>
		public static Opc.Da.Server Server
		{
			get 
			{
				lock (typeof(ComplexTypeCache)) 
				{ 
					return m_server;  
				}
			}
			
			set 
			{
				lock (typeof(ComplexTypeCache)) 
				{ 
					m_server = value; 
					m_items.Clear();
					m_dictionaries.Clear();
					m_descriptions.Clear();
				}
			}
		}

		/// <summary>
		/// Returns the complex item for the specified item id.
		/// </summary>
		public static ComplexItem GetComplexItem(ItemIdentifier itemID)
		{
			if (itemID == null) return null;

			lock (typeof(ComplexTypeCache))
			{
				ComplexItem item = new ComplexItem(itemID);

				try
				{
					item.Update(m_server);
				}
				catch
				{
					// item is not a valid complex data item.
					item = null;
				}

				m_items[itemID.Key] = item;
				return item;
			}
		}
 
		/// <summary>
		/// Returns the complex item for the specified item browse element.
		/// </summary>
		public static ComplexItem GetComplexItem(BrowseElement element)
		{
			if (element == null) return null;

			lock (typeof(ComplexTypeCache))
			{
				return GetComplexItem(new ItemIdentifier(element.ItemPath, element.ItemName));
			}
		}
				
		/// <summary>
		/// Fetches the type description for the item.
		/// </summary>
		public static string GetTypeDictionary(ItemIdentifier itemID)
		{
			if (itemID == null) return null;

			lock (typeof(ComplexTypeCache))
			{
				string dictionary = (string)m_dictionaries[itemID.Key];

				if (dictionary != null)
				{
					return dictionary;
				}

				ComplexItem item = GetComplexItem(itemID);

				if (item != null)
				{
					dictionary = item.GetTypeDictionary(m_server);
				}

				return dictionary;
			}
		}

		/// <summary>
		/// Fetches the type description for the item.
		/// </summary>
		public static string GetTypeDescription(ItemIdentifier itemID)
		{
			if (itemID == null) return null;

			lock (typeof(ComplexTypeCache))
			{
				string description = null;

				ComplexItem item = GetComplexItem(itemID);

				if (item != null)
				{
					description = (string)m_descriptions[item.TypeItemID.Key];

					if (description != null)
					{
						return description;
					}

					m_descriptions[item.TypeItemID.Key] = description = item.GetTypeDescription(m_server);
				}

				return description;
			}
		}
		
		#region Private Members
		/// <summary>
		/// The active server for the application.
		/// </summary>
		private static Opc.Da.Server m_server = null;

		/// <summary>
		/// A cache of item properties fetched from the active server.
		/// </summary>
		private static Hashtable m_items = new Hashtable();

		/// <summary>
		/// A cache of type dictionaries fetched from the active server.
		/// </summary>
		private static Hashtable m_dictionaries = new Hashtable();

		/// <summary>
		/// A cache of type descriptions fetched from the active server.
		/// </summary>
		private static Hashtable m_descriptions = new Hashtable();
		#endregion
	}
}
