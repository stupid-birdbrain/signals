using Signals.Core;
using System.Net;

namespace Signals;

internal struct Marker : IComponent;
internal struct EntityMarker : IComponent;

//test

public class Program {
    public static void Main() {
        Worlds.Initialize();
        var world = Worlds.DefaultWorld;
        
        world.Set(new Marker());

        for(int i = 0; i < 10; i++) {
            var entity = world.Create();
            entity.Set(new EntityMarker());
        }

        var query = new WorldEntityQuery().With<EntityMarker>().Iterate();
        
        while(query.Next() is { } wld) {
            var entityQueryInWorld = new Query(wld.Index).With<EntityMarker>().Iterate();
            while(entityQueryInWorld.Next() is { } entity) {
                Console.WriteLine($"entity index: {entity.Index}");
            }
        }
    }
}