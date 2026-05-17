using System;
using System.Collections.Generic;
using Rymote.Reflex.Core;
using Rymote.Reflex.Primitives;

namespace Rymote.Reflex.Observers;

internal static class WatchFactory
{
    internal static IDisposable WatchRef<TValue>(
        Ref<TValue> sourceRef,
        Action<TValue, TValue> handler,
        WatchOptions? watchOptions)
    {
        ArgumentNullException.ThrowIfNull(sourceRef);
        ArgumentNullException.ThrowIfNull(handler);
        return WatchSelector(() => sourceRef.Value, handler, watchOptions);
    }

    internal static IDisposable WatchReadOnlyRef<TValue>(
        ReadOnlyRef<TValue> sourceRef,
        Action<TValue, TValue> handler,
        WatchOptions? watchOptions)
    {
        ArgumentNullException.ThrowIfNull(sourceRef);
        ArgumentNullException.ThrowIfNull(handler);
        return WatchSelector(() => sourceRef.Value, handler, watchOptions);
    }

    internal static IDisposable WatchComputed<TValue>(
        Computed<TValue> sourceComputed,
        Action<TValue, TValue> handler,
        WatchOptions? watchOptions)
    {
        ArgumentNullException.ThrowIfNull(sourceComputed);
        ArgumentNullException.ThrowIfNull(handler);
        return WatchSelector(() => sourceComputed.Value, handler, watchOptions);
    }

    internal static IDisposable WatchSelector<TValue>(
        Func<TValue> selectorFunction,
        Action<TValue, TValue> handler,
        WatchOptions? watchOptions)
    {
        ArgumentNullException.ThrowIfNull(selectorFunction);
        ArgumentNullException.ThrowIfNull(handler);

        bool fireImmediately = watchOptions?.Immediate ?? false;
        TValue lastObservedValue = default!;
        bool hasObservedAtLeastOnce = false;

        IDisposable subscription = EffectFactory.CreateAndRun(() =>
        {
            TValue currentValue = selectorFunction();
            if (!hasObservedAtLeastOnce)
            {
                hasObservedAtLeastOnce = true;
                lastObservedValue = currentValue;
                if (fireImmediately)
                    handler(default!, currentValue);
                return;
            }

            if (EqualityComparer<TValue>.Default.Equals(lastObservedValue, currentValue))
                return;

            TValue previousValue = lastObservedValue;
            lastObservedValue = currentValue;
            handler(previousValue, currentValue);
        }, "<watch>");

        return subscription;
    }
}
