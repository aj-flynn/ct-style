using CT.CodeStyle.Analyzers.Rules.CT0002;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CT.CodeStyle.CodeFixes.Rules.CT0002 {
    [ExportCodeFixProvider(
        LanguageNames.CSharp,
        Name = nameof(ArgumentLayoutCodeFixProvider))]
    [Shared]
    public sealed class ArgumentLayoutCodeFixProvider
        : CodeFixProvider {
        public override ImmutableArray<string>
            FixableDiagnosticIds {
            get {
                return ImmutableArray.Create(
                    ArgumentLayoutRule.DiagnosticId);
            }
        }

        public override FixAllProvider
            GetFixAllProvider() {
            return WellKnownFixAllProviders
                .BatchFixer;
        }

        public override async Task
            RegisterCodeFixesAsync(
                CodeFixContext context) {
            var document =
                context.Document;

            var cancellationToken =
                context.CancellationToken;

            var root =
                await document
                    .GetSyntaxRootAsync(
                        cancellationToken)
                    .ConfigureAwait(false);

            if (root == null) {
                return;
            }

            var diagnostic =
                context.Diagnostics[0];

            var token =
                root.FindToken(
                    diagnostic
                        .Location
                        .SourceSpan
                        .Start);

            var argumentList =
                token.Parent
                    .AncestorsAndSelf()
                    .OfType<ArgumentListSyntax>()
                    .FirstOrDefault();

            if (argumentList == null) {
                return;
            }

            var sourceText =
                await document
                    .GetTextAsync(
                        cancellationToken)
                    .ConfigureAwait(false);

            var syntaxTree =
                await document
                    .GetSyntaxTreeAsync(
                        cancellationToken)
                    .ConfigureAwait(false);

            if (syntaxTree == null) {
                return;
            }

            var analyzerOptions =
                document.Project
                    .AnalyzerOptions
                    .AnalyzerConfigOptionsProvider
                    .GetOptions(syntaxTree);

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

            string title;

            if (desiredLayout ==
                ArgumentLayout.SingleLine) {
                title =
                    "Put arguments on one line";
            }
            else {
                title =
                    "Use hanging argument layout";
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    delegate (
                        CancellationToken ct) {
                            return ApplyFixAsync(
                                document,
                                argumentList,
                                desiredLayout,
                                ct);
                        },
                    ArgumentLayoutRule.DiagnosticId),
                diagnostic);
        }

        private static async Task<Document>
            ApplyFixAsync(
                Document document,
                ArgumentListSyntax argumentList,
                ArgumentLayout desiredLayout,
                CancellationToken cancellationToken) {
            var root =
                await document
                    .GetSyntaxRootAsync(
                        cancellationToken)
                    .ConfigureAwait(false);

            if (root == null) {
                return document;
            }

            var sourceText =
                await document
                    .GetTextAsync(
                        cancellationToken)
                    .ConfigureAwait(false);

            var syntaxTree =
                await document
                    .GetSyntaxTreeAsync(
                        cancellationToken)
                    .ConfigureAwait(false);

            if (syntaxTree == null) {
                return document;
            }

            var analyzerOptions =
                document.Project
                    .AnalyzerOptions
                    .AnalyzerConfigOptionsProvider
                    .GetOptions(syntaxTree);

            var options =
                ArgumentLayoutOptions.From(
                    analyzerOptions);

            var rewritten =
                ArgumentLayoutHelper.Rewrite(
                    argumentList,
                    sourceText,
                    options,
                    desiredLayout);

            var newRoot =
                root.ReplaceNode(
                    argumentList,
                    rewritten);

            return document.WithSyntaxRoot(
                newRoot);
        }
    }
}