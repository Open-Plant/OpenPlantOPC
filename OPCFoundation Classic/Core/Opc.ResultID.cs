//============================================================================
// TITLE: Opc.Result.cs
//
// CONTENTS:
// 
// Defines static information for well known error/success codes.
//
// (c) Copyright 2002-2004 The OPC Foundation
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
// 2003/04/03 RSA   Initial implementation.
// 2004/12/18 RSA   Ensured that GetHashCode() returns the same value if Equals() is true.
// 2005/11/24 RSA   Made the ResultID structure serializable.

using System;
using System.Xml;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;

namespace Opc
{
	/// <summary>
	/// Contains a unique identifier for a result code.
	/// </summary>
	[Serializable]
	public struct ResultID : ISerializable
	{
		#region Serialization Functions
		/// <summary>
		/// A set of names for fields used in serialization.
		/// </summary>
		private class Names
		{
			internal const string NAME      = "NA";
			internal const string NAMESPACE = "NS";
			internal const string CODE      = "CO";
		}

		//MP During deserialization, SerializationInfo is passed to the class using the constructor provided for this purpose. Any visibility 
		// constraints placed on the constructor are ignored when the object is deserialized; so you can mark the class as public, 
		// protected, internal, or private. However, it is best practice to make the constructor protected unless the class is sealed, in which case 
		// the constructor should be marked private. The constructor should also perform thorough input validation. To avoid misuse by malicious code, 
		// the constructor should enforce the same security checks and permissions required to obtain an instance of the class using any other 
		// constructor. 
		/// <summary>
		/// Contructs a server by de-serializing its URL from the stream.
		/// </summary>
		private ResultID(SerializationInfo info, StreamingContext context)
		{
			string name = (string)info.GetValue(Names.NAME, typeof(string));
			string ns   = (string)info.GetValue(Names.NAMESPACE, typeof(string));
			m_name = new XmlQualifiedName(name, ns);
			m_code = (int)info.GetValue(Names.CODE, typeof(int));
		}

		/// <summary>
		/// Serializes a server into a stream.
		/// </summary>
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (m_name != null)
			{
				info.AddValue(Names.NAME, m_name.Name);
				info.AddValue(Names.NAMESPACE, m_name.Namespace);
			}
			info.AddValue(Names.CODE, m_code);
		}
		#endregion
    
		/// <summary>
		/// Used for result codes identified by a qualified name.
		/// </summary>
		public XmlQualifiedName Name 
		{
			get{ return m_name; }
		}

		/// <summary>
		/// Used for result codes identified by a integer.
		/// </summary>
		public int Code
		{
			get{ return m_code; }
		}
		
		/// <summary>
		/// Returns true if the objects are equal.
		/// </summary>
		public static bool operator==(ResultID a, ResultID b) 
		{
			return a.Equals(b);
		}

		/// <summary>
		/// Returns true if the objects are not equal.
		/// </summary>
		public static bool operator!=(ResultID a, ResultID b) 
		{
			return !a.Equals(b);
		}

		/// <summary>
		/// Checks for the 'S_' prefix that indicates a success condition.
		/// </summary>
		public bool Succeeded()
		{
			if (Code != -1)   return (Code >= 0);
			if (Name != null) return Name.Name.StartsWith("S_");
			return false;
		}

		/// <summary>
		/// Checks for the 'E_' prefix that indicates an error condition.
		/// </summary>
		public bool Failed()
		{
			if (Code != -1)   return (Code < 0);
			if (Name != null) return Name.Name.StartsWith("E_");
			return false;
		}

		#region Constructors
		/// <summary>
		/// Initializes a result code identified by a qualified name.
		/// </summary>
		public ResultID(XmlQualifiedName name) 
		{ 
			m_name = name; 
			m_code = -1; 
		}

		/// <summary>
		/// Initializes a result code identified by an integer.
		/// </summary>
		public ResultID(long code) 
		{ 
			m_name = null; 
			
			if (code > Int32.MaxValue)
			{
				code = -(((long)UInt32.MaxValue)+1-code);
			}

			m_code = (int)code;
		}

		/// <summary>
		/// Initializes a result code identified by a qualified name.
		/// </summary>
		public ResultID(string name, string ns) 
		{ 
			m_name = new XmlQualifiedName(name, ns); 
			m_code = -1;
		}

		/// <summary>
		/// Initializes a result code with a general result code and a specific result code.
		/// </summary>
		public ResultID(ResultID resultID, long code) 
		{ 
			m_name = resultID.Name; 

			if (code > Int32.MaxValue)
			{
				code = -(((long)UInt32.MaxValue)+1-code);
			}

			m_code = (int)code;
		}
		#endregion

		#region Object Method Overrides
		/// <summary>
		/// Returns true if the target object is equal to the object.
		/// </summary>
		public override bool Equals(object target)
		{
			if (target != null && target.GetType() == typeof(ResultID))
			{
				ResultID resultID = (ResultID)target;

				// compare by integer if both specify valid integers.
				if (resultID.Code != -1 && Code != -1)
				{
					return (resultID.Code == Code) && (resultID.Name == Name); 
				}

				// compare by name if both specify valid names.
				if (resultID.Name != null && Name != null)
				{
					return (resultID.Name == Name);
				}
			}

			return false;
		}

		/// <summary>
		/// Formats the result identifier as a string.
		/// </summary>
		public override string ToString()
		{
			if (Name != null) return Name.Name;
			return String.Format("0x{0,0:X}", Code);
		}

		/// <summary>
		/// Returns a useful hash code for the object.
		/// </summary>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		#endregion

		#region Private Members
		private XmlQualifiedName m_name;
		private int m_code;
		#endregion

		/// <remarks/>
		public static readonly ResultID S_OK                       = new ResultID("S_OK",                       Namespace.OPC_DATA_ACCESS);
		/// <remarks/>
		public static readonly ResultID S_FALSE                    = new ResultID("S_FALSE",                    Namespace.OPC_DATA_ACCESS);
		/// <remarks/>
		public static readonly ResultID E_FAIL                     = new ResultID("E_FAIL",                     Namespace.OPC_DATA_ACCESS);
		/// <remarks/>
		public static readonly ResultID E_INVALIDARG               = new ResultID("E_INVALIDARG",               Namespace.OPC_DATA_ACCESS);
		/// <remarks/>
		public static readonly ResultID E_TIMEDOUT                 = new ResultID("E_TIMEDOUT",                 Namespace.OPC_DATA_ACCESS);
		/// <remarks/>
		public static readonly ResultID E_OUTOFMEMORY              = new ResultID("E_OUTOFMEMORY",              Namespace.OPC_DATA_ACCESS);
		/// <remarks/>
		public static readonly ResultID E_NETWORK_ERROR            = new ResultID("E_NETWORK_ERROR",            Namespace.OPC_DATA_ACCESS);
		/// <remarks/>
		public static readonly ResultID E_ACCESS_DENIED            = new ResultID("E_ACCESS_DENIED",            Namespace.OPC_DATA_ACCESS);
		/// <remarks/>
		public static readonly ResultID E_NOTSUPPORTED             = new ResultID("E_NOTSUPPORTED",             Namespace.OPC_DATA_ACCESS);

		/// <summary>
		/// Results codes for Data Access.
		/// </summary>
		public class Da
		{
			/// <remarks/>
			public static readonly ResultID S_DATAQUEUEOVERFLOW        = new ResultID("S_DATAQUEUEOVERFLOW",        Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID S_UNSUPPORTEDRATE          = new ResultID("S_UNSUPPORTEDRATE",          Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID S_CLAMP                    = new ResultID("S_CLAMP",                    Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_INVALIDHANDLE            = new ResultID("E_INVALIDHANDLE",            Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_UNKNOWN_ITEM_NAME        = new ResultID("E_UNKNOWN_ITEM_NAME",        Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_INVALID_ITEM_NAME        = new ResultID("E_INVALID_ITEM_NAME",        Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_UNKNOWN_ITEM_PATH        = new ResultID("E_UNKNOWN_ITEM_PATH",        Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_INVALID_ITEM_PATH        = new ResultID("E_INVALID_ITEM_PATH",        Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_INVALID_PID              = new ResultID("E_INVALID_PID",              Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_READONLY                 = new ResultID("E_READONLY",                 Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_WRITEONLY                = new ResultID("E_WRITEONLY",                Namespace.OPC_DATA_ACCESS);
			/// <remarks/> 
			public static readonly ResultID E_BADTYPE                  = new ResultID("E_BADTYPE",                  Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_RANGE                    = new ResultID("E_RANGE",                    Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_INVALID_FILTER           = new ResultID("E_INVALID_FILTER",           Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_INVALIDCONTINUATIONPOINT = new ResultID("E_INVALIDCONTINUATIONPOINT", Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_NO_WRITEQT               = new ResultID("E_NO_WRITEQT",               Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_NO_ITEM_DEADBAND         = new ResultID("E_NO_ITEM_DEADBAND",         Namespace.OPC_DATA_ACCESS);
			/// <remarks/> 
			public static readonly ResultID E_NO_ITEM_SAMPLING         = new ResultID("E_NO_ITEM_SAMPLING",         Namespace.OPC_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_NO_ITEM_BUFFERING        = new ResultID("E_NO_ITEM_BUFFERING",        Namespace.OPC_DATA_ACCESS);
		}

		/// <summary>
		/// Results codes for Complex Data.
		/// </summary>
		public class Cpx
		{			
			/// <remarks/>
			public static readonly ResultID E_TYPE_CHANGED             = new ResultID("E_TYPE_CHANGED",             Namespace.OPC_COMPLEX_DATA);
			/// <remarks/>
			public static readonly ResultID E_FILTER_DUPLICATE         = new ResultID("E_FILTER_DUPLICATE",         Namespace.OPC_COMPLEX_DATA);
			/// <remarks/>
			public static readonly ResultID E_FILTER_INVALID           = new ResultID("E_FILTER_INVALID",           Namespace.OPC_COMPLEX_DATA);
			/// <remarks/>
			public static readonly ResultID E_FILTER_ERROR             = new ResultID("E_FILTER_ERROR",             Namespace.OPC_COMPLEX_DATA);
			/// <remarks/>
			public static readonly ResultID S_FILTER_NO_DATA           = new ResultID("S_FILTER_NO_DATA",           Namespace.OPC_COMPLEX_DATA);
		}

		/// <summary>
		/// Results codes for Historical Data Access.
		/// </summary>
		public class Hda
		{
			/// <remarks/>
			public static readonly ResultID E_MAXEXCEEDED      = new ResultID("E_MAXEXCEEDED",       Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID S_NODATA           = new ResultID("S_NODATA",            Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID S_MOREDATA         = new ResultID("S_MOREDATA",          Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_INVALIDAGGREGATE = new ResultID("E_INVALIDAGGREGATE",  Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID S_CURRENTVALUE     = new ResultID("S_CURRENTVALUE",      Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID S_EXTRADATA        = new ResultID("S_EXTRADATA",         Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID W_NOFILTER         = new ResultID("W_NOFILTER",          Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_UNKNOWNATTRID    = new ResultID("E_UNKNOWNATTRID",     Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_NOT_AVAIL        = new ResultID("E_NOT_AVAIL",         Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_INVALIDDATATYPE  = new ResultID("E_INVALIDDATATYPE",   Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_DATAEXISTS       = new ResultID("E_DATAEXISTS",        Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_INVALIDATTRID    = new ResultID("E_INVALIDATTRID",     Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID E_NODATAEXISTS     = new ResultID("E_NODATAEXISTS",      Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID S_INSERTED         = new ResultID("S_INSERTED",          Namespace.OPC_HISTORICAL_DATA_ACCESS);
			/// <remarks/>
			public static readonly ResultID S_REPLACED         = new ResultID("S_REPLACED",          Namespace.OPC_HISTORICAL_DATA_ACCESS);
		}

		/// <summary>
		/// Results codes for Data eXchange.
		/// </summary>
		public class Dx
		{
			/// <remarks/>
			public static readonly ResultID E_PERSISTING                  = new ResultID("E_PERSISTING",                  Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_NOITEMLIST                  = new ResultID("E_NOITEMLIST",                  Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_SERVER_STATE                = new ResultID("E_SERVER_STATE",                Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_VERSION_MISMATCH            = new ResultID("E_VERSION_MISMATCH",            Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_UNKNOWN_ITEM_PATH           = new ResultID("E_UNKNOWN_ITEM_PATH",           Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_UNKNOWN_ITEM_NAME           = new ResultID("E_UNKNOWN_ITEM_NAME",           Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_INVALID_ITEM_PATH           = new ResultID("E_INVALID_ITEM_PATH",           Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_INVALID_ITEM_NAME           = new ResultID("E_INVALID_ITEM_NAME",           Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_INVALID_NAME                = new ResultID("E_INVALID_NAME",                Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_DUPLICATE_NAME              = new ResultID("E_DUPLICATE_NAME",              Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_INVALID_BROWSE_PATH         = new ResultID("E_INVALID_BROWSE_PATH",         Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_INVALID_SERVER_URL          = new ResultID("E_INVALID_SERVER_URL",          Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_INVALID_SERVER_TYPE         = new ResultID("E_INVALID_SERVER_TYPE",         Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_UNSUPPORTED_SERVER_TYPE     = new ResultID("E_UNSUPPORTED_SERVER_TYPE",     Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_CONNECTIONS_EXIST           = new ResultID("E_CONNECTIONS_EXIST",           Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_TOO_MANY_CONNECTIONS        = new ResultID("E_TOO_MANY_CONNECTIONS",        Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_OVERRIDE_BADTYPE            = new ResultID("E_OVERRIDE_BADTYPE",            Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_OVERRIDE_RANGE              = new ResultID("E_OVERRIDE_RANGE",              Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_SUBSTITUTE_BADTYPE          = new ResultID("E_SUBSTITUTE_BADTYPE",          Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_SUBSTITUTE_RANGE            = new ResultID("E_SUBSTITUTE_RANGE",            Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_INVALID_TARGET_ITEM         = new ResultID("E_INVALID_TARGET_ITEM",         Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_UNKNOWN_TARGET_ITEM         = new ResultID("E_UNKNOWN_TARGET_ITEM",         Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_TARGET_ALREADY_CONNECTED    = new ResultID("E_TARGET_ALREADY_CONNECTED",    Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_UNKNOWN_SERVER_NAME         = new ResultID("E_UNKNOWN_SERVER_NAME",         Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_UNKNOWN_SOURCE_ITEM         = new ResultID("E_UNKNOWN_SOURCE_ITEM",         Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_INVALID_SOURCE_ITEM         = new ResultID("E_INVALID_SOURCE_ITEM",         Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_INVALID_QUEUE_SIZE          = new ResultID("E_INVALID_QUEUE_SIZE",          Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_INVALID_DEADBAND            = new ResultID("E_INVALID_DEADBAND",            Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_INVALID_CONFIG_FILE         = new ResultID("E_INVALID_CONFIG_FILE",         Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_PERSIST_FAILED              = new ResultID("E_PERSIST_FAILED",              Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_TARGET_FAULT                = new ResultID("E_TARGET_FAULT",                Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_TARGET_NO_ACCESSS           = new ResultID("E_TARGET_NO_ACCESSS",           Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_SOURCE_SERVER_FAULT         = new ResultID("E_SOURCE_SERVER_FAULT",         Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_SOURCE_SERVER_NO_ACCESSS    = new ResultID("E_SOURCE_SERVER_NO_ACCESSS",    Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_SUBSCRIPTION_FAULT          = new ResultID("E_SUBSCRIPTION_FAULT",          Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_SOURCE_ITEM_BADRIGHTS       = new ResultID("E_SOURCE_ITEM_BADRIGHTS",       Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_SOURCE_ITEM_BAD_QUALITY     = new ResultID("E_SOURCE_ITEM_BAD_QUALITY",     Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_SOURCE_ITEM_BADTYPE         = new ResultID("E_SOURCE_ITEM_BADTYPE",         Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_SOURCE_ITEM_RANGE           = new ResultID("E_SOURCE_ITEM_RANGE",           Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_SOURCE_SERVER_NOT_CONNECTED = new ResultID("E_SOURCE_SERVER_NOT_CONNECTED", Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_SOURCE_SERVER_TIMEOUT       = new ResultID("E_SOURCE_SERVER_TIMEOUT",       Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_TARGET_ITEM_DISCONNECTED    = new ResultID("E_TARGET_ITEM_DISCONNECTED",    Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_TARGET_NO_WRITES_ATTEMPTED  = new ResultID("E_TARGET_NO_WRITES_ATTEMPTED",  Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_TARGET_ITEM_BADTYPE         = new ResultID("E_TARGET_ITEM_BADTYPE",         Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID E_TARGET_ITEM_RANGE           = new ResultID("E_TARGET_ITEM_RANGE",           Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID S_TARGET_SUBSTITUTED          = new ResultID("S_TARGET_SUBSTITUTED",          Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID S_TARGET_OVERRIDEN            = new ResultID("S_TARGET_OVERRIDEN",            Namespace.OPC_DATA_EXCHANGE);
			/// <remarks/>
			public static readonly ResultID S_CLAMP                       = new ResultID("S_CLAMP",                       Namespace.OPC_DATA_EXCHANGE);
		}

		/// <summary>
		/// Results codes for Alarms and Events
		/// </summary>
		public class Ae
		{
			/// <remarks/>
			public static readonly ResultID S_ALREADYACKED         = new ResultID("S_ALREADYACKED",         Namespace.OPC_ALARM_AND_EVENTS);
			/// <remarks/>
			public static readonly ResultID S_INVALIDBUFFERTIME    = new ResultID("S_INVALIDBUFFERTIME",    Namespace.OPC_ALARM_AND_EVENTS);
			/// <remarks/>
			public static readonly ResultID S_INVALIDMAXSIZE       = new ResultID("S_INVALIDMAXSIZE",       Namespace.OPC_ALARM_AND_EVENTS);
			/// <remarks/>
			public static readonly ResultID S_INVALIDKEEPALIVETIME = new ResultID("S_INVALIDKEEPALIVETIME", Namespace.OPC_ALARM_AND_EVENTS);
			/// <remarks/>
			public static readonly ResultID E_INVALIDBRANCHNAME    = new ResultID("E_INVALIDBRANCHNAME",    Namespace.OPC_ALARM_AND_EVENTS);
			/// <remarks/>
			public static readonly ResultID E_INVALIDTIME          = new ResultID("E_INVALIDTIME",          Namespace.OPC_ALARM_AND_EVENTS);
			/// <remarks/>
			public static readonly ResultID E_BUSY                 = new ResultID("E_BUSY",                 Namespace.OPC_ALARM_AND_EVENTS);
			/// <remarks/>
			public static readonly ResultID E_NOINFO               = new ResultID("E_NOINFO",               Namespace.OPC_ALARM_AND_EVENTS);
		}
	}

	/// <summary>
	/// Used to raise an exception with associated with a specified result code.
	/// </summary>
	[Serializable]
	public class ResultIDException : ApplicationException
	{	/// <remarks/>
		public ResultID Result {get{ return m_result; }}
	
		/// <remarks/>
		public ResultIDException(ResultID result) : base(result.ToString()) { m_result = result; } 
		/// <remarks/>
		public ResultIDException(ResultID result, string message) : base(result.ToString() + "\r\n" + message) { m_result = result; } 
		/// <remarks/>
		public ResultIDException(ResultID result, string message, Exception e) : base(result.ToString() + "\r\n" + message, e) { m_result = result; } 
		/// <remarks/>
		protected ResultIDException(SerializationInfo info, StreamingContext context) : base(info, context) {}
		
		/// <remarks/>
		private ResultID m_result = ResultID.E_FAIL;
	}
}
