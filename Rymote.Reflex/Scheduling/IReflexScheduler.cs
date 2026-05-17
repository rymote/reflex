using Rymote.Reflex.Core;

namespace Rymote.Reflex.Scheduling;

/// <summary>Abstraction over the mechanism that enqueues and runs reactive effects.</summary>
public interface IReflexScheduler
{
    /// <summary>Schedules <paramref name="effect"/> for execution. Implementations must be thread-safe.</summary>
    /// <param name="effect">The effect to schedule.</param>
    void Schedule(ReactiveEffect effect);

    /// <summary>Synchronously drains any pending effects that have been scheduled but not yet run.</summary>
    void FlushPendingNow();
}
