using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Rymote.Reflex.Analyzers.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EffectWithoutReactiveReadAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Descriptor = new(
        id: "REFLEX007",
        title: "Effect closure captures 'this' of a [Reactive] class but reads no property",
        messageFormat: "The Effect lambda captures 'this' of [Reactive] class '{0}' but does not read any reactive property, so the effect will never re-run",
        category: "Rymote.Reflex.Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext analysisContext)
    {
        analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        analysisContext.EnableConcurrentExecution();

        analysisContext.RegisterOperationAction(AnalyzeEffectInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeEffectInvocation(OperationAnalysisContext operationContext)
    {
        if (operationContext.Operation is not IInvocationOperation invocation)
            return;

        if (invocation.TargetMethod.Name != "Effect")
            return;

        INamedTypeSymbol? containingType = invocation.TargetMethod.ContainingType;
        if (containingType is null)
            return;

        if (containingType.Name != "Reflex" || containingType.ContainingNamespace?.ToDisplayString() != "Rymote.Reflex")
            return;

        if (invocation.Arguments.Length == 0)
            return;

        IOperation firstArgument = invocation.Arguments[0].Value;
        if (firstArgument is not IAnonymousFunctionOperation lambdaOperation)
            return;

        INamedTypeSymbol? capturedReactiveType = FindCapturedReactiveThisType(lambdaOperation, operationContext.Compilation);
        if (capturedReactiveType is null)
            return;

        bool hasAnyReactivePropertyRead = LambdaBodyReadsReactiveProperty(lambdaOperation, operationContext.Compilation);
        if (hasAnyReactivePropertyRead)
            return;

        operationContext.ReportDiagnostic(
            Diagnostic.Create(Descriptor, firstArgument.Syntax.GetLocation(), capturedReactiveType.Name));
    }

    private static INamedTypeSymbol? FindCapturedReactiveThisType(
        IAnonymousFunctionOperation lambdaOperation,
        Compilation compilation)
    {
        INamedTypeSymbol? reactiveAttributeType = compilation.GetTypeByMetadataName("Rymote.Reflex.Attributes.ReactiveAttribute");
        INamedTypeSymbol? iReactiveType = compilation.GetTypeByMetadataName("Rymote.Reflex.Utilities.IReactive");

        foreach (IOperation descendant in lambdaOperation.Descendants())
        {
            if (descendant is not IInstanceReferenceOperation instanceReference)
                continue;

            ITypeSymbol? instanceType = instanceReference.Type;
            if (instanceType is not INamedTypeSymbol namedInstanceType)
                continue;

            bool hasReactiveAttribute = reactiveAttributeType is not null &&
                namedInstanceType.GetAttributes()
                    .Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, reactiveAttributeType));

            bool implementsIReactive = iReactiveType is not null &&
                namedInstanceType.AllInterfaces
                    .Any(implementedInterface => SymbolEqualityComparer.Default.Equals(implementedInterface, iReactiveType));

            if (hasReactiveAttribute || implementsIReactive)
                return namedInstanceType;
        }

        return null;
    }

    private static bool LambdaBodyReadsReactiveProperty(
        IAnonymousFunctionOperation lambdaOperation,
        Compilation compilation)
    {
        INamedTypeSymbol? reactiveAttributeType = compilation.GetTypeByMetadataName("Rymote.Reflex.Attributes.ReactiveAttribute");
        INamedTypeSymbol? iReactiveType = compilation.GetTypeByMetadataName("Rymote.Reflex.Utilities.IReactive");

        foreach (IOperation descendant in lambdaOperation.Descendants())
        {
            if (descendant is not IPropertyReferenceOperation propertyReference)
                continue;

            INamedTypeSymbol? propertyContainingType = propertyReference.Property.ContainingType;
            if (propertyContainingType is null)
                continue;

            bool containingTypeHasReactiveAttribute = reactiveAttributeType is not null &&
                propertyContainingType.GetAttributes()
                    .Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, reactiveAttributeType));

            bool containingTypeImplementsIReactive = iReactiveType is not null &&
                propertyContainingType.AllInterfaces
                    .Any(implementedInterface => SymbolEqualityComparer.Default.Equals(implementedInterface, iReactiveType));

            if (containingTypeHasReactiveAttribute || containingTypeImplementsIReactive)
                return true;
        }

        return false;
    }
}
