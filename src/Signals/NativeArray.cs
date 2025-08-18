using System.Runtime.CompilerServices;

namespace Signals;

internal struct NativeArray<T> : IDisposable where T : unmanaged {
    private unsafe T* _array;

    private UIntPtr Size = (UIntPtr)Unsafe.SizeOf<T>();

    public NativeArray(int size) {
        
    }
    
    public void Dispose() {
        
    }
}