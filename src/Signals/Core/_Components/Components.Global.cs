namespace Signals.Core;

partial class Components {
    public class GlobalComponentData<T> where T : struct, IComponent {
        public static T Component;
        public static bool Has;
    }
    
    public static ref T SetGlobal<T>(T component) where T : struct, IComponent {
        GlobalComponentData<T>.Has = true;
        GlobalComponentData<T>.Component = component;

        return ref GlobalComponentData<T>.Component;
    }

    public static void RemoveGlobal<T>() where T : struct, IComponent {
        GlobalComponentData<T>.Has = false;
        GlobalComponentData<T>.Component = default;
    }

    public static ref T GetGlobal<T>() where T : struct, IComponent => ref GlobalComponentData<T>.Component;
    public static bool HasGlobal<T>() where T : struct, IComponent => GlobalComponentData<T>.Has;
}