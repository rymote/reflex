using System;

namespace Rymote.Reflex.Attributes;

/// <summary>Excludes a property from Reflex source generation on a <see cref="ReactiveAttribute"/>-annotated class.
/// Apply to properties that should retain their original implementation without reactive backing.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ReactiveIgnoreAttribute : Attribute
{
}
