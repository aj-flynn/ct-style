using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;

namespace CT.CodeStyle.Analyzers.Rules.CT0002 {
    public enum ArgumentLayout {
        None,
        SingleLine,
        Hanging
    }

    public sealed class ArgumentLayoutOptions {
        public const int DefaultMaxLineLength = 120;
        public const int DefaultTabWidth = 4;

        public ArgumentLayoutOptions(
            int maxLineLength,
            int tabWidth,
            bool useTabs) {
            MaxLineLength = maxLineLength;
            TabWidth = tabWidth;
            UseTabs = useTabs;
        }

        public int MaxLineLength { get; private set; }

        public int TabWidth { get; private set; }

        public bool UseTabs { get; private set; }

        public static ArgumentLayoutOptions From(
            AnalyzerConfigOptions options) {
            var maxLineLength = ReadPositiveInt(
                options,
                "ct_argument_layout_max_line_length",
                DefaultMaxLineLength);

            var tabWidth = ReadPositiveInt(
                options,
                "tab_width",
                DefaultTabWidth);

            string indentStyle;

            var useTabs =
                options.TryGetValue(
                    "indent_style",
                    out indentStyle) &&
                string.Equals(
                    indentStyle,
                    "tab",
                    StringComparison.OrdinalIgnoreCase);

            return new ArgumentLayoutOptions(
                maxLineLength,
                tabWidth,
                useTabs);
        }

        private static int ReadPositiveInt(
            AnalyzerConfigOptions options,
            string key,
            int fallback) {
            string value;
            int parsed;

            if (options.TryGetValue(key, out value) &&
                int.TryParse(value, out parsed) &&
                parsed > 0) {
                return parsed;
            }

            return fallback;
        }
    }

    public static class ArgumentLayoutHelper {
        public static ArgumentLayout GetDesiredLayout(
            ArgumentListSyntax argumentList,
            SourceText sourceText,
            ArgumentLayoutOptions options) {
            if (argumentList.Arguments.Count < 2) {
                return ArgumentLayout.None;
            }

            if (!CanSafelyRewrite(argumentList)) {
                return ArgumentLayout.None;
            }

            if (HasMultilineArgument(
                argumentList,
                sourceText)) {
                return ArgumentLayout.None;
            }

            if (WouldFitOnOneLine(
                argumentList,
                sourceText,
                options)) {
                return ArgumentLayout.SingleLine;
            }

            return ArgumentLayout.Hanging;
        }

        public static ArgumentListSyntax Rewrite(
            ArgumentListSyntax argumentList,
            SourceText sourceText,
            ArgumentLayoutOptions options,
            ArgumentLayout layout) {
            if (layout == ArgumentLayout.None) {
                return argumentList;
            }

            var openParen =
                argumentList.OpenParenToken
                    .WithTrailingTrivia(
                        SyntaxTriviaList.Empty);

            var closeParen =
                argumentList.CloseParenToken
                    .WithLeadingTrivia(
                        SyntaxTriviaList.Empty);

            var firstArgumentColumn =
                GetVisualColumn(
                    sourceText,
                    argumentList.OpenParenToken.Span.End,
                    options.TabWidth);

            var newLine =
                GetNewLine(sourceText);

            var indentation =
                CreateIndentation(
                    firstArgumentColumn,
                    options);

            var nodesAndTokens =
                new List<SyntaxNodeOrToken>(
                    argumentList.Arguments.Count * 2 - 1);

            for (var i = 0;
                 i < argumentList.Arguments.Count;
                 i++) {
                var argument =
                    argumentList.Arguments[i]
                        .WithLeadingTrivia(
                            SyntaxTriviaList.Empty)
                        .WithTrailingTrivia(
                            SyntaxTriviaList.Empty);

                nodesAndTokens.Add(argument);

                if (i >=
                    argumentList.Arguments.SeparatorCount) {
                    continue;
                }

                var separator =
                    argumentList.Arguments
                        .GetSeparator(i)
                        .WithLeadingTrivia(
                            SyntaxTriviaList.Empty);

                if (layout ==
                    ArgumentLayout.SingleLine) {
                    separator =
                        separator.WithTrailingTrivia(
                            SyntaxFactory.Space);
                }
                else if (layout ==
                         ArgumentLayout.Hanging) {
                    separator =
                        separator.WithTrailingTrivia(
                            SyntaxFactory.EndOfLine(
                                newLine),
                            SyntaxFactory.Whitespace(
                                indentation));
                }

                nodesAndTokens.Add(separator);
            }

            var arguments =
                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                    nodesAndTokens);

            return argumentList
                .WithOpenParenToken(openParen)
                .WithArguments(arguments)
                .WithCloseParenToken(closeParen);
        }

        private static bool WouldFitOnOneLine(
            ArgumentListSyntax argumentList,
            SourceText sourceText,
            ArgumentLayoutOptions options) {
            var owner = argumentList.Parent;

            if (owner == null) {
                return false;
            }

            /*
             * Measure the expression containing the argument list.
             *
             * For:
             *
             *     SomeObject.DoSomething(a, b)
             *
             * this includes:
             *
             *     SomeObject.DoSomething(
             *
             * and the closing portion after the final argument.
             */

            var prefixSpan =
                TextSpan.FromBounds(
                    owner.SpanStart,
                    argumentList.OpenParenToken.Span.End);

            var suffixSpan =
                TextSpan.FromBounds(
                    argumentList.CloseParenToken.SpanStart,
                    owner.Span.End);

            var prefix =
                sourceText.ToString(prefixSpan);

            var suffix =
                sourceText.ToString(suffixSpan);

            if (ContainsLineBreak(prefix) ||
                ContainsLineBreak(suffix)) {
                return false;
            }

            var column =
                GetVisualColumn(
                    sourceText,
                    owner.SpanStart,
                    options.TabWidth);

            column =
                AdvanceColumn(
                    column,
                    prefix,
                    options.TabWidth);

            for (var i = 0;
                 i < argumentList.Arguments.Count;
                 i++) {
                if (i > 0) {
                    column =
                        AdvanceColumn(
                            column,
                            ", ",
                            options.TabWidth);
                }

                var argumentText =
                    sourceText.ToString(
                        argumentList.Arguments[i].Span);

                if (ContainsLineBreak(argumentText)) {
                    return false;
                }

                column =
                    AdvanceColumn(
                        column,
                        argumentText,
                        options.TabWidth);

                if (column >
                    options.MaxLineLength) {
                    return false;
                }
            }

            column =
                AdvanceColumn(
                    column,
                    suffix,
                    options.TabWidth);

            return column <=
                   options.MaxLineLength;
        }

        private static bool HasMultilineArgument(
            ArgumentListSyntax argumentList,
            SourceText sourceText) {
            foreach (var argument
                     in argumentList.Arguments) {
                var startLine =
                    sourceText.Lines
                        .GetLineFromPosition(
                            argument.SpanStart)
                        .LineNumber;

                var endLine =
                    sourceText.Lines
                        .GetLineFromPosition(
                            argument.Span.End)
                        .LineNumber;

                if (startLine != endLine) {
                    return true;
                }
            }

            return false;
        }

        private static bool CanSafelyRewrite(
            ArgumentListSyntax argumentList) {
            if (!ContainsOnlyWhitespace(
                    argumentList
                        .OpenParenToken
                        .TrailingTrivia)) {
                return false;
            }

            if (!ContainsOnlyWhitespace(
                    argumentList
                        .CloseParenToken
                        .LeadingTrivia)) {
                return false;
            }

            foreach (var argument
                     in argumentList.Arguments) {
                if (!ContainsOnlyWhitespace(
                        argument.GetLeadingTrivia())) {
                    return false;
                }

                if (!ContainsOnlyWhitespace(
                        argument.GetTrailingTrivia())) {
                    return false;
                }
            }

            foreach (var separator
                     in argumentList.Arguments
                         .GetSeparators()) {
                if (!ContainsOnlyWhitespace(
                        separator.LeadingTrivia)) {
                    return false;
                }

                if (!ContainsOnlyWhitespace(
                        separator.TrailingTrivia)) {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsOnlyWhitespace(
            SyntaxTriviaList triviaList) {
            foreach (var trivia in triviaList) {
                if (!trivia.IsKind(
                        SyntaxKind.WhitespaceTrivia) &&
                    !trivia.IsKind(
                        SyntaxKind.EndOfLineTrivia)) {
                    return false;
                }
            }

            return true;
        }

        private static string CreateIndentation(
            int column,
            ArgumentLayoutOptions options) {
            if (column <= 0) {
                return string.Empty;
            }

            if (!options.UseTabs) {
                return new string(
                    ' ',
                    column);
            }

            var tabs =
                column / options.TabWidth;

            var spaces =
                column % options.TabWidth;

            return
                new string('\t', tabs) +
                new string(' ', spaces);
        }

        private static int GetVisualColumn(
            SourceText sourceText,
            int position,
            int tabWidth) {
            var line =
                sourceText.Lines
                    .GetLineFromPosition(position);

            var text =
                sourceText.ToString(
                    TextSpan.FromBounds(
                        line.Start,
                        position));

            return AdvanceColumn(
                0,
                text,
                tabWidth);
        }

        private static int AdvanceColumn(
            int column,
            string text,
            int tabWidth) {
            foreach (var character in text) {
                if (character == '\t') {
                    column +=
                        tabWidth -
                        column % tabWidth;
                }
                else {
                    column++;
                }
            }

            return column;
        }

        private static bool ContainsLineBreak(
            string text) {
            return
                text.IndexOf('\r') >= 0 ||
                text.IndexOf('\n') >= 0;
        }

        private static string GetNewLine(
            SourceText sourceText) {
            foreach (var line
                     in sourceText.Lines) {
                if (line.EndIncludingLineBreak >
                    line.End) {
                    return sourceText.ToString(
                        TextSpan.FromBounds(
                            line.End,
                            line.EndIncludingLineBreak));
                }
            }

            return "\r\n";
        }
    }
}