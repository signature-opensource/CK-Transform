using CK.Core;
using NUnit.Framework;
using Shouldly;
using static CK.Testing.MonitorTestHelper;

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

    // A "x.test.t" contains transformers of "test" items: its language is the auto language "Test.T"
    // (the language of the transformers of test transformers).
    [TestCase( "file.test.t", ".test.t" )]
    [TestCase( "file.test.T", ".test.T" )]
    public void a_transformer_of_a_registered_language_has_the_auto_language( string fileName, string expectedExtension )
    {
        var h = new TransformerHost( new TestLanguage() );
        var l = h.FindFromFileName( fileName, out var extension ).ShouldNotBeNull();
        l.IsAutoLanguage.ShouldBeTrue();
        l.IsTransformLanguage.ShouldBeTrue();
        l.TransformLanguage.ShouldNotBeSameAs( TransformerHost.RootTransformLanguage );
        extension.ToString().ShouldBe( expectedExtension );
    }

    // Above one level, and for an unknown language, the root transformer language is used.
    [TestCase( "file.test.t.t", ".test.t.t" )]
    [TestCase( "file.test.T.t", ".test.T.t" )]
    [TestCase( "file.unknown.t", ".t" )]
    [TestCase( "file.unknown.T", ".T" )]
    [TestCase( "file.t", ".t" )]
    public void other_transformer_files_have_the_root_language( string fileName, string expectedExtension )
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

    // The language of a "create <language> transformer" header: <test.t> is the auto language of the
    // transformers of test transformers, whatever has been parsed before.
    [TestCase( false )]
    [TestCase( true )]
    public void the_header_language_of_a_transformer_of_transformer( bool parseDeeperFirst )
    {
        var h = new TransformerHost( new TestLanguage() );
        if( parseDeeperFirst )
        {
            h.TryParseFunction( TestHelper.Monitor, """create <test.t.t> transformer on "X" begin end""" )
             .ShouldNotBeNull()
             .Language.TransformLanguage.ShouldBeSameAs( TransformerHost.RootTransformLanguage );
        }
        var f = h.TryParseFunction( TestHelper.Monitor, """create <test.t> transformer on "X" begin end""" ).ShouldNotBeNull();
        f.Language.IsAutoLanguage.ShouldBeTrue();
        f.Language.IsTransformLanguage.ShouldBeTrue();
        f.Language.TransformLanguage.ShouldNotBeSameAs( TransformerHost.RootTransformLanguage );

        h.TryParseFunction( TestHelper.Monitor, """create <test> transformer begin end""" )
         .ShouldNotBeNull()
         .Language.TransformLanguage.ShouldBeOfType<TestLanguage>();
        h.TryParseFunction( TestHelper.Monitor, """create <t> transformer on "X" begin end""" )
         .ShouldNotBeNull()
         .Language.TransformLanguage.ShouldBeSameAs( TransformerHost.RootTransformLanguage );
    }
}
