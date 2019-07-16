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
// 2004/11/11 RSA   Added a base interfaces for BrowsePosition.

using System;
using System.Xml;
using System.Net;
using System.Threading;
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Opc;
using Opc.Da;
using OpcCom.Da;
using OpcRcw.Da;
using OpcRcw.Comn;

namespace OpcCom.Da.Wrapper
{	
	/// <summary>
	/// A class that implements the COM-DA interfaces.
	/// </summary>
	[CLSCompliant(false)]
	public class Server : 
		ConnectionPointContainer,
		IOPCCommon,
		IOPCServer, 
		IOPCBrowseServerAddressSpace, 
		IOPCItemProperties,
		IOPCBrowse, 
		IOPCItemIO,
		IOPCWrappedServer
	{	
		/// <summary>
		/// Initializes the object with the default values.
		/// </summary>
		protected Server()
		{
			RegisterInterface(typeof(OpcRcw.Comn.IOPCShutdown).GUID);
		}

		/// <summary>
		/// The data access server object that is being wrapped and exposed via COM.
		/// </summary>
		public Opc.Da.IServer IServer
		{
			get { return m_server;  }
			set { m_server = value; }
		}

		/// <summary>
		/// Changes the name of an existing group.
		/// </summary>
		public int SetGroupName(string oldName, string newName)
		{
			lock (this)
			{
				// look up existing group.
				Group group = (Group)m_groups[oldName];

				if (newName == null || newName.Length == 0 || group == null)
				{
					return ResultIDs.E_INVALIDARG;
				}

				// check that new name is unique among all groups.
				if (m_groups.Contains(newName))
				{
					return ResultIDs.E_DUPLICATENAME;
				}

				// update group table.
				m_groups.Remove(oldName);
				m_groups[newName] = group;

				return ResultIDs.S_OK;
			}
		}

		/// <summary>
		/// Converts an exception to an exception that returns a COM error code.
		/// </summary>
		public static Exception CreateException(Exception e)
		{
			// nothing special required for external exceptions.
			if (typeof(ExternalException).IsInstanceOfType(e))
			{
				return e;
			}

			// convert result id exceptions to external exception.
			if (typeof(ResultIDException).IsInstanceOfType(e))
			{				
				return new ExternalException(e.Message,	OpcCom.Interop.GetResultID(((ResultIDException)e).Result));
			}

			// convert other exceptions to E_FAIL.
			return new ExternalException(e.Message, ResultIDs.E_FAIL);
		}

		/// <summary>
		/// Creates an exception from a COM error code.
		/// </summary>
		public static Exception CreateException(int code)
		{
			return new ExternalException(String.Format("0x{0:X8}", code), code);
		}

		/// <summary>
		/// Creates a new group.
		/// </summary>
		internal Group CreateGroup(ref SubscriptionState state, int lcid, int timebias)
		{
			lock (this)
			{
				// create subscription.
				ISubscription subscription = m_server.CreateSubscription(state);

				// get actual state.
				state = subscription.GetState();
				
				if (state == null)
				{
					throw Server.CreateException(ResultIDs.E_FAIL); 
				}

				// check for duplicate group name.
				if (m_groups.Contains(state.Name))
				{
					m_server.CancelSubscription(subscription);
					throw new ExternalException("E_DUPLICATENAME", ResultIDs.E_DUPLICATENAME);
				}

				Group newGroup = new Group(this, state.Name, ++m_nextHandle, lcid, timebias, subscription);

				// save group object.
				m_groups[state.Name] = newGroup;

				// return the new group.
				return newGroup;
			}
		}
		
		#region IOPCWrappedServer Members
		/// <summary>
		/// Called when the object is loaded by the COM wrapper process.
		/// </summary>
		public virtual void Load(Guid clsid)
		{
			// may be override by the subclass.
		}

		/// <summary>
		/// Called when the object is unloaded by the COM wrapper process.
		/// </summary>
		public virtual void Unload()
		{
			// may be override by the subclass.
		}
		#endregion

		#region IOPCCommon Members
		/// <remarks/>
		public void SetLocaleID(int dwLcid)
		{
			lock (this)
			{
				try
				{
					// set locale.
					m_server.SetLocale(OpcCom.Interop.GetLocale(dwLcid));

					// save actual numeric value passed in.
					m_lcid = dwLcid;
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void QueryAvailableLocaleIDs(out int pdwCount, out System.IntPtr pdwLcid)
		{
			lock (this)
			{
				try
				{
					pdwCount = 0;
					pdwLcid  = IntPtr.Zero;

					string[] locales = m_server.GetSupportedLocales();

					if (locales != null && locales.Length > 0)
					{
						pdwLcid = Marshal.AllocCoTaskMem(locales.Length*Marshal.SizeOf(typeof(int)));

						int[] lcids = new int[locales.Length];

						for (int ii = 0; ii < locales.Length; ii++)
						{
							lcids[ii] = OpcCom.Interop.GetLocale(locales[ii]);
						}

						Marshal.Copy(lcids, 0, pdwLcid, locales.Length);
						pdwCount = locales.Length;
					}				
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void GetLocaleID(out int pdwLcid)
		{			
			lock (this)
			{
				try
				{
					pdwLcid = m_lcid; 
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		void OpcRcw.Comn.IOPCCommon.GetErrorString(int dwError, out string ppString)
		{
			lock (this)
			{
				try
				{
					ppString = m_server.GetErrorText(m_server.GetLocale(), OpcCom.Interop.GetResultID(dwError));
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void SetClientName(string szName)
		{
			// do nothing.
		}
		#endregion

		#region IOPCServer Members
		/// <remarks/>
		public void GetGroupByName(string szName, ref Guid riid, out object ppUnk)
		{
			lock (this)
			{
				try
				{
					foreach (Group group in m_groups.Values)
					{
						if (group.Name == szName)
						{
							ppUnk = group;
							return;
						}
					}

					throw new ExternalException("E_INVALIDARG", ResultIDs.E_INVALIDARG);		
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void GetErrorString(int dwError, int dwLocale, out string ppString)
		{
			lock (this)
			{
				try
				{
					ppString = m_server.GetErrorText(OpcCom.Interop.GetLocale(dwLocale), OpcCom.Interop.GetResultID(dwError));
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void RemoveGroup(int hServerGroup, int bForce)
		{
			lock (this)
			{
				try
				{
					foreach (Group group in m_groups.Values)
					{
						if (group.ServerHandle == hServerGroup)
						{
							m_groups.Remove(group.Name);
							group.Dispose();
							return;
						}
					}

					throw new ExternalException("E_FAIL", ResultIDs.E_FAIL);
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void CreateGroupEnumerator(OpcRcw.Da.OPCENUMSCOPE dwScope, ref Guid riid, out object ppUnk)
		{
			lock (this)
			{
				try
				{
					switch (dwScope)
					{
						case OPCENUMSCOPE.OPC_ENUM_PUBLIC:
						case OPCENUMSCOPE.OPC_ENUM_PUBLIC_CONNECTIONS:
						{
							if (riid == typeof(OpcRcw.Comn.IEnumString).GUID)
							{
								ppUnk = new EnumString(null);
								return;
							}

							if (riid == typeof(OpcRcw.Comn.IEnumUnknown).GUID)
							{
								ppUnk = new EnumUnknown(null);
								return;
							}

							throw new ExternalException("E_NOINTERFACE", ResultIDs.E_NOINTERFACE);
						}
					}

					if (riid == typeof(IEnumUnknown).GUID)
					{
						ppUnk = new EnumUnknown(m_groups);
						return;
					}

					if (riid == typeof(OpcRcw.Comn.IEnumString).GUID)
					{
						ArrayList names = new ArrayList(m_groups.Count);

						foreach (Group group in m_groups.Values)
						{
							names.Add(group.Name);
						}

						ppUnk = new EnumString(names);
						return;
					}

					throw new ExternalException("E_NOINTERFACE", ResultIDs.E_NOINTERFACE);
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void AddGroup(
			string        szName, 
			int           bActive, 
			int           dwRequestedUpdateRate, 
			int           hClientGroup, 
			System.IntPtr pTimeBias, 
			System.IntPtr pPercentDeadband, 
			int           dwLCID, 
			out int       phServerGroup, 
			out int       pRevisedUpdateRate, 
			ref Guid      riid,
			out object    ppUnk)
		{
			lock (this)
			{
				try
				{
					// initialize state.
					SubscriptionState state = new SubscriptionState();

					state.Name         = szName;
					state.ServerHandle = null;
					state.ClientHandle = hClientGroup;
					state.Active       = (bActive != 0);
					state.Deadband     = 0;
					state.KeepAlive    = 0;
					state.Locale       = OpcCom.Interop.GetLocale(dwLCID);
					state.UpdateRate   = dwRequestedUpdateRate;

					if (pPercentDeadband != IntPtr.Zero)
					{
						float[] buffer = new float[1];
						Marshal.Copy(pPercentDeadband, buffer, 0, 1);
						state.Deadband = buffer[0];
					}

					// look up default time zone.
					DateTime now = DateTime.Now;

					int timebias = (int)-TimeZone.CurrentTimeZone.GetUtcOffset(now).TotalMinutes;

					if (TimeZone.CurrentTimeZone.IsDaylightSavingTime(now))
					{
						timebias += 60;
					}

					// use specifed time zone bias.
					if (pTimeBias != IntPtr.Zero)
					{
						timebias = Marshal.ReadInt32(pTimeBias);
					}

					// create a new group.
					Group group = CreateGroup(ref state, dwLCID, timebias);

					phServerGroup      = (int)group.ServerHandle;
					pRevisedUpdateRate = state.UpdateRate;
					ppUnk              = group;
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void GetStatus(out System.IntPtr ppServerStatus)
		{
			lock (this)
			{
				try
				{
					OpcRcw.Da.OPCSERVERSTATUS status = Interop.GetServerStatus(m_server.GetStatus(), m_groups.Count);
					ppServerStatus = Marshal.AllocCoTaskMem(Marshal.SizeOf(status.GetType()));
					Marshal.StructureToPtr(status, ppServerStatus, false);
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}
		#endregion

		#region IOPCBrowse Members
		/// <remarks/>
		public void Browse(
			string            szItemID, 
			ref System.IntPtr pszContinuationPoint, 
			int               dwMaxElementsReturned, 
			OPCBROWSEFILTER   dwBrowseFilter,
			string            szElementNameFilter, 
			string            szVendorFilter,
			int               bReturnAllProperties, 
			int               bReturnPropertyValues, 
			int               dwPropertyCount, 
			int[]             pdwPropertyIDs, 
			out int           pbMoreElements,
			out int           pdwCount, 
			out System.IntPtr ppBrowseElements)
		{
			lock (this)
			{
				try
				{
					// construct item id.
					ItemIdentifier itemID = new ItemIdentifier(szItemID);

					// construct browse filters.
					BrowseFilters filters = new BrowseFilters();

					filters.MaxElementsReturned  = dwMaxElementsReturned;
					filters.BrowseFilter         = Interop.GetBrowseFilter(dwBrowseFilter);
					filters.ElementNameFilter    = szElementNameFilter;
					filters.VendorFilter         = szVendorFilter;
					filters.ReturnAllProperties  = bReturnAllProperties != 0;
					filters.ReturnPropertyValues = bReturnPropertyValues != 0;
					filters.PropertyIDs          = Interop.GetPropertyIDs(pdwPropertyIDs);

					Opc.Da.BrowsePosition  position = null;
					Opc.Da.BrowseElement[] elements = null;

					// unmarhshal continuation point.
					string continuationPoint = null;

					if (pszContinuationPoint != IntPtr.Zero)
					{
						continuationPoint = Marshal.PtrToStringUni(pszContinuationPoint);
					}

					// begin new browse operation.
					if (continuationPoint == null || continuationPoint.Length == 0)
					{
						elements = m_server.Browse(itemID, filters, out position);
					}

					// continue existing browse operation.
					else
					{
						// find existing continuation point.
						ContinuationPoint cp = (ContinuationPoint)m_continuationPoints[continuationPoint];

						if (cp != null)
						{
							position = cp.Position;
							m_continuationPoints.Remove(continuationPoint);
						}
						
						// check for valid continuation point.
						if (position == null)
						{
							throw new ExternalException("E_INVALIDCONTINUATIONPOINT", ResultIDs.E_INVALIDCONTINUATIONPOINT);
						}

						// free continuation point.
						Marshal.FreeCoTaskMem(pszContinuationPoint);
						pszContinuationPoint = IntPtr.Zero;

						// update max elements returned.
						position.MaxElementsReturned = dwMaxElementsReturned;

						// fetch next set of elements.
						elements = m_server.BrowseNext(ref position);
					}

					// clear any expired continuation points.
					CleanupContinuationPoints();

					// create a new continuation point.
					if (position != null)
					{
						continuationPoint = Guid.NewGuid().ToString();
						m_continuationPoints[continuationPoint] = new ContinuationPoint(position);
						pszContinuationPoint = Marshal.StringToCoTaskMemUni(continuationPoint);
					}

					// return a valid the continution point.
					if (pszContinuationPoint == IntPtr.Zero)
					{
						pszContinuationPoint = Marshal.StringToCoTaskMemUni(String.Empty);
					}
			
					// marshal return arguments.
					pbMoreElements   = 0;
					pdwCount         = 0;
					ppBrowseElements = IntPtr.Zero;

					if (elements != null)
					{
						pdwCount         = elements.Length;
						ppBrowseElements = Interop.GetBrowseElements(elements, dwPropertyCount > 0);
					}
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void GetProperties(int dwItemCount, string[] pszItemIDs, int bReturnPropertyValues, int dwPropertyCount, int[] pdwPropertyIDs, out System.IntPtr ppItemProperties)
		{
			lock (this)
			{
				try
				{
					if (dwItemCount == 0 || pszItemIDs == null)
					{
						throw new ExternalException("E_INVALIDARG", ResultIDs.E_INVALIDARG);
					}

					ppItemProperties = IntPtr.Zero;

					// unmarshal item ids.
					ItemIdentifier[] itemIDs = new ItemIdentifier[dwItemCount];

					for (int ii = 0; ii < dwItemCount; ii++)
					{
						itemIDs[ii] = new ItemIdentifier(pszItemIDs[ii]);
					}

					// unmarshal property ids.
					PropertyID[] propertyIDs = null;
			
					if (dwPropertyCount > 0 && pdwPropertyIDs != null)
					{
						propertyIDs = Interop.GetPropertyIDs(pdwPropertyIDs);
					}

					// get properties.
					ItemPropertyCollection[] properties = m_server.GetProperties(itemIDs, propertyIDs, bReturnPropertyValues != 0);

					if (properties != null)
					{
						ppItemProperties = Interop.GetItemPropertyCollections(properties);
					}
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}
		#endregion

		#region IOPCBrowseServerAddressSpace Members
		/// <remarks/>
		public void GetItemID(string szItemDataID, out string szItemID)
		{
			lock (this)
			{
				try
				{
					if (szItemDataID == null || szItemDataID.Length == 0)
					{
						if (m_browseStack.Count == 0)
						{
							szItemID = "";
						}
						else
						{
							szItemID = ((ItemIdentifier)m_browseStack.Peek()).ItemName;
						}
					}

					else
					{
						if (IsItem(szItemDataID))
						{
							szItemID = szItemDataID;
						}
						else
						{
							BrowseElement element = FindChild(szItemDataID);

							if (element == null)
							{
								throw Server.CreateException(ResultIDs.E_INVALIDARG);
							}

							szItemID = element.ItemName;
						}
					}
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void BrowseAccessPaths(string szItemID, out IEnumString ppIEnumString)
		{
			lock (this)
			{
				try
				{
					// access paths not supported.
					throw new ExternalException("BrowseAccessPaths", ResultIDs.E_NOTIMPL);
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void QueryOrganization(out OpcRcw.Da.OPCNAMESPACETYPE pNameSpaceType)
		{
			lock (this)
			{
				try
				{
					// only hierarchial spaces supported.
					pNameSpaceType = OPCNAMESPACETYPE.OPC_NS_HIERARCHIAL;
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void ChangeBrowsePosition(OpcRcw.Da.OPCBROWSEDIRECTION dwBrowseDirection, string szString)
		{
			lock (this)
			{
				try
				{
					BrowseFilters filters = new BrowseFilters();

					filters.MaxElementsReturned  = 0;
					filters.BrowseFilter         = browseFilter.all;
					filters.ElementNameFilter    = null; 
					filters.VendorFilter         = null;
					filters.ReturnAllProperties  = false;
					filters.PropertyIDs          = null;
					filters.ReturnPropertyValues = false;				

					ItemIdentifier  itemID   = null;
					Opc.Da.BrowsePosition position = null;

					switch (dwBrowseDirection)
					{
						case OPCBROWSEDIRECTION.OPC_BROWSE_TO:
						{
							// move to root.
							if (szString == null || szString.Length == 0)
							{
								m_browseStack.Clear();
								break;
							}

							itemID = new ItemIdentifier(szString);

							// validate item id.
							BrowseElement[] children = null;

							try
							{
								children = m_server.Browse(itemID, filters, out position);		
							}
							catch (Exception)
							{
								throw Server.CreateException(ResultIDs.E_INVALIDARG);
							}

							// check that actually a branch.
							if (children == null || children.Length == 0)
							{
								throw Server.CreateException(ResultIDs.E_INVALIDARG);
							}

							// update stack.
							m_browseStack.Clear();

							// push null to indicate that browse did not start at root.
							m_browseStack.Push(null);
							m_browseStack.Push(itemID); 

							break;
						}

						case OPCBROWSEDIRECTION.OPC_BROWSE_DOWN:
						{
							// check for invalid name.
							if (szString == null || szString.Length == 0)
							{
								throw Server.CreateException(ResultIDs.E_INVALIDARG);
							}

							// find the specified child.
							BrowseElement element = FindChild(szString);

							if (element == null || !element.HasChildren)
							{
								throw Server.CreateException(ResultIDs.E_INVALIDARG);
							}

							// add child to stack.
							m_browseStack.Push(new ItemIdentifier(element.ItemName)); 

							break;
						}

						case OPCBROWSEDIRECTION.OPC_BROWSE_UP:
						{
							// can't move up from root.
							if (m_browseStack.Count == 0)
							{
								throw Server.CreateException(ResultIDs.E_FAIL);
							}

							itemID = (ItemIdentifier)m_browseStack.Pop();

							// check for browse up after a browse to.
							if (m_browseStack.Count > 0 && m_browseStack.Peek() == null)
							{
								BuildBrowseStack(itemID);
							}

							break;
						}
					}

					// dispose of the position object properly if returned.
					if (position != null)
					{
						position.Dispose();
						position = null;
					}
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}
		
		/// <remarks/>
		public void BrowseOPCItemIDs(OpcRcw.Da.OPCBROWSETYPE dwBrowseFilterType, string szFilterCriteria, short vtDataTypeFilter, int dwAccessRightsFilter, out IEnumString ppIEnumString)
		{
			lock (this)
			{
				try
				{
					// get current browse position.
					ItemIdentifier itemID = null;

					if (m_browseStack.Count > 0)
					{
						itemID = (ItemIdentifier)m_browseStack.Peek();
					}

					ArrayList hits = new ArrayList();

					// browse for items.
					Browse(itemID, dwBrowseFilterType, szFilterCriteria, vtDataTypeFilter, dwAccessRightsFilter, hits);
					
					// create enumerator.
					ppIEnumString = (IEnumString)new EnumString(hits);
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}
		#endregion

		#region IOPCItemProperties Members
		/// <remarks/>
		public void LookupItemIDs(string szItemID, int dwCount, int[] pdwPropertyIDs, out System.IntPtr ppszNewItemIDs, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				try
				{
					// validate arguments.
					if (szItemID == null || szItemID.Length == 0 || dwCount == 0 || pdwPropertyIDs == null)
					{
						throw Server.CreateException(ResultIDs.E_INVALIDARG);
					}

					// initialize query parameters.
					ItemIdentifier[] itemIDs = new ItemIdentifier[]	{ new ItemIdentifier(szItemID) };

					PropertyID[] propertyIDs = new PropertyID[pdwPropertyIDs.Length];

					for (int ii = 0; ii < propertyIDs.Length; ii++)
					{
						propertyIDs[ii] = OpcCom.Da.Interop.GetPropertyID(pdwPropertyIDs[ii]);
					}

					// fetch properties.
					ItemPropertyCollection[] results = m_server.GetProperties(itemIDs, propertyIDs, false);

					if (results == null || results.Length != 1)
					{
						throw Server.CreateException(ResultIDs.E_FAIL);
					}

					// check result.
					if (results[0].ResultID.Failed())
					{
						throw new ResultIDException(results[0].ResultID);
					}

					// marshal item ids.
					string[] propertyItemIDs = new string[results[0].Count];

					for (int ii = 0; ii < results[0].Count; ii++)
					{
						ItemProperty property = results[0][ii];

						// these properties are not allow to have item ids.
						if (property.ID.Code <= Property.EUINFO.Code)
						{
							property.ResultID = ResultID.Da.E_INVALID_PID;
						}

						if (property.ResultID.Succeeded())
						{
							propertyItemIDs[ii] = property.ItemName;
						}
					}

					ppszNewItemIDs = OpcCom.Interop.GetUnicodeStrings(propertyItemIDs);
					
					// marshal error codes.
					ppErrors = Interop.GetHRESULTs((IResult[])results[0].ToArray(typeof(IResult)));
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void QueryAvailableProperties(string szItemID, out int pdwCount, out System.IntPtr ppPropertyIDs, out System.IntPtr ppDescriptions, out System.IntPtr ppvtDataTypes)
		{			
			lock (this)
			{
				try
				{
					// validate arguments.
					if (szItemID == null || szItemID.Length == 0)
					{
						throw new ExternalException("QueryAvailableProperties", ResultIDs.E_INVALIDARG);
					}

					// initialize query parameters.
					ItemIdentifier[] itemIDs = new ItemIdentifier[]	{ new ItemIdentifier(szItemID) };

					// fetch properties.
					ItemPropertyCollection[] results = m_server.GetProperties(itemIDs, null, false);

					if (results == null || results.Length != 1)
					{
						throw new ExternalException("LookupItemIDs", ResultIDs.E_FAIL);
					}

					// check result.
					if (results[0].ResultID.Failed())
					{
						throw new ResultIDException(results[0].ResultID);
					}

					// build result lists.
					int[]    propertyIDs  = new int[results[0].Count];
					string[] descriptions = new string[results[0].Count];
					short[]  datatypes    = new short[results[0].Count];

					for (int ii = 0; ii < results[0].Count; ii++)
					{
						ItemProperty property = results[0][ii];

						if (property.ResultID.Succeeded())
						{
							propertyIDs[ii] = property.ID.Code;

							PropertyDescription description = PropertyDescription.Find(property.ID);

							if (description != null)
							{
								descriptions[ii] = description.Name;
								datatypes[ii]    = (short)OpcCom.Interop.GetType(description.Type);
							}
						}
					}

					// marshal results.
					pdwCount       = propertyIDs.Length;
					ppPropertyIDs  = OpcCom.Interop.GetInt32s(propertyIDs);
					ppDescriptions = OpcCom.Interop.GetUnicodeStrings(descriptions);
					ppvtDataTypes  = OpcCom.Interop.GetInt16s(datatypes);
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void GetItemProperties(string szItemID, int dwCount, int[] pdwPropertyIDs, out System.IntPtr ppvData, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				try
				{
					// validate arguments.
					if (dwCount == 0 || pdwPropertyIDs == null)
					{
						throw Server.CreateException(ResultIDs.E_INVALIDARG);
					}

					// validate item id.
					if (szItemID == null || szItemID.Length == 0)
					{
						throw Server.CreateException(ResultIDs.E_INVALIDITEMID);
					}

					// initialize query parameters.
					ItemIdentifier[] itemIDs = new ItemIdentifier[]	{ new ItemIdentifier(szItemID) };

					PropertyID[] propertyIDs = new PropertyID[pdwPropertyIDs.Length];

					for (int ii = 0; ii < propertyIDs.Length; ii++)
					{
						propertyIDs[ii] = OpcCom.Da.Interop.GetPropertyID(pdwPropertyIDs[ii]);
					}

					// fetch properties.
					ItemPropertyCollection[] results = m_server.GetProperties(itemIDs, propertyIDs, true);

					if (results == null || results.Length != 1)
					{
						throw Server.CreateException(ResultIDs.E_FAIL);
					}

					// check result.
					if (results[0].ResultID.Failed())
					{
						throw new ResultIDException(results[0].ResultID);
					}

					// marshal item ids.
					object[] values = new object[results[0].Count];

					for (int ii = 0; ii < results[0].Count; ii++)
					{
						ItemProperty property = results[0][ii];

						if (property.ResultID.Succeeded())
						{
							values[ii] = Interop.MarshalPropertyValue(property.ID, property.Value);
						}
					}

					// marshal values.
					ppvData = OpcCom.Interop.GetVARIANTs(values, false);
					
					// marshal error codes.
					ppErrors = Interop.GetHRESULTs((IResult[])results[0].ToArray(typeof(IResult)));
				}
				catch (Exception e)
				{
					throw CreateException(e);
				}
			}
		}
		#endregion

		#region IOPCItemIO Members
		/// <remarks/>
		public void WriteVQT(int dwCount, string[] pszItemIDs, OPCITEMVQT[] pItemVQT, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				// validate arguments.
				if (dwCount == 0 || pszItemIDs == null || pItemVQT == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{
					// compile set of item modifications.
					ItemValue[] items = new ItemValue[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new ItemValue(new ItemIdentifier(pszItemIDs[ii]));

						items[ii].Value              = pItemVQT[ii].vDataValue;
						items[ii].Quality            = new Quality(pItemVQT[ii].wQuality);
						items[ii].QualitySpecified   = pItemVQT[ii].bQualitySpecified != 0;
						items[ii].Timestamp          = OpcCom.Interop.GetFILETIME(Interop.Convert(pItemVQT[ii].ftTimeStamp));
						items[ii].TimestampSpecified = pItemVQT[ii].bTimeStampSpecified != 0;
					}

					// read items.
					IdentifiedResult[] results = m_server.Write(items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Read(int dwCount, string[] pszItemIDs, int[] pdwMaxAge, out System.IntPtr ppvValues, out System.IntPtr ppwQualities, out System.IntPtr ppftTimeStamps, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				// validate arguments.
				if (dwCount == 0 || pszItemIDs == null || pdwMaxAge == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item(new ItemIdentifier(pszItemIDs[ii]));

						items[ii].MaxAge = (pdwMaxAge[ii] < 0)?Int32.MaxValue:pdwMaxAge[ii];
						items[ii].MaxAgeSpecified = true;
					}

					// read items.
					ItemValueResult[] results = m_server.Read(items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					object[] values = new object[results.Length];
					short[] qualities = new short[results.Length];
					DateTime[] timestamps = new DateTime[results.Length];

					for (int ii = 0; ii < results.Length; ii++)
					{
						values[ii]     = results[ii].Value;
						qualities[ii]  = (results[ii].QualitySpecified)?results[ii].Quality.GetCode():(short)0;
						timestamps[ii] = (results[ii].TimestampSpecified)?results[ii].Timestamp:DateTime.MinValue;
					}

					// marshal results.
					ppvValues      = OpcCom.Interop.GetVARIANTs(values, false);
					ppwQualities   = OpcCom.Interop.GetInt16s(qualities);
					ppftTimeStamps = OpcCom.Interop.GetFILETIMEs(timestamps);
					ppErrors       = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion

		#region Private Members
		private Opc.Da.IServer m_server = null;
		private Hashtable m_groups = new Hashtable();
		private Hashtable m_continuationPoints = new Hashtable();
		private Stack m_browseStack = new Stack();
		private int m_lcid = OpcCom.Interop.LOCALE_SYSTEM_DEFAULT;
		private int m_nextHandle = 1;

		/// <summary>
		/// Removes all expired continuation points.
		/// </summary>
		private void CleanupContinuationPoints()
		{
			// build list of expired continuation points.
			ArrayList expiredPoints = new ArrayList();

			foreach (DictionaryEntry entry in m_continuationPoints)
			{
				try 
				{ 
					ContinuationPoint cp = entry.Value as ContinuationPoint;

					if (DateTime.UtcNow.Ticks - cp.Timestamp.Ticks > TimeSpan.TicksPerMinute*10)
					{
						expiredPoints.Add(entry.Key);
					}
				}
				catch
				{
					expiredPoints.Add(entry.Key);
				}
			}

			// released expired continuation points.
			foreach (string continuationPoint in expiredPoints)
			{
				ContinuationPoint cp = (ContinuationPoint)m_continuationPoints[continuationPoint];
				m_continuationPoints.Remove(continuationPoint);
				cp.Position.Dispose();
			}
		}

		/// <summary>
		/// Finds the item id at the current browse position corresponding the name.
		/// </summary>
		private bool IsItem(string name)
		{
			ItemIdentifier itemID = new ItemIdentifier(name);

			/*
			// get the current position.
			if (m_browseStack.Count > 0)
			{
				itemID = (ItemIdentifier)m_browseStack.Peek();
			}
			*/

			try
			{
				// fetch item properties.
				ItemPropertyCollection[] properties = m_server.GetProperties(
					new ItemIdentifier[] { itemID }, 
					new PropertyID[] { Property.DATATYPE }, 
					false);

				// check for invalid list.
				if (properties == null || properties.Length != 1)
				{
					return false;
				}

				// check for property error.
				if (properties[0].ResultID.Failed() || properties[0][0].ResultID.Failed())
				{
					return false;
				}

				// is a valid item,
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
		
		/// <summary>
		/// Finds the item id at the current browse position corresponding the name.
		/// </summary>
		private BrowseElement FindChild(string name)
		{
			ItemIdentifier itemID = null;

			// get the current position.
			if (m_browseStack.Count > 0)
			{
				itemID = (ItemIdentifier)m_browseStack.Peek();
			}

			BrowseElement[] children = null;

			// search for child by name.
			try
			{
				BrowseFilters filters = new BrowseFilters();

				filters.MaxElementsReturned  = 0;
				filters.BrowseFilter         = browseFilter.all;
				filters.ElementNameFilter    = name; 
				filters.VendorFilter         = null;
				filters.ReturnAllProperties  = false;
				filters.PropertyIDs          = null;
				filters.ReturnPropertyValues = false;		

				Opc.Da.BrowsePosition position = null;

				children = m_server.Browse(itemID, filters, out position);

				// dispose of the position object properly if returned.
				if (position != null)
				{
					position.Dispose();
					position = null;
				}
			}
			catch (Exception)
			{
				return null;
			}

			// name not found.
			if (children != null && children.Length > 0)
			{
				return children[0];
			}
			
			// no match found.
			return null;
		}
		
		/// <summary>
		/// Recursively rebuilds the browse stack.
		/// </summary>
		private void BuildBrowseStack(ItemIdentifier itemID)
		{
			m_browseStack.Clear();
			BuildBrowseStack(null, itemID);
		}

		/// <summary>
		/// Recursively rebuilds the browse stack.
		/// </summary>
		private bool BuildBrowseStack(ItemIdentifier itemID, ItemIdentifier targetID)
		{
			// fetch children of current node.
			BrowseFilters filters = new BrowseFilters();

			filters.MaxElementsReturned  = 0;
			filters.BrowseFilter         = browseFilter.all;
			filters.ElementNameFilter    = null; 
			filters.VendorFilter         = null;
			filters.ReturnAllProperties  = false;
			filters.PropertyIDs          = null;
			filters.ReturnPropertyValues = false;				

			BrowseElement[] children = null;
			Opc.Da.BrowsePosition position = null;

			try
			{
				children = m_server.Browse(itemID, filters, out position);
			}
			catch (Exception)
			{
				m_browseStack.Clear();
				return false;
			}

			// dispose of the position object properly if returned.
			if (position != null)
			{
				position.Dispose();
				position = null;
			}

			// target not found.
			if (children == null || children.Length == 0)
			{
				m_browseStack.Clear();
				return false;
			}

			foreach (BrowseElement child in children)
			{
				// check if target found.
				if (child.ItemName == targetID.ItemName)
				{
					return true;
				}

				// check if child an ancestor of the target.
				if (targetID.ItemName.StartsWith(child.ItemName))
				{
					ItemIdentifier childID = new ItemIdentifier(targetID.ItemName);
					m_browseStack.Push(childID);

					return BuildBrowseStack(childID, targetID);
				}
			}

			// target not found.
			return false;
		}

		/// <summary>
		/// Browses for children of the specified item.
		/// </summary>
		private void Browse(
			ItemIdentifier          itemID,
			OpcRcw.Da.OPCBROWSETYPE dwBrowseFilterType, 
			string                  szFilterCriteria, 
			short                   vtDataTypeFilter, 
			int                     dwAccessRightsFilter, 
			ArrayList               hits)
		{
			BrowseFilters filters = new BrowseFilters();

			filters.MaxElementsReturned  = 0;
			filters.BrowseFilter         = browseFilter.all;
			filters.ElementNameFilter    = (dwBrowseFilterType != OPCBROWSETYPE.OPC_FLAT)?szFilterCriteria:""; 
			filters.VendorFilter         = null;
			filters.ReturnAllProperties  = false;
			filters.PropertyIDs          = new PropertyID[] { Property.DATATYPE, Property.ACCESSRIGHTS };
			filters.ReturnPropertyValues = true;		

			BrowseElement[] children = null;

			try
			{
				Opc.Da.BrowsePosition position = null;

				children = m_server.Browse(itemID, filters, out position);
				
				// dispose of the position object properly if returned.
				if (position != null)
				{
					position.Dispose();
					position = null;
				}
			}
			catch
			{
				throw new ExternalException("BrowseOPCItemIDs", ResultIDs.E_FAIL);
			}

			foreach (BrowseElement child in children)
			{
				// apply flat filter.
				if (dwBrowseFilterType == OPCBROWSETYPE.OPC_FLAT)
				{ 
					if (child.HasChildren)
					{ 
						Browse(new ItemIdentifier(child.ItemName), dwBrowseFilterType, szFilterCriteria, vtDataTypeFilter, dwAccessRightsFilter, hits);
					}
				}

				// apply branch filter.
				else if (dwBrowseFilterType == OPCBROWSETYPE.OPC_BRANCH)
				{
					if (!child.HasChildren)
					{
						continue;
					}
				}
						
				// apply leaf filter.
				else if (dwBrowseFilterType == OPCBROWSETYPE.OPC_LEAF)
				{
					if (child.HasChildren)
					{
						continue;
					}
				}

				// apply property filters.
				if (child.IsItem)
				{
					// apply datatype filter.
					if (vtDataTypeFilter != 0)
					{
						short datatype = (short)OpcCom.Interop.GetType((System.Type)child.Properties[0].Value);

						if (datatype != vtDataTypeFilter)
						{
							continue;
						}
					}

					// apply access right filter. 
					if (dwAccessRightsFilter != 0) 
					{ 
						accessRights rights = (accessRights)child.Properties[1].Value; 

						if (dwAccessRightsFilter == 0x1 && rights == accessRights.writable) 
						{ 
							continue; 
						} 

						if (dwAccessRightsFilter == 0x2 && rights == accessRights.readable) 
						{ 
							continue; 
						} 
					} 
				}

				// add item to hit list.
				if (dwBrowseFilterType != OPCBROWSETYPE.OPC_FLAT)
				{
					hits.Add(child.Name);
				}
				else if (child.IsItem)
				{
					if (szFilterCriteria.Length == 0 || Opc.Convert.Match(child.ItemName, szFilterCriteria, true))
					{
						hits.Add(child.ItemName);
					}
				}
			}
		}
		#endregion

		#region ContinuationPoint Class
		/// <summary>
		/// Stores information about a continuation point.
		/// </summary>
		private class ContinuationPoint
		{
			public DateTime Timestamp;
			public Opc.Da.BrowsePosition Position;

			public ContinuationPoint(Opc.Da.BrowsePosition position)
			{
				Timestamp = DateTime.UtcNow;
				Position  = position;
			}
		}
		#endregion
	}

	/// <summary>
	/// A class that implements the COM-DA interfaces.
	/// </summary>
	[CLSCompliant(false)]
	public class Group : 
		ConnectionPointContainer,
		IDisposable,
		IOPCItemMgt,
		IOPCSyncIO,
		IOPCSyncIO2,
		IOPCAsyncIO2,
		IOPCAsyncIO3,
		IOPCGroupStateMgt,
		IOPCGroupStateMgt2,
		IOPCItemDeadbandMgt,
		IOPCItemSamplingMgt
	{	
		/// <summary>
		/// Initializes the object with the default values.
		/// </summary>
		public Group(
			Server               server,
			string               name, 
			int                  handle,
			int                  lcid,
			int                  timebias,
			Opc.Da.ISubscription subscription)
		{
			RegisterInterface(typeof(OpcRcw.Da.IOPCDataCallback).GUID);

			m_server       = server;
			m_name         = name;
			m_serverHandle = handle;
			m_lcid         = lcid;
			m_timebias     = timebias;
			m_subscription = subscription;
		}

		/// <summary>
		/// The unique name for the group.
		/// </summary>
		public string Name
		{
			get {lock (this) { return m_name; }}
		}

		/// <summary>
		/// The unique server assigned handle for the group.
		/// </summary>
		public int ServerHandle
		{
			get {lock (this) { return m_serverHandle; }}
		}

		/// <summary>
		/// Called when a IConnectionPoint.Advise is called.
		/// </summary>
		public override void OnAdvise(Guid riid)
		{
			lock (this)
			{
				m_dataChanged = new DataChangedEventHandler(OnDataChanged);
				m_subscription.DataChanged += m_dataChanged;
			}
		}

		/// <summary>
		/// Called when a IConnectionPoint.Unadvise is called.
		/// </summary>
		public override void OnUnadvise(Guid riid)
		{
			lock (this)
			{
				if (m_dataChanged != null)
				{
					m_subscription.DataChanged -= m_dataChanged;
					m_dataChanged = null;
				}
			}
		}
        
        #region IDisposable Members
        /// <summary>
        /// The finalizer.
        /// </summary>
        ~Group()
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

                        if (m_subscription != null)
                        {
                            m_subscription.DataChanged -= m_dataChanged;
                            m_server.IServer.CancelSubscription(m_subscription);
                            m_subscription = null;
                        }
                    }

                    // Free your own state (unmanaged objects).
                    // Set large fields to null.
                }

                m_disposed = true;
            }
        }

        private bool m_disposed = false;
		#endregion

		#region IOPCItemMgt Members
		/// <remarks/>
		public void SetActiveState(int dwCount, int[] phServer, int bActive, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item((ItemIdentifier)m_items[phServer[ii]]);
						
						items[ii].Active = (bActive != 0);
						items[ii].ActiveSpecified = true;
					}

					// modify items.
					ItemResult[] results = m_subscription.ModifyItems((int)StateMask.Active, items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// update cached item objects.
					for (int ii = 0; ii < dwCount; ii++)
					{
						if (results[ii].ResultID.Succeeded())
						{
							m_items[phServer[ii]] = results[ii];
						}
					}

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void AddItems(int dwCount, OPCITEMDEF[] pItemArray, out System.IntPtr ppAddResults, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || pItemArray == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item();

						items[ii].ItemName        = pItemArray[ii].szItemID;
						items[ii].ItemPath        = pItemArray[ii].szAccessPath;
						items[ii].ClientHandle    = pItemArray[ii].hClient;
						items[ii].ServerHandle    = null;
						items[ii].Active          = pItemArray[ii].bActive != 0;
						items[ii].ActiveSpecified = true;
						items[ii].ReqType         = OpcCom.Interop.GetType((VarEnum)pItemArray[ii].vtRequestedDataType);
					}

					// add items.
					ItemResult[] results = m_subscription.AddItems(items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// fetch properties to return with results.
					ItemPropertyCollection[] properties = m_server.IServer.GetProperties(
						items, 
						new PropertyID[] { Property.DATATYPE, Property.ACCESSRIGHTS },
						true);
                    
					// masrhal item result structures.
					ppAddResults = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(OPCITEMRESULT))*results.Length);

					IntPtr pos = ppAddResults;

					for (int ii = 0; ii < results.Length; ii++)
					{
						OPCITEMRESULT result = new OPCITEMRESULT();

						result.hServer             = 0;
						result.dwBlobSize          = 0;
						result.pBlob               = IntPtr.Zero;
						result.vtCanonicalDataType = (short)VarEnum.VT_EMPTY;
						result.dwAccessRights      = 0;
						result.wReserved           = 0;

						if (results[ii].ResultID.Succeeded())
						{
							result.hServer             = ++m_nextHandle;
							result.vtCanonicalDataType = (short)Interop.MarshalPropertyValue(Property.DATATYPE, properties[ii][0].Value);
							result.dwAccessRights      = (int)Interop.MarshalPropertyValue(Property.ACCESSRIGHTS, properties[ii][1].Value);

							m_items[m_nextHandle] = results[ii];
						}
						
						Marshal.StructureToPtr(result, pos, false);
                        pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMRESULT)));
					}

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void SetClientHandles(int dwCount, int[] phServer, int[] phClient, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null || phClient == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item((ItemIdentifier)m_items[phServer[ii]]);
						items[ii].ClientHandle = phClient[ii];
					}

					// modify items.
					ItemResult[] results = m_subscription.ModifyItems((int)StateMask.ClientHandle, items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// update cached item objects.
					for (int ii = 0; ii < dwCount; ii++)
					{
						if (results[ii].ResultID.Succeeded())
						{
							m_items[phServer[ii]] = results[ii];
						}
					}

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void SetDatatypes(int dwCount, int[] phServer, short[] pRequestedDatatypes, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null || pRequestedDatatypes == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item((ItemIdentifier)m_items[phServer[ii]]);
						items[ii].ReqType = OpcCom.Interop.GetType((VarEnum)pRequestedDatatypes[ii]);
					}

					// modify items.
					ItemResult[] results = m_subscription.ModifyItems((int)StateMask.ReqType, items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// update cached item objects.
					for (int ii = 0; ii < dwCount; ii++)
					{
						if (results[ii].ResultID.Succeeded())
						{
							m_items[phServer[ii]] = results[ii];
						}
					}

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void ValidateItems(int dwCount, OPCITEMDEF[] pItemArray, int bBlobUpdate, out System.IntPtr ppValidationResults, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || pItemArray == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item();

						items[ii].ItemName        = pItemArray[ii].szItemID;
						items[ii].ItemPath        = pItemArray[ii].szAccessPath;
						items[ii].ClientHandle    = pItemArray[ii].hClient;
						items[ii].ServerHandle    = null;
						items[ii].Active          = false;
						items[ii].ActiveSpecified = true;
						items[ii].ReqType         = OpcCom.Interop.GetType((VarEnum)pItemArray[ii].vtRequestedDataType);
					}

					// add items.
					ItemResult[] results = m_subscription.AddItems(items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// remove items immediately.
					m_subscription.RemoveItems(results);

					// fetch properties to return with results.
					ItemPropertyCollection[] properties = m_server.IServer.GetProperties(
						items, 
						new PropertyID[] { Property.DATATYPE, Property.ACCESSRIGHTS },
						true);
                    
					// masrhal item result structures.
					ppValidationResults = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(OPCITEMRESULT))*results.Length);

					IntPtr pos = ppValidationResults;

					for (int ii = 0; ii < results.Length; ii++)
					{
						OPCITEMRESULT result = new OPCITEMRESULT();

						result.hServer             = 0;
						result.dwBlobSize          = 0;
						result.pBlob               = IntPtr.Zero;
						result.vtCanonicalDataType = (short)VarEnum.VT_EMPTY;
						result.dwAccessRights      = 0;
						result.wReserved           = 0;

						if (results[ii].ResultID.Succeeded())
						{
							result.vtCanonicalDataType = (short)Interop.MarshalPropertyValue(Property.DATATYPE, properties[ii][0].Value);
							result.dwAccessRights      = (int)Interop.MarshalPropertyValue(Property.ACCESSRIGHTS, properties[ii][1].Value);
						}
						
						Marshal.StructureToPtr(result, pos, false);
                        pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMRESULT)));
					}

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void CreateEnumerator(ref Guid riid, out object ppUnk)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				if (riid != typeof(OpcRcw.Da.IEnumOPCItemAttributes).GUID)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{			
					// fetch properties for items in group.
					int[]  serverHandles = new int[m_items.Count];
					Item[] items         = new Item[m_items.Count];

					int index = 0;

					IDictionaryEnumerator enumerator = m_items.GetEnumerator();

					while (enumerator.MoveNext())
					{
						serverHandles[index] = (int)enumerator.Key;
						items[index]         = (Item)enumerator.Value;
						index++;
					}

					PropertyID[] propertyIDs = new PropertyID[]
					{ 
						Property.ACCESSRIGHTS, 
						Property.DATATYPE, 
						Property.EUTYPE,
						Property.EUINFO,
						Property.HIGHEU,
						Property.LOWEU
					};

					ItemPropertyCollection[] properties = m_server.IServer.GetProperties(items,	propertyIDs, true);

					// build list of item attributes
					EnumOPCItemAttributes.ItemAttributes[] attributes = new EnumOPCItemAttributes.ItemAttributes[m_items.Count];

					for (int ii = 0; ii < items.Length; ii++)
					{
						attributes[ii] = new OpcCom.Da.Wrapper.EnumOPCItemAttributes.ItemAttributes();

						attributes[ii].ItemID            = items[ii].ItemName;
						attributes[ii].AccessPath        = items[ii].ItemPath;
						attributes[ii].ClientHandle      = (int)items[ii].ClientHandle;
						attributes[ii].ServerHandle      = serverHandles[ii];
						attributes[ii].Active            = items[ii].Active;
						attributes[ii].RequestedDataType = items[ii].ReqType;
						attributes[ii].AccessRights      = (accessRights)properties[ii][0].Value;
						attributes[ii].CanonicalDataType = (System.Type)properties[ii][1].Value;
						attributes[ii].EuType            = (euType)properties[ii][2].Value;
						attributes[ii].EuInfo            = (string[])properties[ii][3].Value;

						if (attributes[ii].EuType == euType.analog)
						{
							attributes[ii].MaxValue = (double)properties[ii][4].Value;
							attributes[ii].MinValue = (double)properties[ii][5].Value;
						}
					}

					ppUnk = new EnumOPCItemAttributes(attributes);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void RemoveItems(int dwCount, int[] phServer, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				int[] handles = (int[])new ArrayList(m_items.Keys).ToArray(typeof(int));
					
				// validate arguments.
				if (dwCount == 0 || phServer == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{
					// compile set of items to remove.
					ItemIdentifier[] itemIDs = new ItemIdentifier[dwCount];

					for (int ii = 0; ii < itemIDs.Length; ii++)
					{
						itemIDs[ii] = new ItemIdentifier((ItemIdentifier)m_items[phServer[ii]]);
					}

					// modify items.
					IdentifiedResult[] results = m_subscription.RemoveItems(itemIDs);

					if (results == null || results.Length != itemIDs.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// update cached item objects.
					for (int ii = 0; ii < dwCount; ii++)
					{
						if (results[ii].ResultID.Succeeded())
						{
							m_items.Remove(phServer[ii]);
						}
					}

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion
		
		#region IOPCSyncIO Members
		/// <remarks/>
		public void Read(OpcRcw.Da.OPCDATASOURCE dwSource, int dwCount, int[] phServer, out System.IntPtr ppItemValues, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item((ItemIdentifier)m_items[phServer[ii]]);

						items[ii].MaxAge = (dwSource == OPCDATASOURCE.OPC_DS_DEVICE)?0:Int32.MaxValue; 
						items[ii].MaxAgeSpecified = true;
					}

					// read items.
					ItemValueResult[] results = m_subscription.Read(items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// marshal values.
					ppItemValues = Interop.GetItemStates(results);

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Write(int dwCount, int[] phServer, object[] pItemValues, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null || pItemValues == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{
					// compile set of item modifications.
					ItemValue[] items = new ItemValue[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new ItemValue((ItemIdentifier)m_items[phServer[ii]]);

						items[ii].Value              = pItemValues[ii];
						items[ii].Quality            = Quality.Bad;
						items[ii].QualitySpecified   = false;
						items[ii].Timestamp          = DateTime.MinValue;
						items[ii].TimestampSpecified = false;
					}

					// read items.
					IdentifiedResult[] results = m_subscription.Write(items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion

		#region IOPCSyncIO2 Members
		/// <remarks/>
		public void ReadMaxAge(int dwCount, int[] phServer, int[] pdwMaxAge, out System.IntPtr ppvValues, out System.IntPtr ppwQualities, out System.IntPtr ppftTimeStamps, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null || pdwMaxAge == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item((ItemIdentifier)m_items[phServer[ii]]);

						items[ii].MaxAge = (pdwMaxAge[ii] < 0)?Int32.MaxValue:pdwMaxAge[ii];
						items[ii].MaxAgeSpecified = true;
					}

					// read items.
					ItemValueResult[] results = m_subscription.Read(items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					object[] values = new object[results.Length];
					short[] qualities = new short[results.Length];
					DateTime[] timestamps = new DateTime[results.Length];

					for (int ii = 0; ii < results.Length; ii++)
					{
						values[ii]     = results[ii].Value;
						qualities[ii]  = (results[ii].QualitySpecified)?results[ii].Quality.GetCode():(short)0;
						timestamps[ii] = (results[ii].TimestampSpecified)?results[ii].Timestamp:DateTime.MinValue;
					}

					// marshal results.
					ppvValues      = OpcCom.Interop.GetVARIANTs(values, false);
					ppwQualities   = OpcCom.Interop.GetInt16s(qualities);
					ppftTimeStamps = OpcCom.Interop.GetFILETIMEs(timestamps);
					ppErrors       = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		
		/// <remarks/>
		public void WriteVQT(int dwCount, int[] phServer, OPCITEMVQT[] pItemVQT, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null || pItemVQT == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{
					// compile set of item modifications.
					ItemValue[] items = new ItemValue[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new ItemValue((ItemIdentifier)m_items[phServer[ii]]);

						items[ii].Value              = pItemVQT[ii].vDataValue;
						items[ii].Quality            = new Quality(pItemVQT[ii].wQuality);
						items[ii].QualitySpecified   = pItemVQT[ii].bQualitySpecified != 0;
						items[ii].Timestamp          = OpcCom.Interop.GetFILETIME(Interop.Convert(pItemVQT[ii].ftTimeStamp));
						items[ii].TimestampSpecified = pItemVQT[ii].bTimeStampSpecified != 0;
					}

					// read items.
					IdentifiedResult[] results = m_subscription.Write(items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion

		#region IOPCAsyncIO2 Members
		/// <remarks/>
		public void Read(int dwCount, int[] phServer, int dwTransactionID, out int pdwCancelID, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				// check for callback.
				if (!IsConnected(typeof(IOPCDataCallback).GUID))
				{
					throw Server.CreateException(ResultIDs.CONNECT_E_NOCONNECTION);
				}

				try
				{
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item((ItemIdentifier)m_items[phServer[ii]]);

						items[ii].MaxAge = 0;
						items[ii].MaxAgeSpecified = true;
					}

					// assign unique transaction handle.
					pdwCancelID = AssignHandle();

					// begin read items.
					IRequest request = null;
					
					IdentifiedResult[] results = m_subscription.Read(items, pdwCancelID, new ReadCompleteEventHandler(OnReadComplete), out request);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// save transaction.
					if (request != null)
					{
						m_requests[request] = dwTransactionID;
					}

					// marshal results.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Write(int dwCount, int[] phServer, object[] pItemValues, int dwTransactionID, out int pdwCancelID, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null || pItemValues == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				// check for callback.
				if (!IsConnected(typeof(IOPCDataCallback).GUID))
				{
					throw Server.CreateException(ResultIDs.CONNECT_E_NOCONNECTION);
				}

				try
				{
					// compile set of item modifications.
					ItemValue[] items = new ItemValue[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new ItemValue((ItemIdentifier)m_items[phServer[ii]]);

						items[ii].Value              = pItemValues[ii];
						items[ii].Quality            = Quality.Bad;
						items[ii].QualitySpecified   = false;
						items[ii].Timestamp          = DateTime.MinValue;
						items[ii].TimestampSpecified = false;
					}

					// assign unique transaction handle.
					pdwCancelID = AssignHandle();

					// begin write items.
					IRequest request = null;
					
					IdentifiedResult[] results = m_subscription.Write(items, pdwCancelID, new WriteCompleteEventHandler(OnWriteComplete), out request);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// save transaction.
					if (request != null)
					{
						m_requests[request] = dwTransactionID;
					}

					// marshal results.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Cancel2(int dwCancelID)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);
				
				// check for callback.
				if (!IsConnected(typeof(IOPCDataCallback).GUID))
				{
					throw Server.CreateException(ResultIDs.CONNECT_E_NOCONNECTION);
				}

				try
				{
					// find transaction.
					IDictionaryEnumerator enumerator = m_requests.GetEnumerator();

					while (enumerator.MoveNext())
					{
						Opc.Da.Request request = (Opc.Da.Request)enumerator.Key;

						if (request.Handle.Equals(dwCancelID))
						{
							m_requests.Remove(request);
							return;
						}
					}

					// transaction does not exist.
					throw Server.CreateException(ResultIDs.E_FAIL);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Refresh2(OpcRcw.Da.OPCDATASOURCE dwSource, int dwTransactionID, out int pdwCancelID)
		{
			lock (this)
			{	
				// check for callback.
				if (!IsConnected(typeof(IOPCDataCallback).GUID))
				{
					throw Server.CreateException(ResultIDs.CONNECT_E_NOCONNECTION);
				}

				// calculate max age.
				int maxAge = (dwSource == OPCDATASOURCE.OPC_DS_DEVICE)?0:Int32.MaxValue;

				// call refresh.
				RefreshMaxAge(maxAge, dwTransactionID, out pdwCancelID);		
			}
		}

		/// <remarks/>
		public void SetEnable(int bEnable)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);
				
				// check for callback.
				if (!IsConnected(typeof(IOPCDataCallback).GUID))
				{
					throw Server.CreateException(ResultIDs.CONNECT_E_NOCONNECTION);
				}

				try
				{					
					m_subscription.SetEnabled(bEnable != 0);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void GetEnable(out int pbEnable)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);
				
				// check for callback.
				if (!IsConnected(typeof(IOPCDataCallback).GUID))
				{
					throw Server.CreateException(ResultIDs.CONNECT_E_NOCONNECTION);
				}

				try
				{					
					pbEnable = (m_subscription.GetEnabled())?1:0;
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion

		#region IOPCAsyncIO3 Members
		/// <remarks/>
		public void ReadMaxAge(int dwCount, int[] phServer, int[] pdwMaxAge, int dwTransactionID, out int pdwCancelID, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null || pdwMaxAge == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}
				
				// check for callback.
				if (!IsConnected(typeof(IOPCDataCallback).GUID))
				{
					throw Server.CreateException(ResultIDs.CONNECT_E_NOCONNECTION);
				}

				try
				{
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item((ItemIdentifier)m_items[phServer[ii]]);

						items[ii].MaxAge = (pdwMaxAge[ii] < 0)?Int32.MaxValue:pdwMaxAge[ii];
						items[ii].MaxAgeSpecified = true;
					}

					// assign unique transaction handle.
					pdwCancelID = AssignHandle();

					// begin read items.
					IRequest request = null;
					
					IdentifiedResult[] results = m_subscription.Read(items, pdwCancelID, new ReadCompleteEventHandler(OnReadComplete), out request);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// save transaction.
					if (request != null)
					{
						m_requests[request] = dwTransactionID;
					}

					// marshal results.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void WriteVQT(int dwCount, int[] phServer, OPCITEMVQT[] pItemVQT, int dwTransactionID, out int pdwCancelID, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null || pItemVQT == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}
				
				// check for callback.
				if (!IsConnected(typeof(IOPCDataCallback).GUID))
				{
					throw Server.CreateException(ResultIDs.CONNECT_E_NOCONNECTION);
				}

				try
				{
					// compile set of item modifications.
					ItemValue[] items = new ItemValue[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new ItemValue((ItemIdentifier)m_items[phServer[ii]]);

						items[ii].Value              = pItemVQT[ii].vDataValue;
						items[ii].Quality            = new Quality(pItemVQT[ii].wQuality);
						items[ii].QualitySpecified   = pItemVQT[ii].bQualitySpecified != 0;
						items[ii].Timestamp          = OpcCom.Interop.GetFILETIME(Interop.Convert(pItemVQT[ii].ftTimeStamp));
						items[ii].TimestampSpecified = pItemVQT[ii].bTimeStampSpecified != 0;
					}

					// assign unique transaction handle.
					pdwCancelID = AssignHandle();

					// begin write items.
					IRequest request = null;
					
					IdentifiedResult[] results = m_subscription.Write(items, pdwCancelID, new WriteCompleteEventHandler(OnWriteComplete), out request);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// save transaction.
					if (request != null)
					{
						m_requests[request] = dwTransactionID;
					}

					// marshal results.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void RefreshMaxAge(int dwMaxAge, int dwTransactionID, out int pdwCancelID)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);
				
				// check for callback.
				if (!IsConnected(typeof(IOPCDataCallback).GUID))
				{
					throw Server.CreateException(ResultIDs.CONNECT_E_NOCONNECTION);
				}

				try
				{
					// assign unique transaction handle.
					pdwCancelID = AssignHandle();

					// begin refresh.
					IRequest request = null;
					m_subscription.Refresh(pdwCancelID, out request);

					// save transaction.
					if (request != null)
					{
						m_requests[request] = dwTransactionID;
					}
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion

		#region IOPCGroupStateMgt Members
		/// <remarks/>
		public void GetState(out int pUpdateRate, out int pActive, out string ppName, out int pTimeBias, out float pPercentDeadband, out int pLCID, out int phClientGroup, out int phServerGroup)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				try
				{
					// fetch the current state.
					SubscriptionState state = m_subscription.GetState();
					
					if (state == null)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// return the results.
					pUpdateRate      = state.UpdateRate;
					pActive          = (state.Active)?1:0;
					ppName           = state.Name;
					pTimeBias        = m_timebias;
					pPercentDeadband = state.Deadband;
					pLCID            = m_lcid;
					phClientGroup    = m_clientHandle = (int)state.ClientHandle;
					phServerGroup    = m_serverHandle;

					// update locale copy.
					m_name = state.Name;
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void CloneGroup(string szName, ref Guid riid, out object ppUnk)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				Group group = null;

				try
				{
					// get current group state.
					SubscriptionState state = m_subscription.GetState();

					state.Name   = szName;
					state.Active = false;

					// create new group.
					group = m_server.CreateGroup(ref state, m_lcid, m_timebias);

					// add items.
					Item[] items = new Item[m_items.Count];

					int ii = 0;

					foreach (Item item in m_items.Values)
					{
						items[ii++] = item;
					}

					group.AddItems(items);

					// return new group
					ppUnk = group;
				}
				catch (Exception e)
				{
					// remove new group on error.
					if (group != null)
					{
						try   { m_server.RemoveGroup((int)group.ServerHandle, 0); }
						catch {}
					}

					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void SetName(string szName)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				try
				{
					// fetch the current state.
					SubscriptionState state = m_subscription.GetState();
					
					if (state == null)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// validate new name.
					int result = m_server.SetGroupName(state.Name, szName);
 
					if (result != ResultIDs.S_OK)
					{
						throw new ExternalException("SetName", result);
					}

					// update the current state.
					m_name = state.Name = szName;
					m_subscription.ModifyState((int)StateMask.Name, state);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void SetState(System.IntPtr pRequestedUpdateRate, out int pRevisedUpdateRate, System.IntPtr pActive, System.IntPtr pTimeBias, System.IntPtr pPercentDeadband, System.IntPtr pLCID, System.IntPtr phClientGroup)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				try
				{
					// fetch the current state.
					SubscriptionState state = new SubscriptionState();
					
					if (state == null)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					int masks = 0;

					// update rate.
					if (pRequestedUpdateRate != IntPtr.Zero)
					{
						state.UpdateRate = Marshal.ReadInt32(pRequestedUpdateRate);
						masks |= (int)StateMask.UpdateRate;
					}

					// active.
					if (pActive != IntPtr.Zero)
					{
						state.Active = (Marshal.ReadInt32(pActive) != 0);
						masks |= (int)StateMask.Active;
					}

					// time bias.
					if (pTimeBias != IntPtr.Zero)
					{
						m_timebias = Marshal.ReadInt32(pTimeBias);
					}

					// deadband.
					if (pPercentDeadband != IntPtr.Zero)
					{
						float[] buffer = new float[1];
						Marshal.Copy(pPercentDeadband, buffer, 0, 1);
						state.Deadband = buffer[0];
						masks |= (int)StateMask.Deadband;
					}

					// locale.
					if (pLCID != IntPtr.Zero)
					{
						m_lcid = Marshal.ReadInt32(pLCID);
						state.Locale = OpcCom.Interop.GetLocale(m_lcid);
						masks |= (int)StateMask.Locale;
					}

					// client handle.
					if (phClientGroup != IntPtr.Zero)
					{
						state.ClientHandle = m_clientHandle = Marshal.ReadInt32(phClientGroup);
						masks |= (int)StateMask.ClientHandle;
					}
			
					// update the current state.
					state = m_subscription.ModifyState(masks, state);

					// return the actual update rate.
					pRevisedUpdateRate = state.UpdateRate;
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion

		#region IOPCGroupStateMgt2 Members
		/// <remarks/>
		public void GetKeepAlive(out int pdwKeepAliveTime)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				try
				{
					// fetch the current state.
					SubscriptionState state = m_subscription.GetState();

					if (state == null)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// return the results.
					pdwKeepAliveTime = state.KeepAlive;
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void SetKeepAlive(int dwKeepAliveTime, out int pdwRevisedKeepAliveTime)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				try
				{
					// initialize a new state object.
					SubscriptionState state = new SubscriptionState();
					
					if (state == null)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					state.KeepAlive = dwKeepAliveTime;
			
					// update the current state.
					state = m_subscription.ModifyState((int)StateMask.KeepAlive, state);

					// return the actual keep alive rate.
					pdwRevisedKeepAliveTime = state.KeepAlive;
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion

		#region IOPCItemDeadbandMgt Members
		/// <remarks/>
		public void SetItemDeadband(int dwCount, int[] phServer, float[] pPercentDeadband, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null || pPercentDeadband == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{			
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item((ItemIdentifier)m_items[phServer[ii]]);

						items[ii].Deadband          = pPercentDeadband[ii];
						items[ii].DeadbandSpecified = true;
					}

					// modify items.
					ItemResult[] results = m_subscription.ModifyItems((int)StateMask.Deadband, items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// update cached item objects.
					for (int ii = 0; ii < dwCount; ii++)
					{
						if (results[ii].ResultID.Succeeded())
						{
							m_items[phServer[ii]] = results[ii];
						}
					}

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		} 

		/// <remarks/>
		public void GetItemDeadband(int dwCount, int[] phServer, out System.IntPtr ppPercentDeadband, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{			
					// read information from cached item objects.
					float[] deadbands = new float[dwCount];
					int[] errors      = new int[dwCount];

					for (int ii = 0; ii < dwCount; ii++)
					{
						ItemResult item = (ItemResult)m_items[phServer[ii]];

						errors[ii] = ResultIDs.E_INVALIDHANDLE;

						if (item != null && item.ResultID.Succeeded())
						{
							if (item.DeadbandSpecified)
							{
								deadbands[ii] = item.Deadband;
								errors[ii]    = ResultIDs.S_OK;
							}
							else
							{
								errors[ii] = ResultIDs.E_DEADBANDNOTSET;
							}
						}
					}

					// marshal deadbands.
					ppPercentDeadband = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(float))*dwCount);
					Marshal.Copy(deadbands, 0, ppPercentDeadband, dwCount);

					// marshal error codes.
					ppErrors = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int))*dwCount);
					Marshal.Copy(errors, 0, ppErrors, dwCount);

				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		} 

		/// <remarks/>
		public void ClearItemDeadband(int dwCount, int[] phServer, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{			
					// compile set of item modifications.
					ArrayList alreadyCleared = new ArrayList();

					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						// get existing item.
						Item item = (Item)m_items[phServer[ii]];

						items[ii] = new Item((ItemIdentifier)item);

						// check if deadband is currently specified.
						if (item != null)
						{
							if (item.DeadbandSpecified)
							{
								items[ii].Deadband          = 0;
								items[ii].DeadbandSpecified = false;
							}
							else
							{
								alreadyCleared.Add(ii);
							}
						}
					}

					// modify items.
					ItemResult[] results = m_subscription.ModifyItems((int)StateMask.Deadband, items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// set deadband error if deadband already cleared.
					foreach (int ii in alreadyCleared)
					{
						if (results[ii].ResultID.Succeeded())
						{
							results[ii].ResultID = new ResultID(ResultIDs.E_DEADBANDNOTSET);
						}
					}

					// update cached item objects.
					for (int ii = 0; ii < dwCount; ii++)
					{
						if (results[ii].ResultID.Succeeded())
						{
							m_items[phServer[ii]] = results[ii];
						}
					}

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		} 
		#endregion

		#region IOPCItemSamplingMgt Members
		/// <remarks/>
		public void SetItemSamplingRate(int dwCount, int[] phServer, int[] pdwRequestedSamplingRate, out System.IntPtr ppdwRevisedSamplingRate, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null || pdwRequestedSamplingRate == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{			
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item((ItemIdentifier)m_items[phServer[ii]]);

						items[ii].SamplingRate          = pdwRequestedSamplingRate[ii];
						items[ii].SamplingRateSpecified = true;
					}

					// modify items.
					ItemResult[] results = m_subscription.ModifyItems((int)StateMask.SamplingRate, items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					int[] samplingRates = new int[dwCount];

					// update cached item objects.
					for (int ii = 0; ii < dwCount; ii++)
					{
						if (results[ii].ResultID.Succeeded())
						{
							m_items[phServer[ii]] = results[ii];

							// set revised sampling rate.
							samplingRates[ii] = results[ii].SamplingRate;
						}
					}

					// marshal revised sampling rates.
					ppdwRevisedSamplingRate = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int))*dwCount);
					Marshal.Copy(samplingRates, 0, ppdwRevisedSamplingRate, dwCount);

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		} 

		/// <remarks/>
		public void GetItemSamplingRate(int dwCount, int[] phServer, out System.IntPtr ppdwSamplingRate, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{			
					// read information from cached item objects.
					int[] samplingRates = new int[dwCount];
					int[] errors        = new int[dwCount];

					for (int ii = 0; ii < dwCount; ii++)
					{
						ItemResult item = (ItemResult)m_items[phServer[ii]];

						errors[ii] = ResultIDs.E_INVALIDHANDLE;

						if (item != null && item.ResultID.Succeeded())
						{
							if (item.SamplingRateSpecified)
							{
								samplingRates[ii] = item.SamplingRate;
								errors[ii] = ResultIDs.S_OK;
							}
							else
							{
								errors[ii] = ResultIDs.E_RATENOTSET;
							}
						}
					}

					// marshal sampling rates.
					ppdwSamplingRate = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int))*dwCount);
					Marshal.Copy(samplingRates, 0, ppdwSamplingRate, dwCount);

					// marshal error codes.
					ppErrors = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int))*dwCount);
					Marshal.Copy(errors, 0, ppErrors, dwCount);

				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		} 

		/// <remarks/>
		public void ClearItemSamplingRate(int dwCount, int[] phServer, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{			
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item((ItemIdentifier)m_items[phServer[ii]]);

						items[ii].SamplingRate          = 0;
						items[ii].SamplingRateSpecified = false;
					}

					// modify items.
					ItemResult[] results = m_subscription.ModifyItems((int)StateMask.SamplingRate, items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// update cached item objects.
					for (int ii = 0; ii < dwCount; ii++)
					{
						if (results[ii].ResultID.Succeeded())
						{
							m_items[phServer[ii]] = results[ii];
						}
					}

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		} 

		/// <remarks/>
		public void SetItemBufferEnable(int dwCount, int[] phServer, int[] pbEnable, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null || pbEnable == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{			
					// compile set of item modifications.
					Item[] items = new Item[dwCount];

					for (int ii = 0; ii < items.Length; ii++)
					{
						items[ii] = new Item((ItemIdentifier)m_items[phServer[ii]]);

						items[ii].EnableBuffering          = (pbEnable[ii] != 0)?true:false;
						items[ii].EnableBufferingSpecified = (pbEnable[ii] != 0)?true:false;
					}

					// modify items.
					ItemResult[] results = m_subscription.ModifyItems((int)StateMask.EnableBuffering, items);

					if (results == null || results.Length != items.Length)
					{
						throw Server.CreateException(ResultIDs.E_FAIL); 
					}

					// update cached item objects.
					for (int ii = 0; ii < dwCount; ii++)
					{
						if (results[ii].ResultID.Succeeded())
						{
							m_items[phServer[ii]] = results[ii];
						}
					}

					// marshal error codes.
					ppErrors = Interop.GetHRESULTs(results);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		} 

		/// <remarks/>
		public void GetItemBufferEnable(int dwCount, int[] phServer, out System.IntPtr ppbEnable, out System.IntPtr ppErrors)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (dwCount == 0 || phServer == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				try
				{			
					// read information from cached item objects.
					int[] enableBuffering = new int[dwCount];
					int[] errors          = new int[dwCount];

					for (int ii = 0; ii < dwCount; ii++)
					{
						ItemResult item = (ItemResult)m_items[phServer[ii]];

						errors[ii] = ResultIDs.E_INVALIDHANDLE;

						if (item != null && item.ResultID.Succeeded())
						{
							enableBuffering[ii] = (item.EnableBuffering && item.EnableBufferingSpecified)?1:0;
							errors[ii]          = ResultIDs.S_OK;
						}
					}

					// marshal enable buffering flags.
					ppbEnable = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int))*dwCount);
					Marshal.Copy(enableBuffering, 0, ppbEnable, dwCount);

					// marshal error codes.
					ppErrors = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int))*dwCount);
					Marshal.Copy(errors, 0, ppErrors, dwCount);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		} 
		#endregion

		#region Private Members
		private Server m_server = null;
		private int m_serverHandle = 0;
		private int m_clientHandle = 0;
		private string m_name = null;
		private Opc.Da.ISubscription m_subscription = null;
		private int m_timebias = 0;
		private int m_lcid = LOCALE_SYSTEM_DEFAULT;
		private Hashtable m_items = new Hashtable();	
		private Hashtable m_requests = new Hashtable();	
		private int m_nextHandle = 1000;
		private DataChangedEventHandler m_dataChanged = null;

		private const int LOCALE_SYSTEM_DEFAULT = 0x800;

		/// <summary>
		/// Creates a unique handle for transactions.
		/// </summary>
		private int AssignHandle()
		{
			return ++m_nextHandle;
		}

		/// <summary>
		/// A delegate to receive asynchronous dta changed notifications.
		/// </summary>
		private void OnDataChanged(object subscriptionHandle, object requestHandle, ItemValueResult[] results)
		{
			InvokeCallback(requestHandle, results, true);
		}

		/// <summary>
		/// A delegate to receive asynchronous read completed notifications.
		/// </summary>
		private void OnReadComplete(object requestHandle, ItemValueResult[] results)
		{
			InvokeCallback(requestHandle, results, false);
		}

		/// <summary>
		/// A delegate to receive asynchronous read completed notifications.
		/// </summary>
		private void InvokeCallback(object requestHandle, ItemValueResult[] results, bool dataChanged)
		{
			try
			{
				object callback = null;
 
				// declare callback parameters.
				int                  transactionID = 0;
				int                  groupHandle   = 0;
				int                  masterError   = ResultIDs.S_OK;
				int                  masterQuality = ResultIDs.S_OK;
				int[]                clientHandles = null;
				object[]             values        = null;
				short[]              qualities     = null;
				OpcRcw.Da.FILETIME[] timestamps    = null;
				int[]                errors        = null;

				lock (this)
				{
					// find transaction.
					bool found = false;

					IDictionaryEnumerator enumerator = m_requests.GetEnumerator();

					while (enumerator.MoveNext())
					{
						Opc.Da.Request request = (Opc.Da.Request)enumerator.Key;

						if (request.Handle.Equals(requestHandle))
						{
							transactionID = (int)enumerator.Value;
							m_requests.Remove(request);
							found = true;
							break;
						}
					}

					// check if transaction is still valid (except for data changed updates).
					if (!dataChanged && !found)
					{
						return;
					}

					// get callback object.
					callback = GetCallback(typeof(IOPCDataCallback).GUID);

					if (callback == null)
					{
						return;
					}

					groupHandle = m_clientHandle;

					// marshal item results.
					if (results != null)
					{
						clientHandles = new int[results.Length];
						values        = new object[results.Length];
						qualities     = new short[results.Length];
						timestamps    = new OpcRcw.Da.FILETIME[results.Length];
						errors        = new int[results.Length];

						for (int ii = 0; ii < results.Length; ii++)
						{
							clientHandles[ii] = (int)results[ii].ClientHandle;		
							values[ii]        = results[ii].Value;
							qualities[ii]     = (results[ii].QualitySpecified)?results[ii].Quality.GetCode():(short)0;
							timestamps[ii]    = Interop.Convert(OpcCom.Interop.GetFILETIME(results[ii].Timestamp));
							errors[ii]        = OpcCom.Interop.GetResultID(results[ii].ResultID);

							// set master quality.
							if (results[ii].Quality.QualityBits != qualityBits.good)
							{
								masterQuality = ResultIDs.S_FALSE;
							}

							// set master error.
							if (results[ii].ResultID != ResultID.S_OK)
							{
								masterError = ResultIDs.S_FALSE;
							}
						}
					}
				}

				// invoke callback without maintaining a lock on the group object.
				if (dataChanged)
				{
					((IOPCDataCallback)callback).OnDataChange(
						transactionID,
						groupHandle,
						masterQuality,
						masterError,
						clientHandles.Length,
						clientHandles,
						values,
						qualities,
						timestamps,
						errors);
				}
				else
				{
					((IOPCDataCallback)callback).OnReadComplete(
						transactionID,
						groupHandle,
						masterQuality,
						masterError,
						clientHandles.Length,
						clientHandles,
						values,
						qualities,
						timestamps,
						errors);
				}
			}
			catch (Exception e)
			{
				string message = e.Message;
			}
		}
		
		/// <summary>
		/// A delegate to receive asynchronous write completed notifications.
		/// </summary>
		private void OnWriteComplete(object clientHandle, IdentifiedResult[] results)
		{
			try
			{
				object callback = null;
 
				// declare callback parameters.
				int   transactionID = -1;
				int   groupHandle   = -1;
				int   masterError   = ResultIDs.S_OK;
				int[] clientHandles = null;
				int[] errors        = null;

				lock (this)
				{
					// find transaction.
					bool found = false;

					IDictionaryEnumerator enumerator = m_requests.GetEnumerator();

					while (enumerator.MoveNext())
					{
						Opc.Da.Request request = (Opc.Da.Request)enumerator.Key;

						if (request.Handle.Equals(clientHandle))
						{
							transactionID = (int)enumerator.Value;
							m_requests.Remove(request);
							found = true;
							break;
						}
					}

					// check if transaction is still valid.
					if (!found)
					{
						return;
					}

					// get callback object.
					callback = GetCallback(typeof(IOPCDataCallback).GUID);

					if (callback == null)
					{
						return;
					}

					groupHandle = m_clientHandle;

					// marshal item results.
					if (results != null)
					{
						clientHandles = new int[results.Length];
						errors        = new int[results.Length];

						for (int ii = 0; ii < results.Length; ii++)
						{
							clientHandles[ii] = (int)results[ii].ClientHandle;		
							errors[ii]        = OpcCom.Interop.GetResultID(results[ii].ResultID);

							// set master error.
							if (results[ii].ResultID != ResultID.S_OK)
							{
								masterError = ResultIDs.S_FALSE;
							}
						}
					}
				}

				// invoke callback without maintaining a lock on the group object.
				((IOPCDataCallback)callback).OnWriteComplete(
					transactionID,
					groupHandle,
					masterError,
					clientHandles.Length,
					clientHandles,
					errors);
			}
			catch (Exception e)
			{
				string message = e.Message;
			}
		}

		/// <summary>
		/// Adds the items to group.
		/// </summary>
		private void AddItems(Item[] items)
		{
			lock (this)
			{
				if (m_subscription == null) throw Server.CreateException(ResultIDs.E_FAIL);

				// validate arguments.
				if (items == null)
				{
					throw Server.CreateException(ResultIDs.E_INVALIDARG);
				}

				// add items.
				ItemResult[] results = m_subscription.AddItems(items);

				if (results == null || results.Length != items.Length)
				{
					throw Server.CreateException(ResultIDs.E_FAIL); 
				}

				// save item results.
				for (int ii = 0; ii < results.Length; ii++)
				{
					if (results[ii].ResultID.Succeeded())
					{
						m_items[++m_nextHandle] = results[ii];
					}
				}
			}
		}
		#endregion
	}

	/// <summary>
	/// A class that implements the COM-DA interfaces.
	/// </summary>
	[CLSCompliant(false)]
	public class ConnectionPointContainer : IConnectionPointContainer
	{
		#region Public Members
		/// <summary>
		/// Called when a IConnectionPoint.Advise is called.
		/// </summary>
		public virtual void OnAdvise(Guid riid)
		{
			// does nothing.
		}

		/// <summary>
		/// Called when a IConnectionPoint.Unadvise is called.
		/// </summary>
		public virtual void OnUnadvise(Guid riid)
		{
			// does nothing.
		}
		#endregion

		#region Protected Members
		/// <summary>
		/// Initializes the object with default values.
		/// </summary>
		protected ConnectionPointContainer()
		{
			// does nothing.
		}

		/// <summary>
		/// Registers an interface as a connection point.
		/// </summary>
		protected void RegisterInterface(Guid iid)
		{
			m_connectionPoints[iid] = new ConnectionPoint(iid, this);
		}

		/// <summary>
		/// Unregisters an interface as a connection point.
		/// </summary>
		protected void UnregisterInterface(Guid iid)
		{
			m_connectionPoints.Remove(iid);
		}

		/// <summary>
		/// Returns the callback interface for the connection point (if currently connected).
		/// </summary>
		protected object GetCallback(Guid iid)
		{
			ConnectionPoint connectionPoint = (ConnectionPoint)m_connectionPoints[iid];

			if (connectionPoint != null)
			{
				return connectionPoint.Callback;
			}

			return null;
		}

		/// <summary>
		/// Whether a client has connected to the specified connection point.
		/// </summary>
		protected bool IsConnected(Guid iid)
		{
			ConnectionPoint connectionPoint = (ConnectionPoint)m_connectionPoints[iid];

			if (connectionPoint != null)
			{
				return connectionPoint.IsConnected;
			}

			return false;
		}
		#endregion

		#region IConnectionPointContainer Members
		/// <remarks/>
		public void EnumConnectionPoints(out IEnumConnectionPoints ppenum)
		{
			lock (this)
			{
				try
				{
					ppenum = new EnumConnectionPoints(m_connectionPoints.Values);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void FindConnectionPoint(ref Guid riid, out IConnectionPoint ppCP)
		{
			lock (this)
			{
				try
				{
					ppCP = null;

					ConnectionPoint connectionPoint = (ConnectionPoint)m_connectionPoints[riid];

					if (connectionPoint == null)
					{
						throw new ExternalException("CONNECT_E_NOCONNECTION", ResultIDs.CONNECT_E_NOCONNECTION);
					}

					ppCP = connectionPoint;
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion

		#region Private Members
		Hashtable m_connectionPoints = new Hashtable();
		#endregion
	}

	/// <summary>
	/// A class that implements the COM-DA interfaces.
	/// </summary>
	[CLSCompliant(false)]
	public class ConnectionPoint : IConnectionPoint
	{			
		/// <summary>
		/// Creates a connection point for the specified interface and container.
		/// </summary>
		public ConnectionPoint(Guid iid, ConnectionPointContainer container)
		{
			m_interface = iid;
			m_container = container;
		}

		/// <summary>
		/// The current callback object.
		/// </summary>
		public object Callback
		{
			get { return m_callback; }
		}

		/// <summary>
		/// Whether the client has connected to the connection point.
		/// </summary>
		public bool IsConnected
		{
			get { return m_callback != null; }
		}

		#region IConnectionPoint Members
		/// <remarks/>
		public void Advise(object pUnkSink, out int pdwCookie)
		{
			lock (this)
			{
				try
				{
					// invalid arguments.
					if (pUnkSink == null)
					{
						throw new ExternalException("E_POINTER", ResultIDs.E_POINTER);
					}

					pdwCookie = 0;

					// check if an callback already exists.
					if (m_callback != null)
					{
						throw new ExternalException("CONNECT_E_ADVISELIMIT", ResultIDs.CONNECT_E_ADVISELIMIT);
					}

					m_callback = pUnkSink;
					pdwCookie  = ++m_cookie;

					// notify container.
					m_container.OnAdvise(m_interface);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Unadvise(int dwCookie)
		{
			lock (this)
			{
				try
				{
					// not a valid connection id.
					if (m_cookie != dwCookie || m_callback == null)
					{
						throw new ExternalException("CONNECT_E_NOCONNECTION", ResultIDs.CONNECT_E_NOCONNECTION);
					}

					// clear the callback.
					m_callback = null;

					// notify container.
					m_container.OnUnadvise(m_interface);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void GetConnectionInterface(out Guid pIID)
		{
			lock (this)
			{
				try
				{
					pIID = m_interface;
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void EnumConnections(out IEnumConnections ppenum)
		{
			throw new ExternalException("E_NOTIMPL", ResultIDs.E_NOTIMPL);
		}

		/// <remarks/>
		public void GetConnectionPointContainer(out IConnectionPointContainer ppCPC)
		{
			lock (this)
			{
				try
				{
					ppCPC = m_container;
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion

		#region Private Members
		private Guid m_interface = Guid.Empty;
		private ConnectionPointContainer m_container = null;
		private object m_callback = null;
		private int m_cookie = 0;
		#endregion
	}
	
	/// <summary>
	/// A class that implements the COM-DA interfaces.
	/// </summary>
	[CLSCompliant(false)]
	public class EnumConnectionPoints : IEnumConnectionPoints
	{	
		/// <summary>
		/// Initializes the object with a set of connection points.
		/// </summary>
		internal EnumConnectionPoints(ICollection connectionPoints)
		{
			if (connectionPoints != null)
			{
				foreach (IConnectionPoint connectionPoint in connectionPoints)
				{
					m_connectionPoints.Add(connectionPoint);
				}
			}
		}

		#region IEnumConnectionPoints Members
		/// <remarks/>
		public void Skip(int cConnections)
		{
			lock (this)
			{
				try
				{
					m_index += cConnections;

					if (m_index > m_connectionPoints.Count)
					{
						m_index = m_connectionPoints.Count;
					}
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Clone(out IEnumConnectionPoints ppenum)
		{
			lock (this)
			{
				try
				{
					ppenum = new EnumConnectionPoints(m_connectionPoints);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Reset()
		{
			lock (this)
			{
				try
				{
					m_index = 0;
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
        public void RemoteNext(int cConnections, IntPtr ppCP, out int pcFetched)
		{
			lock (this)
			{
				try
                {
                    if (ppCP == IntPtr.Zero)
                    {
                        throw new ExternalException("E_INVALIDARG", ResultIDs.E_INVALIDARG);
                    }

                    IntPtr[] pCPs = new IntPtr[cConnections];

					pcFetched = 0;

					if (m_index >= m_connectionPoints.Count)
					{
						return;
					}

					for (int ii = 0; ii < m_connectionPoints.Count - m_index && ii < cConnections; ii++)
					{
                        IConnectionPoint cp = (IConnectionPoint)m_connectionPoints[m_index + ii];
                        pCPs[ii] = Marshal.GetComInterfaceForObject(cp, typeof(IConnectionPoint));
						pcFetched++;
					}

					m_index += pcFetched;

                    Marshal.Copy(pCPs, 0, ppCP, pcFetched);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion

		#region Private Members
		private ArrayList m_connectionPoints = new ArrayList();
		private int m_index = 0;
		#endregion
	}

	/// <summary>
	/// A class that implements the COM-DA interfaces.
	/// </summary>
	[CLSCompliant(false)]
	public class EnumOPCItemAttributes : IEnumOPCItemAttributes
	{	
		/// <remarks/>
		public class ItemAttributes
		{
			/// <remarks/>
			public string       ItemID            = null;
			/// <remarks/>
			public string       AccessPath        = null;
			/// <remarks/>
			public int          ClientHandle      = -1;
			/// <remarks/>
			public int          ServerHandle      = -1;
			/// <remarks/>
			public bool         Active            = false;
			/// <remarks/>
			public System.Type  CanonicalDataType = null;
			/// <remarks/>
			public System.Type  RequestedDataType = null;
			/// <remarks/>
			public accessRights AccessRights      = accessRights.readWritable;
			/// <remarks/>
			public euType       EuType            = euType.noEnum;
			/// <remarks/>
			public double       MaxValue          = 0;
			/// <remarks/>
			public double       MinValue          = 0;
			/// <remarks/>
			public string[]     EuInfo            = null;
		}

		/// <summary>
		/// Initializes the object with a set of connection points.
		/// </summary>
		internal EnumOPCItemAttributes(ICollection items)
		{
			if (items != null)
			{
				foreach (ItemAttributes item in items)
				{
					m_items.Add(item);
				}
			}
		}
		
		#region IEnumOPCItemAttributes Members
		/// <remarks/>
		public void Skip(int celt)
		{
			lock (this)
			{
				try
				{
					m_index += celt;

					if (m_index > m_items.Count)
					{
						m_index = m_items.Count;
					}
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Clone(out IEnumOPCItemAttributes ppEnumItemAttributes)
		{			
			lock (this)
			{
				try
				{
					ppEnumItemAttributes = new EnumOPCItemAttributes(m_items);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Reset()
		{
			lock (this)
			{
				try
				{
					m_index = 0;
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Next(int celt, out System.IntPtr ppItemArray, out int pceltFetched)
		{
			lock (this)
			{
				try
				{
					pceltFetched = 0;
					ppItemArray  = IntPtr.Zero;

					if (m_index >= m_items.Count)
					{
						return;
					}

					// determine how many items to return.
					pceltFetched = m_items.Count - m_index;

					if (pceltFetched > celt)
					{
						pceltFetched = celt;
					}

					// allocate return array.
					ppItemArray = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(OPCITEMATTRIBUTES))*pceltFetched);

					// marshal items to return.
					IntPtr pos = ppItemArray;

					for (int ii = 0; ii < pceltFetched; ii++)
					{
						ItemAttributes item = (ItemAttributes)m_items[m_index+ii];

						OPCITEMATTRIBUTES copy = new OPCITEMATTRIBUTES();

						copy.szItemID            = item.ItemID;
						copy.szAccessPath        = item.AccessPath;
						copy.hClient             = item.ClientHandle;
						copy.hServer             = item.ServerHandle;
						copy.bActive             = (item.Active)?1:0;
						copy.vtCanonicalDataType = (short)OpcCom.Interop.GetType(item.CanonicalDataType);
						copy.vtRequestedDataType = (short)OpcCom.Interop.GetType(item.RequestedDataType);
						copy.dwAccessRights      = (int)Interop.MarshalPropertyValue(Property.ACCESSRIGHTS, item.AccessRights);
						copy.dwBlobSize          = 0;
						copy.pBlob               = IntPtr.Zero;
						copy.dwEUType            = (OPCEUTYPE)Interop.MarshalPropertyValue(Property.EUTYPE, item.EuType);
						copy.vEUInfo             = null;

						switch (item.EuType)
						{
							case euType.analog:     { copy.vEUInfo = new double[] { item.MinValue, item.MaxValue }; break; }
							case euType.enumerated: { copy.vEUInfo = item.EuInfo;                                   break; }
						}				

						Marshal.StructureToPtr(copy, pos, false);
                        pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMATTRIBUTES)));
					}

					// update index.
					m_index += pceltFetched;
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion

		#region Private Members
		private ArrayList m_items = new ArrayList();
		private int m_index = 0;
		#endregion
	}	

	/// <summary>
	/// A class that implements the COM-DA interfaces.
	/// </summary>
	[CLSCompliant(false)]
	public class EnumUnknown : IEnumUnknown
	{	
		/// <summary>
		/// Initializes the object with a set of interface pointers.
		/// </summary>
		internal EnumUnknown(ICollection unknowns)
		{
			if (unknowns != null)
			{
				foreach (object unknown in unknowns)
				{
					m_unknowns.Add(unknown);
				}
			}
		}

		#region EnumUnknown Members
		/// <remarks/>
		public void Skip(int celt)
		{
			lock (this)
			{
				try
				{
					m_index += celt;

					if (m_index > m_unknowns.Count)
					{
						m_index = m_unknowns.Count;
					}
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Clone(out IEnumUnknown ppenum)
		{
			lock (this)
			{
				try
				{
					ppenum = new EnumUnknown(m_unknowns);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Reset()
		{
			lock (this)
			{
				try
				{
					m_index = 0;
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
        public void RemoteNext(int celt, IntPtr rgelt, out int pceltFetched)
		{
			lock (this)
			{
				try
				{
                    if (rgelt == IntPtr.Zero)
					{
						throw new ExternalException("E_INVALIDARG", ResultIDs.E_INVALIDARG);
					}

                    IntPtr[] pUnknowns = new IntPtr[celt];

					pceltFetched = 0;
					
					if (m_index >= m_unknowns.Count)
					{
						return;
					}

                    for (int ii = 0; ii < m_unknowns.Count - m_index && ii < pUnknowns.Length; ii++)
                    {
                        pUnknowns[ii] = Marshal.GetIUnknownForObject(m_unknowns[m_index+ii]);
						pceltFetched++;
					}

                    m_index += pceltFetched;

                    Marshal.Copy(pUnknowns, 0, rgelt, pceltFetched);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion
		
		#region Private Members
		private ArrayList m_unknowns = new ArrayList();
		private int m_index = 0;
		#endregion
	}

	/// <summary>
	/// A class that implements the COM-DA interfaces.
	/// </summary>
	[CLSCompliant(false)]
	public class EnumString : OpcRcw.Comn.IEnumString
	{	
		/// <summary>
		/// Initializes the object with a set of interface pointers.
		/// </summary>
		internal EnumString(ICollection strings)
		{
			if (strings != null)
			{
				foreach (object instance in strings)
				{
					m_strings.Add(instance);
				}
			}
		}

		#region EnumString Members
		/// <remarks/>
		public void Skip(int celt)
		{
			lock (this)
			{
				try
				{
					m_index += celt;

					if (m_index > m_strings.Count)
					{
						m_index = m_strings.Count;
					}
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Clone(out OpcRcw.Comn.IEnumString ppenum)
		{
			lock (this)
			{
				try
				{
					ppenum = new EnumString(m_strings);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void Reset()
		{
			lock (this)
			{
				try
				{
					m_index = 0;
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}

		/// <remarks/>
		public void RemoteNext(int celt, IntPtr rgelt, out int pceltFetched)
		{
			lock (this)
			{
				try
				{
                    if (rgelt == IntPtr.Zero)
					{
						throw new ExternalException("E_INVALIDARG", ResultIDs.E_INVALIDARG);
                    }

                    IntPtr[] pStrings = new IntPtr[celt];

					pceltFetched = 0;

					if (m_index >= m_strings.Count)
					{
						return;
					}

					for (int ii = 0; ii < m_strings.Count - m_index && ii < pStrings.Length; ii++)
					{
                        pStrings[ii] = Marshal.StringToCoTaskMemUni((string)m_strings[m_index + ii]);
						pceltFetched++;
					}

					m_index += pceltFetched;

                    Marshal.Copy(pStrings, 0, rgelt, pceltFetched);
				}
				catch (Exception e)
				{
					throw Server.CreateException(e);
				}
			}
		}
		#endregion
		
		#region Private Members
		private ArrayList m_strings = new ArrayList();
		private int m_index = 0;
		#endregion
	}
}
