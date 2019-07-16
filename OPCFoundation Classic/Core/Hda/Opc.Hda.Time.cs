//============================================================================
// TITLE: Opc.Hda.Time.cs
//
// CONTENTS:
// 
// Classes used to represent absolute or relative instances in time.
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
// 2004/01/03 RSA   Initial implementation.

using System;
using System.Text;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace Opc.Hda
{	
	/// <summary>
	/// A time specified as either an absolute or relative value.
	/// </summary>
	[Serializable]
	public class Time
	{
		/// <summary>
		/// Initializes the object with its default values.
		/// </summary>
		public Time() {}

		/// <summary>
		/// Initializes the object with an absolute time.
		/// </summary>
		/// <param name="time">The absolute time.</param>
		public Time(DateTime time)
		{
			AbsoluteTime = time;
		}

		/// <summary>
		/// Initializes the object with a relative time.
		/// </summary>
		/// <param name="time">The relative time.</param>
		public Time(string time)
		{
			Time value = Parse(time);
			
			m_absoluteTime = DateTime.MinValue;
			m_baseTime     = value.m_baseTime;
			m_offsets      = value.m_offsets;
		}

		/// <summary>
		/// Whether the time is a relative or absolute time.
		/// </summary>
		public bool IsRelative
		{
			get { return (m_absoluteTime == DateTime.MinValue); }
			set { m_absoluteTime = DateTime.MinValue; }
		}

		/// <summary>
		/// The time as abolute UTC value.
		/// </summary>
		public DateTime AbsoluteTime
		{
			get { return m_absoluteTime;  }
			set { m_absoluteTime = value; }
		}

		/// <summary>
		/// The base for a relative time value.
		/// </summary>
		public RelativeTime BaseTime
		{
			get { return m_baseTime; }
			set { m_baseTime = value; }
		}

		/// <summary>
		/// The set of offsets to be applied to the base of a relative time.
		/// </summary>
		public TimeOffsetCollection Offsets
		{
			get { return m_offsets; }
		}
		
		/// <summary>
		/// Converts a relative time to an absolute time by using the system clock.
		/// </summary>
		public DateTime ResolveTime()
		{
			// nothing special to do for absolute times.
			if (!IsRelative)
			{
				return m_absoluteTime;
			}

			// get local time from the system.
			DateTime time = DateTime.UtcNow;

			int years        = time.Year;
			int months       = time.Month;
			int days         = time.Day;
			int hours        = time.Hour;
			int minutes      = time.Minute;
			int seconds      = time.Second;
			int milliseconds = time.Millisecond;

			// move to the beginning of the period indicated by the base time.
			switch (BaseTime)
			{
				case RelativeTime.Year:
				{
					months       = 0;
					days         = 0;
					hours        = 0;
					minutes      = 0;
					seconds      = 0;
					milliseconds = 0;
					break;
				}

				case RelativeTime.Month:
				{
					days         = 0;
					hours        = 0;
					minutes      = 0;
					seconds      = 0;
					milliseconds = 0;
					break;
				}

				case RelativeTime.Week:
				case RelativeTime.Day:
				{
					hours        = 0;
					minutes      = 0;
					seconds      = 0;
					milliseconds = 0;
					break;
				}

				case RelativeTime.Hour:
				{
					minutes      = 0;
					seconds      = 0;
					milliseconds = 0;
					break;
				}

				case RelativeTime.Minute:
				{
					seconds      = 0;
					milliseconds = 0;
					break;
				}

				case RelativeTime.Second:
				{
					milliseconds = 0;
					break;
				}
			}

			// contruct base time.
			time = new DateTime(years, months, days, hours, minutes, seconds, milliseconds);

			// adjust to beginning of week.
			if (BaseTime == RelativeTime.Week && time.DayOfWeek != DayOfWeek.Sunday)
			{
				time = time.AddDays(-((int)time.DayOfWeek));
			}

			// add offsets.
			foreach (TimeOffset offset in Offsets)
			{
				switch (offset.Type)
				{
					case RelativeTime.Year:   { time = time.AddYears(offset.Value);   break; }
					case RelativeTime.Month:  { time = time.AddMonths(offset.Value);  break; }
					case RelativeTime.Week:   { time = time.AddDays(offset.Value*7);  break; }
					case RelativeTime.Day:    { time = time.AddDays(offset.Value);    break; }
					case RelativeTime.Hour:   { time = time.AddHours(offset.Value);   break; }
					case RelativeTime.Minute: { time = time.AddMinutes(offset.Value); break; }
					case RelativeTime.Second: { time = time.AddSeconds(offset.Value); break; }
				}
			}

			// return resolved time.
			return time;
		}

		/// <summary>
		/// Returns a String that represents the current Object.
		/// </summary>
		/// <returns>A String that represents the current Object.</returns>
		public override string ToString()
		{
			if (!IsRelative)
			{
				return Opc.Convert.ToString(m_absoluteTime);
			}

			StringBuilder buffer = new StringBuilder(256);

			buffer.Append(BaseTypeToString(BaseTime));
			buffer.Append(Offsets.ToString());

			return buffer.ToString();
		}

		/// <summary>
		/// Parses a string representation of a time.
		/// </summary>
		/// <param name="buffer">The string representation to parse.</param>
		/// <returns>A Time object initailized with the string.</returns>
		public static Time Parse(string buffer)
		{
			// remove trailing and leading white spaces.
			buffer = buffer.Trim();
			
			Time time = new Time();

			// determine if string is a relative time.
			bool isRelative = false;

			foreach (RelativeTime baseTime in Enum.GetValues(typeof(RelativeTime)))
			{
				string token = BaseTypeToString(baseTime);

				if (buffer.StartsWith(token))
				{
					buffer = buffer.Substring(token.Length).Trim();
					time.BaseTime = baseTime;
					isRelative = true;
					break;
				}
			}

			// parse an absolute time string.
			if (!isRelative)
			{
				time.AbsoluteTime = System.Convert.ToDateTime(buffer).ToUniversalTime();
				return time;
			}

			// parse the offset portion of the relative time.
			if (buffer.Length > 0)
			{
				time.Offsets.Parse(buffer);
			}

			return time;
		}

		#region Private Members
		/// <summary>
		/// Converts a base time to a string token.
		/// </summary>
		/// <param name="baseTime">The base time value to convert.</param>
		/// <returns>The string token representing the base time.</returns>
		private static string BaseTypeToString(RelativeTime baseTime)
		{
			switch (baseTime)
			{
				case RelativeTime.Now:    { return "NOW"; }
				case RelativeTime.Second: { return "SECOND"; }
				case RelativeTime.Minute: { return "MINUTE"; }
				case RelativeTime.Hour:   { return "HOUR"; }
				case RelativeTime.Day:    { return "DAY"; }
				case RelativeTime.Week:   { return "WEEK"; }
				case RelativeTime.Month:  { return "MONTH"; }
				case RelativeTime.Year:   { return "YEAR"; }
			}

			throw new ArgumentOutOfRangeException("baseTime", baseTime.ToString(), "Invalid value for relative base time.");
		}
		#endregion
        		
		#region Private Members
		private DateTime m_absoluteTime = DateTime.MinValue;
		private RelativeTime m_baseTime = RelativeTime.Now;
		private TimeOffsetCollection m_offsets = new TimeOffsetCollection();
		#endregion
	}

	/// <summary>
	/// Possible base or offset types for relative times.
	/// </summary>
	public enum RelativeTime
	{
		/// <summary>
		/// Start from the current time.
		/// </summary>
		Now,

		/// <summary>
		/// The start of the current second or an offset in seconds.
		/// </summary>
		Second,
		
		/// <summary>
		/// The start of the current minutes or an offset in minutes.
		/// </summary>
		Minute,
		
		/// <summary>
		/// The start of the current hour or an offset in hours.
		/// </summary>
		Hour,
		
		/// <summary>
		/// The start of the current day or an offset in days.
		/// </summary>
		Day,
		
		/// <summary>
		/// The start of the current week or an offset in weeks.
		/// </summary>
		Week,
		
		/// <summary>
		/// The start of the current month or an offset in months.
		/// </summary>
		Month,
		
		/// <summary>
		/// The start of the current year or an offset in years.
		/// </summary>
		Year
	}
	
	/// <summary>
	/// An offset component of a relative time.
	/// </summary>
	[Serializable]
	public struct TimeOffset
	{
		/// <summary>
		/// A signed value indicated the magnitude of the time offset.
		/// </summary>
		public int Value
		{
			get { return m_value;  }
			set { m_value = value; }
		}

		/// <summary>
		/// The time interval to use when applying the offset.
		/// </summary>
		public RelativeTime Type
		{
			get { return m_type;  }
			set { m_type = value; }
		}

		/// <summary>
		/// Converts a offset type to a string token.
		/// </summary>
		/// <param name="offsetType">The offset type value to convert.</param>
		/// <returns>The string token representing the offset type.</returns>
		internal static string OffsetTypeToString(RelativeTime offsetType)
		{
			switch (offsetType)
			{
				case RelativeTime.Second: { return "S"; }
				case RelativeTime.Minute: { return "M"; }
				case RelativeTime.Hour:   { return "H"; }
				case RelativeTime.Day:    { return "D"; }
				case RelativeTime.Week:   { return "W"; }
				case RelativeTime.Month:  { return "MO"; }
				case RelativeTime.Year:   { return "Y"; }
			}

			throw new ArgumentOutOfRangeException("offsetType", offsetType.ToString(), "Invalid value for relative time offset type.");
		}

		#region Private Members
		private int m_value;
		private RelativeTime m_type;
		#endregion
	}

	/// <summary>
	/// A collection of time offsets used in a relative time.
	/// </summary>
	[Serializable]
	public class TimeOffsetCollection : ArrayList
	{
		/// <summary>
		/// Accessor for elements in the time offset collection.
		/// </summary>
		public new TimeOffset this[int index]
		{
			get { return this[index];  }
			set { this[index] = value; }
		}

		/// <summary>
		/// Adds a new offset to the collection.
		/// </summary>
		/// <param name="value">The offset value.</param>
		/// <param name="type">The offset type.</param>
		public int Add(int value, RelativeTime type)
		{
			TimeOffset offset = new TimeOffset();

			offset.Value = value;
			offset.Type  = type;

			return base.Add(offset);
		}

		/// <summary>
		/// Returns a String that represents the current Object.
		/// </summary>
		/// <returns>A String that represents the current Object.</returns>
		public override string ToString()
		{
			StringBuilder buffer = new StringBuilder(256);

			foreach (TimeOffset offset in (ICollection)this)
			{
				if (offset.Value >= 0)
				{
					buffer.Append("+");
				}
				
				buffer.AppendFormat("{0}", offset.Value);
				buffer.Append(TimeOffset.OffsetTypeToString(offset.Type));
			}

			return buffer.ToString();
		}

		/// <summary>
		/// Initializes the collection from a set of offsets contained in a string. 
		/// </summary>
		/// <param name="buffer">A string containing the time offset fields.</param>
		public void Parse(string buffer)
		{
			// clear existing offsets.
			Clear();

			// parse the offsets.
			bool   positive  = true;
			int    magnitude = 0;
			string units     = "";
			int    state     = 0;

			// state = 0 - looking for start of next offset field.
			// state = 1 - looking for beginning of offset value.
			// state = 2 - reading offset value.
			// state = 3 - reading offset type.

			for (int ii = 0; ii < buffer.Length; ii++)
			{
				// check for sign part of the offset field.
				if (buffer[ii] == '+' || buffer[ii] == '-')
				{
					if (state == 3)
					{
						Add(CreateOffset(positive, magnitude, units));
						
						magnitude = 0;
						units     = "";
						state     = 0;
					}

					if (state != 0)
					{
						throw new FormatException("Unexpected token encountered while parsing relative time string."); 
					}

					positive = buffer[ii] == '+';
					state    = 1;
				}

				// check for integer part of the offset field.
				else if (Char.IsDigit(buffer, ii))
				{
					if (state == 3)
					{
						Add(CreateOffset(positive, magnitude, units));

						magnitude = 0;
						units     = "";
						state     = 0;
					}

					if (state != 0 && state != 1 && state != 2)
					{
						throw new FormatException("Unexpected token encountered while parsing relative time string."); 
					}

					magnitude *= 10;
					magnitude += System.Convert.ToInt32(buffer[ii] - '0');
					
					state = 2;
				}

				// check for units part of the offset field.
				else if (!Char.IsWhiteSpace(buffer, ii))
				{
					if (state != 2 && state != 3)
					{
						throw new FormatException("Unexpected token encountered while parsing relative time string."); 
					}
				
					units += buffer[ii];
					state = 3;
				}
			}

			// process final field.
			if (state == 3)
			{
				Add(CreateOffset(positive, magnitude, units));
				state = 0;
			}
			
			// check final state.
			if (state != 0)
			{
				throw new FormatException("Unexpected end of string encountered while parsing relative time string."); 
			}
		}

		#region ICollection Members
		/// <summary>
		/// Copies the objects to an Array, starting at a the specified index.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination for the objects.</param>
		/// <param name="index">The zero-based index in the Array at which copying begins.</param>
		public void CopyTo(TimeOffset[] array, int index)
		{
			CopyTo((Array)array, index);
		}
		#endregion

		#region IList Members
		/// <summary>
		/// Inserts an item to the IList at the specified position.
		/// </summary>
		/// <param name="index">The zero-based index at which value should be inserted.</param>
		/// <param name="value">The Object to insert into the IList. </param>
		public void Insert(int index, TimeOffset value)
		{
			Insert(index, (object)value);
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the IList.
		/// </summary>
		/// <param name="value">The Object to remove from the IList.</param>
		public void Remove(TimeOffset value)
		{
			Remove((object)value);
		}

		/// <summary>
		/// Determines whether the IList contains a specific value.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>true if the Object is found in the IList; otherwise, false.</returns>
		public bool Contains(TimeOffset value)
		{
			return Contains((object)value);
		}

		/// <summary>
		/// Determines the index of a specific item in the IList.
		/// </summary>
		/// <param name="value">The Object to locate in the IList.</param>
		/// <returns>The index of value if found in the list; otherwise, -1.</returns>
		public int IndexOf(TimeOffset value)
		{
			return IndexOf((object)value);
		}

		/// <summary>
		/// Adds an item to the IList.
		/// </summary>
		/// <param name="value">The Object to add to the IList. </param>
		/// <returns>The position into which the new element was inserted.</returns>
		public int Add(TimeOffset value)
		{
			return Add((object)value);
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Creates a new offset object from the components extracted from a string.
		/// </summary>
		private static TimeOffset CreateOffset(bool positive, int magnitude, string units)
		{
			foreach (RelativeTime offsetType in Enum.GetValues(typeof(RelativeTime)))
			{
				if (offsetType == RelativeTime.Now)
				{
					continue;
				}

				if (units == TimeOffset.OffsetTypeToString(offsetType))
				{
					TimeOffset offset = new TimeOffset();

					offset.Value = (positive)?magnitude:-magnitude;
					offset.Type  = offsetType;

					return offset;
				}
			}

			throw new ArgumentOutOfRangeException("units", units, "String is not a valid offset time type.");
		}
		#endregion
	}
}
