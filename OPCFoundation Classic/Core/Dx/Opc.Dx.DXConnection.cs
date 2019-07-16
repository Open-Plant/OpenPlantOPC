//============================================================================
// TITLE: Opc.Dx.DXConnection.cs
//
// CONTENTS:
// 
// Classes used to store information related to a DX Source Server
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
	/// Describes how an item in the server address space should be accessed. 
	/// </summary>
	[Serializable]
	public class DXConnection : ItemIdentifier
	{
		/// <summary>
		/// A unique name for the connection within the configuration.
		/// </summary>
		public string Name
		{
			get { return m_name;  }
			set { m_name = value; }
		}

		/// <summary>
		/// A unique name for the connection within the configuration.
		/// </summary>
		public BrowsePathCollection BrowsePaths
		{
			get { return m_browsePaths;  }
		}

		/// <summary>
		/// A more detailed description of the connection.
		/// </summary>
		public string Description
		{
			get { return m_description;  }
			set { m_description = value; }
		}

		/// <summary>
		/// A keyword that can be used to search for a connection.
		/// </summary>
		public string Keyword
		{
			get { return m_keyword;  }
			set { m_keyword = value; }
		}

		/// <summary>
		/// Whether data is acquired from source item on startup.
		/// </summary>
		public bool DefaultSourceItemConnected
		{
			get { return m_defaultSourceItemConnected;  }
			set { m_defaultSourceItemConnected = value; }
		}

		/// <summary>
		/// Whether the DefaultSourceItemConnected property is specified.
		/// </summary>
		public bool DefaultSourceItemConnectedSpecified
		{
			get { return m_defaultSourceItemConnectedSpecified;  }
			set { m_defaultSourceItemConnectedSpecified = value; }
		}

		/// <summary>
		/// Whether data is written to the target item on startup.
		/// </summary>
		public bool DefaultTargetItemConnected
		{
			get { return m_defaultTargetItemConnected;  }
			set { m_defaultTargetItemConnected = value; }
		}

		/// <summary>
		/// Whether the DefaultTargetItemConnected property is specified.
		/// </summary>
		public bool DefaultTargetItemConnectedSpecified
		{
			get { return m_defaultTargetItemConnectedSpecified;  }
			set { m_defaultTargetItemConnectedSpecified = value; }
		}

		/// <summary>
		/// Whether data the override value should be written to the target item.
		/// </summary>
		public bool DefaultOverridden
		{
			get { return m_defaultOverridden;  }
			set { m_defaultOverridden = value; }
		}

		/// <summary>
		/// Whether the DefaultOverridden property is specified.
		/// </summary>
		public bool DefaultOverriddenSpecified
		{
			get { return m_defaultOverriddenSpecified;  }
			set { m_defaultOverriddenSpecified = value; }
		}

		/// <summary>
		/// The value to use when writing an override value to the target item.
		/// </summary>
		public object DefaultOverrideValue
		{
			get { return m_defaultOverrideValue;  }
			set { m_defaultOverrideValue = value; }
		}

		/// <summary>
		/// Whether data the substitute value should be written to the target item.
		/// </summary>
		public bool EnableSubstituteValue
		{
			get { return m_enableSubstituteValue;  }
			set { m_enableSubstituteValue = value; }
		}

		/// <summary>
		/// Whether the EnableSubstituteValue property is specified.
		/// </summary>
		public bool EnableSubstituteValueSpecified
		{
			get { return m_enableSubstituteValueSpecified;  }
			set { m_enableSubstituteValueSpecified = value; }
		}	

		/// <summary>
		/// The value to use when writing an substitute value to the target item.
		/// </summary>
		public object SubstituteValue
		{
			get { return m_substituteValue;  }
			set { m_substituteValue = value; }
		}

		/// <summary>
		/// The item name for the target item.
		/// </summary>
		public string TargetItemName
		{
			get { return m_targetItemName;  }
			set { m_targetItemName = value; }
		}

		/// <summary>
		/// The item path for the target item.
		/// </summary>
		public string TargetItemPath
		{
			get { return m_targetItemPath;  }
			set { m_targetItemPath = value; }
		}

		/// <summary>
		/// The name of the source server.
		/// </summary>
		public string SourceServerName
		{
			get { return m_sourceServerName;  }
			set { m_sourceServerName = value; }
		}

		/// <summary>
		/// The item name for the source item.
		/// </summary>
		public string SourceItemName
		{
			get { return m_sourceItemName;  }
			set { m_sourceItemName = value; }
		}

		/// <summary>
		/// The item path for the source item.
		/// </summary>
		public string SourceItemPath
		{
			get { return m_sourceItemPath;  }
			set { m_sourceItemPath = value; }
		}

		/// <summary>
		/// The size of the queue to maintain for values received from the source.
		/// </summary>
		public int SourceItemQueueSize
		{
			get { return m_sourceItemQueueSize;  }
			set { m_sourceItemQueueSize = value; }
		}

		/// <summary>
		/// Whether the SourceItemQueueSize property is specified.
		/// </summary>
		public bool SourceItemQueueSizeSpecified
		{
			get { return m_sourceItemQueueSizeSpecified;  }
			set { m_sourceItemQueueSizeSpecified = value; }
		}

		/// <summary>
		/// The rate at which values should be acquired from the source.
		/// </summary>
		public int UpdateRate
		{
			get { return m_updateRate;  }
			set { m_updateRate = value; }
		}

		/// <summary>
		/// Whether the UpdateRate property is specified.
		/// </summary>
		public bool UpdateRateSpecified
		{
			get { return m_updateRateSpecified;  }
			set { m_updateRateSpecified = value; }
		}

		/// <summary>
		/// A deadband to use when acquiring data from the source.
		/// </summary>
		public float Deadband
		{
			get { return m_deadband;  }
			set { m_deadband = value; }
		}

		/// <summary>
		/// Whether the Deadband property is specified.
		/// </summary>
		public bool DeadbandSpecified
		{
			get { return m_deadbandSpecified;  }
			set { m_deadbandSpecified = value; }
		}

		/// <summary>
		/// Vendor specific information associated with a connection.
		/// </summary>
		public string VendorData
		{
			get { return m_vendorData;  }
			set { m_vendorData = value; }
		}

		#region Constructors
		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		public DXConnection() {}

		/// <summary>
		/// Initializes object with the specified ItemIdentifier object.
		/// </summary>
		public DXConnection(ItemIdentifier item) : base(item) {}

		/// <summary>
		/// Initializes object with the specified DXConnection object.
		/// </summary>
		public DXConnection(DXConnection connection) : base(connection)
		{
			if (connection != null)
			{
				BrowsePaths.AddRange(connection.BrowsePaths);

				Name                                = connection.Name;
				Description                         = connection.Description;
				Keyword                             = connection.Keyword;
				DefaultSourceItemConnected          = connection.DefaultSourceItemConnected;
				DefaultSourceItemConnectedSpecified = connection.DefaultSourceItemConnectedSpecified;
				DefaultTargetItemConnected          = connection.DefaultTargetItemConnected;
				DefaultTargetItemConnectedSpecified = connection.DefaultTargetItemConnectedSpecified;
				DefaultOverridden                   = connection.DefaultOverridden;
				DefaultOverriddenSpecified          = connection.DefaultOverriddenSpecified;
				DefaultOverrideValue                = connection.DefaultOverrideValue;
				EnableSubstituteValue               = connection.EnableSubstituteValue;
				EnableSubstituteValueSpecified      = connection.EnableSubstituteValueSpecified;
				SubstituteValue                     = connection.SubstituteValue;
				TargetItemName                      = connection.TargetItemName;
				TargetItemPath                      = connection.TargetItemPath;
				SourceServerName                    = connection.SourceServerName;
				SourceItemName                      = connection.SourceItemName;
				SourceItemPath                      = connection.SourceItemPath;
				SourceItemQueueSize                 = connection.SourceItemQueueSize;
				SourceItemQueueSizeSpecified        = connection.SourceItemQueueSizeSpecified;
				UpdateRate                          = connection.UpdateRate;
				UpdateRateSpecified                 = connection.UpdateRateSpecified;
				Deadband                            = connection.Deadband;
				DeadbandSpecified                   = connection.DeadbandSpecified;
				VendorData                          = connection.VendorData;
			}
		}
		#endregion
		
		#region Private Members
		private string m_name = null;
		private BrowsePathCollection m_browsePaths = new BrowsePathCollection();
		private string m_description = null;
		private string m_keyword = null;
		private bool m_defaultSourceItemConnected = false;
		private bool m_defaultSourceItemConnectedSpecified = false;
		private bool m_defaultTargetItemConnected = false;
		private bool m_defaultTargetItemConnectedSpecified = false;
		private bool m_defaultOverridden = false;
		private bool m_defaultOverriddenSpecified = false;
		private object m_defaultOverrideValue = null;
		private bool m_enableSubstituteValue = false;
		private bool m_enableSubstituteValueSpecified = false;
		private object m_substituteValue = null;
		private string m_targetItemName = null;
		private string m_targetItemPath = null;
		private string m_sourceServerName = null;
		private string m_sourceItemName = null;
		private string m_sourceItemPath = null;
		private int m_sourceItemQueueSize = 1;
		private bool m_sourceItemQueueSizeSpecified = false;
		private int m_updateRate = 0;
		private bool m_updateRateSpecified = false;
		private float m_deadband = 0;
		private bool m_deadbandSpecified = false;
		private string m_vendorData = null;
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public override object Clone()
		{
			return new DXConnection(this);
		}
		#endregion
	}

	/// <summary>
	/// A collection of browse paths.
	/// </summary>
	[Serializable]
	public class BrowsePathCollection : ArrayList
	{
		/// <summary>
		/// Gets the browse path at the specified index.
		/// </summary>
		public new string this[int index]
		{
			get { return (string)this[index]; }
			set { this[index] = value; }
		}	

		/// <summary>
		/// Converts the collection to an array of strings.
		/// </summary>
		public new string[] ToArray()
		{
			return (string[])ToArray(typeof(string));
		}

		/// <summary>
		/// Adds a new browse path to the collection.
		/// </summary>
		public int Add(string browsePath)
		{
			return base.Add(browsePath);
		}

		/// <summary>
		/// Inserts a new browse path to the collection at the specified index.
		/// </summary>
		public void Insert(int index, string browsePath)
		{
			if (browsePath == null) throw new ArgumentNullException("browsePath");

			base.Insert(index, browsePath);
		}

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		public BrowsePathCollection() {}

		/// <summary>
		/// Initializes object with the specified ICollection object.
		/// </summary>
		public BrowsePathCollection(ICollection browsePaths)
		{
			if (browsePaths != null)
			{
				foreach (string browsePath in browsePaths)
				{
					Add(browsePath);
				}
			}
		}
	}

	/// <summary>
	/// A collection of DX connections.
	/// </summary>
	[Serializable]
	public class DXConnectionCollection : ICollection, ICloneable, IList, ISerializable
	{
		/// <summary>
		/// Gets the source server at the specified index.
		/// </summary>
		public DXConnection this[int index]
		{
			get { return (DXConnection)m_connections[index]; }
		}	

		/// <summary>
		/// Returns the contents of the collection as an array.
		/// </summary>
		/// <returns>The set of connections in the collection.</returns>
		public DXConnection[] ToArray()
		{
			return (DXConnection[])m_connections.ToArray(typeof(DXConnection));
		}

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		internal DXConnectionCollection() {}

		/// <summary>
		/// Initializes object with the specified ResultCollection object.
		/// </summary>
		internal DXConnectionCollection(ICollection connections)
		{
			if (connections != null)
			{
				foreach (DXConnection sourceServer in connections)
				{
					m_connections.Add(sourceServer);
				}
			}
		}

		#region ISerializable Members
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string CONNECTIONS = "Connections";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected DXConnectionCollection(SerializationInfo info, StreamingContext context)
		{
			DXConnection[] connections = (DXConnection[])info.GetValue(Names.CONNECTIONS, typeof(DXConnection[]));
		
			if (connections != null)
			{
				foreach (DXConnection connection in connections)
				{
					m_connections.Add(connection);
				}
			}
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			DXConnection[] connections = null;

			if (m_connections.Count > 0)
			{
				connections = new DXConnection[m_connections.Count];

				for (int ii = 0; ii < connections.Length; ii++)
				{
					connections[ii] = (DXConnection)m_connections[ii];
				}
			}

			info.AddValue(Names.CONNECTIONS, connections);
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone()
		{
			DXConnectionCollection clone = (DXConnectionCollection)MemberwiseClone();

			clone.m_connections = new ArrayList();

			foreach (DXConnection item in m_connections)
			{
				clone.m_connections.Add(item.Clone());
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
			get { return (m_connections != null)?m_connections.Count:0; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_connections != null)
			{
				m_connections.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(DXConnection[] array, int index)
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
			return m_connections.GetEnumerator();
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
			get	{ return m_connections[index]; }
			set	{ Insert(index, value);    }
		}

		/// <summary>
		/// Removes the IList item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index)
		{
			if (index < 0 || index >= m_connections.Count)
			{
				throw new ArgumentOutOfRangeException("index");
			}

			Remove(m_connections[index]);
		}

		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, object value)
		{
			if (!typeof(DXConnection).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add DXConnection objects into the collection.");
			}

			m_connections.Insert(index, (DXConnection)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(object value)
		{
			if (!typeof(Opc.Dx.ItemIdentifier).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only delete Opc.Dx.ItemIdentifier obejcts from the collection.");
			}

			// find the server in the collection.
			foreach (Opc.Dx.ItemIdentifier itemID in m_connections)
			{
				// remove server from collection.
				if (itemID.Equals(value))
				{
					m_connections.Remove(itemID);
					break;
				}
			}
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(object value)
		{
			foreach (Opc.Dx.ItemIdentifier itemID in m_connections)
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
			m_connections.Clear();
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(object value)
		{
			return m_connections.IndexOf(value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(object value)
		{
			Insert(m_connections.Count, value);
			return m_connections.Count-1;
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
		public void Insert(int index, DXConnection value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(DXConnection value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(DXConnection value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(DXConnection value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(DXConnection value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Members
		private ArrayList m_connections = new ArrayList();
		#endregion
	}
}
