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
	///  A class that implements the HDA data callback interface.
	/// </summary>
	internal class  DataCallback : OpcRcw.Hda.IOPCHDA_DataCallback
	{	
		/// <summary>
		/// Initializes the object with the containing subscription object.
		/// </summary>
		public DataCallback() {}

		/// <summary>
		/// Fired when an exception occurs during callback processing.
		/// </summary>
		public event CallbackExceptionEventHandler CallbackException
		{
			add	   {lock (this) { m_callbackException += value; }}
			remove {lock (this) { m_callbackException -= value; }}
		}
	
		/// <summary>
		/// Creates a new request object.
		/// </summary>
		public Request CreateRequest(object requestHandle, Delegate callback)
		{
			lock (this)
			{
				// create a new request.
				Request request = new Request(requestHandle, callback, ++m_nextID);

				// no items yet - callback may return before async call returns.
				m_requests[request.RequestID] = request;	
		
				// return requests.
				return request;
			}
		}

		/// <summary>
		/// Cancels an existing request.
		/// </summary>
		public bool CancelRequest(Request request, CancelCompleteEventHandler callback)
		{
			lock (this)
			{
				// check if it is a valid request.
				if (!m_requests.Contains(request.RequestID))
				{
					return false;
				}

				// request will be removed when the cancel complete callback arrives.
				if (callback != null)
				{
					request.CancelComplete += callback;
				}

				// no confirmation required - remove request immediately.
				else
				{
					m_requests.Remove(request.RequestID);
				}

				// request will be cancelled.
				return true;
			}
		}

		#region IOPCHDA_DataCallback Members
		/// <summary>
		/// Called when new data arrives for a subscription.
		/// </summary>
		public void OnDataChange(
			int		      dwTransactionID, 
			int	          hrStatus,
			int		      dwNumItems, 
			OPCHDA_ITEM[] pItemValues,
			int[]         phrErrors)
		{
			try
			{
				lock (this)
				{
					// lookup request transaction.
					Request request = (Request)m_requests[dwTransactionID];

					if (request == null)
					{
						return;
					}

					// unmarshal results.
					ItemValueCollection[] results = new ItemValueCollection[pItemValues.Length];

					for (int ii = 0; ii < pItemValues.Length; ii++)
					{
						results[ii] = Interop.GetItemValueCollection(pItemValues[ii], false);

						results[ii].ServerHandle = results[ii].ClientHandle;
						results[ii].ClientHandle = null;
						results[ii].ResultID     = OpcCom.Interop.GetResultID(phrErrors[ii]);
					}

					// invoke callback - remove request if unexpected error occured.
					if (request.InvokeCallback(results))
					{
						m_requests.Remove(request.RequestID);
					}
				}
			}
			catch (Exception exception)
			{
				HandleException(dwTransactionID, exception);
			}
		}

		/// <summary>
		/// Called when an asynchronous read request completes.
		/// </summary>
		public void OnReadComplete(
			int		      dwTransactionID, 
			int	          hrStatus,
			int		      dwNumItems, 
			OPCHDA_ITEM[] pItemValues,
			int[]         phrErrors)
		{
			try
			{
				lock (this)
				{
					// lookup request transaction.
					Request request = (Request)m_requests[dwTransactionID];

					if (request == null)
					{
						return;
					}

					// unmarshal results.
					ItemValueCollection[] results = new ItemValueCollection[pItemValues.Length];

					for (int ii = 0; ii < pItemValues.Length; ii++)
					{
						results[ii] = Interop.GetItemValueCollection(pItemValues[ii], false);

						results[ii].ServerHandle = pItemValues[ii].hClient;
						results[ii].ResultID     = OpcCom.Interop.GetResultID(phrErrors[ii]);
					}

					// invoke callback - remove request if all results arrived.
					if (request.InvokeCallback(results))
					{
						m_requests.Remove(request.RequestID);
					}
				}
			}
			catch (Exception exception)
			{
				HandleException(dwTransactionID, exception);
			}
		}

		/// <summary>
		/// Called when an asynchronous read modified request completes.
		/// </summary>
		public void OnReadModifiedComplete(
			int                   dwTransactionID, 
			int	                  hrStatus,
			int		              dwNumItems, 
			OPCHDA_MODIFIEDITEM[] pItemValues,
			int[]                 phrErrors)
		{
			try
			{
				lock (this)
				{
					// lookup request transaction.
					Request request = (Request)m_requests[dwTransactionID];

					if (request == null)
					{
						return;
					}

					// unmarshal results.
					ModifiedValueCollection[] results = new ModifiedValueCollection[pItemValues.Length];

					for (int ii = 0; ii < pItemValues.Length; ii++)
					{
						results[ii] = Interop.GetModifiedValueCollection(pItemValues[ii], false);

						results[ii].ServerHandle = pItemValues[ii].hClient;
						results[ii].ResultID     = OpcCom.Interop.GetResultID(phrErrors[ii]);
					}

					// invoke callback - remove request if all results arrived.
					if (request.InvokeCallback(results))
					{
						m_requests.Remove(request.RequestID);
					}
				}
			}
			catch (Exception exception)
			{
				HandleException(dwTransactionID, exception);
			}
		}

		/// <summary>
		/// Called when an asynchronous read attributes request completes.
		/// </summary>
		public void OnReadAttributeComplete(
			int		           dwTransactionID, 
			int		           hrStatus,
			int                hClient, 
			int   	           dwNumItems, 
			OPCHDA_ATTRIBUTE[] pAttributeValues,
			int[]              phrErrors)
		{
			try
			{
				lock (this)
				{
					// lookup request transaction.
					Request request = (Request)m_requests[dwTransactionID];

					if (request == null)
					{
						return;
					}

					// create item object to collect results.
					ItemAttributeCollection item = new ItemAttributeCollection();
					item.ServerHandle = hClient;

					// unmarshal results.
					AttributeValueCollection[] results = new AttributeValueCollection[pAttributeValues.Length];

					for (int ii = 0; ii < pAttributeValues.Length; ii++)
					{
						results[ii] = Interop.GetAttributeValueCollection(pAttributeValues[ii], false);

						results[ii].ResultID = OpcCom.Interop.GetResultID(phrErrors[ii]);
					
						item.Add(results[ii]);
					}

					// invoke callback - remove request if all results arrived.
					if (request.InvokeCallback(item))
					{
						m_requests.Remove(request.RequestID);
					}
				}
			}
			catch (Exception exception)
			{
				HandleException(dwTransactionID, exception);
			}
		}

		/// <summary>
		/// Called when an asynchronous read annotations request completes.
		/// </summary>
		public void OnReadAnnotations(
			int				    dwTransactionID, 
			int			        hrStatus,
			int				    dwNumItems, 
			OPCHDA_ANNOTATION[] pAnnotationValues,
			int[]               phrErrors)
		{
			try
			{
				lock (this)
				{
					// lookup request transaction.
					Request request = (Request)m_requests[dwTransactionID];

					if (request == null)
					{
						return;
					}

					// unmarshal results.
					AnnotationValueCollection[] results = new AnnotationValueCollection[pAnnotationValues.Length];

					for (int ii = 0; ii < pAnnotationValues.Length; ii++)
					{
						results[ii] = Interop.GetAnnotationValueCollection(pAnnotationValues[ii], false);

						results[ii].ServerHandle = pAnnotationValues[ii].hClient;
						results[ii].ResultID     = OpcCom.Interop.GetResultID(phrErrors[ii]);
					}

					// invoke callback - remove request if all results arrived.
					if (request.InvokeCallback(results))
					{
						m_requests.Remove(request.RequestID);
					}
				}
			}
			catch (Exception exception)
			{
				HandleException(dwTransactionID, exception);
			}
		}

		/// <summary>
		/// Called when an asynchronous insert annotations request completes.
		/// </summary>
		public void OnInsertAnnotations(
			int	  dwTransactionID, 
			int	  hrStatus,
			int	  dwCount, 
			int[] phClients, 
			int[] phrErrors)
		{

			try
			{
				lock (this)
				{
					// lookup request transaction.
					Request request = (Request)m_requests[dwTransactionID];

					if (request == null)
					{
						return;
					}

					// unmarshal results.
					ArrayList results = new ArrayList();

					if (dwCount > 0)
					{
						// group results in collections for the same item id.
						int currentHandle = phClients[0];

						ResultCollection itemResults = new ResultCollection();

						for (int ii = 0; ii < dwCount; ii++)
						{
							// create a new collection for the next item's results.
							if (phClients[ii] != currentHandle)
							{
								itemResults.ServerHandle = currentHandle;
								results.Add(itemResults);
								
								currentHandle = phClients[ii];
								itemResults = new ResultCollection();
							}

							Result result = new Result(OpcCom.Interop.GetResultID(phrErrors[ii]));
							itemResults.Add(result);
						}

						// add the last set of item results.
						itemResults.ServerHandle = currentHandle;
						results.Add(itemResults);
					}

					// invoke callback - remove request if all results arrived.
					if (request.InvokeCallback((ResultCollection[])results.ToArray(typeof(ResultCollection))))
					{
						m_requests.Remove(request.RequestID);
					}
				}
			}
			catch (Exception exception)
			{
				HandleException(dwTransactionID, exception);
			}
		}

		/// <summary>
		/// Called when a batch of data from playback request arrives.
		/// </summary>
		public void OnPlayback(
			int    dwTransactionID, 
			int    hrStatus,
			int    dwNumItems, 
			IntPtr ppItemValues,
			int[]  phrErrors)
		{
			try
			{
				lock (this)
				{
					// lookup request transaction.
					Request request = (Request)m_requests[dwTransactionID];

					if (request == null)
					{
						return;
					}

					// unmarshal results.
					ItemValueCollection[] results = new ItemValueCollection[dwNumItems];

					// the data is transfered as a array of pointers to items instead of simply
					// as an array of items. This is due to a mistake in the HDA IDL.
					int[] pItems = OpcCom.Interop.GetInt32s(ref ppItemValues, dwNumItems, false);

					for (int ii = 0; ii < dwNumItems; ii++)
					{
						// get pointer to item.
						IntPtr pItem = (IntPtr)pItems[ii];
			
						// unmarshal item as an array of length 1.
						ItemValueCollection[] item = Interop.GetItemValueCollections(ref pItem, 1, false);

						if (item != null && item.Length == 1)
						{
							results[ii]              = item[0];
							results[ii].ServerHandle = results[ii].ClientHandle;
							results[ii].ClientHandle = null;
							results[ii].ResultID     = OpcCom.Interop.GetResultID(phrErrors[ii]);
						}
					}

					// invoke callback - remove request if unexpected error occured.
					if (request.InvokeCallback(results))
					{
						m_requests.Remove(request.RequestID);
					}
				}
			}
			catch (Exception exception)
			{
				HandleException(dwTransactionID, exception);
			}
		}

		/// <summary>
		/// Called when an asynchronous update request completes.
		/// </summary>
		public void OnUpdateComplete(
			int	  dwTransactionID, 
			int	  hrStatus,
			int	  dwCount, 
			int[] phClients, 
			int[] phrErrors)
		{
			try
			{
				lock (this)
				{
					// lookup request transaction.
					Request request = (Request)m_requests[dwTransactionID];

					if (request == null)
					{
						return;
					}

					// unmarshal results.
					ArrayList results = new ArrayList();

					if (dwCount > 0)
					{
						// group results in collections for the same item id.
						int currentHandle = phClients[0];

						ResultCollection itemResults = new ResultCollection();

						for (int ii = 0; ii < dwCount; ii++)
						{
							// create a new collection for the next item's results.
							if (phClients[ii] != currentHandle)
							{
								itemResults.ServerHandle = currentHandle;
								results.Add(itemResults);
								
								currentHandle = phClients[ii];
								itemResults = new ResultCollection();
							}

							Result result = new Result(OpcCom.Interop.GetResultID(phrErrors[ii]));
							itemResults.Add(result);
						}

						// add the last set of item results.
						itemResults.ServerHandle = currentHandle;
						results.Add(itemResults);
					}

					// invoke callback - remove request if all results arrived.
					if (request.InvokeCallback((ResultCollection[])results.ToArray(typeof(ResultCollection))))
					{
						m_requests.Remove(request.RequestID);
					}
				}
			}
			catch (Exception exception)
			{
				HandleException(dwTransactionID, exception);
			}
		}

		/// <summary>
		/// Called when an asynchronous request was cancelled successfully.
		/// </summary>
		public void OnCancelComplete(int dwCancelID)
		{
			try
			{
				lock (this)
				{
					// lookup request.
					Request request = (Request)m_requests[dwCancelID];

					if (request == null)
					{
						return;
					}

					// send the cancel complete notification.
					request.OnCancelComplete();

					// remove the request.
					m_requests.Remove(request.RequestID);
				}
			}
			catch (Exception exception)
			{
				HandleException(dwCancelID, exception);
			}
		}
		#endregion	

		#region Private Methods
		/// <summary>
		/// Fires an event indicating an exception occurred during callback processing.
		/// </summary>
		void HandleException(int requestID, Exception exception)
		{
			lock (this)
			{
				// lookup request.
				Request request = (Request)m_requests[requestID];

				if (request != null)
				{
					// send notification.
					if (m_callbackException != null)
					{
						m_callbackException(request, exception);
					}
				}
			}
		}
		#endregion

		#region Private Members
		private int m_nextID = 0;
		private Hashtable m_requests = new Hashtable();
		private CallbackExceptionEventHandler m_callbackException = null;
		#endregion
	}
}
