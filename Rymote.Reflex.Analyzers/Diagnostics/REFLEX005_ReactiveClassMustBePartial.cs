using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Rymote.Reflex.Analyzers.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReactiveClassMustBePartialAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        id: "REFLEX005",
        title: "[Reactive] class must be partial",
        messageFormat: "Class '{0}' is decorated with [Reactive] but is not partial",
        category: "Rymote.Reflex.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext analysisContext)
    {
        analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        analysisContext.EnableConcurrentExecution();
        analysisContext.RegisterSymbolAction(InspectClass, SymbolKind.NamedType);
    }

    private static void InspectClass(SymbolAnalysisContext typeContext)
    {
        if (typeContext.Symbol is not INamedTypeSymbol classSymbol) return;
        if (classSymbol.TypeKind != TypeKind.Class) return;

        bool isReactive = false;
        foreach (AttributeData attribute in classSymbol.GetAttributes())
            if (attribute.AttributeClass?.ToDisplayString() == "Rymote.Reflex.Attributes.ReactiveAttribute")
            { isReactive = true; break; }

        if (!isReactive) return;

        bool isPartial = false;
        foreach (SyntaxReference declarationReference in classSymbol.DeclaringSyntaxReferences)
            if (declarationReference.GetSyntax() is ClassDeclarationSyntax classDeclaration
                && classDeclaration.Modifiers.Any(token => token.Text == "partial"))
            { isPartial = true; break; }

        if (isPartial) return;

        typeContext.ReportDiagnostic(Diagnostic.Create(
            Descriptor,
            classSymbol.Locations[0],
            classSymbol.Name));
    }
}
