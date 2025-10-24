using System.Runtime.CompilerServices;

namespace Signals.Core;

/// <summary>
///     A lightweight handle to a world.
/// </summary>
public readonly struct World() {
    public readonly uint Index;
    private readonly CreationOptions? _options;

    public bool Valid => Index < Worlds.WorldCount;

    public World(uint index, CreationOptions options) : this() {
        Index = index;
        _options = options;
    }

    public World(uint index) : this() {
        Index = index;
    }

    public Entity Create() => Entities.Create(Index);

    public Query Query() => new(Index);

    public bool Has<T>() where T : struct, IComponent => Components.HasWorldComponent<T>(Index);
    public ref T Get<T>() where T : struct, IComponent => ref Components.GetWorldComponent<T>(Index);
    public ref T Set<T>(in T value) where T : struct, IComponent => ref Components.SetWorldComponent<T>(Index, in value);
    public void Remove<T>() where T : struct, IComponent => Components.RemoveWorldComponent<T>(Index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void Send<T>(in T message) where T : struct, ISignal => Signals.SendMessage(Index, in message);
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public ReadOnlySpan<T> Read<T>() where T : struct, ISignal => Signals.ReadMessages<T>(Index);
}