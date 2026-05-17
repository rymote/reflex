using Microsoft.CodeAnalysis;

namespace Rymote.Reflex.Generators.Diagnostics;

internal static class GeneratorDiagnostics
{
    internal static readonly DiagnosticDescriptor ReactiveClassMustBePartial = new(
        id: "REFLEX005",
        title: "[Reactive] class must be partial",
        messageFormat: "Class '{0}' is decorated with [Reactive] but is not partial. The source generator cannot extend it.",
        category: "Rymote.Reflex.Generation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor ReactiveClassMustNotBeStatic = new(
        id: "REFLEX008",
        title: "[Reactive] class must not be static",
        messageFormat: "Class '{0}' is decorated with [Reactive] but is static. Static classes cannot be made reactive.",
        category: "Rymote.Reflex.Generation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
