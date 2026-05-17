using System;
using Rymote.Reflex.Primitives;

namespace Rymote.Reflex.Interop;

/// <summary>Extension methods for converting reactive primitives to <see cref="IObservable{T}"/> sequences.</summary>
public static class ObservableExtensions
{
    /// <summary>Returns an <see cref="IObservable{T}"/> that emits the current value of <paramref name="sourceRef"/> and re-emits on each change.</summary>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="sourceRef">The ref to observe.</param>
    public static IObservable<TValue> AsObservable<TValue>(this Ref<TValue> sourceRef)
    {
        ArgumentNullException.ThrowIfNull(sourceRef);
        return new ReactivePushObservable<TValue>(observer =>
            Reflex.Effect(() => observer.OnNext(sourceRef.Value)));
    }

    /// <summary>Returns an <see cref="IObservable{T}"/> that emits the current value of <paramref name="sourceRef"/> and re-emits on each change.</summary>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="sourceRef">The read-only ref to observe.</param>
    public static IObservable<TValue> AsObservable<TValue>(this ReadOnlyRef<TValue> sourceRef)
    {
        ArgumentNullException.ThrowIfNull(sourceRef);
        return new ReactivePushObservable<TValue>(observer =>
            Reflex.Effect(() => observer.OnNext(sourceRef.Value)));
    }

    /// <summary>Returns an <see cref="IObservable{T}"/> that emits the current value of <paramref name="sourceComputed"/> and re-emits on each re-evaluation.</summary>
    /// <typeparam name="TValue">Computed result type.</typeparam>
    /// <param name="sourceComputed">The computed to observe.</param>
    public static IObservable<TValue> AsObservable<TValue>(this Computed<TValue> sourceComputed)
    {
        ArgumentNullException.ThrowIfNull(sourceComputed);
        return new ReactivePushObservable<TValue>(observer =>
            Reflex.Effect(() => observer.OnNext(sourceComputed.Value)));
    }
}
