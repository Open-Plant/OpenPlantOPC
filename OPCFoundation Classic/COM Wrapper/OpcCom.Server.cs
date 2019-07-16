//============================================================================
// TITLE: Server.cs
//
// CONTENTS:
// 
// An in-process wrapper for a remote OPC Data Access 3.00 server.
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
using System.Runtime.InteropServices;
using Opc;
using OpcRcw.Comn;

namespace OpcCom
{
	/// <summary>
	/// An in-process wrapper for a remote OPC COM-DA server (not thread safe).
	/// </summary>
	public class Server : Opc.IServer
	{	
		#region Constructors
		/// <summary>
		/// Initializes the object.
		/// </summary>
		internal Server()
		{
			m_url      = null;
			m_server   = null;
			m_callback = new Callback(this);
		}
		
		/// <summary>
		/// Initializes the object with the specifed COM server.
		/// </summary>
		internal Server(URL url, object server) 
		{
			if (url == null) throw new ArgumentNullException("url");

			m_url      = (URL)url.Clone();
			m_server   = server;
			m_callback = new Callback(this);
		}
		#endregion
        
        #region IDisposable Members
        /// <summary>
        /// The finalizer.
        /// </summary>
        ~Server()
        {
            Dispose (false);
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
                lock (this)
                {
                    if (disposing)
                    {
                        // Free other state (managed objects).

                        // close callback connections.
                        if (m_connection != null)
                        {
                            m_connection.Dispose();
                            m_connection = null;
                        }
                    }

                    // Free your own state (unmanaged objects).
                    // Set large fields to null.                

				    // release server.
				    OpcCom.Interop.ReleaseServer(m_server);
				    m_server = null;
			    }

                m_disposed = true;
            }
        }

        private bool m_disposed = false;
		#endregion

		//======================================================================
		// Initialization

		/// <summary>
		/// Connects to the server with the specified URL and credentials.
		/// </summary>
		public virtual void Initialize(URL url, ConnectData connectData)
		{
			if (url == null) throw new ArgumentNullException("url");

			lock (this)
			{
				// re-connect only if the url has changed or has not been initialized.
				if (m_url == null || !m_url.Equals(url))
				{
					// release the current server.
					if (m_server != null)
					{
						Uninitialize();
					}

					// instantiate a new server.
					m_server = (IOPCCommon)OpcCom.Factory.Connect(url, connectData);
				}
		
				// save url.
				m_url = (URL)url.Clone();
			}
		}
		
		/// <summary>
		/// Releases the remote server.
		/// </summary>
		public virtual void Uninitialize()
		{
			lock (this)
			{
				Dispose();
			}
		}

		#region IServer Members
		//======================================================================
		// Events

		/// <summary>
		/// An event to receive server shutdown notifications.
		/// </summary>
		public virtual event ServerShutdownEventHandler ServerShutdown
		{
			add    
			{
				lock (this)
				{ 
					try
					{
						Advise(); 
						m_callback.ServerShutdown += value;
					}
					catch
					{
						// shutdown not supported.
					}
				}
			}
			
			remove 
			{
				lock (this)
				{ 
					m_callback.ServerShutdown -= value; 
					Unadvise(); 
				}
			}
		}

		//======================================================================
		// Localization

		/// <summary>
		/// The locale used in any error messages or results returned to the client.
		/// </summary>
		/// <returns>The locale name in the format "[languagecode]-[country/regioncode]".</returns>
		public virtual string GetLocale()
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					int localeID = 0;
					((IOPCCommon)m_server).GetLocaleID(out localeID);
					return OpcCom.Interop.GetLocale(localeID);
				}
				catch (Exception e)
				{
					throw Interop.CreateException("IOPCCommon.GetLocaleID", e);
				}
			}
		}

		/// <summary>
		/// Sets the locale used in any error messages or results returned to the client.
		/// </summary>
		/// <param name="locale">The locale name in the format "[languagecode]-[country/regioncode]".</param>
		/// <returns>A locale that the server supports and is the best match for the requested locale.</returns>
		public virtual string SetLocale(string locale)
		{
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				int lcid = OpcCom.Interop.GetLocale(locale);

				try
				{
					((IOPCCommon)m_server).SetLocaleID(lcid);
				}
				catch (Exception e)
				{
					if (lcid != 0)
					{
						throw Interop.CreateException("IOPCCommon.SetLocaleID", e);
					}

					// use LOCALE_SYSTEM_DEFAULT if the server does not support the Neutral LCID.
					try   { ((IOPCCommon)m_server).SetLocaleID(0x800); }
					catch {}
				}

				return GetLocale();
			}
		}

		/// <summary>
		/// Returns the locales supported by the server
		/// </summary>
		/// <remarks>The first element in the array must be the default locale for the server.</remarks>
		/// <returns>An array of locales with the format "[languagecode]-[country/regioncode]".</returns>
		public virtual string[] GetSupportedLocales() 
		{ 
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					int    count      = 0;
					IntPtr pLocaleIDs = IntPtr.Zero;

					((IOPCCommon)m_server).QueryAvailableLocaleIDs(out count, out pLocaleIDs);

					int[] localeIDs = OpcCom.Interop.GetInt32s(ref pLocaleIDs, count, true);
   
					if (localeIDs != null)
					{
						ArrayList locales = new ArrayList();

						foreach (int localeID in localeIDs)
						{
							try   { locales.Add(OpcCom.Interop.GetLocale(localeID)); }
							catch {}
						}

						return (string[])locales.ToArray(typeof(string));
					}

					return null;
				}
				catch (Exception e)
				{
					throw Interop.CreateException("IOPCCommon.QueryAvailableLocaleIDs", e);
				}
			}
		}

		/// <summary>
		/// Returns the localized text for the specified result code.
		/// </summary>
		/// <param name="locale">The locale name in the format "[languagecode]-[country/regioncode]".</param>
		/// <param name="resultID">The result code identifier.</param>
		/// <returns>A message localized for the best match for the requested locale.</returns>
		public virtual string GetErrorText(string locale, ResultID resultID)
		{ 
			lock (this)
			{
				if (m_server == null) throw new NotConnectedException();

				try
				{
					string currentLocale = GetLocale();

					if (currentLocale != locale)
					{
						SetLocale(locale);
					}

					string errorText = null;
					((IOPCCommon)m_server).GetErrorString(resultID.Code, out errorText);
					
					if (currentLocale != locale)
					{
						SetLocale(currentLocale);
					}
		
					return errorText;
				}
				catch (Exception e)
				{
					throw Interop.CreateException("IOPCServer.GetErrorString", e);
				}
			}
		}	
		#endregion

		#region Private Methods
		/// <summary>
		/// Establishes a connection point callback with the COM server.
		/// </summary>
		private void Advise()
		{
			if (m_connection == null)
			{
				m_connection = new ConnectionPoint(m_server, typeof(OpcRcw.Comn.IOPCShutdown).GUID);
				m_connection.Advise(m_callback);
			}
		}

		/// <summary>
		/// Closes a connection point callback with the COM server.
		/// </summary>
		private void Unadvise()
		{
			if (m_connection != null)
			{
				if (m_connection.Unadvise() == 0)
				{
					m_connection.Dispose();
					m_connection = null;
				}
			}
		}

		//======================================================================
		// IOPCShutdown

		/// <summary>
		/// A class that implements the IOPCShutdown interface.
		/// </summary>
		private class Callback : OpcRcw.Comn.IOPCShutdown
		{
			/// <summary>
			/// Initializes the object with the containing subscription object.
			/// </summary>
			public Callback(Server server) 
			{ 
				m_server = server;
			}

			/// <summary>
			/// An event to receive server shutdown notificiations.
			/// </summary>
			public event ServerShutdownEventHandler ServerShutdown
			{
				add    {lock (this){ m_serverShutdown += value; }}
				remove {lock (this){ m_serverShutdown -= value; }}
			}
				
			/// <summary>
			/// A table of item identifiers indexed by internal handle.
			/// </summary>
			private Server m_server = null;
			
			/// <summary>
			/// Raised when data changed callbacks arrive.
			/// </summary>
			private event ServerShutdownEventHandler m_serverShutdown = null;

			/// <summary>
			/// Called when a shutdown event is received.
			/// </summary>
			public void ShutdownRequest(string reason)
			{
				try
				{
					lock (this)
					{
						if (m_serverShutdown != null)
						{
							m_serverShutdown(reason);
						}
					}
				}
				catch (Exception e) 
				{ 
					string stack = e.StackTrace;
				}
			}
		}
		#endregion

		#region Private Members
		/// <summary>
		/// The COM server wrapped by the object.
		/// </summary>
		protected object m_server = null;

		/// <summary>
		/// The URL containing host, prog id and clsid information for the remote server.
		/// </summary>
		protected URL m_url = null;

		/// <summary>
		/// A connect point with the COM server.
		/// </summary>
		private ConnectionPoint m_connection = null;

		/// <summary>
		/// The internal object that implements the IOPCShutdown interface.
		/// </summary>
		private Callback m_callback = null;
		#endregion
	}
}
