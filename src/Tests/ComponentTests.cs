using NUnit.Framework;
using Signals.Core;

namespace Tests;

internal struct Apple : IComponent {
    public int Data;
}

internal struct Orange : IComponent {
    public int Data;
}

[TestFixture]
public class ComponentTests {
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
            ?.SetValue(null, Array.Empty<Components.ComponentInfo>());
    }
    
    [Test]
    public void ComponentRegistrationAssignsUniqueHandles() {
        var appleHandle = Components.GetComponentHandle<Apple>();
        var orangeHandle = Components.GetComponentHandle<Orange>();

        Assert.That(appleHandle.IsValid, Is.True);
        Assert.That(orangeHandle.IsValid, Is.True);

        Assert.That(appleHandle.Id, Is.Not.EqualTo(0));
        Assert.That(orangeHandle.Id, Is.Not.EqualTo(0));

        Assert.That(appleHandle.Id, Is.Not.EqualTo(orangeHandle.Id));

        Assert.That(Components.ComponentMasksPerEntity, Is.GreaterThanOrEqualTo(1));
    }
    
    [Test]
    public void EntityGetComponent() {
        var entity = Entities.Create(Worlds.DefaultWorld.Index);

        entity.Set(new Apple() with { Data = 10} );

        var apple = entity.Get<Apple>();
        
        Assert.That(apple.Data, Is.EqualTo(10));
    }
    
    [Test]
    public void EntityHasComponent() {
        var entity = Entities.Create(Worlds.DefaultWorld.Index);

        entity.Set(new Apple() with { Data = 10} );
        
        Assert.That(entity.Has<Apple>());
    }
    
    [Test]
    public void EntitySetRemoveSetComponent() {
        var entity = Entities.Create(Worlds.DefaultWorld.Index);

        entity.Set(new Apple() with { Data = 10 } );
        entity.Remove<Apple>();
        entity.Set(new Apple() with { Data = 5 } );
        
        var apple = entity.Get<Apple>();
        
        Assert.That(apple.Data, Is.EqualTo(5));
    }
    
    [Test]
    public void EntityDestroyRemovesComponents() {
        var entity = Entities.Create(Worlds.DefaultWorld.Index);

        entity.Set(new Apple() with { Data = 10} );
        entity.Set(new Orange() with { Data = 10} );

        entity.Destroy();
        
        Assert.That(!entity.Has<Apple>());
        Assert.That(!entity.Has<Orange>());
    }
}