using CK.Transform.Core;

namespace CK.TypeScript.Transform;

/// <summary>
/// The JSON language (comments and trailing commas accepted). Accepts ".json" and ".jsonc" file name extensions.
/// </summary>
public sealed class JsonLanguage : TransformLanguage
{
    internal const string _languageName = "Json";

    /// <summary>
    /// Initializes a new JsonLanguage instance.
    /// </summary>
    public JsonLanguage()
        : base( _languageName, ".json", ".jsonc" )
    {
    }

    /// <inheritdoc/>
    protected override TransformLanguageAnalyzer CreateAnalyzer( TransformerHost.Language language )
    {
        return new JsonTransformStatementAnalyzer( language, new JsonAnalyzer() );
    }
}

sealed class JsonTransformStatementAnalyzer : TransformLanguageAnalyzer
{
    internal JsonTransformStatementAnalyzer( TransformerHost.Language language, JsonAnalyzer analyzer )
        : base( language, analyzer )
    {
    }
}
