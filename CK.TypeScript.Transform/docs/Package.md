The TypeScript language of [CK.Transform.Core](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.Transform.Core/README.md):
transformer functions that modify `.ts` files, and ensure their imports.

Register `TypeScriptLanguage` in a `TransformerHost` to apply `create <ts> transformer` functions to TypeScript texts.
`TypeScriptAnalyzer` exposes the `ImportStatement` and `ClassDefinition` spans and the span specifications
`{braces}`, `{brackets}`, `{parens}`, `{class}` and `{import}`; the `ensure import ...;` statement
(`EnsureImportStatement`, also callable from code with an `ImportLine`) merges an import into the existing ones.

`JsonLanguage` and `JsonAnalyzer` handle JSON with comments (`.json`, `.jsonc`) with the same tokenizer;
`JsonCodeEditorExtensions` (`SetJsonValue`, `AddJsonProperty`, `EnsureJsonValue`, `RemoveJsonProperty`) edit a document by
path and keep its comments and layout.

See the [README](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.TypeScript.Transform/README.md).
