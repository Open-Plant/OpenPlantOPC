//============================================================================
// TITLE: Opc.Ae.ResultIDs.cs
//
// CONTENTS:
// 
// Defines static information for well known error/success codes.
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
// 2004/11/08 RSA   Initial implementation.

using System;
using System.Runtime.InteropServices;

namespace OpcCom.Ae
{
	/// <summary>
	/// Defines all well known COM AE HRESULT codes.
	/// </summary>
	public struct ResultIDs
	{		
		/// <remarks/>
		public const int S_ALREADYACKED         = +0x00040200; // 0x00040200
		/// <remarks/>
		public const int S_INVALIDBUFFERTIME    = +0x00040201; // 0x00040201
		/// <remarks/>
		public const int S_INVALIDMAXSIZE       = +0x00040202; // 0x00040202
		/// <remarks/>
		public const int S_INVALIDKEEPALIVETIME = +0x00040203; // 0x00040203
		/// <remarks/>
		public const int E_INVALIDBRANCHNAME    = -0x3FFBFDFD; // 0xC0040203
		/// <remarks/>
		public const int E_INVALIDTIME          = -0x3FFBFDFC; // 0xC0040204
		/// <remarks/>
		public const int E_BUSY                 = -0x3FFBFDFB; // 0xC0040205
		/// <remarks/>
		public const int E_NOINFO               = -0x3FFBFDFA; // 0xC0040206
	}
}
