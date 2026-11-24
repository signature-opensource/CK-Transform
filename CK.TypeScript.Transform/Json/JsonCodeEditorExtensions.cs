using CK.Core;
using CK.Transform.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CK.TypeScript.Transform;

/// <summary>
/// Edits of a JSON(C) source code by path: the edited tokens change, the rest of the text (the comments, the layout) is
/// kept byte for byte. These are the building blocks of the JSON transform statements.
/// <para>
/// The <see cref="SourceCodeEditor"/> must be bound to a <see cref="JsonAnalyzer"/> (or to the <see cref="JsonLanguage"/>):
/// the code is reparsed after each edit. On failure, an error is logged in the editor's monitor and false is returned.
/// </para>
/// </summary>
public static class JsonCodeEditorExtensions
{
    static readonly JsonSerializerOptions _nameOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// Replaces the value of an existing property. The comments around the value are kept.
    /// </summary>
    /// <param name="editor">This editor.</param>
    /// <param name="path">The path of the property.</param>
    /// <param name="jsonValue">
    /// The new value (JSON). A multi-line value is indented at the level of the property (write it with a zero base
    /// indentation).
    /// </param>
    /// <param name="comparison">The property name comparison.</param>
    /// <returns>True on success, false on error.</returns>
    public static bool SetJsonValue( this SourceCodeEditor editor,
                                     IReadOnlyList<string> path,
                                     string jsonValue,
                                     StringComparison comparison = StringComparison.Ordinal )
    {
        if( !CheckEditor( editor ) ) return false;
        var p = FindProperty( editor, path, comparison );
        if( p == null ) return false;
        var tokens = editor.Code.Tokens;
        var layout = new Layout( editor.Code );
        var value = ParseValue( editor.Monitor, layout.Indent( jsonValue, layout.LineIndentation( p.NameIndex ) ) );
        if( value == null ) return false;
        var v = p.ValueSpan;
        value[0] = With( value[0], leading: tokens[v.Beg].LeadingTrivias );
        value[^1] = With( value[^1], trailing: tokens[v.End - 1].TrailingTrivias );
        // The span bookkeeping of the editor handles 1:1 replacements, whole span removals and insertions: a replacement
        // that changes the token count is done as a removal then an insertion (the code is reparsed after).
        using( var e = editor.OpenGlobalEditor() )
        {
            if( value.Count == v.Length )
            {
                e.Replace( v.Beg, value.ToArray() );
            }
            else
            {
                e.RemoveRange( v.Beg, v.Length );
                e.InsertAt( v.Beg, value.ToArray() );
            }
        }
        return editor.Reparse();
    }

    /// <inheritdoc cref="SetJsonValue(SourceCodeEditor, IReadOnlyList{string}, string, StringComparison)"/>
    public static bool SetJsonValue( this SourceCodeEditor editor,
                                     string path,
                                     string jsonValue,
                                     StringComparison comparison = StringComparison.Ordinal )
        => SetJsonValue( editor, JsonSourceCodeExtensions.SplitJsonPath( path ), jsonValue, comparison );

    /// <summary>
    /// Adds a property at the end of an object (an error if it already exists). It is written with the indentation of its
    /// siblings; a comma is inserted after the previous property, before its trailing comment if any.
    /// An empty object becomes a multi-line one.
    /// </summary>
    /// <param name="editor">This editor.</param>
    /// <param name="objectPath">The path of the object (empty for the root object).</param>
    /// <param name="name">The property name.</param>
    /// <param name="jsonValue">The value (JSON), written with a zero base indentation.</param>
    /// <param name="comparison">The property name comparison.</param>
    /// <returns>True on success, false on error.</returns>
    public static bool AddJsonProperty( this SourceCodeEditor editor,
                                        IReadOnlyList<string> objectPath,
                                        string name,
                                        string jsonValue,
                                        StringComparison comparison = StringComparison.Ordinal )
    {
        Throw.CheckNotNullArgument( name );
        if( !CheckEditor( editor ) ) return false;
        var o = editor.Code.FindJsonObject( objectPath, comparison );
        if( o == null )
        {
            editor.Monitor.Error( $"Unable to find the JSON object '{string.Join( ':', objectPath )}'." );
            return false;
        }
        if( o.FindProperty( name, comparison ) != null )
        {
            editor.Monitor.Error( $"The JSON object '{string.Join( ':', objectPath )}' already has a property '{name}'." );
            return false;
        }
        var tokens = editor.Code.Tokens;
        var layout = new Layout( editor.Code );
        var last = o.Properties.LastOrDefault();
        var objectIndentation = layout.LineIndentation( o.Span.Beg );
        if( last == null )
        {
            // An empty object: it becomes a multi-line one.
            var propertyIndentation = objectIndentation + layout.IndentUnit;
            var prop = ParseProperty( editor.Monitor, name, layout.Indent( jsonValue, propertyIndentation ) );
            if( prop == null ) return false;
            prop[0] = With( prop[0], leading: Whitespace( propertyIndentation ) );
            prop[^1] = With( prop[^1], trailing: Whitespace( layout.NewLine ) );
            int openIdx = o.Span.Beg;
            int closeIdx = o.Span.End - 1;
            var open = tokens[openIdx];
            var close = tokens[closeIdx];
            using( var e = editor.OpenGlobalEditor() )
            {
                e.Replace( openIdx, With( open, trailing: layout.EnsureEndsWithNewLine( open.TrailingTrivias ) ) );
                if( IsWhitespaceOnly( close.LeadingTrivias ) )
                {
                    e.Replace( closeIdx, With( close, leading: Whitespace( objectIndentation ) ) );
                }
                e.InsertAt( openIdx + 1, prop.ToArray() );
            }
            return editor.Reparse();
        }
        bool multiLine = layout.IsLineStart( last.NameIndex );
        var indentation = multiLine ? layout.LineIndentation( last.NameIndex ) : "";
        var newProp = ParseProperty( editor.Monitor, name, layout.Indent( jsonValue, indentation ) );
        if( newProp == null ) return false;
        int lastIdx = last.Span.End - 1;
        var lastToken = tokens[lastIdx];
        bool lastEndsLine = HasNewLine( lastToken.TrailingTrivias );
        if( tokens[last.Span.End].TokenType == TokenType.Comma )
        {
            // A trailing comma: the new property goes after it, with its own trailing comma.
            int commaIdx = last.Span.End;
            var comma = tokens[commaIdx];
            newProp[0] = With( newProp[0], leading: Whitespace( multiLine ? indentation : "" ) );
            newProp[^1] = With( newProp[^1], trailing: ImmutableArray<Trivia>.Empty );
            newProp.Add( new Token( TokenType.Comma,
                                    ImmutableArray<Trivia>.Empty,
                                    ",",
                                    HasNewLine( comma.TrailingTrivias ) ? Whitespace( layout.NewLine ) : Whitespace( " " ) ) );
            if( !multiLine && comma.TrailingTrivias.IsEmpty )
            {
                newProp[0] = With( newProp[0], leading: Whitespace( " " ) );
            }
            using( var e = editor.OpenGlobalEditor() )
            {
                e.InsertAt( commaIdx + 1, newProp.ToArray() );
            }
            return editor.Reparse();
        }
        // The comma takes the trailing trivias of the last value (a comment stays after the comma).
        ImmutableArray<Trivia> commaTrailing;
        ImmutableArray<Trivia> newLastTrailing;
        if( multiLine )
        {
            commaTrailing = lastEndsLine ? lastToken.TrailingTrivias : layout.EnsureEndsWithNewLine( lastToken.TrailingTrivias );
            newLastTrailing = lastEndsLine ? Whitespace( layout.NewLine ) : lastToken.TrailingTrivias;
            newProp[0] = With( newProp[0], leading: Whitespace( indentation ) );
        }
        else
        {
            commaTrailing = Whitespace( " " );
            newLastTrailing = lastToken.TrailingTrivias;
        }
        newProp[^1] = With( newProp[^1], trailing: newLastTrailing );
        newProp.Insert( 0, new Token( TokenType.Comma, ImmutableArray<Trivia>.Empty, ",", commaTrailing ) );
        using( var e = editor.OpenGlobalEditor() )
        {
            e.Replace( lastIdx, With( lastToken, trailing: ImmutableArray<Trivia>.Empty ) );
            e.InsertAt( lastIdx + 1, newProp.ToArray() );
        }
        return editor.Reparse();
    }

    /// <inheritdoc cref="AddJsonProperty(SourceCodeEditor, IReadOnlyList{string}, string, string, StringComparison)"/>
    public static bool AddJsonProperty( this SourceCodeEditor editor,
                                        string objectPath,
                                        string name,
                                        string jsonValue,
                                        StringComparison comparison = StringComparison.Ordinal )
        => AddJsonProperty( editor, JsonSourceCodeExtensions.SplitJsonPath( objectPath ), name, jsonValue, comparison );

    /// <summary>
    /// Sets the value of a property, adding it at the end of its object if it doesn't exist (the object must exist).
    /// </summary>
    /// <param name="editor">This editor.</param>
    /// <param name="path">The path of the property.</param>
    /// <param name="jsonValue">The value (JSON), written with a zero base indentation.</param>
    /// <param name="comparison">The property name comparison.</param>
    /// <returns>True on success, false on error.</returns>
    public static bool EnsureJsonValue( this SourceCodeEditor editor,
                                        IReadOnlyList<string> path,
                                        string jsonValue,
                                        StringComparison comparison = StringComparison.Ordinal )
    {
        Throw.CheckArgument( path.Count > 0 );
        if( !CheckEditor( editor ) ) return false;
        return editor.Code.FindJsonProperty( path, comparison ) != null
                ? SetJsonValue( editor, path, jsonValue, comparison )
                : AddJsonProperty( editor, path.Take( path.Count - 1 ).ToArray(), path[^1], jsonValue, comparison );
    }

    /// <inheritdoc cref="EnsureJsonValue(SourceCodeEditor, IReadOnlyList{string}, string, StringComparison)"/>
    public static bool EnsureJsonValue( this SourceCodeEditor editor,
                                        string path,
                                        string jsonValue,
                                        StringComparison comparison = StringComparison.Ordinal )
        => EnsureJsonValue( editor, JsonSourceCodeExtensions.SplitJsonPath( path ), jsonValue, comparison );

    /// <summary>
    /// Removes a property with its comma, the comments on the lines above it and the comment that follows it on its line.
    /// An object that becomes empty is written <c>{}</c> when it has no comment.
    /// </summary>
    /// <param name="editor">This editor.</param>
    /// <param name="path">The path of the property.</param>
    /// <param name="comparison">The property name comparison.</param>
    /// <returns>True on success, false on error.</returns>
    public static bool RemoveJsonProperty( this SourceCodeEditor editor,
                                           IReadOnlyList<string> path,
                                           StringComparison comparison = StringComparison.Ordinal )
    {
        if( !CheckEditor( editor ) ) return false;
        var p = FindProperty( editor, path, comparison );
        if( p == null ) return false;
        var o = p.Owner;
        Throw.DebugAssert( o != null );
        var tokens = editor.Code.Tokens;
        int beg = p.Span.Beg;
        int end = p.Span.End;
        using( var e = editor.OpenGlobalEditor() )
        {
            if( tokens[end].TokenType == TokenType.Comma )
            {
                // Removes the property and its comma (and the comment after the comma).
                e.RemoveRange( beg, end - beg + 1 );
            }
            else if( tokens[beg - 1].TokenType == TokenType.Comma )
            {
                // The last property: removes the previous comma. The previous value keeps the comment of the comma
                // (it was about it) or takes the end of line of the removed property.
                var comma = tokens[beg - 1];
                var previous = tokens[beg - 2];
                var trailing = HasNewLine( comma.TrailingTrivias )
                                ? comma.TrailingTrivias
                                : tokens[end - 1].TrailingTrivias;
                e.Replace( beg - 2, With( previous, trailing: trailing ) );
                e.RemoveRange( beg - 1, end - beg + 1 );
            }
            else
            {
                // The only property.
                var open = tokens[beg - 1];
                var close = tokens[end];
                if( IsWhitespaceOnly( open.TrailingTrivias ) && IsWhitespaceOnly( close.LeadingTrivias ) )
                {
                    e.Replace( beg - 1, With( open, trailing: ImmutableArray<Trivia>.Empty ) );
                    e.Replace( end, With( close, leading: ImmutableArray<Trivia>.Empty ) );
                }
                e.RemoveRange( beg, end - beg );
            }
        }
        return editor.Reparse();
    }

    /// <inheritdoc cref="RemoveJsonProperty(SourceCodeEditor, IReadOnlyList{string}, StringComparison)"/>
    public static bool RemoveJsonProperty( this SourceCodeEditor editor,
                                           string path,
                                           StringComparison comparison = StringComparison.Ordinal )
        => RemoveJsonProperty( editor, JsonSourceCodeExtensions.SplitJsonPath( path ), comparison );

    static bool CheckEditor( SourceCodeEditor editor )
    {
        Throw.CheckNotNullArgument( editor );
        if( editor.Analyzer is not JsonAnalyzer )
        {
            editor.Monitor.Error( "The SourceCodeEditor must be bound to the JsonAnalyzer to edit JSON." );
            return false;
        }
        if( editor.NeedReparse && !editor.Reparse() ) return false;
        return true;
    }

    static JsonProperty? FindProperty( SourceCodeEditor editor, IReadOnlyList<string> path, StringComparison comparison )
    {
        Throw.CheckArgument( path.Count > 0 );
        var p = editor.Code.FindJsonProperty( path, comparison );
        if( p == null )
        {
            editor.Monitor.Error( $"Unable to find the JSON property '{string.Join( ':', path )}'." );
        }
        return p;
    }

    static List<Token>? ParseValue( IActivityMonitor monitor, string jsonValue )
    {
        var r = new JsonAnalyzer().Parse( jsonValue.AsMemory() );
        if( !r.Success || r.SourceCode.Tokens.Count == 0 )
        {
            monitor.Error( $"Invalid JSON value: {r.FirstError?.ErrorMessage ?? "empty"}{Environment.NewLine}{jsonValue}" );
            return null;
        }
        return Detach( r.SourceCode.Tokens );
    }

    static List<Token>? ParseProperty( IActivityMonitor monitor, string name, string jsonValue )
    {
        var quoted = JsonSerializer.Serialize( name, _nameOptions );
        var r = new JsonAnalyzer().Parse( $"{{{quoted}: {jsonValue}}}".AsMemory() );
        if( !r.Success )
        {
            monitor.Error( $"Invalid JSON value: {r.FirstError?.ErrorMessage}{Environment.NewLine}{jsonValue}" );
            return null;
        }
        var t = r.SourceCode.Tokens;
        return Detach( t.Skip( 1 ).Take( t.Count - 2 ) );
    }

    // The parsed tokens reference their own text: they are kept as they are (their text is the inserted text).
    static List<Token> Detach( IEnumerable<Token> tokens ) => tokens.ToList();

    static Token With( Token t, ImmutableArray<Trivia>? leading = null, ImmutableArray<Trivia>? trailing = null )
    {
        return new Token( t.TokenType, leading ?? t.LeadingTrivias, t.Text, trailing ?? t.TrailingTrivias );
    }

    static ImmutableArray<Trivia> Whitespace( string s ) => s.Length == 0
                                                            ? ImmutableArray<Trivia>.Empty
                                                            : [new Trivia( TokenType.Whitespace, s )];

    static bool HasNewLine( ImmutableArray<Trivia> trivias )
    {
        foreach( var t in trivias ) if( t.Content.Span.Contains( '\n' ) ) return true;
        return false;
    }

    static bool IsWhitespaceOnly( ImmutableArray<Trivia> trivias )
    {
        foreach( var t in trivias ) if( t.TokenType != TokenType.Whitespace ) return false;
        return true;
    }

    // The layout of the edited code: its new line, its indentation unit, the indentation of a line.
    readonly struct Layout
    {
        readonly IReadOnlyList<Token> _tokens;
        public readonly string NewLine;
        public readonly string IndentUnit;

        public Layout( SourceCode code )
        {
            _tokens = code.Tokens;
            var text = code.ToString();
            int iNL = text.IndexOf( '\n' );
            NewLine = iNL > 0 && text[iNL - 1] == '\r' ? "\r\n" : iNL >= 0 ? "\n" : Environment.NewLine;
            IndentUnit = "  ";
            foreach( var o in code.Spans.AllSpans.OfType<JsonObject>() )
            {
                var first = o.Properties.FirstOrDefault();
                if( first != null && IsLineStart( first.NameIndex ) )
                {
                    var objectIndentation = LineIndentation( o.Span.Beg );
                    var propertyIndentation = LineIndentation( first.NameIndex );
                    if( propertyIndentation.Length > objectIndentation.Length
                        && propertyIndentation.StartsWith( objectIndentation, StringComparison.Ordinal ) )
                    {
                        IndentUnit = propertyIndentation.Substring( objectIndentation.Length );
                        break;
                    }
                }
            }
        }

        public bool IsLineStart( int index )
        {
            if( index == 0 ) return true;
            return HasNewLine( _tokens[index - 1].TrailingTrivias ) || HasNewLine( _tokens[index].LeadingTrivias );
        }

        // The indentation of the line of the token at index: the whitespace that starts the line.
        public string LineIndentation( int index )
        {
            while( index > 0 && !IsLineStart( index ) ) --index;
            var leading = new StringBuilder();
            foreach( var t in _tokens[index].LeadingTrivias ) leading.Append( t.Content );
            var s = leading.ToString();
            int iNL = s.LastIndexOf( '\n' );
            if( iNL >= 0 ) s = s.Substring( iNL + 1 );
            int i = 0;
            while( i < s.Length && (s[i] == ' ' || s[i] == '\t') ) ++i;
            return s.Substring( 0, i );
        }

        public string Indent( string jsonValue, string indentation )
        {
            var v = jsonValue.Replace( "\r\n", "\n" ).Trim();
            return v.Replace( "\n", NewLine + indentation );
        }

        public ImmutableArray<Trivia> EnsureEndsWithNewLine( ImmutableArray<Trivia> trivias )
        {
            return HasNewLine( trivias ) ? trivias : trivias.Add( new Trivia( TokenType.Whitespace, NewLine ) );
        }
    }
}
