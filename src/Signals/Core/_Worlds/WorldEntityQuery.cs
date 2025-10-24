namespace Signals.Core;

/// <summary>
///     Defines a query that finds worlds containing entities matching a specific criteria (components).
/// </summary>
public readonly struct WorldEntityQuery {
    internal readonly BitmaskArray256 RequiredEntityComponents;
    internal readonly BitmaskArray256 ExcludedEntityComponents;

    public WorldEntityQuery() {
        RequiredEntityComponents = new BitmaskArray256();
        ExcludedEntityComponents = new BitmaskArray256();
    }

    internal WorldEntityQuery(
        BitmaskArray256 requiredMask,
        BitmaskArray256 excludedMask
    ) {
        RequiredEntityComponents = requiredMask;
        ExcludedEntityComponents = excludedMask;
    }

    public WorldEntityQuery With<T>() where T : struct, IComponent {
        uint componentId = Components.GetComponentIndex<T>();
        var newRequired = RequiredEntityComponents.CloneAndSet((int)componentId);
        return new WorldEntityQuery(newRequired, ExcludedEntityComponents);
    }

    public WorldEntityQuery Without<T>() where T : struct, IComponent {
        uint componentId = Components.GetComponentIndex<T>();
        var newExcluded = ExcludedEntityComponents.CloneAndSet((int)componentId);
        return new WorldEntityQuery(RequiredEntityComponents, newExcluded);
    }

    public Iterator Iterate() {
        return new Iterator(this);
    }

    public ref struct Iterator(WorldEntityQuery query) {
        private int _currentWorldIndex = -1;

        public World? Next() {
            var liveWorldCount = Worlds.WorldCount;

            while(++_currentWorldIndex < liveWorldCount) {
                uint worldId = (uint)_currentWorldIndex;
                World currentWorld = Worlds.GetWorld(worldId);

                if(!currentWorld.Valid || currentWorld.Index == Worlds.PrefabWorld.Index) {
                    continue;
                }

                var entityQueryForWorld = new Query(worldId,
                    query.RequiredEntityComponents,
                    query.ExcludedEntityComponents);

                var entityIterator = entityQueryForWorld.Iterate();
                if(entityIterator.Next() is { } _) {
                    return currentWorld;
                }
            }

            return null;
        }
    }
}