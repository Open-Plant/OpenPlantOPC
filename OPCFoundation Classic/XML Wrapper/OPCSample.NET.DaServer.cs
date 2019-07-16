//============================================================================
// TITLE: Server.cs
//
// CONTENTS:
// 
// A server that implements the COM-DA interfaces. 
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
using System.Net;
using System.Threading;
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Reflection;
using System.Runtime.InteropServices;
using Opc;
using Opc.Da;
using OpcCom.Da;

namespace OPCSample.NET
{
	/// <summary>
	/// A XML-DA server implementation that wraps a COM-DA server.
	/// </summary>
	[CLSCompliant(false)]
	public class DaServer : OpcCom.Da.Wrapper.Server
	{			
		public DaServer() 
		{
			IServer = m_server = new Opc.Da.SampleServer.Server(true);
		}

		#region Private Members
		private Opc.Da.SampleServer.Server m_server = null;
		#endregion
	}
}
