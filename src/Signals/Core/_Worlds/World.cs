namespace Signals.Core;

public struct World() {
    public uint Index;
    private readonly Worlds.CreationOptions _options;
    
    public World(uint index, Worlds.CreationOptions options) : this() {
        Index = index;
        _options = options;
    }
}