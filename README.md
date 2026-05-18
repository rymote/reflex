<div align="center">
    <a href="https://github.com/rymote/reflex"><img src="https://github.com/rymote/reflex/blob/master/.github/rymote-reflex-cover.png" alt="rymote/reflex" /></a>
</div>
<br />

<div align="center">
  Rymote.Reflex - Fine-grained reactive primitives for .NET
</div>

<div align="center">
  <sub>
    Brought to you by
    <a href="https://github.com/jovanivanovic">@jovanivanovic</a>,
    <a href="https://github.com/rymote">@rymote</a>
  </sub>
</div>

## Overview

Rymote.Reflex brings fine-grained reactivity to server-side .NET 10. It provides `Ref<T>`, `Computed<T>`, `Effect`, `Watch`, reactive collection wrappers, and a Roslyn source generator that turns plain `[Reactive]` POCO classes into fully-tracked objects — with no UI framework dependency.

The library is designed for scenarios where you need observable state that drives derived computations and side effects: real-time backends, game servers, rule engines, data pipelines, and any other server-side workload that benefits from push-based reactivity without polling.

Reactivity is tracked at the sub-property level. Reading `session.UserName` inside an effect subscribes only to `UserName`, not to the entire object. Writes notify only the effects that actually read the changed value, keeping CPU overhead proportional to the number of observers rather than the size of the object graph.

## Features

- **Fine-grained tracking** — per-property and per-index dependency slots; no dirty-checking or full-tree diffing.
- **Lazy computed values** — `Computed<T>` caches results and re-evaluates only when a dependency changes.
- **Effect system** — effects run automatically when their reactive reads change; self-disposing effects supported.
- **Watch subscriptions** — `Watch` fires `(oldValue, newValue)` callbacks on `Ref`, `Computed`, or any selector.
- **Reactive collections** — `Reflex.Reactive(list/dict/set)` wraps BCL collections with per-index/per-key tracking and `IObservable` change streams.
- **[Reactive] source generator** — annotate a `partial` class; the generator emits reactive backing fields and `Compute*` method wiring at compile time.
- **Roslyn analyzers** — seven built-in diagnostics catch common mistakes (missing `partial`, escaping inner collections, mutation during iteration, etc.).
- **IObservable / IAsyncEnumerable interop** — convert any reactive primitive to a standard stream.
- **Concurrency-safe** — per-source write locks; `AsyncLocal`-flowed dependency tracking; batching via `Reflex.Batch()`.
- **Test-friendly** — swap in `SynchronousTestScheduler` for deterministic, single-threaded test runs.

## Installation

```bash
dotnet add package Rymote.Reflex
```

The package bundles the runtime, the source generator, and the analyzers. No additional references are required.

## Quick start

### Ref + Effect

```csharp
using Rymote.Reflex;

Ref<int> counter = new(0);

using IDisposable subscription = Reflex.Effect(() =>
    Console.WriteLine($"Counter is {counter.Value}"));
// Output: Counter is 0

counter.Value = 1;  // Output: Counter is 1
counter.Value = 2;  // Output: Counter is 2
```

### Computed

```csharp
Ref<string> firstName = new("Ada");
Ref<string> lastName  = new("Lovelace");

Computed<string> fullName = Reflex.Computed(() => $"{firstName.Value} {lastName.Value}");

Console.WriteLine(fullName.Value);  // Ada Lovelace

lastName.Value = "Byron";
Console.WriteLine(fullName.Value);  // Ada Byron
```

### [Reactive] POCO with source generator

```csharp
using Rymote.Reflex.Attributes;

[Reactive]
public partial class UserSession
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;

    // Getter-only partial property — generator wires ComputeFullName() as a Computed<string>
    public partial string FullName { get; }
    private string ComputeFullName() => $"{FirstName} {LastName}";
}

// Usage
UserSession session = new();
using IDisposable subscription = Reflex.Effect(() =>
    Console.WriteLine($"Full name: {session.FullName}"));

session.FirstName = "Grace";
session.LastName  = "Hopper";
// Output: Full name: Grace Hopper
```

### Reactive collections

```csharp
IList<string> messages = Reflex.Reactive(new List<string>());

using IDisposable subscription = Reflex.Effect(() =>
    Console.WriteLine($"Message count: {messages.Count}"));

messages.Add("Hello");   // Output: Message count: 1
messages.Add("World");   // Output: Message count: 2
messages.RemoveAt(0);    // Output: Message count: 1
```

## API surface

| Method | Description |
|---|---|
| `Reflex.Effect(action)` | Run an action reactively; re-runs when dependencies change |
| `Reflex.Computed(func)` | Create a lazy derived value |
| `Reflex.Watch(source, handler)` | Subscribe to value changes with `(old, new)` callbacks |
| `Reflex.Reactive(list/dict/set)` | Wrap a BCL collection in a reactive proxy |
| `Reflex.Batch()` | Defer effect notifications until the returned handle is disposed |
| `Reflex.Scope()` | Create an `EffectScope` that disposes child effects together |
| `Reflex.WatchSyncEffect(action)` | Synchronous watch effect; runs on the writer thread |
| `Reflex.WatchPostEffect(action)` | Post-tick effect; runs after normal effects complete |
| `Reflex.ToRef(source, selector)` | Project a reactive property into a standalone `Ref<T>` |
| `Reflex.Unref(candidate)` | Unwrap a `Ref`, `ReadOnlyRef`, or `Computed` to its raw value |
| `Reflex.IsRef/IsComputed/IsReactive` | Runtime type guards |

## Concurrency model

Each reactive source (`Ref<T>`, collection slot, etc.) holds a private write lock. Reads outside a lock register a dependency via an `AsyncLocal`-flowed effect stack, so dependency tracking flows correctly across `await` boundaries without locking. Writes acquire the source lock, update the value, then notify subscribers outside the lock to avoid deadlocks.

Batching (`Reflex.Batch()`) pushes a frame onto a thread-local batch stack. While a frame is active, notifications are accumulated rather than dispatched. When the outermost frame is disposed, all pending notifications are flushed once, coalescing redundant re-runs.

The scheduler abstraction (`IReflexScheduler`) controls when effects actually execute. The default `ThreadPoolScheduler` queues normal effects and post-tick effects on the thread pool. Swap in `SynchronousTestScheduler` during testing for single-threaded, deterministic execution.

## Source generator

Annotate any `partial` class with `[Reactive]`. The generator produces a second `partial` file (e.g. `MyNamespace.MyClass.Reflex.g.cs`) that:

- Replaces each eligible auto-property with a reactive backing field and `Ref<T>`-based get/set.
- Wires any `partial` getter-only property whose name follows the `ComputeXxx` convention to a `Computed<T>` backed by the corresponding private method.
- Wraps `List<T>`, `Dictionary<TKey, TValue>`, and `HashSet<T>` properties in their reactive collection equivalents.
- Makes the class implement `IReactive` for runtime and analyzer detection.

Use `[ReactiveIgnore]` on a property to opt it out of generation.

```csharp
[Reactive]
public partial class ShoppingCart
{
    public string CustomerId { get; set; } = string.Empty;

    [ReactiveIgnore]
    public DateTime CreatedAt { get; set; }  // skipped — not reactive

    public List<string> Items { get; set; } = new();  // wrapped as ReactiveListWrapper<string>

    public partial int ItemCount { get; }
    private int ComputeItemCount() => Items.Count;
}
```

## Analyzers

| ID | Severity | Description |
|---|---|---|
| REFLEX001 | Warning | Public field on a `[Reactive]` class is not tracked; convert to a property |
| REFLEX002 | Info | Collection property type will shift from concrete to interface after generation |
| REFLEX003 | Warning | Mutation of a collection inside a `foreach` loop over that same collection |
| REFLEX004 | Warning | BCL collection passed to `Reflex.Reactive` is used after wrapping, bypassing the proxy |
| REFLEX005 | Error | `[Reactive]` class must be declared `partial` |
| REFLEX006 | Warning | More than one `Reflex.Configure` call site detected |
| REFLEX007 | Info | `Reflex.Effect` lambda captures `this` of a `[Reactive]` class but reads no reactive property |

## Support the project

If Reflex has helped you ship faster, please consider supporting ongoing development:

- [Patreon](https://www.patreon.com/rymote)
- [Open Collective](https://opencollective.com/rymote)

## License

This project is licensed under the BSD 3-Clause License — see [LICENSE.md](./LICENSE.md) for details.

## Authors

- [@jovanivanovic](https://github.com/jovanivanovic)
- [@rymote](https://github.com/rymote)
