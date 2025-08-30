using System.Numerics;
using System.Runtime.CompilerServices;

namespace Signals.Core;

public unsafe readonly struct Query(uint worldId) {
    private readonly uint _worldId = worldId;
    private readonly BitArray<ulong> _requiredComponents = new();
    private readonly BitArray<ulong> _excludedComponents = new();

    private Query(uint worldId, BitArray<ulong> requiredMask, BitArray<ulong> excludedMask) : this(worldId) {
        _worldId = worldId;
        _requiredComponents = requiredMask;
        _excludedComponents = excludedMask;
    }

    public Query With<T>() where T : struct, IComponent {
        uint componentId = Components.GetComponentIndex<T>();
        var newRequired = _requiredComponents.Clone((int)componentId);

        return new Query(_worldId, newRequired, _excludedComponents);
    }

    public Query Without<T>() where T : struct, IComponent 
        => new Query(_worldId, _requiredComponents, _excludedComponents.Clone((int)Components.GetComponentIndex<T>()));
    
    
    public Query With(in ReadOnlySpan<BitSet<ulong>> maskSpan) {
        if (maskSpan.IsEmpty) return this;
        
        var newRequired = _requiredComponents.CloneMerge(maskSpan);
        return new Query(_worldId, newRequired, _excludedComponents);
    }

    public Query Without(in ReadOnlySpan<BitSet<ulong>> maskSpan) {
        if (maskSpan.IsEmpty) return this;
        
        var newExcluded = _excludedComponents.CloneMerge(maskSpan);
        return new Query(_worldId, _requiredComponents, newExcluded);
    }

    public Iterator Iterate() {
        Entities.EnsureWorldCapacity(_worldId); 
        return new Iterator(this);
    }
    
#pragma warning disable CS8500
    public ref struct Iterator {
        private readonly uint _worldId;
        private readonly ReadOnlySpan<BitSet<ulong>> _requiredComponentsSpan;
        private readonly ReadOnlySpan<BitSet<ulong>> _excludedComponentsSpan;
        
        private Entities.UniqueWorldData* _worldData; 

        private int _currentPresenceMaskArrayIndex;
        private BitSet<ulong> _currentPresenceBitSet;

        private readonly uint _componentMasksPerEntityCache; 

        public Iterator(Query query) {
            _worldId = query._worldId;
            _requiredComponentsSpan = query._requiredComponents.Array;
            _excludedComponentsSpan = query._excludedComponents.Array;

            fixed (Entities.UniqueWorldData* ptr = &Entities.WorldData[_worldId])
                _worldData = ptr;

            _componentMasksPerEntityCache = Components.ComponentMasksPerEntity;

            _currentPresenceMaskArrayIndex = -1;
            _currentPresenceBitSet = BitSet<ulong>.Zero;
        }

        public Entity? Next() {
            if (_worldData == null || _worldId >= Entities.WorldData.Length) { 
                return null;
            }

            ref var worldData = ref *_worldData; 

            var liveEntityPresenceMasksArray = worldData.EntityPresenceMasks.Array; 
            var liveAllEntityComponentMasksArray = worldData.EntityComponentMasks;
            var liveEntityGenerationsArray = worldData.EntityGenerations;
            
            var liveWorldGenerationsLength = (uint)liveEntityGenerationsArray.Length;
            var liveExpectedEntityMaskLength = (int)_componentMasksPerEntityCache; 

            var requiredSpan = _requiredComponentsSpan;
            var excludedSpan = _excludedComponentsSpan;

            bool hasRequiredMask = !requiredSpan.IsEmpty;
            bool hasExcludedMask = !excludedSpan.IsEmpty;

            while (true) {
                if (_currentPresenceBitSet.IsZero) {
                    _currentPresenceMaskArrayIndex++;
                    if (_currentPresenceMaskArrayIndex >= liveEntityPresenceMasksArray.Length) {
                        return null; 
                    }
                    _currentPresenceBitSet = liveEntityPresenceMasksArray[_currentPresenceMaskArrayIndex];
                    if (_currentPresenceBitSet.IsZero) {
                        continue;
                    }
                }

                int bitInCurrentMask = _currentPresenceBitSet.TrailingZeroCount();
                _currentPresenceBitSet.Unset(bitInCurrentMask);

                uint entityId = (uint)(_currentPresenceMaskArrayIndex * BitSet<ulong>.BitSize + bitInCurrentMask);

                if (entityId >= liveWorldGenerationsLength) {
                    continue; 
                }
                
                uint entityGeneration = liveEntityGenerationsArray[entityId]; 

                var candidateEntity = new Entity(entityId, entityGeneration, _worldId);
                
                if (!candidateEntity.Valid) {
                    continue; 
                }

                var entityComponentMaskSpan = GetEntityComponentMaskSpanForQuery(
                    liveAllEntityComponentMasksArray, 
                    entityId, 
                    _componentMasksPerEntityCache,    
                    liveExpectedEntityMaskLength      
                );

                if (hasRequiredMask) {
                    if (!Components.HasAllComponents(candidateEntity, requiredSpan)) {
                        continue;
                    }
                }

                if (hasExcludedMask) {
                    if (Components.HasAnyComponents(candidateEntity, excludedSpan)) {
                        continue;
                    }
                }

                return candidateEntity; 
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<BitSet<ulong>> GetEntityComponentMaskSpanForQuery(ReadOnlySpan<BitSet<ulong>> allMasks, uint entityIndex, uint masksPerEntity, int expectedLength) {
            var startOffset = (int)(entityIndex * masksPerEntity);
            if (startOffset + expectedLength > allMasks.Length || allMasks.IsEmpty) {
                 return ReadOnlySpan<BitSet<ulong>>.Empty;
            }
            return allMasks.Slice(startOffset, expectedLength);
        }
    }
#pragma warning restore CS8500
}