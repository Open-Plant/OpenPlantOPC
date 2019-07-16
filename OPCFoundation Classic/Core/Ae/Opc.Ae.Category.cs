//============================================================================
// TITLE: Opc.Ae.Category.cs
//
// CONTENTS:
// 
// Classes used to store information related to item categories.
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
	#region Category Class
	/// <summary>
	/// The description of an event category supported by the server.
	/// </summary>
	[Serializable]
	public class Category : ICloneable
	{
		#region Public Interface
		/// <summary>
		/// A unique identifier for the category.
		/// </summary>
		public int ID
		{
			get { return m_id;  } 
			set { m_id = value; } 
		}

		/// <summary>
		/// The unique name for the category.
		/// </summary>
		public string Name
		{
			get { return m_name;  } 
			set { m_name = value; } 
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
		private int    m_id   = 0;
		private string m_name = null;
		#endregion
	}
	#endregion
}
