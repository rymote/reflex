using System.Collections.Immutable;
using System.Threading;

namespace Rymote.Reflex.Core;

public sealed class DependencySlot
{
    private ImmutableHashSet<ReactiveEffect> _subscribedEffects = ImmutableHashSet<ReactiveEffect>.Empty;

    public void RegisterCurrentEffectAsDependent()
    {
        ReactiveEffect? currentEffect = EffectStack.Current;
        if (currentEffect is null)
            return;

        AddSubscriber(currentEffect);
        currentEffect.TrackSlotForCleanup(this);
    }

    public void NotifyAllSubscribers()
    {
        ImmutableHashSet<ReactiveEffect> snapshotOfSubscribers =
            Volatile.Read(ref _subscribedEffects);

        using BatchFrame implicitNotificationBatch = BatchStack.BeginNewFrame();
        foreach (ReactiveEffect subscribedEffect in snapshotOfSubscribers)
            subscribedEffect.ScheduleRerun();
    }

    internal void RemoveSubscriber(ReactiveEffect effectToRemove)
    {
        ImmutableHashSet<ReactiveEffect> currentSnapshot;
        ImmutableHashSet<ReactiveEffect> replacementSnapshot;
        do
        {
            currentSnapshot = Volatile.Read(ref _subscribedEffects);
            if (!currentSnapshot.Contains(effectToRemove))
                return;
            replacementSnapshot = currentSnapshot.Remove(effectToRemove);
        }
        while (Interlocked.CompareExchange(
            ref _subscribedEffects, replacementSnapshot, currentSnapshot) != currentSnapshot);
    }

    private void AddSubscriber(ReactiveEffect effectToAdd)
    {
        ImmutableHashSet<ReactiveEffect> currentSnapshot;
        ImmutableHashSet<ReactiveEffect> replacementSnapshot;
        do
        {
            currentSnapshot = Volatile.Read(ref _subscribedEffects);
            if (currentSnapshot.Contains(effectToAdd))
                return;
            replacementSnapshot = currentSnapshot.Add(effectToAdd);
        }
        while (Interlocked.CompareExchange(
            ref _subscribedEffects, replacementSnapshot, currentSnapshot) != currentSnapshot);
    }
}
