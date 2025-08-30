namespace Signals.Core;

/// <summary>
///     A handle to a registered component type.
/// </summary>
internal readonly struct ComponentHandle {
    public static readonly ComponentHandle Invalid = default;

    public readonly uint Id;
    public readonly bool IsValid => Id != 0;

    internal ComponentHandle(uint id) => Id = id;
    
    public override bool Equals(object? obj) => obj is ComponentHandle other && Equals(other);
    public bool Equals(ComponentHandle other) => Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(ComponentHandle left, ComponentHandle right) => left.Equals(right);
    public static bool operator !=(ComponentHandle left, ComponentHandle right) => !left.Equals(right);
}