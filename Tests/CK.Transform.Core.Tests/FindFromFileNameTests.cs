using CK.Core;
using NUnit.Framework;
using Shouldly;

namespace CK.Transform.Core.Tests;

public class FindFromFileNameTests
{
    [TestCase( "file.test", ".test" )]
    [TestCase( "Some/Path/file.test", ".test" )]
    public void a_registered_language_is_found_by_its_file_extension( string fileName, string expectedExtension )
    {
        var h = new TransformerHost( new TestLanguage() );
        var l = h.FindFromFileName( fileName, out var extension ).ShouldNotBeNull();
        l.TransformLanguage.ShouldBeOfType<TestLanguage>();
        l.IsAutoLanguage.ShouldBeFalse();
        extension.ToString().ShouldBe( expectedExtension );
    }

    [TestCase( "file.test.t.t", ".test.t.t" )]
    [TestCase( "file.test.T.t", ".test.T.t" )]
    [TestCase( "file.test.t.T", ".test.t.T" )]
    public void a_transformer_of_the_transformers_of_a_registered_language_is_an_auto_language( string fileName, string expectedExtension )
    {
        var h = new TransformerHost( new TestLanguage() );
        var l = h.FindFromFileName( fileName, out var extension ).ShouldNotBeNull();
        l.IsAutoLanguage.ShouldBeTrue();
        l.TransformLanguage.ShouldNotBeSameAs( TransformerHost.RootTransformLanguage );
        extension.ToString().ShouldBe( expectedExtension );
    }

    // A transformer file is read by the root transformer language: the language of its functions
    // comes from their "create <language> transformer" header (and the name before the ".t" can be a hint).
    [TestCase( "file.test.t", ".t" )]
    [TestCase( "file.test.T", ".T" )]
    [TestCase( "file.unknown.t", ".t" )]
    [TestCase( "file.unknown.T", ".T" )]
    [TestCase( "file.t", ".t" )]
    public void a_transformer_file_is_read_by_the_root_language( string fileName, string expectedExtension )
    {
        var h = new TransformerHost( new TestLanguage() );
        var l = h.FindFromFileName( fileName, out var extension ).ShouldNotBeNull();
        l.TransformLanguage.ShouldBeSameAs( TransformerHost.RootTransformLanguage );
        extension.ToString().ShouldBe( expectedExtension );
    }

    [TestCase( "" )]
    [TestCase( "file" )]
    [TestCase( "file.unknown" )]
    [TestCase( "file.testT" )]
    [TestCase( "file.test.tT" )]
    public void other_names_have_no_language( string fileName )
    {
        var h = new TransformerHost( new TestLanguage() );
        h.FindFromFileName( fileName, out var extension ).ShouldBeNull();
        extension.IsEmpty.ShouldBeTrue();
    }
}
