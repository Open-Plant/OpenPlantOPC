//============================================================================
// TITLE: Opc.Ae.Attribute.cs
//
// CONTENTS:
// 
// Classes used to store information related to event attributes.
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
// 2004/11/08 RSA   Initial implementation.

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using Opc;

namespace Opc.Ae
{
	#region Attribute Class
	/// <summary>
	/// The description of an attribute supported by the server.
	/// </summary>
	[Serializable]
	public class Attribute : ICloneable
	{
		#region Public Interface
		/// <summary>
		/// A unique identifier for the attribute.
		/// </summary>
		public int ID
		{
			get { return m_id;  } 
			set { m_id = value; } 
		}

		/// <summary>
		/// The unique name for the attribute.
		/// </summary>
		public string Name
		{
			get { return m_name;  } 
			set { m_name = value; } 
		}

		/// <summary>
		/// The data type of the attribute.
		/// </summary>
		public System.Type DataType
		{
			get { return m_datatype;  } 
			set { m_datatype = value; } 
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Name;
		}
		#endregion 

		#region ICloneable Members
		/// <summary>
		/// Creates a shallow copy of the object.
		/// </summary>
		public virtual object Clone() { return MemberwiseClone(); }
		#endregion
		
		#region Private Members
		private int m_id = 0;
		private string m_name = null;
		private System.Type m_datatype = null;
		#endregion
	}
	#endregion

	#region AttributeValue Class
	/// <summary>
	/// The value of an attribute for an event source.
	/// </summary>
	[Serializable]
	public class AttributeValue : ICloneable, Opc.IResult
	{
		#region Public Interface
		/// <summary>
		/// A unique identifier for the attribute.
		/// </summary>
		public int ID
		{
			get { return m_id;  } 
			set { m_id = value; } 
		}
		
		/// <summary>
		/// The attribute value.
		/// </summary>
		public object Value
		{
			get { return m_value;  }
			set { m_value = value; }
		}
		#endregion

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
			AttributeValue clone = (AttributeValue)MemberwiseClone();
			clone.Value = Opc.Convert.Clone(Value);
			return clone;
		}
		#endregion
		
		#region Private Members
		private int         m_id             = 0;
		private object      m_value          = null;
		private ResultID    m_resultID       = ResultID.S_OK; 
		private string      m_diagnosticInfo = null;
		#endregion
	}
	#endregion

	#region AttributeDictionary Class
	/// <summary>
	/// Contains multiple lists of the attributes indexed by category.
	/// </summary>
	[Serializable]
	public class AttributeDictionary : WriteableDictionary
	{
		#region Public Interface
		/// <summary>
		/// Gets or sets the atrtibute collection for the specified category. 
		/// </summary>
		public AttributeCollection this[int categoryID]
		{
			get { return (AttributeCollection)base[categoryID]; }
			
			set	
			{
				if (value != null)
				{
					base[categoryID] = value;
				}
				else
				{
					base[categoryID] = new AttributeCollection();
				}
			}
		}

		/// <summary>
		/// Adds an element with the provided key and value to the IDictionary.
		/// </summary>
		public virtual void Add(int key, int[] value)
		{
			if (value != null)
			{
				base.Add(key, new AttributeCollection(value));
			}
			else
			{
				base.Add(key, new AttributeCollection());
			}
		}

		/// <summary>
		/// Constructs an empty dictionary.
		/// </summary>
		public AttributeDictionary() : base(null, typeof(int), typeof(AttributeCollection)) {}

		/// <summary>
		/// Constructs an dictionary from a set of category ids.
		/// </summary>
		public AttributeDictionary(int[] categoryIDs) : base(null, typeof(int), typeof(AttributeCollection)) 
		{
			for (int ii = 0; ii < categoryIDs.Length; ii++)
			{
				this.Add(categoryIDs[ii], null);
			}
		}

		#endregion
	}
	#endregion

	#region AttributeCollection Class
	/// <summary>
	/// Contains a writeable collection attribute ids.
	/// </summary>
	[Serializable]
	public class AttributeCollection : WriteableCollection
	{			
		#region Public Interface
		/// <summary>
		/// An indexer for the collection.
		/// </summary>
		public new int this[int index]
		{
			get	{ return (int)Array[index]; }
		}

		/// <summary>
		/// Returns a copy of the collection as an array.
		/// </summary>
		public new int[] ToArray()
		{
			return (int[])Array.ToArray(typeof(int));
		}

		/// <summary>
		/// Creates an empty collection.
		/// </summary>
		internal AttributeCollection() : base(null, typeof(int)) {}

		/// <summary>
		/// Creates a collection from an array.
		/// </summary>
		internal AttributeCollection(int[] attributeIDs) : base(attributeIDs, typeof(int)) {}
		#endregion
	}
	#endregion

}
