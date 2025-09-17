using System.Numerics;
using System.Runtime.CompilerServices;

namespace Signals.Core;

public unsafe readonly struct Query(uint worldId) {
    private readonly uint _worldId = worldId;
    internal readonly BitmaskArray256 _requiredComponents = new();
    internal readonly BitmaskArray256 _excludedComponents = new();

    internal Query(
        uint worldId,
        BitmaskArray256 requiredMask,
        BitmaskArray256 excludedMask
    )
        : this(worldId)
    {
        _worldId = worldId;
        _requiredComponents = requiredMask;
        _excludedComponents = excludedMask;
    }

    public Query With<T>() where T : struct, IComponent {
        uint componentId = Components.GetComponentIndex<T>();
        var newRequired = _requiredComponents.CloneAndSet((int)componentId);

        return new Query(_worldId, newRequired, _excludedComponents);
    }

    public Query Without<T>() where T : struct, IComponent => new Query(_worldId,
        _requiredComponents,
        _excludedComponents.CloneAndSet((int)Components.GetComponentIndex<T>()));

    public Iterator Iterate() => new Iterator(this);

#pragma warning disable CS8500
    public ref struct Iterator {
        private readonly uint _worldId;
        private readonly ReadOnlySpan<Bitset256> _requiredComponentsSpan;
        private readonly ReadOnlySpan<Bitset256> _excludedComponentsSpan;

        private readonly Entities.UniqueWorldData* _worldData;

        private int _currentPresenceMaskArrayIndex;
        private BitSet<ulong> _currentPresenceBitSet;

        public Iterator(Query query) {
            _worldId = query._worldId;
            _requiredComponentsSpan = query._requiredComponents.AsSpan();
            _excludedComponentsSpan = query._excludedComponents.AsSpan();

            fixed (Entities.UniqueWorldData* ptr = &Entities.WorldData[_worldId])
                _worldData = ptr;

            _currentPresenceMaskArrayIndex = -1;
            _currentPresenceBitSet = BitSet<ulong>.Zero;
        }

        public Entity? Next() {
            if (_worldData == null || _worldId >= Entities.WorldData.Length) {
                return null;
            }

            ref var worldData = ref *_worldData;

            var liveEntityPresenceMasksArray = worldData.EntityPresenceMasks.Array;
            var liveEntityGenerationsArray = worldData.EntityGenerations;

            var liveWorldGenerationsLength = (uint)liveEntityGenerationsArray.Length;

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

                uint entityId = (uint)(_currentPresenceMaskArrayIndex *
                    BitSet<ulong>.BitSize +
                    bitInCurrentMask);

                if (entityId >= liveWorldGenerationsLength) {
                    continue;
                }

                uint entityGeneration = liveEntityGenerationsArray[entityId];
                var candidateEntity = new Entity(entityId, entityGeneration, _worldId);

                if (!candidateEntity.Valid) {
                    continue;
                }

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
    }
#pragma warning restore CS8500
}