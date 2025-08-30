using System.Numerics;
using Raylib_cs;
using Signals.Core;
using static Raylib_cs.Raylib;
using static Raylib_cs.Raymath;

namespace Sample;

internal struct Position : IComponent {
    public Vector2 Value;
}

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

        for (int i = 0; i < 50 ; i++) {
            var entity = world.Create();
            entity.Set(new Position() { Value = new Vector2(Random.Shared.Next(0, 300), Random.Shared.Next(0, 300)) });
        }

        while (!WindowShouldClose()) {
            #region drawing
            BeginDrawing();
            ClearBackground(Color.DarkGray);
            
            var query = new WorldEntityQuery().With<Position>().Iterate();

            while (query.Next() is { } wld) {
                var entityquery = wld.Query().With<Position>().Iterate();
                while (entityquery.Next() is { } entity) {
                    ref var pos = ref entity.Get<Position>();
                    
                    DrawRectangleV(pos.Value, new Vector2(10, 10), Color.White);
                }
            }
            
            DrawFPS(0, 0);
            
            EndDrawing();
            #endregion
        }
        
        CloseWindow();
    }
}