using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Rymote.Reflex.Analyzers.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InnerCollectionEscapesAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        id: "REFLEX004",
        title: "Inner BCL collection passed to Reflex.Reactive escapes the wrapper",
        messageFormat: "BCL collection '{0}' was wrapped by Reflex.Reactive(...) and is being used elsewhere. Mutating the original instance bypasses the reactive wrapper.",
        category: "Rymote.Reflex.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext analysisContext)
    {
        analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        analysisContext.EnableConcurrentExecution();

        analysisContext.RegisterCompilationStartAction(compilationStartContext =>
        {
            ConcurrentDictionary<ISymbol, Location> wrappedSymbols =
                new(SymbolEqualityComparer.Default);

            compilationStartContext.RegisterOperationAction(
                operationContext => AnalyzeInvocationForWrapping(operationContext, wrappedSymbols),
                OperationKind.Invocation);

            compilationStartContext.RegisterOperationAction(
                operationContext => AnalyzeMemberReferenceForEscape(operationContext, wrappedSymbols),
                OperationKind.FieldReference,
                OperationKind.LocalReference,
                OperationKind.ParameterReference);
        });
    }

    private static void AnalyzeInvocationForWrapping(
        OperationAnalysisContext operationContext,
        ConcurrentDictionary<ISymbol, Location> wrappedSymbols)
    {
        if (operationContext.Operation is not IInvocationOperation invocation)
            return;

        IMethodSymbol targetMethod = invocation.TargetMethod;
        if (targetMethod.Name != "Reactive")
            return;

        INamedTypeSymbol? containingType = targetMethod.ContainingType;
        if (containingType is null)
            return;

        if (containingType.Name != "Reflex" || containingType.ContainingNamespace?.ToDisplayString() != "Rymote.Reflex")
            return;

        if (invocation.Arguments.Length == 0)
            return;

        IOperation argumentOperation = invocation.Arguments[0].Value;
        ISymbol? argumentSymbol = ExtractReferencedSymbol(argumentOperation);
        if (argumentSymbol is null)
            return;

        wrappedSymbols.TryAdd(argumentSymbol, invocation.Syntax.GetLocation());
    }

    private static void AnalyzeMemberReferenceForEscape(
        OperationAnalysisContext operationContext,
        ConcurrentDictionary<ISymbol, Location> wrappedSymbols)
    {
        ISymbol? referencedSymbol = ExtractReferencedSymbol(operationContext.Operation);
        if (referencedSymbol is null)
            return;

        if (!wrappedSymbols.TryGetValue(referencedSymbol, out Location? wrapLocation))
            return;

        Location currentLocation = operationContext.Operation.Syntax.GetLocation();
        if (currentLocation.Equals(wrapLocation))
            return;

        if (IsDirectArgumentToReactive(operationContext.Operation))
            return;

        operationContext.ReportDiagnostic(
            Diagnostic.Create(Descriptor, currentLocation, referencedSymbol.Name));
    }

    private static bool IsDirectArgumentToReactive(IOperation operation)
    {
        if (operation.Parent is IArgumentOperation argument &&
            argument.Parent is IInvocationOperation parentInvocation)
        {
            INamedTypeSymbol? containingType = parentInvocation.TargetMethod.ContainingType;
            return parentInvocation.TargetMethod.Name == "Reactive" &&
                   containingType?.Name == "Reflex" &&
                   containingType.ContainingNamespace?.ToDisplayString() == "Rymote.Reflex";
        }
        return false;
    }

    private static ISymbol? ExtractReferencedSymbol(IOperation? operation)
    {
        return operation switch
        {
            ILocalReferenceOperation localReference => localReference.Local,
            IFieldReferenceOperation fieldReference => fieldReference.Field,
            IParameterReferenceOperation parameterReference => parameterReference.Parameter,
            _ => null
        };
    }
}
