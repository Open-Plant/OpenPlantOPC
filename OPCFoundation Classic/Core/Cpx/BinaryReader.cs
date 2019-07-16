//============================================================================
// TITLE: BinaryReader.cs
//
// CONTENTS:
// 
// A class that reads a complex data item from a binary buffer.
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
	/// A class that reads a complex data item from a binary buffer.
	/// </summary>
	public class BinaryReader : BinaryStream
	{
		/// <summary>
		/// Initializes the reader with defaults.
		/// </summary>
		public BinaryReader() {}
	
		/// <summary>
		/// Reads a value of the specified type from the buffer.
		/// </summary>
		/// <param name="buffer">The buffer containing binary data to read.</param>
		/// <param name="dictionary">The type dictionary that contains a complex type identified with the type name.</param>
		/// <param name="typeName">The name of the type that describes the data.</param>
		/// <returns>A structured represenation of the data in the buffer.</returns>
		public ComplexValue Read(byte[] buffer, TypeDictionary dictionary, string typeName)
		{
			if (buffer == null)     throw new ArgumentNullException("buffer");
			if (dictionary == null) throw new ArgumentNullException("dictionary");
			if (typeName == null)   throw new ArgumentNullException("typeName");

			Context context = InitializeContext(buffer, dictionary, typeName);		

			ComplexValue complexValue = null;

			int bytesRead = ReadType(context, out complexValue);
			
			if (bytesRead == 0)
			{
				throw new InvalidSchemaException("Type '" + typeName + "' not found in dictionary.");
			}

			return complexValue;
		}

		/// <summary>
		/// Reads an instance of a type from the buffer,
		/// </summary>
		private int ReadType(Context context, out ComplexValue complexValue)
		{
			complexValue = null;

			TypeDescription type       = context.Type;
			byte[]          buffer     = context.Buffer;
			int             startIndex = context.Index;

			byte bitOffset = 0;

			ArrayList fieldValues = new ArrayList();

			for (int ii = 0; ii < type.Field.Length; ii++)
			{
				FieldType field = type.Field[ii];

				ComplexValue fieldValue = new ComplexValue();

				fieldValue.Name  = (field.Name != null && field.Name.Length != 0)?field.Name:"[" + ii.ToString() + "]";
				fieldValue.Type  = null;
				fieldValue.Value = null;

				// check if additional padding is required after the end of a bit field.
				if (bitOffset != 0)
				{
					if (field.GetType() != typeof(BitString))
					{
						context.Index++;
						bitOffset = 0;
					}
				}
				
				int bytesRead = 0;

				if (IsArrayField(field))
				{
					bytesRead = ReadArrayField(context, field, ii, fieldValues, out fieldValue.Value);
				}
				else if (field.GetType() == typeof(TypeReference))
				{
					object typeValue = null;

					bytesRead = ReadField(context, (TypeReference)field, out typeValue);

					// assign a name appropriate for the current context.
					fieldValue.Name  = field.Name;
					fieldValue.Type  = ((ComplexValue)typeValue).Type;
					fieldValue.Value = ((ComplexValue)typeValue).Value;
				}
				else
				{
					bytesRead = ReadField(context, field, ii, fieldValues, out fieldValue.Value, ref bitOffset);
				}

				if (bytesRead == 0 && bitOffset == 0)
				{
					throw new InvalidDataInBufferException("Could not read field '" + field.Name + "' in type '" + type.TypeID +"'.");
				}
				
				context.Index += bytesRead;
			
				// assign a value for field type.
				if (fieldValue.Type == null)
				{
					fieldValue.Type = Opc.Convert.ToString(fieldValue.Value.GetType());
				}

				fieldValues.Add(fieldValue);
			}

			// skip padding bits at the end of a type.
			if (bitOffset != 0)
			{
				context.Index++;
			}

			complexValue = new ComplexValue();
			
			complexValue.Name  = type.TypeID;
			complexValue.Type  = type.TypeID;
			complexValue.Value = (ComplexValue[])fieldValues.ToArray(typeof(ComplexValue));
			
			return (context.Index - startIndex);
		}

		/// <summary>
		/// Reads the value contained in a field from the buffer.
		/// </summary>
		private int ReadField(
			Context    context, 
			FieldType  field, 
			int        fieldIndex,
			ArrayList  fieldValues,
			out object fieldValue,
			ref byte   bitOffset
		)
		{
			fieldValue = null;

			System.Type type = field.GetType(); 

			if (type == typeof(Integer) || type.IsSubclassOf(typeof(Integer)))
			{
				return ReadField(context, (Integer)field, out fieldValue);
			}		
			else if (type == typeof(FloatingPoint) || type.IsSubclassOf(typeof(FloatingPoint)))
			{
				return ReadField(context, (FloatingPoint)field, out fieldValue);
			}	
			else if (type == typeof(CharString) || type.IsSubclassOf(typeof(CharString)))
			{
				return ReadField(context, (CharString)field, fieldIndex, fieldValues, out fieldValue);
			}	
			else if (type == typeof(BitString) || type.IsSubclassOf(typeof(BitString)))
			{
				return ReadField(context, (BitString)field, out fieldValue, ref bitOffset);
			}
			else if (type == typeof(TypeReference))
			{
				return ReadField(context, (TypeReference)field, out fieldValue);
			}
			else
			{
				throw new NotImplementedException("Fields of type '" + type.ToString() + "' are not implemented yet.");
			}
		}

		/// <summary>
		/// Reads a complex type from the buffer.
		/// </summary>
		private int ReadField(Context context, TypeReference field, out object fieldValue)
		{
			fieldValue = null;

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

			ComplexValue complexValue = null;

			int bytesRead = ReadType(context, out complexValue);
			
			if (bytesRead == 0)
			{			
				fieldValue = null;
			}

			fieldValue = complexValue;

			return bytesRead;
		}

		/// <summary>
		/// Reads a integer value from the buffer.
		/// </summary>
		private int ReadField(Context context, Integer field, out object fieldValue)
		{
			fieldValue = null;

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

			// check if there is enough data left.
			if (buffer.Length - context.Index < length)
			{
				throw new InvalidDataInBufferException("Unexpected end of buffer.");
			}

			// copy and swap bytes if required.
			byte[] bytes = new byte[length];
			
			for (int ii = 0; ii < length; ii++)
			{
				bytes[ii] = buffer[context.Index+ii];
			}

			if (context.BigEndian)
			{
				SwapBytes(bytes, 0, length);
			}

			// convert to object.
			if (signed)
			{
				switch (length)
				{

					case 1: 
					{
						if (bytes[0] < 128)
						{
							fieldValue = (sbyte)bytes[0];
						}
						else
						{
							fieldValue = (sbyte)(0 - bytes[0]);
						}

						break; 
					}

					case 2:  { fieldValue = BitConverter.ToInt16(bytes, 0);   break; }
					case 4:  { fieldValue = BitConverter.ToInt32(bytes, 0);   break; }
					case 8:  { fieldValue = BitConverter.ToInt64(bytes, 0);   break; }
					default: { fieldValue = bytes;                            break; }
				}
			}
			else
			{
				switch (length)
				{
					case 1:  { fieldValue = bytes[0];                        break; }
					case 2:  { fieldValue = BitConverter.ToUInt16(bytes, 0); break; }
					case 4:  { fieldValue = BitConverter.ToUInt32(bytes, 0); break; }
					case 8:  { fieldValue = BitConverter.ToUInt64(bytes, 0); break; }
					default: { fieldValue = bytes;                           break; }
				}
			}

			return length;
		}

		/// <summary>
		/// Reads a floating point value from the buffer.
		/// </summary>
		private int ReadField(Context context, FloatingPoint field, out object fieldValue)
		{
			fieldValue = null;

			byte[] buffer = context.Buffer;

			// initialize serialization paramters.
			int    length = (field.LengthSpecified)?(int)field.Length:4;
			string format = (field.FloatFormat != null)?field.FloatFormat:context.FloatFormat;

			// apply defaults for built in types.
			if      (field.GetType() == typeof(Opc.Cpx.Single)) { length = 4; format = Context.FLOAT_FORMAT_IEEE754; }
			else if (field.GetType() == typeof(Opc.Cpx.Double)) { length = 8; format = Context.FLOAT_FORMAT_IEEE754; }

			// check if there is enough data left.
			if (buffer.Length - context.Index < length)
			{
				throw new InvalidDataInBufferException("Unexpected end of buffer.");
			}

			// copy bytes.
			byte[] bytes = new byte[length];
			
			for (int ii = 0; ii < length; ii++)
			{
				bytes[ii] = buffer[context.Index+ii];
			}

			// convert to object.
			if (format == Context.FLOAT_FORMAT_IEEE754)
			{
				switch (length)
				{
					case 4:  { fieldValue = BitConverter.ToSingle(bytes, 0); break; }
					case 8:  { fieldValue = BitConverter.ToDouble(bytes, 0); break; }
					default: { fieldValue = bytes;                           break; }
				}
			}
			else
			{
				fieldValue = bytes;
			}

			return length;
		}

		/// <summary>
		/// Reads a char string value from the buffer.
		/// </summary>
		private int ReadField(
			Context    context, 
			CharString field, 
			int        fieldIndex,
			ArrayList  fieldValues, 
			out object fieldValue
		)
		{
			fieldValue = null;

			byte[] buffer = context.Buffer;

			// initialize serialization parameters.
			int charWidth = (field.CharWidthSpecified)?(int)field.CharWidth:(int)context.CharWidth;
			int charCount = (field.LengthSpecified)?(int)field.Length:-1;

			// apply defaults for built in types.
			if      (field.GetType() == typeof(Opc.Cpx.Ascii))   { charWidth = 1; }
			else if (field.GetType() == typeof(Opc.Cpx.Unicode)) { charWidth = 2; }

			if (field.CharCountRef != null)
			{
				charCount = ReadReference(context, field, fieldIndex, fieldValues, field.CharCountRef);
			}

			// find null terminator
			if (charCount == -1)
			{
				charCount = 0;

				for (int ii = context.Index; ii < context.Buffer.Length-charWidth+1; ii += charWidth)
				{
					charCount++;

					bool isNull = true;

					for (int jj = 0; jj < charWidth; jj++)
					{
						if (context.Buffer[ii+jj] != 0)
						{
							isNull = false;
							break;
						}
					}

					if (isNull)
					{
						break;
					}
				}
			}

			// check if there is enough data left.
			if (buffer.Length - context.Index < charWidth*charCount)
			{
				throw new InvalidDataInBufferException("Unexpected end of buffer.");
			}

			if (charWidth > 2)
			{
				// copy bytes.
				byte[] bytes = new byte[charCount*charWidth];
			
				for (int ii = 0; ii < charCount*charWidth; ii++)
				{
					bytes[ii] = buffer[context.Index+ii];
				}

				// swap bytes.
				if (context.BigEndian)
				{
					for (int ii = 0; ii < bytes.Length; ii += charWidth)
					{
						SwapBytes(bytes, 0, charWidth);
					}
				}

				fieldValue = bytes;
			}
			else
			{
				// copy characters.
				char[] chars = new char[charCount];
					
				for (int ii = 0; ii < charCount; ii++)
				{
					if (charWidth == 1)
					{
						chars[ii] = System.Convert.ToChar(buffer[context.Index+ii]);
					}
					else
					{
						byte[] charBytes = new byte[]
						{
							buffer[context.Index+2*ii],
							buffer[context.Index+2*ii+1]
						};

						if (context.BigEndian)
						{
							SwapBytes(charBytes, 0, 2);
						}

						chars[ii] = BitConverter.ToChar(charBytes, 0);
					}
				}		

				fieldValue = new string(chars).TrimEnd(new char[] {'\0'});
			}

			return charCount*charWidth;
		}

		/// <summary>
		/// Reads a bit string value from the buffer.
		/// </summary>
		private int ReadField(Context context, BitString field, out object fieldValue, ref byte bitOffset)
		{
			fieldValue = null;

			byte[] buffer = context.Buffer;

			// initialize serialization paramters.
			int bits   = (field.LengthSpecified)?(int)field.Length:8;
			int length = (bits%8 == 0)?bits/8:bits/8+1; 

			// check if there is enough data left.
			if (buffer.Length - context.Index < length)
			{
				throw new InvalidDataInBufferException("Unexpected end of buffer.");
			}

			// allocate space for the value.
			byte[] bytes = new byte[length];
			
			int bitsLeft = bits;   
			byte mask = (byte)(~((1<<bitOffset)-1));

			// loop until all bits read.
			for (int ii = 0; bitsLeft >= 0 && ii < length; ii++)
			{
				// add the bits from the lower byte.
				bytes[ii] = (byte)((mask & buffer[context.Index+ii])>>bitOffset);

				// check if no more bits need to be read.
				if (bitsLeft + bitOffset <= 8)
				{
					// mask out un-needed bits.
					bytes[ii] &= (byte)((1<<bitsLeft)-1);
					break;
				}

				// check if possible to read the next byte.
				if (context.Index + ii + 1 >= buffer.Length)
				{
					throw new InvalidDataInBufferException("Unexpected end of buffer.");
				}

				// add the bytes from the higher byte.
				bytes[ii] += (byte)((~mask & buffer[context.Index+ii+1])<<(8-bitOffset));

				// check if all done.
				if (bitsLeft <= 8)
				{
					// mask out un-needed bits.
					bytes[ii] &= (byte)((1<<bitsLeft)-1);
					break;
				}

				// decrement the bit count.
				bitsLeft -= 8;
			}

			fieldValue = bytes;

			// update the length bit offset.
			length    = (bits + bitOffset)/8;
			bitOffset = (byte)((bits + bitOffset)%8);

			// return the bytes read.
			return length;
		}

		/// <summary>
		/// Reads a field containing an array of values.
		/// </summary>
		private int ReadArrayField(
			Context    context, 
			FieldType  field, 
			int        fieldIndex,
			ArrayList  fieldValues, 
			out object fieldValue
		)
		{
			fieldValue = null;

			int startIndex = context.Index;

			ArrayList array = new ArrayList();
			object elementValue = null;

			byte bitOffset = 0; 
			
			// read fixed length array.
			if (field.ElementCountSpecified)
			{
				for (int ii = 0; ii < field.ElementCount; ii++)
				{
					int bytesRead = ReadField(context, field, fieldIndex, fieldValues, out elementValue, ref bitOffset);

					if (bytesRead == 0 && bitOffset == 0)
					{
						break;
					}

					array.Add(elementValue);

					context.Index += bytesRead;
				}
			}

			// read variable length array.
			else if (field.ElementCountRef != null)
			{
				int count = ReadReference(context, field, fieldIndex, fieldValues, field.ElementCountRef);

				for (int ii = 0; ii < count; ii++)
				{
					int bytesRead = ReadField(context, field, fieldIndex, fieldValues, out elementValue, ref bitOffset);

					if (bytesRead == 0 && bitOffset == 0)
					{
						break;
					}

					array.Add(elementValue);

					context.Index += bytesRead;
				}
			}

			// read terminated array.
			else if (field.FieldTerminator != null)
			{
				byte[] terminator = GetTerminator(context, field);

				while (context.Index < context.Buffer.Length)
				{
					bool found = true;

					for (int ii = 0; ii < terminator.Length; ii++)
					{
						if (terminator[ii] != context.Buffer[context.Index+ii])
						{
							found = false;
							break;
						}
					}

					if (found)
					{
						context.Index += terminator.Length;
						break;
					}

					int bytesRead = ReadField(context, field, fieldIndex, fieldValues, out elementValue, ref bitOffset);

					if (bytesRead == 0 && bitOffset == 0)
					{
						break;
					}
					
					array.Add(elementValue);
					
					context.Index += bytesRead;
				}
			}
			
			// skip padding bits at the end of an array.
			if (bitOffset != 0)
			{
				context.Index++;
			}

			// convert array list to a fixed length array of a single type. 
			System.Type type = null;

			foreach (object element in array)
			{
				if (type == null)
				{
					type = element.GetType();
				}
				else
				{
					if (type != element.GetType())
					{
						type = typeof(object);
						break;
					}
				}
			}

			fieldValue = array.ToArray(type);

			// return the total bytes read.
			return (context.Index - startIndex);
		}

		/// <summary>
		/// Finds the integer value referenced by the field name.
		/// </summary>
		private int ReadReference(
			Context   context, 
			FieldType field,
			int       fieldIndex,
			ArrayList fieldValues,
			string    fieldName
		)
		{
			ComplexValue complexValue = null;

			if (fieldName.Length == 0)
			{
				if (fieldIndex > 0 && fieldIndex-1 < fieldValues.Count)
				{
					complexValue = (ComplexValue)fieldValues[fieldIndex-1];
				}
			}
			else
			{
				for (int ii = 0; ii < fieldIndex; ii++)
				{
					complexValue = (ComplexValue)fieldValues[ii];

					if (complexValue.Name == fieldName)
					{
						break;
					}

					complexValue = null;
				}
			}

			if (complexValue == null)
			{
				throw new InvalidSchemaException("Referenced field not found (" + fieldName + ").");
			}

			return System.Convert.ToInt32(complexValue.Value);
		}
	}
}
