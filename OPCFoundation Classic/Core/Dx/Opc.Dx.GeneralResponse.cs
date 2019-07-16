//============================================================================
// TITLE: Opc.Dx.GeneralResponse.cs
//
// CONTENTS:
// 
// A class that stores the results of an update to the DX server configuration.
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
// 2004/05/17 RSA   Initial implementation.

using System;
using System.Text;
using System.Xml;
using System.Collections;

namespace Opc.Dx
{
	/// <summary>
	/// A collection of source servers.
	/// </summary>
	[Serializable]
	public class GeneralResponse : ICloneable, ICollection
	{
		/// <summary>
		/// The configuration version after all changes were applied.
		/// </summary>
		public string Version
		{
			get { return m_version;  }
			set { m_version = value; }
		}

		/// <summary>
		/// Creates an empty collection.
		/// </summary>
		public GeneralResponse() {}

		/// <summary>
		/// Initializes the object with any IdentifiedResults contained in the collection.
		/// </summary>
		public GeneralResponse(string version, ICollection results)
		{
			Version = version;
			Init(results);
		}

		/// <summary>
		/// Returns the IdentifiedResult at the specified index.
		/// </summary>
		public IdentifiedResult this[int index]
		{
			get { return m_results[index];  }
			set { m_results[index] = value; }
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
		private string m_version = null;
		private IdentifiedResult[] m_results = new IdentifiedResult[0];

		/// <summary>
		/// Initializes the object with any item ids contained in the collection.
		/// </summary>
		/// <param name="collection">A collection containing item ids.</param>
		private void Init(ICollection collection)
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
		#endregion
	}
}
