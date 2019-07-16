//============================================================================
// TITLE: Factory.cs
//
// CONTENTS:
// 
// A class used to instantiate XML server objects.
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
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace OpcXml
{
	/// <summary>
	/// A class used to instantiate XML server object.
	/// </summary>
	[Serializable]
	public class Factory : Opc.Factory
	{
		//======================================================================
		// Construction
		
		/// <summary>
		/// Initializes an instance to auto-detect the COM server.
		/// </summary>
		public Factory() : base(null, false)
		{
			// do nothing.
		}

		/// <summary>
		/// Initializes an instance with the specified system type.
		/// </summary>
		public Factory(System.Type systemType, bool useRemoting) : base(systemType, useRemoting)
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
			// validate requested type.
			if (SystemType != null)
			{
				// XML-DA 1.00
				if (SystemType == typeof(OpcXml.Da.Server))
				{
					OpcXml.Da.Server server = new OpcXml.Da.Server();

					try   { server.Initialize(url, connectData); }
					catch { throw new NotSupportedException(SystemType.FullName); }

					return server;
				}
				
				#if (UA)
				// UA 1.00
				if (SystemType == typeof(OpcXml.Ua.XmlServiceClient))
				{
					OpcXml.Ua.XmlServiceClient server = new OpcXml.Ua.XmlServiceClient();

					try   { server.Initialize(url, connectData); }
					catch { throw new NotSupportedException(SystemType.FullName); }

					return server;
				}
				#endif
			
				// object does not support requested interface type.
				throw new NotSupportedException(SystemType.FullName);
			}

			// auto-detect server type.
			else
			{
				// XML-DA 1.00
				if (url.Scheme == Opc.UrlScheme.HTTP)
				{
					try   
					{ 
						OpcXml.Da.Server server = new OpcXml.Da.Server();
						server.Initialize(url, connectData); 
						return server;
					}
					catch (Exception e) 
					{
						throw new Opc.ResultIDException(Opc.ResultID.E_FAIL, e.Message, e);
					}
				}

				#if (UA)
				// UA 1.00 over TCP
				if (url.Scheme == Opc.UrlScheme.UA_TCP)
				{
					try   
					{ 
						OpcXml.Ua.TcpServiceClient server = new OpcXml.Ua.TcpServiceClient();
						server.Initialize(url, connectData); 
						return server;
					}
					catch (Exception e) 
					{
						throw new Opc.ResultIDException(Opc.ResultID.E_FAIL, e.Message, e);
					}
				}

				// UA 1.00 over HTTP
				if (url.Scheme == Opc.UrlScheme.UA_HTTP)
				{
					try   
					{ 
						OpcXml.Ua.XmlServiceClient server = new OpcXml.Ua.XmlServiceClient();
						server.Initialize(url, connectData); 
						return server;
					}
					catch (Exception e) 
					{
						throw new Opc.ResultIDException(Opc.ResultID.E_FAIL, e.Message, e);
					}
				}
				#endif

				// object does not support requested url scheme.
				throw new NotSupportedException(url.Scheme);
			}
		}
	}
}
