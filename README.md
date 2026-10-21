# CT.CodeStyle

CT.CodeStyle creates a NuGet package that applies consistent C# formatting across consuming solutions. Reference the package in a C# project, then run `dotnet format` on that project or its solution to apply the custom rules through their Roslyn code fixes.

## Using the package

Add the package to the projects you want to format, using a NuGet source containing the package:

```sh
dotnet add path/to/YourProject.csproj package CT.CodeStyle --version 1.0.3
dotnet format path/to/YourSolution.sln
```

To apply only this package's custom rules:

```sh
dotnet format analyzers path/to/YourSolution.sln --diagnostics CT0001 CT0002
```

The version currently declared in the packaging project is `1.0.3`. The repository does not configure a publishing feed.

## Custom formatting rules

Both rules are enabled by default with warning severity, skip generated code, and provide code fixes with batch Fix All support. They analyze parenthesized argument lists, rather than method parameter declarations or bracketed indexer arguments.

| Rule | Behavior |
| --- | --- |
| `CT0001` | Places the closing `)` immediately after `}` when the last argument ends in a closing brace and the two delimiters are on different lines. Skips cases with comments or other non-whitespace trivia between them. |
| `CT0002` | Keeps argument lists on one line when they fit the configured width; otherwise keeps the first argument next to `(` and aligns each subsequent argument beneath it. Requires at least two arguments and skips lists with multiline arguments or non-whitespace trivia at the boundaries it would rewrite. |

For example, `CT0001` changes:

```csharp
Run(() => {
    Work();
}
);
```

to:

```csharp
Run(() => {
    Work();
});
```

For `CT0002`, short calls become `Send(first, second)`. Calls exceeding the width use this layout:

```csharp
Send(firstArgument,
     secondArgument,
     thirdArgument);
```

The width calculation includes the expression's starting column, its prefix, arguments, and closing portion. It does not enforce a maximum length on every line in the file. Rewrites preserve argument contents and use the first newline style found in the source file, falling back to CRLF.

## Configuration

Set the custom options in a consuming project's `.editorconfig`, for example:

```ini
[*.cs]
dotnet_diagnostic.CT0001.severity = warning
dotnet_diagnostic.CT0002.severity = warning
ct_argument_layout_max_line_length = 120
tab_width = 4
indent_style = space
```

`CT0002` defaults to a maximum line length of 120 and tab width of 4. Invalid or nonpositive numeric options fall back to those defaults. Setting `indent_style = tab` uses tabs plus any spaces needed to align continuation arguments.

The package also contains `CT.CodeStyle.globalconfig`, which defines broader .NET and C# preferences: same-line opening braces, file-scoped namespaces, using directives outside namespaces, expression and `var` preferences, spacing, and naming conventions.

The global configuration loads automatically when a consuming project references the package. NuGet imports `buildTransitive/CT.CodeStyle.props`, which registers `config/CT.CodeStyle.globalconfig` as a `GlobalAnalyzerConfigFiles` item using a path relative to the props file. No manual import or configuration copying is required. A consuming project's `.editorconfig` can override the shared defaults.

## Code organization

| Project | Responsibility |
| --- | --- |
| `CT.CodeStyle.Analyzers` | Diagnostic descriptors and syntax analyzers. `ArgumentLayoutHelper` contains the shared layout selection, width calculation, options, and syntax rewrite logic for `CT0002`. |
| `CT.CodeStyle.CodeFixes` | Roslyn code-fix providers. `CT0001` removes whitespace between the closing delimiters; `CT0002` uses the shared helper to rewrite argument layout. References the analyzer project. |
| `CT.CodeStyle.Package` | Builds the `CT.CodeStyle` NuGet package, including both DLLs under `analyzers/dotnet/cs`, the global configuration under `config`, and the props file under `buildTransitive`. Suppresses package dependencies and excludes its own build output. |

All three projects target `netstandard2.0`. The analyzer and code-fix projects use Roslyn `5.9.0`; the code-fix project also references `System.Composition.AttributedModel` `10.0.12`. The consuming compiler/formatter host must support the Roslyn APIs used by these assemblies.

## Building the NuGet package

Use a .NET SDK that supports the `.slnx` solution format and the source's current C# syntax:

```sh
dotnet restore CT.CodeStyle.Analyzers.slnx
dotnet build CT.CodeStyle.Analyzers.slnx --configuration Release --no-restore
dotnet pack CT.CodeStyle.Package/CT.CodeStyle.Package.csproj --configuration Release --no-build --no-restore --output artifacts
```

Run each command after the previous one succeeds. The expected package output is `artifacts/CT.CodeStyle.1.0.3.nupkg`.

## Validation status

The Release solution build was verified with .NET SDK `10.0.401`, existing restored dependencies, and `-p:UseSharedCompilation=false -m:1`. It succeeded with three nullable-reference warnings: two `CS8600` warnings in `ArgumentLayoutHelper` and one `CS8602` warning in `ArgumentLayoutCodeFixProvider`.

Package creation and automatic configuration loading were verified using a fresh consuming project restored from a local NuGet feed. MSBuild registered the packaged global configuration, and `dotnet format whitespace` applied its same-line brace style without a manual config import or local style settings.

There are currently no automated test projects in the solution. The custom rules' code fixes were not exercised in this configuration-loading check.
