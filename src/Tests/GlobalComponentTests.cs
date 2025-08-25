using NUnit.Framework;
using Signals.Core;

namespace Tests;

[TestFixture]
public class GlobalComponentTests {
    [SetUp]
    public void Setup() {
        typeof(Components)
            .GetField("_componentCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, 0u);

        typeof(Components)
            .GetField("_components", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, Array.Empty<Components.Info>());
    }

    [Test]
    public void SetGetGlobalComponent() {
        Globals.Set(new Apple());
        
        Assert.That(Globals.Has<Apple>());
    }
    
    [Test]
    public void RemoveWorldComponent() {
        Globals.Remove<Apple>();
        
        Assert.That(!Globals.Has<Apple>());
    }
}