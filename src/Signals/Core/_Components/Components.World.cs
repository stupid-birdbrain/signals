using System.Numerics;
using System.Runtime.CompilerServices;

namespace Signals.Core;

partial class Components {
    internal class WorldComponentData<T> where T : struct, IComponent {
        internal struct LocalWorldComponentData {
            public T Component;
            public bool HasComponent;
        }

        internal static LocalWorldComponentData[] Data = [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureWorldComponentCapacity<T>(uint worldIndex) where T : struct, IComponent {
        ref var worldComponentDataArray = ref WorldComponentData<T>.Data;
        if(worldIndex >= worldComponentDataArray.Length) {
            int newLength = (int)BitOperations.RoundUpToPowerOf2(worldIndex + 1);
            Array.Resize(ref worldComponentDataArray, newLength);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasWorldComponent<T>(uint worldIndex) where T : struct, IComponent {
        if(worldIndex >= WorldComponentData<T>.Data.Length) {
            return false;
        }
        return WorldComponentData<T>.Data[worldIndex].HasComponent;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetWorldComponent<T>(uint worldIndex) where T : struct, IComponent {
        EnsureWorldComponentCapacity<T>(worldIndex);

        if(!WorldComponentData<T>.Data[worldIndex].HasComponent) {
            throw new InvalidOperationException($"world {worldIndex} does not have component {typeof(T).Name}.");
        }
        return ref WorldComponentData<T>.Data[worldIndex].Component;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T SetWorldComponent<T>(uint worldIndex, in T value) where T : struct, IComponent {
        EnsureWorldComponentCapacity<T>(worldIndex);

        ref var worldComponentData = ref WorldComponentData<T>.Data[worldIndex];

        worldComponentData.Component = value;
        worldComponentData.HasComponent = true;

        return ref worldComponentData.Component;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveWorldComponent<T>(uint worldIndex) where T : struct, IComponent {
        if(worldIndex >= WorldComponentData<T>.Data.Length) {
            return;
        }

        ref var worldComponentData = ref WorldComponentData<T>.Data[worldIndex];

        worldComponentData.Component = default;
        worldComponentData.HasComponent = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAllWorldComponents(uint worldId, ReadOnlySpan<Bitset256> requiredMaskSpan) {
        if(requiredMaskSpan.IsEmpty) return true;

        for(int i = 0; i < requiredMaskSpan.Length; i++) {
            ref readonly var requiredBitset = ref requiredMaskSpan[i];
            if(requiredBitset.IsZero) continue;

            foreach(int bitIndex in requiredBitset) {
                uint componentId = (uint)(i * Bitset256.CAPACITY + bitIndex);
                if(!HasWorldComponent(componentId, worldId)) {
                    return false;
                }
            }
        }
        return true;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasAnyWorldComponents(uint worldId, ReadOnlySpan<Bitset256> queryMaskSpan) {
        if(queryMaskSpan.IsEmpty) return false;

        //lol
        for(int i = 0; i < queryMaskSpan.Length; i++) {
            ref readonly var queryBitset = ref queryMaskSpan[i];
            if(queryBitset.IsZero) continue;

            foreach(int bitIndex in queryBitset) {
                uint componentId = (uint)(i * Bitset256.CAPACITY + bitIndex);
                if(HasWorldComponent(componentId, worldId)) {
                    return true;
                }
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasWorldComponent(uint componentId, uint worldId) {
        if(componentId == 0 || componentId >= ComponentInfos.Length) {
            return false;
        }
        return ComponentInfos[componentId].HasWorldComponentFunc(worldId);
    }
}