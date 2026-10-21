using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace CT.CodeStyle.Analyzers.Rules.CT0002 {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ArgumentLayoutAnalyzer
        : DiagnosticAnalyzer {
        public override
            ImmutableArray<DiagnosticDescriptor>
            SupportedDiagnostics {
            get {
                return ImmutableArray.Create(
                    ArgumentLayoutRule.Descriptor);
            }
        }

        public override void Initialize(
            AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(
                GeneratedCodeAnalysisFlags.None);

            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(
                AnalyzeArgumentList,
                SyntaxKind.ArgumentList);
        }

        private static void AnalyzeArgumentList(
            SyntaxNodeAnalysisContext context) {
            var argumentList =
                (ArgumentListSyntax)context.Node;

            var sourceText =
                argumentList.SyntaxTree.GetText(
                    context.CancellationToken);

            var analyzerOptions =
                context.Options
                    .AnalyzerConfigOptionsProvider
                    .GetOptions(
                        argumentList.SyntaxTree);

            var options =
                ArgumentLayoutOptions.From(
                    analyzerOptions);

            var desiredLayout =
                ArgumentLayoutHelper
                    .GetDesiredLayout(
                        argumentList,
                        sourceText,
                        options);

            if (desiredLayout ==
                ArgumentLayout.None) {
                return;
            }

            var rewritten =
                ArgumentLayoutHelper.Rewrite(
                    argumentList,
                    sourceText,
                    options,
                    desiredLayout);

            if (string.Equals(
                argumentList.ToFullString(),
                rewritten.ToFullString(),
                StringComparison.Ordinal)) {
                return;
            }

            var description =
                desiredLayout ==
                ArgumentLayout.SingleLine
                    ? "single-line"
                    : "hanging";

            context.ReportDiagnostic(
                Diagnostic.Create(
                    ArgumentLayoutRule.Descriptor,
                    argumentList
                        .OpenParenToken
                        .GetLocation(),
                    description));
        }
    }
}