using System;
using Rymote.Reflex.Primitives;

namespace Rymote.Reflex.Interop;

public static class ObservableExtensions
{
    public static IObservable<TValue> AsObservable<TValue>(this Ref<TValue> sourceRef)
    {
        ArgumentNullException.ThrowIfNull(sourceRef);
        return new ReactivePushObservable<TValue>(observer =>
            Reflex.Effect(() => observer.OnNext(sourceRef.Value)));
    }

    public static IObservable<TValue> AsObservable<TValue>(this ReadOnlyRef<TValue> sourceRef)
    {
        ArgumentNullException.ThrowIfNull(sourceRef);
        return new ReactivePushObservable<TValue>(observer =>
            Reflex.Effect(() => observer.OnNext(sourceRef.Value)));
    }

    public static IObservable<TValue> AsObservable<TValue>(this Computed<TValue> sourceComputed)
    {
        ArgumentNullException.ThrowIfNull(sourceComputed);
        return new ReactivePushObservable<TValue>(observer =>
            Reflex.Effect(() => observer.OnNext(sourceComputed.Value)));
    }
}
