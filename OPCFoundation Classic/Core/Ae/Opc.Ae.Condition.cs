 //============================================================================
// TITLE: Opc.Ae.ConditionState.cs
//
// CONTENTS:
// 
// Classes used to store information related to event conditionStates.
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

using System;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using Opc;

namespace Opc.Ae
{	
	#region Condition Class
	/// <summary>
	/// The description of an item condition state supported by the server.
	/// </summary>
	[Serializable]
	public class Condition : ICloneable
	{
		#region Public Interface
		/// <summary>
		/// A bit mask indicating the current state of the condition
		/// </summary>
		public int State
		{
			get { return m_state;  } 
			set { m_state = value; } 
		}

		/// <summary>
		/// The currently active sub-condition, for multi-state conditions which are active. 
		/// For a single-state condition, this contains the information about the condition itself.
		/// For inactive conditions, this value is null.
		/// </summary>
		public SubCondition ActiveSubCondition
		{
			get { return m_activeSubcondition;  } 
			set { m_activeSubcondition = value; } 
		}

		/// <summary>
		/// The quality associated with the condition state.
		/// </summary>
		public Opc.Da.Quality Quality
		{
			get { return m_quality;  } 
			set { m_quality = value; } 
		}

		/// <summary>
		/// The time of the most recent acknowledgment of this condition (of any sub-condition).
		/// </summary>
		public DateTime LastAckTime
		{
			get { return m_lastAckTime;  } 
			set { m_lastAckTime = value; } 
		}

		/// <summary>
		/// Time of the most recent transition into active sub-condition. 
		/// This is the time value which must be specified when acknowledging the condition. 
		/// If the condition has never been active, this value is DateTime.MinValue.
		/// </summary>
		public DateTime SubCondLastActive
		{
			get { return m_subCondLastActive;  } 
			set { m_subCondLastActive = value; } 
		}

		/// <summary>
		/// Time of the most recent transition into the condition. 
		/// There may be transitions among the sub-conditions which are more recent. 
		/// If the condition has never been active, this value is DateTime.MinValue.
		/// </summary>
		public DateTime CondLastActive
		{
			get { return m_condLastActive;  } 
			set { m_condLastActive = value; } 
		}

		/// <summary>
		/// Time of the most recent transition out of this condition. 
		/// This value is DateTime.MinValue if the condition has never been active, 
		/// or if it is currently active for the first time and has never been exited.
		/// </summary>
		public DateTime CondLastInactive
		{
			get { return m_condLastInactive;  } 
			set { m_condLastInactive = value; } 
		}

		/// <summary>
		/// This is the ID of the client who last acknowledged this condition. 
		/// This value is null if the condition has never been acknowledged.
		/// </summary>
		public string AcknowledgerID
		{
			get { return m_acknowledgerID;  } 
			set { m_acknowledgerID = value; } 
		}

		/// <summary>
		/// The comment string passed in by the client who last acknowledged this condition.
		/// This value is null if the condition has never been acknowledged.
		/// </summary>
		public string Comment
		{
			get { return m_comment;  } 
			set { m_comment = value; } 
		}

		/// <summary>
		/// The sub-conditions defined for this condition. 
		/// For single-state conditions, the collection will contain one element, the value of which describes the condition.
		/// </summary>
		public SubConditionCollection SubConditions
		{
			get { return m_subconditions; } 
		}

		/// <summary>
		/// The values of the attributes requested for this condition. 
		/// </summary>
		public AttributeValueCollection Attributes
		{
			get { return m_attributes; } 
		}

		#region AttributeCollection Class
		/// <summary>
		/// Contains a read-only collection of AttributeValues.
		/// </summary>
		public class AttributeValueCollection : WriteableCollection
		{			
			/// <summary>
			/// An indexer for the collection.
			/// </summary>
			public new AttributeValue this[int index]
			{
				get	{ return (AttributeValue)Array[index]; }
			}

			/// <summary>
			/// Returns a copy of the collection as an array.
			/// </summary>
			public new AttributeValue[] ToArray()
			{
				return (AttributeValue[])Array.ToArray();
			}

			/// <summary>
			/// Creates an empty collection.
			/// </summary>
			internal AttributeValueCollection() : base(null, typeof(AttributeValue)) {}
		}
		#endregion
		
		#region SubConditionCollection Class
		/// <summary>
		/// Contains a read-only collection of SubConditions.
		/// </summary>
		public class SubConditionCollection : WriteableCollection
		{			
			/// <summary>
			/// An indexer for the collection.
			/// </summary>
			public new SubCondition this[int index]
			{
				get	{ return (SubCondition)Array[index]; }
			}

			/// <summary>
			/// Returns a copy of the collection as an array.
			/// </summary>
			public new SubCondition[] ToArray()
			{
				return (SubCondition[])Array.ToArray();
			}

			/// <summary>
			/// Creates an empty collection.
			/// </summary>
			internal SubConditionCollection() : base(null, typeof(SubCondition)) {}
		}
		#endregion

		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a deep copy of the object.
		/// </summary>
		public virtual object Clone() 
		{ 
			Condition clone = (Condition)MemberwiseClone(); 

			clone.m_activeSubcondition = (SubCondition)this.m_activeSubcondition.Clone();
			clone.m_subconditions      = (SubConditionCollection)this.m_subconditions.Clone();
			clone.m_attributes         = (AttributeValueCollection)this.m_attributes.Clone();

			return clone;
		}
		#endregion
		
		#region Private Members
		private int m_state = 0;
		private SubCondition m_activeSubcondition = new SubCondition();
		private Opc.Da.Quality m_quality = Opc.Da.Quality.Bad;
		private DateTime m_lastAckTime = DateTime.MinValue;
		private DateTime m_subCondLastActive = DateTime.MinValue;
		private DateTime m_condLastActive = DateTime.MinValue;
		private DateTime m_condLastInactive = DateTime.MinValue;
		private string m_acknowledgerID = null;
		private string m_comment = null;
		private SubConditionCollection m_subconditions = new SubConditionCollection();
		private AttributeValueCollection m_attributes = new AttributeValueCollection();
		#endregion
	}
	#endregion

	#region ConditionState Enumeration
	/// <summary>
	/// The possible states for a condition.
	/// </summary>
	[Flags]	
	public enum ConditionState
	{
		/// <summary>
		/// The server is currently checking the state of the condition.
		/// </summary>
		Enabled = 0x0001,

		/// <summary>
		/// The associated object is in the state represented by the condition.
		/// </summary>
		Active = 0x0002,

		/// <summary>
		/// The condition has been acknowledged.
		/// </summary>
		Acknowledged = 0x0004
	}
	#endregion

	#region SubCondition Class
	/// <summary>
	/// The description of an item sub-condition supported by the server.
	/// </summary>
	[Serializable]
	public class SubCondition : ICloneable
	{
		#region Public Interface
		/// <summary>
		/// The name assigned to the sub-condition.
		/// </summary>
		public string Name
		{
			get { return m_name;  } 
			set { m_name = value; } 
		}

		/// <summary>
		/// A server-specific expression which defines the sub-state represented by the sub-condition.
		/// </summary>
		public string Definition
		{
			get { return m_definition;  } 
			set { m_definition = value; } 
		}

		/// <summary>
		/// The severity of any event notifications generated on behalf of this sub-condition.
		/// </summary>
		public int Severity
		{
			get { return m_severity;  } 
			set { m_severity = value; } 
		}

		/// <summary>
		/// The text string to be included in any event notification generated on behalf of this sub-condition.
		/// </summary>
		public string Description
		{
			get { return m_description;  } 
			set { m_description = value; } 
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Name;
		}
		#endregion

		#region ICloneable Members
		/// <summary>
		/// Creates a shallow copy of the object.
		/// </summary>
		public virtual object Clone() { return MemberwiseClone(); }
		#endregion
		
		#region Private Members
		private string m_name        = null;
		private string m_definition  = null;
		private int    m_severity    = 1;
		private string m_description = null;
		#endregion
	}
	#endregion
}
