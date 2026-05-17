using System;
using System.Collections.Generic;
using Rymote.Reflex.Observers;

namespace Rymote.Reflex.Core;

/// <summary>Owns a group of reactive effects and child scopes, disposing them all when the scope is disposed.</summary>
public sealed class EffectScope : IDisposable
{
    private readonly List<IDisposable> _attachedSubscriptions = new();
    private readonly List<EffectScope> _childScopes = new();
    private bool _isDisposed;

    /// <summary>Creates and registers a reactive effect within this scope. The effect is disposed when the scope is disposed.</summary>
    /// <param name="effectCallback">The effect body.</param>
    /// <param name="debugName">Optional name shown in diagnostics and error messages.</param>
    /// <returns>A disposable that stops the effect before the scope is disposed.</returns>
    public IDisposable Effect(Action effectCallback, string? debugName = null)
    {
        ThrowIfDisposed();
        IDisposable subscription = EffectFactory.CreateAndRun(effectCallback, debugName);
        _attachedSubscriptions.Add(subscription);
        return subscription;
    }

    /// <summary>Creates and registers a self-referencing reactive effect within this scope.</summary>
    /// <param name="effectCallbackWithSelf">Effect body that receives its own subscription handle.</param>
    /// <param name="debugName">Optional name shown in diagnostics and error messages.</param>
    /// <returns>A disposable that stops the effect before the scope is disposed.</returns>
    public IDisposable Effect(Action<IDisposable> effectCallbackWithSelf, string? debugName = null)
    {
        ThrowIfDisposed();
        IDisposable subscription = EffectFactory.CreateAndRunWithSelf(effectCallbackWithSelf, debugName);
        _attachedSubscriptions.Add(subscription);
        return subscription;
    }

    /// <summary>Creates a child scope whose lifetime is tied to this scope.</summary>
    /// <returns>A new, empty child <see cref="EffectScope"/>.</returns>
    public EffectScope CreateChildScope()
    {
        ThrowIfDisposed();
        EffectScope childScope = new();
        _childScopes.Add(childScope);
        return childScope;
    }

    /// <summary>Disposes all child scopes and effects registered in this scope.</summary>
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
