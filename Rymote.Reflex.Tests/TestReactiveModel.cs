using Rymote.Reflex.Attributes;

namespace Rymote.Reflex.Tests;

[Reactive]
public partial class TestReactiveModel
{
    public partial int Counter { get; set; }
    public partial string Label { get; set; }
}
