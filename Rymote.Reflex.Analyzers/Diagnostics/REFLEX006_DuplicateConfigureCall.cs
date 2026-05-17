using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Rymote.Reflex.Analyzers.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicateConfigureCallAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        id: "REFLEX006",
        title: "More than one Reflex.Configure call site",
        messageFormat: "Reflex.Configure is called more than once; only the last call takes effect",
        category: "Rymote.Reflex.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: null,
        helpLinkUri: null,
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext analysisContext)
    {
        analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        analysisContext.EnableConcurrentExecution();

        analysisContext.RegisterCompilationStartAction(compilationStartContext =>
        {
            ConcurrentBag<Location> configureCallLocations = new();

            compilationStartContext.RegisterOperationAction(
                operationContext => CollectConfigureCallLocation(operationContext, configureCallLocations),
                OperationKind.Invocation);

            compilationStartContext.RegisterCompilationEndAction(
                compilationEndContext => ReportDuplicateLocations(compilationEndContext, configureCallLocations));
        });
    }

    private static void CollectConfigureCallLocation(
        OperationAnalysisContext operationContext,
        ConcurrentBag<Location> configureCallLocations)
    {
        if (operationContext.Operation is not IInvocationOperation invocationOperation) return;

        IMethodSymbol targetMethod = invocationOperation.TargetMethod;
        if (targetMethod.Name != "Configure") return;
        if (targetMethod.ContainingType?.ToDisplayString() != "Rymote.Reflex.Reflex") return;
        if (targetMethod.Parameters.Length != 1) return;

        IParameterSymbol firstParameter = targetMethod.Parameters[0];
        string parameterTypeName = firstParameter.Type.ToDisplayString();
        if (!parameterTypeName.StartsWith("System.Action<") && parameterTypeName != "System.Action") return;

        configureCallLocations.Add(invocationOperation.Syntax.GetLocation());
    }

    private static void ReportDuplicateLocations(
        CompilationAnalysisContext compilationEndContext,
        ConcurrentBag<Location> configureCallLocations)
    {
        Location[] allLocations = configureCallLocations.ToArray();
        if (allLocations.Length <= 1) return;

        foreach (Location duplicateLocation in allLocations.Skip(1))
            compilationEndContext.ReportDiagnostic(Diagnostic.Create(Descriptor, duplicateLocation));
    }
}
