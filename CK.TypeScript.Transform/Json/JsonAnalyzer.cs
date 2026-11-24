using CK.Core;
using CK.Transform.Core;
using System;
using System.Text.Json;

namespace CK.TypeScript.Transform;

/// <summary>
/// JSON analyzer that accepts comments and trailing commas (JSONC).
/// <para>
/// JSON is a subset of JavaScript: the tokens are the ones of the <see cref="TypeScriptAnalyzer"/>. The source text is kept
/// exactly (the comments and the layout are trivias of the tokens). The spans are the <see cref="JsonObject"/>,
/// <see cref="JsonArray"/> and <see cref="JsonProperty"/>. The <see cref="JsonCodeEditorExtensions"/> edit them.
/// </para>
/// </summary>
public sealed class JsonAnalyzer : TargetLanguageAnalyzer
{
    /// <summary>
    /// Initializes a new JsonAnalyzer.
    /// </summary>
    public JsonAnalyzer()
        : base( JsonLanguage._languageName )
    {
    }

    /// <summary>
    /// Calls <see cref="TriviaHeadExtensions.AcceptCLikeLineComment(ref TriviaHead)"/>
    /// and <see cref="TriviaHeadExtensions.AcceptCLikeStarComment(ref TriviaHead)"/>.
    /// </summary>
    /// <param name="c">The trivia head.</param>
    protected override void ParseTrivia( ref TriviaHead c )
    {
        c.AcceptCLikeLineComment();
        c.AcceptCLikeStarComment();
    }

    /// <inheritdoc/>
    protected override LowLevelToken LowLevelTokenize( ReadOnlySpan<char> head ) => TypeScriptAnalyzer.Tokenize( head );

    /// <summary>
    /// Parses one JSON value (an empty text is valid: it has no token).
    /// </summary>
    /// <param name="head">The head.</param>
    protected override void DoParse( ref TokenizerHead head )
    {
        if( head.EndOfInput != null ) return;
        if( ParseValue( ref head ) && head.EndOfInput == null )
        {
            head.AppendUnexpectedToken();
        }
    }

    static bool ParseValue( ref TokenizerHead head )
    {
        switch( head.LowLevelTokenType )
        {
            case TokenType.OpenBrace:
                return ParseObject( ref head );
            case TokenType.OpenBracket:
                return ParseArray( ref head );
            case TokenType.GenericString:
                return AcceptString( ref head, "a string" );
            case TokenType.GenericNumber:
                head.AcceptLowLevelToken();
                return true;
            case TokenType.Minus:
                head.AcceptLowLevelToken();
                return head.MatchToken( TokenType.GenericNumber, "a number" ) is not TokenError;
            case TokenType.GenericIdentifier when head.LowLevelTokenText is "true" or "false" or "null":
                head.AcceptLowLevelToken();
                return true;
            case TokenType.None when head.EndOfInput is null:
                head.AppendError( "Unrecognized token.", 1 );
                return false;
            case TokenType.None or TokenType.EndOfInput:
                head.AppendMissingToken( "a JSON value" );
                return false;
            default:
                head.AppendError( "Expected a JSON value.", head.LowLevelTokenText.Length );
                return false;
        }
    }

    static bool AcceptString( ref TokenizerHead head, string description )
    {
        if( head.LowLevelTokenText[0] != '"' )
        {
            head.AppendError( $"Expected {description}: a JSON string uses double quotes.", head.LowLevelTokenText.Length );
            return false;
        }
        head.AcceptLowLevelToken();
        return true;
    }

    static bool ParseObject( ref TokenizerHead head )
    {
        Throw.DebugAssert( head.LowLevelTokenType is TokenType.OpenBrace );
        int beg = head.LastTokenIndex + 1;
        head.AcceptLowLevelToken();
        for(; ; )
        {
            if( head.LowLevelTokenType is TokenType.CloseBrace ) break;
            int propBeg = head.LastTokenIndex + 1;
            if( head.LowLevelTokenType is not TokenType.GenericString )
            {
                head.AppendMissingToken( "a property name or '}'" );
                return false;
            }
            if( !AcceptString( ref head, "a property name" ) ) return false;
            var name = UnescapeString( head.LastToken!.Text.Span );
            if( head.MatchToken( TokenType.Colon, "':'" ) is TokenError ) return false;
            if( !ParseValue( ref head ) ) return false;
            head.AddSpan( new JsonProperty( propBeg, head.LastTokenIndex + 1, name ) );
            if( head.LowLevelTokenType is TokenType.Comma )
            {
                // A trailing comma is accepted (JSONC).
                head.AcceptLowLevelToken();
                continue;
            }
            if( head.LowLevelTokenType is not TokenType.CloseBrace )
            {
                head.AppendMissingToken( "',' or '}'" );
                return false;
            }
        }
        head.AcceptLowLevelToken();
        head.AddSpan( new JsonObject( beg, head.LastTokenIndex + 1 ) );
        return true;
    }

    static bool ParseArray( ref TokenizerHead head )
    {
        Throw.DebugAssert( head.LowLevelTokenType is TokenType.OpenBracket );
        int beg = head.LastTokenIndex + 1;
        head.AcceptLowLevelToken();
        for(; ; )
        {
            if( head.LowLevelTokenType is TokenType.CloseBracket ) break;
            if( !ParseValue( ref head ) ) return false;
            if( head.LowLevelTokenType is TokenType.Comma )
            {
                head.AcceptLowLevelToken();
                continue;
            }
            if( head.LowLevelTokenType is not TokenType.CloseBracket )
            {
                head.AppendMissingToken( "',' or ']'" );
                return false;
            }
        }
        head.AcceptLowLevelToken();
        head.AddSpan( new JsonArray( beg, head.LastTokenIndex + 1 ) );
        return true;
    }

    internal static string UnescapeString( ReadOnlySpan<char> quoted )
    {
        var inner = quoted.Slice( 1, quoted.Length - 2 );
        return inner.IndexOf( '\\' ) < 0
                ? inner.ToString()
                : JsonSerializer.Deserialize<string>( quoted )!;
    }

    /// <summary>
    /// Handles "{braces}" (or "{}"), "{brackets}" (or "[]"), "{object}", "{array}" and "{property}".
    /// </summary>
    /// <param name="tokenSpec">The span specification to analyze.</param>
    /// <returns>The provider or an error string.</returns>
    protected override object ParseSpanSpec( BalancedString tokenSpec )
    {
        var spanSpec = tokenSpec.InnerText.Trim();
        return spanSpec switch
        {
            "{}" or "braces" => new SpanEnclosedOperator( "{braces}", ( tokens, idx ) => tokens[idx].TokenType switch
                                {
                                    TokenType.OpenBrace => EnclosingTokenType.Open,
                                    TokenType.CloseBrace => EnclosingTokenType.Close,
                                    _ => EnclosingTokenType.None
                                } ),
            "[]" or "brackets" => new SpanEnclosedOperator( "{brackets}", ( tokens, idx ) => tokens[idx].TokenType switch
                                {
                                    TokenType.OpenBracket => EnclosingTokenType.Open,
                                    TokenType.CloseBracket => EnclosingTokenType.Close,
                                    _ => EnclosingTokenType.None
                                } ),
            "object" => new SpanTypeOperator( "{object}", typeof( JsonObject ) ),
            "array" => new SpanTypeOperator( "{array}", typeof( JsonArray ) ),
            "property" => new SpanTypeOperator( "{property}", typeof( JsonProperty ) ),
            _ => $$"""
                   Invalid span type '{spanSpec}'.
                   Allowed are "braces", "{}", "brackets", "[]", "object", "array", "property".
                   """
        };
    }

    /// <inheritdoc/>
    protected override Trivia CreateInjectionPointTrivia( InjectionPoint target,
                                                          InjectionPoint.Kind syntax,
                                                          bool inlineIfPossible )
    {
        var tag = InjectionPoint.GetString( target.Name, syntax );
        return inlineIfPossible
                ? new Trivia( TokenTypeExtensions.GetTriviaBlockCommentType( 2, 2 ), $"/* {tag} */" )
                : new Trivia( TokenTypeExtensions.GetTriviaLineCommentType( 2 ), $"// {tag}{Environment.NewLine}" );
    }
}
