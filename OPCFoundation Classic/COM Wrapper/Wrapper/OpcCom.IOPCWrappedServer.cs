//============================================================================
// (c) Copyright 2005 The OPC Foundation
// ALL RIGHTS RESERVED.
//
// DISCLAIMER:
//  This code is provided by the OPC Foundation solely to assist in 
//  understanding and use of the appropriate OPC Specification(s) and may be 
//  used as set forth in the License Grant section of the OPC Specification.
//  This code is provided as-is and without warranty or support of any sort
//  and is subject to the Warranty and Liability Disclaimers which appear
//  in the printed OPC Specification.

using System;
using System.Runtime.InteropServices;

namespace OpcCom
{
	/// <summary>
	/// An interface that is invoked when the wrapper loads/unloads the server.
	/// </summary>
	[ComImport]
	[GuidAttribute("50E8496C-FA60-46a4-AF72-512494C664C6")]
	[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
	public interface IOPCWrappedServer
	{
		/// <summary>
		/// Called when the object is loaded by the COM wrapper process.
		/// </summary>
		void Load([MarshalAs(UnmanagedType.LPStruct)] Guid clsid);

		/// <summary>
		/// Called when the object is unloaded by the COM wrapper process.
		/// </summary>
		void Unload();
	}
}
