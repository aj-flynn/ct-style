using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CT.CodeStyle.Rules.CT0001;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ClosingDelimitersAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ClosingDelimitersRule.Descriptor];

    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeArgumentList, SyntaxKind.ArgumentList);
    }

    private static void AnalyzeArgumentList(
        SyntaxNodeAnalysisContext context) {
        var argumentList = (ArgumentListSyntax)context.Node;

        if (argumentList.Arguments.Count == 0)
            return;

        var lastArgument = argumentList.Arguments[argumentList.Arguments.Count - 1];

        var lastToken = lastArgument.GetLastToken(includeZeroWidth: true);

        if (!lastToken.IsKind(SyntaxKind.CloseBraceToken))
            return;

        var closeParen = argumentList.CloseParenToken;

        if (closeParen.IsMissing)
            return;

        if (!OnlyWhitespace(lastToken.TrailingTrivia) || !OnlyWhitespace(closeParen.LeadingTrivia))
            return;

        var braceLine = lastToken.GetLocation()
            .GetLineSpan().StartLinePosition.Line;

        var parenLine = closeParen.GetLocation()
            .GetLineSpan().StartLinePosition.Line;

        if (braceLine == parenLine)
            return;

        context.ReportDiagnostic(Diagnostic.Create(ClosingDelimitersRule.Descriptor, closeParen.GetLocation()));
    }

    private static bool OnlyWhitespace(SyntaxTriviaList trivia) {
        foreach (var item in trivia) {
            if (!item.IsKind(SyntaxKind.WhitespaceTrivia) &&
                !item.IsKind(SyntaxKind.EndOfLineTrivia))
                return false;
        }

        return true;
    }
}