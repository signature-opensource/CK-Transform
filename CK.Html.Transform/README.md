# CK.Html.Transform

The HTML files of a package (Angular templates among them) must be adaptable by other packages without copying them: a
transformer adds a fragment at an injection point, inserts or replaces some text. This project is the HTML language of
[CK.Transform.Core](../CK.Transform.Core/README.md): it registers in a `TransformerHost`, parses the HTML text into
tokens and spans, and lets the transformers of the core apply to it.

## The language

[`HtmlLanguage`](HtmlLanguage.cs) is the `TransformLanguage` named `Html`, for the `.html` and `.htm` files:

```csharp
var host = new TransformerHost( new HtmlLanguage() );
var function = host.TryParseFunction( monitor, """"
    create <html> transformer
    begin
        inject """
               First injection ever...

               """ into <FirstInjectionPointEver>;
    end
    """" );
SourceCode? result = host.Transform( monitor, source, function );
```

The target of a transformer is its language name or one of its file extensions (`<html>` above).

The statements are the ones of the core (`inject ... into`, `in`, `replace`, `insert`, `unless`, `reparse` and the
`begin` ... `end` blocks): this language adds none, and no span specification (`in {...}`) either. A match pattern
(`"..."`) is parsed into HTML tokens and must match whole tokens.

## The analyzer

[`HtmlAnalyzer`](HtmlAnalyzer.cs) is the `TargetLanguageAnalyzer` of the language. It is permissive (a tag name starts
with an ASCII letter, then anything up to a white space, `/`, `<` or `>`) and swaps between two modes:

- **Text**: white spaces are part of the text tokens, not trivias. The trivias are the comments (`<!-- ... -->`, and the
  `--!>` end) and the `<![CDATA[ ... ]]>` sections.
- **Element**: between a `<tag` that has attributes and its `>` or `/>`, white spaces are trivias of the attribute
  tokens (names, `=`, values). No comment nor CDATA is analyzed there.

The tokens are described by the predicates of [`TokenTypeExtensions`](TokenTypeExtensions.cs) (which reserves the token
class n°22 for "Html"): `IsHtmlText`, `IsHtmlStartingTag` (a `<tag` with attributes), `IsHtmlEndingTag` (`</tag>`),
`IsHtmlEmptyElement` (`<tag/>`), `IsHtmlStartingEmptyElement` (`<tag>` without attributes), `IsEmptyVoidElement` (a
void element like `<br>` without attributes), `IsHtmlStartingVoidElement` (a void element with attributes) and
`IsHtmlEndTokenTag` (`/>`). `TokenExtensions.GetHtmlTagName` gives the tag name of a starting or ending tag token.

An element is covered by an [`HtmlElementSpan`](HtmlElementSpan.cs), from its starting tag to its closing tag, its
ending `/>`, or the `>` of a void element with attributes; the single-token elements (`<tag/>`, `<br>`) have no span. An
ending tag closes the innermost element of the same name (case insensitive), and the elements left open inside it; an
ending tag that matches no element is left as is.

An injection point is an HTML comment that contains `<Name />` (like `<!-- <Name /> -->`): once something is injected
into it, it becomes `<!--<Name>-->` ... `<!--</Name>-->`.

## Tests

The tests in [CK.Html.Transform.Tests](../Tests/CK.Html.Transform.Tests) show the statements on HTML texts
(`InjectIntoTests`, `InsertTests`, `ReplaceTests`).
