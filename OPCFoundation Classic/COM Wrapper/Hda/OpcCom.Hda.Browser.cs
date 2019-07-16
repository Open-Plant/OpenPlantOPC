//============================================================================
// TITLE: OpcCom.Hda.Browser.cs
//
// CONTENTS:
// 
// An in-process wrapper for an OPC HDA browser object.
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
// 2003/12/31 RSA   Initial implementation.
// 2004/11/11 RSA   Added a base interfaces for BrowsePosition.

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
	/// An in-process wrapper an OPC HDA browser object.
	/// </summary>
	public class  Browser : Opc.Hda.IBrowser
	{	
		//======================================================================
		// Construction

		/// <summary>
		/// Initializes the object with the specifed COM server.
		/// </summary>
		internal Browser(OpcCom.Hda.Server server, IOPCHDA_Browser browser, BrowseFilter[] filters, ResultID[] results)
		{
			if (browser == null) throw new ArgumentNullException("browser");

			// save the server object that created the browser.
			m_server = server;

			// save the COM server (released in Dispose()).
			m_browser = browser;

			// save only the filters that were accepted.
			if (filters != null)
			{
				ArrayList validFilters = new ArrayList();

				for (int ii = 0; ii < filters.Length; ii++)
				{
					if (results[ii].Succeeded())
					{
						validFilters.Add(filters[ii]);
					}
				}

				m_filters = new BrowseFilterCollection(validFilters);
			}
		}

        #region IDisposable Members
        /// <summary>
        /// The finalizer.
        /// </summary>
        ~Browser()
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
                if (disposing)
                {
                    // Free other state (managed objects).
                }

                // Free your own state (unmanaged objects).
                // Set large fields to null.

                lock (this)
                {
                    m_server = null;
                    OpcCom.Interop.ReleaseServer(m_browser);
                    m_browser = null;
                }

                m_disposed = true;
            }
        }

        private bool m_disposed = false;
		#endregion

		//======================================================================
		// Filters

		/// <summary>
		/// Returns the set of attribute filters used by the browser. 
		/// </summary>
		public BrowseFilterCollection Filters 
		{ 
			get 
			{
				lock (this)
				{
					return (BrowseFilterCollection)m_filters.Clone();
				}
			}
		}

		//======================================================================
		// Browse
		
		/// <summary>
		/// Browses the server's address space at the specified branch.
		/// </summary>
		/// <param name="itemID">The item id of the branch to search.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		public BrowseElement[] Browse(ItemIdentifier itemID)
		{
			IBrowsePosition position = null;

			BrowseElement[] elements = Browse(itemID, 0, out position);

			if (position != null)
			{
				position.Dispose();
			}

			return elements;
		}
		
		/// <summary>
		/// Begins a browsing the server's address space at the specified branch.
		/// </summary>
		/// <param name="itemID">The item id of the branch to search.</param>
		/// <param name="maxElements">The maximum number of elements to return.</param>
		/// <param name="position">The position object used to continue a browse operation.</param>
		/// <returns>The set of elements that meet the filter criteria.</returns>
		public BrowseElement[] Browse(ItemIdentifier itemID, int maxElements, out IBrowsePosition position)
		{
			position = null;

			// interpret invalid values as 'no limit'.
			if (maxElements <= 0)
			{
				maxElements = Int32.MaxValue;
			}

			lock (this)
			{
				string branchPath = (itemID != null && itemID.ItemName != null)?itemID.ItemName:"";

				// move to the correct position in the server's address space.
				try
				{					
					m_browser.ChangeBrowsePosition(OPCHDA_BROWSEDIRECTION.OPCHDA_BROWSE_DIRECT, branchPath);
				}
				catch (Exception e)
				{
					throw OpcCom.Interop.CreateException("IOPCHDA_Browser.ChangeBrowsePosition", e);
				}

				// browse for branches
				EnumString enumerator = GetEnumerator(true);

				ArrayList elements = FetchElements(enumerator, maxElements, true);

				// check if max element count reached.
				if (elements.Count >= maxElements)
				{
					position = new BrowsePosition(branchPath, enumerator, false);
					return (BrowseElement[])elements.ToArray(typeof(BrowseElement));
				}

				// release enumerator.
				enumerator.Dispose();

				// browse for items
				enumerator = GetEnumerator(false);

				ArrayList items = FetchElements(enumerator, maxElements-elements.Count, false);

				if (items != null)
				{
					elements.AddRange(items);
				}

				// check if max element count reached.
				if (elements.Count >= maxElements)
				{
					position = new BrowsePosition(branchPath, enumerator, true);
					return (BrowseElement[])elements.ToArray(typeof(BrowseElement));
				}

				// release enumerator.
				enumerator.Dispose();

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
			// check arguments.
			if (position == null || position.GetType() != typeof(OpcCom.Hda.BrowsePosition))
			{
				throw new ArgumentException("Not a valid browse position object.", "position");
			}

			// interpret invalid values as 'no limit'.
			if (maxElements <= 0)
			{
				maxElements = Int32.MaxValue;
			}

			lock (this)
			{
				OpcCom.Hda.BrowsePosition pos = (OpcCom.Hda.BrowsePosition)position;

				ArrayList elements = new ArrayList();

				if (!pos.FetchingItems)
				{
					elements = FetchElements(pos.Enumerator, maxElements, true);

					// check if max element count reached.
					if (elements.Count >= maxElements)
					{
						return (BrowseElement[])elements.ToArray(typeof(BrowseElement));
					}

					// release enumerator.
					pos.Enumerator.Dispose();
					
					pos.Enumerator = null;
					pos.FetchingItems = true;

					// move to the correct position in the server's address space.
					try
					{					
						m_browser.ChangeBrowsePosition(OPCHDA_BROWSEDIRECTION.OPCHDA_BROWSE_DIRECT, pos.BranchPath);
					}
					catch (Exception e)
					{
						throw OpcCom.Interop.CreateException("IOPCHDA_Browser.ChangeBrowsePosition", e);
					}

					// create enumerator for items.
					pos.Enumerator = GetEnumerator(false);
				}

				// fetch next set of items.
				ArrayList items = FetchElements(pos.Enumerator, maxElements-elements.Count, false);

				if (items != null)
				{
					elements.AddRange(items);
				}

				// check if max element count reached.
				if (elements.Count >= maxElements)
				{
					return (BrowseElement[])elements.ToArray(typeof(BrowseElement));
				}

				// release position object.
				position.Dispose();
				position = null;

				// return elements.
				return (BrowseElement[])elements.ToArray(typeof(BrowseElement));
			}
		}

		#region Private Methods
		/// <summary>
		/// Creates an enumerator for the elements contained with the current branch.
		/// </summary>
		private EnumString GetEnumerator(bool isBranch)
		{
			try
			{
				OPCHDA_BROWSETYPE browseType = (isBranch)?OPCHDA_BROWSETYPE.OPCHDA_BRANCH:OPCHDA_BROWSETYPE.OPCHDA_LEAF;

				IEnumString pEnumerator = null;
				m_browser.GetEnum(browseType, out pEnumerator);

				return new EnumString(pEnumerator);
			}
			catch (Exception e)
			{
				throw OpcCom.Interop.CreateException("IOPCHDA_Browser.GetEnum", e);
			}
		}

        /// <summary>
        /// Fetches the full branch name for the relative branch name
        /// </summary>
        private string GetFullBranchName(string name)
        {
            string branch = null;

            try
            {
                m_browser.ChangeBrowsePosition(OPCHDA_BROWSEDIRECTION.OPCHDA_BROWSE_DOWN, name);
            }
            catch
            {
                // if we can't get to the branch then something is wrong
                return null;
            }

            try
            {
                m_browser.GetBranchPosition(out branch);
            }
            catch
            {
                // If something goes wrong then we still need to get back to where we were
            }

            m_browser.ChangeBrowsePosition(OPCHDA_BROWSEDIRECTION.OPCHDA_BROWSE_UP, "");

            return branch;
        }

		/// <summary>
		/// Fetches the element names and item ids for each element.
		/// </summary>
		private ArrayList FetchElements(EnumString enumerator, int maxElements, bool isBranch)
		{
			ArrayList elements = new ArrayList();

			while (elements.Count < maxElements)
			{			
				// fetch next batch of element names.
				int count = Browser.BLOCK_SIZE;

				if (elements.Count + count > maxElements)
				{
					count = maxElements - elements.Count; 
				}

				string[] names = enumerator.Next(count);

				// check if no more elements found.
				if (names == null || names.Length == 0)
				{
					break;
				}

				// create new element objects.
				foreach (string name in names)
				{			
                    BrowseElement element = new BrowseElement();

                    element.Name        = name;
                    element.ItemPath    = null;
                    element.HasChildren = isBranch;

                    string itemID = null;

                    try
                    {
                        if (isBranch)
                        {
                            itemID = GetFullBranchName(name);
                        }
                        else
                        {
                            m_browser.GetItemID(name, out itemID);
                        }
                    }
                    catch
                    {
	                    itemID = null;
                    }
                    
                    element.ItemName = itemID;

                    elements.Add(element);
				}
			}

			// validate items - this is necessary to set the IsItem flag correctly.
			IdentifiedResult[] results = m_server.ValidateItems((ItemIdentifier[])elements.ToArray(typeof(ItemIdentifier)));

			if (results != null)
			{
				for (int ii = 0; ii < results.Length; ii++)
				{
					if (results[ii].ResultID.Succeeded())
					{
						((BrowseElement)elements[ii]).IsItem = true;
					}
				}
			}

			// return results.
			return elements;
		}
		#endregion

		#region Private Members
		private OpcCom.Hda.Server m_server = null;
		private IOPCHDA_Browser m_browser = null;
		private BrowseFilterCollection m_filters = new BrowseFilterCollection();
		private const int BLOCK_SIZE = 10;
		#endregion
	}

	/// <summary>
	/// Stores the state of a browse operation that was halted.
	/// </summary>
	internal class BrowsePosition : Opc.Hda.BrowsePosition
	{
		/// <summary>
		/// Initializes a the object with the browse operation state information.
		/// </summary>
		/// <param name="branchPath">The item id of branch used in the browse operation.</param>
		/// <param name="enumerator">The enumerator used for the browse operation.</param>
		/// <param name="fetchingItems">Whether the enumerator is return branches or items.</param>
		internal BrowsePosition(string branchPath, EnumString enumerator, bool fetchingItems)
		{
			m_branchPath    = branchPath;
			m_enumerator    = enumerator;
			m_fetchingItems = fetchingItems;
		}

		/// <summary>
		/// The item id of the branch being browsed.
		/// </summary>
		internal string BranchPath
		{
			get { return m_branchPath;  }
			set { m_branchPath = value; }
		}

		/// <summary>
		/// The enumerator that was in use when the browse halted.
		/// </summary>
		internal EnumString Enumerator
		{
			get { return m_enumerator;  }
			set { m_enumerator = value; }
		}

		/// <summary>
		/// Whether the browse halted while fetching items.
		/// </summary>
		internal bool FetchingItems
		{
			get { return m_fetchingItems;  }
			set { m_fetchingItems = value; }
		}

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

                    if (m_enumerator != null)
                    {
                        m_enumerator.Dispose();
                        m_enumerator = null;
                    }
                }

                // Release unmanaged resources.
                // Set large fields to null.

                // Call Dispose on your base class.
                m_disposed = true;
            }

            base.Dispose(disposing);
        }

        private bool m_disposed = false;
        #endregion

		#region Private Members
		private string m_branchPath = null;
		private EnumString m_enumerator = null;
		private bool m_fetchingItems = false;
		#endregion
	}
}
