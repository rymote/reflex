using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rymote.Reflex.Analyzers.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FieldOnReactiveClassAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        id: "REFLEX001",
        title: "Public field on [Reactive] class is not tracked",
        messageFormat: "Field '{0}' on [Reactive] class '{1}' will not be tracked. Convert it to a property.",
        category: "Rymote.Reflex.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext analysisContext)
    {
        analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        analysisContext.EnableConcurrentExecution();
        analysisContext.RegisterSymbolAction(InspectField, SymbolKind.Field);
    }

    private static void InspectField(SymbolAnalysisContext fieldContext)
    {
        if (fieldContext.Symbol is not IFieldSymbol fieldSymbol) return;
        if (fieldSymbol.IsImplicitlyDeclared || fieldSymbol.IsStatic) return;
        if (fieldSymbol.ContainingType is not INamedTypeSymbol containingType) return;

        bool containingTypeIsReactive = false;
        foreach (AttributeData attribute in containingType.GetAttributes())
            if (attribute.AttributeClass?.ToDisplayString() == "Rymote.Reflex.Attributes.ReactiveAttribute")
            { containingTypeIsReactive = true; break; }

        if (!containingTypeIsReactive) return;

        fieldContext.ReportDiagnostic(Diagnostic.Create(
            Descriptor,
            fieldSymbol.Locations[0],
            fieldSymbol.Name,
            containingType.Name));
    }
}
