using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rymote.Reflex.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FieldToPropertyCodeFix)), Shared]
public sealed class FieldToPropertyCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("REFLEX001");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext fixContext)
    {
        SyntaxNode? syntaxRoot = await fixContext.Document.GetSyntaxRootAsync(fixContext.CancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null) return;

        foreach (Diagnostic diagnostic in fixContext.Diagnostics)
        {
            SyntaxNode? targetNode = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            if (targetNode is not VariableDeclaratorSyntax variableDeclarator) continue;
            if (variableDeclarator.Parent is not VariableDeclarationSyntax variableDeclaration) continue;
            if (variableDeclaration.Parent is not FieldDeclarationSyntax fieldDeclaration) continue;

            fixContext.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Convert field '{variableDeclarator.Identifier.Text}' to property",
                    createChangedDocument: _ =>
                    {
                        TypeSyntax fieldType = fieldDeclaration.Declaration.Type;
                        PropertyDeclarationSyntax propertyDeclaration = SyntaxFactory
                            .PropertyDeclaration(fieldType, variableDeclarator.Identifier)
                            .WithModifiers(fieldDeclaration.Modifiers)
                            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
                            {
                                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            })));

                        if (variableDeclarator.Initializer is not null)
                            propertyDeclaration = propertyDeclaration
                                .WithInitializer(variableDeclarator.Initializer)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

                        SyntaxNode updatedRoot = syntaxRoot.ReplaceNode(fieldDeclaration, propertyDeclaration);
                        return Task.FromResult(fixContext.Document.WithSyntaxRoot(updatedRoot));
                    },
                    equivalenceKey: "ConvertFieldToProperty"),
                diagnostic);
        }
    }
}
