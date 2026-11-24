using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CK.TypeScript.Transform;

/// <summary>
/// A JSON object: from its opening <c>{</c> to its closing <c>}</c>. Its children are its <see cref="JsonProperty"/>.
/// </summary>
public sealed class JsonObject : SourceSpan
{
    internal JsonObject( int beg, int end )
        : base( beg, end )
    {
    }

    /// <summary>
    /// Gets the properties of this object.
    /// </summary>
    public IEnumerable<JsonProperty> Properties => Children.OfType<JsonProperty>();

    /// <summary>
    /// Finds a property by its name.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="comparison">
    /// The name comparison. JSON names are case sensitive, the .NET configuration keys are not
    /// (use <see cref="StringComparison.OrdinalIgnoreCase"/> for them).
    /// </param>
    /// <returns>The property or null.</returns>
    public JsonProperty? FindProperty( string name, StringComparison comparison = StringComparison.Ordinal )
    {
        foreach( var p in Properties )
        {
            if( string.Equals( p.Name, name, comparison ) ) return p;
        }
        return null;
    }
}

/// <summary>
/// A JSON array: from its opening <c>[</c> to its closing <c>]</c>. Its children are the spans of its object and
/// array items (a scalar item has no span).
/// </summary>
public sealed class JsonArray : SourceSpan
{
    internal JsonArray( int beg, int end )
        : base( beg, end )
    {
    }
}

/// <summary>
/// A property of a <see cref="JsonObject"/>: its name token, the colon and its value (its comma is not part of it).
/// </summary>
public sealed class JsonProperty : SourceSpan
{
    internal JsonProperty( int beg, int end, string name )
        : base( beg, end )
    {
        Name = name;
    }

    /// <summary>
    /// Gets the property name (unescaped).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the index of the name token.
    /// </summary>
    public int NameIndex => Span.Beg;

    /// <summary>
    /// Gets the tokens of the value.
    /// </summary>
    public TokenSpan ValueSpan => new TokenSpan( Span.Beg + 2, Span.End );

    /// <summary>
    /// Gets the object or array value of this property, or null when the value is a scalar.
    /// </summary>
    public SourceSpan? Value => FirstChild;

    /// <summary>
    /// Gets the object value of this property or null.
    /// </summary>
    public JsonObject? ObjectValue => FirstChild as JsonObject;

    /// <summary>
    /// Gets the object that contains this property.
    /// </summary>
    public JsonObject? Owner => Parent as JsonObject;

    /// <inheritdoc/>
    public override string ToString() => $"JsonProperty '{Name}' {Span}";
}
