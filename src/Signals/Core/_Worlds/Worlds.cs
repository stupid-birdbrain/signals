namespace Signals.Core;

public struct CreationOptions() {
    public required bool SupportsMultithreading = false;
}

internal partial class Worlds {
    private static List<World> _worlds = [];
    public static int WorldCount => _worlds.Count;
    public static Span<World> AllWorlds => _worlds.ToArray().AsSpan();

    public static World DefaultWorld { get; internal set; }
    public static World PrefabWorld { get; internal set; }

    public static void Initialize() {
        DefaultWorld = CreateWorld();
        PrefabWorld = CreateWorld();
    }
    
    public static World CreateWorld(CreationOptions? options = null) {
        var creationOptions = options ?? new CreationOptions { SupportsMultithreading = false };
        var world = new World((uint)_worlds.Count, creationOptions);
        
        _worlds.Add(world);
        
        return world;
    }

    public static World GetWorld(uint index) {
        if (index >= _worlds.Count) {
            throw new IndexOutOfRangeException($"world with index {index} does not exist!");
        }
        var world = _worlds[(int)index];
        
        return world;
    }
}