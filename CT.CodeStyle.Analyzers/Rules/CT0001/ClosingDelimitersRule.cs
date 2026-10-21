using Microsoft.CodeAnalysis;

namespace CT.CodeStyle.Rules.CT0001;

public static class ClosingDelimitersRule {
    public const string DiagnosticId = "CT0001";

    public static readonly DiagnosticDescriptor Descriptor = new(
        DiagnosticId,
        "Closing brace and parenthesis should be on the same line",
        "Place ')' immediately after '}'",
        "Formatting",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}