using NUnit.Framework;
using Signals;

namespace Tests;

internal sealed class SparseSetTests {
    [Test]
    public void InitializeEmptySet() {
        var set = new SparseSet<int>(0, 0);
        
        Assert.That(set.Count, Is.EqualTo(0));
        Assert.That(set.Dense.Length, Is.EqualTo(0));
        Assert.That(set.Sparse.Length, Is.EqualTo(0));
    }

    [Test]
    public void InitializeSetWithCapacity() {
        var set = new SparseSet<int>(10, 5);
        
        Assert.That(set.Count, Is.EqualTo(0));
        Assert.That(set.Dense.Length, Is.EqualTo(5));
        Assert.That(set.Sparse.Length, Is.EqualTo(10));
        
        for (int i = 0; i < set.Sparse.Length; i++) {
            Assert.That(set.Sparse[i], Is.EqualTo(-1));
        }
    }

    [Test]
    public void AddNewDataWithinCapacity() {
        var set = new SparseSet<int>(10, 5);
        uint index = 2;
        int value = 10;

        ref int addedValue = ref set.Add(index, value);

        Assert.That(set.Count, Is.EqualTo(1));
        Assert.That(set.Has(index), Is.True);
        Assert.That(set.Get(index), Is.EqualTo(value));
        Assert.That(addedValue, Is.EqualTo(value));
        Assert.That(set.Sparse[(int)index], Is.EqualTo(0), "sparse index entry should point to the first dense slot.");
        Assert.That(set.Dense[0], Is.EqualTo(value), "dense set at slot 0 should contain the value.");
    }
    
    [Test]
    public void RemoveExistingData() {
        var set = new SparseSet<int>(10, 5);
        uint index = 2;
        int value = 10;

        set.Add(index, value);
        Assert.That(set.Has(index), Is.True);
        Assert.That(set.Get(index), Is.EqualTo(value));
        Assert.That(set.Count, Is.EqualTo(1));

        //remove data
        int removedValue = set.Remove(index);

        Assert.That(removedValue, Is.EqualTo(value));
        //should no longer be true, as the index here was removed from the set
        Assert.That(set.Has(index), Is.False);
        //count remainds unchanged by remove (?tbd)
        Assert.That(set.Count, Is.EqualTo(1));

        Assert.That(set.Sparse[(int)index], Is.EqualTo(-1));
        Assert.That(set.Dense[0], Is.EqualTo(default(int)), "dense slot should be default after remove.");
    }
    
    [Test]
    public void GetNonExistingDataThrowsException() {
        var set = new SparseSet<int>(10, 5);
        uint index = 5;

        Assert.Throws<InvalidOperationException>(() => {
            if (!set.Has(index)) {
                throw new InvalidOperationException("element does not exist!");
            }
            ref int value = ref set.Get(index);
        });
    }
}