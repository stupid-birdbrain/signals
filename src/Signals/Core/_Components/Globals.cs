namespace Signals.Core;

public static class Globals {
    public static bool Has<T>() where T : struct, IComponent => Components.HasGlobal<T>();
    public static ref T Get<T>() where T : struct, IComponent => ref Components.GetGlobal<T>();
    public static ref T Set<T>(T component) where T : struct, IComponent => ref Components.SetGlobal(component);
    public static void Remove<T>() where T : struct, IComponent => Components.RemoveGlobal<T>();
}