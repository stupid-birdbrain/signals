﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Numerics;
using Signals;
using Standard;

namespace Signals.Core;

/// <summary>
///     Defines a query that finds worlds containing entities matching a specific criteria (components).
/// </summary>
public readonly struct WorldEntityQuery
{ internal readonly BitmaskArray256 _requiredEntityComponents;
    internal readonly BitmaskArray256 _excludedEntityComponents;

    public WorldEntityQuery()
    { _requiredEntityComponents = new BitmaskArray256();
        _excludedEntityComponents = new BitmaskArray256();
    }

    internal WorldEntityQuery(
        BitmaskArray256 requiredMask,
        BitmaskArray256 excludedMask
    )
    {
        _requiredEntityComponents = requiredMask;
        _excludedEntityComponents = excludedMask;
    }

    public WorldEntityQuery With<T>() where T : struct, IComponent {
        uint componentId = Components.GetComponentIndex<T>();
        var newRequired = _requiredEntityComponents.CloneAndSet((int)componentId);
        return new WorldEntityQuery(newRequired, _excludedEntityComponents);
    }

    public WorldEntityQuery Without<T>() where T : struct, IComponent {
        uint componentId = Components.GetComponentIndex<T>();
        var newExcluded = _excludedEntityComponents.CloneAndSet((int)componentId);
        return new WorldEntityQuery(_requiredEntityComponents, newExcluded);
    }

    public Iterator Iterate() {
        return new Iterator(this);
    }

    public ref struct Iterator {
        private readonly WorldEntityQuery _query;
        private int _currentWorldIndex;

        public Iterator(WorldEntityQuery query) {
            _query = query;
            _currentWorldIndex = -1;
        }

        public World? Next() {
            var liveWorldCount = Worlds.WorldCount;

            while (++_currentWorldIndex < liveWorldCount) {
                uint worldId = (uint)_currentWorldIndex;
                World currentWorld = Worlds.GetWorld(worldId);

                if (!currentWorld.Valid || currentWorld.Index == Worlds.PrefabWorld.Index) {
                    continue;
                }

                var entityQueryForWorld = new Query(worldId,
                    _query._requiredEntityComponents,
                    _query._excludedEntityComponents);

                var entityIterator = entityQueryForWorld.Iterate();
                if (entityIterator.Next() is { } _) {
                    return currentWorld;
                }
            }

            return null;
        }
    }
}