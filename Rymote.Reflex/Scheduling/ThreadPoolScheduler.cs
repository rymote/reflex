using System.Collections.Concurrent;
using System.Threading;
using Rymote.Reflex.Core;

namespace Rymote.Reflex.Scheduling;

public sealed class ThreadPoolScheduler : IReflexScheduler
{
    private readonly ConcurrentQueue<ReactiveEffect> _normalQueue = new();
    private readonly ConcurrentQueue<ReactiveEffect> _postQueue = new();
    private int _drainScheduled;

    public void Schedule(ReactiveEffect effect)
    {
        if (!effect.MarkPending()) return;

        if (effect.IsPostTick) _postQueue.Enqueue(effect);
        else _normalQueue.Enqueue(effect);

        if (Interlocked.CompareExchange(ref _drainScheduled, 1, 0) == 0)
        {
            ThreadPool.UnsafeQueueUserWorkItem(
                static schedulerInstance => ((ThreadPoolScheduler)schedulerInstance!).Drain(),
                this);
        }
    }

    public void FlushPendingNow() { Drain(); }

    private void Drain()
    {
        Interlocked.Exchange(ref _drainScheduled, 0);

        while (_normalQueue.TryDequeue(out ReactiveEffect? nextEffect))
        {
            nextEffect.ClearPending();
            nextEffect.Run();
        }
        while (_postQueue.TryDequeue(out ReactiveEffect? nextEffect))
        {
            nextEffect.ClearPending();
            nextEffect.Run();
        }
    }
}
