# CK.Less.Transform

The style sheets of a package must be adaptable by other packages without copying them: a transformer injects rules at
an injection point, or ensures that a style sheet imports another one. This project is the Less language of
[CK.Transform.Core](../CK.Transform.Core/README.md): it registers in a `TransformerHost`, parses Less (and CSS) texts
into tokens and spans, and adds the `ensure @import` statement to the transformers of the core.

## The language

[`LessLanguage`](LessLanguage.cs) is the `TransformLanguage` named `Less`, for the `.less` and `.css` files:

```csharp
var host = new TransformerHost( new LessLanguage() );
var function = host.TryParseFunction( monitor, """"
    create <less> transformer
    begin
        ensure @import (!reference, less) 's.less';
    end
    """" );
SourceCode? result = host.Transform( monitor, source, function );
```

Besides the statements of the core (`inject ... into`, `in`, `replace`, `insert`, `unless`, `reparse` and the `begin`
... `end` blocks), a Less transformer accepts `ensure @import ...;`. The language has no span specification
(`in {...}`).

## The analyzer

[`LessAnalyzer`](LessAnalyzer.cs) is the `TargetLanguageAnalyzer` of the language. Its trivias are the line (`//`) and
star (`/* */`) comments. Its tokenization is basic: numbers, quoted strings, and "extended identifiers" that cover the
selectors, the property names, the `@variable`, the `@{variable-interpolation}` and the like; `:` and `::` are separate
tokens. It recognizes one construct: each `@import` becomes an [`ImportStatement`](Import/ImportStatement.cs) span (its
`ImportKeyword` and its `ImportPath`).

An injection point is a comment that contains `<Name />`, like `// <Name />`.

## Ensuring an import

`ensure @import (keywords) 'path';` is an [`EnsureImportStatement`](Import/EnsureImportStatement.cs):

- When an `@import` of the same path exists, its keywords are updated: the statement's keywords are added, the ones
  prefixed with `!` are removed (`(!reference, less)` removes `reference` and adds `less`).
- Otherwise a new `@import` is inserted after the last one (or at the beginning of the text).

The keywords are the [`ImportKeyword`](Import/ImportKeyword.cs) flags: `reference`, `inline`, `less`, `css`, `once`,
`multiple` and `optional`. `less` and `css` exclude each other, as `once` and `multiple` do; `once` is the default and
is never written.

From code, `EnsureImportStatement.EnsureOrderedImports` ensures a list of [`EnsureImportLine`](Import/EnsureImportLine.cs)
in a `SourceCodeEditor`: the imports are created or updated as above, then moved so that they appear in the order of the
list.

## Tests

The tests in [CK.Less.Transform.Tests](../Tests/CK.Less.Transform.Tests) parse real style sheets (`ParsingTests`:
bootstrap.css and the Less files of ng-zorro-antd) and show the statements (`EnsureImportTests`, `InjectIntoTests`).
