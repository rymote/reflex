namespace Rymote.Reflex.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void Facade_type_is_accessible()
    {
        System.Type facadeType = typeof(Reflex);
        Assert.NotNull(facadeType);
    }
}
