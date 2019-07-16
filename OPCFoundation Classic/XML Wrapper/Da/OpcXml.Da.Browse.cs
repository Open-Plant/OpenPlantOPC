//============================================================================
// TITLE: Browse.cs
//
// CONTENTS:
// 
// Specializations for XML-DA of classes used to browse the server address space.
//
// (c) Copyright 2003 The OPC Foundation
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
using System.Collections;
using Opc.Da;

namespace OpcXml.Da
{
	/// <summary>
	/// Implements an object that handles multi-step browse operations.
	/// </summary>
	[Serializable]
	public class BrowsePosition : Opc.Da.BrowsePosition
	{
		/// <summary>
		/// The continuation point for a browse operation.
		/// </summary>
		internal string ContinuationPoint = null;

		/// <summary>
		/// Indicates that elements that meet the filter criteria have not been returned.
		/// </summary>
		internal bool MoreElements = false;
		
		/// <summary>
		/// The the locale used for the browse operation.
		/// </summary>
		internal string Locale = null;

		/// <summary>
		/// Whether localized error texts should be reurned with the browse results.
		/// </summary>
		internal bool ReturnErrorText = false;

		/// <summary>
		/// Initializes a browse posistion 
		/// </summary>
		internal BrowsePosition(
			Opc.ItemIdentifier itemID, 
			BrowseFilters      filters, 
			string             locale,
			bool               returnErrorText) 
		: 
			base(itemID, filters) 
		{
			Locale          = locale;
			ReturnErrorText = returnErrorText;
		}
	}	

	/// <summary>
	/// Implements an object that handles multi-step browse operations at root
	/// </summary>
	[Serializable]
	public class RootBrowsePosition : BrowsePosition
	{
		/// <summary>
		/// The names of servers at root.
		/// </summary>
		internal string[] Names; 

		/// <summary>
		/// The index in the names array.
		/// </summary>
		internal int Index; 

		/// <summary>
		/// Initializes a browse posistion 
		/// </summary>
		internal RootBrowsePosition(
			Opc.ItemIdentifier itemID, 
			BrowseFilters      filters, 
			string             locale,
			bool               returnErrorText) 
		: 
			base(itemID, filters, locale, returnErrorText) 
		{
		}	
	}
}
