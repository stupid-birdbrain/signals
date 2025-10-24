namespace Signals.Core;

public struct CreationOptions() {
    public required bool SupportsMultithreading = false;
}

public class Worlds {
    private static readonly List<World> worlds = [];
    public static int WorldCount => worlds.Count;
    public static Span<World> AllWorlds => worlds.ToArray().AsSpan();

    public static World DefaultWorld { get; internal set; }
    public static World PrefabWorld { get; internal set; }

    public static void Initialize() {
        DefaultWorld = CreateWorld();
        PrefabWorld = CreateWorld();
    }

    public static World CreateWorld(CreationOptions? options = null) {
        var creationOptions = options ?? new CreationOptions { SupportsMultithreading = false };
        var world = new World((uint)worlds.Count, creationOptions);

        worlds.Add(world);

        return world;
    }

    public static World GetWorld(uint index) {
        if(index >= worlds.Count) {
            throw new IndexOutOfRangeException($"world with index {index} does not exist!");
        }
        var world = worlds[(int)index];

        return world;
    }

    public static WorldQuery Query() => new();
    public static WorldEntityQuery QueryEntities(Query sourceQuery) => new WorldEntityQuery(sourceQuery.RequiredComponents, sourceQuery.ExcludedComponents);
}