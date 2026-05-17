using Rymote.Reflex.Scheduling;

namespace Rymote.Reflex.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void ConfigureForTests_replaces_active_scheduler()
    {
        SynchronousTestScheduler synchronousScheduler = new();
        Reflex.ConfigureForTests(options => options.Scheduler = synchronousScheduler);

        Assert.Same(synchronousScheduler, Reflex.CurrentOptions.Scheduler);
    }

    [Fact]
    public void Default_scheduler_is_ThreadPoolScheduler()
    {
        Reflex.ConfigureForTests(options => options.Scheduler = new ThreadPoolScheduler());

        Assert.IsType<ThreadPoolScheduler>(Reflex.CurrentOptions.Scheduler);
    }
}
