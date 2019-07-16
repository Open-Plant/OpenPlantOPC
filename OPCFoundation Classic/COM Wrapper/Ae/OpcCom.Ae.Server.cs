//============================================================================
// TITLE: OpcCom.Ae.Server.cs
//
// CONTENTS:
// 
// A .NET wrapper for a COM server that implements the AE server interfaces.
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
// 2004/11/08 RSA   Initial implementation.

#pragma warning disable 0618

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using Opc;
using Opc.Ae;
using OpcRcw.Ae;
using OpcRcw.Comn;

namespace OpcCom.Ae
{
	/// <summary>
	/// A .NET wrapper for a COM server that implements the AE server interfaces.
	/// </summary>
	[Serializable]
	public class Server : OpcCom.Server, Opc.Ae.IServer
	{
		#region Constructors
		/// <summary>
		/// Initializes the object with the specified URL and COM server.
		/// </summary>
		public Server(URL url, object server)  : base(url, server)
		{
			m_supportsAE11 = true;

			// check if the V1.1 interfaces are supported.
			try
			{
				IOPCEventServer2 server2 = (IOPCEventServer2)server;
			}
			catch
			{
				m_supportsAE11 = false;
			}
		}
		#endregion

        #region IDisposable Members
        /// <summary>
        /// Releases unmanaged resources held by the object.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                lock (this)
                {
                    if (disposing)
                    {
                        // Release managed resources.

                        // release the server.
                        if (m_server != null)
                        {
                            // release all subscriptions.
                            foreach (Subscription subscription in m_subscriptions.Values)
                            {
                                // dispose of the subscription object (disconnects all subscriptions connections).
                                subscription.Dispose();
                            }

                            // clear subscription table.
                            m_subscriptions.Clear();
                        }
                    }

                    // Release unmanaged resources.
                    // Set large fields to null.

                    // release the browser.
                    if (m_browser != null)
                    {
                        OpcCom.Interop.ReleaseServer(m_browser);
                        m_browser = null;
                    }

                    // release the server.
                    if (m_server != null)
                    {
                        OpcCom.Interop.ReleaseServer(m_server);
                        m_server = null;
                    }
                }

                // Call Dispose on your base class.
                m_disposed = true;
            }

            base.Dispose(disposing);
        }

        private bool m_disposed = false;
        #endregion

		#region Opc.Ae.IServer Members
		//======================================================================
		// Get Status

		/// <summary>
		/// Returns the current server status.
		/// </summary>
		/// <returns>The current server status.</returns>
		public ServerStatus GetStatus()
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				IntPtr pStatus = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCEventServer)m_server).GetStatus(out pStatus);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventServer.GetStatus", e);
				}		
				
				// return results.
				return OpcCom.Ae.Interop.GetServerStatus(ref pStatus, true);
			}
		}

		//======================================================================
		// Event Subscription

		/// <summary>
		/// Creates a new event subscription.
		/// </summary>
		/// <param name="state">The initial state for the subscription.</param>
		/// <returns>The new subscription object.</returns>
		public ISubscription CreateSubscription(SubscriptionState state)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();
				if (state == null)    throw new ArgumentNullException("state");

				// initialize arguments.
				object unknown    = null;
				Guid   riid       = typeof(OpcRcw.Ae.IOPCEventSubscriptionMgt).GUID;
				int    bufferTime = 0;
				int    maxSize    = 0;

				// invoke COM method.
				try
				{
					((IOPCEventServer)m_server).CreateEventSubscription(
						(state.Active)?1:0,
						state.BufferTime,
						state.MaxSize,
						++m_handles,
						ref riid,
						out unknown,
						out bufferTime,
						out maxSize);						
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventServer.CreateEventSubscription", e);
				}		

				// save actual values.
				state.BufferTime = bufferTime;
				state.MaxSize    = maxSize;

				Subscription subscription = new OpcCom.Ae.Subscription(state, unknown);

				// set keep alive.
				subscription.ModifyState((int)StateMask.KeepAlive, state);
				
				// save subscription.
				m_subscriptions.Add(m_handles, subscription);

				// return results.
				return subscription;
			}
		}

		//======================================================================
		// QueryAvailableFilters

		/// <summary>
		/// Returns the event filters supported by the server.
		/// </summary>
		/// <returns>A bit mask of all event filters supported by the server.</returns>
		public int QueryAvailableFilters()
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				int filters = 0;

				// invoke COM method.
				try
				{
					((IOPCEventServer)m_server).QueryAvailableFilters(out filters);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventServer.QueryAvailableFilters", e);
				}		
				
				// return results.
				return filters;
			}
		}

		//======================================================================
		// QueryEventCategories

		/// <summary>		
		/// Returns the event categories supported by the server for the specified event types.
		/// </summary>
		/// <param name="eventType">A bit mask for the event types of interest.</param>
		/// <returns>A collection of event categories.</returns>
		public Category[] QueryEventCategories(int eventType)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				int count = 0;

				IntPtr ppdwEventCategories    = IntPtr.Zero;
				IntPtr ppszEventCategoryDescs = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCEventServer)m_server).QueryEventCategories(
						eventType, 
						out count,
						out ppdwEventCategories, 
						out ppszEventCategoryDescs);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventServer.QueryEventCategories", e);
				}		
				
				// check for empty list.
				if (count == 0)
				{
					return new Category[0];
				}

				// unmarshal arguments.
				int[]    ids   = OpcCom.Interop.GetInt32s(ref ppdwEventCategories, count, true);
				string[] names = OpcCom.Interop.GetUnicodeStrings(ref ppszEventCategoryDescs, count, true);

				// build results.
				Category[] categories = new Category[count];

				for (int ii = 0; ii < count; ii++)
				{
					categories[ii] = new Category();

					categories[ii].ID   = ids[ii];
					categories[ii].Name = names[ii];
				}

				// return results.
				return categories;
			}
		}

		//======================================================================
		// QueryConditionNames

		/// <summary>
		/// Returns the condition names supported by the server for the specified event categories.
		/// </summary>
		/// <param name="eventCategory">A bit mask for the event categories of interest.</param>
		/// <returns>A list of condition names.</returns>
		public string[] QueryConditionNames(int eventCategory)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				int count = 0;
				IntPtr ppszConditionNames = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCEventServer)m_server).QueryConditionNames(
						eventCategory, 
						out count,
						out ppszConditionNames);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventServer.QueryConditionNames", e);
				}		
				
				// check for empty list.
				if (count == 0)
				{
					return new string[0];
				}

				// unmarshal arguments.
				string[] names = OpcCom.Interop.GetUnicodeStrings(ref ppszConditionNames, count, true);

				// return results.
				return names;
			}
		}

		//======================================================================
		// QuerySubConditionNames

		/// <summary>
		/// Returns the sub-condition names supported by the server for the specified event condition.
		/// </summary>
		/// <param name="conditionName">The name of the condition.</param>
		/// <returns>A list of sub-condition names.</returns>
		public string[] QuerySubConditionNames(string conditionName)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				int count = 0;
				IntPtr ppszSubConditionNames = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCEventServer)m_server).QuerySubConditionNames(
						conditionName, 
						out count,
						out ppszSubConditionNames);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventServer.QuerySubConditionNames", e);
				}		

				// check for empty list.
				if (count == 0)
				{
					return new string[0];
				}
				
				// unmarshal arguments.
				string[] names = OpcCom.Interop.GetUnicodeStrings(ref ppszSubConditionNames, count, true);

				// return results.
				return names;
			}
		}

		//======================================================================
		// QuerySourceConditions

		/// <summary>
		/// Returns the condition names supported by the server for the specified event source.
		/// </summary>
		/// <param name="sourceName">The name of the event source.</param>
		/// <returns>A list of condition names.</returns>
		public string[] QueryConditionNames(string sourceName)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				int count = 0;
				IntPtr ppszConditionNames = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCEventServer)m_server).QuerySourceConditions(
						sourceName, 
						out count,
						out ppszConditionNames);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventServer.QuerySourceConditions", e);
				}		
					
				// check for empty list.
				if (count == 0)
				{
					return new string[0];
				}

				// unmarshal arguments.
				string[] names = OpcCom.Interop.GetUnicodeStrings(ref ppszConditionNames, count, true);

				// return results.
				return names;
			}
		}

		//======================================================================
		// QueryEventAttributes

		/// <summary>		
		/// Returns the event attributes supported by the server for the specified event categories.
		/// </summary>
		/// <param name="eventCategory">A bit mask for the event categories of interest.</param>
		/// <returns>A collection of event attributes.</returns>
		public Opc.Ae.Attribute[] QueryEventAttributes(int eventCategory)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				int count = 0;
				IntPtr ppdwAttrIDs = IntPtr.Zero;
				IntPtr ppszAttrDescs = IntPtr.Zero;
				IntPtr ppvtAttrTypes = IntPtr.Zero;

				// invoke COM method.
				try
				{
					((IOPCEventServer)m_server).QueryEventAttributes(
						eventCategory, 
						out count,
						out ppdwAttrIDs,
						out ppszAttrDescs,
						out ppvtAttrTypes);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventServer.QueryEventAttributes", e);
				}		
				
				// check for empty list.
				if (count == 0)
				{
					return new Opc.Ae.Attribute[0];
				}

				// unmarshal arguments.
				int[]    ids   = OpcCom.Interop.GetInt32s(ref ppdwAttrIDs, count, true);
				string[] names = OpcCom.Interop.GetUnicodeStrings(ref ppszAttrDescs, count, true);
				short[]  types = OpcCom.Interop.GetInt16s(ref ppvtAttrTypes, count, true);

				// build results.
				Opc.Ae.Attribute[] attributes = new Opc.Ae.Attribute[count];

				for (int ii = 0; ii < count; ii++)
				{
					attributes[ii] = new Opc.Ae.Attribute();

					attributes[ii].ID       = ids[ii];
					attributes[ii].Name     = names[ii];
					attributes[ii].DataType = OpcCom.Interop.GetType((VarEnum)types[ii]);
				}

				// return results.
				return attributes;
			}
		}

		//======================================================================
		// TranslateToItemIDs

		/// <summary>
		/// Returns the DA item ids for a set of attribute ids belonging to events which meet the specified filter criteria.
		/// </summary>
		/// <param name="sourceName">The event source of interest.</param>
		/// <param name="eventCategory">The id of the event category for the events of interest.</param>
		/// <param name="conditionName">The name of a condition within the event category.</param>
		/// <param name="subConditionName">The name of a sub-condition within a multi-state condition.</param>
		/// <param name="attributeIDs">The ids of the attributes to return item ids for.</param>
		/// <returns>A list of item urls for each specified attribute.</returns>
		public ItemUrl[] TranslateToItemIDs(
			string sourceName,
			int    eventCategory,
			string conditionName,
			string subConditionName,
			int[]  attributeIDs)
		{
				lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				IntPtr ppszAttrItemIDs = IntPtr.Zero;
				IntPtr ppszNodeNames = IntPtr.Zero;
				IntPtr ppCLSIDs = IntPtr.Zero;
					
				int count = (attributeIDs != null)?attributeIDs.Length:0;

				// call server.
				try
				{
					((IOPCEventServer)m_server).TranslateToItemIDs(
						(sourceName != null)?sourceName:"",
						eventCategory,
						(conditionName != null)?conditionName:"",
						(subConditionName != null)?subConditionName:"",
						count,
						(count > 0)?attributeIDs:new int[0],
						out ppszAttrItemIDs,
						out ppszNodeNames,
						out ppCLSIDs);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventServer.TranslateToItemIDs", e);
				}		
			
				// unmarshal results.
				string[] itemIDs   = OpcCom.Interop.GetUnicodeStrings(ref ppszAttrItemIDs, count, true);
				string[] nodeNames = OpcCom.Interop.GetUnicodeStrings(ref ppszNodeNames, count, true);
				Guid[]   clsids    = OpcCom.Interop.GetGUIDs(ref ppCLSIDs, count, true);
					
				ItemUrl[] itemUrls = new ItemUrl[count];
			
				// fill in item urls.
				for (int ii = 0; ii < count; ii++)
				{
					itemUrls[ii] = new ItemUrl();

					itemUrls[ii].ItemName = itemIDs[ii];
					itemUrls[ii].ItemPath = null;
					itemUrls[ii].Url.Scheme   = UrlScheme.DA;
					itemUrls[ii].Url.HostName = nodeNames[ii];
					itemUrls[ii].Url.Path     = String.Format("{{{0}}}", clsids[ii]);
				}

				// return results.
				return itemUrls;
			}
		}

		//======================================================================
		// GetConditionState

		/// <summary>
		/// Returns the current state information for the condition instance corresponding to the source and condition name.
		/// </summary>
		/// <param name="sourceName">The source name</param>
		/// <param name="conditionName">A condition name for the source.</param>
		/// <param name="attributeIDs">The list of attributes to return with the condition state.</param>
		/// <returns>The current state of the connection.</returns>
		public Condition GetConditionState(
			string sourceName,
			string conditionName,
			int[]  attributeIDs)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// initialize arguments.
				IntPtr ppConditionState = IntPtr.Zero;

				// call server.
				try
				{
					((IOPCEventServer)m_server).GetConditionState(
						(sourceName != null)?sourceName:"",
						(conditionName != null)?conditionName:"",
						(attributeIDs != null)?attributeIDs.Length:0,
						(attributeIDs != null)?attributeIDs:new int[0],
						out ppConditionState);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventServer.GetConditionState", e);
				}		
			
				// unmarshal results.
				Condition[] conditions = OpcCom.Ae.Interop.GetConditions(ref ppConditionState, 1, true);
			
				// fill in attribute ids.
				for (int ii = 0; ii < conditions[0].Attributes.Count; ii++)
				{
					conditions[0].Attributes[ii].ID = attributeIDs[ii];
				}

				// return results.
				return conditions[0];
			}
		}

		//======================================================================
		// EnableConditionByArea

		/// <summary>
		/// Places the specified process areas into the enabled state.
		/// </summary>
		/// <param name="areas">A list of fully qualified area names.</param>
		/// <returns>The results of the operation for each area.</returns>
		public ResultID[] EnableConditionByArea(string[] areas)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// check for trivial case.
				if (areas == null || areas.Length == 0)
				{
					return new ResultID[0];
				}

				// initialize arguments.
				IntPtr ppErrors = IntPtr.Zero;

				int[] errors = null;

				if (m_supportsAE11)
				{
					try
					{
						((IOPCEventServer2)m_server).EnableConditionByArea2(
							areas.Length, 
							areas,
							out ppErrors);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCEventServer2.EnableConditionByArea2", e);
					}		
				
					// unmarshal arguments.
					errors = OpcCom.Interop.GetInt32s(ref ppErrors, areas.Length, true);
				}
				else
				{
					try
					{
						((IOPCEventServer)m_server).EnableConditionByArea(
							areas.Length, 
							areas);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCEventServer.EnableConditionByArea", e);
					}	
	
					// create dummy error array (0 == S_OK).
					errors = new int[areas.Length];
				}
				
				// build results.
				ResultID[] results = new ResultID[errors.Length];

				for (int ii = 0; ii < errors.Length; ii++)
				{
					results[ii] = OpcCom.Ae.Interop.GetResultID(errors[ii]);
				}

				// return results.
				return results;
			}
		}
		
		//======================================================================
		// DisableConditionByArea

		/// <summary>
		/// Places the specified process areas into the disabled state.
		/// </summary>
		/// <param name="areas">A list of fully qualified area names.</param>
		/// <returns>The results of the operation for each area.</returns>
		public ResultID[] DisableConditionByArea(string[] areas)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// check for trivial case.
				if (areas == null || areas.Length == 0)
				{
					return new ResultID[0];
				}

				// initialize arguments.
				IntPtr ppErrors = IntPtr.Zero;

				int[] errors = null;

				if (m_supportsAE11)
				{
					try
					{
						((IOPCEventServer2)m_server).DisableConditionByArea2(
							areas.Length, 
							areas,
							out ppErrors);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCEventServer2.DisableConditionByArea2", e);
					}		
				
					// unmarshal arguments.
					errors = OpcCom.Interop.GetInt32s(ref ppErrors, areas.Length, true);
				}
				else
				{
					try
					{
						((IOPCEventServer)m_server).DisableConditionByArea(
							areas.Length, 
							areas);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCEventServer.DisableConditionByArea", e);
					}	
	
					// create dummy error array (0 == S_OK).
					errors = new int[areas.Length];
				}
				
				// build results.
				ResultID[] results = new ResultID[errors.Length];

				for (int ii = 0; ii < errors.Length; ii++)
				{
					results[ii] = OpcCom.Ae.Interop.GetResultID(errors[ii]);
				}

				// return results.
				return results;
			}
		}

		//======================================================================
		// EnableConditionBySource

		/// <summary>
		/// Places the specified process areas into the enabled state.
		/// </summary>
		/// <param name="sources">A list of fully qualified source names.</param>
		/// <returns>The results of the operation for each area.</returns>
		public ResultID[] EnableConditionBySource(string[] sources)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// check for trivial case.
				if (sources == null || sources.Length == 0)
				{
					return new ResultID[0];
				}

				// initialize arguments.
				IntPtr ppErrors = IntPtr.Zero;

				int[] errors = null;

				if (m_supportsAE11)
				{
					try
					{
						((IOPCEventServer2)m_server).EnableConditionBySource2(
							sources.Length, 
							sources,
							out ppErrors);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCEventServer2.EnableConditionBySource2", e);
					}		
				
					// unmarshal arguments.
					errors = OpcCom.Interop.GetInt32s(ref ppErrors, sources.Length, true);
				}
				else
				{
					try
					{
						((IOPCEventServer)m_server).EnableConditionBySource(
							sources.Length, 
							sources);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCEventServer.EnableConditionBySource", e);
					}	
	
					// create dummy error array (0 == S_OK).
					errors = new int[sources.Length];
				}
				
				// build results.
				ResultID[] results = new ResultID[errors.Length];

				for (int ii = 0; ii < errors.Length; ii++)
				{
					results[ii] = OpcCom.Ae.Interop.GetResultID(errors[ii]);
				}

				// return results.
				return results;
			}
		}

		//======================================================================
		// DisableConditionBySource

		/// <summary>
		/// Places the specified process areas into the disabled state.
		/// </summary>
		/// <param name="sources">A list of fully qualified source names.</param>
		/// <returns>The results of the operation for each area.</returns>
		public ResultID[] DisableConditionBySource(string[] sources)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// check for trivial case.
				if (sources == null || sources.Length == 0)
				{
					return new ResultID[0];
				}

				// initialize arguments.
				IntPtr ppErrors = IntPtr.Zero;

				int[] errors = null;

				if (m_supportsAE11)
				{
					try
					{
						((IOPCEventServer2)m_server).DisableConditionBySource2(
							sources.Length, 
							sources,
							out ppErrors);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCEventServer2.DisableConditionBySource2", e);
					}		
				
					// unmarshal arguments.
					errors = OpcCom.Interop.GetInt32s(ref ppErrors, sources.Length, true);
				}
				else
				{
					try
					{
						((IOPCEventServer)m_server).DisableConditionBySource(
							sources.Length, 
							sources);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCEventServer.DisableConditionBySource", e);
					}	
	
					// create dummy error array (0 == S_OK).
					errors = new int[sources.Length];
				}
				
				// build results.
				ResultID[] results = new ResultID[errors.Length];

				for (int ii = 0; ii < errors.Length; ii++)
				{
					results[ii] = OpcCom.Ae.Interop.GetResultID(errors[ii]);
				}

				// return results.
				return results;
			}
		}

		//======================================================================
		// GetEnableStateByArea

		/// <summary>
		/// Returns the enabled state for the specified process areas. 
		/// </summary>
		/// <param name="areas">A list of fully qualified area names.</param>
		public EnabledStateResult[] GetEnableStateByArea(string[] areas)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// check for trivial case.
				if (areas == null || areas.Length == 0)
				{
					return new EnabledStateResult[0];
				}

				// return error code if AE 1.1 not supported.
				if (!m_supportsAE11)
				{
					EnabledStateResult[] failures = new EnabledStateResult[areas.Length];

					for (int ii = 0; ii < failures.Length; ii++)
					{
						failures[ii] = new EnabledStateResult();

						failures[ii].Enabled            = false;
						failures[ii].EffectivelyEnabled = false;
						failures[ii].ResultID           = ResultID.E_FAIL;
					}

					return failures;
				}

				// initialize arguments.
				IntPtr pbEnabled            = IntPtr.Zero;
				IntPtr pbEffectivelyEnabled = IntPtr.Zero;
				IntPtr ppErrors             = IntPtr.Zero;

	            try
				{
					((IOPCEventServer2)m_server).GetEnableStateByArea(
						areas.Length, 
						areas,
						out pbEnabled,
						out pbEffectivelyEnabled,
						out ppErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventServer2.GetEnableStateByArea", e);
				}		
				
				// unmarshal arguments.
				int[] enabled             = OpcCom.Interop.GetInt32s(ref pbEnabled, areas.Length, true);
				int[] effectivelyEnabled  = OpcCom.Interop.GetInt32s(ref pbEffectivelyEnabled, areas.Length, true);
				int[] errors              = OpcCom.Interop.GetInt32s(ref ppErrors, areas.Length, true);

				
				// build results.
				EnabledStateResult[] results = new EnabledStateResult[errors.Length];

				for (int ii = 0; ii < errors.Length; ii++)
				{
					results[ii] = new EnabledStateResult();

					results[ii].Enabled            = enabled[ii] != 0;
					results[ii].EffectivelyEnabled = effectivelyEnabled[ii] != 0;
					results[ii].ResultID           = OpcCom.Ae.Interop.GetResultID(errors[ii]);
				}

				// return results
				return results;
			}
		}

		//======================================================================
		// GetEnableStateBySource

		/// <summary>
		/// Returns the enabled state for the specified event sources. 
		/// </summary>
		/// <param name="sources">A list of fully qualified source names.</param>
		public EnabledStateResult[] GetEnableStateBySource(string[] sources)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// check for trivial case.
				if (sources == null || sources.Length == 0)
				{
					return new EnabledStateResult[0];
				}

				// return error code if AE 1.1 not supported.
				if (!m_supportsAE11)
				{
					EnabledStateResult[] failures = new EnabledStateResult[sources.Length];

					for (int ii = 0; ii < failures.Length; ii++)
					{
						failures[ii] = new EnabledStateResult();

						failures[ii].Enabled            = false;
						failures[ii].EffectivelyEnabled = false;
						failures[ii].ResultID           = ResultID.E_FAIL;
					}

					return failures;
				}

				// initialize arguments.
				IntPtr pbEnabled            = IntPtr.Zero;
				IntPtr pbEffectivelyEnabled = IntPtr.Zero;
				IntPtr ppErrors             = IntPtr.Zero;

				try
				{
					((IOPCEventServer2)m_server).GetEnableStateBySource(
						sources.Length, 
						sources,
						out pbEnabled,
						out pbEffectivelyEnabled,
						out ppErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventServer2.GetEnableStateBySource", e);
				}		
					
				// unmarshal arguments.
				int[] enabled             = OpcCom.Interop.GetInt32s(ref pbEnabled, sources.Length, true);
				int[] effectivelyEnabled  = OpcCom.Interop.GetInt32s(ref pbEffectivelyEnabled, sources.Length, true);
				int[] errors              = OpcCom.Interop.GetInt32s(ref ppErrors, sources.Length, true);

					
				// build results.
				EnabledStateResult[] results = new EnabledStateResult[errors.Length];

				for (int ii = 0; ii < errors.Length; ii++)
				{
					results[ii] = new EnabledStateResult();

					results[ii].Enabled            = enabled[ii] != 0;
					results[ii].EffectivelyEnabled = effectivelyEnabled[ii] != 0;
					results[ii].ResultID           = OpcCom.Ae.Interop.GetResultID(errors[ii]);
				}

				// return results
				return results;
			}
		}

		//======================================================================
		// AcknowledgeCondition

		/// <summary>
		/// Used to acknowledge one or more conditions in the event server.
		/// </summary>
		/// <param name="acknowledgerID">The identifier for who is acknowledging the condition.</param>
		/// <param name="comment">A comment associated with the acknowledgment.</param>
		/// <param name="conditions">The conditions being acknowledged.</param>
		/// <returns>A list of result id indictaing whether each condition was successfully acknowledged.</returns>
		public ResultID[] AcknowledgeCondition(
			string                 acknowledgerID,
			string                 comment,
			EventAcknowledgement[] conditions)
		{				
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// check for trivial case.
				if (conditions == null || conditions.Length == 0)
				{
					return new ResultID[0];
				}

				// initialize arguments.
				int count = conditions.Length;

				string[]             pszSource        = new string[count];
				string[]             pszConditionName = new string[count];
				OpcRcw.Ae.FILETIME[] pftActiveTime    = new OpcRcw.Ae.FILETIME[count];
				int[]                pdwCookie        = new int[count];

				for (int ii = 0; ii < count; ii ++)
				{
					pszSource[ii]        = conditions[ii].SourceName;
					pszConditionName[ii] = conditions[ii].ConditionName;
					pftActiveTime[ii]    = OpcCom.Ae.Interop.Convert(OpcCom.Interop.GetFILETIME(conditions[ii].ActiveTime));
					pdwCookie[ii]        = conditions[ii].Cookie;
				}

				IntPtr ppErrors = IntPtr.Zero;

				// call server.
				try
				{
					((IOPCEventServer)m_server).AckCondition(
						conditions.Length,
						acknowledgerID,
						comment,
						pszSource,
						pszConditionName,
						pftActiveTime,
						pdwCookie,
						out ppErrors);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventServer.AckCondition", e);
				}		
				
				// unmarshal results.
				int[] errors = OpcCom.Interop.GetInt32s(ref ppErrors, count, true);
				
				// build results.
				ResultID[] results = new ResultID[count];

				for (int ii = 0; ii < count; ii++)
				{
					results[ii] = OpcCom.Ae.Interop.GetResultID(errors[ii]);
				}

				// return results.
				return results;
			}
		}

		//======================================================================
		// Browse

		/// <summary>
		/// Browses for all children of the specified area that meet the filter criteria.
		/// </summary>
		/// <param name="areaID">The full-qualified id for the area.</param>
		/// <param name="browseType">The type of children to return.</param>
		/// <param name="browseFilter">The expression used to filter the names of children returned.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		public BrowseElement[] Browse(
			string     areaID,
			BrowseType browseType, 
			string     browseFilter)
		{
			lock (this)
			{
				// intialize arguments.
				IBrowsePosition position = null;

				// browse for all elements at the current position.
				BrowseElement[] elements = Browse(areaID, browseType, browseFilter, 0, out position);

				// free position object.
				if (position != null)
				{
					position.Dispose();
				}

				// return results.
				return elements;
			}
		}

		/// <summary>
		/// Browses for all children of the specified area that meet the filter criteria.
		/// </summary>
		/// <param name="areaID">The full-qualified id for the area.</param>
		/// <param name="browseType">The type of children to return.</param>
		/// <param name="browseFilter">The expression used to filter the names of children returned.</param>
		/// <param name="maxElements">The maximum number of elements to return.</param>
		/// <param name="position">The object used to continue the browse if the number nodes exceeds the maximum specified.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		public BrowseElement[] Browse(
			string              areaID,
			BrowseType          browseType, 
			string              browseFilter, 
			int                 maxElements,
			out IBrowsePosition position)
		{
			position = null;

			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();

				// initialize browser.
				InitializeBrowser();

				// move to desired area.
				ChangeBrowsePosition(areaID);

				// create enumerator.
				UCOMIEnumString enumerator = (UCOMIEnumString)CreateEnumerator(browseType, browseFilter);

				// fetch elements.
				ArrayList elements = new ArrayList();

				int result = FetchElements(browseType, maxElements, enumerator, elements);
				
				// dispose of enumerator if all done.
				if (result != 0)
				{
					OpcCom.Interop.ReleaseServer(enumerator);
				}

				// create continuation point.
				else
				{
					position = new BrowsePosition(areaID, browseType, browseFilter, enumerator);
				}

				// return results.
				return (BrowseElement[])elements.ToArray(typeof(BrowseElement));
			}
		}
		
		//======================================================================
		// BrowseNext

		/// <summary>
		/// Continues browsing the server's address space at the specified position.
		/// </summary>
		/// <param name="maxElements">The maximum number of elements to return.</param>
		/// <param name="position">The position object used to continue a browse operation.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		public BrowseElement[] BrowseNext(int maxElements, ref IBrowsePosition position)
		{
			lock (this)
			{
				// verify state and arguments.
				if (m_server == null) throw new NotConnectedException();
				if (position == null) throw new ArgumentNullException("position");

				// initialize browser.
				InitializeBrowser();

				// move to desired area.
				ChangeBrowsePosition(((BrowsePosition)position).AreaID);

				// fetch enumerator from position object.
				UCOMIEnumString enumerator = ((BrowsePosition)position).Enumerator;
			
				// fetch elements.
				ArrayList elements = new ArrayList();

				int result = FetchElements(((BrowsePosition)position).BrowseType, maxElements, enumerator, elements);
				
				// dispose of position object if all done.
				if (result != 0)
				{
					position.Dispose();
					position = null;
				}

				// return results.
				return (BrowseElement[])elements.ToArray(typeof(BrowseElement));
			}
		}	
		#endregion

		#region Private Methods
		/// <summary>
		/// Creates an area browser object for use by all browse requests.
		/// </summary>
		private void InitializeBrowser()
		{
			// do nothing if browser already exists.
			if (m_browser != null)
			{
				return;
			}

			// initialize arguments.
			object unknown = null;
			Guid riid = typeof(IOPCEventAreaBrowser).GUID;

			// invoke COM method.
			try
			{
				((IOPCEventServer)m_server).CreateAreaBrowser(
					ref riid,
					out unknown);
			}
			catch (Exception e)
			{
				throw OpcCom.Interop.CreateException("IOPCEventServer.CreateAreaBrowser", e);
			}		

			// verify object.
			if (unknown == null)
			{
				throw new InvalidResponseException("unknown == null");
			}

			// save object.
			m_browser = unknown;
		}
		
		/// <summary>
		/// Moves the browse position prior to executing a browse operation.
		/// </summary>
		private void ChangeBrowsePosition(string areaID)
		{
			string targetID = (areaID != null)?areaID:"";

			// invoke COM method.
			try
			{
				((IOPCEventAreaBrowser)m_browser).ChangeBrowsePosition(
					OPCAEBROWSEDIRECTION.OPCAE_BROWSE_TO,
					targetID);
			}
			catch (Exception e)
			{
				throw OpcCom.Interop.CreateException("IOPCEventAreaBrowser.ChangeBrowsePosition", e);
			}		
		}
		
		/// <summary>
		/// Creates an enumerator for the names at the current position.
		/// </summary>
		private object CreateEnumerator(BrowseType browseType, string browseFilter)
		{
			// initialize arguments.
			OPCAEBROWSETYPE dwBrowseFilterType = Interop.GetBrowseType(browseType);
			IEnumString enumerator = null;

			// invoke COM method.
			try
			{
				((IOPCEventAreaBrowser)m_browser).BrowseOPCAreas(
					dwBrowseFilterType,
					(browseFilter != null)?browseFilter:"",
					out enumerator);
			}
			catch (Exception e)
			{
				throw OpcCom.Interop.CreateException("IOPCEventAreaBrowser.BrowseOPCAreas", e);
			}		

			// verify object.
			if (enumerator == null)
			{
				throw new InvalidResponseException("enumerator == null");
			}

			// return result.
			return enumerator;
		}

		/// <summary>
		/// Returns the qualified name for the node at the current position.
		/// </summary>
		private string GetQualifiedName(string name, BrowseType browseType)
		{
			// initialize arguments.
			string nodeID = null;

			// fetch area qualified name.
			if (browseType == BrowseType.Area)
			{
				try
				{
					((IOPCEventAreaBrowser)m_browser).GetQualifiedAreaName(name, out nodeID);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventAreaBrowser.GetQualifiedAreaName", e);
				}
			}
				
			// fetch source qualified name.
			else
			{
				try
				{
					((IOPCEventAreaBrowser)m_browser).GetQualifiedSourceName(name, out nodeID);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCEventAreaBrowser.GetQualifiedSourceName", e);
				}
			}

			// return results.
			return nodeID;
		}

		/// <summary>
		/// Fetches up to max elements and returns an flag indicating whether there are any elements left.
		/// </summary>
		private int FetchElements(BrowseType browseType, int maxElements, UCOMIEnumString enumerator, ArrayList elements)
		{
			string[] buffer = new string[1];

			// re-calculate buffer length.
			int bufferLength = (maxElements > 0 && maxElements-elements.Count < buffer.Length)?maxElements-elements.Count:buffer.Length;

			// fetch first batch of names.
			int fetched = 0;
			int result = enumerator.Next(bufferLength, buffer, out fetched);

			while (result == 0)
			{
				// create elements.
				for (int ii = 0; ii < fetched; ii++)
				{
					BrowseElement element = new BrowseElement();

					element.Name          = buffer[ii];
					element.QualifiedName = GetQualifiedName(buffer[ii], browseType);
					element.NodeType      = browseType;
				
					elements.Add(element);
				}

				// check for halt.
				if (maxElements > 0 && elements.Count >= maxElements)
				{
					break;
				}

				// re-calculate buffer length.
				bufferLength = (maxElements > 0 && maxElements-elements.Count < buffer.Length)?maxElements-elements.Count:buffer.Length;
					
				// fetch next block.
				result = enumerator.Next(bufferLength, buffer, out fetched);
			}

			// return final result.
			return result;
		}
		#endregion

		#region Private Members
		private bool m_supportsAE11 = true;
		private object m_browser = null;
		private int m_handles = 1;
		private Hashtable m_subscriptions = new Hashtable();
		#endregion
	}
	
	#region BrowsePosition Class
	/// <summary>
	/// Stores the state of a browse operation.
	/// </summary>
	[Serializable]
	public class BrowsePosition : Opc.Ae.BrowsePosition
	{
		#region Constructors
		/// <summary>
		/// Saves the parameters for an incomplete browse information.
		/// </summary>
		public BrowsePosition(
			string          areaID,
			BrowseType      browseType, 
			string          browseFilter,
			UCOMIEnumString enumerator)
		:
			base (areaID, browseType, browseFilter)
		{
			m_enumerator   = enumerator;
		}
		#endregion
        
        #region IDisposable Members
        /// <summary>
        /// Releases unmanaged resources held by the object.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    // Release managed resources.
                }

                // Release unmanaged resources.
                // Set large fields to null.

			    if (m_enumerator != null)
			    {				
				    OpcCom.Interop.ReleaseServer(m_enumerator);
				    m_enumerator = null;
			    }

                // Call Dispose on your base class.
                m_disposed = true;
            }

            base.Dispose(disposing);
        }

        private bool m_disposed = false;
        #endregion

		#region Public Interface
		/// <summary>
		/// Returns the enumerator stored in the object.
		/// </summary>
		public UCOMIEnumString Enumerator
		{
			get { return m_enumerator; }
		}
		#endregion

		#region Private Members
		UCOMIEnumString m_enumerator = null;
		#endregion
	}
	#endregion
}
