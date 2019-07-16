/* ========================================================================
 * Copyright (c) 2005-2010 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

#pragma warning disable 1591

namespace OpcRcw.Dx
{
    /// <exclude />
	[ComImport]
	[GuidAttribute("A0C85BB8-4161-4fd6-8655-BB584601C9E0")]
	[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    public interface CATID_OPCDXServer10 {}
    
    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct ItemIdentifier
    {
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szItemPath;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szItemName;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szVersion;
        [MarshalAs(UnmanagedType.I4)]
	    public int dwReserved;
    }
    
    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct IdentifiedResult
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string  szItemPath;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string  szItemName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szVersion;
        [MarshalAs(UnmanagedType.I4)]
        public int hResultCode;
    }
    
    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct DXGeneralResponse
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szConfigurationVersion;
        [MarshalAs(UnmanagedType.I4)]
        public int dwCount;
        public IntPtr pIdentifiedResults; // IdentifiedResult
        [MarshalAs(UnmanagedType.I4)]
        public int dwReserved;
    }
    
    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct SourceServer
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint dwMask;      
        [MarshalAs(UnmanagedType.LPWStr)]                          
	    public string szItemPath;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szItemName;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szVersion;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szName;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szDescription;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szServerType;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szServerURL;
        [MarshalAs(UnmanagedType.I4)]
	    public int bDefaultSourceServerConnected;
        [MarshalAs(UnmanagedType.I4)]
	    public int dwReserved;
    }

    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct DXConnection
    {
        [MarshalAs(UnmanagedType.U4)]
        public uint dwMask;      
        [MarshalAs(UnmanagedType.LPWStr)]    
        public string szItemPath;
        [MarshalAs(UnmanagedType.LPWStr)]    
        public string szItemName;
        [MarshalAs(UnmanagedType.LPWStr)]    
        public string  szVersion;
        [MarshalAs(UnmanagedType.I4)]
		public int dwBrowsePathCount;
	    public IntPtr pszBrowsePaths; // LPWSTR
        [MarshalAs(UnmanagedType.LPWStr)]    
        public string szName;
        [MarshalAs(UnmanagedType.LPWStr)]    
        public string szDescription;
        [MarshalAs(UnmanagedType.LPWStr)]    
        public string szKeyword;
        [MarshalAs(UnmanagedType.I4)]
        public int bDefaultSourceItemConnected;
        [MarshalAs(UnmanagedType.I4)]
        public int bDefaultTargetItemConnected;
        [MarshalAs(UnmanagedType.I4)]
        public int bDefaultOverridden;
        [MarshalAs(UnmanagedType.Struct)]
        public object vDefaultOverrideValue;
        [MarshalAs(UnmanagedType.Struct)]
        public object vSubstituteValue;
        [MarshalAs(UnmanagedType.I4)]
        public int bEnableSubstituteValue;
        [MarshalAs(UnmanagedType.LPWStr)]  
        public string szTargetItemPath;
        [MarshalAs(UnmanagedType.LPWStr)]  
        public string szTargetItemName;
        [MarshalAs(UnmanagedType.LPWStr)]  
        public string szSourceServerName;
        [MarshalAs(UnmanagedType.LPWStr)]  
        public string szSourceItemPath;
        [MarshalAs(UnmanagedType.LPWStr)]  
        public string szSourceItemName;
        [MarshalAs(UnmanagedType.I4)]
        public int dwSourceItemQueueSize;
        [MarshalAs(UnmanagedType.I4)]
        public int dwUpdateRate;
        [MarshalAs(UnmanagedType.R4)]
        public float fltDeadBand;
        [MarshalAs(UnmanagedType.LPWStr)]  
        public string szVendorData;
    }
    
    /// <exclude />
    [ComImport]
    [GuidAttribute("C130D281-F4AA-4779-8846-C2C4CB444F2A")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
    public interface IOPCConfiguration
    { 
        void GetServers(
            [Out][MarshalAs(UnmanagedType.I4)]
            out int pdwCount,
            [Out]
            out IntPtr ppServers // SourceServer
        );

        void AddServers(
            [MarshalAs(UnmanagedType.I4)]
            int dwCount,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=0)]  
            SourceServer[] pServers,
            [Out] 
            out DXGeneralResponse pResponse
        );

        void ModifyServers(
            [MarshalAs(UnmanagedType.I4)]
            int dwCount,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=0)]  
            SourceServer[] pServers,
            [Out] 
            out DXGeneralResponse pResponse
        );

        void DeleteServers(
            [MarshalAs(UnmanagedType.I4)]
            int dwCount,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=0)]  
            ItemIdentifier[] pServers,
            [Out] 
            out DXGeneralResponse pResponse
        );

        void CopyDefaultServerAttributes(
            [MarshalAs(UnmanagedType.I4)]
		    int bConfigToStatus,
            [MarshalAs(UnmanagedType.I4)]
            int dwCount,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=1)]  
            ItemIdentifier[] pServers,
            [Out] 
            out DXGeneralResponse pResponse
        );

        void QueryDXConnections(
            [MarshalAs(UnmanagedType.LPWStr)]
            string szBrowsePath,
            [MarshalAs(UnmanagedType.I4)]
            int dwNoOfMasks,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=1)]  
            DXConnection[] pDXConnectionMasks,
            [MarshalAs(UnmanagedType.I4)]
		    int bRecursive,
            [Out]
            out IntPtr ppErrors, // HRESULT
            [Out][MarshalAs(UnmanagedType.I4)]
            out int pdwCount,
            [Out]
            out IntPtr ppConnections // DXConnection
        );

        void AddDXConnections( 
            [MarshalAs(UnmanagedType.I4)]
            int dwCount,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=0)]  
            DXConnection[] pConnections,
            [Out]
            out DXGeneralResponse pResponse
        );
        
        void UpdateDXConnections(
            [MarshalAs(UnmanagedType.LPWStr)]
		    string szBrowsePath,
            [MarshalAs(UnmanagedType.I4)]
            int dwNoOfMasks,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=1)]  
            DXConnection[] pDXConnectionMasks,
            [MarshalAs(UnmanagedType.I4)]
		    int bRecursive,
		    ref DXConnection pDXConnectionDefinition,
            [Out][MarshalAs(UnmanagedType.I4)]
            out IntPtr ppErrors, // HRESULT
            [Out]
            out DXGeneralResponse pResponse
        );

        void ModifyDXConnections(
            [MarshalAs(UnmanagedType.I4)]
            int dwCount,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=0)]  
            DXConnection[] pDXConnectionDefinitions,
            [Out]
            out DXGeneralResponse pResponse
        );

        void DeleteDXConnections(
            [MarshalAs(UnmanagedType.LPWStr)]
		    string szBrowsePath,
            [MarshalAs(UnmanagedType.I4)]
            int dwNoOfMasks,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=1)]  
            DXConnection[] pDXConnectionMasks,
            [MarshalAs(UnmanagedType.I4)]
		    int bRecursive,
            [Out][MarshalAs(UnmanagedType.I4)]
            out IntPtr ppErrors, // HRESULT
            [Out]
            out DXGeneralResponse pResponse
        );

        void CopyDXConnectionDefaultAttributes(
            [MarshalAs(UnmanagedType.I4)]
		    int bConfigToStatus,
            [MarshalAs(UnmanagedType.LPWStr)]
		    string szBrowsePath,
            [MarshalAs(UnmanagedType.I4)]
            int dwNoOfMasks,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=2)]  
            DXConnection[] pDXConnectionMasks,
            [MarshalAs(UnmanagedType.I4)]
		    int bRecursive,
            [Out][MarshalAs(UnmanagedType.I4)]
            out IntPtr ppErrors, // HRESULT
            [Out]
            out DXGeneralResponse pResponse
        );

        void ResetConfiguration(
            [MarshalAs(UnmanagedType.LPWStr)]
            string szConfigurationVersion,
            [Out][MarshalAs(UnmanagedType.LPWStr)]
            out string pszConfigurationVersion
        );
    };
    
    /// <exclude />
	public static class Names
	{
		// category description strings.
		public const string  OPC_CATEGORY_DESCRIPTION_DX10         = "OPC Data Exchange Servers Version 1.0";

		// common names.
		public const string  OPCDX_NAMESPACE_V10                   = "http://opcfoundation.org/webservices/OPCDX/10";
		public const string  OPCDX_DATABASE_ROOT                   = "DX";
		public const string  OPCDX_SEPARATOR						= "/";
		public const string  OPCDX_ITEM_PATH                       = "ItemPath";
		public const string  OPCDX_ITEM_NAME                       = "ItemName";
		public const string  OPCDX_VERSION                         = "Version";

		// server status.
		public const string  OPCDX_SERVER_STATUS_TYPE              = "DXServerStatus";
		public const string  OPCDX_SERVER_STATUS                   = "ServerStatus";
		public const string  OPCDX_CONFIGURATION_VERSION           = "ConfigurationVersion";
		public const string  OPCDX_SERVER_STATE                    = "ServerState";
		public const string  OPCDX_CONNECTION_COUNT                = "DXConnectionCount";
		public const string  OPCDX_MAX_CONNECTIONS                 = "MaxDXConnections";
		public const string  OPCDX_SERVER_ERROR_ID                 = "ErrorID";
		public const string  OPCDX_SERVER_ERROR_DIAGNOSTIC         = "ErrorDiagnostic";
		public const string  OPCDX_DIRTY_FLAG                      = "DirtyFlag";
		public const string  OPCDX_SOURCE_SERVER_TYPES             = "SourceServerTypes";
		public const string  OPCDX_MAX_QUEUE_SIZE                  = "MaxQueueSize";

		// connection configuration.
		public const string  OPCDX_CONNECTIONS_ROOT                = "DXConnectionsRoot";
		public const string  OPCDX_CONNECTION_TYPE                 = "DXConnection";
		public const string  OPCDX_CONNECTION_NAME                 = "Name";
		public const string  OPCDX_CONNECTION_BROWSE_PATHS         = "BrowsePath";
		public const string  OPCDX_CONNECTION_VERSION              = "Version";
		public const string  OPCDX_CONNECTION_DESCRIPTION          = "Description";
		public const string  OPCDX_CONNECTION_KEYWORD              = "Keyword";
		public const string  OPCDX_DEFAULT_SOURCE_ITEM_CONNECTED   = "DefaultSourceItemConnected";
		public const string  OPCDX_DEFAULT_TARGET_ITEM_CONNECTED   = "DefaultTargetItemConnected";
		public const string  OPCDX_DEFAULT_OVERRIDDEN              = "DefaultOverridden";
		public const string  OPCDX_DEFAULT_OVERRIDE_VALUE          = "DefaultOverrideValue";
		public const string  OPCDX_ENABLE_SUBSTITUTE_VALUE         = "EnableSubstituteValue";
		public const string  OPCDX_SUBSTITUTE_VALUE                = "SubstituteValue";
		public const string  OPCDX_TARGET_ITEM_PATH                = "TargetItemPath";
		public const string  OPCDX_TARGET_ITEM_NAME                = "TargetItemName";
		public const string  OPCDX_CONNECTION_SOURCE_SERVER_NAME   = "SourceServerName";
		public const string  OPCDX_SOURCE_ITEM_PATH                = "SourceItemPath";
		public const string  OPCDX_SOURCE_ITEM_NAME                = "SourceItemName";
		public const string  OPCDX_SOURCE_ITEM_QUEUE_SIZE          = "QueueSize";
		public const string  OPCDX_UPDATE_RATE                     = "UpdateRate";
		public const string  OPCDX_DEADBAND                        = "Deadband";
		public const string  OPCDX_VENDOR_DATA                     = "VendorData";
	
		// connection status.
		public const string  OPCDX_CONNECTION_STATUS               = "Status";
		public const string  OPCDX_CONNECTION_STATUS_TYPE          = "DXConnectionStatus";
		public const string  OPCDX_CONNECTION_STATE                = "DXConnectionState";
		public const string  OPCDX_WRITE_VALUE                     = "WriteValue";
		public const string  OPCDX_WRITE_TIMESTAMP                 = "WriteTimestamp";
		public const string  OPCDX_WRITE_QUALITY                   = "WriteQuality";
		public const string  OPCDX_WRITE_ERROR_ID                  = "WriteErrorID";
		public const string  OPCDX_WRITE_ERROR_DIAGNOSTIC          = "WriteErrorDiagnostic";
		public const string  OPCDX_SOURCE_VALUE                    = "SourceValue";
		public const string  OPCDX_SOURCE_TIMESTAMP                = "SourceTimestamp";
		public const string  OPCDX_SOURCE_QUALITY                  = "SourceQuality";
		public const string  OPCDX_SOURCE_ERROR_ID                 = "SourceErrorID";
		public const string  OPCDX_SOURCE_ERROR_DIAGNOSTIC         = "SourceErrorDiagnostic";
		public const string  OPCDX_ACTUAL_UPDATE_RATE              = "ActualUpdateRate";
		public const string  OPCDX_QUEUE_HIGH_WATER_MARK           = "QueueHighWaterMark";
		public const string  OPCDX_QUEUE_FLUSH_COUNT               = "QueueFlushCount";
		public const string  OPCDX_SOURCE_ITEM_CONNECTED           = "SourceItemConnected";
		public const string  OPCDX_TARGET_ITEM_CONNECTED           = "TargetItemConnected";
		public const string  OPCDX_OVERRIDDEN                      = "Overridden";
		public const string  OPCDX_OVERRIDE_VALUE                  = "OverrideValue";
		
		// source server configuration.
		public const string  OPCDX_SOURCE_SERVERS_ROOT             = "SourceServers";
		public const string  OPCDX_SOURCE_SERVER_TYPE              = "SourceServer";
		public const string  OPCDX_SOURCE_SERVER_NAME              = "Name";
		public const string  OPCDX_SOURCE_SERVER_VERSION           = "Version";
		public const string  OPCDX_SOURCE_SERVER_DESCRIPTION       = "Description";
		public const string  OPCDX_SERVER_URL                      = "ServerURL";
		public const string  OPCDX_SERVER_TYPE                     = "ServerType";
		public const string  OPCDX_DEFAULT_SOURCE_SERVER_CONNECTED = "DefaultSourceServerConnected";

		// source server status.
		public const string  OPCDX_SOURCE_SERVER_STATUS_TYPE       = "DXSourceServerStatus";
		public const string  OPCDX_SOURCE_SERVER_STATUS            = "Status";
		public const string  OPCDX_SERVER_CONNECT_STATUS           = "ConnectStatus";
		public const string  OPCDX_SOURCE_SERVER_ERROR_ID          = "ErrorID";
		public const string  OPCDX_SOURCE_SERVER_ERROR_DIAGNOSTIC  = "ErrorDiagnostic";
		public const string  OPCDX_LAST_CONNECT_TIMESTAMP          = "LastConnectTimestamp";
		public const string  OPCDX_LAST_CONNECT_FAIL_TIMESTAMP     = "LastConnectFailTimestamp";
		public const string  OPCDX_CONNECT_FAIL_COUNT              = "ConnectFailCount";
		public const string  OPCDX_PING_TIME                       = "PingTime";
		public const string  OPCDX_LAST_DATA_CHANGE_TIMESTAMP      = "LastDataChangeTimestamp";
		public const string  OPCDX_SOURCE_SERVER_CONNECTED         = "SourceServerConnected";

		// quality
		public const string  OPCDX_QUALITY                         = "DXQuality";
		public const string  OPCDX_QUALITY_STATUS                  = "Quality";
		public const string  OPCDX_LIMIT_BITS                      = "LimitBits";
		public const string  OPCDX_VENDOR_BITS                     = "VendorBits";

		// error
		public const string  OPCDX_ERROR                           = "OPCError";
		public const string  OPCDX_ERROR_ID                        = "ID";
		public const string  OPCDX_ERROR_TEXT                      = "Text";

		// source server url scheme names.
		public const string  OPCDX_SOURCE_SERVER_URL_SCHEME_OPCDA  = "opcda";
		public const string  OPCDX_SOURCE_SERVER_URL_SCHEME_XMLDA  = "http";
	}
    
    /// <exclude />
	public static class QualityStatusName
	{
		public const string  OPCDX_QUALITY_BAD                           = "bad";
		public const string  OPCDX_QUALITY_BAD_CONFIG_ERROR              = "badConfigurationError";
		public const string  OPCDX_QUALITY_BAD_NOT_CONNECTED             = "badNotConnected";
		public const string  OPCDX_QUALITY_BAD_DEVICE_FAILURE            = "badDeviceFailure";
		public const string  OPCDX_QUALITY_BAD_SENSOR_FAILURE            = "badSensorFailure";
		public const string  OPCDX_QUALITY_BAD_LAST_KNOWN_VALUE          = "badLastKnownValue";
		public const string  OPCDX_QUALITY_BAD_COMM_FAILURE              = "badCommFailure";
		public const string  OPCDX_QUALITY_BAD_OUT_OF_SERVICE            = "badOutOfService";
		public const string  OPCDX_QUALITY_UNCERTAIN                     = "uncertain";
		public const string  OPCDX_QUALITY_UNCERTAIN_LAST_USABLE_VALUE   = "uncertainLastUsableValue";
		public const string  OPCDX_QUALITY_UNCERTAIN_SENSOR_NOT_ACCURATE = "uncertainSensorNotAccurate";
		public const string  OPCDX_QUALITY_UNCERTAIN_EU_EXCEEDED         = "uncertainEUExceeded";
		public const string  OPCDX_QUALITY_UNCERTAIN_SUB_NORMAL          = "uncertainSubNormal";
		public const string  OPCDX_QUALITY_GOOD                          = "good";
		public const string  OPCDX_QUALITY_GOOD_LOCAL_OVERRIDE           = "goodLocalOverride";
	}
    
    /// <exclude />
	public static class LimitStatusName
	{
		public const string  OPCDX_LIMIT_NONE     = "none";
		public const string  OPCDX_LIMIT_LOW      = "low";
		public const string  OPCDX_LIMIT_HIGH     = "high";
		public const string  OPCDX_LIMIT_CONSTANT = "constant";
	}
    
    /// <exclude />
	public static class ServerTypeName
	{
		public const string  OPCDX_SERVER_TYPE_COM_DA10  = "COM-DA1.0";
		public const string  OPCDX_SERVER_TYPE_COM_DA204 = "COM-DA2.04";
		public const string  OPCDX_SERVER_TYPE_COM_DA205 = "COM-DA2.05";
		public const string  OPCDX_SERVER_TYPE_COM_DA30  = "COM-DA3.0";
		public const string  OPCDX_SERVER_TYPE_XML_DA10  = "XML-DA1.0";
	}
    
    /// <exclude />
	public static class ServerStateName
	{
		public const string  OPCDX_SERVER_STATE_RUNNING    = "running";
		public const string  OPCDX_SERVER_STATE_FAILED     = "failed";
		public const string  OPCDX_SERVER_STATE_NOCONFIG   = "noConfig";
		public const string  OPCDX_SERVER_STATE_SUSPENDED  = "suspended";
		public const string  OPCDX_SERVER_STATE_TEST       = "test";
		public const string  OPCDX_SERVER_STATE_COMM_FAULT = "commFault";
		public const string  OPCDX_SERVER_STATE_UNKNOWN    = "unknown";
	}
	
    /// <exclude />
	public static class ConnectStatusName
	{
		public const string  OPCDX_CONNECT_STATUS_CONNECTED    = "connected";
		public const string  OPCDX_CONNECT_STATUS_DISCONNECTED = "disconnected";
		public const string  OPCDX_CONNECT_STATUS_CONNECTING   = "connecting";
		public const string  OPCDX_CONNECT_STATUS_FAILED       = "failed";
	} 
    
    /// <exclude />
	public static class ConnectionStateName
	{		
		public const string  OPCDX_CONNECTION_STATE_INITIALIZING                = "initializing";
		public const string  OPCDX_CONNECTION_STATE_OPERATIONAL                 = "operational";
		public const string  OPCDX_CONNECTION_STATE_DEACTIVATED                 = "deactivated";
		public const string  OPCDX_CONNECTION_STATE_SOURCE_SERVER_NOT_CONNECTED = "sourceServerNotConnected";
		public const string  OPCDX_CONNECTION_STATE_SUBSCRIPTION_FAILED         = "subscriptionFailed";
		public const string  OPCDX_CONNECTION_STATE_TARGET_ITEM_NOT_FOUND       = "targetItemNotFound";
	} 

	// source server type enumeration.
	public enum ServerType
	{
		ServerType_COM_DA10  = 1,
		ServerType_COM_DA204,
		ServerType_COM_DA205,
		ServerType_COM_DA30,
		ServerType_XML_DA10
	}

	// source server state enumeration - intentionally compatible with OPCSERVERSTATE.
	public enum ServerState 
	{ 
		ServerState_RUNNING = 1, 
		ServerState_FAILED, 
		ServerState_NOCONFIG, 
		ServerState_SUSPENDED, 
		ServerState_TEST,
		ServerState_COMM_FAULT,
		ServerState_UNKNOWN
	} 

	// connection state enumeration.
	public enum ConnectionState
	{
		ConnectionState_INITIALIZING = 1,
		ConnectionState_OPERATIONAL,
		ConnectionState_DEACTIVATED,
		ConnectionState_SOURCE_SERVER_NOT_CONNECTED,
		ConnectionState_SUBSCRIPTION_FAILED,
		ConnectionState_TARGET_ITEM_NOT_FOUND
	}

	// source server connect status enumeration.
	public enum ConnectStatus
	{
		ConnectStatus_CONNECTED = 1,
		ConnectStatus_DISCONNECTED,
		ConnectStatus_CONNECTING,
		ConnectStatus_FAILED
	}

	// bit masks for optional fields in source server or connection structures.
    public enum Mask
    {
		None                         = 0x0,
		ItemPath                     = 0x1,
		ItemName                     = 0x2,
		Version                      = 0x4,
		BrowsePaths                  = 0x8,
		Name                         = 0x10,
		Description                  = 0x20,
		Keyword                      = 0x40,
		DefaultSourceItemConnected   = 0x80,
		DefaultTargetItemConnected   = 0x100,
		DefaultOverridden            = 0x200,
		DefaultOverrideValue         = 0x400,
		SubstituteValue              = 0x800,
		EnableSubstituteValue        = 0x1000,
		TargetItemPath               = 0x2000,
		TargetItemName               = 0x4000,
		SourceServerName             = 0x8000,
		SourceItemPath               = 0x10000,
		SourceItemName               = 0x20000,
		SourceItemQueueSize          = 0x40000,
		UpdateRate                   = 0x80000,
		DeadBand                     = 0x100000,
		VendorData                   = 0x200000,
		ServerType                   = 0x400000,
		ServerURL                    = 0x800000,
		DefaultSourceServerConnected = 0x1000000,
        All                          = 0x7FFFFFFF
    }
}
