using System;
using System.Collections.Generic;
using Rymote.Reflex.Observers;

namespace Rymote.Reflex.Core;

public sealed class EffectScope : IDisposable
{
    private readonly List<IDisposable> _attachedSubscriptions = new();
    private readonly List<EffectScope> _childScopes = new();
    private bool _isDisposed;

    public IDisposable Effect(Action effectCallback, string? debugName = null)
    {
        ThrowIfDisposed();
        IDisposable subscription = EffectFactory.CreateAndRun(effectCallback, debugName);
        _attachedSubscriptions.Add(subscription);
        return subscription;
    }

    public IDisposable Effect(Action<IDisposable> effectCallbackWithSelf, string? debugName = null)
    {
        ThrowIfDisposed();
        IDisposable subscription = EffectFactory.CreateAndRunWithSelf(effectCallbackWithSelf, debugName);
        _attachedSubscriptions.Add(subscription);
        return subscription;
    }

    public EffectScope CreateChildScope()
    {
        ThrowIfDisposed();
        EffectScope childScope = new();
        _childScopes.Add(childScope);
        return childScope;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        foreach (EffectScope childScope in _childScopes)
            childScope.Dispose();
        _childScopes.Clear();

        foreach (IDisposable subscription in _attachedSubscriptions)
            subscription.Dispose();
        _attachedSubscriptions.Clear();
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(EffectScope));
    }
}
