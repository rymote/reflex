using System;
using Rymote.Reflex.Core;

namespace Rymote.Reflex.Observers;

internal static class EffectFactory
{
    internal static IDisposable CreateAndRun(Action effectCallback, string? debugName)
    {
        ArgumentNullException.ThrowIfNull(effectCallback);

        ReactiveEffect createdEffect = new(_ => effectCallback(), debugName);
        Subscription subscription = new(createdEffect);
        createdEffect.RunInitially();
        return subscription;
    }

    internal static IDisposable CreateAndRunWithSelf(
        Action<IDisposable> effectCallbackWithSelf,
        string? debugName)
    {
        ArgumentNullException.ThrowIfNull(effectCallbackWithSelf);

        Subscription? subscriptionReference = null;
        ReactiveEffect createdEffect = new(_ => effectCallbackWithSelf(subscriptionReference!), debugName);
        subscriptionReference = new Subscription(createdEffect);
        createdEffect.RunInitially();
        return subscriptionReference;
    }
}
