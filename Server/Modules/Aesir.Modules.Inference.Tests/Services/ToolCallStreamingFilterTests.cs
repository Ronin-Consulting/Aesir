using Aesir.Common.Models;
using Aesir.Modules.Inference.Services;

namespace Aesir.Modules.Inference.Tests.Services;

public class ToolCallStreamingFilterTests
{
    #region DetermineToolType Tests

    [Theory]
    [InlineData("HybridDocumentSearch", null, ToolCallType.DocumentSearch)]
    [InlineData("hybriddocumentsearch", null, ToolCallType.DocumentSearch)]
    [InlineData("SemanticDocumentSearch", null, ToolCallType.DocumentSearch)]
    [InlineData("semanticdocumentsearch", null, ToolCallType.DocumentSearch)]
    [InlineData("DocumentSearch", null, ToolCallType.DocumentSearch)]
    [InlineData("documentsearch", null, ToolCallType.DocumentSearch)]
    // Note: "SearchDocuments" would not match since it checks for "documentsearch" substring
    public void DetermineToolType_ReturnsDocumentSearch_ForDocumentSearchFunctions(
        string functionName, string? pluginName, ToolCallType expected)
    {
        // Act
        var result = ToolCallStreamingFilter.DetermineToolType(functionName, pluginName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("WebSearch", null, ToolCallType.WebSearch)]
    [InlineData("websearch", null, ToolCallType.WebSearch)]
    [InlineData("web_search", null, ToolCallType.WebSearch)]
    [InlineData("search_web", null, ToolCallType.WebSearch)]
    // Note: "SearchWeb" would not match since it checks for "websearch", "web_search", or "search_web" substrings
    public void DetermineToolType_ReturnsWebSearch_ForWebSearchFunctions(
        string functionName, string? pluginName, ToolCallType expected)
    {
        // Act
        var result = ToolCallStreamingFilter.DetermineToolType(functionName, pluginName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("AnalyzeImage", null, ToolCallType.ImageAnalysis)]
    [InlineData("analyzeimage", null, ToolCallType.ImageAnalysis)]
    [InlineData("analyze_image", null, ToolCallType.ImageAnalysis)]
    [InlineData("ImageContent", null, ToolCallType.ImageAnalysis)]
    [InlineData("imagecontent", null, ToolCallType.ImageAnalysis)]
    public void DetermineToolType_ReturnsImageAnalysis_ForImageFunctions(
        string functionName, string? pluginName, ToolCallType expected)
    {
        // Act
        var result = ToolCallStreamingFilter.DetermineToolType(functionName, pluginName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Summarize", null, ToolCallType.Summarization)]
    [InlineData("summarize", null, ToolCallType.Summarization)]
    [InlineData("Summary", null, ToolCallType.Summarization)]
    [InlineData("GetSummary", null, ToolCallType.Summarization)]
    [InlineData("CreateSummary", null, ToolCallType.Summarization)]
    public void DetermineToolType_ReturnsSummarization_ForSummarizeFunctions(
        string functionName, string? pluginName, ToolCallType expected)
    {
        // Act
        var result = ToolCallStreamingFilter.DetermineToolType(functionName, pluginName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("SomeFunction", "MCP_FileSystem", ToolCallType.McpServer)]
    [InlineData("AnotherFunction", "MCPBrowser", ToolCallType.McpServer)]
    [InlineData("ReadFile", "mcp_filesystem", ToolCallType.McpServer)]
    [InlineData("ExecuteCommand", "MyMcpServer", ToolCallType.McpServer)]
    [InlineData("DoSomething", "TestMcpServerPlugin", ToolCallType.McpServer)]
    public void DetermineToolType_ReturnsMcpServer_ForMcpPlugins(
        string functionName, string? pluginName, ToolCallType expected)
    {
        // Act
        var result = ToolCallStreamingFilter.DetermineToolType(functionName, pluginName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("CustomFunction", null, ToolCallType.Other)]
    [InlineData("DoSomething", "RegularPlugin", ToolCallType.Other)]
    [InlineData("Calculate", "MathPlugin", ToolCallType.Other)]
    [InlineData("GetWeather", "WeatherPlugin", ToolCallType.Other)]
    public void DetermineToolType_ReturnsOther_ForUnknownFunctions(
        string functionName, string? pluginName, ToolCallType expected)
    {
        // Act
        var result = ToolCallStreamingFilter.DetermineToolType(functionName, pluginName);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void DetermineToolType_FunctionNameTakesPrecedence_OverPluginName()
    {
        // A function named "WebSearch" should be WebSearch even if in an MCP plugin
        var result = ToolCallStreamingFilter.DetermineToolType("WebSearch", "MCPPlugin");

        result.Should().Be(ToolCallType.WebSearch);
    }

    #endregion

    #region SerializeArgument Tests

    [Fact]
    public void SerializeArgument_ReturnsNull_ForNullValue()
    {
        // Act
        var result = ToolCallStreamingFilter.SerializeArgument(null);

        // Assert
        result.Should().Be("null");
    }

    [Fact]
    public void SerializeArgument_ReturnsString_ForStringValue()
    {
        // Act
        var result = ToolCallStreamingFilter.SerializeArgument("test string");

        // Assert
        result.Should().Be("test string");
    }

    [Fact]
    public void SerializeArgument_ReturnsJson_ForComplexObject()
    {
        // Arrange
        var obj = new { Name = "Test", Value = 42 };

        // Act
        var result = ToolCallStreamingFilter.SerializeArgument(obj);

        // Assert
        result.Should().Contain("Name");
        result.Should().Contain("Test");
        result.Should().Contain("Value");
        result.Should().Contain("42");
    }

    [Fact]
    public void SerializeArgument_TruncatesLongStrings()
    {
        // Arrange
        var longString = new string('x', 300);
        var obj = new { Data = longString };

        // Act
        var result = ToolCallStreamingFilter.SerializeArgument(obj);

        // Assert
        result.Length.Should().BeLessThanOrEqualTo(203); // 200 + "..."
        result.Should().EndWith("...");
    }

    [Fact]
    public void SerializeArgument_ReturnsToString_ForNonSerializableObject()
    {
        // Arrange - Type is not easily serializable
        var obj = typeof(string);

        // Act
        var result = ToolCallStreamingFilter.SerializeArgument(obj);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(42, "42")]
    [InlineData(3.14, "3.14")]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void SerializeArgument_SerializesPrimitives(object value, string expected)
    {
        // Act
        var result = ToolCallStreamingFilter.SerializeArgument(value);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void SerializeArgument_SerializesArray()
    {
        // Arrange
        var arr = new[] { 1, 2, 3 };

        // Act
        var result = ToolCallStreamingFilter.SerializeArgument(arr);

        // Assert
        result.Should().Be("[1,2,3]");
    }

    #endregion

    #region TruncateResult Tests

    [Fact]
    public void TruncateResult_ReturnsNull_ForNullResult()
    {
        // Act
        var result = ToolCallStreamingFilter.TruncateResult(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TruncateResult_ReturnsString_ForShortString()
    {
        // Act
        var result = ToolCallStreamingFilter.TruncateResult("short result");

        // Assert
        result.Should().Be("short result");
    }

    [Fact]
    public void TruncateResult_TruncatesLongString()
    {
        // Arrange
        var longString = new string('x', 600);

        // Act
        var result = ToolCallStreamingFilter.TruncateResult(longString);

        // Assert
        result.Should().NotBeNull();
        result!.Length.Should().Be(503); // 500 + "..."
        result.Should().EndWith("...");
    }

    [Fact]
    public void TruncateResult_SerializesObject_ThenTruncates()
    {
        // Arrange
        var obj = new { Data = new string('x', 600) };

        // Act
        var result = ToolCallStreamingFilter.TruncateResult(obj);

        // Assert
        result.Should().NotBeNull();
        result!.Length.Should().BeLessThanOrEqualTo(503);
    }

    [Fact]
    public void TruncateResult_DoesNotTruncate_ExactlyMaxLength()
    {
        // Arrange
        var exactString = new string('x', 500);

        // Act
        var result = ToolCallStreamingFilter.TruncateResult(exactString);

        // Assert
        result.Should().Be(exactString);
        result.Should().NotEndWith("...");
    }

    [Fact]
    public void TruncateResult_Truncates_OnePastMaxLength()
    {
        // Arrange
        var overString = new string('x', 501);

        // Act
        var result = ToolCallStreamingFilter.TruncateResult(overString);

        // Assert
        result.Should().EndWith("...");
        result!.Length.Should().Be(503);
    }

    [Fact]
    public void TruncateResult_HandlesEmptyString()
    {
        // Act
        var result = ToolCallStreamingFilter.TruncateResult("");

        // Assert
        result.Should().Be("");
    }

    [Theory]
    [InlineData(42)]
    [InlineData(3.14)]
    [InlineData(true)]
    public void TruncateResult_SerializesPrimitives(object value)
    {
        // Act
        var result = ToolCallStreamingFilter.TruncateResult(value);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    #endregion
}
