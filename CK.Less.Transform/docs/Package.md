The Less language of [CK.Transform.Core](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.Transform.Core/README.md)
for the `.less` and `.css` files: register `LessLanguage` in a `TransformerHost` to apply `create <less> transformer`
functions to style sheets. `LessAnalyzer` parses the text and exposes each `@import` as an `ImportStatement`; the
`ensure @import (keywords) 'path';` statement (`EnsureImportStatement`) adds an import or updates its `ImportKeyword`,
and `EnsureImportStatement.EnsureOrderedImports` ensures an ordered list of imports from code.
See the [README](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.Less.Transform/README.md).
