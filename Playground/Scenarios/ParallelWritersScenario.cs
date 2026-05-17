using System;
using System.Threading.Tasks;
using Rymote.Reflex.Primitives;

namespace Playground.Scenarios;

public static class ParallelWritersScenario
{
    public static void Run()
    {
        Console.WriteLine("[ParallelWritersScenario] starting");

        Ref<int> activeConnectionCount = new(0);

        Task[] writerTasks = new Task[8];
        for (int writerIndex = 0; writerIndex < writerTasks.Length; writerIndex++)
            writerTasks[writerIndex] = Task.Run(() =>
            {
                for (int iterationIndex = 0; iterationIndex < 1000; iterationIndex++)
                    activeConnectionCount.Update(currentCount => currentCount + 1);
            });

        Task.WaitAll(writerTasks);
        Console.WriteLine($"  final count: {activeConnectionCount.Value} (expected 8000)");
    }
}
