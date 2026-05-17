using System;
using System.Threading;

namespace Rymote.Reflex.Core;

internal sealed class Subscription : IDisposable
{
    private readonly ReactiveEffect _ownedEffect;
    private int _disposedFlag;

    internal Subscription(ReactiveEffect ownedEffect)
    {
        _ownedEffect = ownedEffect;
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposedFlag, 1, 0) != 0)
            return;

        _ownedEffect.DisposeAndDetach();
    }
}
