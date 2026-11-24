The HTML language of [CK.Transform.Core](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.Transform.Core/README.md):
transformer functions that modify `.html` and `.htm` files.

Register `HtmlLanguage` in a `TransformerHost` to apply `create <html> transformer` functions to HTML texts.
`HtmlAnalyzer` parses the text into tokens (see `TokenTypeExtensions`) and `HtmlElementSpan` spans; the injection
points are HTML comments (`<!-- <Name /> -->`).

See the [README](https://github.com/signature-opensource/CK-Transform/blob/stable/CK.Html.Transform/README.md).
