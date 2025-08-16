using System.Runtime.InteropServices;

namespace Signals.Core;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Entity {
    public readonly uint Index;
    public readonly uint Generation;
    
    public Entity(uint index, uint generation) {
        Index = index;
        Generation = generation;
    }
}