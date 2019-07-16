/* ========================================================================
 * Copyright (c) 2005-2010 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Xml;
using System.Runtime.Serialization;
using Microsoft.Win32;

namespace OpcRcw
{
    /// <summary>
    /// Utility functions used by COM applications.
    /// </summary>
    public static class Utils
    {
		/// <summary>
		/// Registers the COM types in the specified assembly.
		/// </summary>
		public static List<System.Type> RegisterComTypes(string filePath)
		{
			// load assmebly.
			Assembly assembly = Assembly.LoadFrom(filePath);

			// check that the correct assembly is being registered.
			VerifyCodebase(assembly, filePath);

			RegistrationServices services = new RegistrationServices();

			// list types to register/unregister.
			List<System.Type> types = new List<System.Type>(services.GetRegistrableTypesInAssembly(assembly));

			// register types.
			if (types.Count > 0)
			{
				// unregister types first.	
				if (!services.UnregisterAssembly(assembly))
				{
					throw new ApplicationException("Unregister COM Types Failed.");
				}

				// register types.	
				if (!services.RegisterAssembly(assembly, AssemblyRegistrationFlags.SetCodeBase))
				{
					throw new ApplicationException("Register COM Types Failed.");
				}
			}

			return types;
		}

		/// <summary>
		/// Checks that the assembly loaded has the expected codebase.
		/// </summary>
		private static void VerifyCodebase(Assembly assembly, string filepath)
		{
			string codebase = assembly.CodeBase.ToLower();
			string normalizedPath = filepath.Replace('\\', '/').Replace("//", "/").ToLower();

			if (!normalizedPath.StartsWith("file:///"))
			{
				normalizedPath = "file:///" + normalizedPath;
			}

			if (codebase != normalizedPath)
			{
				throw new ApplicationException(String.Format("Duplicate assembly loaded. You need to restart the application.\r\n{0}\r\n{1}", codebase, normalizedPath));
			}
		}

		/// <summary>
		/// Unregisters the COM types in the specified assembly.
		/// </summary>
		public static List<System.Type> UnregisterComTypes(string filePath)
		{
			// load assmebly.
			Assembly assembly = Assembly.LoadFrom(filePath);

			// check that the correct assembly is being unregistered.
			VerifyCodebase(assembly, filePath);

			RegistrationServices services = new RegistrationServices();

			// list types to register/unregister.
			List<System.Type> types = new List<System.Type>(services.GetRegistrableTypesInAssembly(assembly));

			// unregister types.	
			if (!services.UnregisterAssembly(assembly))
			{
				throw new ApplicationException("Unregister COM Types Failed.");
			}

			return types;
		}

        /// <summary>
        /// Retrieves the system message text for the specified error.
        /// </summary>
        public static string GetSystemMessage(int error, int localeId)
        {
            int langId = 0;

            switch (localeId)
            {
                case LOCALE_SYSTEM_DEFAULT:
                {
                    langId = GetSystemDefaultLangID();
                    break;
                }

                case LOCALE_USER_DEFAULT:
                {
                    langId = GetUserDefaultLangID();
                    break;
                }

                default:
                {
                    langId = (0xFFFF & localeId);
                    break;
                }
            }

            IntPtr buffer = Marshal.AllocCoTaskMem(MAX_MESSAGE_LENGTH);

            int result = FormatMessageW(
                (int)FORMAT_MESSAGE_FROM_SYSTEM,
                IntPtr.Zero,
                error,
                langId,
                buffer,
                MAX_MESSAGE_LENGTH - 1,
                IntPtr.Zero);

            if (result > 0)
            {
                string msg = Marshal.PtrToStringUni(buffer);
                Marshal.FreeCoTaskMem(buffer);

                if (msg != null && msg.Length > 0)
                {
                    return msg.Trim();
                }
            }

            return String.Format("0x{0:X8}", error);
        }

		/// <summary>
		/// Returns the prog id from the clsid.
		/// </summary>
		public static string ProgIDFromCLSID(Guid clsid)
		{
			RegistryKey key = Registry.ClassesRoot.OpenSubKey(String.Format(@"CLSID\{{{0}}}\ProgId", clsid));
					
			if (key != null)
			{
				try
				{
					return key.GetValue("") as string;
				}
				finally
				{
					key.Close();
				}
			}

			return null;
		}

		/// <summary>
		/// Returns the prog id from the clsid.
		/// </summary>
		public static Guid CLSIDFromProgID(string progID)
		{
			if (String.IsNullOrEmpty(progID))
			{
				return Guid.Empty;
			}

			RegistryKey key = Registry.ClassesRoot.OpenSubKey(String.Format(@"{0}\CLSID", progID));
					
			if (key != null)
			{
				try
				{
					string clsid = key.GetValue(null) as string;

					if (clsid != null)
					{
						return new Guid(clsid.Substring(1, clsid.Length-2));
					}
				}
				finally
				{
					key.Close();
				}
			}

			return Guid.Empty;
		}
          
        /// <summary>
        /// Returns the implemented categories for the class.
        /// </summary>
        public static List<Guid> GetImplementedCategories(Guid clsid)
        {
            List<Guid> categories = new List<Guid>();

			string categoriesKey = String.Format(@"CLSID\{{{0}}}\Implemented Categories", clsid);
			
			RegistryKey key = Registry.ClassesRoot.OpenSubKey(categoriesKey);

			if (key != null)
			{
                try
                {
				    foreach (string catid in key.GetSubKeyNames())
				    {
                        try
                        {
                            Guid guid = new Guid(catid.Substring(1, catid.Length-2));
                            categories.Add(guid);
                        }
                        catch (Exception)
                        {
                            // ignore invalid keys.
                        }
				    }
                }
                finally
                {
                    key.Close();
                }
			}

            return categories;
        }

		/// <summary>
		/// Fetches the classes in the specified categories.
		/// </summary>
		public static List<Guid> EnumClassesInCategories(params Guid[] categories)
		{
			ICatInformation manager = (ICatInformation)CreateLocalServer(CLSID_StdComponentCategoriesMgr);
	
			object unknown = null;

			try
			{
				manager.EnumClassesOfCategories(
					1,
                    categories, 
					0, 
					null,
					out unknown);
                
				IEnumGUID enumerator = (IEnumGUID)unknown;

				List<Guid> classes = new List<Guid>();

				Guid[] buffer = new Guid[10];

				while (true)
				{
					int fetched = 0;

                    IntPtr pGuids = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(Guid))*buffer.Length);
                    
                    try
                    {
					    enumerator.Next(buffer.Length, pGuids, out fetched);

					    if (fetched == 0)
					    {
						    break;
					    }
                        
			            IntPtr pos = pGuids;

			            for (int ii = 0; ii < fetched; ii++)
			            {
				            buffer[ii] = (Guid)Marshal.PtrToStructure(pos, typeof(Guid));
				            pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(Guid)));
			            }
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pGuids);
                    }

					for (int ii = 0; ii < fetched; ii++)
					{
						classes.Add(buffer[ii]);
					}
				}
			
				return classes;
			}
			finally
			{
				ReleaseServer(unknown);
				ReleaseServer(manager);
			}
        }

        /// <summary>
        /// COM servers that support the DA 2.0 specification.
        /// </summary>
        public static readonly Guid CATID_OPCDAServer20 = typeof(OpcRcw.Da.CATID_OPCDAServer20).GUID;

        /// <summary>
        /// COM servers that support the DA 3.0 specification.
        /// </summary>
        public static readonly Guid CATID_OPCDAServer30 = typeof(OpcRcw.Da.CATID_OPCDAServer30).GUID;

        /// <summary>
        /// COM servers that support the AE 1.0 specification.
        /// </summary>
        public static readonly Guid CATID_OPCAEServer10 = typeof(OpcRcw.Ae.CATID_OPCAEServer10).GUID;

        /// <summary>
        /// COM servers that support the HDA 1.0 specification.
        /// </summary>
        public static readonly Guid CATID_OPCHDAServer10 = typeof(OpcRcw.Hda.CATID_OPCHDAServer10).GUID;

		/// <summary>
		/// Returns the location of the COM server executable.
		/// </summary>
		public static string GetExecutablePath(Guid clsid)
		{
			RegistryKey key = Registry.ClassesRoot.OpenSubKey(String.Format(@"CLSID\{{{0}}}\LocalServer32", clsid));

			if (key == null)
			{
				key	= Registry.ClassesRoot.OpenSubKey(String.Format(@"CLSID\{{{0}}}\InprocServer32", clsid));
			}

			if (key != null)
			{
				try
				{
					string codebase = key.GetValue("Codebase") as string;

					if (codebase == null)
					{
						return key.GetValue(null) as string;
					}

					return codebase;
				}
				finally
				{
					key.Close();
				}
			}

			return null;
		}

		/// <summary>
		/// Creates an instance of a COM server on the current machine using the current user.
		/// </summary>
		public static object CreateLocalServer(Guid clsid)
		{
			COSERVERINFO coserverInfo = new COSERVERINFO();

			coserverInfo.pwszName     = null;
			coserverInfo.pAuthInfo    = IntPtr.Zero;
			coserverInfo.dwReserved1  = 0;
			coserverInfo.dwReserved2  = 0;

			GCHandle hIID = GCHandle.Alloc(IID_IUnknown, GCHandleType.Pinned);

			MULTI_QI[] results = new MULTI_QI[1];

			results[0].iid  = hIID.AddrOfPinnedObject();
			results[0].pItf = null;
			results[0].hr   = 0;

			try
			{
				// create an instance.
				CoCreateInstanceEx(
					ref clsid,
					null,
					CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER,
					ref coserverInfo,
					1,
					results);
			}
			finally
			{
				hIID.Free();
			}

			if (results[0].hr != 0)
			{
				throw new ExternalException("CoCreateInstanceEx: 0x{0:X8}" + results[0].hr);
			}

			return results[0].pItf;
		}

        #region ServerInfo Class
        /// <summary>
        /// A class used to allocate and deallocate the elements of a COSERVERINFO structure.
        /// </summary>
        class ServerInfo
        {
            #region Public Interface
            /// <summary>
            /// Allocates a COSERVERINFO structure.
            /// </summary>
            public COSERVERINFO Allocate(string hostName, string username, string password, string domain)
            {
                // initialize server info structure.
                COSERVERINFO serverInfo = new COSERVERINFO();

                serverInfo.pwszName = hostName;
                serverInfo.pAuthInfo = IntPtr.Zero;
                serverInfo.dwReserved1 = 0;
                serverInfo.dwReserved2 = 0;

                // no authentication for default identity
                if (String.IsNullOrEmpty(username))
                {
                    return serverInfo;
                }

                m_hUserName = GCHandle.Alloc(username, GCHandleType.Pinned);
                m_hPassword = GCHandle.Alloc(password, GCHandleType.Pinned);
                m_hDomain = GCHandle.Alloc(domain, GCHandleType.Pinned);

                m_hIdentity = new GCHandle();

                // create identity structure.
                COAUTHIDENTITY authIdentity = new COAUTHIDENTITY();

                authIdentity.User = m_hUserName.AddrOfPinnedObject();
                authIdentity.UserLength = (uint)((username != null) ? username.Length : 0);
                authIdentity.Password = m_hPassword.AddrOfPinnedObject();
                authIdentity.PasswordLength = (uint)((password != null) ? password.Length : 0);
                authIdentity.Domain = m_hDomain.AddrOfPinnedObject();
                authIdentity.DomainLength = (uint)((domain != null) ? domain.Length : 0);
                authIdentity.Flags = SEC_WINNT_AUTH_IDENTITY_UNICODE;

                m_hIdentity = GCHandle.Alloc(authIdentity, GCHandleType.Pinned);

                // create authorization info structure.
                COAUTHINFO authInfo = new COAUTHINFO();

                authInfo.dwAuthnSvc = RPC_C_AUTHN_WINNT;
                authInfo.dwAuthzSvc = RPC_C_AUTHZ_NONE;
                authInfo.pwszServerPrincName = IntPtr.Zero;
                authInfo.dwAuthnLevel = RPC_C_AUTHN_LEVEL_CONNECT;
                authInfo.dwImpersonationLevel = RPC_C_IMP_LEVEL_IMPERSONATE;
                authInfo.pAuthIdentityData = m_hIdentity.AddrOfPinnedObject();
                authInfo.dwCapabilities = EOAC_NONE; // EOAC_DYNAMIC_CLOAKING;

                m_hAuthInfo = GCHandle.Alloc(authInfo, GCHandleType.Pinned);

                // update server info structure.
                serverInfo.pAuthInfo = m_hAuthInfo.AddrOfPinnedObject();

                return serverInfo;
            }

            /// <summary>
            /// Deallocated memory allocated when the COSERVERINFO structure was created.
            /// </summary>
            public void Deallocate()
            {
                if (m_hUserName.IsAllocated) m_hUserName.Free();
                if (m_hPassword.IsAllocated) m_hPassword.Free();
                if (m_hDomain.IsAllocated) m_hDomain.Free();
                if (m_hIdentity.IsAllocated) m_hIdentity.Free();
                if (m_hAuthInfo.IsAllocated) m_hAuthInfo.Free();
            }
            #endregion

            #region Private Members
            private GCHandle m_hUserName;
            private GCHandle m_hPassword;
            private GCHandle m_hDomain;
            private GCHandle m_hIdentity;
            private GCHandle m_hAuthInfo;
            #endregion
        }
        #endregion

        /// <summary>
        /// Creates an instance of a COM server.
        /// </summary>
        public static object CreateInstance(Guid clsid, string hostName, string username, string password, string domain)
        {
            ServerInfo serverInfo = new ServerInfo();
            COSERVERINFO coserverInfo = serverInfo.Allocate(hostName, username, password, domain);

            GCHandle hIID = GCHandle.Alloc(IID_IUnknown, GCHandleType.Pinned);

            MULTI_QI[] results = new MULTI_QI[1];

            results[0].iid = hIID.AddrOfPinnedObject();
            results[0].pItf = null;
            results[0].hr = 0;

            try
            {
                // check whether connecting locally or remotely.
                uint clsctx = CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER;

                if (!String.IsNullOrEmpty(hostName) && hostName != "localhost")
                {
                    clsctx = CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER;
                }

                // create an instance.
                CoCreateInstanceEx(
                    ref clsid,
                    null,
                    clsctx,
                    ref coserverInfo,
                    1,
                    results);
            }
            finally
            {
                if (hIID.IsAllocated) hIID.Free();
                serverInfo.Deallocate();
            }

            if (results[0].hr != 0)
            {
                throw CreateComException(
                    ResultIds.E_FAIL, 
                    "Could not create COM server '{0}' on host '{1}'. Reason: {2}.", 
                    clsid, hostName,
                    GetSystemMessage((int)results[0].hr, LOCALE_SYSTEM_DEFAULT));
            }

            return results[0].pItf;
        }

        /// <summary>
		/// Releases the server if it is a true COM server.
		/// </summary>
		public static void ReleaseServer(object server)
		{
			if (server != null && server.GetType().IsCOMObject)
			{
				Marshal.ReleaseComObject(server);
			}
		}
        
		/// <summary>
		/// Registers the classes in the specified category.
		/// </summary>
		public static void RegisterClassInCategory(Guid clsid, Guid catid)
		{
			RegisterClassInCategory(clsid, catid, null);
		}

		/// <summary>
		/// Registers the classes in the specified category.
		/// </summary>
		public static void RegisterClassInCategory(Guid clsid, Guid catid, string description)
		{
			ICatRegister manager = (ICatRegister)CreateLocalServer(CLSID_StdComponentCategoriesMgr);
	
			try
			{
				string existingDescription = null;

				try
				{
					((ICatInformation)manager).GetCategoryDesc(catid, 0, out existingDescription);
				}
				catch (Exception e)
				{
					existingDescription = description;

					if (String.IsNullOrEmpty(existingDescription))
					{
						if (catid == CATID_OPCDAServer20)
						{
							existingDescription = CATID_OPCDAServer20_Description;
						}
						else if (catid == CATID_OPCDAServer30)
						{
							existingDescription = CATID_OPCDAServer30_Description;
						}
						else if (catid == CATID_OPCAEServer10)
						{
							existingDescription = CATID_OPCAEServer10_Description;
						}
						else if (catid == CATID_OPCHDAServer10)
						{
							existingDescription = CATID_OPCHDAServer10_Description;
						}
						else
						{
							throw new ApplicationException("No description for category available", e);
						}
					}

					CATEGORYINFO info;

					info.catid         = catid;
					info.lcid          = 0;
					info.szDescription = existingDescription;

					// register category.
					manager.RegisterCategories(1, new CATEGORYINFO[] { info });
				}

				// register class in category.
				manager.RegisterClassImplCategories(clsid, 1, new Guid[] { catid });
			}
			finally
			{
				ReleaseServer(manager);
			}
		}

        /// <summary>
        /// Removes the registration for a COM server from the registry.
        /// </summary>
        public static void UnregisterComServer(Guid clsid)
        {
			// unregister class in categories.
			string categoriesKey = String.Format(@"CLSID\{{{0}}}\Implemented Categories", clsid);
			
			RegistryKey key = Registry.ClassesRoot.OpenSubKey(categoriesKey);

			if (key != null)
			{
				try	  
				{ 
					foreach (string catid in key.GetSubKeyNames())
					{
						try	  
						{ 
							Utils.UnregisterClassInCategory(clsid, new Guid(catid.Substring(1, catid.Length-2)));
						}
						catch (Exception)
						{
							// ignore errors.
						}
					}
				}
				finally
				{
					key.Close();
				}
			}

			string progidKey = String.Format(@"CLSID\{{{0}}}\ProgId", clsid);

			// delete prog id.
			key = Registry.ClassesRoot.OpenSubKey(progidKey);
					
			if (key != null)
			{
				string progId = key.GetValue(null) as string;
				key.Close();

				if (!String.IsNullOrEmpty(progId))
				{
					try	  
					{ 
						Registry.ClassesRoot.DeleteSubKeyTree(progId); 
					}
					catch (Exception)
					{
						// ignore errors.
					}
				}
			}

			// delete clsid.
			try	  
			{ 
				Registry.ClassesRoot.DeleteSubKeyTree(String.Format(@"CLSID\{{{0}}}", clsid)); 
			}
			catch (Exception)
			{
				// ignore errors.
			}
        }

		/// <summary>
		/// Unregisters the classes in the specified category.
		/// </summary>
		public static void UnregisterClassInCategory(Guid clsid, Guid catid)
		{
            ICatRegister manager = (ICatRegister)CreateLocalServer(CLSID_StdComponentCategoriesMgr);
	
			try
			{
				manager.UnRegisterClassImplCategories(clsid, 1, new Guid[] { catid });
			}
			finally
			{
				ReleaseServer(manager);
			}
		}

        /// <summary>
        /// Converts an exception to an exception that returns a COM error code.
        /// </summary>
        public static Exception CreateComException(Exception e)
        {
            return CreateComException(e, 0, null);
        }

        /// <summary>
        /// Converts an exception to an exception that returns a COM error code.
        /// </summary>
        public static Exception CreateComException(int code, string message, params object[] args)
        {
            return CreateComException(null, code, message, args);
        }

        /// <summary>
        /// Converts an exception to an exception that returns a COM error code.
        /// </summary>
        public static Exception CreateComException(Exception e, int code, string message, params object[] args)
        {
            // check for code.
            if (code == 0)
            {
                if (e is COMException)
                {
                    code = ((COMException)e).ErrorCode;
                }
                else
                {
                    code = ResultIds.E_FAIL;
                }
            }

            // check for message to format.
            if (!String.IsNullOrEmpty(message))
            {
                if (args != null && args.Length > 0)
                {
                    message = String.Format(System.Globalization.CultureInfo.CurrentUICulture, message, args);
                }
            }
            else
            {
                if (e != null)
                {
                    message = e.Message;
                }
                else
                {
                    message = GetSystemMessage(code, System.Globalization.CultureInfo.CurrentUICulture.LCID);
                }
            }

            // construct exception.
            return new COMException(message, code);
        }

        #region COM Interop Declarations
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		private struct MULTI_QI
		{
			public IntPtr iid;
			[MarshalAs(UnmanagedType.IUnknown)]
			public object pItf;
			public uint   hr;
		}
		
		private const uint CLSCTX_INPROC_SERVER	 = 0x1;
		private const uint CLSCTX_INPROC_HANDLER = 0x2;
		private const uint CLSCTX_LOCAL_SERVER	 = 0x4;
		private const uint CLSCTX_REMOTE_SERVER	 = 0x10;

		private static readonly Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
		
		[DllImport("ole32.dll")]
		private static extern void CoCreateInstanceEx(
			ref Guid         clsid,
			[MarshalAs(UnmanagedType.IUnknown)]
			object           punkOuter,
			uint             dwClsCtx,
			[In]
			ref COSERVERINFO pServerInfo,
			uint             dwCount,
			[In, Out]
			MULTI_QI[]       pResults);

	    [ComImport]
	    [GuidAttribute("0002E000-0000-0000-C000-000000000046")]
	    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
        private interface IEnumGUID 
        {
            void Next(
		        [MarshalAs(UnmanagedType.I4)]
                int celt,
                [Out]
                IntPtr rgelt,
                [Out][MarshalAs(UnmanagedType.I4)]
                out int pceltFetched);

            void Skip(
		        [MarshalAs(UnmanagedType.I4)]
                int celt);

            void Reset();

            void Clone(
                [Out]
                out IEnumGUID ppenum);
        }

        [ComImport]
		[GuidAttribute("0002E013-0000-0000-C000-000000000046")]
		[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
		private interface ICatInformation
		{
			void EnumCategories( 
				int lcid,				
				[MarshalAs(UnmanagedType.Interface)]
				[Out] out object ppenumCategoryInfo);
        
			void GetCategoryDesc( 
				[MarshalAs(UnmanagedType.LPStruct)] 
				Guid rcatid,
				int lcid,
				[MarshalAs(UnmanagedType.LPWStr)]
				[Out] out string pszDesc);
        
			void EnumClassesOfCategories( 
				int cImplemented,
				[MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=0)] 
				Guid[] rgcatidImpl,
				int cRequired,
				[MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=2)] 
				Guid[] rgcatidReq,
				[MarshalAs(UnmanagedType.Interface)]
				[Out] out object ppenumClsid);
        
			void IsClassOfCategories( 
				[MarshalAs(UnmanagedType.LPStruct)] 
				Guid rclsid,
				int cImplemented,
				[MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=1)] 
				Guid[] rgcatidImpl,
				int cRequired,
				[MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=3)] 
				Guid[] rgcatidReq);
        
			void EnumImplCategoriesOfClass( 
				[MarshalAs(UnmanagedType.LPStruct)] 
				Guid rclsid,
				[MarshalAs(UnmanagedType.Interface)]
				[Out] out object ppenumCatid);
        
			void EnumReqCategoriesOfClass(
				[MarshalAs(UnmanagedType.LPStruct)] 
				Guid rclsid,
				[MarshalAs(UnmanagedType.Interface)]
				[Out] out object ppenumCatid);
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
		struct CATEGORYINFO 
		{
			public Guid catid;
			public int lcid;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=127)] 
			public string szDescription;
		}

		[ComImport]
		[GuidAttribute("0002E012-0000-0000-C000-000000000046")]
		[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
		private interface ICatRegister
		{
			void RegisterCategories(
				int cCategories,
				[MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=0)] 
				CATEGORYINFO[] rgCategoryInfo);

			void UnRegisterCategories(
				int cCategories,
				[MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=0)] 
				Guid[] rgcatid);

			void RegisterClassImplCategories(
				[MarshalAs(UnmanagedType.LPStruct)] 
				Guid rclsid,
				int cCategories,
				[MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=1)] 
				Guid[] rgcatid);

			void UnRegisterClassImplCategories(
				[MarshalAs(UnmanagedType.LPStruct)] 
				Guid rclsid,
				int cCategories,
				[MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=1)] 
				Guid[] rgcatid);

			void RegisterClassReqCategories(
				[MarshalAs(UnmanagedType.LPStruct)] 
				Guid rclsid,
				int cCategories,
				[MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=1)] 
				Guid[] rgcatid);

			void UnRegisterClassReqCategories(
				[MarshalAs(UnmanagedType.LPStruct)] 
				Guid rclsid,
				int cCategories,
				[MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=1)] 
				Guid[] rgcatid);
		}
        
        private static readonly Guid CLSID_StdComponentCategoriesMgr = new Guid("0002E005-0000-0000-C000-000000000046");

        private const string CATID_OPCDAServer20_Description  = "OPC Data Access Servers Version 2.0";
        private const string CATID_OPCDAServer30_Description  = "OPC Data Access Servers Version 3.0";
        private const string CATID_OPCAEServer10_Description  = "OPC Alarm & Event Server Version 1.0";
        private const string CATID_OPCHDAServer10_Description = "OPC History Data Access Servers Version 1.0";


        #region OLE32 Function/Interface Declarations
        private const int MAX_MESSAGE_LENGTH = 1024;

        private const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

        [DllImport("Kernel32.dll")]
        private static extern int FormatMessageW(
            int dwFlags,
            IntPtr lpSource,
            int dwMessageId,
            int dwLanguageId,
            IntPtr lpBuffer,
            int nSize,
            IntPtr Arguments);

        [DllImport("Kernel32.dll")]
        private static extern int GetSystemDefaultLangID();

        [DllImport("Kernel32.dll")]
        private static extern int GetUserDefaultLangID();

        /// <summary>
        /// The WIN32 system default locale.
        /// </summary>
        public const int LOCALE_SYSTEM_DEFAULT = 0x800;

        /// <summary>
        /// The WIN32 user default locale.
        /// </summary>
        public const int LOCALE_USER_DEFAULT = 0x400;

        /// <summary>
        /// The base for the WIN32 FILETIME structure.
        /// </summary>
        private static readonly DateTime FILETIME_BaseTime = new DateTime(1601, 1, 1);

        /// <summary>
        /// WIN32 GUID struct declaration.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct GUID
        {
            public int Data1;
            public short Data2;
            public short Data3;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Data4;
        }

        /// <summary>
        /// The size, in bytes, of a VARIANT structure.
        /// </summary>
        private const int VARIANT_SIZE = 0x10;

        [DllImport("OleAut32.dll")]
        private static extern int VariantChangeTypeEx(
            IntPtr pvargDest,
            IntPtr pvarSrc,
            int lcid,
            ushort wFlags,
            short vt);

        /// <summary>
        /// Intializes a pointer to a VARIANT.
        /// </summary>
        [DllImport("oleaut32.dll")]
        private static extern void VariantInit(IntPtr pVariant);

        /// <summary>
        /// Frees all memory referenced by a VARIANT stored in unmanaged memory.
        /// </summary>
        [DllImport("oleaut32.dll")]
        public static extern void VariantClear(IntPtr pVariant);

        private const int DISP_E_TYPEMISMATCH = -0x7FFDFFFB; // 0x80020005
        private const int DISP_E_OVERFLOW = -0x7FFDFFF6; // 0x8002000A

        private const int VARIANT_NOVALUEPROP = 0x01;
        private const int VARIANT_ALPHABOOL = 0x02; // For VT_BOOL to VT_BSTR conversions convert to "True"/"False" instead of

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SOLE_AUTHENTICATION_SERVICE
        {
            public uint dwAuthnSvc;
            public uint dwAuthzSvc;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pPrincipalName;
            public int hr;
        }

        private const uint RPC_C_AUTHN_NONE = 0;
        private const uint RPC_C_AUTHN_DCE_PRIVATE = 1;
        private const uint RPC_C_AUTHN_DCE_PUBLIC = 2;
        private const uint RPC_C_AUTHN_DEC_PUBLIC = 4;
        private const uint RPC_C_AUTHN_GSS_NEGOTIATE = 9;
        private const uint RPC_C_AUTHN_WINNT = 10;
        private const uint RPC_C_AUTHN_GSS_SCHANNEL = 14;
        private const uint RPC_C_AUTHN_GSS_KERBEROS = 16;
        private const uint RPC_C_AUTHN_DPA = 17;
        private const uint RPC_C_AUTHN_MSN = 18;
        private const uint RPC_C_AUTHN_DIGEST = 21;
        private const uint RPC_C_AUTHN_MQ = 100;
        private const uint RPC_C_AUTHN_DEFAULT = 0xFFFFFFFF;

        private const uint RPC_C_AUTHZ_NONE = 0;
        private const uint RPC_C_AUTHZ_NAME = 1;
        private const uint RPC_C_AUTHZ_DCE = 2;
        private const uint RPC_C_AUTHZ_DEFAULT = 0xffffffff;

        private const uint RPC_C_AUTHN_LEVEL_DEFAULT = 0;
        private const uint RPC_C_AUTHN_LEVEL_NONE = 1;
        private const uint RPC_C_AUTHN_LEVEL_CONNECT = 2;
        private const uint RPC_C_AUTHN_LEVEL_CALL = 3;
        private const uint RPC_C_AUTHN_LEVEL_PKT = 4;
        private const uint RPC_C_AUTHN_LEVEL_PKT_INTEGRITY = 5;
        private const uint RPC_C_AUTHN_LEVEL_PKT_PRIVACY = 6;

        private const uint RPC_C_IMP_LEVEL_ANONYMOUS = 1;
        private const uint RPC_C_IMP_LEVEL_IDENTIFY = 2;
        private const uint RPC_C_IMP_LEVEL_IMPERSONATE = 3;
        private const uint RPC_C_IMP_LEVEL_DELEGATE = 4;

        private const uint EOAC_NONE = 0x00;
        private const uint EOAC_MUTUAL_AUTH = 0x01;
        private const uint EOAC_CLOAKING = 0x10;
        private const uint EOAC_STATIC_CLOAKING = 0x20;
        private const uint EOAC_DYNAMIC_CLOAKING = 0x40;
        private const uint EOAC_SECURE_REFS = 0x02;
        private const uint EOAC_ACCESS_CONTROL = 0x04;
        private const uint EOAC_APPID = 0x08;

        [DllImport("ole32.dll")]
        private static extern int CoInitializeSecurity(
            IntPtr pSecDesc,
            int cAuthSvc,
            SOLE_AUTHENTICATION_SERVICE[] asAuthSvc,
            IntPtr pReserved1,
            uint dwAuthnLevel,
            uint dwImpLevel,
            IntPtr pAuthList,
            uint dwCapabilities,
            IntPtr pReserved3);

        [DllImport("ole32.dll")]
        private static extern int CoQueryProxyBlanket(
            [MarshalAs(UnmanagedType.IUnknown)]
			object pProxy,
            ref uint pAuthnSvc,
            ref uint pAuthzSvc,
            [MarshalAs(UnmanagedType.LPWStr)]
			ref string pServerPrincName,
            ref uint pAuthnLevel,
            ref uint pImpLevel,
            ref IntPtr pAuthInfo,
            ref uint pCapabilities);

        [DllImport("ole32.dll")]
        private static extern int CoSetProxyBlanket(
            [MarshalAs(UnmanagedType.IUnknown)]
			object pProxy,
            uint pAuthnSvc,
            uint pAuthzSvc,
            IntPtr pServerPrincName,
            uint pAuthnLevel,
            uint pImpLevel,
            IntPtr pAuthInfo,
            uint pCapabilities);

        private static readonly IntPtr COLE_DEFAULT_PRINCIPAL = new IntPtr(-1);
        private static readonly IntPtr COLE_DEFAULT_AUTHINFO = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct COSERVERINFO
        {
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pwszName;
            public IntPtr pAuthInfo;
            public uint dwReserved2;
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct COAUTHINFO
        {
            public uint dwAuthnSvc;
            public uint dwAuthzSvc;
            public IntPtr pwszServerPrincName;
            public uint dwAuthnLevel;
            public uint dwImpersonationLevel;
            public IntPtr pAuthIdentityData;
            public uint dwCapabilities;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct COAUTHIDENTITY
        {
            public IntPtr User;
            public uint UserLength;
            public IntPtr Domain;
            public uint DomainLength;
            public IntPtr Password;
            public uint PasswordLength;
            public uint Flags;
        }

        private const uint SEC_WINNT_AUTH_IDENTITY_ANSI = 0x1;
        private const uint SEC_WINNT_AUTH_IDENTITY_UNICODE = 0x2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct LICINFO
        {
            public int cbLicInfo;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fRuntimeKeyAvail;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fLicVerified;
        }

        [ComImport]
        [GuidAttribute("00000001-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IClassFactory
        {
            void CreateInstance(
                [MarshalAs(UnmanagedType.IUnknown)]
				object punkOuter,
                [MarshalAs(UnmanagedType.LPStruct)] 
				Guid riid,
                [MarshalAs(UnmanagedType.Interface)]
				[Out] out object ppvObject);

            void LockServer(
                [MarshalAs(UnmanagedType.Bool)]
				bool fLock);
        }

        [ComImport]
        [GuidAttribute("B196B28F-BAB4-101A-B69C-00AA00341D07")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IClassFactory2
        {
            void CreateInstance(
                [MarshalAs(UnmanagedType.IUnknown)]
				object punkOuter,
                [MarshalAs(UnmanagedType.LPStruct)] 
				Guid riid,
                [MarshalAs(UnmanagedType.Interface)]
				[Out] out object ppvObject);

            void LockServer(
                [MarshalAs(UnmanagedType.Bool)]
				bool fLock);

            void GetLicInfo(
                [In, Out] ref LICINFO pLicInfo);

            void RequestLicKey(
                int dwReserved,
                [MarshalAs(UnmanagedType.BStr)]
				string pbstrKey);

            void CreateInstanceLic(
                [MarshalAs(UnmanagedType.IUnknown)]
				object punkOuter,
                [MarshalAs(UnmanagedType.IUnknown)]
				object punkReserved,
                [MarshalAs(UnmanagedType.LPStruct)]  
				Guid riid,
                [MarshalAs(UnmanagedType.BStr)]
				string bstrKey,
                [MarshalAs(UnmanagedType.IUnknown)]
				[Out] out object ppvObject);
        }

        [DllImport("ole32.dll")]
        private static extern void CoGetClassObject(
            [MarshalAs(UnmanagedType.LPStruct)] 
			Guid clsid,
            uint dwClsContext,
            [In] ref COSERVERINFO pServerInfo,
            [MarshalAs(UnmanagedType.LPStruct)] 
			Guid riid,
            [MarshalAs(UnmanagedType.IUnknown)]
			[Out] out object ppv);

        private const int LOGON32_PROVIDER_DEFAULT = 0;
        private const int LOGON32_LOGON_INTERACTIVE = 2;
        private const int LOGON32_LOGON_NETWORK = 3;

        private const int SECURITY_ANONYMOUS = 0;
        private const int SECURITY_IDENTIFICATION = 1;
        private const int SECURITY_IMPERSONATION = 2;
        private const int SECURITY_DELEGATION = 3;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LogonUser(
            string lpszUsername,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            ref IntPtr phToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private extern static bool CloseHandle(IntPtr handle);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private extern static bool DuplicateToken(
            IntPtr ExistingTokenHandle,
            int SECURITY_IMPERSONATION_LEVEL,
            ref IntPtr DuplicateTokenHandle);
        #endregion
        #endregion
    }
}
