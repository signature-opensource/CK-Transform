# CK-Transform

Parsers and "transformer languages" for text files: a common infrastructure and the languages built on it, used by
the TypeScript engine to transform the files of the packages (HTML, Less/CSS and TypeScript).

| Package | Description | Latest stable |
|---------|-------------|---------------|
| [CK.Transform.Core](CK.Transform.Core/README.md) | The common basis of the parsers and of the transformer languages. | [![nuget](https://img.shields.io/nuget/v/CK.Transform.Core.svg?label=CK.Transform.Core)](https://www.nuget.org/packages/CK.Transform.Core/) |
| [CK.Html.Transform](CK.Html.Transform/README.md) | The HTML language (".html" and ".htm" files). | [![nuget](https://img.shields.io/nuget/v/CK.Html.Transform.svg?label=CK.Html.Transform)](https://www.nuget.org/packages/CK.Html.Transform/) |
| [CK.Less.Transform](CK.Less.Transform/README.md) | The Less language (".less" and ".css" files), with the statements that ensure an import. | [![nuget](https://img.shields.io/nuget/v/CK.Less.Transform.svg?label=CK.Less.Transform)](https://www.nuget.org/packages/CK.Less.Transform/) |
| [CK.TypeScript.Transform](CK.TypeScript.Transform/README.md) | The TypeScript language (".ts" files), with the statements that ensure an import. | [![nuget](https://img.shields.io/nuget/v/CK.TypeScript.Transform.svg?label=CK.TypeScript.Transform)](https://www.nuget.org/packages/CK.TypeScript.Transform/) |

The three languages build on `CK.Transform.Core`: read it first.
