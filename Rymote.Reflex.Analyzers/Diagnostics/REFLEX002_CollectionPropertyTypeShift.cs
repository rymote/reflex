using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rymote.Reflex.Analyzers.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CollectionPropertyTypeShiftAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        id: "REFLEX002",
        title: "Collection property on [Reactive] class will be exposed as the standard interface",
        messageFormat: "Property '{0}' on [Reactive] class '{1}' will be exposed as the standard collection interface in the generated partial. Reflection on the declared type will not see the wrapper.",
        category: "Rymote.Reflex.Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext analysisContext)
    {
        analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        analysisContext.EnableConcurrentExecution();
        analysisContext.RegisterSymbolAction(InspectProperty, SymbolKind.Property);
    }

    private static void InspectProperty(SymbolAnalysisContext propertyContext)
    {
        if (propertyContext.Symbol is not IPropertySymbol propertySymbol) return;
        if (propertySymbol.ContainingType is not INamedTypeSymbol containingType) return;

        bool containingTypeIsReactive = false;
        foreach (AttributeData attribute in containingType.GetAttributes())
            if (attribute.AttributeClass?.ToDisplayString() == "Rymote.Reflex.Attributes.ReactiveAttribute")
            { containingTypeIsReactive = true; break; }

        if (!containingTypeIsReactive) return;

        if (propertySymbol.Type is not INamedTypeSymbol propertyType) return;

        string genericDefinitionName = propertyType.ConstructedFrom?.ToDisplayString() ?? string.Empty;
        bool isTrackedCollectionType =
            genericDefinitionName == "System.Collections.Generic.List<T>"
            || genericDefinitionName == "System.Collections.Generic.Dictionary<TKey, TValue>"
            || genericDefinitionName == "System.Collections.Generic.HashSet<T>";

        if (!isTrackedCollectionType) return;

        propertyContext.ReportDiagnostic(Diagnostic.Create(
            Descriptor,
            propertySymbol.Locations[0],
            propertySymbol.Name,
            containingType.Name));
    }
}
