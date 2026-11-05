The common basis of the parsers and of the "transformer languages" of CK-Transform: tokens with their trivias
(`Token`, `Trivia`, `TokenError`), analyzers (`Analyzer`, `TargetLanguageAnalyzer`), the parsed `SourceCode` with its
`SourceSpan` and the `SourceCodeEditor`. A `TransformerHost` registers `TransformLanguage` implementations, parses
`create <language> transformer ... begin ... end` functions (`TransformerHost.TryParseFunction`, a `TransformerFunction`)
and applies them to a text (`TransformerHost.Transform`) with the statements common to every language: `inject ... into`,
`in`, `replace`, `insert`, `unless` and `reparse`.
See the [README](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.Transform.Core/README.md).
