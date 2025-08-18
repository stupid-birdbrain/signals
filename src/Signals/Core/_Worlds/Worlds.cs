namespace Signals.Core;

public partial class Worlds {
    public struct CreationOptions() {
        public required bool SupportsMultithreading = false;
    }
    
    private static List<World> _worlds = [];
    public static Span<World> AllWorlds => _worlds.ToArray().AsSpan();
    
    public static World CreateWorld(CreationOptions? options = null) {
        var creationOptions = options ?? new CreationOptions { SupportsMultithreading = false };
        var world = new World((uint)_worlds.Count, creationOptions);

        _worlds.Add(world);
        
        return world;
    }
}