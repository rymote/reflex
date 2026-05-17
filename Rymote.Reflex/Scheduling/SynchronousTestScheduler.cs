using System.Collections.Generic;
using Rymote.Reflex.Core;

namespace Rymote.Reflex.Scheduling;

/// <summary>Test scheduler that runs reactive effects synchronously on the calling thread.
/// Use this in unit tests to avoid timing-related non-determinism. Not safe for concurrent use.</summary>
public sealed class SynchronousTestScheduler : IReflexScheduler
{
    private readonly Queue<ReactiveEffect> _normalQueue = new();
    private readonly Queue<ReactiveEffect> _postQueue = new();
    private bool _isDraining;

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void FlushPendingNow() { }
}
