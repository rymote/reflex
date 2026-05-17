using System;
using System.Collections.Generic;
using Rymote.Reflex.Collections;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class ReactiveDictionaryTests
{
    public ReactiveDictionaryTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void Per_key_tracking_only_re_runs_relevant_effect()
    {
        ReactiveDictionaryWrapper<Guid, string> sessions = new(new Dictionary<Guid, string>());
        Guid aliceId = Guid.NewGuid();
        Guid bobId = Guid.NewGuid();
        string aliceObserved = "";
        int bobEffectRunCount = 0;

        using IDisposable aliceSubscription = Reflex.Effect(() =>
        {
            sessions.TryGetValue(aliceId, out string? aliceSession);
            aliceObserved = aliceSession ?? "";
        });
        using IDisposable bobSubscription = Reflex.Effect(() =>
        {
            sessions.TryGetValue(bobId, out string? bobSession);
            bobEffectRunCount++;
        });

        int bobRunsBeforeAliceSet = bobEffectRunCount;
        sessions[aliceId] = "alice-session";

        Assert.Equal("alice-session", aliceObserved);
        Assert.Equal(bobRunsBeforeAliceSet, bobEffectRunCount);
    }

    [Fact]
    public void Count_dependency_re_runs_on_add_and_remove()
    {
        ReactiveDictionaryWrapper<int, string> sessions = new(new Dictionary<int, string>());
        int observedCount = -1;

        using IDisposable subscription = Reflex.Effect(() => observedCount = sessions.Count);

        sessions[1] = "a";
        Assert.Equal(1, observedCount);

        sessions[2] = "b";
        Assert.Equal(2, observedCount);

        sessions.Remove(1);
        Assert.Equal(1, observedCount);
    }

    [Fact]
    public void Key_slots_are_dropped_after_removing_all_entries()
    {
        ReactiveDictionaryWrapper<int, string> lookup = new(new Dictionary<int, string>());

        for (int entryIndex = 0; entryIndex < 100; entryIndex++)
        {
            lookup[entryIndex] = entryIndex.ToString();
            _ = lookup.ContainsKey(entryIndex);
        }

        Assert.Equal(100, lookup.InternalTrackedSlotCount);

        for (int entryIndex = 0; entryIndex < 100; entryIndex++)
            lookup.Remove(entryIndex);

        Assert.Equal(0, lookup.InternalTrackedSlotCount);
    }

    [Fact]
    public void Key_slots_are_dropped_on_clear()
    {
        ReactiveDictionaryWrapper<int, string> lookup = new(new Dictionary<int, string>());

        for (int entryIndex = 0; entryIndex < 100; entryIndex++)
        {
            lookup[entryIndex] = entryIndex.ToString();
            _ = lookup.ContainsKey(entryIndex);
        }

        Assert.Equal(100, lookup.InternalTrackedSlotCount);

        lookup.Clear();

        Assert.Equal(0, lookup.InternalTrackedSlotCount);
    }
}
