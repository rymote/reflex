using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using Rymote.Reflex.Primitives;

namespace Rymote.Reflex.Interop;

/// <summary>Extension methods for converting reactive primitives to <see cref="IAsyncEnumerable{T}"/> streams.</summary>
public static class AsyncEnumerableExtensions
{
    /// <summary>Converts <paramref name="sourceRef"/> into an async sequence that yields a new value each time the ref changes.</summary>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="sourceRef">The ref to stream.</param>
    /// <param name="cancellationToken">Token that ends the stream when cancelled.</param>
    public static IAsyncEnumerable<TValue> ToAsyncEnumerable<TValue>(
        this Ref<TValue> sourceRef,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceRef);
        return StreamValuesAsync(() => sourceRef.Value, cancellationToken);
    }

    /// <summary>Converts <paramref name="sourceRef"/> into an async sequence that yields a new value each time the ref changes.</summary>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="sourceRef">The read-only ref to stream.</param>
    /// <param name="cancellationToken">Token that ends the stream when cancelled.</param>
    public static IAsyncEnumerable<TValue> ToAsyncEnumerable<TValue>(
        this ReadOnlyRef<TValue> sourceRef,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceRef);
        return StreamValuesAsync(() => sourceRef.Value, cancellationToken);
    }

    /// <summary>Converts <paramref name="sourceComputed"/> into an async sequence that yields a new value each time the computed re-evaluates to a different result.</summary>
    /// <typeparam name="TValue">Computed result type.</typeparam>
    /// <param name="sourceComputed">The computed to stream.</param>
    /// <param name="cancellationToken">Token that ends the stream when cancelled.</param>
    public static IAsyncEnumerable<TValue> ToAsyncEnumerable<TValue>(
        this Computed<TValue> sourceComputed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceComputed);
        return StreamValuesAsync(() => sourceComputed.Value, cancellationToken);
    }

    private static async IAsyncEnumerable<TValue> StreamValuesAsync<TValue>(
        Func<TValue> valueAccessor,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Channel<TValue> deliveryChannel = Channel.CreateUnbounded<TValue>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        using IDisposable effectSubscription = Reflex.Effect(() =>
            deliveryChannel.Writer.TryWrite(valueAccessor()));

        await foreach (TValue currentValue in
            deliveryChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return currentValue;
        }
    }
}
