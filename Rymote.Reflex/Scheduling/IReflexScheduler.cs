using Rymote.Reflex.Core;

namespace Rymote.Reflex.Scheduling;

public interface IReflexScheduler
{
    void Schedule(ReactiveEffect effect);
    void FlushPendingNow();
}
