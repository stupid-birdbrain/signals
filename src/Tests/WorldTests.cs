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
        
        Worlds.Initialize();
    }
    
    [Test]
    public void WorldCreationIncrementsWorldCount() {
        var world2 = Worlds.CreateWorld();
        
        Assert.That(Worlds.AllWorlds.Length, Is.EqualTo(3));
        Assert.That(world2.Index, Is.EqualTo(2));
    }
    
    [Test]
    public void GetWorld() {
        var world = Worlds.CreateWorld();
        var worldref = Worlds.GetWorld(0);
        
        Assert.That(worldref.Index, Is.EqualTo(0));
    }
    
    [Test]
    public void GetInvalidWorld() {
        Assert.Throws<IndexOutOfRangeException>(() => Worlds.GetWorld(100));
    }
}