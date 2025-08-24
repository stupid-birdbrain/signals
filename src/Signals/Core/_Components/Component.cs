namespace Signals.Core;

public readonly struct Component {
    public static readonly Component Invalid = default;

    public readonly uint Id;
    public readonly bool IsValid => Id != 0;

    internal Component(uint id) => Id = id;

    public override bool Equals(object? obj) => obj is Component other && Equals(other);
    public bool Equals(Component other) => Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(Component left, Component right) => left.Equals(right);
    public static bool operator !=(Component left, Component right) => !left.Equals(right);
}