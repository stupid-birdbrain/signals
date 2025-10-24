namespace Signals.Core;

public readonly struct WorldQuery {
    private readonly BitmaskArray256 _requiredComponents;
    private readonly BitmaskArray256 _excludedComponents;

    public WorldQuery() {
        _requiredComponents = new BitmaskArray256();
        _excludedComponents = new BitmaskArray256();
    }

    private WorldQuery(BitmaskArray256 requiredMask, BitmaskArray256 excludedMask) {
        _requiredComponents = requiredMask;
        _excludedComponents = excludedMask;
    }

    public WorldQuery With<T>() where T : struct, IComponent {
        uint componentId = Components.GetComponentIndex<T>();
        if(componentId == 0) {
            throw new InvalidOperationException($"component type {typeof(T).Name} is not registered.");
        }

        var newRequired = _requiredComponents.CloneAndSet((int)componentId);

        return new WorldQuery(newRequired, _excludedComponents);
    }

    public WorldQuery Without<T>() where T : struct, IComponent {
        uint componentId = Components.GetComponentIndex<T>();
        if(componentId == 0) {
            throw new InvalidOperationException($"component type {typeof(T).Name} is not registered.");
        }

        var newExcluded = _excludedComponents.CloneAndSet((int)componentId);

        return new WorldQuery(_requiredComponents, newExcluded);
    }

    public Iterator Iterate() {
        Entities.EnsureWorldCapacity((uint)Worlds.WorldCount);
        return new Iterator(this);
    }

    public ref struct Iterator {
        private readonly ReadOnlySpan<Bitset256> _requiredComponentsSpan;
        private readonly ReadOnlySpan<Bitset256> _excludedComponentsSpan;

        private int _currentWorldIndex;

        private readonly bool _hasRequiredMask;
        private readonly bool _hasExcludedMask;

        public Iterator(WorldQuery query) {
            _requiredComponentsSpan = query._requiredComponents.AsSpan();
            _excludedComponentsSpan = query._excludedComponents.AsSpan();

            _hasRequiredMask = !_requiredComponentsSpan.IsEmpty;
            _hasExcludedMask = !_excludedComponentsSpan.IsEmpty;

            _currentWorldIndex = -1;
        }

        public World? Next() {
            var requiredSpan = _requiredComponentsSpan;
            var excludedSpan = _excludedComponentsSpan;
            var liveWorldCount = Worlds.WorldCount;

            while(true) {
                _currentWorldIndex++;
                // check against the current world count
                if(_currentWorldIndex >= liveWorldCount) {
                    return null;
                }

                uint worldId = (uint)_currentWorldIndex;

                /*ensure UniqueWorldData slot for this world index in entity storage exists, and initialized before we try to access it*/
                World currentWorld;
                try {
                    currentWorld = Worlds.GetWorld(worldId);
                }
                catch(IndexOutOfRangeException) {
                    /*this should never happen, but technically could happen if world count decreases (should never happen)*/
                    continue;
                }

                if(!currentWorld.Valid) {
                    continue;
                }

                if(_hasRequiredMask) {
                    if(!Components.HasAllWorldComponents(currentWorld.Index, requiredSpan)) {
                        continue;
                    }
                }

                if(_hasExcludedMask) {
                    if(Components.HasAnyWorldComponents(currentWorld.Index, excludedSpan)) {
                        continue;
                    }
                }

                return currentWorld;
            }
        }
    }
}