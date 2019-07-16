//============================================================================
// TITLE: OpcCom.Da.Interop.cs
//
// CONTENTS:
// 
// An object to handle asynchronous requests.
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
// 2003/04/03 RSA   Initial implementation.
// 2005/11/24 RSA   Fixed problem with build number in GetServerStatus.

#pragma warning disable 0618

using System;
using System.Threading;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Reflection;
using Opc;
using Opc.Da;

namespace OpcCom.Da
{
	/// <summary>
	/// Contains state information for a single asynchronous OpcCom.Da.Interop.
	/// </summary>
	public class Interop
	{		
		/// <summary>
		/// Converts a standard FILETIME to an OpcRcw.Da.FILETIME structure.
		/// </summary>
		internal static OpcRcw.Da.FILETIME Convert(FILETIME input)
		{
			OpcRcw.Da.FILETIME output = new OpcRcw.Da.FILETIME();
			output.dwLowDateTime   = input.dwLowDateTime;
			output.dwHighDateTime  = input.dwHighDateTime;
			return output;
		}

		/// <summary>
		/// Converts an OpcRcw.Da.FILETIME to a standard FILETIME structure.
		/// </summary>
		internal static FILETIME Convert(OpcRcw.Da.FILETIME input)
		{
			FILETIME output       = new FILETIME();
			output.dwLowDateTime  = input.dwLowDateTime;
			output.dwHighDateTime = input.dwHighDateTime;
			return output;
		}

		/// <summary>
		/// Allocates and marshals a OPCSERVERSTATUS structure.
		/// </summary>
		internal static OpcRcw.Da.OPCSERVERSTATUS GetServerStatus(ServerStatus input, int groupCount)
		{
			OpcRcw.Da.OPCSERVERSTATUS output = new OpcRcw.Da.OPCSERVERSTATUS();
			
			if (input != null)
			{
				output.szVendorInfo     = input.VendorInfo;
				output.wMajorVersion    = 0;
				output.wMinorVersion    = 0;
				output.wBuildNumber     = 0;
				output.dwServerState    = (OpcRcw.Da.OPCSERVERSTATE)input.ServerState;
				output.ftStartTime      = Convert(OpcCom.Interop.GetFILETIME(input.StartTime));
				output.ftCurrentTime    = Convert(OpcCom.Interop.GetFILETIME(input.CurrentTime));
				output.ftLastUpdateTime = Convert(OpcCom.Interop.GetFILETIME(input.LastUpdateTime));
				output.dwBandWidth      = -1;
				output.dwGroupCount     = groupCount;
				output.wReserved        = 0;

				if (input.ProductVersion != null)
				{
					string[] versions = input.ProductVersion.Split(new char[] {'.'});

					if (versions.Length > 0)
					{
						try   { output.wMajorVersion = System.Convert.ToInt16(versions[0]); }
						catch { output.wMajorVersion = 0; }
					}

					if (versions.Length > 1)
					{
						try   { output.wMinorVersion = System.Convert.ToInt16(versions[1]); }
						catch { output.wMinorVersion = 0; }
					}

					output.wBuildNumber = 0;

					for (int ii = 2; ii < versions.Length; ii++)
					{
						try
						{
							output.wBuildNumber = (short)(output.wBuildNumber * 100 + System.Convert.ToInt16(versions[ii])); 
						}
						catch
						{ 
							output.wBuildNumber = 0;
							break;
						}
					}
				}
			}

			return output;
		}

		/// <summary>
		/// Unmarshals and deallocates a OPCSERVERSTATUS structure.
		/// </summary>
		internal static ServerStatus GetServerStatus(ref IntPtr pInput, bool deallocate)
		{
			ServerStatus output = null;
			
			if (pInput != IntPtr.Zero)
			{
				OpcRcw.Da.OPCSERVERSTATUS status = (OpcRcw.Da.OPCSERVERSTATUS)Marshal.PtrToStructure(pInput, typeof(OpcRcw.Da.OPCSERVERSTATUS));
		
				output = new ServerStatus();

				output.VendorInfo     = status.szVendorInfo;
				output.ProductVersion = String.Format("{0}.{1}.{2}", status.wMajorVersion, status.wMinorVersion, status.wBuildNumber);
				output.ServerState    = (serverState)status.dwServerState;
				output.StatusInfo     = null;
				output.StartTime      = OpcCom.Interop.GetFILETIME(Convert(status.ftStartTime));
				output.CurrentTime    = OpcCom.Interop.GetFILETIME(Convert(status.ftCurrentTime));
				output.LastUpdateTime = OpcCom.Interop.GetFILETIME(Convert(status.ftLastUpdateTime));

				if (deallocate)
				{
					Marshal.DestroyStructure(pInput, typeof(OpcRcw.Da.OPCSERVERSTATUS));
					Marshal.FreeCoTaskMem(pInput);
					pInput = IntPtr.Zero;
				}
			}

			return output;
		}

		/// <summary>
		/// Converts a browseFilter values to the COM equivalent.
		/// </summary>
		internal static OpcRcw.Da.OPCBROWSEFILTER GetBrowseFilter(browseFilter input)
		{			
			switch (input)
			{
				case browseFilter.all:    return OpcRcw.Da.OPCBROWSEFILTER.OPC_BROWSE_FILTER_ALL;
				case browseFilter.branch: return OpcRcw.Da.OPCBROWSEFILTER.OPC_BROWSE_FILTER_BRANCHES;
				case browseFilter.item:   return OpcRcw.Da.OPCBROWSEFILTER.OPC_BROWSE_FILTER_ITEMS;
			}

			return OpcRcw.Da.OPCBROWSEFILTER.OPC_BROWSE_FILTER_ALL;
		}

		/// <summary>
		/// Converts a browseFilter values from the COM equivalent.
		/// </summary>
		internal static browseFilter GetBrowseFilter(OpcRcw.Da.OPCBROWSEFILTER input)
		{			
			switch (input)
			{
				case OpcRcw.Da.OPCBROWSEFILTER.OPC_BROWSE_FILTER_ALL:      return browseFilter.all;
				case OpcRcw.Da.OPCBROWSEFILTER.OPC_BROWSE_FILTER_BRANCHES: return browseFilter.branch;
				case OpcRcw.Da.OPCBROWSEFILTER.OPC_BROWSE_FILTER_ITEMS:    return browseFilter.item;
			}

			return browseFilter.all;
		}

		/// <summary>
		/// Allocates and marshals an array of HRESULT codes.
		/// </summary>
		internal static IntPtr GetHRESULTs(IResult[] results)
		{
			// extract error codes from results.
			int[] errors = new int[results.Length];

			for (int ii = 0; ii < results.Length; ii++)
			{
				if (results[ii] != null)
				{
					errors[ii] = OpcCom.Interop.GetResultID(results[ii].ResultID);
				}
				else
				{
					errors[ii] = ResultIDs.E_INVALIDHANDLE;
				}
			}

			// marshal error codes.
			IntPtr pErrors = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(int))*results.Length);
			Marshal.Copy(errors, 0, pErrors, results.Length);

			// return results.
			return pErrors;
		}

		/// <summary>
		/// Unmarshals and deallocates an array of OPCBROWSEELEMENT structures.
		/// </summary>
		internal static BrowseElement[] GetBrowseElements(ref IntPtr pInput, int count, bool deallocate)
		{
			BrowseElement[] output = null;
			
			if (pInput != IntPtr.Zero && count > 0)
			{
				output = new BrowseElement[count];

				IntPtr pos = pInput;

				for (int ii = 0; ii < count; ii++)
				{
					output[ii] = GetBrowseElement(pos, deallocate);
                    pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Da.OPCBROWSEELEMENT)));
				}

				if (deallocate)
				{
					Marshal.FreeCoTaskMem(pInput);
					pInput = IntPtr.Zero;
				}
			}

			return output;
		}

		/// <summary>
		/// Allocates and marshals an array of OPCBROWSEELEMENT structures.
		/// </summary>
		internal static IntPtr GetBrowseElements(BrowseElement[] input, bool propertiesRequested)
		{
			IntPtr output = IntPtr.Zero;
			
			if (input != null && input.Length > 0)
			{
				output = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(OpcRcw.Da.OPCBROWSEELEMENT))*input.Length);

				IntPtr pos = output;

				for (int ii = 0; ii < input.Length; ii++)
				{
					OpcRcw.Da.OPCBROWSEELEMENT element = GetBrowseElement(input[ii], propertiesRequested); 
					Marshal.StructureToPtr(element, pos, false);
                    pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Da.OPCBROWSEELEMENT)));
				}
			}

			return output;
		}

		/// <summary>
		/// Unmarshals and deallocates a OPCBROWSEELEMENT structures.
		/// </summary>
		internal static BrowseElement GetBrowseElement(IntPtr pInput, bool deallocate)
		{
			BrowseElement output = null;
			
			if (pInput != IntPtr.Zero)
			{
				OpcRcw.Da.OPCBROWSEELEMENT element = (OpcRcw.Da.OPCBROWSEELEMENT)Marshal.PtrToStructure(pInput, typeof(OpcRcw.Da.OPCBROWSEELEMENT));
		
				output = new BrowseElement();

				output.Name        = element.szName;
				output.ItemPath    = null;
				output.ItemName    = element.szItemID;
				output.IsItem      = ((element.dwFlagValue & OpcRcw.Da.Constants.OPC_BROWSE_ISITEM) != 0);
				output.HasChildren = ((element.dwFlagValue & OpcRcw.Da.Constants.OPC_BROWSE_HASCHILDREN) != 0);
				output.Properties  = GetItemProperties(ref element.ItemProperties, deallocate);

				if (deallocate)
				{
					Marshal.DestroyStructure(pInput, typeof(OpcRcw.Da.OPCBROWSEELEMENT));
				}
			}

			return output;
		}

		/// <summary>
		/// Allocates and marshals an OPCBROWSEELEMENT structure.
		/// </summary>
		internal static OpcRcw.Da.OPCBROWSEELEMENT GetBrowseElement(BrowseElement input, bool propertiesRequested)
		{
			OpcRcw.Da.OPCBROWSEELEMENT output = new OpcRcw.Da.OPCBROWSEELEMENT();
			
			if (input != null)
			{
				output.szName         = input.Name;
				output.szItemID       = input.ItemName;
				output.dwFlagValue    = 0;
				output.ItemProperties = GetItemProperties(input.Properties);

				if (input.IsItem)
				{
					output.dwFlagValue |= OpcRcw.Da.Constants.OPC_BROWSE_ISITEM;
				}

				if (input.HasChildren)
				{
					output.dwFlagValue |= OpcRcw.Da.Constants.OPC_BROWSE_HASCHILDREN;
				}
			}

			return output;
		}

		/// <summary>
		/// Creates an array of property codes.
		/// </summary>
		internal static int[] GetPropertyIDs(PropertyID[] propertyIDs)
		{
			ArrayList output = new ArrayList();

			if (propertyIDs != null)
			{
				foreach (PropertyID propertyID in propertyIDs)
				{
					output.Add(propertyID.Code);
				}
			}

			return (int[])output.ToArray(typeof(int));
		}

		/// <summary>
		/// Creates an array of property codes.
		/// </summary>
		internal static PropertyID[] GetPropertyIDs(int[] propertyIDs)
		{
			ArrayList output = new ArrayList();

			if (propertyIDs != null)
			{
				foreach (int propertyID in propertyIDs)
				{
					output.Add(GetPropertyID(propertyID));
				}
			}

			return (PropertyID[])output.ToArray(typeof(PropertyID));
		}

		/// <summary>
		/// Unmarshals and deallocates an array of OPCITEMPROPERTIES structures.
		/// </summary>
		internal static ItemPropertyCollection[] GetItemPropertyCollections(ref IntPtr pInput, int count, bool deallocate)
		{
			ItemPropertyCollection[] output = null;
			
			if (pInput != IntPtr.Zero && count > 0)
			{
				output = new ItemPropertyCollection[count];

				IntPtr pos = pInput;

				for (int ii = 0; ii < count; ii++)
				{
					OpcRcw.Da.OPCITEMPROPERTIES list = (OpcRcw.Da.OPCITEMPROPERTIES)Marshal.PtrToStructure(pos, typeof(OpcRcw.Da.OPCITEMPROPERTIES));

					output[ii]          = new ItemPropertyCollection();
					output[ii].ItemPath = null;
					output[ii].ItemName = null;
					output[ii].ResultID = OpcCom.Interop.GetResultID(list.hrErrorID);

					ItemProperty[] properties = GetItemProperties(ref list, deallocate);

					if (properties != null)
					{
						output[ii].AddRange(properties);
					}

					if (deallocate)
					{
						Marshal.DestroyStructure(pos, typeof(OpcRcw.Da.OPCITEMPROPERTIES));
					}

                    pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMPROPERTIES)));
				}

				if (deallocate)
				{
					Marshal.FreeCoTaskMem(pInput);
					pInput = IntPtr.Zero;
				}
			}

			return output;
		}

		/// <summary>
		/// Allocates and marshals an array of OPCITEMPROPERTIES structures.
		/// </summary>
		internal static IntPtr GetItemPropertyCollections(ItemPropertyCollection[] input)
		{
			IntPtr output = IntPtr.Zero;
			
			if (input != null && input.Length > 0)
			{
				output = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMPROPERTIES))*input.Length);

				IntPtr pos = output;

				for (int ii = 0; ii < input.Length; ii++)
				{
					OpcRcw.Da.OPCITEMPROPERTIES properties = new OpcRcw.Da.OPCITEMPROPERTIES();

					if (input[ii].Count > 0)
					{
						properties = GetItemProperties((ItemProperty[])input[ii].ToArray(typeof(ItemProperty)));
					}

					properties.hrErrorID = OpcCom.Interop.GetResultID(input[ii].ResultID);

					Marshal.StructureToPtr(properties, pos, false);

                    pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMPROPERTIES)));
				}
			}

			return output;
		}

		/// <summary>
		/// Unmarshals and deallocates a OPCITEMPROPERTIES structures.
		/// </summary>
		internal static ItemProperty[] GetItemProperties(ref OpcRcw.Da.OPCITEMPROPERTIES input, bool deallocate)
		{
			ItemProperty[] output = null;
			
			if (input.dwNumProperties > 0)
			{
				output = new ItemProperty[input.dwNumProperties];

				IntPtr pos = input.pItemProperties;

				for (int ii = 0; ii < output.Length; ii++)
				{
                    try
                    {
                        output[ii] = GetItemProperty(pos, deallocate);
                    }
                    catch (Exception e)
                    {
                        output[ii] = new ItemProperty();
                        output[ii].Description = e.Message;
                        output[ii].ResultID = ResultID.E_FAIL;
                    }

                    pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMPROPERTY)));
				}

				if (deallocate)
				{
					Marshal.FreeCoTaskMem(input.pItemProperties);
					input.pItemProperties = IntPtr.Zero;
				}
			}

			return output;
		}

		/// <summary>
		/// Allocates and marshals an array of OPCITEMPROPERTIES structures.
		/// </summary>
		internal static OpcRcw.Da.OPCITEMPROPERTIES GetItemProperties(ItemProperty[] input)
		{
			OpcRcw.Da.OPCITEMPROPERTIES output = new OpcRcw.Da.OPCITEMPROPERTIES();
			
			if (input != null && input.Length > 0)
			{
				output.hrErrorID       = ResultIDs.S_OK;
				output.dwReserved      = 0;
				output.dwNumProperties = input.Length;
				output.pItemProperties = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMPROPERTY))*input.Length);

				bool error = false;

				IntPtr pos = output.pItemProperties;

				for (int ii = 0; ii < input.Length; ii++)
				{
					OpcRcw.Da.OPCITEMPROPERTY property = GetItemProperty(input[ii]); 
					Marshal.StructureToPtr(property, pos, false);
                    pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMPROPERTY)));

					if (input[ii].ResultID.Failed())
					{
						error = true;
					}
				}

				// set flag indicating one or more properties contained errors.
				if (error)
				{
					output.hrErrorID = ResultIDs.S_FALSE;
				}
			}

			return output;
		}

		/// <summary>
		/// Unmarshals and deallocates a OPCITEMPROPERTY structures.
		/// </summary>
		internal static ItemProperty GetItemProperty(IntPtr pInput, bool deallocate)
		{
			ItemProperty output = null;
						
			if (pInput != IntPtr.Zero)
			{
				OpcRcw.Da.OPCITEMPROPERTY property = (OpcRcw.Da.OPCITEMPROPERTY)Marshal.PtrToStructure(pInput, typeof(OpcRcw.Da.OPCITEMPROPERTY));
		
				output = new ItemProperty();

				output.ID          = GetPropertyID(property.dwPropertyID);
				output.Description = property.szDescription;
				output.DataType    = OpcCom.Interop.GetType((VarEnum)property.vtDataType);
				output.ItemPath    = null;
				output.ItemName    = property.szItemID;
				output.Value       = UnmarshalPropertyValue(output.ID, property.vValue);
				output.ResultID    = OpcCom.Interop.GetResultID(property.hrErrorID);

				// convert COM DA code to unified DA code.
				if (property.hrErrorID == ResultIDs.E_BADRIGHTS) output.ResultID = new ResultID(ResultID.Da.E_WRITEONLY, ResultIDs.E_BADRIGHTS);
				
				if (deallocate)
				{
					Marshal.DestroyStructure(pInput, typeof(OpcRcw.Da.OPCITEMPROPERTY));
				}
			}

			return output;
		}

		/// <summary>
		/// Allocates and marshals an arary of OPCITEMPROPERTY structures.
		/// </summary>
		internal static OpcRcw.Da.OPCITEMPROPERTY GetItemProperty(ItemProperty input)
		{
			OpcRcw.Da.OPCITEMPROPERTY output = new OpcRcw.Da.OPCITEMPROPERTY();

			if (input != null)
			{
				output.dwPropertyID  = input.ID.Code;
				output.szDescription = input.Description;
				output.vtDataType    = (short)OpcCom.Interop.GetType(input.DataType);
				output.vValue        = MarshalPropertyValue(input.ID, input.Value);
				output.wReserved     = 0;
				output.hrErrorID     = OpcCom.Interop.GetResultID(input.ResultID);

				// set the property data type.
				PropertyDescription description = PropertyDescription.Find(input.ID);

				if (description != null)
				{
					output.vtDataType = (short)OpcCom.Interop.GetType(description.Type);
				}

				// convert unified DA code to COM DA code.
				if (input.ResultID == ResultID.Da.E_WRITEONLY) output.hrErrorID = ResultIDs.E_BADRIGHTS;
			}

			return output;
		}

		/// <remarks/>
		public static PropertyID GetPropertyID(int input)
		{
			FieldInfo[] fields = typeof(Opc.Da.Property).GetFields(BindingFlags.Static | BindingFlags.Public);

			foreach (FieldInfo field in fields)
			{
				PropertyID property = (PropertyID)field.GetValue(typeof(PropertyID));

				if (input == property.Code)
				{
					return property;
				}
			}

			return new PropertyID(input);
		}

		/// <summary>
		/// Converts the property value to a type supported by the unified interface.
		/// </summary>
		internal static object UnmarshalPropertyValue(PropertyID propertyID, object input)
		{	
			if (input == null) return null;
					
			try
			{
				if (propertyID == Property.DATATYPE)
				{
					return OpcCom.Interop.GetType((VarEnum)System.Convert.ToUInt16(input));
				}

				if (propertyID == Property.ACCESSRIGHTS)
				{
					switch (System.Convert.ToInt32(input))
					{
						case OpcRcw.Da.Constants.OPC_READABLE:  return accessRights.readable;
						case OpcRcw.Da.Constants.OPC_WRITEABLE: return accessRights.writable;
						
						case OpcRcw.Da.Constants.OPC_READABLE | OpcRcw.Da.Constants.OPC_WRITEABLE: 
						{
							return accessRights.readWritable;
						}
					}

					return null;
				}

				if (propertyID == Property.EUTYPE)
				{
					switch ((OpcRcw.Da.OPCEUTYPE)input)
					{
						case OpcRcw.Da.OPCEUTYPE.OPC_NOENUM:     return euType.noEnum;
						case OpcRcw.Da.OPCEUTYPE.OPC_ANALOG:     return euType.analog;
						case OpcRcw.Da.OPCEUTYPE.OPC_ENUMERATED: return euType.enumerated;
					}

					return null;
				}

				if (propertyID == Property.QUALITY)
				{
					return new Opc.Da.Quality(System.Convert.ToInt16(input));
				}

				// convert UTC time in property to local time for the unified DA interface.
				if (propertyID == Property.TIMESTAMP)
				{
					if (input.GetType() == typeof(DateTime))
					{
						DateTime dateTime = (DateTime)input;

						if (dateTime != DateTime.MinValue)
						{
							return dateTime.ToLocalTime();
						}

						return dateTime;
					}
				}
			}
			catch {}

			return input;
		}
		
		/// <summary>
		/// Converts the property value to a type supported by COM-DA interface.
		/// </summary>
		internal static object MarshalPropertyValue(PropertyID propertyID, object input)
		{	
			if (input == null) return null;
					
			try
			{
				if (propertyID == Property.DATATYPE)
				{
					return (short)OpcCom.Interop.GetType((System.Type)input);
				}

				if (propertyID == Property.ACCESSRIGHTS)
				{
					switch ((accessRights)input)
					{
						case accessRights.readable:     return OpcRcw.Da.Constants.OPC_READABLE;
						case accessRights.writable:     return OpcRcw.Da.Constants.OPC_WRITEABLE;
						case accessRights.readWritable: return OpcRcw.Da.Constants.OPC_READABLE | OpcRcw.Da.Constants.OPC_WRITEABLE;
					}

					return null;
				}

				if (propertyID == Property.EUTYPE)
				{
					switch ((euType)input)
					{
						case euType.noEnum:     return OpcRcw.Da.OPCEUTYPE.OPC_NOENUM;
						case euType.analog:     return OpcRcw.Da.OPCEUTYPE.OPC_ANALOG;
						case euType.enumerated: return OpcRcw.Da.OPCEUTYPE.OPC_ENUMERATED;
					}

					return null;
				}

				if (propertyID == Property.QUALITY)
				{
					return ((Opc.Da.Quality)input).GetCode();
				}

				// convert local time in property to UTC time for the COM DA interface.
				if (propertyID == Property.TIMESTAMP)
				{
					if (input.GetType() == typeof(DateTime))
					{
						DateTime dateTime = (DateTime)input;

						if (dateTime != DateTime.MinValue)
						{
							return dateTime.ToUniversalTime();
						}

						return dateTime;
					}
				}
			}
			catch {}

			return input;
		}

		/// <summary>
		/// Converts an array of item values to an array of OPCITEMVQT objects.
		/// </summary>
		internal static OpcRcw.Da.OPCITEMVQT[] GetOPCITEMVQTs(ItemValue[] input)
		{
			OpcRcw.Da.OPCITEMVQT[] output = null;

			if (input != null)
			{
				output = new OpcRcw.Da.OPCITEMVQT[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = new OpcRcw.Da.OPCITEMVQT();

					DateTime timestamp = (input[ii].TimestampSpecified)?input[ii].Timestamp:DateTime.MinValue;	

					output[ii].vDataValue          = OpcCom.Interop.GetVARIANT(input[ii].Value);
					output[ii].bQualitySpecified   = (input[ii].QualitySpecified)?1:0;
					output[ii].wQuality            = (input[ii].QualitySpecified)?input[ii].Quality.GetCode():(short)0;
					output[ii].bTimeStampSpecified = (input[ii].TimestampSpecified)?1:0;
					output[ii].ftTimeStamp         = OpcCom.Da.Interop.Convert(OpcCom.Interop.GetFILETIME(timestamp));
				}

			}

			return output;
		}

		/// <summary>
		/// Converts an array of item objects to an array of GetOPCITEMDEF objects.
		/// </summary>
		internal static OpcRcw.Da.OPCITEMDEF[] GetOPCITEMDEFs(Item[] input)
		{
			OpcRcw.Da.OPCITEMDEF[] output = null;

			if (input != null)
			{
				output = new OpcRcw.Da.OPCITEMDEF[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = new OpcRcw.Da.OPCITEMDEF();

					output[ii].szItemID            = input[ii].ItemName;
					output[ii].szAccessPath        = (input[ii].ItemPath == null)?String.Empty:input[ii].ItemPath;
					output[ii].bActive             = (input[ii].ActiveSpecified)?((input[ii].Active)?1:0):1;
					output[ii].vtRequestedDataType = (short)OpcCom.Interop.GetType(input[ii].ReqType);
					output[ii].hClient             = 0;
					output[ii].dwBlobSize          = 0;
					output[ii].pBlob               = IntPtr.Zero;
				}
			}

			return output;
		}

		/// <summary>
		/// Unmarshals and deallocates a OPCITEMSTATE structures.
		/// </summary>
		internal static ItemValue[] GetItemValues(ref IntPtr pInput, int count, bool deallocate)
		{
			ItemValue[] output = null;
			
			if (pInput != IntPtr.Zero && count > 0)
			{
				output = new ItemValue[count];

				IntPtr pos = pInput;

				for (int ii = 0; ii < count; ii++)
				{
					OpcRcw.Da.OPCITEMSTATE result = (OpcRcw.Da.OPCITEMSTATE)Marshal.PtrToStructure(pos, typeof(OpcRcw.Da.OPCITEMSTATE));

					output[ii]                    = new ItemValue();
					output[ii].ClientHandle       = result.hClient;
					output[ii].Value              = result.vDataValue;
					output[ii].Quality            = new Opc.Da.Quality(result.wQuality);
					output[ii].QualitySpecified   = true;
					output[ii].Timestamp          = OpcCom.Interop.GetFILETIME(Convert(result.ftTimeStamp));
					output[ii].TimestampSpecified = output[ii].Timestamp != DateTime.MinValue;
					
					if (deallocate)
					{
						Marshal.DestroyStructure(pos, typeof(OpcRcw.Da.OPCITEMSTATE));
					}

                    pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMSTATE)));
				}

				if (deallocate)
				{
					Marshal.FreeCoTaskMem(pInput);
					pInput = IntPtr.Zero;
				}
			}

			return output;
		}

		/// <summary>
		/// Unmarshals and deallocates a OPCITEMRESULT structures.
		/// </summary>
		internal static int[] GetItemResults(ref IntPtr pInput, int count, bool deallocate)
		{
			int[] output = null;
			
			if (pInput != IntPtr.Zero && count > 0)
			{
				output = new int[count];

				IntPtr pos = pInput;

				for (int ii = 0; ii < count; ii++)
				{
					OpcRcw.Da.OPCITEMRESULT result = (OpcRcw.Da.OPCITEMRESULT)Marshal.PtrToStructure(pos, typeof(OpcRcw.Da.OPCITEMRESULT));

					output[ii] = result.hServer;

					if (deallocate)
					{
						Marshal.FreeCoTaskMem(result.pBlob);
						result.pBlob = IntPtr.Zero;

						Marshal.DestroyStructure(pos, typeof(OpcRcw.Da.OPCITEMRESULT));
					}

                    pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMRESULT)));
				}

				if (deallocate)
				{
					Marshal.FreeCoTaskMem(pInput);
					pInput = IntPtr.Zero;
				}
			}

			return output;
		}

		/// <summary>
		/// Allocates and marshals an array of OPCBROWSEELEMENT structures.
		/// </summary>
		internal static IntPtr GetItemStates(ItemValueResult[] input)
		{
			IntPtr output = IntPtr.Zero;
			
			if (input != null && input.Length > 0)
			{
				output = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMSTATE))*input.Length);

				IntPtr pos = output;

				for (int ii = 0; ii < input.Length; ii++)
				{
					OpcRcw.Da.OPCITEMSTATE item = new OpcRcw.Da.OPCITEMSTATE();

					item.hClient     = System.Convert.ToInt32(input[ii].ClientHandle);
					item.vDataValue  = input[ii].Value;
					item.wQuality    = (input[ii].QualitySpecified)?input[ii].Quality.GetCode():(short)0;
					item.ftTimeStamp = Interop.Convert(OpcCom.Interop.GetFILETIME(input[ii].Timestamp));
					item.wReserved   = 0;

					Marshal.StructureToPtr(item, pos, false);
                    pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Da.OPCITEMSTATE)));
				}
			}

			return output;
		}
	}
}
