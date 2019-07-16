//============================================================================
// TITLE: Opc.Server.cs
//
// CONTENTS:
// 
// A base class for an in-process object used to access OPC servers.
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
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Threading;
using System.Resources;
using System.Reflection;
using System.Runtime.Serialization;

namespace Opc
{
	/// <summary>
	/// A base class for an in-process object used to access OPC servers.
	/// </summary>
	[Serializable]
	public class Server : IServer, ISerializable, ICloneable
	{
		#region Constructors
		/// <summary>
		/// Initializes the object with a factory and a default URL.
		/// </summary>
		/// <param name="factory">The Opc.Factory used to connect to remote servers.</param>
		/// <param name="url">The network address of a remote server.</param>
		public Server(Factory factory, URL url)
		{
			if (factory == null) throw new ArgumentNullException("factory");

			m_factory          = (IFactory)factory.Clone();
			m_server           = null;
			m_url              = null;
			m_name             = null;
			m_supportedLocales = null;
			m_resourceManager  = new ResourceManager("Opc.Resources.Strings", Assembly.GetExecutingAssembly());

			if (url != null) SetUrl(url);
		}
		#endregion
        
        #region IDisposable Members
        /// <summary>
        /// The finalizer.
        /// </summary>
        ~Server()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases unmanaged resources held by the object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged resources held by the object.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                	if (m_factory != null)
			        {
				        m_factory.Dispose();
				        m_factory = null;
                    }

                    if (m_server != null)
                    {
                        try { Disconnect(); }
                        catch { }

                        m_server = null;
                    }
                }

                // Free your own state (unmanaged objects).
                // Set large fields to null.

                m_disposed = true;
            }
        }

        private bool m_disposed = false;
        #endregion

		#region ISerializable Members
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string NAME    = "Name";
			internal const string URL     = "Url";
			internal const string FACTORY = "Factory";
		}

		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		protected Server(SerializationInfo info, StreamingContext context)
		{
			m_name    = info.GetString(Names.NAME);
			m_url     = (URL)info.GetValue(Names.URL, typeof(URL));
			m_factory = (IFactory)info.GetValue(Names.FACTORY, typeof(IFactory));
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(Names.NAME,    m_name);
			info.AddValue(Names.URL,     m_url);
			info.AddValue(Names.FACTORY, m_factory);
		}	
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Returns an unconnected copy of the server with the same URL. 
		/// </summary>
		public virtual object Clone()
		{
			// do a memberwise clone.
			Server clone = (Server)MemberwiseClone();
			
			// place clone in disconnected state.
			clone.m_server           = null;
			clone.m_supportedLocales = null;
			clone.m_locale           = null;
			clone.m_resourceManager  = new ResourceManager("Opc.Resources.Strings", Assembly.GetExecutingAssembly());

			// return clone.
			return clone;
		}
		#endregion
		
		//======================================================================
		// Public Properties
        		
		/// <summary>
		/// A short descriptive name for the server assigned by the client.
		/// </summary>
		public virtual string Name 
		{
			get	{ return m_name; }
			set	{ m_name = value; }
		}

		/// <summary>
		/// The URL that describes the network location of the server.
		/// </summary>
		public virtual URL Url 
		{
			get	{ return (m_url != null)?(URL)m_url.Clone():null; }
			set	{ SetUrl(value); }
		}
		
		/// <summary>
		/// The default of locale used by the remote server.
		/// </summary>
		public virtual string Locale {get{ return m_locale; }}
		
		/// <summary>
		/// The set of locales supported by the remote server.
		/// </summary>
		public virtual string[] SupportedLocales {get{ return (m_supportedLocales != null)?(string[])m_supportedLocales.Clone():null; }}
			
		/// <summary>
		/// Whether the remote server is currently connected.
		/// </summary>
		public virtual bool IsConnected {get{ return (m_server != null); }}

		//======================================================================
		// Connection Management

		/// <summary>
		/// Establishes a physical connection to the remote server.
		/// </summary>
		public virtual void Connect()
		{
			Connect(m_url, null); 
		}

		/// <summary>
		/// Establishes a physical connection to the remote server.
		/// </summary>
		/// <param name="connectData">Any protocol configuration or user authenication information.</param>
		public virtual void Connect(ConnectData connectData)
		{
			Connect(m_url, connectData); 
		}

		/// <summary>
		/// Establishes a physical connection to the remote server identified by a URL.
		/// </summary>
		/// <param name="url">The network address of the remote server.</param>
		/// <param name="connectData">Any protocol configuration or user authenication information.</param>
		public virtual void Connect(URL url, ConnectData connectData)
		{ 
			if (url == null) throw new ArgumentNullException("url");
			if (m_server != null) throw new AlreadyConnectedException();

			// save url.
			SetUrl(url);

			try
			{
				// instantiate the server object.
				m_server = m_factory.CreateInstance(url, connectData);

				// save the connect data.
				m_connectData = connectData;
			
				// cache the supported locales.
				GetSupportedLocales();

				// update the default locale.
				SetLocale(m_locale);
			}
			catch (Exception e)
			{
				if (m_server != null)
				{
					try   { Disconnect(); }
					catch {}
				}

				throw e;
			}
		}

		/// <summary>
		/// Disconnects from the server and releases all network resources.
		/// </summary>
		public virtual void Disconnect() 
		{
			if (m_server == null) throw new NotConnectedException();
						
			// dispose of the remote server object.
			m_server.Dispose();
			m_server = null;
		}
		
		//======================================================================
		// Public Methods
		/// <summary>
		/// Creates a new instance of a server object with the same factory and url.
		/// </summary>
		/// <remarks>This method does not copy the value of any properties.</remarks>
		/// <returns>An unconnected duplicate instance of the server object.</returns>
		public virtual Opc.Server Duplicate()
		{
			Server instance = (Opc.Server)Activator.CreateInstance(GetType(), new object[] {m_factory, m_url});

			// preserve the credentials.
			instance.m_connectData = m_connectData; 

			// preserve the locale.
			instance.m_locale = m_locale;

			return instance;
		}		
		
		//======================================================================
		// Events

		/// <summary>
		/// An event to receive server shutdown notifications.
		/// </summary>
		public virtual event ServerShutdownEventHandler ServerShutdown
		{
			add    { m_server.ServerShutdown += value; }
			remove { m_server.ServerShutdown -= value; }
		}

		//======================================================================
		// Localization

		/// <summary>
		/// The locale used in any error messages or results returned to the client.
		/// </summary>
		/// <returns>The locale name in the format "[languagecode]-[country/regioncode]".</returns>
		public virtual string GetLocale()
		{
			if (m_server == null) throw new NotConnectedException();

			// cache the current locale.
			m_locale = m_server.GetLocale();

			// return the cached value.
			return m_locale;
		}

		/// <summary>
		/// Sets the locale used in any error messages or results returned to the client.
		/// </summary>
		/// <param name="locale">The locale name in the format "[languagecode]-[country/regioncode]".</param>
		/// <returns>A locale that the server supports and is the best match for the requested locale.</returns>
		public virtual string SetLocale(string locale)
		{
			if (m_server == null) throw new NotConnectedException();

			try
			{
				// set the requested locale on the server.
				m_locale = m_server.SetLocale(locale);
			}
			catch
			{
				// find a best match and check if the server supports it.
				string revisedLocale = Server.FindBestLocale(locale, m_supportedLocales);

				if (revisedLocale != locale)
				{
					m_server.SetLocale(revisedLocale);
				}

				// cache the revised locale.
				m_locale = revisedLocale;
			}
			
			// return actual local used.
			return m_locale;
		}	

		/// <summary>
		/// Returns the locales supported by the server
		/// </summary>
		/// <remarks>The first element in the array must be the default locale for the server.</remarks>
		/// <returns>An array of locales with the format "[languagecode]-[country/regioncode]".</returns>
		public virtual string[] GetSupportedLocales() 
		{ 
			if (m_server == null) throw new NotConnectedException();
			
			// cache supported locales.
			m_supportedLocales = m_server.GetSupportedLocales();

			// return copy of cached locales. 
			return SupportedLocales;
		}

		/// <summary>
		/// Returns the localized text for the specified result code.
		/// </summary>
		/// <param name="locale">The locale name in the format "[languagecode]-[country/regioncode]".</param>
		/// <param name="resultID">The result code identifier.</param>
		/// <returns>A message localized for the best match for the requested locale.</returns>
		public virtual string GetErrorText(string locale, ResultID resultID)
		{ 
			if (m_server == null) throw new NotConnectedException();

			return m_server.GetErrorText((locale == null)?m_locale:locale, resultID);
		}	
	
		#region Private Methods
		/// <summary>
		/// Returns a localized string with the specified name.
		/// </summary>
		protected string GetString(string name)
		{
			// create a culture object.
			CultureInfo culture = null;
			
			try   { culture = new CultureInfo(Locale); }
			catch {	culture = new CultureInfo(""); }

			// lookup resource string.
			try   { return m_resourceManager.GetString(name, culture); }
			catch { return null; }
		}

		/// <summary>
		/// Updates the URL for the server.
		/// </summary>
		protected void SetUrl(URL url)
		{
			if (url == null) throw new ArgumentNullException("url");

			// cannot change the URL if the remote server is already instantiated.
			if (m_server != null) throw new AlreadyConnectedException();

			// copy the url.
			m_url = (URL)url.Clone();

			// construct a name for the server.
			string name = "";

			// use the host name as a base.
			if (m_url.HostName != null)
			{
				name = m_url.HostName.ToLower();

				// suppress localhoat and loopback as explicit hostnames.
				if (name == "localhost" || name == "127.0.0.1")
				{
					name = "";
				}
			}

			// append the port.
			if (m_url.Port != 0)
			{
				name += String.Format(".{0}", m_url.Port);
			}
				
			// add a separator.
			if (name != "") { name += "."; }

			// use the prog id as the name.
			if (m_url.Scheme != UrlScheme.HTTP)
			{
				string progID = m_url.Path;

				int index = progID.LastIndexOf('/');

				if (index != -1)
				{	
					progID = progID.Substring(0, index);
				}

				name += progID;
			}

			// use full path without the extension as the name.
			else
			{
				string path = m_url.Path;

				// strip the file extension.
				int index = path.LastIndexOf('.');

				if (index != -1)
				{	
					path = path.Substring(0, index);
				}
				
				// replace slashes with dashes.
				while (path.IndexOf('/') != -1)
				{
					path = path.Replace('/', '-');
				}

				name += path;
			}
			
			// save the generated name.
			m_name = name;
		}

		/// <summary>
		/// Finds the best matching locale given a set of supported locales.
		/// </summary>
		public static string FindBestLocale(string requestedLocale, string[] supportedLocales)
		{
			try
			{
				// check for direct match with requested locale.
				foreach (string supportedLocale in supportedLocales)
				{	
					if (supportedLocale == requestedLocale) 
					{
						return requestedLocale;
					}
				}

				// try to find match for parent culture.
				CultureInfo requestedCulture = new CultureInfo(requestedLocale);

				foreach (string supportedLocale in supportedLocales)
				{	
					try
					{
						CultureInfo supportedCulture = new CultureInfo(supportedLocale);

						if (requestedCulture.Parent.Name == supportedCulture.Name)
						{
							return supportedCulture.Name;
						}
					}
					catch
					{
						continue;
					}
				}

				// return default locale.		
				return (supportedLocales != null && supportedLocales.Length > 0)?supportedLocales[0]:"";
			}
			catch
			{
				// return default locale on any error.		
				return (supportedLocales != null && supportedLocales.Length > 0)?supportedLocales[0]:"";
			}
		}
		#endregion

		#region Private Members
		/// <summary>
		/// The remote server object.
		/// </summary>
		protected IServer m_server = null;
		
		/// <summary>
		/// The URL that describes the network location of the server.
		/// </summary>
		private URL m_url = null;

		/// <summary>
		/// The factory used to instantiate the remote server.
		/// </summary>
		protected IFactory m_factory = null;
		
		/// <summary>
		/// The last set of credentials used to connect successfully to the server.
		/// </summary>
		private ConnectData m_connectData = null;

		/// <summary>
		/// A short name for the server.
		/// </summary>
		private string m_name = null;

		/// <summary>
		/// The default locale used by the server.
		/// </summary>
		private string m_locale = null;

		/// <summary>
		/// The set of locales supported by the remote server.
		/// </summary>
		private string[] m_supportedLocales = null;

		/// <summary>
		/// The resource manager used to access localized resources.
		/// </summary>
		protected ResourceManager m_resourceManager = null;
		#endregion
	}

	//=============================================================================
	// Asynchronous Delegates

	/// <summary>
	/// The asynchronous delegate for Connect.
	/// </summary>
	public delegate void ConnectAsyncDelegate(URL url, ConnectData connectData);

	/// <summary>
	/// The asynchronous delegate for Disconnect.
	/// </summary>
	public delegate void DisconnectAsyncDelegate();

	/// <summary>
	/// The asynchronous delegate for GetLocale.
	/// </summary>
	public delegate string GetLocaleAsyncDelegate();

	/// <summary>
	/// The asynchronous delegate for SetLocale.
	/// </summary>
	public delegate void SetLocaleAsyncDelegate(string locale);

	/// <summary>
	/// The asynchronous delegate for GetSupportedLocales.
	/// </summary>
	public delegate string[] GetSupportedLocalesAsyncDelegate();

	/// <summary>
	/// The asynchronous delegate for GetErrorText.
	/// </summary>
	public delegate string GetErrorTextAsyncDelegate(string locale, ResultID resultID);

	//=============================================================================
	// Exceptions

	/// <summary>
	/// Raised if an operation cannot be executed because the server is not connected.
	/// </summary>
	[Serializable]
	public class AlreadyConnectedException : ApplicationException
	{
		private const string Default = "The remote server is already connected.";
		/// <remarks/>
		public AlreadyConnectedException() : base(Default) {} 
		/// <remarks/>
		public AlreadyConnectedException(string message) : base(Default + "\r\n" + message) {}
		/// <remarks/>
		public AlreadyConnectedException(Exception e) : base(Default, e) {}
		/// <remarks/>
		public AlreadyConnectedException(string message, Exception innerException): base (Default + "\r\n" + message, innerException) {}
		/// <remarks/>
		protected AlreadyConnectedException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}

	/// <summary>
	/// Raised if an operation cannot be executed because the server is not connected.
	/// </summary>
	[Serializable]
	public class NotConnectedException : ApplicationException
	{
		private const string Default = "The remote server is not currently connected.";
		/// <remarks/>
		public NotConnectedException() : base(Default) {} 
		/// <remarks/>
		public NotConnectedException(string message) : base(Default + "\r\n" + message) {}
		/// <remarks/>
		public NotConnectedException(Exception e) : base(Default, e) {}
		/// <remarks/>
		public NotConnectedException(string message, Exception innerException): base (Default + "\r\n" + message, innerException) {}
		/// <remarks/>
		protected NotConnectedException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}
	
	/// <summary>
	/// Raised if an operation cannot be executed because the server is not reachable.
	/// </summary>
	[Serializable]
	public class ConnectFailedException : ResultIDException
	{
		private const string Default = "Could not connect to server.";
		/// <remarks/>
		public ConnectFailedException() : base(ResultID.E_ACCESS_DENIED, Default) {} 
		/// <remarks/>
		public ConnectFailedException(string message) : base(ResultID.E_NETWORK_ERROR, Default + "\r\n" + message) {}
		/// <remarks/>
		public ConnectFailedException(Exception e) : base(ResultID.E_NETWORK_ERROR, Default, e) {}
		/// <remarks/>
		public ConnectFailedException(string message, Exception innerException): base(ResultID.E_NETWORK_ERROR, Default + "\r\n" + message, innerException) {}
		/// <remarks/>
		protected ConnectFailedException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}

	/// <summary>
	/// Raised if an operation cannot be executed because the server refuses access.
	/// </summary>
	[Serializable]
	public class AccessDeniedException : ResultIDException
	{
		private const string Default = "The server refused the connection.";
		/// <remarks/>
		public AccessDeniedException() : base(ResultID.E_ACCESS_DENIED, Default) {} 
		/// <remarks/>
		public AccessDeniedException(string message) : base(ResultID.E_ACCESS_DENIED, Default + "\r\n" + message) {}
		/// <remarks/>
		public AccessDeniedException(Exception e) : base(ResultID.E_ACCESS_DENIED, Default, e) {}
		/// <remarks/>
		public AccessDeniedException(string message, Exception innerException): base(ResultID.E_NETWORK_ERROR, Default + "\r\n" + message, innerException) {}
		/// <remarks/>
		protected AccessDeniedException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}

	/// <summary>
	/// Raised an remote operation by the server timed out
	/// </summary>
	public class ServerTimeoutException : ResultIDException
	{
		private const string Default = "The server did not respond within the specified timeout period.";
		/// <remarks/>
		public ServerTimeoutException() : base(ResultID.E_TIMEDOUT, Default) {} 
		/// <remarks/>
		public ServerTimeoutException(string message) : base(ResultID.E_TIMEDOUT, Default + "\r\n" + message) {}
		/// <remarks/>
		public ServerTimeoutException(Exception e) : base(ResultID.E_TIMEDOUT, Default, e) {}
		/// <remarks/>
		public ServerTimeoutException(string message, Exception innerException): base (ResultID.E_TIMEDOUT, Default + "\r\n" + message, innerException) {}
		/// <remarks/>
		protected ServerTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}

	/// <summary>
	/// Raised an remote operation by the server returned unusable or invalid results.
	/// </summary>
	[Serializable]
	public class InvalidResponseException : ApplicationException
	{
		private const string Default = "The response from the server was invalid or incomplete.";
		/// <remarks/>
		public InvalidResponseException() : base(Default) {} 
		/// <remarks/>
		public InvalidResponseException(string message) : base(Default + "\r\n" + message) {}
		/// <remarks/>
		public InvalidResponseException(Exception e) : base(Default, e) {}		
		/// <remarks/>
		public InvalidResponseException(string message, Exception innerException): base (Default + "\r\n" + message, innerException) {}
		/// <remarks/>
		protected InvalidResponseException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}

	/// <summary>
	/// Raised if the browse position is not valid.
	/// </summary>
	[Serializable]
	public class BrowseCannotContinueException : ApplicationException
	{
		private const string Default = "The browse operation cannot continue.";
		/// <remarks/>
		public BrowseCannotContinueException() : base(Default) {} 
		/// <remarks/>
		public BrowseCannotContinueException(string message) : base(Default + "\r\n" + message) {}
		/// <remarks/>
		public BrowseCannotContinueException(string message, Exception innerException): base (Default + "\r\n" + message, innerException) {}
		/// <remarks/>
		protected BrowseCannotContinueException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}
}
