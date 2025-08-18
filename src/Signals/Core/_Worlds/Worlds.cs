namespace Signals.Core;

public partial class Worlds {
    
    public static World CreateWorld(CreationOptions? options = null) {
        var creationOptions = options ?? new CreationOptions { SupportsMultithreading = false};
        var world = new World();

        return world;
    }
    
    public struct CreationOptions() {
        public required bool SupportsMultithreading = false;
    }
}