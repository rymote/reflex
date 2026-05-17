using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Rymote.Reflex.Generators.Analysis;
using Rymote.Reflex.Generators.Diagnostics;
using Rymote.Reflex.Generators.Emission;

namespace Rymote.Reflex.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class ReactiveSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initializationContext)
    {
        IncrementalValuesProvider<(ReactiveTypeModel? Model, Diagnostic? GeneratorDiagnostic)> resultsProvider =
            initializationContext
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    "Rymote.Reflex.Attributes.ReactiveAttribute",
                    predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax,
                    transform: static (attributeContext, _) => BuildModel(attributeContext));

        initializationContext.RegisterSourceOutput(resultsProvider, (productionContext, result) =>
        {
            if (result.GeneratorDiagnostic is not null)
                productionContext.ReportDiagnostic(result.GeneratorDiagnostic);
            if (result.Model is not null)
                EmitSource(productionContext, result.Model);
        });
    }

    private static (ReactiveTypeModel? Model, Diagnostic? GeneratorDiagnostic) BuildModel(
        GeneratorAttributeSyntaxContext attributeContext)
    {
        if (attributeContext.TargetSymbol is not INamedTypeSymbol classSymbol)
            return (null, null);

        if (classSymbol.IsStatic)
            return (null, Diagnostic.Create(
                GeneratorDiagnostics.ReactiveClassMustNotBeStatic,
                attributeContext.TargetNode.GetLocation(),
                classSymbol.Name));

        bool isDeclaredPartial = false;
        foreach (SyntaxReference declarationReference in classSymbol.DeclaringSyntaxReferences)
        {
            if (declarationReference.GetSyntax() is ClassDeclarationSyntax classDeclaration
                && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                isDeclaredPartial = true;
                break;
            }
        }

        if (!isDeclaredPartial)
            return (null, Diagnostic.Create(
                GeneratorDiagnostics.ReactiveClassMustBePartial,
                attributeContext.TargetNode.GetLocation(),
                classSymbol.Name));

        List<ReactivePropertyModel> propertyModels = new();
        foreach (ISymbol memberSymbol in classSymbol.GetMembers())
        {
            if (memberSymbol is not IPropertySymbol propertySymbol) continue;
            if (propertySymbol.IsStatic) continue;

            ReactivePropertyKind kind = PropertyClassifier.Classify(propertySymbol);
            if (kind == ReactivePropertyKind.Ignored) continue;

            string declaredTypeFullName = propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            string? initializerExpression = null;
            bool hasInitializer = false;
            bool isPartialDeclaration = propertySymbol.IsPartialDefinition;
            foreach (SyntaxReference declarationReference in propertySymbol.DeclaringSyntaxReferences)
            {
                if (declarationReference.GetSyntax() is PropertyDeclarationSyntax propertySyntax)
                {
                    if (!isPartialDeclaration)
                        isPartialDeclaration = propertySyntax.Modifiers.Any(SyntaxKind.PartialKeyword);
                    if (propertySyntax.Initializer is not null)
                    {
                        hasInitializer = true;
                        initializerExpression = propertySyntax.Initializer.Value.ToFullString();
                    }
                }
            }

            propertyModels.Add(new ReactivePropertyModel(
                declaredTypeFullName,
                propertySymbol.Name,
                kind,
                hasInitializer,
                initializerExpression,
                isPartialDeclaration));
        }

        string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        return (new ReactiveTypeModel(
            namespaceName,
            classSymbol.Name,
            classSymbol.IsAbstract,
            propertyModels), null);
    }

    private static void EmitSource(SourceProductionContext productionContext, ReactiveTypeModel typeModel)
    {
        string generatedSource = PartialEmitter.Emit(typeModel);
        string hintName = $"{typeModel.NamespaceName}.{typeModel.ClassName}.Reflex.g.cs";
        productionContext.AddSource(hintName, SourceText.From(generatedSource, Encoding.UTF8));
    }
}
