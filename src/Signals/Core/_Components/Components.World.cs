using System.Numerics;
using System.Runtime.CompilerServices;

namespace Signals.Core;

internal partial class Components {
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
        if (worldIndex >= worldComponentDataArray.Length) {
            int oldLength = worldComponentDataArray.Length; 
            int newLength = (int)BitOperations.RoundUpToPowerOf2(worldIndex + 1);
            Array.Resize(ref worldComponentDataArray, newLength);
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasWorldComponent<T>(uint worldIndex) where T : struct, IComponent {
        if (worldIndex >= WorldComponentData<T>.Data.Length) {
            return false;
        }
        return WorldComponentData<T>.Data[worldIndex].HasComponent;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetWorldComponent<T>(uint worldIndex) where T : struct, IComponent {
        EnsureWorldComponentCapacity<T>(worldIndex);

        if (!WorldComponentData<T>.Data[worldIndex].HasComponent) {
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
        if (worldIndex >= WorldComponentData<T>.Data.Length) {
            return;
        }

        ref var worldComponentData = ref WorldComponentData<T>.Data[worldIndex];
        
        worldComponentData.Component = default;
        worldComponentData.HasComponent = false;
    }
}