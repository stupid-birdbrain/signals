using System.Numerics;
using System.Runtime.CompilerServices;

namespace Signals;

public struct SparseSet<TData>() where TData : unmanaged {
    private const int invalid_data = -1;
    
    private int[] _sparse;
    private TData[] _dense;
    
    public Span<int> Sparse => _sparse;
    public Span<TData> Dense => _dense;
    
    public int Count { get; private set; }

    public ref TData this[int index] {
        get {
            return ref Get((uint)index);
        }
    }
    
    public SparseSet(int sparseAmount, int denseAmount) : this() {
        _sparse = sparseAmount > 0 ? new int[sparseAmount] : Array.Empty<int>();
        _dense = denseAmount > 0 ? new TData[denseAmount] : Array.Empty<TData>();
        
        for (int i = 0; i < _sparse.Length; i++)
            _sparse[i] = invalid_data;

        Count = 0;
    }
    
    public readonly bool Has(uint index) 
        => index < _sparse.Length && _sparse[index] != invalid_data;
    
    public readonly ref TData Get(uint index) {
        if (!Has(index)) {
            throw new InvalidOperationException($"element at index {index} does not exist in the sparse set!");
        }
        return ref _dense[_sparse[index]];
    }

    public ref TData Add(uint index, in TData data) {
        if (index >= _sparse.Length) {
            var oldLength = _sparse.Length;
            var newLength = (int)BitOperations.RoundUpToPowerOf2((index + 1));
            Array.Resize(ref _sparse, newLength);
            for (int i = oldLength; i < newLength; i++) 
                _sparse[i] = invalid_data;
        }

        var denseIdx = _sparse[index];
        if (denseIdx == invalid_data) {
            _sparse[index] = denseIdx = Count++;

            if (denseIdx >= _dense.Length)
                Array.Resize(ref _dense, (int)BitOperations.RoundUpToPowerOf2((uint)(denseIdx + 1)));
        }

        _dense[denseIdx] = data;

        return ref _dense[denseIdx];
    }
    
    public readonly TData Remove(uint index) {
        if (!Has(index)) {
            throw new InvalidOperationException($"element at index {index} does not exist in the sparse set!");
        }
        
        ref var ptr = ref _dense[_sparse[index]];
        var result = ptr;
        _sparse[index] = invalid_data;
        ptr = default;
        return result;
    }
}