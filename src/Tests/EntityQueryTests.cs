using NUnit.Framework;
using Signals.Core;

namespace Tests;

[TestFixture]
public class EntityQueryTests {
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
            .GetField("_components", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, Array.Empty<Components.Info>());
        
        Components.RegisterComponent(typeof(Apple));
        Components.RegisterComponent(typeof(Orange));
    }

    [Test]
    public void QueryEntities() {
        var worldId = Worlds.DefaultWorld.Index;
        
        var query = new Query(worldId).With<Apple>().Iterate();
        
        var entity1 = Entities.Create(worldId);
        var entity2 = Entities.Create(worldId);
        
        entity1.Set(new Apple());
        entity2.Set(new Apple());
        
        var foundEntities = new List<Entity>();

        while(query.Next() is { } entity) {
            foundEntities.Add(entity);
        }
        
        Assert.That(foundEntities, Has.Count.EqualTo(2), "query for Apple should find 2 entities.");
        Assert.That(foundEntities, Does.Contain(entity1));
        Assert.That(foundEntities, Does.Contain(entity2));
    }
    
    [Test]
    public void QueryEmptyWorldYieldsNoResults() {
        var worldId = Worlds.DefaultWorld.Index;
        var query = new Query(worldId);
        var iterator = query.Iterate();

        Assert.That(iterator.Next(), Is.Null, "query in an empty world should not find any entities.");
    }
}