using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using Rymote.Reflex.Primitives;

namespace Rymote.Reflex.Interop;

public static class AsyncEnumerableExtensions
{
    public static IAsyncEnumerable<TValue> ToAsyncEnumerable<TValue>(
        this Ref<TValue> sourceRef,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceRef);
        return StreamValuesAsync(() => sourceRef.Value, cancellationToken);
    }

    public static IAsyncEnumerable<TValue> ToAsyncEnumerable<TValue>(
        this ReadOnlyRef<TValue> sourceRef,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceRef);
        return StreamValuesAsync(() => sourceRef.Value, cancellationToken);
    }

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
