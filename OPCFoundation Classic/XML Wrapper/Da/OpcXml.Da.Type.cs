//============================================================================
// TITLE: Type.cs
//
// CONTENTS:
// 
// Defines constants for all supported XML-DA types.
//
// (c) Copyright 2002-2003 The OPC Foundation
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

using System;
using System.Xml;
using System.Collections;
using System.Reflection;
using Opc;

namespace OpcXml.Da
{
	/// <summary>
	/// Defines constants for all supported XML-DA types.
	/// </summary>
	public class Type
	{
		/// <remarks/>
		public static readonly XmlQualifiedName ANY_TYPE       = new XmlQualifiedName("anyType",              Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName SBYTE          = new XmlQualifiedName("byte",                 Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName BYTE           = new XmlQualifiedName("unsignedByte",         Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName SHORT          = new XmlQualifiedName("short",                Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName USHORT         = new XmlQualifiedName("unsignedShort",        Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName INT            = new XmlQualifiedName("int",                  Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName UINT           = new XmlQualifiedName("unsignedInt",          Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName LONG           = new XmlQualifiedName("long",                 Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName ULONG          = new XmlQualifiedName("unsignedLong",         Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName FLOAT          = new XmlQualifiedName("float",                Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName DOUBLE         = new XmlQualifiedName("double",               Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName DECIMAL        = new XmlQualifiedName("decimal",              Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName DATETIME       = new XmlQualifiedName("dateTime",             Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName TIME           = new XmlQualifiedName("time",                 Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName DATE           = new XmlQualifiedName("date",                 Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName DURATION       = new XmlQualifiedName("duration",             Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName BOOLEAN        = new XmlQualifiedName("boolean",              Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName STRING         = new XmlQualifiedName("string",               Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName QNAME          = new XmlQualifiedName("QName",                Namespace.XML_SCHEMA);
		/// <remarks/>
		public static readonly XmlQualifiedName BINARY         = new XmlQualifiedName("base64Binary",         Namespace.XML_SCHEMA);	
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_SBYTE    = new XmlQualifiedName("ArrayOfByte",          Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_BYTE     = new XmlQualifiedName("ArrayOfUnsignedByte",  Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_SHORT    = new XmlQualifiedName("ArrayOfShort",         Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_USHORT   = new XmlQualifiedName("ArrayOfUnsignedShort", Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_INT      = new XmlQualifiedName("ArrayOfInt",           Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_UINT     = new XmlQualifiedName("ArrayOfUnsignedInt",   Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_LONG     = new XmlQualifiedName("ArrayOfLong",          Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_ULONG    = new XmlQualifiedName("ArrayOfUnsignedLong",  Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_FLOAT    = new XmlQualifiedName("ArrayOfFloat",         Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_DOUBLE   = new XmlQualifiedName("ArrayOfDouble",        Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_DECIMAL  = new XmlQualifiedName("ArrayOfDecimal",       Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_DATETIME = new XmlQualifiedName("ArrayOfDateTime",      Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_BOOLEAN  = new XmlQualifiedName("ArrayOfBoolean",       Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_STRING   = new XmlQualifiedName("ArrayOfString",        Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ARRAY_ANY_TYPE = new XmlQualifiedName("ArrayOfAnyType",       Namespace.OPC_DATA_ACCESS_XML10);
	}
}
