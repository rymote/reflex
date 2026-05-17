using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class GeneratorRuntimeSmokeTests
{
    public GeneratorRuntimeSmokeTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void Generated_partial_implements_IReactive()
    {
        TestReactiveModel model = new();
        Assert.IsAssignableFrom<Rymote.Reflex.Utilities.IReactive>(model);
    }

    [Fact]
    public void Property_setter_triggers_subscribed_effect()
    {
        TestReactiveModel model = new();
        int observed = -1;
        using System.IDisposable subscription = Reflex.Effect(() => observed = model.Counter);
        model.Counter = 42;
        Assert.Equal(42, observed);
    }
}
