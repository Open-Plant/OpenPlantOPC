//============================================================================
// TITLE: OpcCom.Hda.Interop.cs
//
// CONTENTS:
// 
// An object to handle asynchronous requests.
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
// 2003/12/23 RSA   Initial implementation.

#pragma warning disable 0618

using System;
using System.Threading;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Reflection;
using Opc;
using Opc.Hda;

namespace OpcCom.Hda
{
	/// <summary>
	/// Contains state information for a single asynchronous OpcCom.Da.Interop.
	/// </summary>
	public class Interop
	{		
		/// <summary>
		/// Converts a standard FILETIME to an OpcRcw.Da.FILETIME structure.
		/// </summary>
		internal static OpcRcw.Hda.OPCHDA_FILETIME Convert(FILETIME input)
		{
			OpcRcw.Hda.OPCHDA_FILETIME output = new OpcRcw.Hda.OPCHDA_FILETIME();
			output.dwLowDateTime   = input.dwLowDateTime;
			output.dwHighDateTime  = input.dwHighDateTime;
			return output;
		}

		/// <summary>
		/// Converts an OpcRcw.Da.FILETIME to a standard FILETIME structure.
		/// </summary>
		internal static FILETIME Convert(OpcRcw.Hda.OPCHDA_FILETIME input)
		{
			FILETIME output       = new FILETIME();
			output.dwLowDateTime  = input.dwLowDateTime;
			output.dwHighDateTime = input.dwHighDateTime;
			return output;
		}

		/// <summary>
		/// Converts a decimal value to a OpcRcw.Hda.OPCHDA_TIME structure.
		/// </summary>
		internal static OpcRcw.Hda.OPCHDA_FILETIME GetFILETIME(decimal input)
		{
			OpcRcw.Hda.OPCHDA_FILETIME output = new OpcRcw.Hda.OPCHDA_FILETIME();	

			output.dwHighDateTime = (int)((((ulong)(input*10000000)) & 0xFFFFFFFF00000000)>>32);
			output.dwLowDateTime  = (int)((((ulong)(input*10000000)) & 0x00000000FFFFFFFF));

			return output;
		}

		/// <summary>
		/// Returns an array of FILETIMEs.
		/// </summary>
		internal static OpcRcw.Hda.OPCHDA_FILETIME[] GetFILETIMEs(DateTime[] input)
		{
			OpcRcw.Hda.OPCHDA_FILETIME[] output = null;
			
			if (input != null)
			{
				output = new OpcRcw.Hda.OPCHDA_FILETIME[input.Length];

				for (int ii = 0; ii < input.Length; ii++)
				{
					output[ii] = Convert(OpcCom.Interop.GetFILETIME(input[ii]));
				}
			}

			return output;
		}

		/// <summary>
		/// Converts a Opc.Hda.Time object to a OpcRcw.Hda.OPCHDA_TIME structure.
		/// </summary>
		internal static OpcRcw.Hda.OPCHDA_TIME GetTime(Opc.Hda.Time input)
		{
			OpcRcw.Hda.OPCHDA_TIME output = new OpcRcw.Hda.OPCHDA_TIME();

			if (input != null)
			{
				output.ftTime  = Convert(OpcCom.Interop.GetFILETIME(input.AbsoluteTime));
				output.szTime  = (input.IsRelative)?input.ToString():"";
				output.bString = (input.IsRelative)?1:0;
			}

			// create a null value for a time structure.
			else
			{
				output.ftTime  = Convert(OpcCom.Interop.GetFILETIME(DateTime.MinValue));
				output.szTime  = "";
				output.bString = 1;
			}

			return output;
		}
		
		/// <summary>
		/// Unmarshals and deallocates an array of OPCHDA_ITEM structures.
		/// </summary>
		internal static ItemValueCollection[] GetItemValueCollections(ref IntPtr pInput, int count, bool deallocate)
		{
			ItemValueCollection[] output = null;
			
			if (pInput != IntPtr.Zero && count > 0)
			{
				output = new ItemValueCollection[count];

				IntPtr pos = pInput;

				for (int ii = 0; ii < count; ii++)
				{
					output[ii] = GetItemValueCollection(pos, deallocate);
                    pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Hda.OPCHDA_ITEM)));
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
		/// Unmarshals and deallocates an OPCHDA_ITEM structure.
		/// </summary>
		internal static ItemValueCollection GetItemValueCollection(IntPtr pInput, bool deallocate)
		{
			ItemValueCollection output = null;
			
			if (pInput != IntPtr.Zero)
			{
				object item = Marshal.PtrToStructure(pInput, typeof(OpcRcw.Hda.OPCHDA_ITEM));
				
				output = GetItemValueCollection((OpcRcw.Hda.OPCHDA_ITEM)item, deallocate);

				if (deallocate)
				{
					Marshal.DestroyStructure(pInput, typeof(OpcRcw.Hda.OPCHDA_ITEM));
				}
			}

			return output;
		}

		/// <summary>
		/// Unmarshals and deallocates an OPCHDA_ITEM structure.
		/// </summary>
		internal static ItemValueCollection GetItemValueCollection(OpcRcw.Hda.OPCHDA_ITEM input, bool deallocate)
		{
			ItemValueCollection	output = new ItemValueCollection();

			output.ClientHandle = input.hClient;
			output.AggregateID  = input.haAggregate;

			object[]   values     = OpcCom.Interop.GetVARIANTs(ref input.pvDataValues, input.dwCount, deallocate);
			DateTime[] timestamps = OpcCom.Interop.GetFILETIMEs(ref input.pftTimeStamps, input.dwCount, deallocate);
			int[]      qualities  = OpcCom.Interop.GetInt32s(ref input.pdwQualities, input.dwCount, deallocate);

			for (int ii = 0; ii < input.dwCount; ii++)
			{
				ItemValue value = new ItemValue();

				value.Value            = values[ii];
				value.Timestamp        = timestamps[ii];
				value.Quality          = new Opc.Da.Quality((short)(qualities[ii] & 0x0000FFFF));
				value.HistorianQuality = (Opc.Hda.Quality)((int)(qualities[ii] & 0xFFFF0000));

				output.Add(value);
			}

			return output;
		}
		
		/// <summary>
		/// Unmarshals and deallocates an array of OPCHDA_MODIFIEDITEM structures.
		/// </summary>
		internal static ModifiedValueCollection[] GetModifiedValueCollections(ref IntPtr pInput, int count, bool deallocate)
		{
			ModifiedValueCollection[] output = null;
			
			if (pInput != IntPtr.Zero && count > 0)
			{
				output = new ModifiedValueCollection[count];

				IntPtr pos = pInput;

				for (int ii = 0; ii < count; ii++)
				{
					output[ii] = GetModifiedValueCollection(pos, deallocate);
                    pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Hda.OPCHDA_MODIFIEDITEM)));
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
		/// Unmarshals and deallocates an OPCHDA_MODIFIEDITEM structure.
		/// </summary>
		internal static ModifiedValueCollection GetModifiedValueCollection(IntPtr pInput, bool deallocate)
		{
			ModifiedValueCollection output = null;
			
			if (pInput != IntPtr.Zero)
			{
				object item = Marshal.PtrToStructure(pInput, typeof(OpcRcw.Hda.OPCHDA_MODIFIEDITEM));

				output = GetModifiedValueCollection((OpcRcw.Hda.OPCHDA_MODIFIEDITEM)item, deallocate);

				if (deallocate)
				{
					Marshal.DestroyStructure(pInput, typeof(OpcRcw.Hda.OPCHDA_MODIFIEDITEM));
				}
			}

			return output;
		}

		/// <summary>
		/// Unmarshals and deallocates an OPCHDA_MODIFIEDITEM structure.
		/// </summary>
		internal static ModifiedValueCollection GetModifiedValueCollection(OpcRcw.Hda.OPCHDA_MODIFIEDITEM input, bool deallocate)
		{
			ModifiedValueCollection output = new ModifiedValueCollection();

			output.ClientHandle = input.hClient;

			object[]   values            = OpcCom.Interop.GetVARIANTs(ref input.pvDataValues, input.dwCount, deallocate);
			DateTime[] timestamps        = OpcCom.Interop.GetFILETIMEs(ref input.pftTimeStamps, input.dwCount, deallocate);
			int[]      qualities         = OpcCom.Interop.GetInt32s(ref input.pdwQualities, input.dwCount, deallocate);
			DateTime[] modificationTimes = OpcCom.Interop.GetFILETIMEs(ref input.pftModificationTime, input.dwCount, deallocate);
			int[]      editTypes         = OpcCom.Interop.GetInt32s(ref input.pEditType, input.dwCount, deallocate);
			string[]   users             = OpcCom.Interop.GetUnicodeStrings(ref input.szUser, input.dwCount, deallocate);

			for (int ii = 0; ii < input.dwCount; ii++)
			{
				ModifiedValue value = new ModifiedValue();

				value.Value            = values[ii];
				value.Timestamp        = timestamps[ii];
				value.Quality          = new Opc.Da.Quality((short)(qualities[ii] & 0x0000FFFF));
				value.HistorianQuality = (Opc.Hda.Quality)((int)(qualities[ii] & 0xFFFF0000));
				value.ModificationTime = modificationTimes[ii];
				value.EditType         = (Opc.Hda.EditType)editTypes[ii];
				value.User             = users[ii];

				output.Add(value);
			}

			return output;
		}

		/// <summary>
		/// Unmarshals and deallocates an array of OPCHDA_ATTRIBUTE structures.
		/// </summary>
		internal static AttributeValueCollection[] GetAttributeValueCollections(ref IntPtr pInput, int count, bool deallocate)
		{
			AttributeValueCollection[] output = null;
			
			if (pInput != IntPtr.Zero && count > 0)
			{
				output = new AttributeValueCollection[count];

				IntPtr pos = pInput;

				for (int ii = 0; ii < count; ii++)
				{
					output[ii] = GetAttributeValueCollection(pos, deallocate);
                    pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Hda.OPCHDA_ATTRIBUTE)));
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
		/// Unmarshals and deallocates an OPCHDA_ATTRIBUTE structure.
		/// </summary>
		internal static AttributeValueCollection GetAttributeValueCollection(IntPtr pInput, bool deallocate)
		{
			AttributeValueCollection output = null;
			
			if (pInput != IntPtr.Zero)
			{
				object item = Marshal.PtrToStructure(pInput, typeof(OpcRcw.Hda.OPCHDA_ATTRIBUTE));

				output = GetAttributeValueCollection((OpcRcw.Hda.OPCHDA_ATTRIBUTE)item, deallocate);

				if (deallocate)
				{
					Marshal.DestroyStructure(pInput, typeof(OpcRcw.Hda.OPCHDA_ATTRIBUTE));
				}
			}

			return output;
		}

		/// <summary>
		/// Unmarshals and deallocates an OPCHDA_ATTRIBUTE structure.
		/// </summary>
		internal static AttributeValueCollection GetAttributeValueCollection(OpcRcw.Hda.OPCHDA_ATTRIBUTE input, bool deallocate)
		{
			AttributeValueCollection output = new AttributeValueCollection();

			output.AttributeID = input.dwAttributeID;

			object[]   values     = OpcCom.Interop.GetVARIANTs(ref input.vAttributeValues, input.dwNumValues, deallocate);
			DateTime[] timestamps = OpcCom.Interop.GetFILETIMEs(ref input.ftTimeStamps, input.dwNumValues, deallocate);

			for (int ii = 0; ii < input.dwNumValues; ii++)
			{
				AttributeValue value = new AttributeValue();

				value.Value     = values[ii];
				value.Timestamp = timestamps[ii];

				output.Add(value);
			}

			return output;
		}

		/// <summary>
		/// Unmarshals and deallocates an array of OPCHDA_ANNOTATION structures.
		/// </summary>
		internal static AnnotationValueCollection[] GetAnnotationValueCollections(ref IntPtr pInput, int count, bool deallocate)
		{
			AnnotationValueCollection[] output = null;
			
			if (pInput != IntPtr.Zero && count > 0)
			{
				output = new AnnotationValueCollection[count];

				IntPtr pos = pInput;

				for (int ii = 0; ii < count; ii++)
				{
					output[ii] = GetAnnotationValueCollection(pos, deallocate);
                    pos = (IntPtr)(pos.ToInt64() + Marshal.SizeOf(typeof(OpcRcw.Hda.OPCHDA_ANNOTATION)));
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
		/// Unmarshals and deallocates an OPCHDA_ANNOTATION structure.
		/// </summary>
		internal static AnnotationValueCollection GetAnnotationValueCollection(IntPtr pInput, bool deallocate)
		{
			AnnotationValueCollection output = null;
			
			if (pInput != IntPtr.Zero)
			{
				object item = Marshal.PtrToStructure(pInput, typeof(OpcRcw.Hda.OPCHDA_ANNOTATION));

				output = GetAnnotationValueCollection((OpcRcw.Hda.OPCHDA_ANNOTATION)item, deallocate);

				if (deallocate)
				{
					Marshal.DestroyStructure(pInput, typeof(OpcRcw.Hda.OPCHDA_ANNOTATION));
				}
			}

			return output;
		}

		/// <summary>
		/// Unmarshals and deallocates an OPCHDA_ANNOTATION structure.
		/// </summary>
		internal static AnnotationValueCollection GetAnnotationValueCollection(OpcRcw.Hda.OPCHDA_ANNOTATION input, bool deallocate)
		{
			AnnotationValueCollection output = new AnnotationValueCollection();

			output.ClientHandle = input.hClient;

			DateTime[] timestamps    = OpcCom.Interop.GetFILETIMEs(ref input.ftTimeStamps, input.dwNumValues, deallocate);
			string[]   annotations   = OpcCom.Interop.GetUnicodeStrings(ref input.szAnnotation, input.dwNumValues, deallocate);
			DateTime[] creationTimes = OpcCom.Interop.GetFILETIMEs(ref input.ftAnnotationTime, input.dwNumValues, deallocate);
			string[]   users         = OpcCom.Interop.GetUnicodeStrings(ref input.szUser, input.dwNumValues, deallocate);

			for (int ii = 0; ii < input.dwNumValues; ii++)
			{
				AnnotationValue value = new AnnotationValue();

				value.Timestamp    = timestamps[ii];
				value.Annotation   = annotations[ii];
				value.CreationTime = creationTimes[ii];
				value.User         = users[ii];

				output.Add(value);
			}

			return output;
		}
	}
}
