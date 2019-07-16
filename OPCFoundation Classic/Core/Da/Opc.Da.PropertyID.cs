//============================================================================
// TITLE: Opc.Da.Property.cs
//
// CONTENTS:
// 
// Defines static information for well known item properties.
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
// 2002/09/03 RSA   First release.
// 2002/11/16 RSA   Second release.
// 2003/03/23 RSA   Added complex data properties.
// 2004/02/18 RSA   Updated to conform with the .NET design guidelines.
// 2005/11/24 RSA   Made the PropertyID structure serializable.

using System;
using System.Xml;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;

namespace Opc.Da
{
  /// <summary>
  /// Contains a unique identifier for a property.
  /// </summary>
  [Serializable]
  public struct PropertyID: ISerializable
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
    ///<remarks>
    ///<c>MP:</c> During deserialization, SerializationInfo is passed to the class using the constructor provided for this purpose. Any visibility 
    /// constraints placed on the constructor are ignored when the object is deserialized; so you can mark the class as public, 
    /// protected, internal, or private. However, it is best practice to make the constructor protected unless the class is sealed, in which case 
    /// the constructor should be marked private. The constructor should also perform thorough input validation. To avoid misuse by malicious code, 
    /// the constructor should enforce the same security checks and permissions required to obtain an instance of the class using any other 
    /// constructor. 
    /// </remarks>
    /// <summary>
    /// Contructs a server by de-serializing its URL from the stream.
    /// </summary>
    private PropertyID(SerializationInfo info, StreamingContext context)
    {
      SerializationInfoEnumerator enumerator=info.GetEnumerator();
      string name="";
      string ns="";
      enumerator.Reset();
      while(enumerator.MoveNext())
      {
        if(enumerator.Current.Name.Equals(Names.NAME)) 
        {
          name=(string)enumerator.Current.Value;
          continue;
        }
        if(enumerator.Current.Name.Equals(Names.NAMESPACE))
        {
          ns=(string)enumerator.Current.Value;
          continue;
        }
      }
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
    /// Used for properties identified by a qualified name.
    /// </summary>
    public XmlQualifiedName Name 
    {
      get { return m_name; }
    }
		
    /// <summary>
    /// Used for properties identified by a integer.
    /// </summary>
    public int Code
    {
      get { return m_code; }
    }

    /// <summary>
    /// Returns true if the objects are equal.
    /// </summary>
    public static bool operator==(PropertyID a, PropertyID b) 
    {
      return a.Equals(b);
    }

    /// <summary>
    /// Returns true if the objects are not equal.
    /// </summary>
    public static bool operator!=(PropertyID a, PropertyID b) 
    {
      return !a.Equals(b);
    }
        
    #region Constructors
    /// <summary>
    /// Initializes a property identified by a qualified name.
    /// </summary>
    public PropertyID(XmlQualifiedName name) { m_name = name; m_code = 0; }

    /// <summary>
    /// Initializes a property identified by an integer.
    /// </summary>
    public PropertyID(int code) { m_name = null; m_code = code; }

    /// <summary>
    /// Initializes a property identified by a property description.
    /// </summary>
    public PropertyID(string name, int code, string ns) { m_name = new XmlQualifiedName(name, ns); m_code = code; }
    #endregion

    #region Object Member Overrides
    /// <summary>
    /// Returns true if the target object is equal to the object.
    /// </summary>
    public override bool Equals(object target)
    {
      if (target != null && target.GetType() == typeof(PropertyID))
      {
        PropertyID propertyID = (PropertyID)target;

        // compare by integer if both specify valid integers.
        if (propertyID.Code != 0 && Code != 0)
        {
          return (propertyID.Code == Code); 
        }

        // compare by name if both specify valid names.
        if (propertyID.Name != null && Name != null)
        {
          return (propertyID.Name == Name);
        }
      }

      return false;
    }

    /// <summary>
    /// Returns a useful hash code for the object.
    /// </summary>
    public override int GetHashCode()
    {
      if (Code != 0) return Code.GetHashCode();
      if (Name != null) return Name.GetHashCode();
      return base.GetHashCode();
    }

    /// <summary>
    /// Converts the property id to a string.
    /// </summary>
    public override string ToString()
    {
      if (Name != null && Code != 0) return String.Format("{0} ({1})", Name.Name, Code);
      if (Name != null) return Name.Name;
      if (Code != 0) return String.Format("{0}", Code);
      return "";
    }
    #endregion

    #region Private Members
    private int m_code;
    private XmlQualifiedName m_name;
    #endregion
  }

  /// <summary>
  /// Defines identifiers for well-known properties.
  /// </summary>
  public class Property
  {
    #region Data Access Properties
    /// <remarks/>
    public static readonly PropertyID DATATYPE           = new PropertyID("dataType",                  1,   Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID VALUE              = new PropertyID("value",                     2,   Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>    
    public static readonly PropertyID QUALITY            = new PropertyID("quality",                   3,   Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID TIMESTAMP          = new PropertyID("timestamp",                 4,   Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID ACCESSRIGHTS       = new PropertyID("accessRights",              5,   Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID SCANRATE           = new PropertyID("scanRate",                  6,   Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID EUTYPE             = new PropertyID("euType",                    7,   Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID EUINFO             = new PropertyID("euInfo",                    8,   Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID ENGINEERINGUINTS   = new PropertyID("engineeringUnits",          100, Opc.Namespace.OPC_DATA_ACCESS); 
    /// <remarks/>
    public static readonly PropertyID DESCRIPTION        = new PropertyID("description",               101, Opc.Namespace.OPC_DATA_ACCESS); 
    /// <remarks/>
    public static readonly PropertyID HIGHEU             = new PropertyID("highEU",                    102, Opc.Namespace.OPC_DATA_ACCESS); 
    /// <remarks/>
    public static readonly PropertyID LOWEU              = new PropertyID("lowEU",                     103, Opc.Namespace.OPC_DATA_ACCESS); 
    /// <remarks/>
    public static readonly PropertyID HIGHIR             = new PropertyID("highIR",                    104, Opc.Namespace.OPC_DATA_ACCESS); 
    /// <remarks/>
    public static readonly PropertyID LOWIR              = new PropertyID("lowIR",                     105, Opc.Namespace.OPC_DATA_ACCESS); 
    /// <remarks/>
    public static readonly PropertyID CLOSELABEL         = new PropertyID("closeLabel",                106, Opc.Namespace.OPC_DATA_ACCESS); 
    /// <remarks/>     
    public static readonly PropertyID OPENLABEL          = new PropertyID("openLabel",                 107, Opc.Namespace.OPC_DATA_ACCESS); 
    /// <remarks/>
    public static readonly PropertyID TIMEZONE           = new PropertyID("timeZone",                  108, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID CONDITION_STATUS   = new PropertyID("conditionStatus",           300, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID ALARM_QUICK_HELP   = new PropertyID("alarmQuickHelp",            301, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID ALARM_AREA_LIST    = new PropertyID("alarmAreaList",             302, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID PRIMARY_ALARM_AREA = new PropertyID("primaryAlarmArea",          303, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID CONDITION_LOGIC    = new PropertyID("conditionLogic",            304, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID LIMIT_EXCEEDED     = new PropertyID("limitExceeded",             305, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID DEADBAND           = new PropertyID("deadband",                  306, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID HIHI_LIMIT         = new PropertyID("hihiLimit",                 307, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID HI_LIMIT           = new PropertyID("hiLimit",                   308, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID LO_LIMIT           = new PropertyID("loLimit",                   309, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID LOLO_LIMIT         = new PropertyID("loloLimit",                 310, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID RATE_CHANGE_LIMIT  = new PropertyID("rangeOfChangeLimit",        311, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID DEVIATION_LIMIT    = new PropertyID("deviationLimit",            312, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID SOUNDFILE          = new PropertyID("soundFile",                 313, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    #endregion

    #region Complex Data Properties
    public static readonly PropertyID TYPE_SYSTEM_ID      = new PropertyID("typeSystemID",      600, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID DICTIONARY_ID       = new PropertyID("dictionaryID",      601, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID TYPE_ID             = new PropertyID("typeID",            602, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID DICTIONARY          = new PropertyID("dictionary",        603, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID TYPE_DESCRIPTION    = new PropertyID("typeDescription",   604, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID CONSISTENCY_WINDOW  = new PropertyID("consistencyWindow", 605, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID WRITE_BEHAVIOR      = new PropertyID("writeBehavior",     606, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID UNCONVERTED_ITEM_ID = new PropertyID("unconvertedItemID", 607, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID UNFILTERED_ITEM_ID  = new PropertyID("unfilteredItemID",  608, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID DATA_FILTER_VALUE   = new PropertyID("dataFilterValue",   609, Opc.Namespace.OPC_DATA_ACCESS);
    #endregion

    #region XML Data Access Properties
    /// <remarks/>
    public static readonly PropertyID MINIMUM_VALUE       = new PropertyID("minimumValue",      109, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID MAXIMUM_VALUE       = new PropertyID("maximumValue",      110, Opc.Namespace.OPC_DATA_ACCESS);
    /// <remarks/>
    public static readonly PropertyID VALUE_PRECISION     = new PropertyID("valuePrecision",    111, Opc.Namespace.OPC_DATA_ACCESS);
    #endregion
  }

  /// <summary>
  /// Describes an item property.
  /// </summary>
  [Serializable]
  public class PropertyDescription
  {
    /// <summary>
    /// The unique identifier for the property.
    /// </summary>
    public PropertyID ID
    {
      get { return m_id;  }
      set { m_id = value; }
    }
        
    /// <summary>
    /// The .NET data type for the property.
    /// </summary>
    public System.Type Type
    {
      get { return m_type;  }
      set { m_type = value; }
    }

    /// <summary>
    /// The short description defined in the OPC specifications.
    /// </summary>
    public string Name
    {
      get { return m_name;  }
      set { m_name = value; }
    }

    #region Constructors
    /// <summary>
    /// Initializes the object with the specified values.
    /// </summary>
    public PropertyDescription(PropertyID id, System.Type type,	string name)
    {
      ID   = id;
      Type = type;
      Name = name;
    }
    #endregion
        
    #region Object Member Overrides
    /// <summary>
    /// Converts the description to a string.
    /// </summary>
    public override string ToString()
    { 
      return Name; 
    }
    #endregion

    #region Private Members
    private PropertyID m_id;
    private System.Type m_type;
    private string m_name;
    #endregion

    /// <summary>
    /// Returns the description for the specified property.
    /// </summary>
    public static PropertyDescription Find(PropertyID id)
    {
      FieldInfo[] fields = typeof(PropertyDescription).GetFields(BindingFlags.Static | BindingFlags.Public);

      foreach (FieldInfo field in fields)
      {
        PropertyDescription property = (PropertyDescription)field.GetValue(typeof(PropertyDescription));

        if (property.ID == id)
        {
          return property;
        }
      }

      return null;
    }

    /// <summary>
    /// Returns an array of all well-known property descriptions.
    /// </summary>
    public static PropertyDescription[] Enumerate()
    {
      ArrayList values = new ArrayList();

      FieldInfo[] fields = typeof(PropertyDescription).GetFields(BindingFlags.Static | BindingFlags.Public);

      foreach (FieldInfo field in fields)
      {
        values.Add(field.GetValue(typeof(PropertyDescription)));
      }

      return (PropertyDescription[])values.ToArray(typeof(PropertyDescription));
    }

    #region Data Access Properties
    /// <remarks/>
    public static readonly PropertyDescription DATATYPE           = new PropertyDescription(Property.DATATYPE,           typeof(System.Type),  "Item Canonical DataType");
    /// <remarks/>
    public static readonly PropertyDescription VALUE              = new PropertyDescription(Property.VALUE,              typeof(object),       "Item Value");
    /// <remarks/>    
    public static readonly PropertyDescription QUALITY            = new PropertyDescription(Property.QUALITY,            typeof(Quality),      "Item Quality");
    /// <remarks/>
    public static readonly PropertyDescription TIMESTAMP          = new PropertyDescription(Property.TIMESTAMP,          typeof(DateTime),     "Item Timestamp");
    /// <remarks/>
    public static readonly PropertyDescription ACCESSRIGHTS       = new PropertyDescription(Property.ACCESSRIGHTS,       typeof(accessRights), "Item Access Rights");
    /// <remarks/>
    public static readonly PropertyDescription SCANRATE           = new PropertyDescription(Property.SCANRATE,           typeof(float),        "Server Scan Rate");
    /// <remarks/>
    public static readonly PropertyDescription EUTYPE             = new PropertyDescription(Property.EUTYPE,             typeof(euType),       "Item EU Type");
    /// <remarks/>
    public static readonly PropertyDescription EUINFO             = new PropertyDescription(Property.EUINFO,             typeof(string[]),     "Item EU Info");
    /// <remarks/>
    public static readonly PropertyDescription ENGINEERINGUINTS   = new PropertyDescription(Property.ENGINEERINGUINTS,   typeof(string),       "EU Units"); 
    /// <remarks/>
    public static readonly PropertyDescription DESCRIPTION        = new PropertyDescription(Property.DESCRIPTION,        typeof(string),       "Item Description"); 
    /// <remarks/>
    public static readonly PropertyDescription HIGHEU             = new PropertyDescription(Property.HIGHEU,             typeof(double),       "High EU"); 
    /// <remarks/>
    public static readonly PropertyDescription LOWEU              = new PropertyDescription(Property.LOWEU,              typeof(double),       "Low EU"); 
    /// <remarks/>
    public static readonly PropertyDescription HIGHIR             = new PropertyDescription(Property.HIGHIR,             typeof(double),       "High Instrument Range"); 
    /// <remarks/>
    public static readonly PropertyDescription LOWIR              = new PropertyDescription(Property.LOWIR,              typeof(double),       "Low Instrument Range"); 
    /// <remarks/>
    public static readonly PropertyDescription CLOSELABEL         = new PropertyDescription(Property.CLOSELABEL,         typeof(string),       "Contact Close Label"); 
    /// <remarks/>     
    public static readonly PropertyDescription OPENLABEL          = new PropertyDescription(Property.OPENLABEL,          typeof(string),       "Contact Open Label"); 
    /// <remarks/>
    public static readonly PropertyDescription TIMEZONE           = new PropertyDescription(Property.TIMEZONE,           typeof(int),          "Timezone");
    /// <remarks/>
    public static readonly PropertyDescription CONDITION_STATUS   = new PropertyDescription(Property.CONDITION_STATUS,   typeof(string),       "Condition Status");
    /// <remarks/>
    public static readonly PropertyDescription ALARM_QUICK_HELP   = new PropertyDescription(Property.ALARM_QUICK_HELP,   typeof(string),       "Alarm Quick Help");
    /// <remarks/>
    public static readonly PropertyDescription ALARM_AREA_LIST    = new PropertyDescription(Property.ALARM_AREA_LIST,    typeof(string),       "Alarm Area List");
    /// <remarks/>
    public static readonly PropertyDescription PRIMARY_ALARM_AREA = new PropertyDescription(Property.PRIMARY_ALARM_AREA, typeof(string),       "Primary Alarm Area");
    /// <remarks/>
    public static readonly PropertyDescription CONDITION_LOGIC    = new PropertyDescription(Property.CONDITION_LOGIC,    typeof(string),       "Condition Logic");
    /// <remarks/>
    public static readonly PropertyDescription LIMIT_EXCEEDED     = new PropertyDescription(Property.LIMIT_EXCEEDED,     typeof(string),       "Limit Exceeded");
    /// <remarks/>
    public static readonly PropertyDescription DEADBAND           = new PropertyDescription(Property.DEADBAND,           typeof(double),       "Deadband");
    /// <remarks/>
    public static readonly PropertyDescription HIHI_LIMIT         = new PropertyDescription(Property.HIHI_LIMIT,         typeof(double),       "HiHi Limit");
    /// <remarks/>
    public static readonly PropertyDescription HI_LIMIT           = new PropertyDescription(Property.HI_LIMIT,           typeof(double),       "Hi Limit");
    /// <remarks/>
    public static readonly PropertyDescription LO_LIMIT           = new PropertyDescription(Property.LO_LIMIT,           typeof(double),       "Lo Limit");
    /// <remarks/>
    public static readonly PropertyDescription LOLO_LIMIT         = new PropertyDescription(Property.LOLO_LIMIT,         typeof(double),       "LoLo Limit");
    /// <remarks/>
    public static readonly PropertyDescription RATE_CHANGE_LIMIT  = new PropertyDescription(Property.RATE_CHANGE_LIMIT,  typeof(double),       "Rate of Change Limit");
    /// <remarks/>
    public static readonly PropertyDescription DEVIATION_LIMIT    = new PropertyDescription(Property.DEVIATION_LIMIT,    typeof(double),       "Deviation Limit");
    /// <remarks/>
    public static readonly PropertyDescription SOUNDFILE          = new PropertyDescription(Property.SOUNDFILE,          typeof(string),       "Sound File");
    #endregion

    #region Complex Data Properties
    /// <remarks/>
    public static readonly PropertyDescription TYPE_SYSTEM_ID      = new PropertyDescription(Property.TYPE_SYSTEM_ID,      typeof(string),   "Type System ID");
    /// <remarks/>
    public static readonly PropertyDescription DICTIONARY_ID       = new PropertyDescription(Property.DICTIONARY_ID,       typeof(string),   "Dictionary ID");
    /// <remarks/>
    public static readonly PropertyDescription TYPE_ID             = new PropertyDescription(Property.TYPE_ID,             typeof(string),   "Type ID");
    /// <remarks/>
    public static readonly PropertyDescription DICTIONARY          = new PropertyDescription(Property.DICTIONARY,          typeof(object),   "Dictionary");
    /// <remarks/>
    public static readonly PropertyDescription TYPE_DESCRIPTION    = new PropertyDescription(Property.TYPE_DESCRIPTION,    typeof(string),   "Type Description");
    /// <remarks/>
    public static readonly PropertyDescription CONSISTENCY_WINDOW  = new PropertyDescription(Property.CONSISTENCY_WINDOW,  typeof(string),   "Consistency Window");
    /// <remarks/>
    public static readonly PropertyDescription WRITE_BEHAVIOR      = new PropertyDescription(Property.WRITE_BEHAVIOR,      typeof(string),   "Write Behavior");
    /// <remarks/>
    public static readonly PropertyDescription UNCONVERTED_ITEM_ID = new PropertyDescription(Property.UNCONVERTED_ITEM_ID, typeof(string),   "Unconverted Item ID");
    /// <remarks/>
    public static readonly PropertyDescription UNFILTERED_ITEM_ID  = new PropertyDescription(Property.UNFILTERED_ITEM_ID,  typeof(string),   "Unfiltered Item ID");
    /// <remarks/>
    public static readonly PropertyDescription DATA_FILTER_VALUE   = new PropertyDescription(Property.DATA_FILTER_VALUE,   typeof(string),   "Data Filter Value");
    #endregion
		
    #region XML Data Access Properties
    /// <remarks/>
    public static readonly PropertyDescription MINIMUM_VALUE       = new PropertyDescription(Property.MINIMUM_VALUE,       typeof(object),   "Minimum Value");
    /// <remarks/>
    public static readonly PropertyDescription MAXIMUM_VALUE       = new PropertyDescription(Property.MAXIMUM_VALUE,       typeof(object),   "Maximum Value");
    /// <remarks/>
    public static readonly PropertyDescription VALUE_PRECISION     = new PropertyDescription(Property.VALUE_PRECISION,     typeof(object),   "Value Precision");
    #endregion
  }

  /// <summary>
  /// Defines possible item access rights.
  /// </summary>
  public enum accessRights : int
  {
    /// <remarks/>
    readable     = 0x01,
    /// <remarks/>
    writable     = 0x02,
    /// <remarks/>
    readWritable = 0x03
  }

  /// <summary>
  /// Defines possible item engineering unit types
  /// </summary>
  public enum euType : int
  {
    /// <remarks/>
    noEnum     = 0x01,
    /// <remarks/>
    analog     = 0x02,
    /// <remarks/>
    enumerated = 0x03
  }
}
