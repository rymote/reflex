using System.Collections.Generic;
using Rymote.Reflex.Collections;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class ReactiveListTests
{
    public ReactiveListTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void Add_increases_Count_and_triggers_count_dependent_effect()
    {
        ReactiveListWrapper<string> messages = new(new List<string>());
        int observedCount = -1;

        using System.IDisposable subscription = Reflex.Effect(() => observedCount = messages.Count);

        messages.Add("hello");
        Assert.Equal(1, observedCount);

        messages.Add("world");
        Assert.Equal(2, observedCount);
    }

    [Fact]
    public void Index_set_triggers_only_index_dependent_effect()
    {
        ReactiveListWrapper<string> messages = new(new List<string> { "a", "b", "c" });
        int countEffectRuns = 0;
        string indexZeroSnapshot = "";

        using System.IDisposable countSubscription = Reflex.Effect(() =>
        {
            _ = messages.Count;
            countEffectRuns++;
        });
        using System.IDisposable indexSubscription = Reflex.Effect(() =>
        {
            indexZeroSnapshot = messages[0];
        });

        int countRunsBeforeIndexSet = countEffectRuns;
        messages[0] = "replacement";

        Assert.Equal("replacement", indexZeroSnapshot);
        Assert.Equal(countRunsBeforeIndexSet, countEffectRuns);
    }

    [Fact]
    public void Foreach_tracks_iteration_slot_and_rebuilds_on_any_change()
    {
        ReactiveListWrapper<int> values = new(new List<int> { 1, 2, 3 });
        int observedSum = 0;

        using System.IDisposable subscription = Reflex.Effect(() =>
        {
            int runningSum = 0;
            foreach (int value in values) runningSum += value;
            observedSum = runningSum;
        });

        Assert.Equal(6, observedSum);

        values.Add(4);
        Assert.Equal(10, observedSum);

        values.RemoveAt(0);
        Assert.Equal(9, observedSum);
    }
}
