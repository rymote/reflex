using System;

namespace Rymote.Reflex.Attributes;

/// <summary>Marks a <see langword="partial"/> class for Reflex source generation.
/// The generator will emit reactive backing fields and notification logic for all eligible properties.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ReactiveAttribute : Attribute
{
}
