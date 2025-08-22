using NUnit.Framework;
using Signals.Core;

namespace Tests;

[TestFixture]
public class EntityTests {
    [SetUp]
    public void Setup() {
        typeof(Worlds)
            .GetField("_worlds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, new List<World>());
        
        Entities.WorldData = Array.Empty<Entities.UniqueWorldData>();
        
        Worlds.Initialize();
    }

    [Test]
    public void CreateEntity() {
        var entity = Entities.Create(Worlds.DefaultWorld.Index);
        
        Assert.That(entity.Index, Is.EqualTo(0));
        Assert.That(entity.Generation, Is.EqualTo(1));
        
        var entity2 = Entities.Create(Worlds.DefaultWorld.Index);
        
        Assert.That(entity2.Index, Is.EqualTo(1));
        Assert.That(entity2.Generation, Is.EqualTo(1));
    }
    
    [Test]
    public void DestroyEntity() {
        var entity = Entities.Create(Worlds.DefaultWorld.Index);
        
        Assert.That(entity.Index, Is.EqualTo(0));
        Assert.That(entity.Generation, Is.EqualTo(1));
        
        Entities.Destroy(Worlds.DefaultWorld.Index, entity.Index);
        
        Assert.That(entity.Index, Is.EqualTo(0));
        Assert.That(entity.Generation, Is.EqualTo(1));
    }
    
    [Test]
    public void DestroyCreateEntityReusesIndexAndIncrementsGeneration() {
        var entity1 = Entities.Create(Worlds.DefaultWorld.Index);
        Assert.That(entity1.Index, Is.EqualTo(0));
        Assert.That(entity1.Generation, Is.EqualTo(1));
        
        var destroyed = Entities.Destroy(Worlds.DefaultWorld.Index, entity1.Index);
        Assert.That(destroyed, Is.True);

        ref var worldData = ref Entities.WorldData[Worlds.DefaultWorld.Index];
        Assert.That(worldData.EntityGenerations[entity1.Index], Is.EqualTo(2));

        var entity2reused = Entities.Create(Worlds.DefaultWorld.Index);
        
        Assert.That(entity2reused.Index, Is.EqualTo(0));
        Assert.That(entity2reused.Generation, Is.EqualTo(3));
        Assert.That(entity2reused.WorldIndex, Is.EqualTo(Worlds.DefaultWorld.Index));
    }

    [Test]
    public void EntityActive() {
        var entity = Entities.Create(Worlds.DefaultWorld.Index);
        var isActive = entity.Valid;
        Assert.That(isActive, Is.True);
    }

    [Test]
    public void EntityInactive() {
        var entity = Entities.Create(Worlds.DefaultWorld.Index);
        entity.Destroy();
        
        var isActive = entity.Valid;
        Assert.That(isActive, Is.False);
    }
}