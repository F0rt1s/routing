﻿// Itinero - Routing for .NET
// Copyright (C) 2016 Abelshausen Ben
// 
// This file is part of Itinero.
// 
// Itinero is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// Itinero is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Itinero. If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.IO;
using Itinero.Attributes;
using System.Globalization;
using System;
using System.Text;

namespace Itinero
{
    /// <summary>
    /// Contains general extensions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets the value associated with the specified key or returns the default.
        /// </summary>
        public static TValue TryGetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            if(dictionary.TryGetValue(key, out value))
            {
                return value;
            }
            return default(TValue);
        }

        /// <summary>
        /// Gets the next power of two.
        /// </summary>
        public static int NextPowerOfTwo(int i)
        {
            if(i == 0)
            {
                return 0;
            }
            else if(i == 1)
            {
                return 1;
            }
            else if(i == 2)
            {
                return 2;
            }
            else if (!((i & (i - 1)) == 0))
            {
                i |= i >> 1;
                i |= i >> 2;
                i |= i >> 4;
                i |= i >> 8;
                i |= i >> 16;
                i++;
            }
            return i;
        }

        /// <summary>
        /// Writes the given value with size prefix.
        /// </summary>
        public static long WriteWithSize(this System.IO.Stream stream, string value)
        {
            var bytes = System.Text.UnicodeEncoding.Unicode.GetBytes(value);
            return stream.WriteWithSize(bytes);
        }

        /// <summary>
        /// Writes the given value with size prefix.
        /// </summary>
        public static long WriteWithSize(this System.IO.Stream stream, string[] values)
        {
            var memoryStream = new MemoryStream();
            for (var i = 0; i < values.Length; i++)
            {
                memoryStream.WriteWithSize(values[i]);
            }
            return stream.WriteWithSize(memoryStream.ToArray());
        }

        /// <summary>
        /// Writes the given value with size prefix.
        /// </summary>
        public static long WriteWithSize(this System.IO.Stream stream, byte[] value)
        {
            stream.Write(System.BitConverter.GetBytes((long)value.Length), 0, 8);
            stream.Write(value, 0, value.Length);
            return value.Length + 8;
        }

        /// <summary>
        /// Writes an attributes collection to the given stream and prefixed with it's size.
        /// </summary>
        public static long WriteWithSize(this IAttributeCollection attributes, System.IO.Stream stream)
        {
            return attributes.SerializeWithSize(stream);
        }

        /// <summary>
        /// Reads an attributes collection.
        /// </summary>
        public static IAttributeCollection ReadWithSizeAttributesCollection(this System.IO.Stream stream)
        {
            return IAttributeCollectionExtension.DeserializeWithSize(stream);
        }

        /// <summary>
        /// Reads a string array.
        /// </summary>
        public static string[] ReadWithSizeStringArray(this System.IO.Stream stream)
        {
            var longBytes = new byte[8];
            stream.Read(longBytes, 0, 8);
            var position = stream.Position;
            var size = System.BitConverter.ToInt64(longBytes, 0);

            var strings = new List<string>();
            while (stream.Position < position + size)
            {
                strings.Add(stream.ReadWithSizeString());
            }
            return strings.ToArray();
        }

        /// <summary>
        /// Reads a string.
        /// </summary>
        public static string ReadWithSizeString(this System.IO.Stream stream)
        {
            var longBytes = new byte[8];
            stream.Read(longBytes, 0, 8);
            var size = BitConverter.ToInt64(longBytes, 0);
            var data = new byte[size];
            stream.Read(data, 0, (int)size);

            return System.Text.UnicodeEncoding.Unicode.GetString(data, 0, data.Length);
        }

        /// <summary>
        /// Sets the position within the given stream. 
        /// </summary>
        public static long SeekBegin(this BinaryWriter stream, long offset)
        {
            if(offset <= int.MaxValue)
            {
                return stream.Seek((int)offset, SeekOrigin.Begin);
            }
            stream.Seek(0, SeekOrigin.Begin);
            while(offset > int.MaxValue)
            {
                stream.Seek(int.MaxValue, SeekOrigin.Current);
                offset -= int.MaxValue;
            }
            return stream.Seek((int)offset, SeekOrigin.Current);
        }
        
        /// <summary>
        /// Returns a string representing the object in a culture invariant way.
        /// </summary>
        public static string ToInvariantString(this object obj)
        {
            return obj is IConvertible ? ((IConvertible)obj).ToString(CultureInfo.InvariantCulture)
                : obj is IFormattable ? ((IFormattable)obj).ToString(null, CultureInfo.InvariantCulture)
                : obj.ToString();
        }

        /// <summary>
        /// Returns a string for the given array even when null.
        /// </summary>
        public static string ToStringSafe<T>(this T[] array)
        {
            if (array == null)
            {
                return "null";
            }
            var stringBuilder = new StringBuilder();
            stringBuilder.Append('[');
            for(var i = 0; i < array.Length; i++)
            {
                if (i > 0)
                {
                    stringBuilder.Append(',');
                }
                stringBuilder.Append(array[i].ToInvariantString());
            }
            stringBuilder.Append(']');
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Shrinks and copies the given list, removes elements with indices in the toRemove set.
        /// </summary>
        public static List<T> ShrinkAndCopyList<T>(this List<T> list, HashSet<int> toRemove)
        {
            var shrinked = new List<T>(System.Math.Max(list.Count - toRemove.Count, 0));
            for (var i = 0; i < list.Count; i++)
            {
                if (!toRemove.Contains(i))
                {
                    shrinked.Add(list[i]);
                }
            }
            return shrinked;
        }

        /// <summary>
        /// Shrinks and copies the matrix and removes rows/columns with indices in the toRemove set.
        /// </summary>
        public static T[][] SchrinkAndCopyMatrix<T>(this T[][] matrix, HashSet<int> toRemove)
        {
            var schrunk = new T[matrix.Length - toRemove.Count][];
            var schrunkX = 0;
            for (var x = 0; x < matrix.Length; x++)
            {
                if (!toRemove.Contains(x))
                { // keep this element.
                    var schrunkY = 0;
                    schrunk[schrunkX] = new T[matrix.Length - toRemove.Count];
                    for (var y = 0; y < matrix[x].Length; y++)
                    {
                        if (!toRemove.Contains(y))
                        { // keep this element.
                            schrunk[schrunkX][schrunkY] = matrix[x][y];
                            schrunkY++;
                        }
                    }
                    schrunkX++;
                }
            }
            return schrunk;
        }

        /// <summary>
        /// Moves this enumerator until a given condition is met.
        /// </summary>
        public static bool MoveNextUntil<T>(this IEnumerator<T> enumerator, Func<T, bool> stopHere)
        {
            while (enumerator.MoveNext())
            {
                if (stopHere(enumerator.Current))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Copies elements to the given target.
        /// </summary>
        public static void CopyTo<T>(this T[] array, T[] target, int index, int sourceIndex, int count)
        {
            for(var i = 0; i < count; i++)
            {
                target[index + i] = array[sourceIndex + i];
            }
        }

        /// <summary>
        /// Creates a new sub-array, containing the element starting at index, with the given length.
        /// </summary>
        public static T[] SubArray<T>(this T[] array, int index, int length)
        {
            var sub = new T[length];
            for(var i = 0; i < length; i++)
            {
                sub[i] = array[i + index];
            }
            return sub;
        }

        /// <summary>
        /// Creates a new array appending the given array.
        /// </summary>
        public static T[] Append<T>(this T[] array, T[] other)
        {
            var result = new T[array.Length + other.Length];
            for(var i = 0; i < array.Length; i++)
            {
                result[i] = array[i];
            }
            for (var i = 0; i < other.Length; i++)
            {
                result[array.Length + i] = other[i];
            }
            return result;
        }

        /// <summary>
        /// Creates a new array appending the given element.
        /// </summary>
        public static T[] Append<T>(this T[] array, T other)
        {
            var result = new T[array.Length + 1];
            for (var i = 0; i < array.Length; i++)
            {
                result[i] = array[i];
            }
            result[array.Length] = other;
            return result;
        }

        /// <summary>
        /// Reverse the elements in the given array.
        /// </summary>
        public static void Reverse<T>(this T[] array)
        {
            System.Array.Reverse(array);
        }
        
        /// <summary>
        /// Returns a copy of the given array with the elements reversed.
        /// </summary>
        public static T[] ReverseAndCopy<T>(this T[] array)
        {
            var reversed = new T[array.Length];
            for(var i = 0; i < array.Length; i++)
            {
                reversed[array.Length - 1 - i] = array[i];
            }
            return reversed;
        }
    }
}