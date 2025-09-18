namespace Signals.Core;

public readonly struct ComponentAddedSignal<T>() : ISignal where T : struct, IComponent {
    public readonly Entity Entity;
    public readonly T Value;

    public ComponentAddedSignal(Entity entity, T val) : this() {
        Entity = entity;
        Value = val;
    }
}

public readonly struct ComponentRemovedSignal<T>() : ISignal where T : struct, IComponent {
    public readonly Entity Entity;
    public readonly T Value;

    public ComponentRemovedSignal(Entity entity, T val) : this() {
        Entity = entity;
        Value = val;
    }
}

public readonly struct EntityCreatedSignal() : ISignal {
    public readonly Entity Entity;

    public EntityCreatedSignal(Entity entity) : this() {
        Entity = entity;
    }
}

public readonly struct EntityDestroyedSignal() : ISignal {
    public readonly Entity Entity;

    public EntityDestroyedSignal(Entity entity) : this() {
        Entity = entity;
    }
}