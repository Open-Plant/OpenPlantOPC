//============================================================================
// TITLE: Opc.Hda.Aggregate.cs
//
// CONTENTS:
// 
// Classes used to store information related to item aggregates.
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

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using Opc;

namespace Opc.Hda
{	
	/// <summary>
	/// The description of an item aggregate supported by the server.
	/// </summary>
	[Serializable]
	public class Aggregate : ICloneable
	{
		/// <summary>
		/// A unique identifier for the aggregate.
		/// </summary>
		public int ID
		{
			get { return m_id;  } 
			set { m_id = value; } 
		}

		/// <summary>
		/// The unique name for the aggregate.
		/// </summary>
		public string Name
		{
			get { return m_name;  } 
			set { m_name = value; } 
		}

		/// <summary>
		/// A short description of the aggregate.
		/// </summary>
		public string Description
		{
			get { return m_description;  } 
			set { m_description = value; } 
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Name;
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a shallow copy of the object.
		/// </summary>
		public virtual object Clone() { return MemberwiseClone(); }
		#endregion
		
		#region Private Members
		private int         m_id          = 0;
		private string      m_name        = null;
		private string      m_description = null;
		#endregion
	}
	
	/// <summary>
	/// The description of an item aggregate supported by the server.
	/// </summary>
	[Serializable]
	public class AggregateCollection : ICloneable, ICollection
	{
		/// <summary>
		/// Creates an empty collection.
		/// </summary>
		public AggregateCollection()
		{
			// do nothing.
		}

		/// <summary>
		/// Initializes the object with any Aggregates contained in the collection.
		/// </summary>
		/// <param name="collection">A collection containing aggregate descriptions.</param>
		public AggregateCollection(ICollection collection)
		{
			Init(collection);
		}

		/// <summary>
		/// Returns the aggregate at the specified index.
		/// </summary>
		public Aggregate this[int index]
		{
			get { return m_aggregates[index];  }
			set { m_aggregates[index] = value; }
		}

		/// <summary>
		/// Returns the first aggregate with the specified id.
		/// </summary>
		public Aggregate Find(int id)
		{
			foreach (Aggregate aggregate in m_aggregates)
			{
				if (aggregate.ID == id)
				{
					return aggregate;
				}
			}

			return null;
		}

		/// <summary>
		/// Initializes the object with any aggregates contained in the collection.
		/// </summary>
		/// <param name="collection">A collection containing aggregate descriptions.</param>
		public void Init(ICollection collection)
		{
			Clear();

			if (collection != null)
			{
				ArrayList aggregates = new ArrayList(collection.Count);

				foreach (object value in collection)
				{
					if (value.GetType() == typeof(Aggregate))
					{
						aggregates.Add(Opc.Convert.Clone(value));
					}
				}

				m_aggregates = (Aggregate[])aggregates.ToArray(typeof(Aggregate));
			}
		}

		/// <summary>
		/// Removes all aggregates in the collection.
		/// </summary>
		public void Clear()
		{
			m_aggregates = new Aggregate[0];
		}

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return new AggregateCollection(this);
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
			get { return (m_aggregates != null)?m_aggregates.Length:0; }
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Array array, int index)
		{
			if (m_aggregates != null)
			{
				m_aggregates.CopyTo(array, index);
			}
		}

		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(Aggregate[] array, int index)
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
			return m_aggregates.GetEnumerator();
		}
		#endregion

		#region Private Members
		private Aggregate[] m_aggregates = new Aggregate[0];
		#endregion
	}

	/// <summary>
	/// Defines constants for well-known item aggregates.
	/// </summary>
	public class AggregateID
	{
		/// <remarks/>
		public const int NOAGGREGATE	   = 0;
		/// <remarks/>
		public const int INTERPOLATIVE	   = 1;
		/// <remarks/>
		public const int TOTAL		       = 2;
		/// <remarks/>
		public const int AVERAGE		   = 3;
		/// <remarks/>
		public const int TIMEAVERAGE	   = 4;
		/// <remarks/>
		public const int COUNT		       = 5;
		/// <remarks/>
		public const int STDEV		       = 6;
		/// <remarks/>
		public const int MINIMUMACTUALTIME = 7;
		/// <remarks/>
		public const int MINIMUM		   = 8;
		/// <remarks/>
		public const int MAXIMUMACTUALTIME = 9;
		/// <remarks/>
		public const int MAXIMUM		   = 10;
		/// <remarks/>
		public const int START		       = 11;
		/// <remarks/>
		public const int END		       = 12;
		/// <remarks/>
		public const int DELTA		       = 13;
		/// <remarks/>
		public const int REGSLOPE		   = 14;
		/// <remarks/>
		public const int REGCONST		   = 15;
		/// <remarks/>
		public const int REGDEV		       = 16;
		/// <remarks/>
		public const int VARIANCE		   = 17;
		/// <remarks/>
		public const int RANGE		       = 18;
		/// <remarks/>
		public const int DURATIONGOOD	   = 19;
		/// <remarks/>
		public const int DURATIONBAD	   = 20;
		/// <remarks/>
		public const int PERCENTGOOD	   = 21;
		/// <remarks/>
		public const int PERCENTBAD		   = 22;
		/// <remarks/>
		public const int WORSTQUALITY	   = 23;
		/// <remarks/>
		public const int ANNOTATIONS       = 24;
	}
}
