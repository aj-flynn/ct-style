using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace CT.CodeStyle.Rules.CT0001;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClosingDelimitersCodeFixProvider))]
[Shared]
public sealed class ClosingDelimitersCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds => [ClosingDelimitersRule.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var root = await context.Document
            .GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (root is null)
            return;

        foreach (var diagnostic in context.Diagnostics) {
            var closeParen =
                root.FindToken(diagnostic.Location.SourceSpan.Start);

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Place ')' after '}'",
                    cancellationToken =>
                        FixAsync(
                            context.Document,
                            closeParen,
                            cancellationToken),
                    ClosingDelimitersRule.DiagnosticId),
                diagnostic);
        }
    }

    private static async Task<Document> FixAsync(Document document, SyntaxToken closeParen, CancellationToken cancellationToken) {
        var root = await document
            .GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);

        if (root is null)
            return document;

        var closeBrace = closeParen.GetPreviousToken(includeZeroWidth: true);

        if (!closeBrace.IsKind(SyntaxKind.CloseBraceToken))
            return document;

        var newRoot = root.ReplaceTokens(
            [closeBrace, closeParen],
            (original, _) => {
                if (original == closeBrace)
                    return closeBrace.WithTrailingTrivia();

                if (original == closeParen)
                    return closeParen.WithLeadingTrivia();

                return original;
            });

        return document.WithSyntaxRoot(newRoot);
    }
}