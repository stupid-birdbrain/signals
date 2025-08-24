using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Signals.Core;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Entity {
    public readonly uint Index;
    public readonly uint WorldIndex;
    public readonly uint Generation;

    public readonly bool Valid => Entities.IsValid(WorldIndex, this);
    
    public Entity(uint index, uint generation, uint worldIndex) {
        Index = index;
        Generation = generation;
        WorldIndex = worldIndex;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<T>() where T : struct, IComponent => Components.HasComponent<T>(this);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get<T>() where T : struct, IComponent => ref Components.GetComponent<T>(this);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Set<T>(in T value) where T : struct, IComponent => ref Components.SetComponent(this, in value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove<T>() where T : struct, IComponent => Components.RemoveComponent<T>(this);
    
    public readonly bool Destroy() => Entities.Destroy(WorldIndex, Index);
}