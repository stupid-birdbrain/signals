namespace Signals.Core;

public interface IComponent;

internal partial class Components {
    internal class EntityComponentData<T> where T : struct, IComponent {
        internal struct UniqueWorldEntityComponentData() {
            public static SparseSet<T> SparseSet = new();
        }
    }
}