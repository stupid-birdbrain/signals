using System.Runtime.InteropServices;

namespace Signals.Core;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Entity {
    public readonly uint Index;
    public readonly uint WorldIndex;
    public readonly uint Generation;
    
    public Entity(uint index, uint generation, uint worldIndex) {
        Index = index;
        Generation = generation;
        WorldIndex = worldIndex;
    }
    
    public readonly bool Destroy() => Entities.Destroy(WorldIndex, Index);
}