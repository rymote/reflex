using System;
using System.Collections.Generic;

namespace Rymote.Reflex.Core;

internal sealed class BatchFrame : IDisposable
{
    private readonly HashSet<ReactiveEffect> _dirtyEffects = new();
    private readonly object _dirtyEffectsLock = new();
    private bool _isDisposed;

    internal void AddDirtyEffect(ReactiveEffect effect)
    {
        lock (_dirtyEffectsLock)
            _dirtyEffects.Add(effect);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        HashSet<ReactiveEffect> drainedEffects;
        lock (_dirtyEffectsLock)
        {
            drainedEffects = new HashSet<ReactiveEffect>(_dirtyEffects);
            _dirtyEffects.Clear();
        }

        BatchStack.PopAndMaybeFlush(this, drainedEffects);
    }
}
