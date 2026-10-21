using Microsoft.CodeAnalysis;

namespace CT.CodeStyle.Analyzers.Rules.CT0002 {
    public static class ArgumentLayoutRule {
        public const string DiagnosticId = "CT0002";

        public static readonly DiagnosticDescriptor Descriptor =
            new DiagnosticDescriptor(
                DiagnosticId,
                "Use preferred argument layout",
                "Argument list should use {0} layout",
                "Formatting",
                DiagnosticSeverity.Warning,
                true,
                "Calls that fit within the configured line length should remain on one line. " +
                "Longer calls should keep the first argument next to the opening parenthesis " +
                "and align subsequent arguments beneath it.");
    }
}