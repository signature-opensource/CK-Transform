using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.TypeScript.Transform;

/// <summary>
/// Navigation in a <see cref="SourceCode"/> parsed by the <see cref="JsonAnalyzer"/>.
/// <para>
/// A path is a list of property names. Its string form separates them with ':' (like the .NET configuration keys):
/// <c>"Builders:App/App.Builder:App:Local"</c>. The empty path is the root object.
/// </para>
/// </summary>
public static class JsonSourceCodeExtensions
{
    /// <summary>
    /// Splits a ':' separated path. The empty string is the empty path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The property names.</returns>
    public static IReadOnlyList<string> SplitJsonPath( string path )
    {
        Throw.CheckNotNullArgument( path );
        return path.Length == 0 ? [] : path.Split( ':' );
    }

    /// <summary>
    /// Gets the root object (null if the root value is not an object).
    /// </summary>
    /// <param name="code">This source code.</param>
    /// <returns>The root object or null.</returns>
    public static JsonObjectSpan? GetJsonRootObject( this SourceCode code ) => code.Spans.FirstOrDefault() as JsonObjectSpan;

    /// <summary>
    /// Finds an object by its path.
    /// </summary>
    /// <param name="code">This source code.</param>
    /// <param name="path">The property names.</param>
    /// <param name="comparison">The name comparison.</param>
    /// <returns>The object or null.</returns>
    public static JsonObjectSpan? FindJsonObject( this SourceCode code,
                                              IReadOnlyList<string> path,
                                              StringComparison comparison = StringComparison.Ordinal )
    {
        if( path.Count == 0 ) return code.GetJsonRootObject();
        return FindJsonProperty( code, path, comparison )?.ObjectValue;
    }

    /// <inheritdoc cref="FindJsonObject(SourceCode, IReadOnlyList{string}, StringComparison)"/>
    public static JsonObjectSpan? FindJsonObject( this SourceCode code,
                                              string path,
                                              StringComparison comparison = StringComparison.Ordinal )
        => FindJsonObject( code, SplitJsonPath( path ), comparison );

    /// <summary>
    /// Finds a property by its path.
    /// </summary>
    /// <param name="code">This source code.</param>
    /// <param name="path">The property names (not empty).</param>
    /// <param name="comparison">The name comparison.</param>
    /// <returns>The property or null.</returns>
    public static JsonPropertySpan? FindJsonProperty( this SourceCode code,
                                                  IReadOnlyList<string> path,
                                                  StringComparison comparison = StringComparison.Ordinal )
    {
        Throw.CheckArgument( path.Count > 0 );
        var o = code.GetJsonRootObject();
        JsonPropertySpan? p = null;
        foreach( var name in path )
        {
            if( o == null ) return null;
            p = o.FindProperty( name, comparison );
            if( p == null ) return null;
            o = p.ObjectValue;
        }
        return p;
    }

    /// <inheritdoc cref="FindJsonProperty(SourceCode, IReadOnlyList{string}, StringComparison)"/>
    public static JsonPropertySpan? FindJsonProperty( this SourceCode code,
                                                  string path,
                                                  StringComparison comparison = StringComparison.Ordinal )
        => FindJsonProperty( code, SplitJsonPath( path ), comparison );

    /// <summary>
    /// Gets the source text of the value of a property.
    /// </summary>
    /// <param name="code">This source code.</param>
    /// <param name="property">The property.</param>
    /// <returns>The text of the value, without its leading and trailing trivias.</returns>
    public static string GetJsonValueText( this SourceCode code, JsonPropertySpan property )
    {
        var v = property.ValueSpan;
        var b = new System.Text.StringBuilder();
        for( int i = v.Beg; i < v.End; ++i )
        {
            var t = code.Tokens[i];
            if( i > v.Beg ) foreach( var l in t.LeadingTrivias ) b.Append( l.Content );
            b.Append( t.Text );
            if( i < v.End - 1 ) foreach( var l in t.TrailingTrivias ) b.Append( l.Content );
        }
        return b.ToString();
    }
}
