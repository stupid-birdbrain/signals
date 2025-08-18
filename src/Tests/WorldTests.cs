using NUnit.Framework;
using Signals.Core;

namespace Tests;

[TestFixture]
public class WorldTests {
    [SetUp]
    public void Setup() {
        typeof(Worlds)
            .GetField("_worlds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, new List<World>());
    }
    
    [Test]
    public void WorldCreation_IncrementsWorldCount() {
        var world = Worlds.CreateWorld();
        
        Assert.That(Worlds.AllWorlds.Length, Is.EqualTo(1));
        Assert.That(world.Index, Is.EqualTo(0));
        
        var world2 = Worlds.CreateWorld();
        
        Assert.That(Worlds.AllWorlds.Length, Is.EqualTo(2));
        Assert.That(world2.Index, Is.EqualTo(1));
    }
}