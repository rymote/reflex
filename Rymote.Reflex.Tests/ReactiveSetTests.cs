using System;
using System.Collections.Generic;
using Rymote.Reflex.Collections;
using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class ReactiveSetTests
{
    public ReactiveSetTests()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new SynchronousTestScheduler());
    }

    [Fact]
    public void Per_value_tracking_only_re_runs_relevant_effect()
    {
        ReactiveSetWrapper<string> onlineUsernames = new(new HashSet<string>());
        bool aliceOnlineObserved = false;
        int bobEffectRunCount = 0;

        using IDisposable aliceSubscription = Reflex.Effect(() =>
            aliceOnlineObserved = onlineUsernames.Contains("alice"));
        using IDisposable bobSubscription = Reflex.Effect(() =>
        {
            _ = onlineUsernames.Contains("bob");
            bobEffectRunCount++;
        });

        int bobRunsBeforeAliceAdd = bobEffectRunCount;
        onlineUsernames.Add("alice");

        Assert.True(aliceOnlineObserved);
        Assert.Equal(bobRunsBeforeAliceAdd, bobEffectRunCount);
    }

    [Fact]
    public void Value_slots_are_dropped_after_removing_all_items()
    {
        ReactiveSetWrapper<int> numbers = new(new HashSet<int>());

        for (int itemIndex = 0; itemIndex < 100; itemIndex++)
        {
            numbers.Add(itemIndex);
            _ = numbers.Contains(itemIndex);
        }

        Assert.Equal(100, numbers.InternalTrackedSlotCount);

        for (int itemIndex = 0; itemIndex < 100; itemIndex++)
            numbers.Remove(itemIndex);

        Assert.Equal(0, numbers.InternalTrackedSlotCount);
    }

    [Fact]
    public void Value_slots_are_dropped_on_clear()
    {
        ReactiveSetWrapper<int> numbers = new(new HashSet<int>());

        for (int itemIndex = 0; itemIndex < 100; itemIndex++)
        {
            numbers.Add(itemIndex);
            _ = numbers.Contains(itemIndex);
        }

        Assert.Equal(100, numbers.InternalTrackedSlotCount);

        numbers.Clear();

        Assert.Equal(0, numbers.InternalTrackedSlotCount);
    }
}
