using System;
using System.Threading;
using Rymote.Reflex.Core;

namespace Rymote.Reflex.Primitives;

public sealed class Computed<TValue>
{
    private readonly Func<TValue> _evaluateFunction;
    private readonly DependencySlot _publishedValueSlot = new();
    private readonly object _evaluationLock = new();
    private readonly ReactiveEffect _invalidationEffect;
    private TValue _cachedValue = default!;
    private bool _cacheIsDirty = true;
    private int _isEvaluating;

    public Computed(Func<TValue> evaluateFunction)
    {
        ArgumentNullException.ThrowIfNull(evaluateFunction);
        _evaluateFunction = evaluateFunction;
        _invalidationEffect = new ReactiveEffect(_ => MarkDirtyAndPropagate(), "<computed>");
    }

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
