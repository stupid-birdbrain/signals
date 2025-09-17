using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using Raylib_cs;
using Signals.Core;
using static Raylib_cs.Raylib;
using static Raylib_cs.Raymath;

namespace Sample;

internal struct Position : IComponent {
    public Vector2 Value;
}

internal struct Velocity : IComponent {
    public Vector2 Value;
}

public struct Transform2D : IComponent {
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;
}

internal struct TestComponent : IComponent
{
    public int Data;
}

internal struct Marker : IComponent;

public static class Program {
    public static void Main() {
        SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        InitWindow(
            800,
            450,
            "Signals Sample"
        );
        
        SetTargetFPS(9000);
        
        
        Worlds.Initialize();
        
        var world = Worlds.DefaultWorld;
        
        // Components.RegisterComponent(typeof(Position));
        // Components.RegisterComponent(typeof(Velocity));
        // Components.RegisterComponent(typeof(Transform2D));
        
        PrefabLoading.LoadAllPrefabs(Assembly.GetExecutingAssembly());

        // Prefabs.TryGetPrefab("TestPrefab", out var prefab);
        //
        // var ent = Prefabs.Instantiate(prefab, world.Index);
        //
        //
        // var data = ent.Get<TestComponent>().Data;
        // var velo = ent.Get<Velocity>().Value;
        //
        // Console.WriteLine(data);
        // Console.WriteLine(velo);
        
        
        
        for (int i = 0; i < 50 ; i++) {
            var entity = world.Create();
            entity.Set(new Transform2D() { Position = new Vector2(Random.Shared.Next(0, 300), Random.Shared.Next(0, 300)) });
            entity.Set(new Velocity() { Value = new Vector2(Random.Shared.Next(-5, 5), Random.Shared.Next(-5, 5)) });
        }
        
        var srcEntity = world.Create();
        srcEntity.Set(new Position() { Value = new Vector2(Random.Shared.Next(0, 300), Random.Shared.Next(0, 300)) });
        srcEntity.Set(new Velocity() { Value = new Vector2(Random.Shared.Next(-5, 5), Random.Shared.Next(-5, 5)) });
        srcEntity.Set(new Marker());

        // var asd = Entities.WorldData[Worlds.DefaultWorld.Index].EntityComponentMasks[srcEntity.Index];
        //
        // Console.WriteLine(asd.ToString());

        while (!WindowShouldClose()) {

            var query = world.Query().With<Transform2D>().With<Velocity>().Iterate();
            while (query.Next() is { } entity) {
                ref var pos = ref entity.Get<Transform2D>();
                ref var vel = ref entity.Get<Velocity>();
                
                pos.Position += vel.Value * GetFrameTime();
            }
            
            #region drawing
            BeginDrawing();
            ClearBackground(Color.DarkGray);
            
            var worldQuery = new WorldEntityQuery().With<Transform2D>().Iterate();

            while (worldQuery.Next() is { } wld) {
                //Console.WriteLine(wld.Index);
                
                var entityquery = wld.Query().With<Transform2D>().Iterate();
                while (entityquery.Next() is { } entity) {
                    ref var pos = ref entity.Get<Transform2D>();
                    
                    DrawRectangleV(pos.Position, new Vector2(10, 10), Color.White);
                }
            }
            
            DrawFPS(0, 0);
            
            EndDrawing();
            #endregion
        }
        
        CloseWindow();
    }
}