//============================================================================
// TITLE: Opc.Ae.IBrowser.cs
//
// CONTENTS:
// 
// Classes that are used when browsing an AE server's address space.
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
// Aete       By    Notes
// ---------- ---   -----
// 2004/11/08 RSA   Initial implementation.

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;

namespace Opc.Ae
{	
	#region BrowseElement Class
	/// <summary>
	/// Contains a description of an element in the server address space.
	/// </summary>
	[Serializable]
	public class BrowseElement
	{
		#region Public Interface
		/// <summary>
		/// A descriptive name for element that is unique within a branch.
		/// </summary>
		public string Name
		{
			get { return m_name;  } 
			set { m_name = value; } 
		}

		/// <summary>
		/// The fully qualified name for the element.
		/// </summary>
		public string QualifiedName
		{
			get { return m_qualifiedName;  } 
			set { m_qualifiedName = value; } 
		}

		/// <summary>
		/// Whether the element is a source or an area.
		/// </summary>
		public BrowseType NodeType
		{
			get { return m_nodeType;  } 
			set { m_nodeType = value; } 
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return MemberwiseClone();
		}
		#endregion

		#region Private Members
		private string m_name = null;
		private string m_qualifiedName = null;
		private BrowseType m_nodeType = BrowseType.Area;
		#endregion
	};
	#endregion

	#region BrowsePosition Class
	/// <summary>
	/// Stores the state of a browse operation.
	/// </summary>
	[Serializable]
	public class BrowsePosition : IBrowsePosition
	{
		#region Constructors
		/// <summary>
		/// Saves the parameters for an incomplete browse information.
		/// </summary>
		public BrowsePosition(
			string     areaID,
			BrowseType browseType, 
			string     browseFilter)
		{
			m_areaID       = areaID;
			m_browseType   = browseType;
			m_browseFilter = browseFilter;
		}
		#endregion

		#region Public Interface
		/// <summary>
		/// The fully qualified id for the area being browsed.
		/// </summary>
		public string AreaID
		{
			get { return m_areaID; }
		}

		/// <summary>
		/// The type of child element being returned with the browse.
		/// </summary>
		public BrowseType BrowseType
		{
			get { return m_browseType; }
		}

		/// <summary>
		/// The filter applied to the name of the elements being returned.
		/// </summary>
		public string BrowseFilter
		{
			get { return m_browseFilter; }
		}
		#endregion

        #region IDisposable Members
        /// <summary>
        /// The finalizer.
        /// </summary>
        ~BrowsePosition()
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
                m_disposed = true;
            }
        }

        private bool m_disposed = false;
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a shallow copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			return (BrowsePosition)MemberwiseClone();
		}
		#endregion

		#region Private Members
		private string m_areaID = null;
		private BrowseType m_browseType = BrowseType.Area;
		private	string m_browseFilter = null;
		#endregion
	}
	#endregion

	#region BrowseType Enumeration
	/// <summary>
	/// The type of nodes to return during a browse.
	/// </summary>
	public enum BrowseType 
	{               
		/// <summary>
		/// Return only nodes that are process areas.
		/// </summary>
		Area,
        
		/// <summary>
		/// Return only nodes that are event soucres.
		/// </summary>
		Source
	}
	#endregion
}