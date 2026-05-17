using System;

namespace Rymote.Reflex.Interop;

internal sealed class ChangeObservableAdapter<TChangeEvent> : IObservable<TChangeEvent>
{
    private readonly Action<Action<TChangeEvent>> _registerHandler;
    private readonly Action<Action<TChangeEvent>> _unregisterHandler;

    internal ChangeObservableAdapter(
        Action<Action<TChangeEvent>> registerHandler,
        Action<Action<TChangeEvent>> unregisterHandler)
    {
        _registerHandler = registerHandler;
        _unregisterHandler = unregisterHandler;
    }

    public IDisposable Subscribe(IObserver<TChangeEvent> observer)
    {
        void Handler(TChangeEvent changeEvent) => observer.OnNext(changeEvent);
        _registerHandler(Handler);
        return new UnregisterOnDispose(() => _unregisterHandler(Handler));
    }

    private sealed class UnregisterOnDispose : IDisposable
    {
        private Action? _unregister;
        internal UnregisterOnDispose(Action unregister) { _unregister = unregister; }
        public void Dispose() { _unregister?.Invoke(); _unregister = null; }
    }
}
