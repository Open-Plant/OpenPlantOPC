//============================================================================
// TITLE: BinaryStream.cs
//
// CONTENTS:
// 
// A base class that reading/writing complex data item from/to a binary buffer.
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
// 2003/09/2 RSA   First release.

using System;
using System.Collections;
using System.Runtime.Serialization;

namespace Opc.Cpx
{
	/// <summary>
	/// Stores a value with an associated name and/or type.
	/// </summary>
	public class ComplexValue
	{
		/// <summary>
		/// The name of the value.
		/// </summary>
		public string Name  = null;

		/// <summary>
		/// The complex or simple data type of the value.
		/// </summary>
		public string Type  = null;

		/// <summary>
		/// The actual value.
		/// </summary>
		public object Value = null;
	}

	/// <summary>
	/// Stores the current serialization context.
	/// </summary>
	internal struct Context
	{
		public byte[]          Buffer;
		public int             Index;
		public TypeDictionary  Dictionary;
		public TypeDescription Type;
		public bool            BigEndian;
		public int             CharWidth;
		public string          StringEncoding;
		public string          FloatFormat;

		public Context(byte[] buffer)
		{
			Buffer         = buffer;
			Index          = 0;
			Dictionary     = null;
			Type           = null;
			BigEndian      = false;
			CharWidth      = 2;
			StringEncoding = STRING_ENCODING_UCS2;
			FloatFormat    = FLOAT_FORMAT_IEEE754;
		}

		public const string STRING_ENCODING_ACSII = "ASCII";
		public const string STRING_ENCODING_UCS2  = "UCS-2";
		public const string FLOAT_FORMAT_IEEE754  = "IEEE-754";
	}

	/// <summary>
	/// A class that reads a complex data item from a binary buffer.
	/// </summary>
	public class BinaryStream
	{
		/// <summary>
		/// Initializes the binary stream with defaults.
		/// </summary>
		protected BinaryStream() {}
	
		/// <summary>
		/// Determines if a field contains an array of values.
		/// </summary>
		internal bool IsArrayField(FieldType field)
		{
			if (field.ElementCountSpecified)
			{
				if (field.ElementCountRef != null || field.FieldTerminator != null)
				{
					throw new InvalidSchemaException("Multiple array size attributes specified for field '" + field.Name + " '.");
				}

				return true;
			}

			if (field.ElementCountRef != null)
			{
				if (field.FieldTerminator != null)
				{
					throw new InvalidSchemaException("Multiple array size attributes specified for field '" + field.Name + " '.");
				}

				return true;
			}

			if (field.FieldTerminator != null)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns the termininator for the field.
		/// </summary>
		internal byte[] GetTerminator(Context context, FieldType field)
		{
			if (field.FieldTerminator == null) throw new InvalidSchemaException(field.Name + " is not a terminated group.");

			string terminator = System.Convert.ToString(field.FieldTerminator).ToUpper();

			byte[] bytes = new byte[terminator.Length/2];

			for (int ii = 0; ii < bytes.Length; ii++)
			{
				bytes[ii] = System.Convert.ToByte(terminator.Substring(ii*2, 2), 16);
			}

			return bytes;
		}

		/// <summary>
		/// Looks up the type name in the dictionary and initializes the context.
		/// </summary>
		internal Context InitializeContext(byte[] buffer, TypeDictionary dictionary, string typeName)
		{
			Context context = new Context(buffer);
			
			context.Dictionary     = dictionary;
			context.Type           = null;
			context.BigEndian      = dictionary.DefaultBigEndian;
			context.CharWidth      = dictionary.DefaultCharWidth;
			context.StringEncoding = dictionary.DefaultStringEncoding;
			context.FloatFormat    = dictionary.DefaultFloatFormat;

			foreach (TypeDescription type in dictionary.TypeDescription)
			{
				if (type.TypeID == typeName)
				{
					context.Type = type;

					if (type.DefaultBigEndianSpecified)     context.BigEndian      = type.DefaultBigEndian;
					if (type.DefaultCharWidthSpecified)     context.CharWidth      = type.DefaultCharWidth;
					if (type.DefaultStringEncoding != null) context.StringEncoding = type.DefaultStringEncoding;
					if (type.DefaultFloatFormat != null)    context.FloatFormat    = type.DefaultFloatFormat;

					break;
				}
			}

			if (context.Type == null)
			{
				throw new InvalidSchemaException("Type '" + typeName + "' not found in dictionary.");
			}

			return context;
		}	

		/// <summary>
		/// Swaps the order of bytes in the buffer.
		/// </summary>
		internal void SwapBytes(byte[] bytes, int index, int length)
		{
			for (int ii = 0; ii < length/2; ii++)
			{
				byte temp                = bytes[index+length-1-ii];
				bytes[index+length-1-ii] = bytes[index+ii];
				bytes[index+ii]          = temp;
			}
		}
	}
	
	/// <summary>
	/// Raised if the data in buffer is not consistent with the schema.
	/// </summary>
	[Serializable]
	public class InvalidDataInBufferException : ApplicationException
	{
		private const string Default = "The data in the buffer cannot be read because it is not consistent with the schema.";
		/// <remarks/>
		public InvalidDataInBufferException() : base(Default) {} 
		/// <remarks/>
		public InvalidDataInBufferException(string message) : base(Default + "\r\n" + message) {}
		/// <remarks/>
		public InvalidDataInBufferException(Exception e) : base(Default, e) {}		
		/// <remarks/>
		public InvalidDataInBufferException(string message, Exception innerException): base (Default + "\r\n" + message, innerException) {}
		/// <remarks/>
		protected InvalidDataInBufferException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}

	/// <summary>
	/// Raised if the schema contains errors or inconsistencies.
	/// </summary>
	[Serializable]
	public class InvalidSchemaException : ApplicationException
	{
		private const string Default = "The schema cannot be used because it contains errors or inconsitencies.";
		/// <remarks/>
		public InvalidSchemaException() : base(Default) {} 
		/// <remarks/>
		public InvalidSchemaException(string message) : base(Default + "\r\n" + message) {}
		/// <remarks/>
		public InvalidSchemaException(Exception e) : base(Default, e) {}
		/// <remarks/>
		public InvalidSchemaException(string message, Exception innerException): base (Default + "\r\n" + message, innerException) {}
		/// <remarks/>
		protected InvalidSchemaException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}

	/// <summary>
	/// Raised if the data in buffer is not consistent with the schema.
	/// </summary>
	[Serializable]
	public class InvalidDataToWriteException : ApplicationException
	{
		private const string Default = "The object cannot be written because it is not consistent with the schema.";
		/// <remarks/>
		public InvalidDataToWriteException() : base(Default) {} 
		/// <remarks/>
		public InvalidDataToWriteException(string message) : base(Default + "\r\n" + message) {}
		/// <remarks/>
		public InvalidDataToWriteException(Exception e) : base(Default, e) {}
		/// <remarks/>
		public InvalidDataToWriteException(string message, Exception innerException): base (Default + "\r\n" + message, innerException) {}
		/// <remarks/>
		protected InvalidDataToWriteException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}
}
