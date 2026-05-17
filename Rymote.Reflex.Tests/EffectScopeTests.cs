using Rymote.Reflex.Core;
using Rymote.Reflex.Primitives;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class EffectScopeTests
{
    public EffectScopeTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void Disposing_scope_tears_down_all_attached_effects()
    {
        Ref<int> activeConnectionCount = new(0);
        int firstObservedValue = -1;
        int secondObservedValue = -1;

        EffectScope groupingScope = Reflex.Scope();
        groupingScope.Effect(() => firstObservedValue = activeConnectionCount.Value);
        groupingScope.Effect(() => secondObservedValue = activeConnectionCount.Value);

        groupingScope.Dispose();

        activeConnectionCount.Value = 42;

        Assert.Equal(0, firstObservedValue);
        Assert.Equal(0, secondObservedValue);
    }

    [Fact]
    public void Nested_scope_is_torn_down_when_parent_disposes()
    {
        Ref<int> activeConnectionCount = new(0);
        int observedValueFromNestedEffect = -1;

        EffectScope parentScope = Reflex.Scope();
        EffectScope childScope = parentScope.CreateChildScope();
        childScope.Effect(() => observedValueFromNestedEffect = activeConnectionCount.Value);

        parentScope.Dispose();

        activeConnectionCount.Value = 99;
        Assert.Equal(0, observedValueFromNestedEffect);
    }
}
