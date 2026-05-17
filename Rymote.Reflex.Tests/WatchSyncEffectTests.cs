using Rymote.Reflex.Primitives;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class WatchSyncEffectTests
{
    public WatchSyncEffectTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void WatchSyncEffect_runs_on_writer_thread_inline_with_write()
    {
        Ref<int> activeConnectionCount = new(0);
        int observedThreadId = -1;

        using System.IDisposable subscription = Reflex.WatchSyncEffect(() =>
        {
            _ = activeConnectionCount.Value;
            observedThreadId = System.Environment.CurrentManagedThreadId;
        });

        int writerThreadIdBeforeWrite = System.Environment.CurrentManagedThreadId;
        activeConnectionCount.Value = 9;
        Assert.Equal(writerThreadIdBeforeWrite, observedThreadId);
    }
}
