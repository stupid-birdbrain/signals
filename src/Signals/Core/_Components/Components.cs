using JetBrains.Annotations;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Signals.Core;

public interface IComponent;

/// <summary>
///     Central component storage.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature | ImplicitUseKindFlags.Access)]
internal partial class Components {
    internal class EntityComponentData<T> where T : struct, IComponent {
        internal struct UniqueWorldEntityComponentData() {
            public SparseSet<T> SparseSet = new();
        }
        
        internal static UniqueWorldEntityComponentData[] WorldEntityData = [];
        
        public static ComponentHandle Handle = ComponentHandle.Invalid;

        static EntityComponentData() {
            Handle = RegisterComponentType<T>();
        }
    }
    
    internal struct Info {
        public required Type Type;
        public required Action<Entity, object> AddComponentFromObject; 
    }
    
    private static uint _componentCount;
    internal static uint ComponentMasksPerEntity = 1;
    
    private static Info[] _components = Array.Empty<Info>();
    private static readonly Dictionary<Type, ComponentHandle> _componentByType = new();
    private static readonly Dictionary<string, ComponentHandle> _componentByName = new();
    
    private static ComponentHandle RegisterComponentType<T>() where T : struct, IComponent {
        lock (_componentByType) {
            if(_componentByType.TryGetValue(typeof(T), out var existingHandle)) {
                return existingHandle;
            }

            var handle = new ComponentHandle(++_componentCount);
            if (handle.Id >= _components.Length) {
                Array.Resize(ref _components, (int)BitOperations.RoundUpToPowerOf2(handle.Id + 1));
            }

            _components[handle.Id] = new() {
                Type = typeof(T),
                AddComponentFromObject = (entity, value) => SetComponent(entity, (T)value)
            };
            
            _componentByType[typeof(T)] = handle;
            _componentByName[typeof(T).Name] = handle;

            uint newMasksPerEntityNeeded = (handle.Id / (uint)BitSet<ulong>.BitSize) + 1;
            if (newMasksPerEntityNeeded > ComponentMasksPerEntity) {
                ComponentMasksPerEntity = newMasksPerEntityNeeded;
            }
            
            return handle;
        }
    }
    
    internal static void RegisterComponent(Type type)
        => RuntimeHelpers.RunClassConstructor(typeof(EntityComponentData<>).MakeGenericType(type).TypeHandle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ComponentHandle GetComponentHandle<T>() where T : struct, IComponent
        => EntityComponentData<T>.Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint GetComponentIndex<T>() where T : struct, IComponent
        => EntityComponentData<T>.Handle.Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void EnsureComponentWorldDataCapacity<T>(uint worldId) where T : struct, IComponent {
        ref var worldComponentDatas = ref EntityComponentData<T>.WorldEntityData;
        if (worldId >= worldComponentDatas.Length) {
            int oldLength = worldComponentDatas.Length;
            int newLength = (int)BitOperations.RoundUpToPowerOf2(worldId + 1);
            Array.Resize(ref worldComponentDatas, newLength);
            
            for (int i = oldLength; i < newLength; i++) {
                worldComponentDatas[i] = new EntityComponentData<T>.UniqueWorldEntityComponentData(); 
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void EnsureWorldEntityComponentMaskCapacity(uint worldId, uint entityIndex) {
        Entities.EnsureWorldCapacity(worldId); 
        ref var worldData = ref Entities.WorldData[worldId];

        uint requiredFlatMasksLength = (entityIndex + 1) * ComponentMasksPerEntity;
        if (requiredFlatMasksLength > worldData.EntityComponentMasks.Length) {
            int oldLength = worldData.EntityComponentMasks.Length;
            int newLength = (int)requiredFlatMasksLength;
            
            if (oldLength == 0) {
                newLength = (int)Math.Max(1, requiredFlatMasksLength); 
            } else {
                while (newLength < requiredFlatMasksLength) {
                    newLength *= 2;
                }
            }
            Array.Resize(ref worldData.EntityComponentMasks, newLength);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<BitSet<ulong>> GetEntityComponentMaskSpan(uint worldId, uint entityIndex) {
        ref var worldData = ref Entities.WorldData[worldId];
        var startOffset = (int)(entityIndex * ComponentMasksPerEntity);
        return new Span<BitSet<ulong>>(worldData.EntityComponentMasks, startOffset, (int)ComponentMasksPerEntity);
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasComponent<T>(Entity entity) where T : struct, IComponent {
        if (!entity.Valid) {
            return false;
        }
        
        var componentId = GetComponentIndex<T>();
        if (componentId == 0) return false;
        
        var entityComponentMaskSpan = GetEntityComponentMaskSpan(entity.WorldIndex, entity.Index);

        var (div, rem) = Math.DivRem((int)componentId, BitSet<ulong>.BitSize);
        return div < entityComponentMaskSpan.Length && entityComponentMaskSpan[div].Get((int)rem);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T GetComponent<T>(Entity entity) where T : struct, IComponent {
        if (!HasComponent<T>(entity)) {
            throw new InvalidOperationException($"entity does not have component {typeof(T).Name} or is invalid.");
        }
        
        EnsureComponentWorldDataCapacity<T>(entity.WorldIndex);
        ref var sparseSet = ref EntityComponentData<T>.WorldEntityData[entity.WorldIndex].SparseSet;

        return ref sparseSet.Get(entity.Index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T SetComponent<T>(Entity entity, in T value) where T : struct, IComponent {
        if (!entity.Valid) {
            throw new InvalidOperationException($"cannot add component to invalid entity {entity}.");
        }

        var componentId = GetComponentIndex<T>();
        EnsureComponentWorldDataCapacity<T>(entity.WorldIndex);
        EnsureWorldEntityComponentMaskCapacity(entity.WorldIndex, entity.Index);

        var entityComponentMaskSpan = GetEntityComponentMaskSpan(entity.WorldIndex, entity.Index);
        var (div, rem) = Math.DivRem((int)componentId, BitSet<ulong>.BitSize);
        
        if (div >= entityComponentMaskSpan.Length) { 
             throw new IndexOutOfRangeException($"component {componentId} is too large for mask. mask length: {entityComponentMaskSpan.Length}");
        }
        entityComponentMaskSpan[div].Set((int)rem); 

        ref var sparseSet = ref EntityComponentData<T>.WorldEntityData[entity.WorldIndex].SparseSet;
        return ref sparseSet.Add(entity.Index, in value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RemoveComponent<T>(Entity entity) where T : struct, IComponent {
        if (!entity.Valid) {
            return;
        }

        var componentId = GetComponentIndex<T>();
        if (componentId == 0) return;

        if (entity.WorldIndex >= EntityComponentData<T>.WorldEntityData.Length) return;
        ref var sparseSet = ref EntityComponentData<T>.WorldEntityData[entity.WorldIndex].SparseSet;
        if (!sparseSet.Has(entity.Index)) return; 

        var entityComponentMaskSpan = GetEntityComponentMaskSpan(entity.WorldIndex, entity.Index);
        var (div, rem) = Math.DivRem((int)componentId, BitSet<ulong>.BitSize);
        if (div < entityComponentMaskSpan.Length) {
            entityComponentMaskSpan[div].Unset((int)rem);
        }

        sparseSet.Remove(entity.Index);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasAllComponents(Entity entity, ReadOnlySpan<BitSet<ulong>> requiredMaskSpan) {
        if (!entity.Valid) return false;

        var entityComponentMaskSpan = GetEntityComponentMaskSpan(entity.WorldIndex, entity.Index);

        int minLength = Math.Min(entityComponentMaskSpan.Length, requiredMaskSpan.Length);

        for (int i = 0; i < minLength; i++) {
            if ((requiredMaskSpan[i].Value & entityComponentMaskSpan[i].Value) != requiredMaskSpan[i].Value) {
                return false;
            }
        }
        for (int i = minLength; i < requiredMaskSpan.Length; i++) {
            if (!requiredMaskSpan[i].IsZero) {
                return false;
            }
        }
        return true;
    }
    
    internal static bool HasAnyComponents(Entity entity, ReadOnlySpan<BitSet<ulong>> queryMaskSpan) {
        if (!entity.Valid) return false;
    
        var entityComponentMaskSpan = GetEntityComponentMaskSpan(entity.WorldIndex, entity.Index);
        
        int minLength = Math.Min(entityComponentMaskSpan.Length, queryMaskSpan.Length);
    
        for (int i = 0; i < minLength; i++) {
            if ((queryMaskSpan[i].Value & entityComponentMaskSpan[i].Value) != 0) {
                return true;
            }
        }
        return false;
    }
}