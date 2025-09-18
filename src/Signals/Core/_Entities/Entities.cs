using JetBrains.Annotations;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Signals.Core;

/*  potential ideas
    - specified SIMD 256-bit masks and sets for entity presence and component masks rather than a general use structure
 */

/// <summary>
///     Central entity storage, handles entity creation and deletion.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature | ImplicitUseKindFlags.Access)]
internal static partial class Entities {
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal struct UniqueWorldData() {
        public int NextEntityIndex = 0;

        public BitArray<ulong> EntityPresenceMasks = new();
        public BitSet<ulong>[] EntityComponentMasks = Array.Empty<BitSet<ulong>>(); 
        public uint[] EntityGenerations = Array.Empty<uint>();
        public ConcurrentBag<int> FreeEntityIndices = new();
    }

    internal static UniqueWorldData[] WorldData = Array.Empty<UniqueWorldData>();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EnsureWorldCapacity(uint worldId) {
        int oldLength = WorldData.Length;
        if(worldId >= oldLength) {
            int newLength = (int)worldId + 1;
            Array.Resize(ref WorldData, newLength);
            for(int i = oldLength; i < newLength; i++) {
                WorldData[i] = new();
                WorldData[i].EntityComponentMasks = Array.Empty<BitSet<ulong>>(); 
            }
        }
    }
    
    internal static void ResetForTesting() {
        WorldData = Array.Empty<UniqueWorldData>();
    }

    public static Entity Create(uint worldIndex, bool silent = false) {
        uint index;

        EnsureWorldCapacity(worldIndex);
        ref var worldData = ref WorldData[worldIndex];
        index = (uint)(worldData.FreeEntityIndices.TryTake(out int freeIndex)
            ? freeIndex
            : worldData.NextEntityIndex++);

        if(index >= worldData.EntityGenerations.Length) {
            int oldSize = worldData.EntityGenerations.Length;
            int newSize = Math.Max(1, oldSize); // ensure minimum size of 1 if oldSize was 0
            while (newSize <= index)
                newSize *= 2;
            
            Array.Resize(ref worldData.EntityGenerations, newSize);
        }
        
        int requiredPresenceMasksLength = (worldData.EntityGenerations.Length / BitArray<ulong>.BitsPerMask);
        if (worldData.EntityGenerations.Length % BitArray<ulong>.BitsPerMask != 0) {
            requiredPresenceMasksLength++;
        }

        int currentPresenceMasksLength = worldData.EntityPresenceMasks.Array?.Length ?? 0;
        if (requiredPresenceMasksLength > currentPresenceMasksLength) {
            Array.Resize(ref worldData.EntityPresenceMasks.Array, Math.Max(1, requiredPresenceMasksLength));
        }
        
        Components.EnsureWorldEntityComponentMaskCapacity(worldIndex, index);
        var startOffset = (int)(index * Components.ComponentMasksPerEntity);
        for (int i = 0; i < Components.ComponentMasksPerEntity; i++) {
            worldData.EntityComponentMasks[startOffset + i] = global::Signals.BitSet<ulong>.Zero;
        }
        
        uint version = worldData.EntityGenerations[index] + 1;
        if (version == 0)
            version = 1;

        worldData.EntityGenerations[index] = version;
        worldData.EntityPresenceMasks.Set((int)index);
        
        var entity = new Entity(index, worldData.EntityGenerations[index], worldIndex);
        
        if(silent = false) {
            Signals.SendMessage(worldIndex, new EntityCreatedSignal(entity));
        }
        
        return entity;
    }
    
    public static bool Destroy(uint worldIndex, uint entityIndex, bool silent = false) {
        if(worldIndex >= WorldData.Length)
            return false;

        ref var worldData = ref WorldData[worldIndex];

        if(entityIndex >= worldData.EntityGenerations.Length)
            return false;
        
        var entityToDestroy = new Entity(entityIndex, worldData.EntityGenerations[entityIndex], worldIndex);
        
        if (!IsValid(worldIndex, entityToDestroy)) {
            return false;
        }

        if(silent = false) {
            Signals.SendMessage(worldIndex, new EntityDestroyedSignal(entityToDestroy));
        }
        
        var startOffset = (int)(entityIndex * Components.ComponentMasksPerEntity);
        for (int i = 0; i < Components.ComponentMasksPerEntity; i++) {
            worldData.EntityComponentMasks[startOffset + i] = global::Signals.BitSet<ulong>.Zero;
        }

        ref var entityData = ref worldData.EntityGenerations[entityIndex];

        worldData.EntityGenerations[entityIndex] = worldData.EntityGenerations[entityIndex] + 1;
        if (worldData.EntityGenerations[entityIndex] == 0)
            worldData.EntityGenerations[entityIndex] = 1;

        worldData.EntityPresenceMasks.Unset((int)entityIndex);
        worldData.FreeEntityIndices.Add((int)entityIndex);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(uint worldIndex, Entity entity) {
        if(entity.WorldIndex >= WorldData.Length) 
            return false;
        if (entity.WorldIndex != worldIndex) {
            return false; 
        }

        if(entity.Index >= WorldData[entity.WorldIndex].EntityGenerations.Length)
            return false;

        return entity.Generation != 0 && 
               entity.Generation == WorldData[entity.WorldIndex].EntityGenerations[entity.Index] &&
               WorldData[entity.WorldIndex].EntityPresenceMasks.Get((int)entity.Index);
    }
}