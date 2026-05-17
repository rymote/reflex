using System;

namespace Rymote.Reflex.Interop;

internal sealed class ReactivePushObservable<TValue> : IObservable<TValue>
{
    private readonly Func<IObserver<TValue>, IDisposable> _subscribeFactory;

    internal ReactivePushObservable(Func<IObserver<TValue>, IDisposable> subscribeFactory)
    {
        _subscribeFactory = subscribeFactory;
    }

    public IDisposable Subscribe(IObserver<TValue> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _subscribeFactory(observer);
    }
}
