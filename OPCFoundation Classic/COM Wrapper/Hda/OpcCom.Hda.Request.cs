//============================================================================
// TITLE: OpcCom.Hda.DataCallback.cs
//
// CONTENTS:
// 
// A class that implements the HDA data callback interface.
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
// 2003/01/30 RSA   Initial implementation.

using System;
using System.Xml;
using System.Net;
using System.Threading;
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Runtime.InteropServices;
using Opc;
using Opc.Hda;
using OpcRcw.Hda;
using OpcRcw.Comn;

namespace OpcCom.Hda
{
	/// <summary>
	/// An object that mainatains the state of asynchronous requests.
	/// </summary>
	internal class Request : IRequest, IActualTime
	{
		/// <summary>
		/// The unique id assigned to the request when it was created.
		/// </summary>
		public int RequestID {get { return m_requestID; }}
		
		/// <summary>
		/// The unqiue id assigned by the server when it was created.
		/// </summary>
		public int CancelID {get { return m_cancelID; }}
		
		/// <summary>
		/// Fired when the server acknowledges that a request was cancelled.
		/// </summary>
		public event CancelCompleteEventHandler CancelComplete
		{
			add    {lock (this) { m_cancelComplete += value; }}
			remove {lock (this) { m_cancelComplete -= value; }} 
		}

		/// <summary>
		/// Initializes the object with all required information.
		/// </summary>
		public Request(object requestHandle, Delegate callback, int requestID)
		{			
			m_requestHandle = requestHandle; 
			m_callback      = callback; 
			m_requestID     = requestID; 
		}

		/// <summary>
		/// Updates the request with the initial results.  
		/// </summary>
		public bool Update(int cancelID, ItemIdentifier[] results)
		{
			lock (this)
			{
				// save the server assigned id.
				m_cancelID = cancelID; 

				// create a table of items indexed by the handle returned by the server in a callback.
				m_items = new Hashtable();

				foreach (ItemIdentifier result in results)
				{
					if (!typeof(IResult).IsInstanceOfType(result) || ((IResult)result).ResultID.Succeeded())
					{
						m_items[result.ServerHandle] = new ItemIdentifier(result);
					}
				}

				// nothing more to do - no good items.
				if (m_items.Count == 0)
				{
					return true;
				}

				// invoke callbacks for results that have already arrived.
				bool complete = false;

				if (m_results != null)
				{
					foreach (object result in m_results)
					{
						complete = InvokeCallback(result);
					}
				}

				// all done.
				return complete;
			}
		}

		/// <summary>
		/// Invokes the callback for the request.
		/// </summary>
		public bool InvokeCallback(object results)
		{
			lock (this)
			{
				// save the results if the initial call to the server has not completed yet.
				if (m_items == null)
				{
					// create cache for results.
					if (m_results == null)
					{
						m_results = new ArrayList();
					}

					m_results.Add(results);

					// request not initialized completely
					return false;
				}

				// invoke on data update callback.
				if (typeof(DataUpdateEventHandler).IsInstanceOfType(m_callback))
				{
					return InvokeCallback((DataUpdateEventHandler)m_callback, results);
				}

				// invoke read completed callback.
				if (typeof(ReadValuesEventHandler).IsInstanceOfType(m_callback))
				{
					return InvokeCallback((ReadValuesEventHandler)m_callback, results);
				}
				
				// invoke read attributes completed callback.
				if (typeof(ReadAttributesEventHandler).IsInstanceOfType(m_callback))
				{
					return InvokeCallback((ReadAttributesEventHandler)m_callback, results);
				}
				
				// invoke read annotations completed callback.
				if (typeof(ReadAnnotationsEventHandler).IsInstanceOfType(m_callback))
				{
					return InvokeCallback((ReadAnnotationsEventHandler)m_callback, results);
				}
				
				// invoke update completed callback.
				if (typeof(UpdateCompleteEventHandler).IsInstanceOfType(m_callback))
				{
					return InvokeCallback((UpdateCompleteEventHandler)m_callback, results);
				}
				
				// callback not supported.
				return true;
			}
		}

		/// <summary>
		/// Called when the server acknowledges that a request was cancelled. 
		/// </summary>
		public void OnCancelComplete()
		{
			lock (this)
			{
				if (m_cancelComplete != null)
				{
					m_cancelComplete(this);
				}
			}
		}

		#region IRequest Members
		/// <summary>
		/// An unique identifier, assigned by the client, for the request.
		/// </summary>
		public object Handle
		{
			get { return m_requestHandle; }
		}
		#endregion

		#region IActualTime Members
		/// <summary>
		/// The actual start time used by a server while processing a request.
		/// </summary>
		public DateTime StartTime
		{
			get { return m_startTime;  } 
			set { m_startTime = value; }
		}

		/// <summary>
		/// The actual end time used by a server while processing a request.
		/// </summary>
		public DateTime EndTime
		{
			get { return m_endTime;  } 
			set { m_endTime = value; }
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Invokes callback for a data change update.
		/// </summary>
		private bool InvokeCallback(DataUpdateEventHandler callback, object results)
		{
			// check for valid result type.
			if (!typeof(ItemValueCollection[]).IsInstanceOfType(results))
			{
				return false;
			}

			ItemValueCollection[] values = (ItemValueCollection[])results;

			// update item handles and actual times.
			UpdateResults(values);

			try
			{
				callback(this, values);
			}
			catch 
			{
				// ignore exceptions in the callbacks.
			}
			
			// request never completes.
			return false;
		}

		/// <summary>
		/// Invokes callback for a read request.
		/// </summary>
		private bool InvokeCallback(ReadValuesEventHandler callback, object results)
		{
			// check for valid result type.
			if (!typeof(ItemValueCollection[]).IsInstanceOfType(results))
			{
				return false;
			}

			ItemValueCollection[] values = (ItemValueCollection[])results;

			// update item handles and actual times.
			UpdateResults(values);

			try
			{
				callback(this, values);
			}
			catch 
			{
				// ignore exceptions in the callbacks.
			}

			// check if all data has been sent.
			foreach (ItemValueCollection value in values)
			{
				if (value.ResultID == ResultID.Hda.S_MOREDATA)
				{
					return false;
				}
			}

			// request is complete.
			return true;
		}	

		/// <summary>
		/// Invokes callback for a read attributes request.
		/// </summary>
		private bool InvokeCallback(ReadAttributesEventHandler callback, object results)
		{
			// check for valid result type.
			if (!typeof(ItemAttributeCollection).IsInstanceOfType(results))
			{
				return false;
			}

			ItemAttributeCollection values = (ItemAttributeCollection)results;

			// update item handles and actual times.
			UpdateResults(new ItemAttributeCollection[] { values });

			try
			{
				callback(this, values);
			}
			catch 
			{
				// ignore exceptions in the callbacks.
			}

			// request always completes
			return true;
		}
		
		/// <summary>
		/// Invokes callback for a read annotations request.
		/// </summary>
		private bool InvokeCallback(ReadAnnotationsEventHandler callback, object results)
		{
			// check for valid result type.
			if (!typeof(AnnotationValueCollection[]).IsInstanceOfType(results))
			{
				return false;
			}

			AnnotationValueCollection[] values = (AnnotationValueCollection[])results;

			// update item handles and actual times.
			UpdateResults(values);

			try
			{
				callback(this, values);
			}
			catch 
			{
				// ignore exceptions in the callbacks.
			}

			// request always completes
			return true;
		}

		/// <summary>
		/// Invokes callback for a read annotations request.
		/// </summary>
		private bool InvokeCallback(UpdateCompleteEventHandler callback, object results)
		{
			// check for valid result type.
			if (!typeof(ResultCollection[]).IsInstanceOfType(results))
			{
				return false;
			}

			ResultCollection[] values = (ResultCollection[])results;

			// update item handles and actual times.
			UpdateResults(values);

			try
			{
				callback(this, values);
			}
			catch 
			{
				// ignore exceptions in the callbacks.
			}

			// request always completes
			return true;
		}

		/// <summary>
		/// Updates the result objects with locally cached information.
		/// </summary>
		private void UpdateResults(ItemIdentifier[] results)
		{
			foreach (ItemIdentifier result in results)
			{
				// update actual times.
				if (typeof(IActualTime).IsInstanceOfType(result))
				{
					((IActualTime)result).StartTime = StartTime;
					((IActualTime)result).EndTime   = EndTime;
				}

				// add item identifier to value collection.
				ItemIdentifier itemID = (ItemIdentifier)m_items[result.ServerHandle];

				if (itemID != null)
				{
					result.ItemName     = itemID.ItemName;
					result.ItemPath     = itemID.ItemPath;
					result.ServerHandle = itemID.ServerHandle;
					result.ClientHandle = itemID.ClientHandle;
				}
			}
		}
		#endregion

		#region Private Members
		private object m_requestHandle = null; 
		private Delegate m_callback = null; 
		private int m_requestID = 0; 
		private int m_cancelID = 0;
		private DateTime m_startTime = DateTime.MinValue;
		private DateTime m_endTime = DateTime.MinValue;
		private Hashtable m_items = null;
		private ArrayList m_results = null;
		private event CancelCompleteEventHandler m_cancelComplete = null;
		#endregion
	}
}
