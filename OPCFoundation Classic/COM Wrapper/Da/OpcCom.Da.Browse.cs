//============================================================================
// TITLE: Browse.cs
//
// CONTENTS:
// 
// Specializations for COM-DA of classes used to browse the server address space.
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
// 2003/04/05 RSA   Initial implementation.

using System;
using System.Xml;
using System.Collections;
using Opc.Da;

namespace OpcCom.Da
{
	/// <summary>
	/// Implements an object that handles multi-step browse operations.
	/// </summary>
	[Serializable]
	internal class BrowsePosition : Opc.Da.BrowsePosition
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
		/// Initializes a browse position 
		/// </summary>
		internal BrowsePosition(
			Opc.ItemIdentifier itemID, 
			BrowseFilters      filters, 
			string             continuationPoint)
		:
			base(itemID, filters)
		{
			ContinuationPoint = continuationPoint;
		}
	}	
}
