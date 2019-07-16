//============================================================================
// TITLE: IFactory.cs
//
// CONTENTS:
// 
// A interface and a class used to instantiate server objects.
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
// 2003/08/18 RSA   Initial implementation.

using System;
using System.Xml;
using System.Net;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

using OpcRcw.Comn;

namespace OpcCom
{
	/// <summary>
	/// The default class used to instantiate server objects.
	/// </summary>
	[Serializable]
	public class Factory : Opc.Factory
	{
		//======================================================================
		// Construction
		
		/// <summary>
		/// Initializes an instance for use for in process objects.
		/// </summary>
		public Factory() : base(null, false)
		{
			// do nothing.
		}

		/// <summary>
		/// Initializes an instance for use with .NET remoting.
		/// </summary>
		public Factory(bool useRemoting) : base(null, useRemoting)
		{
			// do nothing.
		}

		//======================================================================
		// ISerializable

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected Factory(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			// do nothing.
		}

		//======================================================================
		// IFactory

		/// <summary>
		/// Creates a new instance of the server.
		/// </summary>
		public override Opc.IServer CreateInstance(Opc.URL url, Opc.ConnectData connectData)
		{
			object comServer = Factory.Connect(url, connectData);
			
			if (comServer == null)
			{
				return null;
			}

			OpcCom.Server server = null; 
			System.Type interfaceType = null;

			try
			{
				// DA
				if (url.Scheme == Opc.UrlScheme.DA)
				{
					// Verify that it is a DA server.
					if (!typeof(OpcRcw.Da.IOPCServer).IsInstanceOfType(comServer))
					{
						interfaceType = typeof(OpcRcw.Da.IOPCServer);
						throw new NotSupportedException();
					}

					// DA 3.00
					if (typeof(OpcRcw.Da.IOPCBrowse).IsInstanceOfType(comServer) && typeof(OpcRcw.Da.IOPCItemIO).IsInstanceOfType(comServer))
					{
						server = new OpcCom.Da.Server(url, comServer);
					}
							
					// DA 2.XX
					else if (typeof(OpcRcw.Da.IOPCItemProperties).IsInstanceOfType(comServer))
					{
						server = new OpcCom.Da20.Server(url, comServer);
					}

					else
					{
						interfaceType = typeof(OpcRcw.Da.IOPCItemProperties);	
						throw new NotSupportedException();
					}
				}

				// AE
				else if (url.Scheme == Opc.UrlScheme.AE)
				{
					// Verify that it is a AE server.
					if (!typeof(OpcRcw.Ae.IOPCEventServer).IsInstanceOfType(comServer))
					{
						interfaceType = typeof(OpcRcw.Ae.IOPCEventServer);	
						throw new NotSupportedException();
					}
					
					server = new OpcCom.Ae.Server(url, comServer);
				}

				// HDA
				else if (url.Scheme == Opc.UrlScheme.HDA)
				{
					// Verify that it is a HDA server.
					if (!typeof(OpcRcw.Hda.IOPCHDA_Server).IsInstanceOfType(comServer))
					{
						interfaceType = typeof(OpcRcw.Hda.IOPCHDA_Server);	
						throw new NotSupportedException();
					}
					
					server = new OpcCom.Hda.Server(url, comServer);
				}
				
				// DX
				else if (url.Scheme == Opc.UrlScheme.DX)
				{
					// Verify that it is a DX server.
					if (!typeof(OpcRcw.Dx.IOPCConfiguration).IsInstanceOfType(comServer))
					{
						interfaceType = typeof(OpcRcw.Dx.IOPCConfiguration);	
						throw new NotSupportedException();
					}
					
					server = new OpcCom.Dx.Server(url, comServer);
				}

				// All other specifications not supported yet.
				else
				{
					throw new NotSupportedException(String.Format("The URL scheme '{0}' is not supported.", url.Scheme));
				}
			}
			catch (NotSupportedException e)
			{
				OpcCom.Interop.ReleaseServer(server);
				server = null;

				if (interfaceType != null)
				{
					StringBuilder message = new StringBuilder();

					message.AppendFormat("The COM server does not support the interface ");
					message.AppendFormat("'{0}'.", interfaceType.FullName);
					message.Append("\r\n\r\nThis problem could be caused by:\r\n");
					message.Append("- incorrectly installed proxy/stubs.\r\n");
					message.Append("- problems with the DCOM security settings.\r\n");
					message.Append("- a personal firewall (sometimes activated by default).\r\n");

					throw new NotSupportedException(message.ToString());
				}

				throw e;
			}
			catch (Exception e)
			{
				OpcCom.Interop.ReleaseServer(server);
				server = null;
			
				throw e;
			}

			// initialize the wrapper object.
			if (server != null)
			{
				server.Initialize(url, connectData);
			}

			return server;
		}

		/// <summary>
		/// Connects to the specified COM server server.
		/// </summary>
		public static object Connect(Opc.URL url, Opc.ConnectData connectData)
		{
			// parse path to find prog id and clsid.
			string progID = url.Path;
			string clsid  = null;

            int index = url.Path.LastIndexOf('/');

			if (index >= 0)
			{
				progID = url.Path.Substring(0, index);
				clsid  = url.Path.Substring(index+1);
			}

			// look up prog id if clsid not specified in the url.
			Guid guid;

			if (clsid == null)
			{
				// use OpcEnum to lookup the prog id.
				guid  = new ServerEnumerator().CLSIDFromProgID(progID, url.HostName, connectData);

				// check if prog id is actually a clsid string.
				if (guid == Guid.Empty)
				{
					try 
					{ 
						guid = new Guid(progID); 
					}
					catch 
					{
						throw new Opc.ConnectFailedException(progID);
					}
				}
			}
				
			// convert clsid string to a guid.
			else
			{
				try 
				{ 
					guid = new Guid(clsid);
				}
				catch 
				{
					throw new Opc.ConnectFailedException(clsid);
				}
			}

			// get the credentials.
			NetworkCredential credentials = (connectData != null)?connectData.GetCredential(null, null):null;

			// instantiate the server using CoCreateInstanceEx.
			if (connectData == null || connectData.LicenseKey == null)
			{
				try
				{
					return OpcCom.Interop.CreateInstance(guid, url.HostName, credentials);
				}
				catch (Exception e)
				{
					throw new Opc.ConnectFailedException(e);
				}	
			}
			
			// instantiate the server using IClassFactory2.
			else
			{
				try
				{
					return OpcCom.Interop.CreateInstanceWithLicenseKey(guid, url.HostName, credentials, connectData.LicenseKey);		
				}
				catch (Exception e)
				{
					throw new Opc.ConnectFailedException(e);
				}	
			}
		}
	}
}
