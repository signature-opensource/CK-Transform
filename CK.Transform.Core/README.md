# CK.Transform.Core

The goal of this library is to provide a common basis to as many kind of parsers as possible in order
to factorize "transformer languages". Parsers can have very different implementations depending on
their purpose(s). Defining a common infrastructure is not a trivial task and is unfortunately
oriented towards some usage. Our primary usage is to ease transformer implementations for multiple
target languages. This repository contains three of them, each one a `TransformLanguage`:
- Html (CK.Html.Transform, Angular templates among the HTML files).
- Less and CSS (CK.Less.Transform).
- TypeScript (CK.TypeScript.Transform).

Other languages can be built on this library the same way. The transformer language itself is also handled: a
transformer can transform another transformer (see `TransformerHost.RootTransformLanguage`).

Exceptions should not be used for the control flow (they remain exceptional): errors should be emitted
via `TokenError` that carries an error token type (negative integer), an error message
and the location of the error.

## CR and CRLF handling
Only '\n' (Unix) and '\r\n' (Windows) are handled ('\r' alone should not be used!).
There is no normalization and normalization is useless.

1. Tokens (leading and trailing trivias) and RawString transparently handle both '\n' and '\r\n' end of line characters.
2. When a transformer injects a new line, it uses `Environment.NewLine`: the
result of a transformation can contain both end of line characters and can be safely transformed again (because of 1).

This design is the most efficient regardless of the platform: the inputs (the source and the code of the transformers)
don't have to be normalized. If outputs need to be normalized (because other participants require normalized end of lines),
this is not the concern of this library.

## The basics: Tokens & Trivias
A `Token` has a `TokenType`, a `Text` (a `ReadOnlyMemory<char>` on the parsed text) and its `LeadingTrivias` and
`TrailingTrivias`. Tokens can be specialized (`TokenError`, and the `RawString` or `InjectionPoint` of the transformer
language for instance).

A `Trivia` is a run of whitespaces (including newlines) or a comment: it has a `TokenType` and a `Content` that covers
the whole trivia (comment delimiters included). Its `TokenType` doesn't say which comment syntax it is: it says whether
it is a line or a block comment and encodes the lengths of the start and end delimiters
(`TokenTypeExtensions.GetTriviaLineCommentType` and `TokenTypeExtensions.GetTriviaBlockCommentType`). The micro parsers
of trivias are `TriviaParser` methods that work on a `TriviaHead`, like `TriviaHeadExtensions.AcceptCLikeLineComment`
or `TriviaHeadExtensions.AcceptCLikeStarComment`.

`TokenType` is a `[Flags]` integer enum split into classes:
- The basic token types (`BasicTokenType`) have no class bit set: they are the generic tokens (`GenericIdentifier`,
  `GenericString`, `OpenBrace`, `SemiColon`...) that any language can reuse.
- A language reserves its own class with `TokenTypeExtensions.ReserveTokenClass` (Html uses the class n°22).
- The sign bit (`TokenType.ErrorClassBit`) marks an error and can be combined with any other token type.
- The class n°23 (`TokenType.TriviaClassNumber`) is the one of the trivias.

## The Analyzer abstraction
An `Analyzer` turns a text into tokens and spans: `Analyzer.Parse( ReadOnlyMemory<char> )` returns an
`AnalyzerResult` with the `SourceCode` (its `Tokens` and its `Spans`, the `SourceSpan` that structure the tokens),
`Success`, `ErrorCount` and `FirstError`. An implementation provides:
- `ParseTrivia( ref TriviaHead )`: the trivias of the language (calls to the `TriviaHead` micro parsers);
- `LowLevelTokenize( ReadOnlySpan<char> )`: the `LowLevelToken` (a candidate token type and length) at the head of the text;
- `DoParse( ref TokenizerHead )`: the parsing itself, that accepts tokens and adds spans on the `TokenizerHead`.

A `TargetLanguageAnalyzer` is the analyzer of a language that transformers target. It has a `LanguageName` and
supports the transformer language: it parses the `{span specification}` and the `"pattern"` of the locations of the
statements (see below) and creates the trivia of an injection point.

A `TransformLanguage` is the factory of a `TargetLanguageAnalyzer` and of its `TransformLanguageAnalyzer`, identified by
its `LanguageName` and its `FileExtensions`. Languages are registered in a `TransformerHost`.

## The transformer language
A `TransformLanguageAnalyzer` parses a transformer, a `TransformerFunction`:
```
create <language> transformer [name] [on <target>] [as]
begin
    ...statements...
end
```
`<language>` is the name or a file extension of a registered language. Its trivias are the `//` line comments and the
`/* */` comments (that can be nested). The strings are `RawString` tokens: they follow the rules of the C# raw strings
(`"single line"`, `"""multi-quoted or multi-line"""`), without escape character.

The statements below are handled by `TransformLanguageAnalyzer.ParseStatement` for every language; a language can add
its own ones. The ending `;` is optional.

| Statement | Description |
|-----------|-------------|
| `inject "text" into <Name>;` | `InjectIntoStatement`: injects the text at the injection point `<Name>` of the current scope. In the target text, an injection point is a comment that contains `<Name />` (each language defines its comment). |
| `in location [in location...] statement` | `InScopeStatement`: restricts the scope of the statement (or of the `begin` ... `end` block) that follows. |
| `replace location with "text";` | `ReplaceStatement`: replaces each matched range of tokens. `*` instead of a location replaces the whole current scope. |
| `insert "text" before\|after location;`<br>`insert before\|after location "text";` | `InsertStatement`: inserts the text before or after the matches (`*` for the whole current scope). |
| `unless <Name> statement` | `UnlessStatement`: applies the statement (or block) only if the injection point `<Name>` doesn't exist, and adds it: applying the transformer again does nothing. |
| `reparse;` | `ReparseStatement`: parses the transformed text again when a previous statement required it (`SourceCodeEditor.SetNeedReparse`); otherwise it does nothing. |
| `begin ... end` | `TransformStatementBlock`: a sequence of statements. |

A location is:
- a `LocationMatcher`: an optional cardinality (`single`, the default, `first [+n] [out of n]`, `last [-n] [out of n]`,
  `all [n]` or `each [n]`) followed by a `SpanMatcher`: a `{span specification}`, a `"pattern"`, or
  `{span specification} where "pattern"`. The span specifications depend on the target language; a pattern is parsed
  with the tokens of the target language and matches a range of tokens;
- or, for `in` only, a `RangeLocation`: `before location`, `after location` or `between location and location`, where
  the locations designate a single match (not `each` or `all`).

## Error handling: inlined errors
The collected tokens can contain `TokenError` that is a specialized `Token`.
This behavior is simple to implement and understand. This enables any analyzer to be
"fault tolerant" but analyze can also stop on the first error.





