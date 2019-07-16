//============================================================================
// TITLE: Opc.Da.Quality.cs
//
// CONTENTS:
// 
// Classes, enumerations and constants used to describe the DA quality codes.
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
// 2003/11/27 RSA   Created a separate file.
// 2004/02/18 RSA   Updated to conform with the .NET design guidelines.
// 2005/11/24 RSA   Made the Quality structure serializable.

using System;
using System.Text;
using System.Xml;

namespace Opc.Da
{	
	/// <summary>
	/// Defines the possible quality status bits.
	/// </summary>
	public enum qualityBits : int
	{
		/// <remarks/>
		good                       = 0x000000C0,
		/// <remarks/>
		goodLocalOverride          = 0x000000D8,
		/// <remarks/>
		bad                        = 0x00000000,
		/// <remarks/>
		badConfigurationError      = 0x00000004,
		/// <remarks/>
		badNotConnected            = 0x00000008,
		/// <remarks/>
		badDeviceFailure           = 0x0000000c,
		/// <remarks/>
		badSensorFailure           = 0x00000010,
		/// <remarks/>
		badLastKnownValue          = 0x00000014,
		/// <remarks/>
		badCommFailure             = 0x00000018,
		/// <remarks/>
		badOutOfService            = 0x0000001C,
		/// <remarks/>
		badWaitingForInitialData   = 0x00000020,
		/// <remarks/>
		uncertain                  = 0x00000040,
		/// <remarks/>
		uncertainLastUsableValue   = 0x00000044,
		/// <remarks/>
		uncertainSensorNotAccurate = 0x00000050,
		/// <remarks/>
		uncertainEUExceeded        = 0x00000054,
		/// <remarks/>
		uncertainSubNormal         = 0x00000058
	}

	/// <summary>
	/// Defines the possible limit status bits.
	/// </summary>
	public enum limitBits : int
	{		
		/// <remarks/>
		none     = 0x0,
		/// <remarks/>
		low      = 0x1,  
		/// <remarks/>
		high     = 0x2,
		/// <remarks/>
		constant = 0x3
	}

	/// <summary>
	/// Defines bit masks for the quality.
	/// </summary>
	public enum qualityMasks : int
	{
		/// <remarks/>
		qualityMask = +0x00FC,
		/// <remarks/>
		limitMask   = +0x0003,
		/// <remarks/>
		vendorMask  = -0x00FD
	}

	/// <summary>
	/// Contains the quality field for an item value.
	/// </summary>
	[Serializable]
	public struct Quality
	{
		/// <summary>
		/// The value in the quality bits field.
		/// </summary>
		public qualityBits QualityBits
		{
			get { return m_qualityBits;  }
			set { m_qualityBits = value; }
		}

		/// <summary>
		/// The value in the limit bits field.
		/// </summary>
		public limitBits LimitBits
		{
			get { return m_limitBits;  }
			set { m_limitBits = value; }
		}

		/// <summary>
		/// The value in the vendor bits field.
		/// </summary>
		public byte VendorBits
		{
			get { return m_vendorBits;  }
			set { m_vendorBits = value; }
		}
		
		/// <summary>
		/// Returns the quality as a 16 bit integer.
		/// </summary>
		public short GetCode()
		{
			ushort code = 0;

			code |= (ushort)QualityBits;
			code |= (ushort)LimitBits;
			code |= (ushort)(VendorBits<<8);
 
			return (code <= Int16.MaxValue)?(short)code:(short)-((UInt16.MaxValue+1-code));
		}

		/// <summary>
		/// Initializes the quality from a 16 bit integer.
		/// </summary>
		public void SetCode(short code)
		{
			m_qualityBits = (qualityBits)(code & (short)qualityMasks.qualityMask);
			m_limitBits   = (limitBits)(code & (short)qualityMasks.limitMask);
			m_vendorBits  = (byte)((code & (short)qualityMasks.vendorMask)>>8);
		}

		/// <summary>
		/// Returns true if the objects are equal.
		/// </summary>
		public static bool operator==(Quality a, Quality b) 
		{
			return a.Equals(b);
		}

		/// <summary>
		/// Returns true if the objects are not equal.
		/// </summary>
		public static bool operator!=(Quality a, Quality b) 
		{
			return !a.Equals(b);
		}

		#region Constructors
		/// <summary>
		/// Initializes the object with the specified quality.
		/// </summary>
		public Quality(qualityBits quality)
		{
			m_qualityBits = quality;
			m_limitBits   = limitBits.none;
			m_vendorBits  = 0;
		}

		/// <summary>
		/// Initializes the object from the contents of a 16 bit integer.
		/// </summary>
		public Quality(short code)
		{
			m_qualityBits = (qualityBits)(code & (short)qualityMasks.qualityMask);
			m_limitBits   = (limitBits)(code & (short)qualityMasks.limitMask);
			m_vendorBits  = (byte)((code & (short)qualityMasks.vendorMask)>>8);
		}
		#endregion

		#region Object Member Overrides
		/// <summary>
		/// Converts a quality to a string with the format: 'quality[limit]:vendor'.
		/// </summary>
		public override string ToString()
		{
			string text = QualityBits.ToString();

			if (LimitBits != limitBits.none)
			{
				text += String.Format("[{0}]", LimitBits.ToString());
			}

			if (VendorBits != 0)
			{
				text += String.Format(":{0,0:X}", VendorBits);
			}

			return text;
		}

		/// <summary>
		/// Determines whether the specified Object is equal to the current Quality
		/// </summary>
		public override bool Equals(object target)
		{
			if (target == null || target.GetType() != typeof(Quality)) return false;

			Quality quality = (Quality)target;

			if (QualityBits != quality.QualityBits) return false;
			if (LimitBits   != quality.LimitBits)   return false;
			if (VendorBits  != quality.VendorBits)  return false;
			
			return true;
		}

		/// <summary>
		/// Returns hash code for the current Quality.
		/// </summary>
		public override int GetHashCode()
		{
			return GetCode();
		}
		#endregion

		#region Private Members
		private qualityBits m_qualityBits;
		private limitBits m_limitBits;
		private byte m_vendorBits;
		#endregion

		/// <summary>
		/// A 'good' quality value.
		/// </summary>
		public static readonly Quality Good = new Quality(qualityBits.good);

		/// <summary>
		/// An 'bad' quality value.
		/// </summary>
		public static readonly Quality Bad  = new Quality(qualityBits.bad);
	}
}
