//============================================================================
// TITLE: Opc.Dx.SourceServer.cs
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
using System.Reflection;

namespace Opc.Dx
{
	/// <summary>
	/// Describes how an item in the server address space should be accessed. 
	/// </summary>
	[Serializable]
	public class SourceServer : ItemIdentifier
	{
		/// <summary>
		/// A unique name for the source server within the configuration.
		/// </summary>
		public string Name
		{
			get { return m_name;  }
			set { m_name = value; }
		}

		/// <summary>
		/// A more detailed description of the source server.
		/// </summary>
		public string Description
		{
			get { return m_description;  }
			set { m_description = value; }
		}

		/// <summary>
		/// The type of interface used to communicate with the source server.
		/// </summary>
		public string ServerType
		{
			get { return m_serverType;  }
			set { m_serverType = value; }
		}

		/// <summary>
		/// The network address of the source server specified with a URL syntax.
		/// </summary>
		public string ServerURL
		{
			get { return m_serverURL;  }
			set { m_serverURL = value; }
		}

		/// <summary>
		/// Whether the DX should be connected to the source server at startup.
		/// </summary>
		public bool DefaultConnected
		{
			get { return m_defaultConnected;  }
			set { m_defaultConnected = value; }
		}

		/// <summary>
		/// Whether a value for the 'default connected' attribute is specified.
		/// </summary>
		public bool DefaultConnectedSpecified
		{
			get { return m_defaultConnectedSpecified;  }
			set { m_defaultConnectedSpecified = value; }
		}

		#region Constructors
		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		public SourceServer() {}

		/// <summary>
		/// Initializes object with the specified ItemIdentifier object.
		/// </summary>
		public SourceServer(ItemIdentifier item) : base(item) {}

		/// <summary>
		/// Initializes object with the specified SourceServer object.
		/// </summary>
		public SourceServer(SourceServer server) : base(server)
		{
			if (server != null)
			{
				m_name                      = server.m_name;
				m_description               = server.m_description;
				m_serverType                = server.m_serverType;
				m_serverURL                 = server.m_serverURL;
				m_defaultConnected          = server.m_defaultConnected;
				m_defaultConnectedSpecified = server.m_defaultConnectedSpecified;
			}
		}
		#endregion
		
		#region Private Members
		private string m_name = null;
		private string m_description = null;
		private string m_serverType = null;
		private string m_serverURL = null;
		private bool m_defaultConnected = false;
		private bool m_defaultConnectedSpecified = false;
		#endregion
	}

	/// <summary>
	/// Defines string constants for the standard source server types supported by a DX server.
	/// </summary>
	public class ServerType
	{
		/// <remarks/>
		public const string COM_DA10  = "COM-DA1.0";
		/// <remarks/>
		public const string COM_DA204 = "COM-DA2.04";
		/// <remarks/>
		public const string COM_DA205 = "COM-DA2.05";
		/// <remarks/>
		public const string COM_DA30  = "COM-DA3.0";
		/// <remarks/>
		public const string XML_DA10  = "XML-DA1.0";

		/// <summary>
		/// Returns an array of all well-known property descriptions.
		/// </summary>
		public static string[] Enumerate()
		{
			ArrayList values = new ArrayList();

			FieldInfo[] fields = typeof(ServerType).GetFields(BindingFlags.Static | BindingFlags.Public);

			foreach (FieldInfo field in fields)
			{
				values.Add(field.GetValue(typeof(string)));
			}

			return (string[])values.ToArray(typeof(string));
		}
	}

	/// <summary>
	/// A collection of source servers.
	/// </summary>
	public class SourceServerCollection : ICollection, ICloneable //, IList
	{
		/// <summary>
		/// Gets the source server at the specified index.
		/// </summary>
		public SourceServer this[int index]
		{
			get { return (SourceServer)m_servers[index]; }
		}	

		/// <summary>
		/// Gets the source server with the specified name.
		/// </summary>
		public SourceServer this[string name]
		{
			get 
			{ 
				foreach (SourceServer server in m_servers)
				{
					if (server.Name == name)
					{
						return server;
					}
				}

				return null;
			}
		}	

		/// <summary>
		/// Initializes object with the default values.
		/// </summary>
		internal SourceServerCollection() {}

		/// <summary>
		/// Initializes object with the specified Collection object.
		/// </summary>
		internal void Initialize(ICollection sourceServers)
		{
			m_servers.Clear();

			if (sourceServers != null)
			{
				foreach (SourceServer sourceServer in sourceServers)
				{
					m_servers.Add(sourceServer);
				}
			}
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone()
		{
			SourceServerCollection clone = (SourceServerCollection)MemberwiseClone();

			clone.m_servers = new ArrayList();

			foreach (SourceServer item in m_servers)
			{
				clone.m_servers.Add(item.Clone());
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
			get { return (m_servers != null)?m_servers.Count:0; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_servers != null)
			{
				m_servers.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(SourceServer[] array, int index)
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
			return m_servers.GetEnumerator();
		}
		#endregion

		#region IList Members
		/*
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
			get	{ return m_servers[index]; }
			set	{ Insert(index, value);    }
		}

		/// <summary>
		/// Removes the IList item at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index)
		{
			if (index < 0 || index >= m_servers.Count)
			{
				throw new ArgumentOutOfRangeException("index");
			}

			Remove(m_servers[index]);
		}

		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, object value)
		{
			if (!typeof(SourceServer).IsInstanceOfType(value))
			{
				throw new ArgumentException("May only add SourceServer objects into the collection.");
			}

			SourceServer sourceServer = (SourceServer)value;

			m_servers.Insert(index, sourceServer);
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
			foreach (Opc.Dx.ItemIdentifier itemID in m_servers)
			{
				if (itemID.Equals(value))
				{
					// remove server from collection.
					m_servers.Remove(itemID);
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
			foreach (Opc.Dx.ItemIdentifier itemID in m_servers)
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
			m_servers.Clear();
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(object value)
		{
			return m_servers.IndexOf(value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(object value)
		{
			Insert(m_servers.Count, value);
			return m_servers.Count-1;
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
		public void Insert(int index, SourceServer value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(SourceServer value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(SourceServer value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(SourceServer value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(SourceServer value)
		{
			return Add((object)value);
		}
		*/
		#endregion

		#region Private Members
		private ArrayList m_servers = new ArrayList();
		#endregion
	}
}
