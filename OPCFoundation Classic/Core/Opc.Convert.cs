//============================================================================
// TITLE: Opc.Convert.cs
//
// CONTENTS:
// 
// Contains classes that facilitate type conversion.
//
// (c) Copyright 2003-2005 The OPC Foundation
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
// 2003/03/26 RSA   Initial implementation.
// 2005/01/26 RSA   Fixed bugs in Match implementation.

using System;
using System.Xml;
using System.Collections;
using System.Text;
using System.Threading;
using System.Globalization;

namespace Opc
{
	/// <summary>
	/// Defines various functions used to convert types.
	/// </summary>
	public class Convert
	{
		/// <summary>
		/// Checks whether the array contains any useful data.
		/// </summary>
		public static bool IsValid(Array array)
		{
			return (array != null && array.Length > 0);
		}

		/// <summary>
		/// Checks whether the array contains any useful data.
		/// </summary>
		public static bool IsEmpty(Array array)
		{
			return (array == null || array.Length == 0);
		}

		/// <summary>
		/// Checks whether the string contains any useful data.
		/// </summary>
		public static bool IsValid(string target)
		{
			return (target != null && target.Length > 0);
		}

		/// <summary>
		/// Checks whether the string contains any useful data.
		/// </summary>
		public static bool IsEmpty(string target)
		{
			return (target == null || target.Length == 0);
		}

		/// <summary>
		/// Performs a deep copy of an object if possible.
		/// </summary>
		public static object Clone(object source)
		{
			if (source == null)               return null;
			if (source.GetType().IsValueType) return source;

			if (source.GetType().IsArray || source.GetType() == typeof(Array))
			{
				Array array = (Array)((Array)source).Clone();

				for (int ii = 0; ii < array.Length; ii++)
				{
					array.SetValue(Convert.Clone(array.GetValue(ii)), ii);
				}

				return array;
			}

			try   { return ((ICloneable)source).Clone(); }
			catch { throw new NotSupportedException("Object cannot be cloned."); }
		}

		/// <summary>
		/// Does a deep comparison between two objects.
		/// </summary>
		public static bool Compare(object a, object b)
		{
			if (a == null || b == null) return (a == null && b == null);

			System.Type type1 = a.GetType();
			System.Type type2 = b.GetType();

			if (type1 != type2) return false;

			if (type1.IsArray && type2.IsArray)
			{
				Array array1 = (Array)a;
				Array array2 = (Array)b;

				if (array1.Length != array2.Length) return false;

				for (int ii = 0; ii < array1.Length; ii++)
				{
					if (!Compare(array1.GetValue(ii), array2.GetValue(ii))) return false;
				}

				return true;
			}

			return a.Equals(b);
		}

		/// <summary>
		/// Converts an object to the specified type and returns a deep copy.
		/// </summary>
		public static object ChangeType(object source, System.Type newType)
		{
			// check for null source object.
			if (source == null)
			{
				if (newType != null && newType.IsValueType)
				{
					return Activator.CreateInstance(newType);
				}

				return null;
			}

			// check for null type or 'object' type.
			if (newType == null || newType == typeof(object) || newType == source.GetType()) 
			{
				return Clone(source);
			}

			System.Type type = source.GetType();

			// convert between array types.
			if (type.IsArray && newType.IsArray)
			{
				ArrayList array = new ArrayList(((Array)source).Length);

				foreach (object element in (Array)source)
				{
					array.Add(Opc.Convert.ChangeType(element, newType.GetElementType()));
				}

				return array.ToArray(newType.GetElementType());
			}

			// convert scalar value to an array type.
			if (!type.IsArray && newType.IsArray)
			{
				ArrayList array = new ArrayList(1);
				array.Add(Convert.ChangeType(source, newType.GetElementType()));
				return array.ToArray(newType.GetElementType());
			}

			// convert single element array type to scalar type.
			if (type.IsArray && !newType.IsArray && ((Array)source).Length == 1)
			{
				return System.Convert.ChangeType(((Array)source).GetValue(0), newType);
			}

			// convert array type to string.
			if (type.IsArray && newType == typeof(string))
			{
				StringBuilder buffer = new StringBuilder();

				buffer.Append("{ ");

				int count = 0;

				foreach (object element in (Array)source)
				{					
					buffer.AppendFormat("{0}", Opc.Convert.ChangeType(element, typeof(string)));

					count++;

					if (count < ((Array)source).Length)
					{
						buffer.Append(" | ");
					}
				}

				buffer.Append(" }");

				return buffer.ToString();
			}

			// convert to enumerated type.
			if (newType.IsEnum)
			{
				if (type == typeof(string))
				{
					// check for an integer passed as a string.
					if (((string)source).Length > 0 && Char.IsDigit((string)source, 0))
					{
						return Enum.ToObject(newType, System.Convert.ToInt32(source));
					}

					// parse a string value.
					return Enum.Parse(newType, (string)source);
				}
				else
				{
					// convert numerical value to an enum.
					return Enum.ToObject(newType, source);
				}
			}

			// convert to boolean type.
			if (newType == typeof(bool))
			{
				// check for an integer passed as a string.
				if (typeof(string).IsInstanceOfType(source))
				{
					string text = (string)source;

					if (text.Length > 0 && (text[0] == '+' ||  text[0] == '-' || Char.IsDigit(text, 0)))
					{
						return System.Convert.ToBoolean(System.Convert.ToInt32(source));
					}
				}

				return System.Convert.ToBoolean(source);
			}

			// use default conversion.
			return System.Convert.ChangeType(source, newType);
		}

		/// <summary>
		/// Formats an item or property value as a string.
		/// </summary>
		public static string ToString(object source)
		{
			// check for null
			if (source == null) return "";

			System.Type type = source.GetType();

			// check for invalid values in date times.
			if (type == typeof(DateTime))
			{
				if (((DateTime)source) == DateTime.MinValue)
				{
					return String.Empty;
				}

				DateTime date = (DateTime)source;

				if (date.Millisecond > 0)
				{
					return date.ToString("yyyy-MM-dd HH:mm:ss.fff");
				}
				else
				{
					return date.ToString("yyyy-MM-dd HH:mm:ss");
				}
			}

			// use only the local name for qualified names.
			if (type == typeof(XmlQualifiedName))
			{
				return ((XmlQualifiedName)source).Name;
			}		
		
			// use only the name for system types.
			if (type.FullName == "System.RuntimeType")
			{
				return ((System.Type)source).Name;
			}

			// treat byte arrays as a special case.
			if (type == typeof(byte[]))
			{
				byte[] bytes = (byte[])source;

				StringBuilder buffer = new StringBuilder(bytes.Length*3);

				for (int ii = 0; ii < bytes.Length; ii++)
				{
					buffer.Append(bytes[ii].ToString("X2"));
					buffer.Append(" ");
				}

				return buffer.ToString();
			}
		
			// show the element type and length for arrays.
			if (type.IsArray)
			{
				return String.Format("{0}[{1}]", type.GetElementType().Name, ((Array)source).Length);
			}

			// instances of array are always treated as arrays of objects.
			if (type == typeof(Array))
			{
				return String.Format("Object[{0}]", ((Array)source).Length);
			}

			// default behavoir.
			return source.ToString();
		}

		/// <summary>
		/// Tests if the specified string matches the specified pattern.
		/// </summary>
		public static bool Match(string target, string pattern, bool caseSensitive)
		{
			// an empty pattern always matches.
			if (pattern == null || pattern.Length == 0)
			{
				return true;
			}

			// an empty string never matches.
			if (target == null || target.Length == 0)
			{
				return false;
			}

			// check for exact match
			if (caseSensitive)
			{
				if (target == pattern)
				{
					return true;
				}
			}
			else
			{
				if (target.ToLower() == pattern.ToLower())
				{
					return true;
				}
			}
 
			char c;
			char p;
			char l;

			int pIndex = 0;
			int tIndex = 0;

			while (tIndex < target.Length && pIndex < pattern.Length)
			{
				p = ConvertCase(pattern[pIndex++], caseSensitive);

				if (pIndex > pattern.Length)
				{
					return (tIndex >= target.Length); // if end of string true
				}
	
				switch (p)
				{
					// match zero or more char.
					case '*':
					{
						while (tIndex < target.Length) 
						{   
							if (Match(target.Substring(tIndex++), pattern.Substring(pIndex), caseSensitive))
							{
								return true;
							}
						}
			
						return Match(target, pattern.Substring(pIndex), caseSensitive);
					}

					// match any one char.
					case '?':
					{
						// check if end of string when looking for a single character.
						if (tIndex >= target.Length) 
						{
							return false;  
						}

						// check if end of pattern and still string data left.
						if (pIndex >= pattern.Length && tIndex < target.Length-1)
						{
							return false;
						}

						tIndex++;
						break;
					}

					// match char set 
					case '[': 
					{
						c = ConvertCase(target[tIndex++], caseSensitive);

						if (tIndex > target.Length)
						{
							return false; // syntax 
						}

						l = '\0'; 

						// match a char if NOT in set []
						if (pattern[pIndex] == '!') 
						{
							++pIndex;

							p = ConvertCase(pattern[pIndex++], caseSensitive);

							while (pIndex < pattern.Length) 
							{
								if (p == ']') // if end of char set, then 
								{
									break; // no match found 
								}

								if (p == '-') 
								{
									// check a range of chars? 
									p = ConvertCase(pattern[pIndex], caseSensitive);

									// get high limit of range 
									if (pIndex > pattern.Length || p == ']')
									{
										return false; // syntax 
									}

									if (c >= l && c <= p) 
									{
										return false; // if in range, return false
									}
								} 

								l = p;
						
								if (c == p) // if char matches this element 
								{
									return false; // return false 
								}
								
								p = ConvertCase(pattern[pIndex++], caseSensitive);
							} 
						}

						// match if char is in set []
						else 
						{
							p = ConvertCase(pattern[pIndex++], caseSensitive);

							while (pIndex < pattern.Length) 
							{
								if (p == ']') // if end of char set, then no match found 
								{
									return false;
								}

								if (p == '-') 
								{   
									// check a range of chars? 
									p = ConvertCase(pattern[pIndex], caseSensitive);
							
									// get high limit of range 
									if (pIndex > pattern.Length || p == ']')
									{
										return false; // syntax 
									}

									if (c >= l  &&  c <= p) 
									{
										break; // if in range, move on 
									}
								} 

								l = p;
						
								if (c == p) // if char matches this element move on 
								{
									break;           
								}
								
								p = ConvertCase(pattern[pIndex++], caseSensitive);
							} 

							while (pIndex < pattern.Length && p != ']') // got a match in char set skip to end of set
							{
								p = pattern[pIndex++];             
							}
						}

						break; 
					}

					// match digit.
					case '#':
					{
						c = target[tIndex++]; 

						if (!Char.IsDigit(c))
						{
							return false; // not a digit
						}

						break;
					}

					// match exact char.
					default: 
					{
						c = ConvertCase(target[tIndex++], caseSensitive); 
				
						if (c != p) // check for exact char
						{
							return false; // not a match
						}

						// check if end of pattern and still string data left.
						if (pIndex >= pattern.Length && tIndex < target.Length-1)
						{
							return false;
						}

						break;
					}
				} 
			} 

			return true;
		} 
		
		// ConvertCase
		private static char ConvertCase(char c, bool caseSensitive)
		{
			return (caseSensitive)?c:Char.ToUpper(c);
		}
	}
}
