using Rymote.Reflex.Attributes;

namespace Playground.Models;

[Reactive]
public partial class UserSession
{
    public partial System.Guid UserId { get; set; }
    public partial string UserName { get; set; }
    public partial System.DateTime LastSeenAt { get; set; }
}
