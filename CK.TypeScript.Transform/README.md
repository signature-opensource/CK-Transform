# CK.TypeScript.Transform

The TypeScript files of a package must be adaptable by other packages without copying them: a transformer ensures an
import, replaces or inserts code in a class or in a decorator's arguments, injects code at an injection point. This
project is the TypeScript language of [CK.Transform.Core](../CK.Transform.Core/README.md): it registers in a
`TransformerHost`, parses TypeScript texts into tokens and spans, and adds the `ensure import` statement and the
TypeScript span specifications to the transformers of the core.

## The language

[`TypeScriptLanguage`](TypeScriptLanguage.cs) is the `TransformLanguage` named `TypeScript`, for the `.ts` files (and
`.typescript`):

```csharp
var host = new TransformerHost( new TypeScriptLanguage() );
var function = host.TryParseFunction( monitor, """"
    create <ts> transformer
    begin
        ensure import { CKGenAppModule } from '@local/ck-gen/CK/Angular/CKGenAppModule';

        in after "@Component"
            in first {^braces}
                in after "imports:"
                    in first {^[]}
                        replace "RouterOutlet" with "RouterOutlet, CKGenAppModule";
        in single {class}
            in first {braces}
            replace * with "{ }";
    end
    """" );
SourceCode? result = host.Transform( monitor, source, function );
```

Besides the statements of the core (`inject ... into`, `in`, `replace`, `insert`, `unless`, `reparse` and the `begin`
... `end` blocks), a TypeScript transformer accepts `ensure import ...;`.

## The analyzer

[`TypeScriptAnalyzer`](TypeScriptAnalyzer.cs) is the `TargetLanguageAnalyzer` of the language. Its trivias are the line
(`//`) and star (`/* */`) comments. Its scanner ([TypeScriptAnalyzer.Scanner.cs](TypeScriptAnalyzer.Scanner.cs))
handles the numbers, the strings, the interpolated strings, the regular expressions and the identifiers, and tracks the
brace depth. The match patterns of the transformers are parsed by the same scanner.

It recognizes two constructs:

- each `import` statement (not the `import(...)` calls) becomes an [`ImportStatement`](Language/ImportStatement.cs)
  span, an `IImportLine`: `SideEffectOnly`, `TypeOnly`, `Namespace`, `DefaultImport`, `NamedImports` and `ImportPath`;
- each class becomes a [`ClassDefinition`](Language/ClassDefinition.cs) span, from its `export` (if any) to its closing
  brace.

An injection point is a comment that contains `<Name />`, like `// <Name />`.

## The span specifications

The `{...}` of a location (`in first {braces}`) restricts the matches to spans:

| Specification | Spans |
|---------------|-------|
| `{braces}` or `{{}}`, `{brackets}` or `{[]}`, `{parens}` or `{()}` | The deepest spans enclosed by `{` `}`, `[` `]` or `(` `)`. |
| `{^braces}` or `{^{}}`, `{^brackets}` or `{^[]}`, `{^parens}` or `{^()}` | The top spans enclosed by these pairs. |
| `{class}`, `{^class}` | The deepest, or the top, `ClassDefinition` spans. |
| `{import}` | The `ImportStatement` spans. |

## Ensuring an import

`ensure import ...;` is an [`EnsureImportStatement`](Transform/EnsureImportStatement.cs): it adds what is missing to
the imports of the text, reusing an existing import of the same path when possible and inserting a new import after the
last one (or at the beginning of the text) otherwise. A name already imported from another path is an error, except when
the new path is under the existing one and imports the same symbol: the name then moves to the new import
(`@local/ck-gen/CK/Angular/CKGenAppModule` replaces `@local/ck-gen` for this name).

From code, `EnsureImportStatement.EnsureImport` does the same in a `SourceCodeEditor` with an `IImportLine`: an
[`ImportLine`](Language/ImportLine.cs) is a mutable one.

## Tests

The tests in [CK.TypeScript.Transform.Tests](../Tests/CK.TypeScript.Transform.Tests) parse TypeScript texts
(`TypeScriptParsingTests`) and show the statements on them (`EnsureImportTests`, `InjectIntoTests`,
`AppTransformerTests`, `EnsureArrayTests`).
