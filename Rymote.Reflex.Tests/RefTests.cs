using Rymote.Reflex.Primitives;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class RefTests
{
    public RefTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void Value_round_trips()
    {
        Ref<int> activeConnectionCount = new(0);
        activeConnectionCount.Value = 5;
        Assert.Equal(5, activeConnectionCount.Value);
    }

    [Fact]
    public void Setting_value_triggers_subscribed_effect()
    {
        Ref<int> activeConnectionCount = new(0);
        int observedValue = -1;

        using System.IDisposable effectSubscription = Reflex.Effect(
            () => observedValue = activeConnectionCount.Value);

        activeConnectionCount.Value = 7;
        Assert.Equal(7, observedValue);
    }

    [Fact]
    public void Update_applies_transform_atomically()
    {
        Ref<int> activeConnectionCount = new(10);
        activeConnectionCount.Update(currentCount => currentCount + 5);
        Assert.Equal(15, activeConnectionCount.Value);
    }
}
