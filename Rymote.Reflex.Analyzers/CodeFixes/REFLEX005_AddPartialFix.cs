using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Rymote.Reflex.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddPartialModifierCodeFix)), Shared]
public sealed class AddPartialModifierCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("REFLEX005");

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext fixContext)
    {
        SyntaxNode? syntaxRoot = await fixContext.Document.GetSyntaxRootAsync(fixContext.CancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null) return;

        foreach (Diagnostic diagnostic in fixContext.Diagnostics)
        {
            SyntaxNode? targetNode = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            ClassDeclarationSyntax? classDeclaration = targetNode as ClassDeclarationSyntax
                ?? targetNode.Parent as ClassDeclarationSyntax;
            if (classDeclaration is null) continue;

            fixContext.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Add 'partial' modifier to '{classDeclaration.Identifier.Text}'",
                    createChangedDocument: _ =>
                    {
                        ClassDeclarationSyntax updatedClass = classDeclaration.AddModifiers(
                            SyntaxFactory.Token(SyntaxKind.PartialKeyword));
                        SyntaxNode updatedRoot = syntaxRoot.ReplaceNode(classDeclaration, updatedClass);
                        return Task.FromResult(fixContext.Document.WithSyntaxRoot(updatedRoot));
                    },
                    equivalenceKey: "AddPartialModifier"),
                diagnostic);
        }
    }
}
