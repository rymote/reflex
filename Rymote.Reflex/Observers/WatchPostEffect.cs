using System;
using Rymote.Reflex.Core;

namespace Rymote.Reflex.Observers;

internal static class WatchPostEffectFactory
{
    internal static IDisposable CreateAndRun(Action effectCallback, string? debugName)
    {
        ArgumentNullException.ThrowIfNull(effectCallback);

        ReactiveEffect createdEffect = new(_ => effectCallback(), debugName,
            isSynchronous: false, isPostTick: true);
        Subscription subscription = new(createdEffect);
        createdEffect.RunInitially();
        return subscription;
    }
}
