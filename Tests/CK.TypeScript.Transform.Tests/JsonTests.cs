using CK.Core;
using CK.Transform.Core;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using static CK.Testing.MonitorTestHelper;

namespace CK.TypeScript.Transform.Tests;

[TestFixture]
public class JsonTests
{
    const string AuthCenter = "Builders:Auth3/Auth3.Auth3.Builder:App:Local:AuthCenter";

    static string ReadSample() => File.ReadAllText( TestHelper.TestProjectFolder.AppendPart( "Samples" ).AppendPart( "ckonf.json" ) );

    static string Edit( string text, Func<SourceCodeEditor, bool> edit )
    {
        var analyzer = new JsonAnalyzer();
        var code = analyzer.ParseOrThrow( text );
        using var editor = new SourceCodeEditor( TestHelper.Monitor, code, analyzer );
        edit( editor ).ShouldBeTrue();
        var result = editor.Code.ToString();
        // The result is valid JSON(C).
        new JsonAnalyzer().Parse( result.AsMemory() ).Success.ShouldBeTrue( result );
        return result;
    }

    [Test]
    public void the_sample_round_trip_is_exact()
    {
        var text = ReadSample();
        var code = new JsonAnalyzer().ParseOrThrow( text );
        code.ToString().ShouldBe( text );
        var rpId = code.FindJsonProperty( AuthCenter + ":Passkey:RpId" );
        rpId.ShouldNotBeNull();
        code.GetJsonValueText( rpId ).ShouldBe( "\"localhost\"" );
        code.FindJsonObject( AuthCenter )!.Properties.ShouldContain( p => p.Name == "Oidc" );
        code.FindJsonProperty( "builders:auth3/auth3.auth3.builder", StringComparison.OrdinalIgnoreCase ).ShouldNotBeNull();
        code.FindJsonProperty( "builders:auth3/auth3.auth3.builder" ).ShouldBeNull();
    }

    [Test]
    public void SetJsonValue_on_the_sample()
    {
        var text = ReadSample();
        var result = Edit( text, e => e.SetJsonValue( AuthCenter + ":Passkey:RpId", "\"example.com\"" ) );
        result.ShouldBe( text.Replace( "\"RpId\": \"localhost\"", "\"RpId\": \"example.com\"" ) );
    }

    [Test]
    public void AddJsonProperty_after_a_commented_object_on_the_sample()
    {
        var text = ReadSample();
        var result = Edit( text, e => e.AddJsonProperty( AuthCenter, "PasswordLost", "true" ) );
        var before = "\r\n            }\r\n            // The approval page";
        var after = "\r\n            },\r\n            \"PasswordLost\": true\r\n            // The approval page";
        text.IndexOf( before ).ShouldBeGreaterThan( 0 );
        result.ShouldBe( text.Replace( before, after ) );
    }

    [Test]
    public void AddJsonProperty_with_a_multi_line_value_on_the_sample()
    {
        var text = ReadSample();
        var result = Edit( text, e => e.AddJsonProperty( AuthCenter + ":Oidc", "Google", """
                                                                                {
                                                                                  "Authority": "https://accounts.google.com",
                                                                                  "Scopes": [ "openid" ]
                                                                                }
                                                                                """ ) );
        var before = "              }\r\n              // Another scheme";
        var after = "              },\r\n"
                    + "              \"Google\": {\r\n"
                    + "                \"Authority\": \"https://accounts.google.com\",\r\n"
                    + "                \"Scopes\": [ \"openid\" ]\r\n"
                    + "              }\r\n"
                    + "              // Another scheme";
        text.IndexOf( before ).ShouldBeGreaterThan( 0 );
        result.ShouldBe( text.Replace( before, after ) );
    }

    [Test]
    public void RemoveJsonProperty_last_one_on_the_sample()
    {
        var text = ReadSample();
        var result = Edit( text, e => e.RemoveJsonProperty( AuthCenter + ":Passkey" ) );
        int start = text.IndexOf( "},\r\n            \"Passkey\": {" );
        int end = text.IndexOf( "            // The approval page" );
        start.ShouldBeGreaterThan( 0 );
        result.ShouldBe( text.Substring( 0, start ) + "}\r\n" + text.Substring( end ) );
    }

    [Test]
    public void RemoveJsonProperty_then_AddJsonProperty_on_the_sample()
    {
        var text = ReadSample();
        var result = Edit( text, e => e.RemoveJsonProperty( AuthCenter + ":SessionLifetime" )
                                      && e.AddJsonProperty( AuthCenter + ":Passkey", "Timeout", "\"00:05:00\"" ) );
        var expected = text.Replace( "            \"SessionLifetime\": \"1.00:00:00\",\r\n", "" )
                           .Replace( "\"RpName\": \"CK Sample\"\r\n", "\"RpName\": \"CK Sample\",\r\n              \"Timeout\": \"00:05:00\"\r\n" );
        result.ShouldBe( expected );
    }

    [TestCase( "{\n  \"a\": 1 // c\n}", "{\n  \"a\": 1, // c\n  \"b\": 2\n}" )]
    [TestCase( "{ \"a\": 1 }", "{ \"a\": 1, \"b\": 2 }" )]
    [TestCase( "{\n  \"a\": 1,\n}", "{\n  \"a\": 1,\n  \"b\": 2,\n}" )]
    [TestCase( "{\n\t\"x\": 0,\n\t\"a\": 1\n}", "{\n\t\"x\": 0,\n\t\"a\": 1,\n\t\"b\": 2\n}" )]
    public void AddJsonProperty_layout( string text, string expected )
    {
        Edit( text, e => e.AddJsonProperty( "", "b", "2" ) ).ShouldBe( expected );
    }

    [Test]
    public void AddJsonProperty_in_an_empty_object_without_new_line_uses_the_environment_one()
    {
        Edit( "{}", e => e.AddJsonProperty( "", "b", "2" ) ).ShouldBe( $"{{{Environment.NewLine}  \"b\": 2{Environment.NewLine}}}" );
    }

    [Test]
    public void AddJsonProperty_in_an_empty_nested_object()
    {
        Edit( "{\n    \"x\": {}\n}", e => e.AddJsonProperty( "x", "a", "{\n    \"b\": true\n}" ) )
            .ShouldBe( "{\n    \"x\": {\n        \"a\": {\n            \"b\": true\n        }\n    }\n}" );
    }

    [TestCase( "{\n  // about a\n  \"a\": 1,\n  \"b\": 2\n}", "a", "{\n  \"b\": 2\n}" )]
    [TestCase( "{\n  \"a\": 1, // about a\n  \"b\": 2 // about b\n}", "b", "{\n  \"a\": 1 // about a\n}" )]
    [TestCase( "{\n  \"x\": {\n    \"a\": 1\n  }\n}", "x:a", "{\n  \"x\": {}\n}" )]
    [TestCase( "{ \"a\": 1, \"b\": 2 }", "b", "{ \"a\": 1 }" )]
    [TestCase( "{ \"a\": 1, \"b\": 2 }", "a", "{ \"b\": 2 }" )]
    [TestCase( "{\n  \"a\": 1,\n  \"b\": 2,\n}", "b", "{\n  \"a\": 1,\n}" )]
    public void RemoveJsonProperty_layout( string text, string path, string expected )
    {
        Edit( text, e => e.RemoveJsonProperty( path ) ).ShouldBe( expected );
    }

    [TestCase( "{\n  \"a\": /* c */ 1 // d\n}", "a", "2", "{\n  \"a\": /* c */ 2 // d\n}" )]
    [TestCase( "{\n  \"a\": 1\n}", "a", "{\n  \"b\": [ 1, 2 ]\n}", "{\n  \"a\": {\n    \"b\": [ 1, 2 ]\n  }\n}" )]
    [TestCase( "{\n  \"a\": { \"x\": 1 }\n}", "a", "null", "{\n  \"a\": null\n}" )]
    [TestCase( "{\n  \"a\": 1\n}", "b", "-3.5", "{\n  \"a\": 1,\n  \"b\": -3.5\n}" )]
    public void EnsureJsonValue( string text, string path, string value, string expected )
    {
        Edit( text, e => e.EnsureJsonValue( path, value ) ).ShouldBe( expected );
    }

    [TestCase( "{ \"a\" 1 }" )]
    [TestCase( "{ 'a': 1 }" )]
    [TestCase( "{ \"a\": undefined }" )]
    [TestCase( "[ 1, 2 " )]
    [TestCase( "{ \"a\": 1 } 2" )]
    [TestCase( "{ \"a\": 1, " )]
    [TestCase( "{ \"a\": 1" )]
    [TestCase( "{" )]
    [TestCase( "{ \"a\": " )]
    public void parse_errors( string text )
    {
        new JsonAnalyzer().Parse( text.AsMemory() ).Success.ShouldBeFalse();
    }

    [Test]
    public void edit_errors_are_logged_and_return_false()
    {
        var analyzer = new JsonAnalyzer();
        var code = analyzer.ParseOrThrow( "{ \"a\": 1 }" );
        using var editor = new SourceCodeEditor( TestHelper.Monitor, code, analyzer );
        using( TestHelper.Monitor.CollectEntries( out var entries ) )
        {
            editor.SetJsonValue( "missing", "1" ).ShouldBeFalse();
            editor.AddJsonProperty( "", "a", "2" ).ShouldBeFalse();
            editor.AddJsonProperty( "", "b", "{ oops" ).ShouldBeFalse();
            editor.RemoveJsonProperty( "a:b" ).ShouldBeFalse();
            entries.Count.ShouldBe( 4 );
        }
        editor.Code.ToString().ShouldBe( "{ \"a\": 1 }" );
    }

    [Test]
    public void JsonLanguage_handles_json_and_jsonc()
    {
        var host = new TransformerHost( new JsonLanguage() );
        host.FindLanguage( ".json", withFileExtensions: true ).ShouldNotBeNull();
        host.FindLanguage( ".jsonc", withFileExtensions: true ).ShouldNotBeNull();
    }
}
