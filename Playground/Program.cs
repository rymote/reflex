using System.Threading;
using Playground.Scenarios;
using Rymote.Reflex.Scheduling;

Rymote.Reflex.Reflex.Configure(options => options.Scheduler = new ThreadPoolScheduler());

ConnectionManagerScenario.Run();
Thread.Sleep(50);

MetricsEmitterScenario.Run();
Thread.Sleep(50);

RateLimiterScenario.Run();
Thread.Sleep(50);

BroadcastHubScenario.Run();
Thread.Sleep(50);

ParallelWritersScenario.Run();
