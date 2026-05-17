using System;
using Rymote.Reflex.Interop;
using Rymote.Reflex.Primitives;

namespace Playground.Scenarios;

public static class MetricsEmitterScenario
{
    public static void Run()
    {
        Console.WriteLine("[MetricsEmitterScenario] starting");

        Ref<int> activeConnectionCount = new(0);
        using IDisposable subscription = activeConnectionCount.AsObservable().Subscribe(
            new MetricsObserver());

        activeConnectionCount.Value = 5;
        activeConnectionCount.Value = 12;
    }

    private sealed class MetricsObserver : IObserver<int>
    {
        public void OnNext(int value) => Console.WriteLine($"  metric pushed: {value}");
        public void OnCompleted() { }
        public void OnError(Exception exception) { }
    }
}
