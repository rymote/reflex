using System;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Core;

public sealed class ReflexOptions
{
    public IReflexScheduler Scheduler { get; set; } = new ThreadPoolScheduler();

    public Action<Exception, EffectExceptionInfo> OnEffectException { get; set; } =
        static (raisedException, exceptionInfo) =>
            Console.Error.WriteLine(
                $"Rymote.Reflex effect '{exceptionInfo.DebugName ?? "<unnamed>"}' threw " +
                $"after {exceptionInfo.ConsecutiveFailures} consecutive failures: {raisedException}");

    public int MaxReentrantRunsPerTick { get; set; } = 100;
}
