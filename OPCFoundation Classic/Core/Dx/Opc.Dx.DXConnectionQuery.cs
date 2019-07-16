//============================================================================
// TITLE: Opc.Dx.DXConnectionQuery.cs
//
// CONTENTS:
// 
// Classes used to store information related to a DX Connection Query.
//
// (c) Copyright 2004 The OPC Foundation
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
// 2004/05/07 RSA   Initial implementation.

using System;
using System.Text;
using System.Xml;
using System.Collections;
using System.Runtime.Serialization;

namespace Opc.Dx
{	
	/// <summary>
	/// Stores parameters for a DX connection query,
	/// </summary>
	[Serializable]
	public class DXConnectionQuery
	{
		/// <summary>
		/// A unique name for the query within the configuration.
		/// </summary>
		public string Name
		{
			get { return m_name;  }
			set { m_name = value; }
		}

		/// <summary>
		/// The browse path where the search begins.
		/// </summary>
		public string BrowsePath
		{
			get { return m_browsePath;  }
			set { m_browsePath = value; }
		}

		/// <summary>
		/// Whether the folders under the browse path are searched as well.
		/// </summary>
		public bool Recursive
		{
			get { return m_recursive;  }
			set { m_recursive = value; }
		}

		/// <summary>
		/// The masks that define the query criteria.
		/// </summary>
		public DXConnectionCollection Masks
		{
			get { return m_masks; }
		}

		/// <summary>
		/// Returns all connections that meet the query criteria.
		/// </summary>
		/// <param name="server">The DX server to query.</param>
		/// <param name="errors">Any errors associated with individual query masks.</param>
		/// <returns>The set of DX connections matching the query criteria.</returns>
		public DXConnection[] Query(Opc.Dx.Server server, out ResultID[] errors)
		{
			if (server == null) throw new ArgumentNullException("server");

			DXConnection[] connections = server.QueryDXConnections(
				BrowsePath,
				Masks.ToArray(),
				Recursive,
				out errors);

			return connections;
		}

		/// <summary>
		/// Updates all connections that meet the query criteria.
		/// </summary>
		/// <param name="server">The DX server to update.</param>
		/// <param name="connectionDefinition">The changes to apply to each connection that meets the query criteria.</param>
		/// <param name="errors">Any errors associated with individual query masks.</param>
		/// <returns>The list of connections that met the criteria and were updated.</returns>
		public GeneralResponse Update(Opc.Dx.Server server, DXConnection connectionDefinition, out ResultID[] errors)
		{
			if (server == null) throw new ArgumentNullException("server");

			GeneralResponse response = server.UpdateDXConnections(
				BrowsePath,
				Masks.ToArray(),
				Recursive,
				connectionDefinition,
				out errors);

			return response;
		}

		/// <summary>
		/// Deletea all connections that meet the query criteria.
		/// </summary>
		/// <param name="server">The DX server to update.</param>
		/// <param name="errors">Any errors associated with individual query masks.</param>
		/// <returns>The list of connections that met the criteria and were deletes.</returns>
		public GeneralResponse Delete(Opc.Dx.Server server, out ResultID[] errors)
		{
			if (server == null) throw new ArgumentNullException("server");

			GeneralResponse response = server.DeleteDXConnections(
				BrowsePath,
				Masks.ToArray(),
				Recursive,
				out errors);

			return response;
		}

		/// <summary>
		/// Changes the default or runtime attributes for a set of connections. 
		/// </summary>
		/// <param name="server">The DX server to update.</param>
		/// <param name="configToStatus">Whether the default attributes are copied to or copied from the runtime attributes.</param>
		/// <param name="errors">Any errors associated with individual query masks.</param>
		/// <returns>The list of connections that met the criteria and were updated.</returns>
		public GeneralResponse CopyDefaultAttributes(Opc.Dx.Server server, bool configToStatus, out ResultID[] errors)
		{
			if (server == null) throw new ArgumentNullException("server");

			GeneralResponse response = server.CopyDXConnectionDefaultAttributes(
				configToStatus,
				BrowsePath,
				Masks.ToArray(),
				Recursive,
				out errors);

			return response;
		}

		#region Constructors
		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		public DXConnectionQuery() {}

		/// <summary>
		/// Initializes object with the specified DXConnection object.
		/// </summary>
		public DXConnectionQuery(DXConnectionQuery query)
		{
			if (query != null)
			{
				Name       = query.Name;
				BrowsePath = query.BrowsePath;
				Recursive  = query.Recursive;
				m_masks    = new DXConnectionCollection(query.Masks);
			}
		}
		#endregion
		
		#region Private Members
		private string m_name = null;
		private string m_browsePath = null;
		private DXConnectionCollection m_masks = new DXConnectionCollection();
		private bool m_recursive = false;
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone()
		{
			return new DXConnectionQuery(this);
		}
		#endregion
	}

	/// <summary>
	/// A collection of DX Connection queries..
	/// </summary>
	public class DXConnectionQueryCollection : ICollection, ICloneable, IList
	{
		/// <summary>
		/// Gets the source server at the specified index.
		/// </summary>
		public DXConnectionQuery this[int index]
		{
			get { return (DXConnectionQuery)m_queries[index]; }
		}	

		/// <summary>
		/// Gets the source server with the specified name.
		/// </summary>
		public DXConnectionQuery this[string name]
		{
			get 
			{ 
				foreach (DXConnectionQuery query in m_queries)
				{
					if (query.Name == name)
					{
						return query;
					}
				}

				return null;
			}
		}	

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		internal DXConnectionQueryCollection() {}

		/// <summary>
		/// Initializes object with the specified Collection object.
		/// </summary>
		internal void Initialize(ICollection queries)
		{
			m_queries.Clear();

			if (queries != null)
			{
				foreach (DXConnectionQuery query in queries)
				{
					m_queries.Add(query);
				}
			}
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone()
		{
			DXConnectionQueryCollection clone = (DXConnectionQueryCollection)MemberwiseClone();

			clone.m_queries = new ArrayList();

			foreach (DXConnectionQuery item in m_queries)
			{
				clone.m_queries.Add(item.Clone());
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
			get { return (m_queries != null)?m_queries.Count:0; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_queries != null)
			{
				m_queries.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(DXConnectionQuery[] array, int index)
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
			return m_queries.GetEnumerator();
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
			get	{ return m_queries[index]; }
			set	{ Insert(index, value);    }
		}

		/// <summary>
		/// Removes the IList item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index)
		{
			if (index < 0 || index >= m_queries.Count)
			{
				throw new ArgumentOutOfRangeException("index");
			}

			Remove(m_queries[index]);
		}

		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, object value)
		{
			if (!typeof(DXConnectionQuery).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add DXConnectionQuery objects into the collection.");
			}

			m_queries.Insert(index, value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(object value)
		{
			if (!typeof(DXConnectionQuery).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only delete DXConnectionQuery obejcts from the collection.");
			}

			m_queries.Remove(value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(object value)
		{
			foreach (Opc.Dx.ItemIdentifier itemID in m_queries)
			{
				if (itemID.Equals(value))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Removes all items from the IList.
		/// </summary>
		public void Clear()
		{
			m_queries.Clear();
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(object value)
		{
			return m_queries.IndexOf(value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(object value)
		{
			Insert(m_queries.Count, value);
			return m_queries.Count-1;
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
		public void Insert(int index, DXConnectionQuery value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(DXConnectionQuery value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(DXConnectionQuery value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(DXConnectionQuery value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(DXConnectionQuery value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Members
		private ArrayList m_queries = new ArrayList();
		#endregion
	}
}
