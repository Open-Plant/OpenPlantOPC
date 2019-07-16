//============================================================================
// TITLE: Property.cs
//
// CONTENTS:
// 
// Defines static information for well known item properties.
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
// 2002/09/03 RSA   First release.
// 2002/11/16 RSA   Second release.
// 2003/03/23 RSA   Added complex data properties.

using System;
using System.Xml;
using System.Collections;
using System.Reflection;

namespace OpcXml.Da
{
	/// <summary>
	/// Defines identifiers for well-known properties.
	/// </summary>
	public class Property
	{
		/// <remarks/>
		public static readonly XmlQualifiedName DATATYPE           = new XmlQualifiedName("dataType",              Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName VALUE              = new XmlQualifiedName("value",                 Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>    
		public static readonly XmlQualifiedName QUALITY            = new XmlQualifiedName("quality",               Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName TIMESTAMP          = new XmlQualifiedName("timestamp",             Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ACCESSRIGHTS       = new XmlQualifiedName("accessRights",          Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName SCANRATE           = new XmlQualifiedName("scanRate",              Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName EUTYPE             = new XmlQualifiedName("euType",                Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName EUINFO             = new XmlQualifiedName("euInfo",                Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ENGINEERINGUINTS   = new XmlQualifiedName("engineeringUnits",      Opc.Namespace.OPC_DATA_ACCESS_XML10); 
		/// <remarks/>
		public static readonly XmlQualifiedName DESCRIPTION        = new XmlQualifiedName("description",           Opc.Namespace.OPC_DATA_ACCESS_XML10); 
		/// <remarks/>
		public static readonly XmlQualifiedName HIGHEU             = new XmlQualifiedName("highEU",                Opc.Namespace.OPC_DATA_ACCESS_XML10); 
		/// <remarks/>
		public static readonly XmlQualifiedName LOWEU              = new XmlQualifiedName("lowEU",                 Opc.Namespace.OPC_DATA_ACCESS_XML10); 
		/// <remarks/>
		public static readonly XmlQualifiedName HIGHIR             = new XmlQualifiedName("highIR",                Opc.Namespace.OPC_DATA_ACCESS_XML10); 
		/// <remarks/>
		public static readonly XmlQualifiedName LOWIR              = new XmlQualifiedName("lowIR",                 Opc.Namespace.OPC_DATA_ACCESS_XML10); 
		/// <remarks/>
		public static readonly XmlQualifiedName CLOSELABEL         = new XmlQualifiedName("closeLabel",            Opc.Namespace.OPC_DATA_ACCESS_XML10); 
		/// <remarks/>     
		public static readonly XmlQualifiedName OPENLABEL          = new XmlQualifiedName("openLabel",             Opc.Namespace.OPC_DATA_ACCESS_XML10); 
		/// <remarks/>
		public static readonly XmlQualifiedName TIMEZONE           = new XmlQualifiedName("timeZone",              Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName CONDITION_STATUS   = new XmlQualifiedName("conditionStatus",       Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ALARM_QUICK_HELP   = new XmlQualifiedName("alarmQuickHelp",        Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName ALARM_AREA_LIST    = new XmlQualifiedName("alarmAreaList",         Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName PRIMARY_ALARM_AREA = new XmlQualifiedName("primaryAlarmArea",      Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName CONDITION_LOGIC    = new XmlQualifiedName("conditionLogic",        Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName LIMIT_EXCEEDED     = new XmlQualifiedName("limitExceeded",         Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName DEADBAND           = new XmlQualifiedName("deadband",              Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName HIHI_LIMIT         = new XmlQualifiedName("hihiLimit",             Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName HI_LIMIT           = new XmlQualifiedName("hiLimit",               Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName LO_LIMIT           = new XmlQualifiedName("loLimit",               Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName LOLO_LIMIT         = new XmlQualifiedName("loloLimit",             Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName RATE_CHANGE_LIMIT  = new XmlQualifiedName("rangeOfChangeLimit",    Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName DEVIATION_LIMIT    = new XmlQualifiedName("deviationLimit",        Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName SOUNDFILE          = new XmlQualifiedName("soundFile",             Opc.Namespace.OPC_DATA_ACCESS_XML10);
		
		//======================================================================
		// Complex Data Properties

		/// <remarks/>
		public static readonly XmlQualifiedName TYPE_SYSTEM_ID      = new XmlQualifiedName("typeSystemID",          Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName DICTIONARY_ID       = new XmlQualifiedName("dictionaryID",          Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName TYPE_ID             = new XmlQualifiedName("typeID",                Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName DICTIONARY          = new XmlQualifiedName("dictionary",            Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName TYPE_DESCRIPTION    = new XmlQualifiedName("typeDescription",       Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName CONSISTENCY_WINDOW  = new XmlQualifiedName("consistencyWindow",     Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName WRITE_BEHAVIOR      = new XmlQualifiedName("writeBehavior",         Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName UNCONVERTED_ITEM_ID = new XmlQualifiedName("unconvertedItemID",     Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName UNFILTERED_ITEM_ID  = new XmlQualifiedName("unfilteredItemID",      Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName DATA_FILTER_VALUE   = new XmlQualifiedName("dataFilterValue",       Opc.Namespace.OPC_DATA_ACCESS_XML10);
	
		//======================================================================
		// XML Data Access Properties

		/// <remarks/>
		public static readonly XmlQualifiedName MINIMUM_VALUE       = new XmlQualifiedName("minimumValue",          Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName MAXIMUM_VALUE       = new XmlQualifiedName("maximumValue",          Opc.Namespace.OPC_DATA_ACCESS_XML10);
		/// <remarks/>
		public static readonly XmlQualifiedName VALUE_PRECISION     = new XmlQualifiedName("valuePrecision",        Opc.Namespace.OPC_DATA_ACCESS_XML10);
	}
}
