//============================================================================
// TITLE: Opc.ItemIdentifier.cs
//
// CONTENTS:
// 
// A class for uniquely identifiable items.
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
// 2003/09/24 RSA   Defined an equality operator for ItemIdentifier.
// 2003/11/27 RSA   Move to the Opc namespace from Opc.Da.

using System;
using System.Collections;
using System.Text;
using System.Xml;

namespace Opc
{
	/// <summary>
	/// A unique item identifier.
	/// </summary>
	[Serializable]
	public class ItemIdentifier : ICloneable
	{
		/// <summary>
		/// The primary identifier for an item within the server namespace.
		/// </summary>
		public string ItemName
		{
			get { return m_itemName;  }
			set { m_itemName = value; }
		}
		/// <summary>
		/// An secondary identifier for an item within the server namespace.
		/// </summary>
		public string ItemPath
		{
			get { return m_itemPath;  }
			set { m_itemPath = value; }
		}

		/// <summary>
		/// A unique item identifier assigned by the client.
		/// </summary>
		public object ClientHandle
		{
			get { return m_clientHandle;  }
			set { m_clientHandle = value; }
		}

		/// <summary>
		/// A unique item identifier assigned by the server.
		/// </summary>
		public object ServerHandle
		{
			get { return m_serverHandle;  }
			set { m_serverHandle = value; }
		}

		/// <summary>
		/// Create a string that can be used as index in a hash table for the item.
		/// </summary>
		public string Key
		{ 
			get 
			{
				return new StringBuilder(64)
					.Append((ItemName == null)?"null":ItemName)
					.Append("\r\n")
					.Append((ItemPath == null)?"null":ItemPath)
					.ToString();
			}
		}

		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		public ItemIdentifier() {}

		/// <summary>
		/// Initializes the object with the specified item name.
		/// </summary>
		public ItemIdentifier(string itemName)
		{
			ItemPath = null;
			ItemName = itemName;
		}

		/// <summary>
		/// Initializes the object with the specified item path and item name.
		/// </summary>
		public ItemIdentifier(string itemPath, string itemName)
		{
			ItemPath = itemPath;
			ItemName = itemName;
		}
		
		/// <summary>
		/// Initializes the object with the specified item identifier.
		/// </summary>
		public ItemIdentifier(ItemIdentifier itemID)
		{
			if (itemID != null)
			{
				ItemPath     = itemID.ItemPath;
				ItemName     = itemID.ItemName;
				ClientHandle = itemID.ClientHandle;
				ServerHandle = itemID.ServerHandle;
			}
		}
			
		#region ICloneable Members
		/// <summary>
		/// Creates a shallow copy of the object.
		/// </summary>
		public virtual object Clone() { return MemberwiseClone(); }
		#endregion

		#region Private Members
		private string m_itemName = null;
		private string m_itemPath = null;
		private object m_clientHandle = null;
		private object m_serverHandle = null;
		#endregion
	}

	/// <summary>
	/// A interface used to access result information associated with a single item/value.
	/// </summary>
	public interface IResult
	{
		/// <summary>
		/// The error id for the result of an operation on an item.
		/// </summary>
		 ResultID ResultID { get; set; }
		
		/// <summary>
		/// Vendor specific diagnostic information (not the localized error text).
		/// </summary>
		string DiagnosticInfo { get; set; }
	}

	/// <summary>
	/// A result code associated with a unique item identifier.
	/// </summary>
	[Serializable]
	public class IdentifiedResult : ItemIdentifier, IResult
	{	
		/// <summary>
		/// Initialize object with default values.
		/// </summary>
		public IdentifiedResult() {}

		/// <summary>
		/// Initialize object with the specified ItemIdentifier object.
		/// </summary>
		public IdentifiedResult(ItemIdentifier item) : base(item)
		{
		}

		/// <summary>
		/// Initialize object with the specified IdentifiedResult object.
		/// </summary>
		public IdentifiedResult(IdentifiedResult item) : base(item)
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
		public IdentifiedResult(string itemName, ResultID resultID) : base(itemName)
		{
			ResultID = resultID;
		}

		/// <summary>
		/// Initialize object with the specified item name, result code and diagnostic info.
		/// </summary>
		public IdentifiedResult(string itemName, ResultID resultID, string diagnosticInfo) : base(itemName)
		{
			ResultID       = resultID;
			DiagnosticInfo = diagnosticInfo;
		}
		
		/// <summary>
		/// Initialize object with the specified ItemIdentifier and result code.
		/// </summary>
		public IdentifiedResult(ItemIdentifier item, ResultID resultID) : base(item)
		{
			ResultID = resultID;
		}

		/// <summary>
		/// Initialize object with the specified ItemIdentifier, result code and diagnostic info.
		/// </summary>
		public IdentifiedResult(ItemIdentifier item, ResultID resultID, string diagnosticInfo) : base(item)
		{
			ResultID       = resultID;
			DiagnosticInfo = diagnosticInfo;
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
	/// A collection of item identifiers.
	/// </summary>
	[Serializable]
	public class ItemIdentifierCollection : ICloneable, ICollection
	{
		/// <summary>
		/// Creates an empty collection.
		/// </summary>
		public ItemIdentifierCollection()
		{
			// do nothing.
		}

		/// <summary>
		/// Initializes the object with any ItemIdentifiers contained in the collection.
		/// </summary>
		/// <param name="collection">A collection containing item ids.</param>
		public ItemIdentifierCollection(ICollection collection)
		{
			Init(collection);
		}

		/// <summary>
		/// Returns the itemID at the specified index.
		/// </summary>
		public ItemIdentifier this[int index]
		{
			get { return m_itemIDs[index];  }
			set { m_itemIDs[index] = value; }
		}

		/// <summary>
		/// Initializes the object with any item ids contained in the collection.
		/// </summary>
		/// <param name="collection">A collection containing item ids.</param>
		public void Init(ICollection collection)
		{
			Clear();

			if (collection != null)
			{
				ArrayList itemIDs = new ArrayList(collection.Count);

				foreach (object value in collection)
				{
					if (typeof(ItemIdentifier).IsInstanceOfType(value))
					{
						itemIDs.Add(((ItemIdentifier)value).Clone());
					}
				}

				m_itemIDs = (ItemIdentifier[])itemIDs.ToArray(typeof(ItemIdentifier));
			}
		}

		/// <summary>
		/// Removes all itemIDs in the collection.
		/// </summary>
		public void Clear()
		{
			m_itemIDs = new ItemIdentifier[0];
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return new ItemIdentifierCollection(this);
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
			get { return (m_itemIDs != null)?m_itemIDs.Length:0; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_itemIDs != null)
			{
				m_itemIDs.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(ItemIdentifier[] array, int index)
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
			return m_itemIDs.GetEnumerator();
		}
		#endregion

		#region Private Members
		private ItemIdentifier[] m_itemIDs = new ItemIdentifier[0];
		#endregion
	}

	/// <summary>
	/// A collection of identified results.
	/// </summary>
	[Serializable]
	public class IdentifiedResultCollection : ICloneable, ICollection
	{
		/// <summary>
		/// Returns the IdentifiedResult at the specified index.
		/// </summary>
		public IdentifiedResult this[int index]
		{
			get { return m_results[index];  }
			set { m_results[index] = value; }
		}

		/// <summary>
		/// Creates an empty collection.
		/// </summary>
		public IdentifiedResultCollection()
		{
			// do nothing.
		}

		/// <summary>
		/// Initializes the object with any IdentifiedResults contained in the collection.
		/// </summary>
		/// <param name="collection">A collection containing item ids.</param>
		public IdentifiedResultCollection(ICollection collection)
		{
			Init(collection);
		}

		/// <summary>
		/// Initializes the object with any item ids contained in the collection.
		/// </summary>
		/// <param name="collection">A collection containing item ids.</param>
		public void Init(ICollection collection)
		{
			Clear();

			if (collection != null)
			{
				ArrayList itemIDs = new ArrayList(collection.Count);

				foreach (object value in collection)
				{
					if (typeof(IdentifiedResult).IsInstanceOfType(value))
					{
						itemIDs.Add(((IdentifiedResult)value).Clone());
					}
				}

				m_results = (IdentifiedResult[])itemIDs.ToArray(typeof(IdentifiedResult));
			}
		}

		/// <summary>
		/// Removes all itemIDs in the collection.
		/// </summary>
		public void Clear()
		{
			m_results = new IdentifiedResult[0];
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return new IdentifiedResultCollection(this);
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
			get { return (m_results != null)?m_results.Length:0; }
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
		public void CopyTo(IdentifiedResult[] array, int index)
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

		#region Private Members
		private IdentifiedResult[] m_results = new IdentifiedResult[0];
		#endregion
	}
}
