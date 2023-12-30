// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Represent a type can be used to index a collection either from the start or the end.
    /// </summary>
    /// <remarks>
    /// Index is used by the C# compiler to support the new index syntax
    /// <code>
    /// int[] someArray = new int[5] { 1, 2, 3, 4, 5 } ;
    /// int lastElement = someArray[^1]; // lastElement = 5
    /// </code>
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal readonly struct Index
    {
        private readonly int _value;

        // The following private constructors mainly created for perf reason to avoid the checks
        private Index(int value) => _value = value;

        /// <summary>
        /// Create an Index from the start at the position indicated by the value.
        /// </summary>
        /// <param name="value">The index value from the start.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Index FromStart(int value)
        {
            return value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "Non-negative number required.") : new Index(value);
        }

        /// <summary>
        /// Indicates whether the index is from the start or the end.
        /// </summary>
        public bool IsFromEnd => _value < 0;

        /// <summary>Calculate the offset from the start using the giving collection length.</summary>
        /// <param name="length">The length of the collection that the Index will be used with. length has to be a positive value</param>
        /// <remarks>
        /// For performance reason, we don't validate the input length parameter and the returned offset value against negative values.
        /// we don't validate either the returned offset is greater than the input length.
        /// It is expected Index will be used with collections which always have non negative length/count. If the returned offset is negative and
        /// then used to index a collection will get out of range exception which will be same affect as the validation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOffset(int length)
        {
            int offset = _value;
            if (IsFromEnd)
            {
                // offset = length - (~value)
                // offset = length + (~(~value) + 1)
                // offset = length + value + 1

                offset += length + 1;
            }
            return offset;
        }

        /// <summary>
        /// Converts integer number to an Index.
        /// </summary>
        public static implicit operator Index(int value) => FromStart(value);
    }

    /// <summary>
    /// Represent a range has start and end indexes.
    /// </summary>
    /// <remarks>
    /// Range is used by the C# compiler to support the range syntax.
    /// <code>
    /// int[] someArray = new int[5] { 1, 2, 3, 4, 5 };
    /// int[] subArray1 = someArray[0..2]; // { 1, 2 }
    /// int[] subArray2 = someArray[1..^0]; // { 2, 3, 4, 5 }
    /// </code>
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal readonly struct Range
    {
        /// <summary>
        /// Represent the inclusive start index of the Range.
        /// </summary>
        public Index Start { get; }

        /// <summary>
        /// Represent the exclusive end index of the Range.
        /// </summary>
        public Index End { get; }

        /// <summary>
        /// Construct a Range object using the start and end indexes.
        /// </summary>
        /// <param name="start">Represent the inclusive start index of the range.</param>
        /// <param name="end">Represent the exclusive end index of the range.</param>
        public Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit;
}
