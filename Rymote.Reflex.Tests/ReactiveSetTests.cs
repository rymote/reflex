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
}
