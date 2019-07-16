//============================================================================
// TITLE: BinaryWriter.cs
//
// CONTENTS:
// 
// A class that writes a complex data item to a binary buffer.
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

namespace Opc.Cpx
{	

	/// <summary>
	/// A class that writes a complex data item to a binary buffer.
	/// </summary>
	public class BinaryWriter : BinaryStream
	{
		/// <summary>
		/// Initializes the binary writer with defaults.
		/// </summary>
		public BinaryWriter() {}

		/// <summary>
		/// Writes a complex value to a buffer.
		/// </summary>
		/// <param name="namedValue">The structured value to write to the buffer.</param>
		/// <param name="dictionary">The type dictionary that contains a complex type identified with the type name.</param>
		/// <param name="typeName">The name of the type that describes the data.</param>
		/// <returns>A buffer containing the binary form of the complex type.</returns>
		public  byte[] Write(ComplexValue namedValue, TypeDictionary dictionary, string typeName)
		{
			if (namedValue == null) throw new ArgumentNullException("namedValue");
			if (dictionary == null) throw new ArgumentNullException("dictionary");
			if (typeName == null)   throw new ArgumentNullException("typeName");

			Context context = InitializeContext(null, dictionary, typeName);		

			// determine the size of buffer required.
			int bytesRequired = WriteType(context, namedValue);
			
			if (bytesRequired == 0)
			{
				throw new InvalidDataToWriteException("Could not write value into buffer.");
			}

			// allocate buffer.
			context.Buffer = new byte[bytesRequired];

			// write data into buffer.
			int bytesWritten = WriteType(context, namedValue);
			
			if (bytesWritten != bytesRequired)
			{
				throw new InvalidDataToWriteException("Could not write value into buffer.");
			}

			return context.Buffer;
		}

		/// <summary>
		/// Writes an instance of a type to the buffer.
		/// </summary>
		private int WriteType(Context context, ComplexValue namedValue)
		{
			TypeDescription type       = context.Type;
			byte[]          buffer     = context.Buffer;
			int             startIndex = context.Index;

			ComplexValue[] fieldValues = null;

			if (namedValue.Value == null || namedValue.Value.GetType() != typeof(ComplexValue[]))
			{
				throw new InvalidDataToWriteException("Type instance does not contain field values.");
			}

			fieldValues = (ComplexValue[])namedValue.Value;

			if (fieldValues.Length != type.Field.Length)
			{
				throw new InvalidDataToWriteException("Type instance does not contain the correct number of fields.");
			}

			byte bitOffset = 0;

			for (int ii = 0; ii < type.Field.Length; ii++)
			{
				FieldType field = type.Field[ii];
				ComplexValue fieldValue = fieldValues[ii];

				if (bitOffset != 0)
				{
					if (field.GetType() != typeof(BitString))
					{
						context.Index++;
						bitOffset = 0;
					}
				}

				int bytesWritten = 0;

				if (IsArrayField(field))
				{
					bytesWritten = WriteArrayField(context, field, ii, fieldValues, fieldValue.Value);
				}				
				else if (field.GetType() == typeof(TypeReference))
				{
					bytesWritten = WriteField(context, (TypeReference)field, fieldValue);
				}
				else
				{
					bytesWritten = WriteField(context, field, ii, fieldValues, fieldValue.Value, ref bitOffset);
				}

				if (bytesWritten == 0 && bitOffset == 0)
				{
					throw new InvalidDataToWriteException("Could not write field '" + field.Name + "' in type '" + type.TypeID +"'.");
				}
				
				context.Index += bytesWritten;
			}

			if (bitOffset != 0)
			{
				context.Index++;
			}
			
			return (context.Index - startIndex);
		}

		/// <summary>
		/// Writes the value contained in a field to the buffer.
		/// </summary>
		private int WriteField(
			Context        context, 
			FieldType      field, 
			int            fieldIndex,
			ComplexValue[] fieldValues, 
			object         fieldValue,
			ref byte       bitOffset
		)
		{
			System.Type type = field.GetType(); 

			if (type == typeof(Integer) || type.IsSubclassOf(typeof(Integer)))
			{
				return WriteField(context, (Integer)field, fieldValue);
			}		
			else if (type == typeof(FloatingPoint) || type.IsSubclassOf(typeof(FloatingPoint)))
			{
				return WriteField(context, (FloatingPoint)field, fieldValue);
			}		
			else if (type == typeof(CharString) || type.IsSubclassOf(typeof(CharString)))
			{
				return WriteField(context, (CharString)field, fieldIndex, fieldValues, fieldValue);
			}	
			else if (type == typeof(BitString) || type.IsSubclassOf(typeof(BitString)))
			{
				return WriteField(context, (BitString)field, fieldValue, ref bitOffset);
			}		
			else if (type == typeof(TypeReference) || type.IsSubclassOf(typeof(TypeReference)))
			{
				return WriteField(context, (TypeReference)field, fieldValue);
			}
			else
			{
				throw new NotImplementedException("Fields of type '" + type.ToString() + "' are not implemented yet.");
			}
		}

		/// <summary>
		/// Writes a complex type from to the buffer.
		/// </summary>
		private int WriteField(Context context, TypeReference field, object fieldValue)
		{
			foreach (TypeDescription type in context.Dictionary.TypeDescription)
			{
				if (type.TypeID == field.TypeID)
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
				throw new InvalidSchemaException("Reference type '" + field.TypeID + "' not found.");
			}

			if (fieldValue.GetType() != typeof(ComplexValue))
			{
				throw new InvalidDataToWriteException("Instance of type is not the correct type.");
			}		

			return WriteType(context, (ComplexValue)fieldValue);
		}

		/// <summary>
		/// Writes a integer value from to the buffer.
		/// </summary>
		private int WriteField(Context context, Integer field, object fieldValue)
		{
			byte[] buffer = context.Buffer;

			// initialize serialization paramters.
			int  length = (field.LengthSpecified)?(int)field.Length:4;
			bool signed = field.Signed;

			// apply defaults for built in types.
			if      (field.GetType() == typeof(Opc.Cpx.Int8))   { length = 1; signed = true;  }
			else if (field.GetType() == typeof(Opc.Cpx.Int16))  { length = 2; signed = true;  }
			else if (field.GetType() == typeof(Opc.Cpx.Int32))  { length = 4; signed = true;  }
			else if (field.GetType() == typeof(Opc.Cpx.Int64))  { length = 8; signed = true;  }
			else if (field.GetType() == typeof(Opc.Cpx.UInt8))  { length = 1; signed = false; }
			else if (field.GetType() == typeof(Opc.Cpx.UInt16)) { length = 2; signed = false; }
			else if (field.GetType() == typeof(Opc.Cpx.UInt32)) { length = 4; signed = false; }
			else if (field.GetType() == typeof(Opc.Cpx.UInt64)) { length = 8; signed = false; }
			
			// only write to the buffer if it has been allocated.
			if (buffer != null)
			{
				// check if there is enough data left.
				if (buffer.Length - context.Index < length)
				{
					throw new InvalidDataToWriteException("Unexpected end of buffer.");
				}

				// copy and swap bytes if required.
				byte[] bytes = null;
			
				if (signed)
				{
					switch (length)
					{
						case 1: 
						{ 
							bytes = new byte[1];

							sbyte value = System.Convert.ToSByte(fieldValue);

							if (value < 0)
							{
								bytes[0] = (byte)(Byte.MaxValue + value + 1);
							}
							else
							{
								bytes[0] = (byte)value;
							}

							break; 
						}

						case 2:  { bytes = BitConverter.GetBytes(System.Convert.ToInt16(fieldValue)); break; }
						case 4:  { bytes = BitConverter.GetBytes(System.Convert.ToInt32(fieldValue)); break; }
						case 8:  { bytes = BitConverter.GetBytes(System.Convert.ToInt64(fieldValue)); break; }
						default: { bytes = (byte[])fieldValue;                                        break; }
					}
				}
				else
				{
					switch (length)
					{ 
						case 1:  { bytes = new byte[] { System.Convert.ToByte(fieldValue) };           break; }
						case 2:  { bytes = BitConverter.GetBytes(System.Convert.ToUInt16(fieldValue)); break; }
						case 4:  { bytes = BitConverter.GetBytes(System.Convert.ToUInt32(fieldValue)); break; }
						case 8:  { bytes = BitConverter.GetBytes(System.Convert.ToUInt64(fieldValue)); break; }
						default: { bytes = (byte[])fieldValue;                                         break; }
					}
				}

				// copy and swap bytes.
				if (context.BigEndian)
				{
					SwapBytes(bytes, 0, length);
				}

				// write bytes to buffer.
				for (int ii = 0; ii < bytes.Length; ii++)
				{
					buffer[context.Index+ii] = bytes[ii];
				}
			}

			return length;
		}

		/// <summary>
		/// Writes a integer value from to the buffer.
		/// </summary>
		private int WriteField(Context context, FloatingPoint field, object fieldValue)
		{
			byte[] buffer = context.Buffer;

			// initialize serialization paramters.
			int    length = (field.LengthSpecified)?(int)field.Length:4;
			string format = (field.FloatFormat != null)?field.FloatFormat:context.FloatFormat;

			// apply defaults for built in types.
			if      (field.GetType() == typeof(Opc.Cpx.Single)) { length = 4; format = Context.FLOAT_FORMAT_IEEE754; }
			else if (field.GetType() == typeof(Opc.Cpx.Double)) { length = 8; format = Context.FLOAT_FORMAT_IEEE754; }

			// only write to the buffer if it has been allocated.
			if (buffer != null)
			{
				// check if there is enough data left.
				if (buffer.Length - context.Index < length)
				{
					throw new InvalidDataToWriteException("Unexpected end of buffer.");
				}

 				// copy bytes if required.
				byte[] bytes = null;
			
				if (format == Context.FLOAT_FORMAT_IEEE754)
				{
					switch (length)
					{
						case 4:  { bytes = BitConverter.GetBytes(System.Convert.ToSingle(fieldValue)); break; }
						case 8:  { bytes = BitConverter.GetBytes(System.Convert.ToDouble(fieldValue)); break; }
						default: { bytes = (byte[])fieldValue;                                         break; }
					}
				}
				else
				{
					bytes = (byte[])fieldValue;
				}

				// write bytes to buffer.
				for (int ii = 0; ii < bytes.Length; ii++)
				{
					buffer[context.Index+ii] = bytes[ii];
				}
			}

			return length;
		}

		/// <summary>
		/// Writes a char string value to the buffer.
		/// </summary>
		private int WriteField(
			Context        context, 
			CharString     field,
			int            fieldIndex,
			ComplexValue[] fieldValues, 
			object         fieldValue
		)
		{
			byte[] buffer = context.Buffer;

			// initialize serialization parameters.
			int charWidth = (field.CharWidthSpecified)?(int)field.CharWidth:(int)context.CharWidth;
			int charCount = (field.LengthSpecified)?(int)field.Length:-1;

			// apply defaults for built in types.
			if      (field.GetType() == typeof(Opc.Cpx.Ascii))   { charWidth = 1; }
			else if (field.GetType() == typeof(Opc.Cpx.Unicode)) { charWidth = 2; }

			byte[] bytes = null;

			if (charCount == -1)
			{
				// extra wide characters stored as byte arrays
				if (charWidth > 2)
				{
					if (fieldValue.GetType() != typeof(byte[]))
					{
						throw new InvalidDataToWriteException("Field value is not a byte array.");
					}

					bytes     = (byte[])fieldValue;
					charCount = bytes.Length/charWidth;
				}
				
				// convert string to byte array.
				else
				{
					if (fieldValue.GetType() != typeof(string))
					{
						throw new InvalidDataToWriteException("Field value is not a string.");
					}

					string stringValue = (string)fieldValue;

					charCount = stringValue.Length+1;

					// calculate length of ascii string by forcing pure unicode characters to two ascii chars.
					if (charWidth == 1)
					{
						charCount = 1;

						foreach (char unicodeChar in stringValue)
						{
							charCount++;

							byte[] charBytes = BitConverter.GetBytes(unicodeChar);

							if (charBytes[1] != 0)
							{
								charCount++;
							}
						}
					}
				}
			}

			// update the char count reference.
			if (field.CharCountRef != null)
			{
				WriteReference(context, field, fieldIndex, fieldValues, field.CharCountRef, charCount);
			}

			if (buffer != null)
			{
				// copy string to buffer.
				if (bytes == null)
				{
					string stringValue = (string)fieldValue;

					bytes = new byte[charWidth*charCount];

					int index = 0;

					for (int ii = 0; ii < stringValue.Length; ii++)
					{
						if (index >= bytes.Length)
						{
							break;
						}

						byte[] charBytes = BitConverter.GetBytes(stringValue[ii]);

						bytes[index++] = charBytes[0];

						if (charWidth == 2 || charBytes[1] != 0)
						{
							bytes[index++] = charBytes[1];
						}
					}
				}

				// check if there is enough data left.
				if (buffer.Length - context.Index < bytes.Length)
				{
					throw new InvalidDataToWriteException("Unexpected end of buffer.");
				}

				// write bytes to buffer.
				for (int ii = 0; ii < bytes.Length; ii++)
				{
					buffer[context.Index+ii] = bytes[ii];
				}

				// swap bytes.
				if (context.BigEndian && charWidth > 1)
				{
					for (int ii = 0; ii < bytes.Length; ii += charWidth)
					{
						SwapBytes(buffer, context.Index+ii, charWidth);
					}
				}
			}

			return charCount*charWidth;
		}

		/// <summary>
		/// Writes a bit string value to the buffer.
		/// </summary>
		private int WriteField(Context context, BitString field, object fieldValue, ref byte bitOffset)
		{
			byte[] buffer = context.Buffer;

			// initialize serialization paramters.
			int bits   = (field.LengthSpecified)?(int)field.Length:8;
			int length = (bits%8 == 0)?bits/8:bits/8+1; 

			if (fieldValue.GetType() != typeof(byte[]))
			{
				throw new InvalidDataToWriteException("Wrong data type to write.");
			}

			// allocate space for the value.
			byte[] bytes = (byte[])fieldValue;
			
			if (buffer != null)
			{
				// check if there is enough data left.
				if (buffer.Length - context.Index < length)
				{
					throw new InvalidDataToWriteException("Unexpected end of buffer.");
				}

				int bitsLeft = bits;   
				byte mask = (bitOffset == 0)?(byte)0xFF:(byte)((0x80>>(bitOffset-1))-1);

				// loop until all bits read.
				for (int ii = 0; bitsLeft >= 0 && ii < length; ii++)
				{
					// add the bits from the lower byte.
					buffer[context.Index+ii] += (byte)((mask & ((1<<bitsLeft)-1) & bytes[ii])<<bitOffset);

					// check if no more bits need to be read.
					if (bitsLeft + bitOffset <= 8)
					{
						break;
					}

					// check if possible to read the next byte.
					if (context.Index + ii + 1 >= buffer.Length)
					{
						throw new InvalidDataToWriteException("Unexpected end of buffer.");
					}

					// add the bytes from the higher byte.
					buffer[context.Index+ii+1] += (byte)((~mask & ((1<<bitsLeft)-1) & bytes[ii])>>(8-bitOffset));

					// check if all done.
					if (bitsLeft <= 8)
					{
						break;
					}

					// decrement the bit count.
					bitsLeft -= 8;
				}
			}

			// update the length bit offset.
			length    = (bits + bitOffset)/8;
			bitOffset = (byte)((bits + bitOffset)%8);

			// return the bytes read.
			return length;
		}

		/// <summary>
		/// Reads a field containing an array of values.
		/// </summary>
		private int WriteArrayField(
			Context         context, 
			FieldType       field, 
			int             fieldIndex,
			ComplexValue[]  fieldValues, 
		    object          fieldValue
		)
		{
			int startIndex = context.Index;

			Array array = null;

			if (!fieldValue.GetType().IsArray)
			{
				throw new InvalidDataToWriteException("Array field value is not an array type.");
			}

			array = (Array)fieldValue;

			byte bitOffset = 0;
			
			// read fixed length array.
			if (field.ElementCountSpecified)
			{
				int count = 0;

				foreach (object elementValue in array)
				{
					// ignore any excess elements.
					if (count == field.ElementCount)
					{
						break;
					}

					int bytesWritten = WriteField(context, field, fieldIndex, fieldValues, elementValue, ref bitOffset);

					if (bytesWritten == 0 && bitOffset == 0)
					{
						break;
					}

					context.Index += bytesWritten;
					count++;
				}

				// write a null value for any missing elements.
				while (count < field.ElementCount)
				{
					int bytesWritten = WriteField(context, field, fieldIndex, fieldValues, null, ref bitOffset);

					if (bytesWritten == 0 && bitOffset == 0)
					{
						break;
					}

					context.Index += bytesWritten;
					count++;
				}
			}

			// read variable length array.
			else if (field.ElementCountRef != null)
			{
				int count = 0;

				foreach (object elementValue in array)
				{
					int bytesWritten = WriteField(context, field, fieldIndex, fieldValues, elementValue, ref bitOffset);

					if (bytesWritten == 0 && bitOffset == 0)
					{
						break;
					}

					context.Index += bytesWritten;
					count++;
				}

				// update the value of the referenced field with the correct element count.
				WriteReference(context, field, fieldIndex, fieldValues, field.ElementCountRef, count);
			}

			// read terminated array.
			else if (field.FieldTerminator != null)
			{
				foreach (object elementValue in array)
				{
					int bytesWritten = WriteField(context, field, fieldIndex, fieldValues, elementValue, ref bitOffset);

					if (bytesWritten == 0 && bitOffset == 0)
					{
						break;
					}

					context.Index += bytesWritten;
				}
				
				// get the terminator.
				byte[] terminator = GetTerminator(context, field);

				if (context.Buffer != null)
				{
					// write the terminator.
					for (int ii = 0; ii < terminator.Length; ii++)
					{
						context.Buffer[context.Index+ii] = terminator[ii];
					}
				}

				context.Index += terminator.Length;
			}

			// clear the bit offset and skip to end of byte at the end of the array.
			if (bitOffset != 0)
			{
				context.Index++;
			}

			// return the total bytes read.
			return (context.Index - startIndex);
		}

		/// <summary>
		/// Finds the integer value referenced by the field name.
		/// </summary>
		private void WriteReference(
			Context         context,
			FieldType       field, 
			int             fieldIndex,
			ComplexValue[]  fieldValues, 
			string          fieldName,
			int             count
		)
		{
			ComplexValue namedValue = null;

			if (fieldName.Length == 0)
			{
				if (fieldIndex > 0 && fieldIndex-1 < fieldValues.Length)
				{
					namedValue = (ComplexValue)fieldValues[fieldIndex-1];
				}
			}
			else
			{
				for (int ii = 0; ii < fieldIndex; ii++)
				{
					namedValue = (ComplexValue)fieldValues[ii];

					if (namedValue.Name == fieldName)
					{
						break;
					}

					namedValue = null;
				}
			}

			if (namedValue == null)
			{
				throw new InvalidSchemaException("Referenced field not found (" + fieldName + ").");
			}

			if (context.Buffer == null)
			{
				namedValue.Value = count;
			}

			if (!count.Equals(namedValue.Value))
			{	
				throw new InvalidDataToWriteException("Reference field value and the actual array length are not equal.");
			}
		}
	}
}
