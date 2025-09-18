using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Signals.Core;

public interface IComponent;

/// <summary>
///     Central component storage.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature | ImplicitUseKindFlags.Access)]
public partial class Components {
    internal class EntityComponentData<T> where T : struct, IComponent {
        internal struct UniqueWorldEntityComponentData() {
            public SparseSet<T> SparseSet = new();
        }
        
        internal static UniqueWorldEntityComponentData[] WorldEntityData = [];
        
        public static Handle Handle = Handle.Invalid;

        static EntityComponentData() {
            Handle = RegisterComponentType<T>();
        }
    }
    
    /// <summary>
    ///     A handle to a registered component type.
    /// </summary>
    internal readonly struct Handle {
        public static readonly Handle Invalid = default;

        public readonly uint Id;
        public readonly bool IsValid => Id != 0;

        internal Handle(uint id) => Id = id;
    
        public override bool Equals(object? obj) => obj is Handle other && Equals(other);
        public bool Equals(Handle other) => Id == other.Id;
        public override int GetHashCode() => Id.GetHashCode();
        public static bool operator ==(Handle left, Handle right) => left.Equals(right);
        public static bool operator !=(Handle left, Handle right) => !left.Equals(right);
    }
    
    /// <summary>
    ///     Simple component metadata.
    /// </summary>
    internal struct Info {
        public required Type Type;
        public required Action<Entity, object> AddComponentFromObject; 
        public required Func<uint, bool> HasWorldComponentFunc;
    }
    
    internal static uint _componentCount;
    internal static uint ComponentMasksPerEntity = 1;
    
    internal static Info[] _components = Array.Empty<Info>();
    
    private static Dictionary<Type, Handle> _componentByType = new();
    private static Dictionary<string, Handle> _componentByName = new();
    
    private static Dictionary<string, Handle> _componentNameToHandle = new();
    private static Dictionary<Handle, Type> _componentHandleToType = new();
    
    private static Handle RegisterComponentType<T>() where T : struct, IComponent {
        lock (_componentByType) {
            if (_componentByType.TryGetValue(typeof(T), out var existingHandle)) {
                return existingHandle;
            }

            var handle = new Handle(++_componentCount);
            if (handle.Id >= _components.Length) {
                Array.Resize(ref _components, (int)BitOperations.RoundUpToPowerOf2(handle.Id + 1));
            }

            _components[handle.Id] = new() {
                Type = typeof(T),
                AddComponentFromObject = (entity, value) => SetComponent(entity, (T)value),
                HasWorldComponentFunc = (worldId) => HasWorldComponent<T>(worldId)
            };

            _componentByType[typeof(T)] = handle;
            _componentNameToHandle[typeof(T).Name] = handle;
            _componentHandleToType[handle] = typeof(T);

            uint newMasksPerEntityNeeded = (handle.Id / (uint)BitSet<ulong>.BitSize) + 1;
            if (newMasksPerEntityNeeded > ComponentMasksPerEntity) {
                ComponentMasksPerEntity = newMasksPerEntityNeeded;
            }

            return handle;
        }
    }
    
    public static void RegisterComponent(Type type)
        => RuntimeHelpers.RunClassConstructor(typeof(EntityComponentData<>).MakeGenericType(type).TypeHandle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Handle GetComponentHandle<T>() where T : struct, IComponent
        => EntityComponentData<T>.Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint GetComponentIndex<T>() where T : struct, IComponent
        => EntityComponentData<T>.Handle.Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void EnsureComponentWorldDataCapacity<T>(uint worldIndex) where T : struct, IComponent {
        ref var worldComponentDatas = ref EntityComponentData<T>.WorldEntityData;
        if (worldIndex >= worldComponentDatas.Length) {
            int oldLength = worldComponentDatas.Length;
            int newLength = (int)BitOperations.RoundUpToPowerOf2(worldIndex + 1);
            Array.Resize(ref worldComponentDatas, newLength);
            
            for (int i = oldLength; i < newLength; i++) {
                worldComponentDatas[i] = new EntityComponentData<T>.UniqueWorldEntityComponentData(); 
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void EnsureWorldEntityComponentMaskCapacity(uint worldIndex, uint entityIndex) {
        Entities.EnsureWorldCapacity(worldIndex); 
        ref var worldData = ref Entities.WorldData[worldIndex];

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
    internal static Span<Bitset256> GetEntityComponentMaskSpan(uint worldIndex, uint entityIndex) {
        ref var worldData = ref Entities.WorldData[worldIndex];
        var startOffset = (int)(entityIndex * ComponentMasksPerEntity);
        return new Span<Bitset256>(worldData.EntityComponentMasks, startOffset,
            (int)ComponentMasksPerEntity);
    }
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasComponent<T>(Entity entity) where T : struct, IComponent {
        if (!entity.Valid) {
            return false;
        }

        var componentId = GetComponentIndex<T>();
        if (componentId == 0) return false;

        var entityComponentMaskSpan =
            GetEntityComponentMaskSpan(entity.WorldIndex, entity.Index);

        var (div, rem) = Math.DivRem((int)componentId, Bitset256.CAPACITY);
        return div < entityComponentMaskSpan.Length &&
               entityComponentMaskSpan[div].IsSet(rem);
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

        Signals.SendMessage(entity.WorldIndex, new ComponentAddedSignal<T>(entity, value));
        
        ref var sparseSet = ref EntityComponentData<T>.WorldEntityData[entity.WorldIndex].SparseSet;
        ref T newVal = ref sparseSet.Add(entity.Index, in value);
        
        Signals.SendMessage(entity.WorldIndex, new ComponentAddedSignal<T>(entity, newVal));
        
        return ref newVal;
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

        var removedComponentValue = sparseSet.Remove(entity.Index);

        var entityComponentMaskSpan = GetEntityComponentMaskSpan(entity.WorldIndex, entity.Index);
        var (div, rem) = Math.DivRem((int)componentId, BitSet<ulong>.BitSize);
        if (div < entityComponentMaskSpan.Length) {
            entityComponentMaskSpan[div].Clear((int)rem);
        }
        
        Signals.SendMessage(entity.WorldIndex, new ComponentRemovedSignal<T>(entity, removedComponentValue));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasAllComponents(Entity entity, ReadOnlySpan<Bitset256> requiredMaskSpan) {
        if (!entity.Valid) return false;

        var entityComponentMaskSpan = GetEntityComponentMaskSpan(entity.WorldIndex, entity.Index);

        int minLength =
            Math.Min(entityComponentMaskSpan.Length, requiredMaskSpan.Length);

        for (int i = 0; i < minLength; i++) {
            if (!entityComponentMaskSpan[i].Contains(requiredMaskSpan[i])) {
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasAnyComponents(Entity entity, ReadOnlySpan<Bitset256> queryMaskSpan) {
        if (!entity.Valid) return false;

        var entityComponentMaskSpan = GetEntityComponentMaskSpan(entity.WorldIndex, entity.Index);

        int minLength = Math.Min(entityComponentMaskSpan.Length, queryMaskSpan.Length);

        for (int i = 0; i < minLength; i++) {
            if (entityComponentMaskSpan[i].AndAny(queryMaskSpan[i])) {
                return true;
            }
        }
        return false;
    }
    
    internal static Handle GetComponentHandle(Type componentType) {
        lock (_componentByType) {
            if (_componentByType.TryGetValue(componentType, out var handle)) {
                return handle;
            }
            throw new ArgumentException($"Component type '{componentType.Name}' not found.");
        }
    }
    
    internal static bool TryGetComponentHandleFromName(string componentName, [NotNullWhen(true)] out Handle handle) => _componentNameToHandle.TryGetValue(componentName, out handle);
    
    internal static Type GetComponentType(Handle handle) {
        if (_componentHandleToType.TryGetValue(handle, out var type)) {
            return type;
        }
        throw new ArgumentException($"no component type registered for handle index {handle.Id}.");
    }
    
    internal static Type GetComponentTypeFromName(string componentName) {
        if (TryGetComponentHandleFromName(componentName, out var handle)) {
            return GetComponentType(handle);
        }
        throw new KeyNotFoundException($"No component type registered with name '{componentName}'.");
    }
    
    internal static void AddComponentByHandle(Entity entity, Handle handle, object value) {
        if (!entity.Valid) {
            throw new InvalidOperationException($"cant add component to invalid entity {entity}.");
        }
        if (!handle.IsValid) {
            throw new ArgumentException("invalid component handle.", nameof(handle));
        }
        var componentInfo = _components[handle.Id];
        componentInfo.AddComponentFromObject(entity, value);
    }
}