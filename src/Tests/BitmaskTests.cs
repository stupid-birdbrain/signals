using NUnit.Framework;
using Signals;

namespace Tests;

[TestFixture]
public class BitSetTests {
    [Test]
    public void Constructor_InitializesWithValue() {
        uint capacity = 64;

        var bitSet = new BitSet<uint>(capacity);
        Assert.That(bitSet.Value, Is.EqualTo(64u));
    }

    [Test]
    public void Size_ReturnsCorrectBitSize() {
        var bitSet = new BitSet<uint>();

        Assert.That(bitSet.Size, Is.EqualTo(32));
    }

    [Test]
    public void IsZero_ReturnsTrueForZeroValue() {
        var bitSet = new BitSet<uint>(0);
        Assert.That(bitSet.IsZero, Is.True);
    }

    [Test]
    public void IsZero_ReturnsFalseForNonZeroValue() {
        var bitSet = new BitSet<uint>(1);
        Assert.That(bitSet.IsZero, Is.False);
    }

    [Test]
    public void Get_ReturnsTrueForSetBit() {
        var bitSet = new BitSet<uint>(4);
        Assert.That(bitSet.Get(2), Is.True);
        Assert.That(bitSet.Get(0), Is.False);
        Assert.That(bitSet.Get(1), Is.False);
    }

    [Test]
    public void Set_SetsBit() {
        var bitSet = new BitSet<uint>(0);
        bitSet.Set(3);

        Assert.That(bitSet.Value, Is.EqualTo(8u));
        Assert.That(bitSet.Get(3), Is.True);
    }

    [Test]
    public void Unset_ClearsBit() {
        var bitSet = new BitSet<uint>(10);
        bitSet.Unset(1);

        Assert.That(bitSet.Value, Is.EqualTo(8u));
        Assert.That(bitSet.Get(1), Is.False);
    }

    [Test]
    public void PopCount_ReturnsNumberOfSetBits() {
        var bitSet = new BitSet<uint>(15);

        Assert.That(bitSet.PopCount(), Is.EqualTo(4));
    }

    [Test]
    public void LeadingZeroCount_ReturnsCorrectCount() {
        var bitSet = new BitSet<uint>(15);

        Assert.That(bitSet.LeadingZeroCount(), Is.EqualTo(28));
    }

    [Test]
    public void TrailingZeroCount_ReturnsCorrectCount() {
        var bitSet = new BitSet<uint>(8);

        Assert.That(bitSet.TrailingZeroCount(), Is.EqualTo(3));
    }

    [Test]
    public void BitwiseOperators_WorkCorrectly() {

        var a = new BitSet<uint>(10);
        var b = new BitSet<uint>(12);

        Assert.That((a & b).Value, Is.EqualTo(8u));
        Assert.That((a | b).Value, Is.EqualTo(14u));
        Assert.That((~a).Value, Is.EqualTo(~10u));
    }

    [Test]
    public void FromBooleans_CreatesCorrectBitSet() {
        bool[] values = new[] { true, false, true, false };

        var bitSet = BitSet<uint>.FromBooleans(values);

        Assert.That(bitSet.Value, Is.EqualTo(5u));
        Assert.That(bitSet.Get(0), Is.True);
        Assert.That(bitSet.Get(1), Is.False);
        Assert.That(bitSet.Get(2), Is.True);
        Assert.That(bitSet.Get(3), Is.False);
    }

    [Test]
    public void Enumeration_ReturnsIndicesOfSetBits() {
        var bitSet = new BitSet<uint>(10);
        var indices = bitSet.ToList();

        Assert.That(indices.Count, Is.EqualTo(2));
        Assert.That(indices, Contains.Item(1));
        Assert.That(indices, Contains.Item(3));
    }
}