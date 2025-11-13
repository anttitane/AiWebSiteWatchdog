using AiWebSiteWatchDog.Application.Parsing;
using FluentAssertions;
using Xunit;

namespace AiWebSiteWatchDog.Tests.Application;

public class GeminiResponseParserTests
{
    [Fact]
    public void ExtractText_ReturnsNull_OnNullOrWhitespace()
    {
        GeminiResponseParser.ExtractText(null).Should().BeNull();
        GeminiResponseParser.ExtractText("   ").Should().BeNull();
    }

    [Fact]
    public void ExtractText_ParsesPrimaryText()
    {
        var json = """
        {
          "candidates": [
            {
              "content": { "parts": [ { "text": "Hello world\n" } ] }
            }
          ]
        }
        """;
        GeminiResponseParser.ExtractText(json).Should().Be("Hello world");
    }

    [Fact]
    public void ExtractText_FallbackConcatenatesAllParts()
    {
        // Force fallback by ensuring candidates[0].content.parts[0] has no 'text'
        var json = """
        {
          "candidates": [
            {
              "content": { "parts": [ { "foo": "no-text" }, { "text": "Line1" } ] }
            },
            {
              "content": { "parts": [ { "text": "Line2" } ] }
            }
          ]
        }
        """;
        GeminiResponseParser.ExtractText(json).Should().Be("Line1\nLine2");
    }

    [Fact]
    public void ExtractText_InvalidJson_ReturnsTrimmedOriginal()
    {
        var original = "not-json\n";
        GeminiResponseParser.ExtractText(original).Should().Be("not-json");
    }
}
