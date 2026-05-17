using System.Threading.Tasks;
using Rymote.Reflex.Primitives;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class ConcurrencyTests
{
    public ConcurrencyTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new ThreadPoolScheduler());
    }

    [Fact]
    public async Task Parallel_writers_using_Update_do_not_lose_increments()
    {
        Ref<int> activeConnectionCount = new(0);
        const int writerCount = 16;
        const int incrementsPerWriter = 1000;

        Task[] writerTasks = new Task[writerCount];
        for (int writerIndex = 0; writerIndex < writerCount; writerIndex++)
            writerTasks[writerIndex] = Task.Run(() =>
            {
                for (int iterationIndex = 0; iterationIndex < incrementsPerWriter; iterationIndex++)
                    activeConnectionCount.Update(currentCount => currentCount + 1);
            });

        await Task.WhenAll(writerTasks);

        Assert.Equal(writerCount * incrementsPerWriter, activeConnectionCount.Value);
    }

    [Fact]
    public async Task Effect_disposal_during_concurrent_writes_is_safe()
    {
        Ref<int> activeConnectionCount = new(0);
        System.IDisposable effectSubscription = Reflex.Effect(() => _ = activeConnectionCount.Value);

        Task writerTask = Task.Run(() =>
        {
            for (int iterationIndex = 0; iterationIndex < 10_000; iterationIndex++)
                activeConnectionCount.Update(currentCount => currentCount + 1);
        });

        await Task.Delay(20);
        effectSubscription.Dispose();
        await writerTask;
    }
}
