using System;
using Rymote.Reflex.Core;

namespace Rymote.Reflex.Observers;

internal static class WatchSyncEffectFactory
{
    internal static IDisposable CreateAndRun(Action effectCallback, string? debugName)
    {
        ArgumentNullException.ThrowIfNull(effectCallback);

        ReactiveEffect createdEffect = new(_ => effectCallback(), debugName,
            isSynchronous: true, isPostTick: false);
        Subscription subscription = new(createdEffect);
        createdEffect.RunInitially();
        return subscription;
    }
}
