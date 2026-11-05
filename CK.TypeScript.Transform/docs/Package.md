The TypeScript language of [CK.Transform.Core](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.Transform.Core/README.md)
for the `.ts` files: register `TypeScriptLanguage` in a `TransformerHost` to apply `create <ts> transformer` functions
to TypeScript texts. `TypeScriptAnalyzer` exposes the `ImportStatement` and `ClassDefinition` spans and the span
specifications `{braces}`, `{brackets}`, `{parens}`, `{class}` and `{import}`; the `ensure import ...;` statement
(`EnsureImportStatement`, also callable from code with an `ImportLine`) merges an import into the existing ones.
See the [README](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.TypeScript.Transform/README.md).
