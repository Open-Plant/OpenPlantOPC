//============================================================================
// TITLE: Request.cs
//
// CONTENTS:
// 
// A base object to handle asynchronous requests.
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
using System.Collections;
using System.Globalization;
using System.Threading;
using System.Reflection;
using Opc;
using Opc.Da;

namespace OpcXml.Da10
{
	/// <summary>
	/// Contains state information for a single asynchronous request.
	/// </summary>
	public class Request 
	{		
		/// <remarks/>
		public static TimeSpan GetRelativeTime(DateTime absoluteTime)
		{
            if (absoluteTime.Kind == DateTimeKind.Local)
            {
                absoluteTime = absoluteTime.ToUniversalTime();
            }

			return absoluteTime.Subtract(DateTime.UtcNow);
		}

		/// <remarks/>
		public static OpcXml.Da.RequestOptions GetRequestOptions(OpcXml.Da10.RequestOptions input)
		{
			// return a default object if the input is null.
			OpcXml.Da.RequestOptions output = new OpcXml.Da.RequestOptions();

			if (input != null)
			{
				output.RequestHandle   = input.ClientRequestHandle;
				output.Locale          = input.LocaleID;
				output.RequestDeadline = (input.RequestDeadlineSpecified)?input.RequestDeadline:DateTime.MinValue;
				output.Filters         = 0;
				
				output.Filters |= (input.ReturnDiagnosticInfo)?(int)ResultFilter.DiagnosticInfo:0;
				output.Filters |= (input.ReturnErrorText)?(int)ResultFilter.ErrorText:0;
				output.Filters |= (input.ReturnItemName)?(int)ResultFilter.ItemName:0;
				output.Filters |= (input.ReturnItemPath)?(int)ResultFilter.ItemPath:0;
				output.Filters |= (input.ReturnItemTime)?(int)ResultFilter.ItemTime:0;
			}
			else
			{
				output.RequestHandle   = null;
				output.Locale          = null;
				output.RequestDeadline = DateTime.MinValue;
				output.Filters         = (int)ResultFilter.ErrorText;
			}

			return output;
		}

		/// <remarks/>
		internal static OpcXml.Da10.RequestOptions GetRequestOptions(string locale, int filters)
		{
			OpcXml.Da10.RequestOptions output = new OpcXml.Da10.RequestOptions();

			output.ClientRequestHandle      = null;
			output.LocaleID                 = locale;
			output.RequestDeadline          = DateTime.MinValue;
			output.RequestDeadlineSpecified = false;
			output.ReturnDiagnosticInfo     = ((filters & (int)ResultFilter.DiagnosticInfo) != 0);
			output.ReturnErrorText          = ((filters & (int)ResultFilter.ErrorText) != 0);
			output.ReturnItemPath           = ((filters & (int)ResultFilter.ItemPath) != 0);
			output.ReturnItemName           = ((filters & (int)ResultFilter.ItemName) != 0);
			output.ReturnItemTime           = ((filters & (int)ResultFilter.ItemTime) != 0);
			output.RequestDeadline          = DateTime.MinValue;
			output.RequestDeadlineSpecified = false;

			return output;
		}	

		/// <remarks/>
		public static OpcXml.Da10.ReplyBase GetReplyBase(string localeID, OpcXml.Da.ReplyBase input)
		{
			OpcXml.Da10.ReplyBase output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.ReplyBase();

				output.ClientRequestHandle  = input.ClientRequestHandle;
				output.RcvTime              = input.RcvTime;
				output.ReplyTime            = input.ReplyTime;
				output.RevisedLocaleID      = input.RevisedLocaleID;
				output.ServerState          = GetServerState(input.ServerState);
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.OPCError[] GetErrors(OpcXml.Da.Error[] input)
		{
			OpcXml.Da10.OPCError[] output = null;

			if (input != null && input.Length > 0)
			{
				output = new OpcXml.Da10.OPCError[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii]      = new OpcXml.Da10.OPCError();
					output[ii].ID   = GetResultID(new Opc.ResultID(input[ii].ID));
					output[ii].Text = input[ii].Text;

				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.Error[] GetErrors(OpcXml.Da10.OPCError[] input)
		{
			OpcXml.Da.Error[] output = null;

			if (input != null && input.Length > 0)
			{
				output = new OpcXml.Da.Error[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii]      = new OpcXml.Da.Error();
					output[ii].ID   = GetResultID(input[ii].ID).Name;
					output[ii].Text = input[ii].Text;
				}
			}

			return output;
		}

		/// <remarks/>
		public static Opc.ResultID GetResultID(XmlQualifiedName input)
		{
			if (input == null) return Opc.ResultID.S_OK; 
			
			if (input == OpcXml.Da.Error.E_FAIL)                     return Opc.ResultID.E_FAIL; 
			if (input == OpcXml.Da.Error.E_OUTOFMEMORY)              return Opc.ResultID.E_OUTOFMEMORY; 
			if (input == OpcXml.Da.Error.E_SERVERSTATE)              return new Opc.ResultID(input); 
			if (input == OpcXml.Da.Error.E_TIMEDOUT)                 return Opc.ResultID.E_TIMEDOUT;
			if (input == OpcXml.Da.Error.E_BUSY)                     return new Opc.ResultID(input);  
			if (input == OpcXml.Da.Error.E_NOSUBSCRIPTION)           return new Opc.ResultID(input);  
			if (input == OpcXml.Da.Error.E_INVALIDHOLDTIME)          return new Opc.ResultID(input);  
			if (input == OpcXml.Da.Error.E_INVALIDCONTINUATIONPOINT) return new Opc.ResultID(input);  
			if (input == OpcXml.Da.Error.E_INVALIDFILTER)            return new Opc.ResultID(input);  
			if (input == OpcXml.Da.Error.E_UNKNOWNITEMNAME)          return Opc.ResultID.Da.E_UNKNOWN_ITEM_NAME;
			if (input == OpcXml.Da.Error.E_INVALIDITEMNAME)          return Opc.ResultID.Da.E_INVALID_ITEM_NAME;
			if (input == OpcXml.Da.Error.E_UNKNOWNITEMPATH)          return Opc.ResultID.Da.E_UNKNOWN_ITEM_PATH;
			if (input == OpcXml.Da.Error.E_INVALIDITEMPATH)          return Opc.ResultID.Da.E_INVALID_ITEM_PATH;
			if (input == OpcXml.Da.Error.E_BADTYPE)                  return Opc.ResultID.Da.E_BADTYPE;
			if (input == OpcXml.Da.Error.E_RANGE)                    return Opc.ResultID.Da.E_RANGE;
			if (input == OpcXml.Da.Error.E_READONLY)                 return Opc.ResultID.Da.E_READONLY;
			if (input == OpcXml.Da.Error.E_WRITEONLY)                return Opc.ResultID.Da.E_WRITEONLY;
			if (input == OpcXml.Da.Error.E_NOTSUPPORTED)             return Opc.ResultID.Da.E_NO_WRITEQT;
			if (input == OpcXml.Da.Error.E_INVALIDPID)               return Opc.ResultID.Da.E_INVALID_PID;
			if (input == OpcXml.Da.Error.S_FALSE)                    return Opc.ResultID.S_FALSE;
			if (input == OpcXml.Da.Error.S_CLAMP)                    return Opc.ResultID.Da.S_CLAMP;
			if (input == OpcXml.Da.Error.S_UNSUPPORTEDRATE)          return Opc.ResultID.Da.S_UNSUPPORTEDRATE;
			if (input == OpcXml.Da.Error.S_DATAQUEUEOVERFLOW)        return Opc.ResultID.Da.S_DATAQUEUEOVERFLOW;
			if (input == OpcXml.Da.Error.E_TYPE_CHANGED)             return Opc.ResultID.Cpx.E_TYPE_CHANGED;
			if (input == OpcXml.Da.Error.E_FILTER_DUPLICATE)         return Opc.ResultID.Cpx.E_FILTER_DUPLICATE;
			if (input == OpcXml.Da.Error.E_FILTER_INVALID)           return Opc.ResultID.Cpx.E_FILTER_INVALID;
			if (input == OpcXml.Da.Error.E_FILTER_ERROR)             return Opc.ResultID.Cpx.E_FILTER_ERROR;
			if (input == OpcXml.Da.Error.S_FILTER_NO_DATA)           return Opc.ResultID.Cpx.S_FILTER_NO_DATA;

			// no conversion for unrecognized errors.
			return new Opc.ResultID(input);
		}

		// these values can show up as return codes from COM-DA servers.
		private const int DISP_E_TYPEMISMATCH = -0x7FFDFFFB; // 0x80020005
		private const int DISP_E_OVERFLOW     = -0x7FFDFFF6; // 0x8002000A

		/// <remarks/>
		public static XmlQualifiedName GetResultID(Opc.ResultID input)
		{
			if (input == Opc.ResultID.S_OK)                   return null; 
			if (input == Opc.ResultID.S_FALSE)                return null; 
			if (input == Opc.ResultID.E_FAIL)                 return OpcXml.Da.Error.E_FAIL; 
			if (input == Opc.ResultID.E_INVALIDARG)           return OpcXml.Da.Error.E_FAIL; 
			if (input == Opc.ResultID.E_OUTOFMEMORY)          return OpcXml.Da.Error.E_OUTOFMEMORY; 
			if (input == Opc.ResultID.E_TIMEDOUT)             return OpcXml.Da.Error.E_TIMEDOUT; 
			if (input == Opc.ResultID.Da.S_DATAQUEUEOVERFLOW) return OpcXml.Da.Error.S_DATAQUEUEOVERFLOW; 
			if (input == Opc.ResultID.Da.S_UNSUPPORTEDRATE)   return OpcXml.Da.Error.S_UNSUPPORTEDRATE; 
			if (input == Opc.ResultID.Da.S_CLAMP)             return OpcXml.Da.Error.S_CLAMP; 
			if (input == Opc.ResultID.Da.E_INVALIDHANDLE)     return Opc.ResultID.Da.E_INVALIDHANDLE.Name; 
			if (input == Opc.ResultID.Da.E_UNKNOWN_ITEM_NAME) return OpcXml.Da.Error.E_UNKNOWNITEMNAME; 
			if (input == Opc.ResultID.Da.E_INVALID_ITEM_NAME) return OpcXml.Da.Error.E_INVALIDITEMNAME; 
			if (input == Opc.ResultID.Da.E_UNKNOWN_ITEM_PATH) return OpcXml.Da.Error.E_UNKNOWNITEMPATH; 
			if (input == Opc.ResultID.Da.E_INVALID_ITEM_PATH) return OpcXml.Da.Error.E_INVALIDITEMPATH; 
			if (input == Opc.ResultID.Da.E_INVALID_PID)       return OpcXml.Da.Error.E_INVALIDPID; 
			if (input == Opc.ResultID.Da.E_READONLY)          return OpcXml.Da.Error.E_READONLY; 
			if (input == Opc.ResultID.Da.E_WRITEONLY)         return OpcXml.Da.Error.E_WRITEONLY; 
			if (input == Opc.ResultID.Da.E_BADTYPE)           return OpcXml.Da.Error.E_BADTYPE; 
			if (input == Opc.ResultID.Da.E_RANGE)             return OpcXml.Da.Error.E_RANGE; 
			if (input == Opc.ResultID.Da.E_NO_WRITEQT)        return OpcXml.Da.Error.E_NOTSUPPORTED; 
			if (input == Opc.ResultID.Da.E_NO_ITEM_DEADBAND)  return Opc.ResultID.Da.E_NO_ITEM_DEADBAND.Name; 
			if (input == Opc.ResultID.Da.E_NO_ITEM_SAMPLING)  return Opc.ResultID.Da.E_NO_ITEM_SAMPLING.Name; 
			if (input == Opc.ResultID.Da.E_NO_ITEM_BUFFERING) return Opc.ResultID.Da.E_NO_ITEM_BUFFERING.Name; 
			if (input == Opc.ResultID.Cpx.E_TYPE_CHANGED)     return OpcXml.Da.Error.E_TYPE_CHANGED;
			if (input == Opc.ResultID.Cpx.E_FILTER_DUPLICATE) return OpcXml.Da.Error.E_FILTER_DUPLICATE;
			if (input == Opc.ResultID.Cpx.E_FILTER_INVALID)   return OpcXml.Da.Error.E_FILTER_INVALID;
			if (input == Opc.ResultID.Cpx.E_FILTER_ERROR)     return OpcXml.Da.Error.E_FILTER_ERROR;
			if (input == Opc.ResultID.Cpx.S_FILTER_NO_DATA)   return OpcXml.Da.Error.S_FILTER_NO_DATA;

			// return a generic error code if no name exists for the result id.
			if (input.Name == null)
			{
				if (input.Code == DISP_E_TYPEMISMATCH)
				{
					return OpcXml.Da.Error.E_BADTYPE;
				}

				if (input.Code == DISP_E_OVERFLOW)
				{
					return OpcXml.Da.Error.E_RANGE;
				}

				return (input.Succeeded())?OpcXml.Da.Error.S_FALSE:OpcXml.Da.Error.E_FAIL;
			}

			// no conversion for unrecognized errors.
			return input.Name;
		}

		/// <remarks/>
		public static OpcXml.Da10.browseFilter GetBrowseFilter(Opc.Da.browseFilter input)
		{
			switch (input)
			{
				case Opc.Da.browseFilter.all:    return OpcXml.Da10.browseFilter.all;
				case Opc.Da.browseFilter.branch: return OpcXml.Da10.browseFilter.branch;
				case Opc.Da.browseFilter.item:   return OpcXml.Da10.browseFilter.item;
			}

			return OpcXml.Da10.browseFilter.all;
		}

		/// <remarks/>
		public static Opc.Da.browseFilter GetBrowseFilter(OpcXml.Da10.browseFilter input)
		{
			switch (input)
			{
				case OpcXml.Da10.browseFilter.all:    return Opc.Da.browseFilter.all;
				case OpcXml.Da10.browseFilter.branch: return Opc.Da.browseFilter.branch;
				case OpcXml.Da10.browseFilter.item:   return Opc.Da.browseFilter.item;
			}

			return Opc.Da.browseFilter.all;
		}

		/// <remarks/>
		public static PropertyID[] GetPropertyIDs(XmlQualifiedName[] input)
		{
			PropertyID[] output = null;

			if (input != null)
			{
				output = new PropertyID[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetPropertyID(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static PropertyID GetPropertyID(XmlQualifiedName input)
		{
			// convert standard properties from xml to unified da.
			FieldInfo[] fields = typeof(Opc.Da.Property).GetFields(BindingFlags.Static | BindingFlags.Public);

			foreach (FieldInfo field in fields)
			{
				PropertyID property = (PropertyID)field.GetValue(typeof(PropertyID));

				if (input.Name == property.Name.Name)
				{
					return property;
				}
			}
			
			// attempt to convert property name to a integer property id for unknown properties.
			return new PropertyID(input.Name, -1, input.Namespace);
		}

		/// <remarks/>
		public static XmlQualifiedName[] GetPropertyNames(PropertyID[] input)
		{
			XmlQualifiedName[] output = null;

			if (input != null)
			{
				output = new XmlQualifiedName[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetPropertyName(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static XmlQualifiedName GetPropertyName(PropertyID input)
		{	
			// check for a vendor defined property with no name.
			if (input.Name == null)
			{
				return new XmlQualifiedName(input.ToString(), "http://default.vendor.com/namespace");
			}

			// convert standard properties from unified da to xml. 
			FieldInfo[] fields = typeof(OpcXml.Da.Property).GetFields(BindingFlags.Static | BindingFlags.Public);

			foreach (FieldInfo field in fields)
			{
				XmlQualifiedName property = (XmlQualifiedName)field.GetValue(typeof(PropertyID));

				if (input.Name.Name == property.Name)
				{
					return property;
				}
			}

			return input.Name;
		}

		/// <remarks/>
		public static Opc.Da.BrowseElement[] GetBrowseElements(OpcXml.Da10.BrowseElement[] input)
		{
			Opc.Da.BrowseElement[] output = null;

			if (input != null)
			{
				output = new Opc.Da.BrowseElement[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetBrowseElement(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.BrowseElement[] GetBrowseElements(Opc.Da.BrowseElement[] input)
		{
			OpcXml.Da10.BrowseElement[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.BrowseElement[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetBrowseElement(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static Opc.Da.BrowseElement GetBrowseElement(OpcXml.Da10.BrowseElement input)
		{
			Opc.Da.BrowseElement output = null;

			if (input != null)
			{
				output             = new Opc.Da.BrowseElement();
				output.Name        = input.Name;
				output.ItemName    = input.ItemName;
				output.ItemPath    = input.ItemPath;
				output.IsItem      = input.IsItem;
				output.HasChildren = input.HasChildren;
				output.Properties  = null;

				if (input.Properties != null)
				{
					output.Properties = new Opc.Da.ItemProperty[input.Properties.Length];
						
					for (int ii = 0; ii < input.Properties.Length; ii++)
					{
						output.Properties[ii] = GetItemProperty(input.Properties[ii]);
					}
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.BrowseElement GetBrowseElement(Opc.Da.BrowseElement input)
		{
			OpcXml.Da10.BrowseElement output = null;

			if (input != null)
			{
				output             = new OpcXml.Da10.BrowseElement();
				output.Name        = input.Name;
				output.ItemName    = input.ItemName;
				output.ItemPath    = input.ItemPath;
				output.IsItem      = input.IsItem;
				output.HasChildren = input.HasChildren;
				output.Properties    = null;

				if (input.Properties != null)
				{
					output.Properties = new OpcXml.Da10.ItemProperty[input.Properties.Length];

					for (int ii = 0; ii < input.Properties.Length; ii++)
					{
						output.Properties[ii] = GetItemProperty(input.Properties[ii]);
					}
				}
			}

			return output;
		}

		/// <remarks/>
		public static Opc.Da.ItemProperty GetItemProperty(OpcXml.Da10.ItemProperty input)
		{
			Opc.Da.ItemProperty output = null;

			if (input != null)
			{
				output             = new Opc.Da.ItemProperty();
				output.ID          = Request.GetPropertyID(input.Name);
				output.Description = input.Description;
				output.Value       = UnmarshalPropertyValue(output.ID, input.Value);
				output.ItemName    = input.ItemName;
				output.ItemPath    = input.ItemPath;
				output.ResultID    = GetResultID(input.ResultID);
		
				// infer data type from value.
				if (output.Value != null)
				{
					output.DataType = output.Value.GetType();
				}
				else
				{
					// lookup well known property description.
					PropertyDescription description = PropertyDescription.Find(output.ID);

					if (description != null)
					{
						output.DataType = description.Type;
					}	
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.ItemProperty GetItemProperty(Opc.Da.ItemProperty input)
		{
			OpcXml.Da10.ItemProperty output = null;

			if (input != null)
			{
				output             = new OpcXml.Da10.ItemProperty();
				output.Name        = GetPropertyName(input.ID);
				output.Description = input.Description;
				output.Value       = MarshalPropertyValue(input.ID, input.Value);
				output.ItemName    = input.ItemName;
				output.ItemPath    = input.ItemPath;
				output.ResultID    = GetResultID(input.ResultID);
			}

			return output;
		}

		/// <remarks/>
		internal static object MarshalPropertyValue(PropertyID propertyID, object input)
		{						
			try
			{
				if (input == null) return null;

				if (propertyID == Opc.Da.Property.QUALITY)
				{
					return Request.GetQuality((Opc.Da.Quality)input);
				}

				if (propertyID == Opc.Da.Property.ACCESSRIGHTS)
				{
					return input.ToString();
				}

				if (propertyID == Opc.Da.Property.EUTYPE)
				{
					return input.ToString();
				}

				if (propertyID == Opc.Da.Property.DATATYPE)
				{
					return GetType((System.Type)input);
				}
			}
			catch {}

			return input;
		}

		/// <remarks/>
		internal static object UnmarshalPropertyValue(PropertyID propertyID, object input)
		{						
			try
			{
				if (input == null) return null;

				if (propertyID == Opc.Da.Property.QUALITY)
				{
					return Request.GetQuality((OpcXml.Da10.OPCQuality)input);
				}

				if (propertyID == Opc.Da.Property.ACCESSRIGHTS)
				{
					return Enum.Parse(typeof(Opc.Da.accessRights), input.ToString(), false);
				}

				if (propertyID == Opc.Da.Property.EUTYPE)
				{
					return Enum.Parse(typeof(Opc.Da.euType), input.ToString(), false);
				}

				if (propertyID == Opc.Da.Property.DATATYPE)
				{
					return GetType((XmlQualifiedName)input);
				}
			}
			catch {}

			return input;
		}

		/// <remarks/>
		internal static XmlQualifiedName GetType(System.Type input)
		{
			if (input == null)               return null;
			if (input == typeof(sbyte))      return OpcXml.Da.Type.SBYTE;
			if (input == typeof(byte))       return OpcXml.Da.Type.BYTE;
			if (input == typeof(short))      return OpcXml.Da.Type.SHORT;
			if (input == typeof(ushort))     return OpcXml.Da.Type.USHORT;
			if (input == typeof(int))        return OpcXml.Da.Type.INT;
			if (input == typeof(uint))       return OpcXml.Da.Type.UINT;
			if (input == typeof(long))       return OpcXml.Da.Type.LONG;
			if (input == typeof(ulong))      return OpcXml.Da.Type.ULONG;
			if (input == typeof(float))      return OpcXml.Da.Type.FLOAT;
			if (input == typeof(double))     return OpcXml.Da.Type.DOUBLE;
			if (input == typeof(decimal))    return OpcXml.Da.Type.DECIMAL;
			if (input == typeof(bool))       return OpcXml.Da.Type.BOOLEAN;
			if (input == typeof(DateTime))   return OpcXml.Da.Type.DATETIME;
			if (input == typeof(string))     return OpcXml.Da.Type.STRING;
			if (input == typeof(sbyte[]))    return OpcXml.Da.Type.ARRAY_SBYTE;
			if (input == typeof(byte[]))     return OpcXml.Da.Type.BINARY;
			if (input == typeof(short[]))    return OpcXml.Da.Type.ARRAY_SHORT;
			if (input == typeof(ushort[]))   return OpcXml.Da.Type.ARRAY_USHORT;
			if (input == typeof(int[]))      return OpcXml.Da.Type.ARRAY_INT;
			if (input == typeof(uint[]))     return OpcXml.Da.Type.ARRAY_UINT;
			if (input == typeof(long[]))     return OpcXml.Da.Type.ARRAY_LONG;
			if (input == typeof(ulong[]))    return OpcXml.Da.Type.ARRAY_ULONG;
			if (input == typeof(float[]))    return OpcXml.Da.Type.ARRAY_FLOAT;
			if (input == typeof(double[]))   return OpcXml.Da.Type.ARRAY_DOUBLE;
			if (input == typeof(decimal[]))  return OpcXml.Da.Type.ARRAY_DECIMAL;
			if (input == typeof(bool[]))     return OpcXml.Da.Type.ARRAY_BOOLEAN;
			if (input == typeof(DateTime[])) return OpcXml.Da.Type.ARRAY_DATETIME;
			if (input == typeof(string[]))   return OpcXml.Da.Type.ARRAY_STRING;
			if (input == typeof(object[]))   return OpcXml.Da.Type.ARRAY_ANY_TYPE;

			return OpcXml.Da.Type.ANY_TYPE;
		}

		/// <remarks/>
		internal static System.Type GetType(XmlQualifiedName input)
		{
			if (input == null)                           return null;
			if (input == OpcXml.Da.Type.SBYTE)           return typeof(sbyte);
			if (input == OpcXml.Da.Type.BYTE)            return typeof(byte);
			if (input == OpcXml.Da.Type.SHORT)           return typeof(short);
			if (input == OpcXml.Da.Type.USHORT)          return typeof(ushort);
			if (input == OpcXml.Da.Type.INT)             return typeof(int);
			if (input == OpcXml.Da.Type.UINT)            return typeof(uint);
			if (input == OpcXml.Da.Type.LONG)            return typeof(long);
			if (input == OpcXml.Da.Type.ULONG)           return typeof(ulong);
			if (input == OpcXml.Da.Type.FLOAT)           return typeof(float);
			if (input == OpcXml.Da.Type.DOUBLE)          return typeof(double);
			if (input == OpcXml.Da.Type.DECIMAL)         return typeof(decimal);
			if (input == OpcXml.Da.Type.BOOLEAN)         return typeof(bool);
			if (input == OpcXml.Da.Type.DATETIME)        return typeof(DateTime);
			if (input == OpcXml.Da.Type.STRING)          return typeof(string);
			if (input == OpcXml.Da.Type.ANY_TYPE)        return typeof(object);
			if (input == OpcXml.Da.Type.BINARY)          return typeof(byte[]);
			if (input == OpcXml.Da.Type.ARRAY_SBYTE)     return typeof(sbyte[]);
			if (input == OpcXml.Da.Type.ARRAY_BYTE)      return typeof(byte[]);
			if (input == OpcXml.Da.Type.ARRAY_SHORT)     return typeof(short[]);
			if (input == OpcXml.Da.Type.ARRAY_USHORT)    return typeof(ushort[]);
			if (input == OpcXml.Da.Type.ARRAY_INT)       return typeof(int[]);
			if (input == OpcXml.Da.Type.ARRAY_UINT)      return typeof(uint[]);
			if (input == OpcXml.Da.Type.ARRAY_LONG)      return typeof(long[]);
			if (input == OpcXml.Da.Type.ARRAY_ULONG)     return typeof(ulong[]);
			if (input == OpcXml.Da.Type.ARRAY_FLOAT)     return typeof(float[]);
			if (input == OpcXml.Da.Type.ARRAY_DOUBLE)    return typeof(double[]);
			if (input == OpcXml.Da.Type.ARRAY_DECIMAL)   return typeof(decimal[]);
			if (input == OpcXml.Da.Type.ARRAY_BOOLEAN)   return typeof(bool[]);
			if (input == OpcXml.Da.Type.ARRAY_DATETIME)  return typeof(DateTime[]);
			if (input == OpcXml.Da.Type.ARRAY_STRING)    return typeof(string[]);
			if (input == OpcXml.Da.Type.ARRAY_ANY_TYPE)  return typeof(object[]);

			return Opc.Type.ILLEGAL_TYPE;
		}

		/// <remarks/>
		public static Opc.ItemIdentifier[] GetItemIdentifiers(OpcXml.Da10.ItemIdentifier[] input)
		{
			Opc.ItemIdentifier[] output = null;

			if (input != null)
			{
				output = new Opc.ItemIdentifier[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = new Opc.ItemIdentifier();

					output[ii].ItemName = input[ii].ItemName;
					output[ii].ItemPath = input[ii].ItemPath;
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.ItemIdentifier[] GetItemIdentifiers(Opc.ItemIdentifier[] input)
		{
			OpcXml.Da10.ItemIdentifier[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.ItemIdentifier[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = new OpcXml.Da10.ItemIdentifier();
 
					output[ii].ItemName = input[ii].ItemName;
					output[ii].ItemPath = input[ii].ItemPath;
				}
			}

			return output;
		}

		/// <remarks/>
		public static ItemPropertyCollection[] GetItemPropertyCollections(OpcXml.Da10.PropertyReplyList[] input)
		{
			ItemPropertyCollection[] output = null;

			if (input != null)
			{
				output = new ItemPropertyCollection[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
				    output[ii]           = new ItemPropertyCollection();
					output[ii].ItemName  = input[ii].ItemName;
					output[ii].ItemPath  = input[ii].ItemPath;
					output[ii].ResultID  = GetResultID(input[ii].ResultID);

					if (input[ii].Properties != null)
					{
						foreach (OpcXml.Da10.ItemProperty property in input[ii].Properties)
						{
							output[ii].Add(GetItemProperty(property));
						}
					}
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.PropertyReplyList[] GetItemPropertyCollections(ItemPropertyCollection[] input)
		{
			OpcXml.Da10.PropertyReplyList[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.PropertyReplyList[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii]          = new OpcXml.Da10.PropertyReplyList();
					output[ii].ItemName = input[ii].ItemName;
					output[ii].ItemPath = input[ii].ItemPath;
					output[ii].ResultID  = GetResultID(input[ii].ResultID);

					if (input[ii].Count > 0)
					{
						output[ii].Properties = new OpcXml.Da10.ItemProperty[input[ii].Count];

						for (int jj = 0; jj < output[ii].Properties.Length; jj++)
						{
							output[ii].Properties[jj] = GetItemProperty(input[ii][jj]);
						}
					}
				}
			}

			return output;
		}

		/// <remarks/>
		public static Opc.Da.serverState GetServerState(OpcXml.Da10.serverState input)
		{
			switch (input)
			{
				case OpcXml.Da10.serverState.running:   return Opc.Da.serverState.running;
				case OpcXml.Da10.serverState.suspended: return Opc.Da.serverState.suspended;
				case OpcXml.Da10.serverState.test:      return Opc.Da.serverState.test;
				case OpcXml.Da10.serverState.noConfig:  return Opc.Da.serverState.noConfig;
				case OpcXml.Da10.serverState.failed:    return Opc.Da.serverState.failed;
				case OpcXml.Da10.serverState.commFault: return Opc.Da.serverState.commFault;
			}

			return Opc.Da.serverState.unknown;
		}

		/// <remarks/>
		public static OpcXml.Da10.serverState GetServerState(Opc.Da.serverState input)
		{
			switch (input)
			{
				case Opc.Da.serverState.running:   return OpcXml.Da10.serverState.running;
				case Opc.Da.serverState.suspended: return OpcXml.Da10.serverState.suspended;
				case Opc.Da.serverState.test:      return OpcXml.Da10.serverState.test;
				case Opc.Da.serverState.noConfig:  return OpcXml.Da10.serverState.noConfig;
				case Opc.Da.serverState.failed:    return OpcXml.Da10.serverState.failed;
				case Opc.Da.serverState.commFault: return OpcXml.Da10.serverState.commFault;
			}

			return OpcXml.Da10.serverState.running;
		}

		/// <remarks/>
		public static Opc.Da.ServerStatus GetServerStatus(OpcXml.Da10.ReplyBase reply, OpcXml.Da10.ServerStatus input)
		{
			Opc.Da.ServerStatus output = null;

			if (input != null)
			{
				output                = new Opc.Da.ServerStatus();
				output.VendorInfo     = input.VendorInfo;
				output.ProductVersion = input.ProductVersion;
				output.ServerState    = GetServerState(reply.ServerState);
				output.StatusInfo     = input.StatusInfo;
				output.StartTime      = input.StartTime;
				output.CurrentTime    = reply.ReplyTime;
				output.LastUpdateTime = DateTime.MinValue;
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.ServerStatus GetServerStatus(Opc.Da.ServerStatus input, string[] locales)
		{
			OpcXml.Da10.ServerStatus output = null;

			if (input != null)
			{
				output                            = new OpcXml.Da10.ServerStatus();
				output.VendorInfo                 = input.VendorInfo;
				output.ProductVersion             = input.ProductVersion;
				output.StatusInfo                 = input.StatusInfo;
				output.StartTime                  = input.StartTime;
				output.SupportedInterfaceVersions = new OpcXml.Da10.interfaceVersion[] { OpcXml.Da10.interfaceVersion.XML_DA_Version_1_0 };
				output.SupportedLocaleIDs         = (locales != null)?(string[])locales.Clone():(string[])null;		
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.ItemList[] GetSubscribeLists(OpcXml.Da10.SubscribeRequestItemList[] input)
		{
			OpcXml.Da.ItemList[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da.ItemList[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetSubscribeList(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.SubscribeRequestItemList[] GetSubscribeLists(OpcXml.Da.ItemList[] input)
		{
			OpcXml.Da10.SubscribeRequestItemList[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.SubscribeRequestItemList[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetSubscribeList(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.ItemList GetSubscribeList(OpcXml.Da10.SubscribeRequestItemList input)
		{
			OpcXml.Da.ItemList output = null;

			if (input != null)
			{
				output = new OpcXml.Da.ItemList(); 

				output.ClientHandle             = null;
				output.ServerHandle             = null;
				output.ItemPath                 = input.ItemPath;
				output.ReqType                  = GetType(input.ReqType);
				output.MaxAge                   = 0;
				output.MaxAgeSpecified          = false;
				output.Deadband                 = input.Deadband;
				output.DeadbandSpecified        = input.DeadbandSpecified;
				output.SamplingRate             = (int)input.RequestedSamplingRate;
				output.SamplingRateSpecified    = input.RequestedSamplingRateSpecified;
				output.EnableBuffering          = input.EnableBuffering;
				output.EnableBufferingSpecified = input.EnableBufferingSpecified;

				if (input.Items!= null)
				{
					foreach (OpcXml.Da10.SubscribeRequestItem item in input.Items)
					{
						output.Add(GetSubscribeItem(item));
					}
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.SubscribeRequestItemList GetSubscribeList(OpcXml.Da.ItemList input)
		{
			OpcXml.Da10.SubscribeRequestItemList output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.SubscribeRequestItemList();

				output.ItemPath                       = input.ItemPath;
				output.ReqType                        = GetType(input.ReqType);
				output.Deadband                       = input.Deadband;
				output.DeadbandSpecified              = input.DeadbandSpecified;
				output.EnableBuffering                = input.EnableBuffering;
				output.EnableBufferingSpecified       = input.EnableBufferingSpecified;
				output.RequestedSamplingRate          = input.SamplingRate;
				output.RequestedSamplingRateSpecified = input.SamplingRateSpecified;

				output.Items= new OpcXml.Da10.SubscribeRequestItem[input.Count];

				for (int ii = 0; ii < output.Items.Length; ii++)
				{
					output.Items[ii] = GetSubscribeItem(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static Item GetSubscribeItem(OpcXml.Da10.SubscribeRequestItem input)
		{
			Item output = null;

			if (input != null)
			{
				output = new Item();

				output.ClientHandle             = input.ClientItemHandle;
				output.ServerHandle             = null;
				output.ItemPath                 = input.ItemPath;
				output.ItemName                 = input.ItemName;
				output.ReqType                  = GetType(input.ReqType);
				output.MaxAge                   = 0;
				output.MaxAgeSpecified          = false;
				output.Deadband                 = input.Deadband;
				output.DeadbandSpecified        = input.DeadbandSpecified;
				output.SamplingRate             = (int)input.RequestedSamplingRate;
				output.SamplingRateSpecified    = input.RequestedSamplingRateSpecified;
				output.EnableBuffering          = input.EnableBuffering;
				output.EnableBufferingSpecified = input.EnableBufferingSpecified;
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.SubscribeRequestItem GetSubscribeItem(Item input)
		{
			OpcXml.Da10.SubscribeRequestItem output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.SubscribeRequestItem();
				
				output.ClientItemHandle               = (string)input.ClientHandle;
				output.ItemPath                       = input.ItemPath;
				output.ItemName                       = input.ItemName;
				output.ReqType                        = GetType(input.ReqType);
				output.Deadband                       = input.Deadband;
				output.DeadbandSpecified              = input.DeadbandSpecified;
				output.EnableBuffering                = input.EnableBuffering;
				output.EnableBufferingSpecified       = input.EnableBufferingSpecified;
				output.RequestedSamplingRate          = input.SamplingRate;
				output.RequestedSamplingRateSpecified = input.SamplingRateSpecified;
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.ItemValueResultList[] GetSubscribeRefreshLists(OpcXml.Da10.SubscribePolledRefreshReplyItemList[] input)
		{
			OpcXml.Da.ItemValueResultList[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da.ItemValueResultList[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetSubscribeRefreshList(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.SubscribePolledRefreshReplyItemList[] GetSubscribeRefreshLists(OpcXml.Da.ItemValueResultList[] input)
		{
			OpcXml.Da10.SubscribePolledRefreshReplyItemList[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.SubscribePolledRefreshReplyItemList[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetSubscribeRefreshList(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.ItemValueResultList GetSubscribeRefreshList(OpcXml.Da10.SubscribePolledRefreshReplyItemList input)
		{
			OpcXml.Da.ItemValueResultList output = null;

			if (input != null)
			{
				output = new OpcXml.Da.ItemValueResultList();

				output.Name                  = null;
				output.ServerHandle          = input.SubscriptionHandle;
				output.ClientHandle          = null;
				output.SamplingRate          = 0;
				output.SamplingRateSpecified = false;

				if (input.Items!= null)
				{
					foreach (OpcXml.Da10.ItemValue item in input.Items)
					{
						output.Add(GetResultItem(item));
					}
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.SubscribePolledRefreshReplyItemList GetSubscribeRefreshList(OpcXml.Da.ItemValueResultList input)
		{
			OpcXml.Da10.SubscribePolledRefreshReplyItemList output = null;

			if (input != null)
			{
				output                    = new OpcXml.Da10.SubscribePolledRefreshReplyItemList();
				output.SubscriptionHandle = (input.ServerHandle != null)?input.ServerHandle.ToString():null;
				output.Items              = new OpcXml.Da10.ItemValue[input.Count];

				for (int ii = 0; ii < output.Items.Length; ii++)
				{
					output.Items[ii] = GetResultItem(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.ItemList[] GetItemLists(OpcXml.Da10.ReadRequestItemList[] input)
		{
			OpcXml.Da.ItemList[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da.ItemList[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetItemList(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.ReadRequestItemList[] GetItemLists(OpcXml.Da.ItemList[] input)
		{
			OpcXml.Da10.ReadRequestItemList[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.ReadRequestItemList[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetItemList(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.ItemList GetItemList(OpcXml.Da10.ReadRequestItemList input)
		{
			OpcXml.Da.ItemList output = null;

			if (input != null)
			{
				output = new OpcXml.Da.ItemList();
				
				output.ClientHandle             = null;
				output.ServerHandle             = null;
				output.ItemPath                 = input.ItemPath;
				output.ReqType                  = GetType(input.ReqType);
				output.MaxAge                   = (int)input.MaxAge;
				output.MaxAgeSpecified          = input.MaxAgeSpecified;
				output.Deadband                 = 0;
				output.DeadbandSpecified        = false;
				output.SamplingRate             = 0;
				output.SamplingRateSpecified    = false;
				output.EnableBuffering          = false;
				output.EnableBufferingSpecified = false;

				if (input.Items != null)
				{
					foreach (OpcXml.Da10.ReadRequestItem item in input.Items)
					{
						output.Add(GetItem(item));
					}
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.ReadRequestItemList GetItemList(OpcXml.Da.ItemList input)
		{
			OpcXml.Da10.ReadRequestItemList output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.ReadRequestItemList();
				
				output.ItemPath        = input.ItemPath;
				output.ReqType         = GetType(input.ReqType);
				output.MaxAge          = input.MaxAge;
				output.MaxAgeSpecified = input.MaxAgeSpecified;

				output.Items = new OpcXml.Da10.ReadRequestItem[input.Count];

				for (int ii = 0; ii < output.Items.Length; ii++)
				{
					 output.Items[ii] = GetItem(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static Opc.Da.Item GetItem(OpcXml.Da10.ReadRequestItem input)
		{
			Opc.Da.Item output = null;

			if (input != null)
			{
				output = new Opc.Da.Item();
				
				output.ClientHandle                   = input.ClientItemHandle;
				output.ServerHandle                   = null;
				output.ItemPath                       = input.ItemPath;
				output.ItemName                       = input.ItemName;
				output.ReqType                        = GetType(input.ReqType);
				output.MaxAge                         = (int)input.MaxAge;
				output.MaxAgeSpecified                = input.MaxAgeSpecified;
				output.Deadband                       = 0;
				output.DeadbandSpecified              = false;
				output.EnableBuffering                = false;
				output.EnableBufferingSpecified       = false;
				output.SamplingRate                   = 0;
				output.SamplingRateSpecified          = false;
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.ReadRequestItem GetItem(Item input)
		{
			OpcXml.Da10.ReadRequestItem output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.ReadRequestItem();
				
				output.ClientItemHandle = (input.ClientHandle != null)?input.ClientHandle.ToString():null;
				output.ItemPath         = input.ItemPath;
				output.ItemName         = input.ItemName;
				output.ReqType          = GetType(input.ReqType);
				output.MaxAge           = input.MaxAge;
				output.MaxAgeSpecified  = input.MaxAgeSpecified;
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.ItemValueList[] GetItemValueLists(OpcXml.Da10.WriteRequestItemList[] input)
		{
			OpcXml.Da.ItemValueList[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da.ItemValueList[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetItemValueList(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.WriteRequestItemList[] GetItemValueLists(OpcXml.Da.ItemValueList[] input)
		{
			OpcXml.Da10.WriteRequestItemList[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.WriteRequestItemList[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetItemValueList(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.ItemValueList GetItemValueList(OpcXml.Da10.WriteRequestItemList input)
		{
			OpcXml.Da.ItemValueList output = null;

			if (input != null)
			{
				output = new OpcXml.Da.ItemValueList();
				
				output.Name                         = null;
				output.ClientHandle                 = null;
				output.ServerHandle                 = null;

				if (input.Items != null)
				{
					foreach (OpcXml.Da10.ItemValue item in input.Items)
					{
						output.Add(GetItemValue(item));
					}
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.WriteRequestItemList GetItemValueList(OpcXml.Da.ItemValueList input)
		{
			OpcXml.Da10.WriteRequestItemList output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.WriteRequestItemList();
				
				output.ItemPath = null;
				output.Items    = new OpcXml.Da10.ItemValue[input.Count];

				for (int ii = 0; ii < output.Items.Length; ii++)
				{
					output.Items[ii] = GetItemValue(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static Opc.Da.ItemValue GetItemValue(OpcXml.Da10.ItemValue input)
		{
			Opc.Da.ItemValue output = null;

			if (input != null)
			{
				output = new Opc.Da.ItemValue();
				
				output.ClientHandle                 = input.ClientItemHandle;
				output.ItemPath                     = input.ItemPath;
				output.ItemName                     = input.ItemName;
				output.Value                        = input.Value;
				output.Quality                      = (input.Quality != null)?GetQuality(input.Quality):Quality.Bad;
				output.QualitySpecified             = (input.Quality != null);
				output.Timestamp                    = (input.TimestampSpecified)?input.Timestamp:DateTime.MinValue;
				output.TimestampSpecified           = (input.TimestampSpecified);
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.ItemValue GetItemValue(Opc.Da.ItemValue input)
		{
			OpcXml.Da10.ItemValue output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.ItemValue();
				
				output.ClientItemHandle      = (input.ClientHandle != null)?input.ClientHandle.ToString():null;
				output.ItemPath              = input.ItemPath;
				output.ItemName              = input.ItemName;
				output.Value                 = input.Value;
				output.ValueTypeQualifier    = null;
				output.Quality               = (input.QualitySpecified)?GetQuality(input.Quality):null;
				output.Timestamp             = (input.TimestampSpecified)?input.Timestamp:DateTime.MinValue;
				output.TimestampSpecified    = (input.TimestampSpecified);
				output.ResultID              = null;
				output.DiagnosticInfo        = null;
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.ItemValueResultList[] GetResultLists(OpcXml.Da10.ReplyItemList[] input)
		{
			OpcXml.Da.ItemValueResultList[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da.ItemValueResultList[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetResultList(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.ReplyItemList[] GetResultLists(OpcXml.Da.ItemValueResultList[] input)
		{
			OpcXml.Da10.ReplyItemList[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.ReplyItemList[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetResultList(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.ItemValueResultList GetResultList(OpcXml.Da10.ReplyItemList input)
		{
			OpcXml.Da.ItemValueResultList output = null;

			if (input != null)
			{
				output = new OpcXml.Da.ItemValueResultList();
				
				output.Name                  = null;
				output.ClientHandle          = null;
				output.ServerHandle          = null;
				output.SamplingRate          = 0;
				output.SamplingRateSpecified = false;

				if (input.Items != null)
				{
					foreach (OpcXml.Da10.ItemValue item in input.Items)
					{
						output.Add(GetResultItem(item));
					}
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.ReplyItemList GetResultList(OpcXml.Da.ItemValueResultList input)
		{
			OpcXml.Da10.ReplyItemList output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.ReplyItemList();
				
				output.Reserved = null;
				output.Items     = new OpcXml.Da10.ItemValue[input.Count];

				for (int ii = 0; ii < output.Items.Length; ii++)
				{
					output.Items[ii] = GetResultItem(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static Opc.Da.ItemValueResult GetResultItem(OpcXml.Da10.ItemValue input)
		{
			Opc.Da.ItemValueResult output = null;

			if (input != null)
			{
				output = new Opc.Da.ItemValueResult();
				
				output.ClientHandle                 = input.ClientItemHandle;
				output.ItemPath                     = input.ItemPath;
				output.ItemName                     = input.ItemName;
				output.Value                        = input.Value;
				output.Quality                      = (input.Quality != null)?GetQuality(input.Quality):Quality.Bad;
				output.QualitySpecified             = (input.Quality != null);
				output.Timestamp                    = (input.TimestampSpecified)?input.Timestamp:DateTime.MinValue;
				output.TimestampSpecified           = (input.TimestampSpecified);
				output.ResultID                     = GetResultID(input.ResultID);
				output.DiagnosticInfo               = input.DiagnosticInfo;
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.ItemValue GetResultItem(Opc.Da.ItemValueResult input)
		{
			OpcXml.Da10.ItemValue output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.ItemValue();
				
				output.ClientItemHandle      = (input.ClientHandle != null)?input.ClientHandle.ToString():null;
				output.ItemPath              = input.ItemPath;
				output.ItemName              = input.ItemName;
				output.Value                 = input.Value;
				output.Quality               = (input.QualitySpecified)?GetQuality(input.Quality):null;
				output.Timestamp             = (input.TimestampSpecified)?input.Timestamp:DateTime.MinValue;
				output.TimestampSpecified    = (input.TimestampSpecified);
				output.ResultID              = GetResultID(input.ResultID);
				output.DiagnosticInfo        = input.DiagnosticInfo;
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.ItemValueResultList[] GetSubscribeResultLists(OpcXml.Da10.SubscribeReplyItemList[] input)
		{
			OpcXml.Da.ItemValueResultList[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da.ItemValueResultList[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetSubscribeResultList(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.SubscribeReplyItemList[] GetSubscribeResultLists(OpcXml.Da.ItemValueResultList[] input)
		{
			OpcXml.Da10.SubscribeReplyItemList[] output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.SubscribeReplyItemList[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = GetSubscribeResultList(input[ii]);
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.ItemValueResultList GetSubscribeResultList(OpcXml.Da10.SubscribeReplyItemList input)
		{
			OpcXml.Da.ItemValueResultList output = null;

			if (input != null)
			{
				output = new OpcXml.Da.ItemValueResultList();
				
				output.Name                  = null;
				output.ServerHandle          = null;
				output.SamplingRate          = (int)input.RevisedSamplingRate;
				output.SamplingRateSpecified = input.RevisedSamplingRateSpecified;

				if (input.Items != null)
				{
					foreach (OpcXml.Da10.SubscribeItemValue item in input.Items)
					{
						output.Add(GetSubscribeResultItem(item));
					}
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.SubscribeReplyItemList GetSubscribeResultList(OpcXml.Da.ItemValueResultList input)
		{
			OpcXml.Da10.SubscribeReplyItemList output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.SubscribeReplyItemList();
				
				output.RevisedSamplingRate          = input.SamplingRate;
				output.RevisedSamplingRateSpecified = input.SamplingRateSpecified;
				output.Items                        = new OpcXml.Da10.SubscribeItemValue[input.Count];

				for (int ii = 0; ii < output.Items.Length; ii++)
				{
					if (input[ii].GetType() == typeof(OpcXml.Da.SubscribeItemValueResult))
					{
						output.Items[ii] = GetSubscribeResultItem((OpcXml.Da.SubscribeItemValueResult)input[ii]);
					}
				}
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da.SubscribeItemValueResult GetSubscribeResultItem(OpcXml.Da10.SubscribeItemValue input)
		{
			OpcXml.Da.SubscribeItemValueResult output = null;

			if (input != null)
			{
				output = new OpcXml.Da.SubscribeItemValueResult();
				
				output.ClientHandle          = input.ItemValue.ClientItemHandle;
				output.ItemPath              = input.ItemValue.ItemPath;
				output.ItemName              = input.ItemValue.ItemName;
				output.Value                 = input.ItemValue.Value;
				output.Quality               = (input.ItemValue.Quality != null)?GetQuality(input.ItemValue.Quality):Quality.Bad;
				output.QualitySpecified      = (input.ItemValue.Quality != null);
				output.Timestamp             = (input.ItemValue.TimestampSpecified)?input.ItemValue.Timestamp:DateTime.MinValue;
				output.TimestampSpecified    = (input.ItemValue.TimestampSpecified);
				output.ResultID              = GetResultID(input.ItemValue.ResultID);
				output.DiagnosticInfo        = input.ItemValue.DiagnosticInfo;
				output.SamplingRate          = (int)input.RevisedSamplingRate;
				output.SamplingRateSpecified = input.RevisedSamplingRateSpecified;
			}

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.SubscribeItemValue GetSubscribeResultItem(OpcXml.Da.SubscribeItemValueResult input)
		{
			OpcXml.Da10.SubscribeItemValue output = null;

			if (input != null)
			{
				output = new OpcXml.Da10.SubscribeItemValue();
				
				output.ItemValue                    = new OpcXml.Da10.ItemValue();
				output.ItemValue.ClientItemHandle   = (input.ClientHandle != null)?input.ClientHandle.ToString():null;
				output.ItemValue.ItemPath           = input.ItemPath;
				output.ItemValue.ItemName           = input.ItemName;
				output.ItemValue.Value              = input.Value;
				output.ItemValue.Quality            = (input.QualitySpecified)?GetQuality(input.Quality):null;
				output.ItemValue.Timestamp          = (input.TimestampSpecified)?input.Timestamp:DateTime.MinValue;
				output.ItemValue.TimestampSpecified = (input.TimestampSpecified);
				output.ItemValue.ResultID           = GetResultID(input.ResultID);
				output.ItemValue.DiagnosticInfo     = input.DiagnosticInfo;
				output.RevisedSamplingRate          = input.SamplingRate;
				output.RevisedSamplingRateSpecified = input.SamplingRateSpecified;
			}

			return output;
		}

		/// <remarks/>
		public static Opc.Da.Quality GetQuality(OpcXml.Da10.OPCQuality input)
		{
			if (input == null)
			{
				return Opc.Da.Quality.Good;
			}

			Opc.Da.Quality output = new Quality();

			output.QualityBits = Opc.Da.qualityBits.good;
			output.LimitBits   = Opc.Da.limitBits.none;
			output.VendorBits  = 0; 

			switch (input.QualityField)
			{
				case OpcXml.Da10.qualityBits.bad:                        { output.QualityBits = Opc.Da.qualityBits.bad;                        break; }
				case OpcXml.Da10.qualityBits.badConfigurationError:      { output.QualityBits = Opc.Da.qualityBits.badConfigurationError;      break; }
				case OpcXml.Da10.qualityBits.badNotConnected:            { output.QualityBits = Opc.Da.qualityBits.badNotConnected;            break; }
				case OpcXml.Da10.qualityBits.badDeviceFailure:           { output.QualityBits = Opc.Da.qualityBits.badDeviceFailure;           break; }
				case OpcXml.Da10.qualityBits.badSensorFailure:           { output.QualityBits = Opc.Da.qualityBits.badSensorFailure;           break; }
				case OpcXml.Da10.qualityBits.badLastKnownValue:          { output.QualityBits = Opc.Da.qualityBits.badLastKnownValue;          break; }
				case OpcXml.Da10.qualityBits.badCommFailure:             { output.QualityBits = Opc.Da.qualityBits.badCommFailure;             break; }
				case OpcXml.Da10.qualityBits.badOutOfService:            { output.QualityBits = Opc.Da.qualityBits.badOutOfService;            break; }
				case OpcXml.Da10.qualityBits.badWaitingForInitialData:   { output.QualityBits = Opc.Da.qualityBits.badWaitingForInitialData;   break; }
				case OpcXml.Da10.qualityBits.uncertain:                  { output.QualityBits = Opc.Da.qualityBits.uncertain;                  break; }
				case OpcXml.Da10.qualityBits.uncertainLastUsableValue:   { output.QualityBits = Opc.Da.qualityBits.uncertainLastUsableValue;   break; }
				case OpcXml.Da10.qualityBits.uncertainSensorNotAccurate: { output.QualityBits = Opc.Da.qualityBits.uncertainSensorNotAccurate; break; }
				case OpcXml.Da10.qualityBits.uncertainEUExceeded:        { output.QualityBits = Opc.Da.qualityBits.uncertainEUExceeded;        break; }
				case OpcXml.Da10.qualityBits.uncertainSubNormal:         { output.QualityBits = Opc.Da.qualityBits.uncertainSubNormal;         break; }
				case OpcXml.Da10.qualityBits.good:                       { output.QualityBits = Opc.Da.qualityBits.good;                       break; }
				case OpcXml.Da10.qualityBits.goodLocalOverride:          { output.QualityBits = Opc.Da.qualityBits.goodLocalOverride;          break; }
			}

			switch (input.LimitField)
			{
				case OpcXml.Da10.limitBits.none:     { output.LimitBits = Opc.Da.limitBits.none;     break; }
				case OpcXml.Da10.limitBits.high:     { output.LimitBits = Opc.Da.limitBits.high;     break; }
				case OpcXml.Da10.limitBits.low:      { output.LimitBits = Opc.Da.limitBits.low;      break; }
				case OpcXml.Da10.limitBits.constant: { output.LimitBits = Opc.Da.limitBits.constant; break; }
			}

			output.VendorBits = (byte)input.VendorField;

			return output;
		}

		/// <remarks/>
		public static OpcXml.Da10.OPCQuality GetQuality(Opc.Da.Quality input)
		{
			OpcXml.Da10.OPCQuality output = new OpcXml.Da10.OPCQuality();

			switch (input.QualityBits)
			{
				case Opc.Da.qualityBits.bad:                        { output.QualityField = OpcXml.Da10.qualityBits.bad;                        break; }
				case Opc.Da.qualityBits.badConfigurationError:      { output.QualityField = OpcXml.Da10.qualityBits.badConfigurationError;      break; }
				case Opc.Da.qualityBits.badNotConnected:            { output.QualityField = OpcXml.Da10.qualityBits.badNotConnected;            break; }
				case Opc.Da.qualityBits.badDeviceFailure:           { output.QualityField = OpcXml.Da10.qualityBits.badDeviceFailure;           break; }
				case Opc.Da.qualityBits.badSensorFailure:           { output.QualityField = OpcXml.Da10.qualityBits.badSensorFailure;           break; }
				case Opc.Da.qualityBits.badLastKnownValue:          { output.QualityField = OpcXml.Da10.qualityBits.badLastKnownValue;          break; }
				case Opc.Da.qualityBits.badCommFailure:             { output.QualityField = OpcXml.Da10.qualityBits.badCommFailure;             break; }
				case Opc.Da.qualityBits.badOutOfService:            { output.QualityField = OpcXml.Da10.qualityBits.badOutOfService;            break; }
				case Opc.Da.qualityBits.badWaitingForInitialData:   { output.QualityField = OpcXml.Da10.qualityBits.badWaitingForInitialData;   break; }
				case Opc.Da.qualityBits.uncertain:                  { output.QualityField = OpcXml.Da10.qualityBits.uncertain;                  break; }
				case Opc.Da.qualityBits.uncertainLastUsableValue:   { output.QualityField = OpcXml.Da10.qualityBits.uncertainLastUsableValue;   break; }
				case Opc.Da.qualityBits.uncertainSensorNotAccurate: { output.QualityField = OpcXml.Da10.qualityBits.uncertainSensorNotAccurate; break; }
				case Opc.Da.qualityBits.uncertainEUExceeded:        { output.QualityField = OpcXml.Da10.qualityBits.uncertainEUExceeded;        break; }
				case Opc.Da.qualityBits.uncertainSubNormal:         { output.QualityField = OpcXml.Da10.qualityBits.uncertainSubNormal;         break; }
				case Opc.Da.qualityBits.good:                       { output.QualityField = OpcXml.Da10.qualityBits.good;                       break; }
				case Opc.Da.qualityBits.goodLocalOverride:          { output.QualityField = OpcXml.Da10.qualityBits.goodLocalOverride;          break; }
			}

			switch (input.LimitBits)
			{
				case Opc.Da.limitBits.none:     { output.LimitField = OpcXml.Da10.limitBits.none;     break; }
				case Opc.Da.limitBits.high:     { output.LimitField = OpcXml.Da10.limitBits.high;     break; }
				case Opc.Da.limitBits.low:      { output.LimitField = OpcXml.Da10.limitBits.low;      break; }
				case Opc.Da.limitBits.constant: { output.LimitField = OpcXml.Da10.limitBits.constant; break; }
			}

			output.VendorField = input.VendorBits;

			return output;
		}
	}
}
