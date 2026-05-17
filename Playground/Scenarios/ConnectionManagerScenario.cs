using System;
using System.Collections.Generic;
using Playground.Models;
using Rymote.Reflex.Collections;

namespace Playground.Scenarios;

public static class ConnectionManagerScenario
{
    public static void Run()
    {
        Console.WriteLine("[ConnectionManagerScenario] starting");

        ReactiveDictionaryWrapper<Guid, UserSession> sessions = new(new Dictionary<Guid, UserSession>());

        using IDisposable countSubscription = Rymote.Reflex.Reflex.Effect(() =>
            Console.WriteLine($"  active sessions: {sessions.Count}"));

        Guid aliceId = Guid.NewGuid();
        sessions[aliceId] = new UserSession { UserId = aliceId, UserName = "alice" };
        sessions[Guid.NewGuid()] = new UserSession { UserName = "bob" };
        sessions.Remove(aliceId);
    }
}
