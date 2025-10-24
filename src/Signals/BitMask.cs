using System.Collections;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Signals;

/// <summary>
///     Provides a way to manipulate flags using bitwise operations.
/// </summary>
public struct BitSet<T> : IEnumerable<int> where T : unmanaged, IUnsignedNumber<T>, IBitwiseOperators<T, T, T>, IShiftOperators<T, int, T> {
    private const MethodImplOptions _inline_flags = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;

    /// <summary>
    ///     An empty bitmask.
    /// </summary>
    public static readonly BitSet<T> Zero = default;

    public static byte BitSize { get; } = (byte)(Marshal.SizeOf<T>() * 8);

    public T Value;

    public readonly int Size => BitSize;
    public readonly bool IsZero => T.IsZero(Value);

    public BitSet(T value) => Value = value;

    [MethodImpl(_inline_flags)]
    public readonly bool Get(int index) => !T.IsZero(Value & (T.One << index));
    [MethodImpl(_inline_flags)]
    public void Set(int index) => Value |= T.One << index;
    [MethodImpl(_inline_flags)]
    public void Unset(int index) => Value &= ~(T.One << index);

    [MethodImpl(_inline_flags)] public readonly int PopCount() => PopCount(Value);
    [MethodImpl(_inline_flags)] public readonly int LeadingZeroCount() => LeadingZeroCount(Value);
    [MethodImpl(_inline_flags)] public readonly int TrailingZeroCount() => TrailingZeroCount(Value);
    [MethodImpl(_inline_flags)] public readonly int LeadingOneCount() => LeadingZeroCount(~Value);
    [MethodImpl(_inline_flags)] public readonly int TrailingOneCount() => TrailingZeroCount(~Value);

    [MethodImpl(_inline_flags)] public static BitSet<T> operator ~(BitSet<T> a) => new(~a.Value);
    [MethodImpl(_inline_flags)] public static BitSet<T> operator &(BitSet<T> a, BitSet<T> b) => new(a.Value & b.Value);
    [MethodImpl(_inline_flags)] public static BitSet<T> operator |(BitSet<T> a, BitSet<T> b) => new(a.Value | b.Value);

    [MethodImpl(_inline_flags)]
    public static int PopCount(T value) {
        if(typeof(T) == typeof(byte)) { return BitOperations.PopCount(Unsafe.As<T, byte>(ref value)); }
        if(typeof(T) == typeof(ushort)) { return BitOperations.PopCount(Unsafe.As<T, ushort>(ref value)); }
        if(typeof(T) == typeof(uint)) { return BitOperations.PopCount(Unsafe.As<T, uint>(ref value)); }
        if(typeof(T) == typeof(ulong)) { return BitOperations.PopCount(Unsafe.As<T, ulong>(ref value)); }
        throw new NotSupportedException();
    }

    [MethodImpl(_inline_flags)]
    public static int LeadingZeroCount(T value) {
        if(typeof(T) == typeof(byte)) { return BitOperations.LeadingZeroCount(Unsafe.As<T, byte>(ref value)); }
        if(typeof(T) == typeof(ushort)) { return BitOperations.LeadingZeroCount(Unsafe.As<T, ushort>(ref value)); }
        if(typeof(T) == typeof(uint)) { return BitOperations.LeadingZeroCount(Unsafe.As<T, uint>(ref value)); }
        if(typeof(T) == typeof(ulong)) { return BitOperations.LeadingZeroCount(Unsafe.As<T, ulong>(ref value)); }
        throw new NotSupportedException();
    }

    [MethodImpl(_inline_flags)]
    public static int TrailingZeroCount(T value) {
        if(typeof(T) == typeof(byte)) { return BitOperations.TrailingZeroCount(Unsafe.As<T, byte>(ref value)); }
        if(typeof(T) == typeof(ushort)) { return BitOperations.TrailingZeroCount(Unsafe.As<T, ushort>(ref value)); }
        if(typeof(T) == typeof(uint)) { return BitOperations.TrailingZeroCount(Unsafe.As<T, uint>(ref value)); }
        if(typeof(T) == typeof(ulong)) { return BitOperations.TrailingZeroCount(Unsafe.As<T, ulong>(ref value)); }
        throw new NotSupportedException();
    }

    public static BitSet<T> FromBooleans(ReadOnlySpan<bool> booleans) {
        Debug.Assert(booleans.Length <= BitSize);

        BitSet<T> result = default;
        for(int i = 0; i < booleans.Length; i++)
            result.Value |= (booleans[i] ? T.One : T.Zero) << i;

        return result;
    }

    readonly IEnumerator IEnumerable.GetEnumerator() => new Iterator(this);
    readonly IEnumerator<int> IEnumerable<int>.GetEnumerator() => new Iterator(this);

    public struct Iterator(BitSet<T> mask) : IEnumerator<int> {
        public BitSet<T> Mask = mask;
        public int Current { get; private set; } = -1;

        readonly object IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() {
            if(Mask.IsZero) {
                Current = -1;
                return false;
            }

            Current = Mask.TrailingZeroCount();
            Mask.Unset(Current);
            return true;
        }

        readonly void IDisposable.Dispose() { }
        void IEnumerator.Reset() => throw new NotImplementedException();
    }
}

public struct BitArray<T>() where T : unmanaged, IUnsignedNumber<T>, IBitwiseOperators<T, T, T>, IShiftOperators<T, int, T> {
    private const MethodImplOptions _inline_flags = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
    public static byte BitsPerMask { get; } = (byte)(Marshal.SizeOf<T>() * 8);

    public BitSet<T>[] Array = [];

    [MethodImpl(_inline_flags)]
    public readonly (int maskIndex, int bitIndex) DivRem(int index)
        => Math.DivRem(index, BitsPerMask);

    [MethodImpl(_inline_flags)]
    public readonly bool Get((int maskIndex, int bitIndex) divrem)
        => Array[divrem.maskIndex].Get(divrem.bitIndex);

    [MethodImpl(_inline_flags)]
    public readonly bool TryGet((int maskIndex, int bitIndex) divrem)
        => divrem.maskIndex < Array.Length && Array[divrem.maskIndex].Get(divrem.bitIndex);

    [MethodImpl(_inline_flags)]
    public readonly void Set((int maskIndex, int bitIndex) divrem)
        => Array[divrem.maskIndex].Set(divrem.bitIndex);

    [MethodImpl(_inline_flags)]
    public readonly void Unset((int maskIndex, int bitIndex) divrem)
        => Array[divrem.maskIndex].Unset(divrem.bitIndex);

    [MethodImpl(_inline_flags)]
    public readonly bool Get(int index) => Get(DivRem(index));
    [MethodImpl(_inline_flags)]
    public readonly bool TryGet(int index) => TryGet(DivRem(index));
    [MethodImpl(_inline_flags)]
    public readonly void Set(int index) => Set(DivRem(index));
    [MethodImpl(_inline_flags)]
    public readonly void Unset(int index) => Unset(DivRem(index));

    [MethodImpl(_inline_flags)]
    public readonly IEnumerable<int> GetSetBits() {
        for(int i = 0; i < Array.Length; ++i) {
            if(!Array[i].IsZero) {
                for(int j = 0; j < BitSet<T>.BitSize; ++j) {
                    if(Array[i].Get(j)) {
                        yield return i * BitSet<T>.BitSize + j;
                    }
                }
            }
        }
    }

    [MethodImpl(_inline_flags)]
    public readonly bool Intersects(BitArray<T> other) {
        if(this.Array == null || other.Array == null || this.Array.Length == 0 || other.Array.Length == 0) {
            return false;
        }

        int minLength = Math.Min(this.Array.Length, other.Array.Length);
        for(int i = 0; i < minLength; i++) {
            if((this.Array[i].Value & other.Array[i].Value) != T.Zero) {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(_inline_flags)]
    public BitArray<T> Clone(int index) {
        var (maskIndex, bitIndex) = DivRem(index);

        int currentLength = Array.Length;
        uint newRequiredLength = (uint)maskIndex + 1;

        uint newArrayLength = (uint)Math.Max(currentLength, (int)newRequiredLength);
        if(newArrayLength > currentLength) {
            newArrayLength = BitOperations.RoundUpToPowerOf2(newArrayLength);
        }

        var newInternalArray = new BitSet<T>[newArrayLength];

        if(currentLength > 0) {
            System.Array.Copy(Array, newInternalArray, System.Math.Min(currentLength, (int)newArrayLength));
        }

        newInternalArray[maskIndex].Set(bitIndex);

        return new BitArray<T> { Array = newInternalArray };
    }

    [MethodImpl(_inline_flags)]
    public BitArray<T> CloneMerge(ReadOnlySpan<BitSet<T>> otherMaskSpan) {
        if(otherMaskSpan.IsEmpty) {
            return this;
        }

        uint newArrayLength = (uint)Math.Max(Array.Length, otherMaskSpan.Length);

        var newInternalArray = new BitSet<T>[newArrayLength];

        for(int i = 0; i < newArrayLength; i++) {
            var currentValue = (i < Array.Length) ? Array[i] : BitSet<T>.Zero;
            var otherValue = (i < otherMaskSpan.Length) ? otherMaskSpan[i] : BitSet<T>.Zero;
            newInternalArray[i] = currentValue | otherValue;
        }

        return new BitArray<T> { Array = newInternalArray };
    }
}