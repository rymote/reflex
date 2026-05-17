using Rymote.Reflex.Primitives;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class ReadOnlyRefTests
{
    public ReadOnlyRefTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void ReadOnlyRef_reflects_source_value()
    {
        Ref<int> activeConnectionCount = new(10);
        ReadOnlyRef<int> publicView = activeConnectionCount.AsReadOnly();

        Assert.Equal(10, publicView.Value);

        activeConnectionCount.Value = 25;
        Assert.Equal(25, publicView.Value);
    }

    [Fact]
    public void Effect_tracking_through_ReadOnlyRef()
    {
        Ref<int> activeConnectionCount = new(0);
        ReadOnlyRef<int> publicView = activeConnectionCount.AsReadOnly();
        int observedValue = -1;

        using System.IDisposable effectSubscription = Reflex.Effect(
            () => observedValue = publicView.Value);

        activeConnectionCount.Value = 42;
        Assert.Equal(42, observedValue);
    }
}
