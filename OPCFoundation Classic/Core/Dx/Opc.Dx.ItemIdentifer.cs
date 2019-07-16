//============================================================================
// TITLE: Opc.Dx.ItemIdentifier.cs
//
// CONTENTS:
// 
// Classes for uniquely identifiable items in a DX configuration.
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
using System.Collections;
using System.Text;
using System.Xml;

namespace Opc.Dx
{
	/// <summary>
	/// A unique identifier for an item with an associated version id.
	/// </summary>
	[Serializable]
	public class ItemIdentifier : Opc.ItemIdentifier
	{
		/// <summary>
		/// A globally unique version identifier associated with a specific instance of an object.
		/// </summary>
		public string Version
		{
			get { return m_version;  }
			set { m_version = value; }
		}

		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		public ItemIdentifier() {}

		/// <summary>
		/// Initializes the object with the specified item name.
		/// </summary>
		public ItemIdentifier(string itemName) : base(itemName) {}

		/// <summary>
		/// Initializes the object with the specified item path and item name.
		/// </summary>
		public ItemIdentifier(string itemPath, string itemName) : base(itemPath, itemName) {}
		
		/// <summary>
		/// Initializes the object with the specified ItemIdentifier object.
		/// </summary>
		public ItemIdentifier(Opc.ItemIdentifier item) : base(item) {}

		/// <summary>
		/// Initializes the object with the specified item identifier.
		/// </summary>
		public ItemIdentifier(ItemIdentifier item) : base(item) 
		{
			if (item != null)
			{
				m_version = item.m_version;
			}
		}
			
		#region Object Method Overrides
		/// <summary>
		/// Returns true if the target object is equal to the object.
		/// </summary>
		public override bool Equals(object target)
		{
			if (typeof(Opc.Dx.ItemIdentifier).IsInstanceOfType(target))
			{
				Opc.Dx.ItemIdentifier itemID = (Opc.Dx.ItemIdentifier)target;

				return ((itemID.ItemName == ItemName) && (itemID.ItemPath == ItemPath) && (itemID.Version == Version));
			}

			return false;
		}

		/// <summary>
		/// Returns a useful hash code for the object.
		/// </summary>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		#endregion

		#region Private Members
		private string m_version = null;
		#endregion
	}

	/// <summary>
	/// A result code associated with a unique item identifier.
	/// </summary>
	[Serializable]
	public class IdentifiedResult : ItemIdentifier
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
}
