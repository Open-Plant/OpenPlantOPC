//============================================================================
// TITLE: Opc.IDiscovery.cs
//
// CONTENTS:
// 
// An interface is used to discover OPC servers on the network.
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
// 2003/03/26 RSA   Initial implementation.

using System;
using System.Net;

namespace Opc
{
	/// <summary>
	/// This interface is used to discover OPC servers on the network.
	/// </summary>
	public interface IDiscovery : IDisposable
	{
		/// <summary>
		/// Returns a list of host names which could contain OPC servers.
		/// </summary>
		/// <returns>A array of strings that are valid network host names.</returns>
		string[] EnumerateHosts();

		/// <summary>
		/// Returns a list of servers that support an OPC specification.
		/// </summary>
		/// <param name="specification">A unique identifier for an OPC specification.</param>
		/// <returns>An array of unconnected OPC server obejcts on the local machine.</returns>
		Server[] GetAvailableServers(Specification specification);

		/// <summary>
		/// Returns a list of servers that support an OPC specification on remote machine.
		/// </summary>
		/// <param name="specification">A unique identifier for an OPC specification.</param>
		/// <param name="host">The network host name of the machine to search for servers.</param>
		/// <param name="connectData">Any necessary user authentication or protocol configuration information.</param>
		/// <returns>An array of unconnected OPC server objects.</returns>
		Server[] GetAvailableServers(Specification specification, string host, ConnectData connectData);
	}

	/// <summary>
	/// A description of an interface version defined by an OPC specification.
	/// </summary>
	[Serializable]
	public struct Specification
	{
		/// <summary>
		/// The unique identifier for the interface version. 
		/// </summary>
		public string ID
		{
			get { return m_id;  }
			set { m_id = value; }
		}

		/// <summary>
		/// The human readable description for the interface version.
		/// </summary>
		public string Description
		{
			get { return m_description;  }
			set { m_description = value; }
		}

		/// <summary>
		/// Returns true if the objects are equal.
		/// </summary>
		public static bool operator==(Specification a, Specification b) 
		{
			return a.Equals(b);
		}

		/// <summary>
		/// Returns true if the objects are not equal.
		/// </summary>
		public static bool operator!=(Specification a, Specification b) 
		{
			return !a.Equals(b);
		}

		#region Constructors
		/// <summary>
		/// Initializes the object with the description and a GUID as a string.
		/// </summary>
		public Specification(string id, string description)
		{
			m_id = id;
			m_description = description;
		}
		#endregion

		#region Object Member Overrides
		/// <summary>
		/// Determines if the object is equal to the specified value.
		/// </summary>
		public override bool Equals(object target)
		{
			if (target != null && target.GetType() == typeof(Specification))
			{
				return (ID == ((Specification)target).ID);
			}

			return false;
		}

		/// <summary>
		/// Converts the object to a string used for display.
		/// </summary>
		public override string ToString()
		{
			return Description;
		}
		
		/// <summary>
		/// Returns a suitable hash code for the result.
		/// </summary>
		public override int GetHashCode()
		{
			return (ID != null)?ID.GetHashCode():base.GetHashCode();
		}
		#endregion

		#region Private Members
		private string m_id;
		private string m_description;
		#endregion

		/// <summary>
		/// A set of Specification objects for existing OPC specifications.
		/// </summary>
		public static readonly Specification COM_AE_10    = new Specification("58E13251-AC87-11d1-84D5-00608CB8A7E9", "Alarms and Event 1.XX");
		/// <remarks/>
		public static readonly Specification COM_BATCH_10 = new Specification("A8080DA0-E23E-11D2-AFA7-00C04F539421", "Batch 1.00");
		/// <remarks/>
		public static readonly Specification COM_BATCH_20 = new Specification("843DE67B-B0C9-11d4-A0B7-000102A980B1", "Batch 2.00");
		/// <remarks/>
		public static readonly Specification COM_DA_10    = new Specification("63D5F430-CFE4-11d1-B2C8-0060083BA1FB", "Data Access 1.0a");
		/// <remarks/>
		public static readonly Specification COM_DA_20    = new Specification("63D5F432-CFE4-11d1-B2C8-0060083BA1FB", "Data Access 2.XX");
		/// <remarks/>
		public static readonly Specification COM_DA_30    = new Specification("CC603642-66D7-48f1-B69A-B625E73652D7", "Data Access 3.00");
		/// <remarks/>
		public static readonly Specification COM_DX_10    = new Specification("A0C85BB8-4161-4fd6-8655-BB584601C9E0", "Data eXchange 1.00");
		/// <remarks/>
		public static readonly Specification COM_HDA_10   = new Specification("7DE5B060-E089-11d2-A5E6-000086339399", "Historical Data Access 1.XX");
		/// <remarks/>
		public static readonly Specification XML_DA_10    = new Specification("3098EDA4-A006-48b2-A27F-247453959408", "XML Data Access 1.00");
		/// <remarks/>
		public static readonly Specification UA10         = new Specification("EC10AFD8-9BC0-4828-B47E-B3D907F929B1", "Unified Architecture 1.00");
	}

	/// <summary>
	/// Contains information required to connect to the server.
	/// </summary>
	[Serializable]
	public class URL : ICloneable
	{
		/// <summary>
		/// The scheme (protocol) for the URL.
		/// </summary>
		public string Scheme
		{
			get { return m_scheme;  }
			set { m_scheme = value; }
		}

		/// <summary>
		/// The host name for the URL.
		/// </summary>
		public string HostName
		{
			get { return m_hostName;  }
			set { m_hostName = value; }
		}

		/// <summary>
		/// The port name for the URL (0 means default for protocol).
		/// </summary>
		public int Port
		{
			get { return m_port;  }
			set { m_port = value; }
		}

		/// <summary>
		/// The path for the URL.
		/// </summary>
		public string Path
		{
			get { return m_path;  }
			set { m_path = value; }
		}

		#region Constructors
		/// <summary>
		/// Initializes an empty instance.
		/// </summary>
		public URL()
		{
			Scheme   = UrlScheme.HTTP;
			HostName = "localhost";
			Port     = 0;
			Path     = null;
		}

        /// <summary>
        /// Initializes an instance by parsing a URL string.
        /// </summary>
        public URL(string url)
        {
            Scheme = UrlScheme.HTTP;
            HostName = "localhost";
            Port = 0;
            Path = null;

            string buffer = url;

            // extract the scheme (default is http).
            int index = buffer.IndexOf("://");

            if (index >= 0)
            {
                Scheme = buffer.Substring(0, index);
                buffer = buffer.Substring(index + 3);
            }

            index = buffer.IndexOfAny(new char[] { '/' });

            if (index < 0)
            {
                Path = buffer;
                return;
            }

            string hostPortString = buffer.Substring(0, index);
            IPAddress address;

            try
            {
                address = IPAddress.Parse(hostPortString);
            }
            catch
            {
                address = null;
            }

            if (address != null && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (hostPortString.Contains("]"))
                {
                    HostName = hostPortString.Substring(0, hostPortString.IndexOf("]") + 1);
                    if (hostPortString.Substring(hostPortString.IndexOf(']')).Contains(":"))
                    {
                        string portString = hostPortString.Substring(hostPortString.LastIndexOf(':') + 1);
                        if (portString != "")
                        {
                            try
                            {
                                Port = System.Convert.ToUInt16(portString);
                            }
                            catch
                            {
                                Port = 0;
                            }
                        }
                        else
                        {
                            Port = 0;
                        }
                    }
                    else
                    {
                        Port = 0;
                    }

                    Path = buffer.Substring(index + 1);
                }
                else
                {
                    HostName = "[" + hostPortString + "]";
                    Port = 0;
                }

                Path = buffer.Substring(index + 1);
            }
            else
            {

                // extract the hostname (default is localhost).
                index = buffer.IndexOfAny(new char[] { ':', '/' });

                if (index < 0)
                {
                    Path = buffer;
                    return;
                }

                HostName = buffer.Substring(0, index);

                // extract the port number (default is 0).
                if (buffer[index] == ':')
                {
                    buffer = buffer.Substring(index + 1);
                    index = buffer.IndexOf("/");

                    string port = null;

                    if (index >= 0)
                    {
                        port = buffer.Substring(0, index);
                        buffer = buffer.Substring(index + 1);
                    }
                    else
                    {
                        port = buffer;
                        buffer = "";
                    }

                    try
                    {
                        Port = (int)System.Convert.ToUInt16(port);
                    }
                    catch
                    {
                        Port = 0;
                    }
                }
                else
                {
                    buffer = buffer.Substring(index + 1);
                }

                // extract the path.
                Path = buffer;
            }
        }
		#endregion

		#region Object Method Overrides
		/// <summary>
		/// Returns a URL string for the object.
		/// </summary>
		public override string ToString()
		{
			string hostName = (HostName == null || HostName == "")?"localhost":HostName;

			if (Port > 0)
			{
				return String.Format("{0}://{1}:{2}/{3}", new object[] { Scheme, hostName, Port, Path });
			}
			else
			{
				return String.Format("{0}://{1}/{2}", new object[] { Scheme, hostName, Path });
			}
		}

		/// <summary>
		/// Compares the object to either another URL object or a URL string.
		/// </summary>
		public override bool Equals(object target)
		{
			URL url = null;

			if (target != null && target.GetType() == typeof(URL)) 
			{
				url = (URL)target;
			}
			
			if (target != null && target.GetType() == typeof(string)) 
			{
				url = new URL((string)target);
			}

			if (url == null)                   return false;
			if (url.Path     != this.Path)     return false;
			if (url.Scheme   != this.Scheme)   return false;
			if (url.HostName != this.HostName) return false;
			if (url.Port     != this.Port)     return false;

			return true;
		}

		/// <summary>
		/// Returns a hash code for the object.
		/// </summary>
		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Returns a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return this.MemberwiseClone(); 
		}
		#endregion

		#region Private Members
		private string m_scheme = null;
		private string m_hostName = null;
		private int m_port = 0;
		private string m_path = null;
		#endregion
	}

	/// <summary>
	/// Defines string constants for well-known URL schemes.
	/// </summary>
	public class UrlScheme
	{
		/// <summary>
		/// XML Web Service.
		/// </summary>
		public const string HTTP = "http";

		/// <summary>
		/// OPC Alarms and Events
		/// </summary>
		public const string AE = "opcae";

		/// <summary>
		/// OPC Batch
		/// </summary>
		public const string BATCH = "opcbatch";

		/// <summary>
		/// OPC Data Access
		/// </summary>
		public const string DA = "opcda";

		/// <summary>
		/// OPC Data eXchange
		/// </summary>
		public const string DX = "opcdx";

		/// <summary>
		/// OPC Historical Data Access
		/// </summary>
		public const string HDA = "opchda";

		/// <summary>
		/// OPC XML Data Access over HTTP.
		/// </summary>
		public const string XMLDA = "opc.xmlda";

		/// <summary>
		/// OPC Unified Architecture over SOAP/HTTP
		/// </summary>
		public const string UA_HTTP = "opc.http";

		/// <summary>
		/// OPC Unified Architecture over SOAP/TCP
		/// </summary>
		public const string UA_TCP = "opc.tcp";
	}
}
