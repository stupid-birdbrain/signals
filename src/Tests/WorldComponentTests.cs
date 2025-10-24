using NUnit.Framework;
using Signals.Core;

namespace Tests;

[TestFixture]
public class WorldComponentTests {
    [SetUp]
    public void Setup() {
        typeof(Worlds)
            .GetField("_worlds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, new List<World>());

        Entities.WorldData = Array.Empty<Entities.UniqueWorldData>();

        Worlds.Initialize();

        typeof(Components)
            .GetField("_componentCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, 0u);

        typeof(Components)
            .GetField("ComponentInfos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, Array.Empty<Components.Info>());
    }

    [Test]
    public void SetGetWorldComponent() {
        var world = Worlds.DefaultWorld;

        world.Set(new Apple());

        Assert.That(world.Has<Apple>());
    }

    [Test]
    public void RemoveWorldComponent() {
        var world = Worlds.DefaultWorld;

        world.Remove<Apple>();

        Assert.That(!world.Has<Apple>());
    }
}