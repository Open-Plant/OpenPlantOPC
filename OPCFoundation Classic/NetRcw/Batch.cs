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

namespace OpcRcw.Batch
{
    /// <exclude />
	[ComImport]
	[GuidAttribute("A8080DA0-E23E-11D2-AFA7-00C04F539421")]
	[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    public interface CATID_OPCBatchServer10 {}

    /// <exclude />
	[ComImport]
	[GuidAttribute("843DE67B-B0C9-11d4-A0B7-000102A980B1")]
	[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    public interface CATID_OPCBatchServer20 {}
    
    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct OPCBATCHSUMMARY 
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szID;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szDescription;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szOPCItemID;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szMasterRecipeID;
        [MarshalAs(UnmanagedType.R4)]
        public float fBatchSize;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szEU;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szExecutionState;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szExecutionMode;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftActualStartTime;
        public  System.Runtime.InteropServices.ComTypes.FILETIME ftActualEndTime;
    } 
    
    /// <exclude />
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct OPCBATCHSUMMARYFILTER
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szID;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szDescription;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szOPCItemID;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szMasterRecipeID;
        [MarshalAs(UnmanagedType.R4)]
        public float fMinBatchSize;
        [MarshalAs(UnmanagedType.R4)]
        public float fMaxBatchSize;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szEU;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szExecutionState;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szExecutionMode;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftMinStartTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftMaxStartTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftMinEndTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftMaxEndTime;
    }
    
    /// <exclude />
    [ComImport]
    [GuidAttribute("8BB4ED50-B314-11d3-B3EA-00C04F8ECEAA")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
    public interface IOPCBatchServer
    {
	    void GetDelimiter(
            [Out][MarshalAs(UnmanagedType.LPWStr)]
		    string pszDelimiter
	    );
        
	    void CreateEnumerator(
            ref Guid riid,
			[Out][MarshalAs(UnmanagedType.IUnknown, IidParameterIndex=0)] 
            out object ppUnk
	    );
    };

    /// <exclude />
    [ComImport]
    [GuidAttribute("895A78CF-B0C5-11d4-A0B7-000102A980B1")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
    public interface IOPCBatchServer2
    {
	    void CreateFilteredEnumerator(
		    Guid riid,
		    OPCBATCHSUMMARYFILTER pFilter,
            [MarshalAs(UnmanagedType.LPWStr)]
		    string szModel,
			[Out][MarshalAs(UnmanagedType.IUnknown, IidParameterIndex=0)] 
		    out object ppUnk
	    );
    };

    /// <exclude />
    [ComImport]
    [GuidAttribute("a8080da2-e23e-11d2-afa7-00c04f539421")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
    public interface IEnumOPCBatchSummary
    {
	    void Next(
            [MarshalAs(UnmanagedType.I4)]
		    int celt,
            [Out]
		    out IntPtr ppSummaryArray, // OPCBATCHSUMMARY
            [Out][MarshalAs(UnmanagedType.I4)]
		    out int celtFetched
	    );

	    void Skip(
            [MarshalAs(UnmanagedType.I4)]
		    int celt
	    );

	    void Reset();

	    void Clone(
		    out IEnumOPCBatchSummary ppEnumBatchSummary
	    );

	    void Count(
            [Out][MarshalAs(UnmanagedType.I4)]
		    out int pcelt
	    );
    };

    /// <exclude />
    [ComImport]
    [GuidAttribute("a8080da3-e23e-11d2-afa7-00c04f539421")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)] 
    public interface IOPCEnumerationSets
    {
	    void QueryEnumerationSets(
            [Out][MarshalAs(UnmanagedType.I4)]
		    out int pdwCount,
            [Out]
		    out IntPtr ppdwEnumSetId, // DWORD
            [Out]
		    out IntPtr ppszEnumSetName // LPWSTR
	    );

	    void QueryEnumeration(
            [MarshalAs(UnmanagedType.I4)]
		    int dwEnumSetId,
            [MarshalAs(UnmanagedType.I4)]
		    int dwEnumValue,
            [Out][MarshalAs(UnmanagedType.LPWStr)]
		    out string pszEnumName
	    );

	    void QueryEnumerationList(
            [MarshalAs(UnmanagedType.I4)]
		    int dwEnumSetId,
            [Out][MarshalAs(UnmanagedType.I4)]
		    out int  pdwCount,
		    out IntPtr ppdwEnumValue, // DWORD
		    out IntPtr ppszEnumName // LPWSTR
	    );  
    }
    
    /// <exclude />
	public static class Constants
	{
		// category description strings.
		const string OPC_CATEGORY_DESCRIPTION_BATCH10 = "OPC Batch Server Version 1.0";
        const string OPC_CATEGORY_DESCRIPTION_BATCH20 = "OPC Batch Server Version 2.0";
	}
    
    /// <exclude />
	public static class EnumSets
	{
		// Custom Enumeration Set IDs start at 100.
		// 
		// Custom Enumeration Values for any of the defined Enumeration sets may be appended.  
		// These custom enumeration values start at 100.
		//
		// The enumeration values and corresponding localized string representation 
		// are returned via the IOPCEnumerationSets interface methods.

		// OPC Batch Enumeration Sets
		const int OPCB_ENUM_PHYS                 = 0;
		const int OPCB_ENUM_PROC                 = 1;
		const int OPCB_ENUM_STATE                = 2;
		const int OPCB_ENUM_MODE                 = 3;
		const int OPCB_ENUM_PARAM                = 4;
		const int OPCB_ENUM_MR_PROC              = 5;
		const int OPCB_ENUM_RE_USE               = 6;

		// OPC Batch Physical Model Level Enumeration
		const int OPCB_PHYS_ENTERPRISE           = 0;
		const int OPCB_PHYS_SITE                 = 1;
		const int OPCB_PHYS_AREA                 = 2;
		const int OPCB_PHYS_PROCESSCELL          = 3;
		const int OPCB_PHYS_UNIT                 = 4;
		const int OPCB_PHYS_EQUIPMENTMODULE      = 5;
		const int OPCB_PHYS_CONTROLMODULE        = 6;
		const int OPCB_PHYS_EPE                  = 7;

		// OPC Batch Procedural Model Level Enumeration
		const int OPCB_PROC_PROCEDURE            = 0;
		const int OPCB_PROC_UNITPROCEDURE        = 1;
		const int OPCB_PROC_OPERATION            = 2;
		const int OPCB_PROC_PHASE                = 3;
		const int OPCB_PROC_PARAMETER_COLLECTION = 4;
		const int OPCB_PROC_PARAMETER            = 5;
		const int OPCB_PROC_RESULT_COLLECTION    = 6;
		const int OPCB_PROC_RESULT               = 7;
		const int OPCB_PROC_BATCH                = 8;
		const int OPCB_PROC_CAMPAIGN             = 9;

		// OPC Batch IEC 61512-1State Index Enumeration
		const int OPCB_STATE_IDLE                = 0;
		const int OPCB_STATE_RUNNING             = 1;
		const int OPCB_STATE_COMPLETE            = 2;
		const int OPCB_STATE_PAUSING             = 3;
		const int OPCB_STATE_PAUSED              = 4;
		const int OPCB_STATE_HOLDING             = 5;
		const int OPCB_STATE_HELD                = 6;
		const int OPCB_STATE_RESTARTING          = 7;
		const int OPCB_STATE_STOPPING            = 8;
		const int OPCB_STATE_STOPPED             = 9;
		const int OPCB_STATE_ABORTING            = 10;
		const int OPCB_STATE_ABORTED             = 11;
		const int OPCB_STATE_UNKNOWN             = 12;

		// OPC Batch IEC 61512-1Mode Index Enumeration
		const int OPCB_MODE_AUTOMATIC            = 0;
		const int OPCB_MODE_SEMIAUTOMATIC        = 1;
		const int OPCB_MODE_MANUAL               = 2;
		const int OPCB_MODE_UNKNOWN              = 3;

		// OPC Batch Parameter Type Enumeration
		const int OPCB_PARAM_PROCESSINPUT        = 0;
		const int OPCB_PARAM_PROCESSPARAMETER    = 1;
		const int OPCB_PARAM_PROCESSOUTPUT       = 2;

		// OPC Batch Master Recipe Procedure Enumeration
		const int OPCB_MR_PROC_PROCEDURE         = 0;
		const int OPCB_MR_PROC_UNITPROCEDURE     = 1;
		const int OPCB_MR_PROC_OPERATION         = 2;
		const int OPCB_MR_PROC_PHASE             = 3;
		const int OPCB_MR_PARAMETER_COLLECTION   = 4;
		const int OPCB_MR_PARAMETER              = 5;
		const int OPCB_MR_RESULT_COLLECTION      = 6;
		const int OPCB_MR_RESULT                 = 7;
		      
		// OPC Batch Recipe Element Use Enumeration
		const int OPCB_RE_USE_INVALID            = 0;
		const int OPCB_RE_USE_LINKED             = 1;
		const int OPCB_RE_USE_EMBEDDED           = 2;
		const int OPCB_RE_USE_COPIED             = 3;
	}

    /// <exclude />
	public static class Properties
	{
		const int OPCB_PROPERTY_ID                                = 400;
		const int OPCB_PROPERTY_VALUE                             = 401;
		const int OPCB_PROPERTY_RIGHTS                            = 402;
		const int OPCB_PROPERTY_EU                                = 403;
		const int OPCB_PROPERTY_DESC                              = 404;
		const int OPCB_PROPERTY_HIGH_VALUE_LIMIT                  = 405;
		const int OPCB_PROPERTY_LOW_VALUE_LIMIT                   = 406;
		const int OPCB_PROPERTY_TIME_ZONE                         = 407;
		const int OPCB_PROPERTY_CONDITION_STATUS                  = 408;
		const int OPCB_PROPERTY_PHYSICAL_MODEL_LEVEL              = 409;
		const int OPCB_PROPERTY_BATCH_MODEL_LEVEL                 = 410;
		const int OPCB_PROPERTY_RELATED_BATCH_IDS                 = 411;
		const int OPCB_PROPERTY_VERSION                           = 412;
		const int OPCB_PROPERTY_EQUIPMENT_CLASS                   = 413;
		const int OPCB_PROPERTY_LOCATION                          = 414;
		const int OPCB_PROPERTY_MAXIMUM_USER_COUNT                = 415;
		const int OPCB_PROPERTY_CURRENT_USER_COUNT                = 416;
		const int OPCB_PROPERTY_CURRENT_USER_LIST                 = 417;
		const int OPCB_PROPERTY_ALLOCATED_EQUIPMENT_LIST          = 418;
		const int OPCB_PROPERTY_REQUESTER_LIST                    = 419;
		const int OPCB_PROPERTY_REQUESTED_LIST                    = 420;
		const int OPCB_PROPERTY_SHARED_BY_LIST                    = 421;
		const int OPCB_PROPERTY_EQUIPMENT_STATE                   = 422;
		const int OPCB_PROPERTY_EQUIPMENT_MODE                    = 423;
		const int OPCB_PROPERTY_UPSTREAM_EQUIPMENT_LIST           = 424;
		const int OPCB_PROPERTY_DOWNSTREAM_EQUIPMENT_LIST         = 425;
		const int OPCB_PROPERTY_EQUIPMENT_PROCEDURAL_ELEMENT_LIST = 426;
		const int OPCB_PROPERTY_CURRENT_PROCEDURE_LIST            = 427;
		const int OPCB_PROPERTY_TRAIN_LIST                        = 428;
		const int OPCB_PROPERTY_DEVICE_DATA_SOURCE                = 429;
		const int OPCB_PROPERTY_DEVICE_DATA_SERVER                = 430;
		const int OPCB_PROPERTY_CAMPAIGN_ID                       = 431;
		const int OPCB_PROPERTY_LOT_ID_LIST                       = 432;
		const int OPCB_PROPERTY_CONTROL_RECIPE_ID                 = 433;
		const int OPCB_PROPERTY_CONTROL_RECIPE_VERSION            = 434;
		const int OPCB_PROPERTY_MASTER_RECIPE_ID                  = 435;
		const int OPCB_PROPERTY_MASTER_RECIPE_VERSION             = 436;
		const int OPCB_PROPERTY_PRODUCT_ID                        = 437;
		const int OPCB_PROPERTY_GRADE                             = 438;
		const int OPCB_PROPERTY_BATCH_SIZE                        = 439;
		const int OPCB_PROPERTY_PRIORITY                          = 440;
		const int OPCB_PROPERTY_EXECUTION_STATE                   = 441;
		const int OPCB_PROPERTY_IEC61512_1_STATE                  = 442;
		const int OPCB_PROPERTY_EXECUTION_MODE                    = 443;
		const int OPCB_PROPERTY_IEC61512_1_MODE                   = 444;
		const int OPCB_PROPERTY_SCHEDULED_START_TIME              = 445;
		const int OPCB_PROPERTY_ACTUAL_START_TIME                 = 446;
		const int OPCB_PROPERTY_ESTIMATED_END_TIME                = 447;
		const int OPCB_PROPERTY_ACTUAL_END_TIME                   = 448;
		const int OPCB_PROPERTY_PHYSICAL_MODEL_REFERENCE          = 449;
		const int OPCB_PROPERTY_EQUIPMENT_PROCEDURAL_ELEMENT      = 450;
		const int OPCB_PROPERTY_PARAMETER_COUNT                   = 451;
		const int OPCB_PROPERTY_PARAMETER_TYPE                    = 452;
		const int OPCB_PROPERTY_VALID_VALUES                      = 453;
		const int OPCB_PROPERTY_SCALING_RULE                      = 454;
		const int OPCB_PROPERTY_EXPRESSION_RULE                   = 455;
		const int OPCB_PROPERTY_RESULT_COUNT                      = 456;
		const int OPCB_PROPERTY_ENUMERATION_SET_ID                = 457;
		const int OPCB_PROPERTY_MASTER_RECIPE_MODEL_LEVEL		    = 458;
		const int OPCB_PROPERTY_PROCEDURE_LOGIC				    = 459;
		const int OPCB_PROPERTY_PROCEDURE_LOGIC_SCHEMA		    = 460;
		const int OPCB_PROPERTY_EQUIPMENT_CANDIDATE_LIST		    = 461;
		const int OPCB_PROPERTY_EQUIPMENT_CLASS_CANDIDATE_LIST    = 462;
		const int OPCB_PROPERTY_VERSION_DATE					    = 463;
		const int OPCB_PROPERTY_APPROVAL_DATE					    = 464;
		const int OPCB_PROPERTY_EFFECTIVE_DATE					= 465;
		const int OPCB_PROPERTY_EXPIRATION_DATE					= 466;
		const int OPCB_PROPERTY_AUTHOR							= 467;
		const int OPCB_PROPERTY_APPROVED_BY						= 468;
		const int OPCB_PROPERTY_USAGE_CONSTRAINT					= 469;
		const int OPCB_PROPERTY_RECIPE_STATUS					    = 470;
		const int OPCB_PROPERTY_RE_USE							= 471;
		const int OPCB_PROPERTY_DERIVED_RE						= 472;
		const int OPCB_PROPERTY_DERIVED_VERSION					= 473;
		const int OPCB_PROPERTY_SCALABLE							= 474;
		const int OPCB_PROPERTY_EXPECTED_DURATION				    = 475;
		const int OPCB_PROPERTY_ACTUAL_DURATION				  	= 476;
		const int OPCB_PROPERTY_TRAIN_LIST2						= 477;
		const int OPCB_PROPERTY_TRAIN_LIST2_SCHEMA				= 478;
	}
}
