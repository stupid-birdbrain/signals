using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Signals.Core;

public struct Bitset256 {
    public const int CAPACITY = 256;
    private const ulong high_bit = 1UL << 63;
    private ulong _0;
    private ulong _1;
    private ulong _2;
    private ulong _3;

    public static Bitset256 Zero => default;

#if !NETSTANDARD
    public bool IsZero => AsVector256() == Vector256<ulong>.Zero;
#else
    public bool IsZero =>
        _0 == 0 && _1 == 0 && _2 == 0 && _3 == 0;
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index) {
        ref ulong lane = ref Unsafe.Add(ref _0, (nuint)(uint)index >> 6);
        lane |= high_bit >> (index & 63);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(int index) {
        ref ulong lane = ref Unsafe.Add(ref _0, (nuint)(uint)index >> 6);
        lane &= ~(high_bit >> (index & 63));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsSet(int index) {
        ulong lane = Unsafe.Add(ref Unsafe.AsRef(in _0), (nuint)(uint)index >> 6);
        return (lane & (high_bit >> (index & 63))) != 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(ref readonly Bitset256 other) {
#if !NETSTANDARD
        if (Avx.IsSupported) {
            return Avx.TestC(AsVector256(), other.AsVector256());
        }
#endif
        return (_0 & other._0) == other._0 &&
            (_1 & other._1) == other._1 &&
            (_2 & other._2) == other._2 &&
            (_3 & other._3) == other._3;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool AndAny(ref readonly Bitset256 other) {
#if !NETSTANDARD
        if (Avx.IsSupported) {
            return !Avx.TestZ(AsVector256(), other.AsVector256());
        }
#endif
        return ((_0 & other._0) |
            (_1 & other._1) |
            (_2 & other._2) |
            (_3 & other._3)) != 0;
    }

    public readonly int PopCount() =>
        BitOperations.PopCount(_0) +
        BitOperations.PopCount(_1) +
        BitOperations.PopCount(_2) +
        BitOperations.PopCount(_3);

#if !NETSTANDARD
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Vector256<ulong> AsVector256() =>
        Vector256.LoadUnsafe(ref Unsafe.AsRef(in _0));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Bitset256 FromVector256(Vector256<ulong> vector)
    {
        Bitset256 result = default;
        vector.StoreUnsafe(ref result._0);
        return result;
    }

    public static Bitset256 operator &(Bitset256 left, Bitset256 right) =>
        FromVector256(left.AsVector256() & right.AsVector256());

    public static Bitset256 operator |(Bitset256 left, Bitset256 right) =>
        FromVector256(left.AsVector256() | right.AsVector256());

    public static Bitset256 operator ^(Bitset256 left, Bitset256 right) =>
        FromVector256(left.AsVector256() ^ right.AsVector256());

    public static Bitset256 operator ~(Bitset256 value) =>
        FromVector256(~value.AsVector256());

    public static bool operator ==(Bitset256 left, Bitset256 right) =>
        left.AsVector256() == right.AsVector256();

    public static bool operator !=(Bitset256 left, Bitset256 right) =>
        left.AsVector256() != right.AsVector256();
#else
    public static Bitset256 operator &(Bitset256 l, Bitset256 r) => new Bitset256
    {
        _0 = l._0 & r._0,
        _1 = l._1 & r._1,
        _2 = l._2 & r._2,
        _3 = l._3 & r._3,
    };
#endif
}

internal struct BitmaskArray256 {
    public Bitset256[]? Array;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int maskIndex, int bitIndex) DivRem(int index) => Math.DivRem(index, Bitset256.CAPACITY);

    public BitmaskArray256 CloneAndSet(int index) {
        var (maskIndex, bitIndex) = DivRem(index);

        int currentLength = Array?.Length ?? 0;
        int requiredLength = maskIndex + 1;
        var newArray = new Bitset256[requiredLength > currentLength ? requiredLength : currentLength];

        if (currentLength > 0) {
            System.Array.Copy(Array!, newArray, currentLength);
        }

        newArray[maskIndex].Set(bitIndex);
        return new BitmaskArray256 { Array = newArray };
    }

    public readonly ReadOnlySpan<Bitset256> AsSpan() => Array;
}