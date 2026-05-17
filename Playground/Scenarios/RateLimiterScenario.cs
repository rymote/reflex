using System;
using Rymote.Reflex.Primitives;

namespace Playground.Scenarios;

public static class RateLimiterScenario
{
    public static void Run()
    {
        Console.WriteLine("[RateLimiterScenario] starting");

        Ref<int> requestsInWindow = new(0);
        Computed<bool> isOverThreshold = Rymote.Reflex.Reflex.Computed(() => requestsInWindow.Value > 10);

        using IDisposable subscription = Rymote.Reflex.Reflex.Effect(() =>
            Console.WriteLine($"  requests={requestsInWindow.Value} overThreshold={isOverThreshold.Value}"));

        for (int requestIndex = 1; requestIndex <= 15; requestIndex++)
            requestsInWindow.Value = requestIndex;
    }
}
