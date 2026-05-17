using System.Collections.Generic;
using Rymote.Reflex.Core;

namespace Rymote.Reflex.Scheduling;

public sealed class SynchronousTestScheduler : IReflexScheduler
{
    private readonly Queue<ReactiveEffect> _normalQueue = new();
    private readonly Queue<ReactiveEffect> _postQueue = new();
    private bool _isDraining;

    public void Schedule(ReactiveEffect effect)
    {
        if (!effect.MarkPending()) return;

        if (effect.IsPostTick) _postQueue.Enqueue(effect);
        else _normalQueue.Enqueue(effect);

        if (_isDraining) return;

        _isDraining = true;
        try
        {
            while (_normalQueue.Count > 0)
            {
                ReactiveEffect nextEffect = _normalQueue.Dequeue();
                nextEffect.ClearPending();
                nextEffect.Run();
            }
            while (_postQueue.Count > 0)
            {
                ReactiveEffect nextEffect = _postQueue.Dequeue();
                nextEffect.ClearPending();
                nextEffect.Run();
            }
        }
        finally
        {
            _isDraining = false;
        }
    }

    public void FlushPendingNow() { }
}
