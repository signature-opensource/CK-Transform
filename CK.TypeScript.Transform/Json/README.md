# The JSON language

A configuration file written in JSON is read by people: its comments are documentation and its layout matters. A tool
that changes one value must not reformat the file nor drop its comments. JSON is a subset of JavaScript: this language
reuses the TypeScript tokenizer, keeps the source text exactly (the comments and the whitespace are trivias of the
tokens), and edits a document by path, changing only the edited tokens.

## The language and the analyzer

[`JsonLanguage`](JsonLanguage.cs) is the `TransformLanguage` named `Json`, for the `.json` and `.jsonc` files. Its
transformers accept the statements of the core (no JSON specific statement yet).

[`JsonAnalyzer`](JsonAnalyzer.cs) parses one JSON value with the tokens of the `TypeScriptAnalyzer`. It accepts the line
(`//`) and star (`/* */`) comments and the trailing commas (JSONC). A string is double quoted (a single quoted one is an
error); the literals are `true`, `false` and `null`. Its spans ([JsonSpans.cs](JsonSpans.cs)):

| Span | Covers | Members |
|------|--------|---------|
| `JsonObjectSpan` | From `{` to `}`. Its children are its properties. | `Properties`, `FindProperty( name, comparison )` |
| `JsonArraySpan` | From `[` to `]`. Its children are the spans of its object and array items. | |
| `JsonPropertySpan` | The name token, the colon and the value (not the comma). | `Name` (unescaped), `NameIndex`, `ValueSpan`, `Value`, `ObjectValue`, `Owner` |

Its span specifications are `{braces}` (or `{{}}`), `{brackets}` (or `{[]}`), `{object}`, `{array}` and `{property}`.

## Paths

A path is a list of property names; its string form separates them with `:`, like the keys of the .NET configuration
(`"Builders:Auth3/Auth3.Auth3.Builder:App:Local:AuthCenter"`). The empty path is the root object. The lookups take a
`StringComparison`: JSON names are case sensitive (`Ordinal`, the default), the .NET configuration keys are not
(`OrdinalIgnoreCase`).

[`JsonSourceCodeExtensions`](JsonSourceCodeExtensions.cs) navigates a `SourceCode`: `GetJsonRootObject`,
`FindJsonObject`, `FindJsonProperty`, `GetJsonValueText`, and `JsonSourceCodeExtensions.SplitJsonPath`.

## Editing

[`JsonCodeEditorExtensions`](JsonCodeEditorExtensions.cs) edits through a `SourceCodeEditor` bound to the `JsonAnalyzer`
(or to the `JsonLanguage`): the code is reparsed after each edit. On failure (a missing path, an existing property, an
invalid value), an error is logged in the editor's monitor and the method returns false.

```csharp
var analyzer = new JsonAnalyzer();
var code = analyzer.ParseOrThrow( text );
using var editor = new SourceCodeEditor( monitor, code, analyzer );
editor.SetJsonValue( "App:Local:AuthCenter:Passkey:RpId", "\"example.com\"" );
editor.AddJsonProperty( "App:Local:AuthCenter", "PasswordLost", "true" );
editor.RemoveJsonProperty( "App:Local:AuthCenter:SessionLifetime" );
string result = editor.Code.ToString();
```

| Method | What it does to the layout |
|--------|----------------------------|
| `SetJsonValue( path, jsonValue )` | Replaces the value tokens; the trivias before and after the value (its comments) are kept. |
| `AddJsonProperty( objectPath, name, jsonValue )` | Adds the property after the last one, with the indentation of its siblings. The comma is inserted right after the previous value, before its trailing comment. A trailing comma style is kept. An empty object becomes a multi-line one. |
| `EnsureJsonValue( path, jsonValue )` | `SetJsonValue` when the property exists, `AddJsonProperty` in its object otherwise. |
| `RemoveJsonProperty( path )` | Removes the property, its comma, the comments on the lines above it and the comment after it on its line. The last property gives the comment of its previous comma back to the previous value. An object left empty without comment becomes `{}`. |

A value is given as JSON text written with a zero base indentation: a multi-line value is indented at the level of its
property. The line endings are the ones of the document (its first line ending, or `Environment.NewLine` when it has
none); the indentation unit is read from the document (two spaces when it has no nested property on its own line).

The tests are in [JsonTests](../../Tests/CK.TypeScript.Transform.Tests/JsonTests.cs): they edit a commented
configuration file ([Samples/ckonf.json](../../Tests/CK.TypeScript.Transform.Tests/Samples/ckonf.json)) and compare
the result with the original text, byte for byte.
