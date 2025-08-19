namespace Signals.Core;

/// <summary>
///     A lightweight handle to a world.
/// </summary>
public readonly struct World() {
    public readonly uint Index;
    private readonly Worlds.CreationOptions _options;
    
    public World(uint index, Worlds.CreationOptions options) : this() {
        Index = index;
        _options = options;
    }
}