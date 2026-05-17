using Rymote.Reflex.Observers;
using Rymote.Reflex.Primitives;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class WatchTests
{
    public WatchTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void Watch_on_Ref_fires_with_previous_and_current()
    {
        Ref<int> activeConnectionCount = new(0);
        int observedPrevious = -1;
        int observedCurrent = -1;

        using System.IDisposable subscription = Reflex.Watch(
            activeConnectionCount,
            (previousValue, currentValue) =>
            {
                observedPrevious = previousValue;
                observedCurrent = currentValue;
            });

        activeConnectionCount.Value = 5;
        Assert.Equal(0, observedPrevious);
        Assert.Equal(5, observedCurrent);

        activeConnectionCount.Value = 12;
        Assert.Equal(5, observedPrevious);
        Assert.Equal(12, observedCurrent);
    }

    [Fact]
    public void Watch_on_selector_fires_only_when_projected_value_changes()
    {
        Ref<int> firstValue = new(10);
        Ref<int> secondValue = new(0);
        int handlerInvocationCount = 0;

        using System.IDisposable subscription = Reflex.Watch(
            () => firstValue.Value > 5,
            (_, _) => handlerInvocationCount++);

        secondValue.Value = 1;
        Assert.Equal(0, handlerInvocationCount);

        firstValue.Value = 3;
        Assert.Equal(1, handlerInvocationCount);

        firstValue.Value = 2;
        Assert.Equal(1, handlerInvocationCount);

        firstValue.Value = 99;
        Assert.Equal(2, handlerInvocationCount);
    }

    [Fact]
    public void Watch_with_Immediate_fires_at_registration()
    {
        Ref<int> activeConnectionCount = new(7);
        int observedCurrent = -1;

        using System.IDisposable subscription = Reflex.Watch(
            activeConnectionCount,
            (_, currentValue) => observedCurrent = currentValue,
            new WatchOptions { Immediate = true });

        Assert.Equal(7, observedCurrent);
    }
}
