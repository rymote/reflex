using System;
using System.Threading;
using System.Threading.Tasks;
using Rymote.Reflex.Interop;
using Rymote.Reflex.Primitives;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class InteropTests
{
    public InteropTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void AsObservable_pushes_initial_value_and_updates()
    {
        Ref<int> activeConnectionCount = new(0);
        System.Collections.Generic.List<int> observedValues = new();

        using IDisposable subscription = activeConnectionCount.AsObservable().Subscribe(
            new InlineObserver<int>(value => observedValues.Add(value)));

        activeConnectionCount.Value = 1;
        activeConnectionCount.Value = 2;

        Assert.Equal(new[] { 0, 1, 2 }, observedValues);
    }

    [Fact]
    public async Task ToAsyncEnumerable_yields_initial_value_and_updates()
    {
        Ref<int> activeConnectionCount = new(0);
        CancellationTokenSource cancellation = new();
        System.Collections.Generic.List<int> observedValues = new();

        Task consumerTask = Task.Run(async () =>
        {
            int receivedCount = 0;
            await foreach (int currentValue in activeConnectionCount.ToAsyncEnumerable(cancellation.Token))
            {
                observedValues.Add(currentValue);
                receivedCount++;
                if (receivedCount == 3) cancellation.Cancel();
            }
        });

        await Task.Delay(50);
        activeConnectionCount.Value = 1;
        activeConnectionCount.Value = 2;

        try { await consumerTask; } catch (OperationCanceledException) { }

        Assert.Equal(new[] { 0, 1, 2 }, observedValues);
    }

    private sealed class InlineObserver<TValue> : IObserver<TValue>
    {
        private readonly Action<TValue> _onNext;
        public InlineObserver(Action<TValue> onNext) { _onNext = onNext; }
        public void OnNext(TValue value) => _onNext(value);
        public void OnCompleted() { }
        public void OnError(Exception exception) { }
    }
}
