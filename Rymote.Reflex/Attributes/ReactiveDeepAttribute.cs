using System;

namespace Rymote.Reflex.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ReactiveDeepAttribute : Attribute
{
}
