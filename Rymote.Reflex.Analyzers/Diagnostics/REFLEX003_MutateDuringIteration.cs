using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Rymote.Reflex.Analyzers.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MutateDuringIterationAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> MutationMethodNames = ImmutableHashSet.Create(
        "Add", "AddRange", "Remove", "RemoveAt", "Clear", "Insert");

    internal static readonly DiagnosticDescriptor Descriptor = new(
        id: "REFLEX003",
        title: "Mutation during iteration of a reactive collection",
        messageFormat: "Mutation of reactive collection '{0}' inside a foreach loop over that collection will throw at runtime",
        category: "Rymote.Reflex.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext analysisContext)
    {
        analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        analysisContext.EnableConcurrentExecution();

        analysisContext.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext operationContext)
    {
        if (operationContext.Operation is not IInvocationOperation invocation)
            return;

        if (!MutationMethodNames.Contains(invocation.TargetMethod.Name))
            return;

        ISymbol? mutationTargetSymbol = ExtractReferencedSymbol(invocation.Instance);
        if (mutationTargetSymbol is null)
            return;

        IForEachLoopOperation? enclosingForEach = FindEnclosingForEach(invocation);
        if (enclosingForEach is null)
            return;

        ISymbol? iteratedCollectionSymbol = ExtractReferencedSymbol(enclosingForEach.Collection);
        if (iteratedCollectionSymbol is null)
            return;

        if (!SymbolEqualityComparer.Default.Equals(mutationTargetSymbol, iteratedCollectionSymbol))
            return;

        operationContext.ReportDiagnostic(
            Diagnostic.Create(Descriptor, invocation.Syntax.GetLocation(), mutationTargetSymbol.Name));
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

    private static IForEachLoopOperation? FindEnclosingForEach(IOperation operation)
    {
        IOperation? current = operation.Parent;
        while (current is not null)
        {
            if (current is IForEachLoopOperation forEachLoop)
                return forEachLoop;
            current = current.Parent;
        }
        return null;
    }
}
