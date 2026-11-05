using System;

namespace CK.Less.Transform;

/// <summary>
/// Defines the possible <c>@import (keyword) 'file';</c>.
/// </summary>
[Flags]
public enum ImportKeyword
{
    /// <summary>
    /// No import keyword.
    /// </summary>
    None = 0,

    /// <summary>
    /// Use a Less file but do not output it.
    /// </summary>
    Reference  = 1,

    /// <summary>
    /// Include the source file in the output but do not process it.
    /// </summary>
    Inline = 1 << 1,

    /// <summary>
    /// Treat the file as a Less file, no matter what the file extension.
    /// This excludes <see cref="Css"/>.
    /// </summary>
    Less = 1 << 2,

    /// <summary>
    /// Treat the file as a CSS file, no matter what the file extension.
    /// This excludes <see cref="Less"/>.
    /// </summary>
    Css = 1 << 3,

    /// <summary>
    /// Only include the file once (this is default behavior).
    /// This excludes <see cref="Multiple"/>.
    /// </summary>
    Once = 1 << 4,

    /// <summary>
    /// Include the file multiple times.
    /// This excludes <see cref="Once"/>.
    /// </summary>
    Multiple = 1 << 5,

    /// <summary>
    /// Continue compiling when file is not found.
    /// </summary>
    Optional = 1 << 6
}

/// <summary>
/// Extension methods of <see cref="ImportKeyword"/> that handle its mutually exclusive keywords.
/// </summary>
public static class ImportKeywordExtensions
{
    /// <summary>
    /// Removes the mutually exclusive keywords: <see cref="ImportKeyword.Multiple"/> wins over <see cref="ImportKeyword.Once"/>
    /// and <see cref="ImportKeyword.Less"/> wins over <see cref="ImportKeyword.Css"/>.
    /// </summary>
    /// <param name="k">This keywords.</param>
    /// <returns>The normalized keywords.</returns>
    public static ImportKeyword Normalize( this ImportKeyword k )
    {
        // Multiple has the priority over the 'once' default.
        if( (k & ImportKeyword.Multiple) != 0 ) k &= ~ImportKeyword.Once;
        if( (k & ImportKeyword.Once) != 0 ) k &= ~ImportKeyword.Multiple;

        // Less has the priority over 'css'.
        if( (k & ImportKeyword.Less) != 0 ) k &= ~ImportKeyword.Css;
        if( (k & ImportKeyword.Css) != 0 ) k &= ~ImportKeyword.Less;

        return k;
    }

    /// <summary>
    /// Includes and excludes keywords. An included <see cref="ImportKeyword.Css"/> excludes <see cref="ImportKeyword.Less"/>
    /// and an included <see cref="ImportKeyword.Once"/> excludes <see cref="ImportKeyword.Multiple"/>: the keywords to
    /// include win over the existing ones. The result is <see cref="Normalize(ImportKeyword)">normalized</see>.
    /// </summary>
    /// <param name="k">This keywords.</param>
    /// <param name="include">The keywords to include.</param>
    /// <param name="exclude">The keywords to exclude.</param>
    /// <returns>The resulting keywords.</returns>
    public static ImportKeyword Apply( this ImportKeyword k, ImportKeyword include, ImportKeyword exclude )
    {
        // Accounting the priorities.
        if( (include & ImportKeyword.Css) != 0 ) exclude |= ImportKeyword.Less;
        if( (include & ImportKeyword.Once) != 0 ) exclude |= ImportKeyword.Multiple;
        var newOne = (k & ~exclude) | include;
        return Normalize( newOne );
    }

}
