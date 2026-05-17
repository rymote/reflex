using Rymote.Reflex.Primitives;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class BatchTests
{
    public BatchTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void Batch_coalesces_multiple_writes_into_one_effect_run()
    {
        Ref<int> firstValue = new(0);
        Ref<int> secondValue = new(0);
        int effectRunCount = 0;

        using System.IDisposable effectSubscription = Reflex.Effect(() =>
        {
            _ = firstValue.Value;
            _ = secondValue.Value;
            effectRunCount++;
        });

        int runsBeforeBatch = effectRunCount;

        using (Reflex.Batch())
        {
            firstValue.Value = 1;
            secondValue.Value = 2;
            firstValue.Value = 3;
        }

        Assert.Equal(runsBeforeBatch + 1, effectRunCount);
    }

    [Fact]
    public void Nested_batches_flush_only_at_outermost_dispose()
    {
        Ref<int> activeConnectionCount = new(0);
        int effectRunCount = 0;

        using System.IDisposable effectSubscription = Reflex.Effect(() =>
        {
            _ = activeConnectionCount.Value;
            effectRunCount++;
        });

        int runsBeforeBatch = effectRunCount;

        using (Reflex.Batch())
        {
            activeConnectionCount.Value = 1;
            using (Reflex.Batch())
            {
                activeConnectionCount.Value = 2;
                activeConnectionCount.Value = 3;
            }
            Assert.Equal(runsBeforeBatch, effectRunCount);
        }

        Assert.Equal(runsBeforeBatch + 1, effectRunCount);
    }
}
