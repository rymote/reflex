using System;
using Playground.Models;

namespace Playground.Scenarios;

public static class BroadcastHubScenario
{
    public static void Run()
    {
        Console.WriteLine("[BroadcastHubScenario] starting");

        ChatRoom chatRoom = new() { RoomName = "general" };
        using IDisposable subscription = Rymote.Reflex.Reflex.Effect(() =>
            Console.WriteLine($"  {chatRoom.RoomName} has {chatRoom.Messages.Count} messages"));

        chatRoom.Messages.Add("hello");
        chatRoom.Messages.Add("world");
    }
}
