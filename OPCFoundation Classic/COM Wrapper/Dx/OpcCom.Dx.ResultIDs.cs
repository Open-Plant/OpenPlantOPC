//============================================================================
// TITLE: Opc.Dx.ResultIDs.cs
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
// 2004/05/18 RSA   Initial implementation.

using System;
using System.Runtime.InteropServices;

namespace OpcCom.Dx
{
	/// <summary>
	/// Defines all well known COM DX HRESULT codes.
	/// </summary>
	public struct ResultIDs
	{		
		/// <remarks/>
		public const int E_PERSISTING                  = -0x3FFBF900; // 0xC0040700
		/// <remarks/>
		public const int E_NOITEMLIST                  = -0x3FFBF8FF; // 0xC0040701
		/// <remarks/>
		public const int E_SERVER_STATE                = -0x3FFBF8FE; // 0xC0040702
		/// <remarks/>
		public const int E_VERSION_MISMATCH            = -0x3FFBF8FD; // 0xC0040703
		/// <remarks/>
		public const int E_UNKNOWN_ITEM_PATH           = -0x3FFBF8FC; // 0xC0040704
		/// <remarks/>
		public const int E_UNKNOWN_ITEM_NAME           = -0x3FFBF8FB; // 0xC0040705
		/// <remarks/>
		public const int E_INVALID_ITEM_PATH           = -0x3FFBF8FA; // 0xC0040706
		/// <remarks/>
		public const int E_INVALID_ITEM_NAME           = -0x3FFBF8F9; // 0xC0040707
		/// <remarks/>
		public const int E_INVALID_NAME                = -0x3FFBF8F8; // 0xC0040708
		/// <remarks/>
		public const int E_DUPLICATE_NAME              = -0x3FFBF8F7; // 0xC0040709
		/// <remarks/>
		public const int E_INVALID_BROWSE_PATH         = -0x3FFBF8F6; // 0xC004070A
		/// <remarks/>
		public const int E_INVALID_SERVER_URL          = -0x3FFBF8F5; // 0xC004070B
		/// <remarks/>
		public const int E_INVALID_SERVER_TYPE         = -0x3FFBF8F4; // 0xC004070C
		/// <remarks/>
		public const int E_UNSUPPORTED_SERVER_TYPE     = -0x3FFBF8F3; // 0xC004070D
		/// <remarks/>
		public const int E_CONNECTIONS_EXIST           = -0x3FFBF8F2; // 0xC004070E
		/// <remarks/>
		public const int E_TOO_MANY_CONNECTIONS        = -0x3FFBF8F1; // 0xC004070F
		/// <remarks/>
		public const int E_OVERRIDE_BADTYPE            = -0x3FFBF8F0; // 0xC0040710
		/// <remarks/>
		public const int E_OVERRIDE_RANGE              = -0x3FFBF8EF; // 0xC0040711
		/// <remarks/>
		public const int E_SUBSTITUTE_BADTYPE          = -0x3FFBF8EE; // 0xC0040712
		/// <remarks/>
		public const int E_SUBSTITUTE_RANGE            = -0x3FFBF8ED; // 0xC0040713
		/// <remarks/>
		public const int E_INVALID_TARGET_ITEM         = -0x3FFBF8EC; // 0xC0040714
		/// <remarks/>
		public const int E_UNKNOWN_TARGET_ITEM         = -0x3FFBF8EB; // 0xC0040715
		/// <remarks/>
		public const int E_TARGET_ALREADY_CONNECTED    = -0x3FFBF8EA; // 0xC0040716
		/// <remarks/>
		public const int E_UNKNOWN_SERVER_NAME         = -0x3FFBF8E9; // 0xC0040717
		/// <remarks/>
		public const int E_UNKNOWN_SOURCE_ITEM         = -0x3FFBF8E8; // 0xC0040718
		/// <remarks/>
		public const int E_INVALID_SOURCE_ITEM         = -0x3FFBF8E7; // 0xC0040719
		/// <remarks/>
		public const int E_INVALID_QUEUE_SIZE          = -0x3FFBF8E6; // 0xC004071A
		/// <remarks/>
		public const int E_INVALID_DEADBAND            = -0x3FFBF8E5; // 0xC004071B
		/// <remarks/>
		public const int E_INVALID_CONFIG_FILE         = -0x3FFBF8E4; // 0xC004071C
		/// <remarks/>
		public const int E_PERSIST_FAILED              = -0x3FFBF8E3; // 0xC004071D
		/// <remarks/>
		public const int E_TARGET_FAULT                = -0x3FFBF8E2; // 0xC004071E
		/// <remarks/>
		public const int E_TARGET_NO_ACCESSS           = -0x3FFBF8E1; // 0xC004071F
		/// <remarks/>
		public const int E_SOURCE_SERVER_FAULT         = -0x3FFBF8E0; // 0xC0040720
		/// <remarks/>
		public const int E_SOURCE_SERVER_NO_ACCESSS    = -0x3FFBF8DF; // 0xC0040721
		/// <remarks/>
		public const int E_SUBSCRIPTION_FAULT          = -0x3FFBF8DE; // 0xC0040722
		/// <remarks/>
		public const int E_SOURCE_ITEM_BADRIGHTS       = -0x3FFBF8DD; // 0xC0040723
		/// <remarks/>
		public const int E_SOURCE_ITEM_BAD_QUALITY     = -0x3FFBF8DC; // 0xC0040724
		/// <remarks/>
		public const int E_SOURCE_ITEM_BADTYPE         = -0x3FFBF8DB; // 0xC0040725
		/// <remarks/>
		public const int E_SOURCE_ITEM_RANGE           = -0x3FFBF8DA; // 0xC0040726
		/// <remarks/>
		public const int E_SOURCE_SERVER_NOT_CONNECTED = -0x3FFBF8D9; // 0xC0040727
		/// <remarks/>
		public const int E_SOURCE_SERVER_TIMEOUT       = -0x3FFBF8D8; // 0xC0040728
		/// <remarks/>
		public const int E_TARGET_ITEM_DISCONNECTED    = -0x3FFBF8D7; // 0xC0040729
		/// <remarks/>
		public const int E_TARGET_NO_WRITES_ATTEMPTED  = -0x3FFBF8D6; // 0xC004072A
		/// <remarks/>
		public const int E_TARGET_ITEM_BADTYPE         = -0x3FFBF8D5; // 0xC004072B
		/// <remarks/>
		public const int E_TARGET_ITEM_RANGE           = -0x3FFBF8D4; // 0xC004072C
		/// <remarks/>
		public const int S_TARGET_SUBSTITUTED          = +0x00040780; // 0x00040780
		/// <remarks/>
		public const int S_TARGET_OVERRIDEN            = +0x00040781; // 0x00040781
		/// <remarks/>
		public const int S_CLAMP                       = +0x00040782; // 0x00040782
	}
}
