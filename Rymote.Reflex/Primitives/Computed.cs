using System;
using System.Threading;
using Rymote.Reflex.Core;

namespace Rymote.Reflex.Primitives;

/// <summary>A lazily-evaluated derived value that caches its result and re-evaluates when any reactive dependency changes.</summary>
/// <typeparam name="TValue">Type of the computed result.</typeparam>
public sealed class Computed<TValue>
{
    private readonly Func<TValue> _evaluateFunction;
    private readonly DependencySlot _publishedValueSlot = new();
    private readonly object _evaluationLock = new();
    private readonly ReactiveEffect _invalidationEffect;
    private TValue _cachedValue = default!;
    private bool _cacheIsDirty = true;
    private int _isEvaluating;

    /// <summary>Initializes a new <see cref="Computed{TValue}"/> backed by the given evaluation function.</summary>
    /// <param name="evaluateFunction">A pure function whose reactive reads form the dependency graph.
    /// The function is not called until <see cref="Value"/> is first accessed.</param>
    public Computed(Func<TValue> evaluateFunction)
    {
        ArgumentNullException.ThrowIfNull(evaluateFunction);
        _evaluateFunction = evaluateFunction;
        _invalidationEffect = new ReactiveEffect(_ => MarkDirtyAndPropagate(), "<computed>");
    }

    /// <summary>Gets the current computed value. Re-evaluates if the cache is stale. Reads inside an active effect register a dependency.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the evaluation function reads its own <see cref="Value"/> (recursive evaluation).</exception>
    public TValue Value
    {
        get
        {
            _publishedValueSlot.RegisterCurrentEffectAsDependent();
            return EnsureFreshAndReturn();
        }
    }

    private TValue EnsureFreshAndReturn()
    {
        lock (_evaluationLock)
        {
            if (!_cacheIsDirty)
                return _cachedValue;

            if (Interlocked.Exchange(ref _isEvaluating, 1) == 1)
                throw new InvalidOperationException(
                    "Recursive Computed evaluation detected.");

            try
            {
                EffectStack.Push(_invalidationEffect);
                try
                {
                    _cachedValue = _evaluateFunction();
                }
                finally
                {
                    EffectStack.Pop(_invalidationEffect);
                }

                _cacheIsDirty = false;
                return _cachedValue;
            }
            finally
            {
                Interlocked.Exchange(ref _isEvaluating, 0);
            }
        }
    }

    private void MarkDirtyAndPropagate()
    {
        bool wasClean;
        lock (_evaluationLock)
        {
            wasClean = !_cacheIsDirty;
            _cacheIsDirty = true;
        }

        if (wasClean)
            _publishedValueSlot.NotifyAllSubscribers();
    }
}
