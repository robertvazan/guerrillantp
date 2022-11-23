#nullable enable
#if !NET5_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace System
{
    // https://github.com/dotnet/runtime/blob/419e949d258ecee4c40a460fb09c66d974229623/src/libraries/System.Private.CoreLib/src/System/Index.cs
    internal readonly struct Index : IEquatable<Index>
    {
        private readonly int _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Index(int value, bool fromEnd = false)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative");
            }

            _value = fromEnd ? ~value : value;
        }

        private Index(int value) => _value = value;

        public static Index Start => new(0);

        public static Index End => new(~0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Index FromStart(int value) => value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative") : new Index(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Index FromEnd(int value) => value < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "value must be non-negative") : new Index(~value);

        public int Value => _value < 0 ? ~_value : _value;

        public bool IsFromEnd => _value < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOffset(int length)
        {
            var offset = _value;
            if (IsFromEnd)
            {
                offset += length + 1;
            }
            return offset;
        }

        public override bool Equals(object? value) => value is Index index && _value == index._value;

        public bool Equals(Index other) => _value == other._value;

        public override int GetHashCode() => _value;

        public static implicit operator Index(int value) => FromStart(value);

        public override string ToString() => IsFromEnd ? "^" + ((uint)Value).ToString() : ((uint)Value).ToString();
    }

    // https://github.com/dotnet/runtime/blob/419e949d258ecee4c40a460fb09c66d974229623/src/libraries/System.Private.CoreLib/src/System/Range.cs
    internal readonly struct Range : IEquatable<Range>
    {
        public Index Start { get; }

        public Index End { get; }

        public Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }

        public override bool Equals(object? value) =>
            value is Range r &&
            r.Start.Equals(Start) &&
            r.End.Equals(End);

        public bool Equals(Range other) => other.Start.Equals(Start) && other.End.Equals(End);

        public override int GetHashCode() => (Start.GetHashCode() * 31) + End.GetHashCode();

        public override string ToString() => Start + ".." + End;

        public static Range StartAt(Index start) => new(start, Index.End);

        public static Range EndAt(Index end) => new(Index.Start, end);

        public static Range All => new(Index.Start, Index.End);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int Offset, int Length) GetOffsetAndLength(int length)
        {
            int start;
            var startIndex = Start;
            start = startIndex.IsFromEnd ? length - startIndex.Value : startIndex.Value;

            int end;
            var endIndex = End;
            end = endIndex.IsFromEnd ? length - endIndex.Value : endIndex.Value;

            return (uint)end > (uint)length || (uint)start > (uint)end
                ? throw new ArgumentOutOfRangeException(nameof(length))
                : ((int Offset, int Length))(start, end - start);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    // Add support for records
    internal static class IsExternalInit { }

    internal static class RuntimeHelpers
    {
        public static T[] GetSubArray<T>(T[] array, Range range)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            (var offset, var length) = range.GetOffsetAndLength(array.Length);

            if (default(T) != null || typeof(T[]) == array.GetType())
            {
                if (length == 0)
                {
                    return Array.Empty<T>();
                }

                var dest = new T[length];
                Array.Copy(array, offset, dest, 0, length);
                return dest;
            }
            else
            {
                var dest = (T[])Array.CreateInstance(array.GetType().GetElementType(), length);
                Array.Copy(array, offset, dest, 0, length);
                return dest;
            }
        }
    }
}
#endif
#nullable disable