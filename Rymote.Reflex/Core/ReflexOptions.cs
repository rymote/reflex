using System;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Core;

/// <summary>Global configuration options for the Reflex runtime.</summary>
public sealed class ReflexOptions
{
    /// <summary>Gets or sets the scheduler responsible for enqueuing and running reactive effects.
    /// Defaults to <see cref="ThreadPoolScheduler"/>. Replace with <see cref="SynchronousTestScheduler"/> in unit tests.</summary>
    public IReflexScheduler Scheduler { get; set; } = new ThreadPoolScheduler();

    /// <summary>Gets or sets a handler invoked when an effect throws an unhandled exception.
    /// The default handler writes the exception to <see cref="Console.Error"/>.</summary>
    public Action<Exception, EffectExceptionInfo> OnEffectException { get; set; } =
        static (raisedException, exceptionInfo) =>
            Console.Error.WriteLine(
                $"Rymote.Reflex effect '{exceptionInfo.DebugName ?? "<unnamed>"}' threw " +
                $"after {exceptionInfo.ConsecutiveFailures} consecutive failures: {raisedException}");

    /// <summary>Gets or sets the maximum number of times an effect may be re-queued within a single scheduler tick
    /// before Reflex aborts with a cycle-detection error. Defaults to 100.</summary>
    public int MaxReentrantRunsPerTick { get; set; } = 100;
}
