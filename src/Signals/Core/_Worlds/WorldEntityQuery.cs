using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Numerics;
using Signals;

namespace Signals.Core;

/// <summary>
///     Defines a query that finds worlds containing entities matching a specific criteria (components).
/// </summary>
public readonly struct WorldEntityQuery {
    private readonly BitArray<ulong> _requiredEntityComponents;
    private readonly BitArray<ulong> _excludedEntityComponents;

    public WorldEntityQuery() {
        _requiredEntityComponents = new BitArray<ulong>();
        _excludedEntityComponents = new BitArray<ulong>();
    }

    internal WorldEntityQuery(BitArray<ulong> requiredMask, BitArray<ulong> excludedMask) {
        _requiredEntityComponents = requiredMask;
        _excludedEntityComponents = excludedMask;
    }

    public WorldEntityQuery With<T>() where T : struct, IComponent {
        uint componentId = Components.GetComponentIndex<T>();
        if (componentId == 0) {
            throw new InvalidOperationException($"component type {typeof(T).Name} is not registered.");
        }

        var newRequired = _requiredEntityComponents.Clone((int)componentId);

        return new WorldEntityQuery(newRequired, _excludedEntityComponents);
    }

    public WorldEntityQuery Without<T>() where T : struct, IComponent {
        uint componentId = Components.GetComponentIndex<T>();
        if (componentId == 0) {
            throw new InvalidOperationException($"component type {typeof(T).Name} is not registered.");
        }

        var newExcluded = _excludedEntityComponents.Clone((int)componentId);

        return new WorldEntityQuery(_requiredEntityComponents, newExcluded);
    }

    public Iterator Iterate() {
        return new Iterator(this);
    }

    public ref struct Iterator {
        private readonly ReadOnlySpan<BitSet<ulong>> _requiredEntityComponentsSpan;
        private readonly ReadOnlySpan<BitSet<ulong>> _excludedEntityComponentsSpan;

        private int _currentWorldIndex;

        private readonly bool _hasRequiredEntityMask;
        private readonly bool _hasExcludedEntityMask;

        public Iterator(WorldEntityQuery query) {
            _requiredEntityComponentsSpan = query._requiredEntityComponents.Array;
            _excludedEntityComponentsSpan = query._excludedEntityComponents.Array;

            _hasRequiredEntityMask = !_requiredEntityComponentsSpan.IsEmpty;
            _hasExcludedEntityMask = !_excludedEntityComponentsSpan.IsEmpty;

            _currentWorldIndex = -1;
        }

        /// <summary> Advances the iterator and returns the next matching world, or null if no more. </summary>
        public World? Next() {
            var requiredSpan = _requiredEntityComponentsSpan;
            var excludedSpan = _excludedEntityComponentsSpan;

            var liveWorldCount = Worlds.WorldCount; 

            while (true) {
                _currentWorldIndex++;

                // check against the current world count
                if (_currentWorldIndex >= liveWorldCount) {
                    return null;
                }
                
                uint worldId = (uint)_currentWorldIndex;

                World currentWorld;
                try {
                    currentWorld = Worlds.GetWorld(worldId);
                } catch (IndexOutOfRangeException) {
                    continue; 
                }

                if (!currentWorld.Valid) {
                    continue;
                }

                var entityQueryForWorld = new Query(worldId, 
                    new BitArray<ulong> { Array = requiredSpan.ToArray() },
                    new BitArray<ulong> { Array = excludedSpan.ToArray() }
                );

                var entityIterator = entityQueryForWorld.Iterate();
                if (entityIterator.Next() is { } _) {
                    return currentWorld;
                }
            }
        }
    }
}