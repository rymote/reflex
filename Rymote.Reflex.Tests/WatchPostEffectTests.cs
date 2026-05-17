using Rymote.Reflex.Primitives;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class WatchPostEffectTests
{
    public WatchPostEffectTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void WatchPostEffect_runs_after_normal_effects_in_same_tick()
    {
        Ref<int> activeConnectionCount = new(0);
        System.Collections.Generic.List<string> executionLog = new();

        using System.IDisposable normalEffectSubscription = Reflex.Effect(() =>
        {
            _ = activeConnectionCount.Value;
            executionLog.Add("normal");
        });
        using System.IDisposable postEffectSubscription = Reflex.WatchPostEffect(() =>
        {
            _ = activeConnectionCount.Value;
            executionLog.Add("post");
        });

        executionLog.Clear();
        activeConnectionCount.Value = 3;

        Assert.Equal(new[] { "normal", "post" }, executionLog);
    }
}
