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

namespace OpcRcw.Cmd
{
    /// <exclude />
	[ComImport]
	[GuidAttribute("2D869D5C-3B05-41fb-851A-642FB2B801A0")]
	[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    public interface CATID_OPCCMDServer10 {}
    
    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct OpcCmdNamespaceDefinition
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szUri;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szDescription;
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfCommandNames;
        public IntPtr pszCommandNames;
    }
    
    /// <exclude />
    public enum OpcCmdBrowseFilter
    {
        OpcCmdBrowseFilter_All,
        OpcCmdBrowseFilter_Branch,
        OpcCmdBrowseFilter_Target
    }
    
    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct OpcCmdTargetElement
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szLabel;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szTargetID;
        [MarshalAs(UnmanagedType.I4)]
        public int bIsTarget;
        [MarshalAs(UnmanagedType.I4)]
        public int bHasChildren;
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfNamespaceUris;
        public IntPtr pszNamespaceUris;
    }
    
    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct OpcCmdEventDefinition
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szDescription;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szDataTypeDefinition;
        [MarshalAs(UnmanagedType.I4)]
	    public int dwReserved;
    }
    
    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct OpcCmdStateDefinition
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szDescription;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szDataTypeDefinition;
        [MarshalAs(UnmanagedType.I4)]
	    public int dwReserved;
    }
    
    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct OpcCmdActionDefinition
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szDescription;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szEventName;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szInArguments;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szOutArguments;
        [MarshalAs(UnmanagedType.I4)]
        public int dwReserved;
    }

    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct OpcCmdStateTransition
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szTransitionID;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szStartState;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szEndState;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szTriggerEvent;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szAction;
        [MarshalAs(UnmanagedType.I4)]
	    public int dwReserved;
    }

    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct OpcCmdArgumentDefinition
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szName;
        [MarshalAs(UnmanagedType.I2)]
	    public short vtValueType;
        [MarshalAs(UnmanagedType.I2)]
	    public short wReserved;
        [MarshalAs(UnmanagedType.I4)]
	    public int bOptional;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szDescription;
        [MarshalAs(UnmanagedType.Struct)]
        public object vDefaultValue;
        [MarshalAs(UnmanagedType.LPWStr)]
	    public string szUnitType;
        [MarshalAs(UnmanagedType.I4)]
	    public int dwReserved;
        [MarshalAs(UnmanagedType.Struct)]
        public object vLowLimit;
        [MarshalAs(UnmanagedType.Struct)]
        public object vHighLimit;
    }
    
    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct OpcCmdArgument
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szName;
        [MarshalAs(UnmanagedType.Struct)]
        public object vValue;
    }
    
    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct OpcCmdCommandDescription
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szDescription;
        [MarshalAs(UnmanagedType.I4)]
        public int bIsGlobal;
        [MarshalAs(UnmanagedType.R8)]
        public double dblExecutionTime;
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfEventDefinitions;   
        public IntPtr pEventDefinitions; // OpcCmdEventDefinition
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfStateDefinitions;   
        public IntPtr pStateDefinitions; // OpcCmdStateDefinition
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfActionDefinitions;   
        public IntPtr pActionDefinitions; // OpcCmdActionDefinition
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfTransitions;   
        public IntPtr pTransitions; // OpcCmdStateTransition
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfInArguments;   
        public IntPtr pInArguments; // OpcCmdArgumentDefinition
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfOutArguments;   
        public IntPtr pOutArguments; // OpcCmdArgumentDefinition
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfSupportedControls;   
        public IntPtr pszSupportedControls; // LPWSTR
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfAndDependencies;   
        public IntPtr pszAndDependencies; // LPWSTR
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfOrDependencies;   
        public IntPtr pszOrDependencies; // LPWSTR
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfNotDependencies;  
        public IntPtr pszNotDependencies; // LPWSTR
    }

    /// <exclude />
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct OpcCmdStateChangeEvent
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szEventName;
        [MarshalAs(UnmanagedType.I4)]
        public int dwReserved;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftEventTime;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szEventData;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szOldState;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szNewState;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szStateData;
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfInArguments;
        public IntPtr pInArguments; // OpcCmdArgument
        [MarshalAs(UnmanagedType.I4)]
        public int dwNoOfOutArguments;
        public IntPtr pOutArguments; // OpcCmdArgument
    }
    
    /// <exclude />
	[ComImport]
	[GuidAttribute("3104B527-2016-442d-9696-1275DE978778")]
	[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
    public interface IOPCComandCallback
    { 
	    void OnStateChange(
            [MarshalAs(UnmanagedType.I4)]
            int dwNoOfEvents,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=0)]  
		    OpcCmdStateChangeEvent[] pEvents,
            [MarshalAs(UnmanagedType.I4)]
		    int dwNoOfPermittedControls,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPWStr, SizeParamIndex=2)]  
		    string pszPermittedControls,
            [MarshalAs(UnmanagedType.I4)]
		    int bNoStateChange);
    };
    
    /// <exclude />
	[ComImport]
	[GuidAttribute("3104B525-2016-442d-9696-1275DE978778")]
	[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
    public interface IOPCCommandInformation
    { 
	    void QueryCapabilities(
		    [Out][MarshalAs(UnmanagedType.R8)]  
		    out double pdblMaxStorageTime,
		    [Out][MarshalAs(UnmanagedType.I4)]  
		    out int pbSupportsEventFilter
	    );

	    void QueryComands(
            [Out][MarshalAs(UnmanagedType.I4)]
            out int pdwCount,
            [Out]
		    out IntPtr ppNamespaces // OpcCmdNamespaceDefinition
        );

	    void BrowseCommandTargets(
            [MarshalAs(UnmanagedType.LPWStr)]  
		    string szTargetID,
            [MarshalAs(UnmanagedType.LPWStr)]  
		    string szNamespaceUri,
		    OpcCmdBrowseFilter eBrowseFilter,
		    [Out][MarshalAs(UnmanagedType.I4)]  
            out int pdwCount,
            [Out]
		    out IntPtr ppTargets // OpcCmdTargetElement
	    );
    	
	    void GetCommandDescription(
            [MarshalAs(UnmanagedType.LPWStr)]  
		    string szCommandName,
            [MarshalAs(UnmanagedType.LPWStr)]  
		    string szNamespaceUri,
            [Out]
		    out OpcCmdCommandDescription pDescription
	    );
    };
    
    
    /// <exclude />
	[ComImport]
	[GuidAttribute("3104B526-2016-442d-9696-1275DE978778")]
	[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
    public interface IOPCCommandExecution
    { 
        void SyncInvoke(
            [MarshalAs(UnmanagedType.LPWStr)]  
	        string szCommandName,
            [MarshalAs(UnmanagedType.LPWStr)]  
	        string szNamespaceUri,
            [MarshalAs(UnmanagedType.LPWStr)]  
	        string szTargetID,
            [MarshalAs(UnmanagedType.I4)]  
	        int dwNoOfArguments,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=3)]  
            OpcCmdArgument[] pArguments,
            [MarshalAs(UnmanagedType.I4)]  
	        int dwNoOfFilters,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPWStr, SizeParamIndex=5)]  
            string[] pszFilters,
            [Out][MarshalAs(UnmanagedType.I4)]  
            out int pdwNoOfEvents,
            [Out]
	        out IntPtr ppEvents // OpcCmdStateChangeEvent
        );

        void AsyncInvoke(
            [MarshalAs(UnmanagedType.LPWStr)]  
	        string szCommandName,
            [MarshalAs(UnmanagedType.LPWStr)]  
	        string szNamespaceUri,
            [MarshalAs(UnmanagedType.LPWStr)]  
	        string szTargetID,
            [MarshalAs(UnmanagedType.I4)]  
	        int dwNoOfArguments,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStruct, SizeParamIndex=3)]  
            OpcCmdArgument[] pArguments,
            [MarshalAs(UnmanagedType.I4)]  
	        int dwNoOfFilters,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPWStr, SizeParamIndex=5)]  
            string[] pszFilters,
	        IOPCComandCallback ipCallback,
            [MarshalAs(UnmanagedType.I4)]  
	        int dwUpdateFrequency,
            [MarshalAs(UnmanagedType.I4)]  
	        int dwKeepAliveTime,
            [Out][MarshalAs(UnmanagedType.LPWStr)]  
	        out string pszInvokeUUID,
            [Out][MarshalAs(UnmanagedType.I4)]  
	        out int pdwRevisedUpdateFrequency
        );

        void Connect(
            [MarshalAs(UnmanagedType.LPWStr)]  
	        string szInvokeUUID,
	        IOPCComandCallback ipCallback,
            [MarshalAs(UnmanagedType.I4)]  
	        int dwUpdateFrequency,
            [MarshalAs(UnmanagedType.I4)]  
	        int dwKeepAliveTime,
            [Out][MarshalAs(UnmanagedType.I4)]  
	        out int pdwRevisedUpdateFrequency
        );

        void Disconnect(
            [MarshalAs(UnmanagedType.LPWStr)]
	        string szInvokeUUID
        );

        void QueryState(
            [MarshalAs(UnmanagedType.LPWStr)]  
	        string szInvokeUUID,
            [MarshalAs(UnmanagedType.I4)]  
	        int dwWaitTime,
            [Out][MarshalAs(UnmanagedType.I4)]  
            out int pdwNoOfEvents,
	        out IntPtr ppEvents, // OpcCmdStateChangeEvent
            [Out][MarshalAs(UnmanagedType.I4)]  
	        out int pdwNoOfPermittedControls,
	        out IntPtr ppszPermittedControls, // LPWSTR
            [Out][MarshalAs(UnmanagedType.I4)]  
	        out int pbNoStateChange
        );

        void Control(
            [MarshalAs(UnmanagedType.LPWStr)]  
	        string szInvokeUUID,
            [MarshalAs(UnmanagedType.LPWStr)]  
	        string szControl
        );
    };
    
    /// <exclude />
	public static class Constants
	{
		public const string OPC_CATEGORY_DESCRIPTION_CMD10 = "OPC Command Execution Servers Version 1.0";
		public const string OPCCMD_NAMESPACE_V10           = "http://opcfoundation.org/webservices/OPCCMD/10";
	}
    
    /// <exclude />
	public static class EventName
	{
		public const string OPCCMD_EVENT_NAME_INVOKE    = "Invoke";
		public const string OPCCMD_EVENT_NAME_FINISHED  = "Finished";
		public const string OPCCMD_EVENT_NAME_ABORTED   = "Aborted";
		public const string OPCCMD_EVENT_NAME_RESET     = "Reset";
		public const string OPCCMD_EVENT_NAME_HALTED    = "Halted";
		public const string OPCCMD_EVENT_NAME_RESUMED   = "Resumed";
		public const string OPCCMD_EVENT_NAME_CANCELLED = "Cancelled";
	}

    /// <exclude />
	public static class StateName
	{
		public const string OPCCMD_STATE_NAME_IDLE             = "Idle";
		public const string OPCCMD_STATE_NAME_EXECUTING        = "Executing";
		public const string OPCCMD_STATE_NAME_COMPLETE         = "Complete";
		public const string OPCCMD_STATE_NAME_ABNORMAL_FAILURE = "AbnormalFailure";
		public const string OPCCMD_STATE_NAME_HALTED           = "Halted";
	}
    
    /// <exclude />
	public static class ControlCommand
	{
		public const string OPCCMD_CONTROL_SUSPEND = "Suspend";
		public const string OPCCMD_CONTROL_RESUME  = "Resume";
		public const string OPCCMD_CONTROL_CANCEL  = "Cancel";
	}
}
