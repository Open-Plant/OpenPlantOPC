//============================================================================
// TITLE: Subscription.cs
//
// CONTENTS:
// 
// An in-process wrapper for a remote OPC Data Access 2.0X group.
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
using System.Globalization;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
using Opc;
using Opc.Da;
using OpcCom;
using OpcCom.Da;
using OpcRcw.Da;
using OpcRcw.Comn;

namespace OpcCom.Da20
{
	/// <summary>
	/// An in-process wrapper for a remote OPC Data Access 2.0X group.
	/// </summary>
	public class Subscription : OpcCom.Da.Subscription
	{	
		//======================================================================
		// Construction

		/// <summary>
		/// Initializes a new instance of a subscription.
		/// </summary>
		internal Subscription(object group, SubscriptionState state, int filters)
		:
			base(group, state, filters)
		{
		}
		
		//======================================================================
		// ISubscription

		/// <summary>
		/// Tells the server to send an data change update for all subscription items containing the cached values. 
		/// </summary>
		public override void Refresh()
		{
			lock (this)
			{
				if (m_group == null) throw new NotConnectedException();

				try
				{
					int cancelID = 0;
					((IOPCAsyncIO2)m_group).Refresh2(OPCDATASOURCE.OPC_DS_CACHE, ++m_counter, out cancelID);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCAsyncIO2.RefreshMaxAge", e);
				}
			}
		}

		/// <summary>
		/// Sets whether data change callbacks are enabled.
		/// </summary>
		public override void SetEnabled(bool enabled)
		{
			lock (this)
			{
				if (m_group == null) throw new NotConnectedException();

				try
				{
					((IOPCAsyncIO2)m_group).SetEnable((enabled)?1:0);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCAsyncIO2.SetEnable", e);
				}
			}
		}

		/// <summary>
		/// Gets whether data change callbacks are enabled.
		/// </summary>
		public override bool GetEnabled()
		{
			lock (this)
			{
				if (m_group == null) throw new NotConnectedException();

				try
				{
					int enabled = 0;
					((IOPCAsyncIO2)m_group).GetEnable(out enabled);
					return enabled != 0;
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCAsyncIO2.GetEnable", e);
				}
			}
		}

		//======================================================================
		// Private Methods

		/// <summary>
		/// Reads a set of items using DA2.0 interfaces.
		/// </summary>
		protected override ItemValueResult[] Read(ItemIdentifier[] itemIDs, Item[] items)
		{
			// create result list.
			ItemValueResult[] results = new ItemValueResult[itemIDs.Length];

			// separate into cache reads and device reads.
			ArrayList cacheReads  = new ArrayList();
			ArrayList deviceReads = new ArrayList();

			for (int ii = 0; ii < itemIDs.Length; ii++)
			{
				results[ii] = new ItemValueResult(itemIDs[ii]);

				if (items[ii].MaxAgeSpecified && (items[ii].MaxAge < 0 || items[ii].MaxAge == Int32.MaxValue))
				{
					cacheReads.Add(results[ii]);
				}
				else
				{
					deviceReads.Add(results[ii]);
				}
			}

			// read items from cache.
			if (cacheReads.Count > 0)
			{
				Read((ItemValueResult[])cacheReads.ToArray(typeof(ItemValueResult)), true);
			}

			// read items from device.
			if (deviceReads.Count > 0)
			{
				Read((ItemValueResult[])deviceReads.ToArray(typeof(ItemValueResult)), false);
			}		

			// return results.
			return results;
		}
		
		/// <summary>
		/// Reads a set of values.
		/// </summary>
		private void Read(ItemValueResult[] items, bool cache)
		{
			if (items.Length == 0) return;

			// marshal input parameters.
			int[] serverHandles = new int[items.Length];

			for (int ii = 0; ii < items.Length; ii++) 
			{	
				serverHandles[ii] = (int)items[ii].ServerHandle;
			}

			// initialize output parameters.
			IntPtr pValues = IntPtr.Zero;
			IntPtr pErrors = IntPtr.Zero;

			try
			{
				((IOPCSyncIO)m_group).Read(
					(cache)?OPCDATASOURCE.OPC_DS_CACHE:OPCDATASOURCE.OPC_DS_DEVICE,
					items.Length,
					serverHandles,
					out pValues,
					out pErrors);	
			}
			catch (Exception e)
			{					
				throw OpcCom.Interop.CreateException("IOPCSyncIO.Read", e);
			}

			// unmarshal output parameters.
			ItemValue[] values = OpcCom.Da.Interop.GetItemValues(ref pValues, items.Length, true);
			int[]       errors = OpcCom.Interop.GetInt32s(ref pErrors, items.Length, true);

			// construct results list.
			for (int ii = 0; ii < items.Length; ii++)
			{
				items[ii].ResultID       = OpcCom.Interop.GetResultID(errors[ii]);
				items[ii].DiagnosticInfo = null;

				// convert COM code to unified DA code.
				if (errors[ii] == ResultIDs.E_BADRIGHTS) { items[ii].ResultID = new ResultID(ResultID.Da.E_WRITEONLY, ResultIDs.E_BADRIGHTS); }

				if (items[ii].ResultID.Succeeded())
				{
					items[ii].Value              = values[ii].Value;
					items[ii].Quality            = values[ii].Quality;
					items[ii].QualitySpecified   = values[ii].QualitySpecified;
					items[ii].Timestamp          = values[ii].Timestamp;
					items[ii].TimestampSpecified = values[ii].TimestampSpecified;
				}
			}
		}

		/// <summary>
		/// Writes a set of items using DA2.0 interfaces.
		/// </summary>
		protected override IdentifiedResult[] Write(ItemIdentifier[] itemIDs, ItemValue[] items)
		{
			// create result list.
			IdentifiedResult[] results = new IdentifiedResult[itemIDs.Length];

			// construct list of valid items to write.
			ArrayList writeItems  = new ArrayList(itemIDs.Length);
			ArrayList writeValues = new ArrayList(itemIDs.Length);

			for (int ii = 0; ii < items.Length; ii++)
			{
				results[ii] = new IdentifiedResult(itemIDs[ii]);

				if (items[ii].QualitySpecified || items[ii].TimestampSpecified)
				{
					results[ii].ResultID       = ResultID.Da.E_NO_WRITEQT;
					results[ii].DiagnosticInfo = null;
					continue;
				}

				writeItems.Add(results[ii]);
				writeValues.Add(items[ii]);
			}

			// check if there is nothing to do.
			if (writeItems.Count == 0)
			{
				return results;
			}

			// initialize input parameters.
			int[]    serverHandles = new int[writeItems.Count];
			object[] values        = new object[writeItems.Count];

			for (int ii = 0; ii < serverHandles.Length; ii++)
			{
				serverHandles[ii] = (int)((IdentifiedResult)writeItems[ii]).ServerHandle;
				values[ii]        = OpcCom.Interop.GetVARIANT(((ItemValue)writeValues[ii]).Value);
			}
						
			IntPtr pErrors = IntPtr.Zero;

			// write item values.
			try
			{
				((IOPCSyncIO)m_group).Write(
					writeItems.Count,
					serverHandles,
					values,
					out pErrors);
			}
			catch (Exception e)
			{
				throw OpcCom.Interop.CreateException("IOPCSyncIO.Write", e);
			}

			// unmarshal results.
			int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, writeItems.Count, true);

			for (int ii = 0; ii < writeItems.Count; ii++)
			{
				IdentifiedResult result = (IdentifiedResult)writeItems[ii];

				result.ResultID       = OpcCom.Interop.GetResultID(errors[ii]);
				result.DiagnosticInfo = null;

				// convert COM code to unified DA code.
				if (errors[ii] == ResultIDs.E_BADRIGHTS) { results[ii].ResultID = new ResultID(ResultID.Da.E_READONLY, ResultIDs.E_BADRIGHTS); }
			}

			// return results.
			return results;
		}

		/// <summary>
		/// Begins an asynchronous read of a set of items using DA2.0 interfaces.
		/// </summary>
		protected override IdentifiedResult[] BeginRead(
			ItemIdentifier[] itemIDs, 
			Item[]           items,
			int              requestID,
			out int          cancelID)
		{
			try
			{
				// marshal input parameters.
				int[] serverHandles = new int[itemIDs.Length];

				for (int ii = 0; ii < itemIDs.Length; ii++) 
				{	
					serverHandles[ii] = (int)itemIDs[ii].ServerHandle;
				}

				// initialize output parameters.
				IntPtr pErrors     = IntPtr.Zero;

				((IOPCAsyncIO2)m_group).Read(
					itemIDs.Length,
					serverHandles,
					requestID,
					out cancelID,
					out pErrors);

				// unmarshal output parameters.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, itemIDs.Length, true);

				// create item results.
				IdentifiedResult[] results = new IdentifiedResult[itemIDs.Length];

				for (int ii = 0; ii < itemIDs.Length; ii++)
				{
					results[ii]                = new IdentifiedResult(itemIDs[ii]);
					results[ii].ResultID       = OpcCom.Interop.GetResultID(errors[ii]);
					results[ii].DiagnosticInfo = null;

					// convert COM code to unified DA code.
					if (errors[ii] == ResultIDs.E_BADRIGHTS) { results[ii].ResultID = new ResultID(ResultID.Da.E_WRITEONLY, ResultIDs.E_BADRIGHTS); }
				}

				// return results.
				return results;
			}
			catch (Exception e)
			{					
				throw OpcCom.Interop.CreateException("IOPCAsyncIO2.Read", e);
			}
		}

		/// <summary>
		/// Begins an asynchronous write for a set of items using DA2.0 interfaces.
		/// </summary>
		protected override IdentifiedResult[] BeginWrite(
			ItemIdentifier[] itemIDs, 
			ItemValue[]      items,
			int              requestID,
			out int          cancelID)
		{
			cancelID = 0;

			ArrayList validItems  = new ArrayList();
			ArrayList validValues = new ArrayList();

			// construct initial result list.
			IdentifiedResult[] results = new IdentifiedResult[itemIDs.Length];

			for (int ii = 0; ii < itemIDs.Length; ii++) 
			{	
				results[ii] = new IdentifiedResult(itemIDs[ii]);

				results[ii].ResultID       = ResultID.S_OK;
				results[ii].DiagnosticInfo = null;

				if (items[ii].QualitySpecified || items[ii].TimestampSpecified)
				{
					results[ii].ResultID       = ResultID.Da.E_NO_WRITEQT;
					results[ii].DiagnosticInfo = null;
					continue;
				}

				validItems.Add(results[ii]);
				validValues.Add(OpcCom.Interop.GetVARIANT(items[ii].Value));
			}

			// check if any valid items exist.
			if (validItems.Count == 0)
			{
				return results;
			}

			try
			{
				// initialize input parameters.
				int[] serverHandles = new int[validItems.Count];

				for (int ii = 0; ii < validItems.Count; ii++) 
				{	
					serverHandles[ii] = (int)((IdentifiedResult)validItems[ii]).ServerHandle;
				}

				// write to sever.
				IntPtr pErrors = IntPtr.Zero;

				((IOPCAsyncIO2)m_group).Write(
					validItems.Count,
					serverHandles,
					(object[])validValues.ToArray(typeof(object)),
					requestID,
					out cancelID,
					out pErrors);

				// unmarshal results.
				int[] errors = OpcCom.Interop.GetInt32s(ref pErrors, validItems.Count, true);

				// create result list.
				for (int ii = 0; ii < validItems.Count; ii++)
				{
					IdentifiedResult result = (IdentifiedResult)validItems[ii];

					result.ResultID       = OpcCom.Interop.GetResultID(errors[ii]);
					result.DiagnosticInfo = null;

					// convert COM code to unified DA code.
					if (errors[ii] == ResultIDs.E_BADRIGHTS) { results[ii].ResultID = new ResultID(ResultID.Da.E_READONLY, ResultIDs.E_BADRIGHTS); }
				}
			}
			catch (Exception e)
			{
				throw OpcCom.Interop.CreateException("IOPCAsyncIO2.Write", e);
			}

			// return results.
			return results;
		}
	}
}
