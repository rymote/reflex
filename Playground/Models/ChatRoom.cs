using System.Collections.Generic;
using Rymote.Reflex.Attributes;

namespace Playground.Models;

[Reactive]
public partial class ChatRoom
{
    public partial string RoomName { get; set; }
    public List<string> Messages { get; set; } = new();
    public Dictionary<System.Guid, UserSession> Participants { get; set; } = new();
}
