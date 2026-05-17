using Rymote.Reflex.Primitives;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class ComputedTests
{
    public ComputedTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void Computed_returns_initial_derived_value()
    {
        Ref<int> activeConnectionCount = new(5);
        Computed<bool> isUnderLoad = Reflex.Computed(() => activeConnectionCount.Value > 10);

        Assert.False(isUnderLoad.Value);
    }

    [Fact]
    public void Computed_recomputes_after_dependency_changes()
    {
        Ref<int> activeConnectionCount = new(5);
        Computed<bool> isUnderLoad = Reflex.Computed(() => activeConnectionCount.Value > 10);

        activeConnectionCount.Value = 25;
        Assert.True(isUnderLoad.Value);
    }

    [Fact]
    public void Computed_caches_between_dependency_changes()
    {
        Ref<int> activeConnectionCount = new(5);
        int evaluationCount = 0;

        Computed<bool> isUnderLoad = Reflex.Computed(() =>
        {
            evaluationCount++;
            return activeConnectionCount.Value > 10;
        });

        bool firstRead = isUnderLoad.Value;
        bool secondRead = isUnderLoad.Value;
        bool thirdRead = isUnderLoad.Value;

        Assert.Equal(firstRead, secondRead);
        Assert.Equal(secondRead, thirdRead);
        Assert.Equal(1, evaluationCount);
    }

    [Fact]
    public void Effect_re_runs_when_computed_dependency_flips()
    {
        Ref<int> activeConnectionCount = new(0);
        Computed<bool> isUnderLoad = Reflex.Computed(() => activeConnectionCount.Value > 10);
        bool observedLoadState = false;

        using System.IDisposable effectSubscription = Reflex.Effect(
            () => observedLoadState = isUnderLoad.Value);

        activeConnectionCount.Value = 25;
        Assert.True(observedLoadState);

        activeConnectionCount.Value = 1;
        Assert.False(observedLoadState);
    }
}
