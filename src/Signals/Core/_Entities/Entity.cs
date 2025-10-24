using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Signals.Core;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct Entity(uint index, uint generation, uint worldIndex) {
    public readonly uint Index = index;
    public readonly uint WorldIndex = worldIndex;
    public readonly uint Generation = generation;

    public bool Valid => Entities.IsValid(WorldIndex, this);

    public static readonly Entity Invalid = default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Has<T>() where T : struct, IComponent => Components.HasComponent<T>(this);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public ref T Get<T>() where T : struct, IComponent => ref Components.GetComponent<T>(this);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public ref T Set<T>(in T value) where T : struct, IComponent => ref Components.SetComponent(this, in value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void Remove<T>() where T : struct, IComponent => Components.RemoveComponent<T>(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool HasAny(ReadOnlySpan<Bitset256> span) => Components.HasAnyComponents(this, span);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool HasAll(ReadOnlySpan<Bitset256> span) => Components.HasAllComponents(this, span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool Destroy() => Entities.Destroy(WorldIndex, Index);
}