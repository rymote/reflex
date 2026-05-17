using Rymote.Reflex.Primitives;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class EffectTests
{
    public EffectTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void Effect_runs_once_at_registration()
    {
        Ref<int> activeConnectionCount = new(3);
        int observedValue = -1;

        using System.IDisposable effectSubscription = Reflex.Effect(
            () => observedValue = activeConnectionCount.Value);

        Assert.Equal(3, observedValue);
    }

    [Fact]
    public void Disposed_effect_stops_reacting()
    {
        Ref<int> activeConnectionCount = new(0);
        int observedValue = -1;

        System.IDisposable effectSubscription = Reflex.Effect(
            () => observedValue = activeConnectionCount.Value);
        effectSubscription.Dispose();

        activeConnectionCount.Value = 9;
        Assert.Equal(0, observedValue);
    }

    [Fact]
    public void Effect_with_self_overload_can_dispose_itself()
    {
        Ref<int> activeConnectionCount = new(0);
        int observedRunCount = 0;

        using System.IDisposable effectSubscription = Reflex.Effect(self =>
        {
            observedRunCount++;
            if (activeConnectionCount.Value > 2)
                self.Dispose();
        });

        activeConnectionCount.Value = 1;
        activeConnectionCount.Value = 5;
        activeConnectionCount.Value = 10;

        Assert.Equal(3, observedRunCount);
    }
}
