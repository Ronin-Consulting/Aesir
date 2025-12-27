using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class ToolCallCardTests : TestContext
{
    public ToolCallCardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Rendering Tests

    [Fact]
    public void Renders_ToolCallCard()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Started, ToolCallType.WebSearch);

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert
        cut.Markup.Should().Contain("tool-call-card");
    }

    [Fact]
    public void Renders_FunctionName()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Started, ToolCallType.WebSearch);
        toolCall.FunctionName = "SearchTheWeb";

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert
        cut.Markup.Should().Contain("Search The Web");
    }

    [Fact]
    public void Renders_PluginBadge_WhenPluginNameProvided()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Started, ToolCallType.WebSearch);
        toolCall.PluginName = "WebPlugin";

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert
        cut.Markup.Should().Contain("plugin-badge");
        cut.Markup.Should().Contain("WebPlugin");
    }

    [Fact]
    public void DoesNotRender_PluginBadge_WhenPluginNameEmpty()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Started, ToolCallType.WebSearch);
        toolCall.PluginName = null;

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert - no element with plugin-badge class
        var badges = cut.FindAll(".plugin-badge");
        badges.Should().BeEmpty();
    }

    [Fact]
    public void Renders_Duration_WhenProvided()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Completed, ToolCallType.WebSearch);
        toolCall.StartedAt = DateTimeOffset.UtcNow.AddMilliseconds(-500);
        toolCall.CompletedAt = DateTimeOffset.UtcNow;

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert
        cut.Markup.Should().Contain("duration");
        cut.Markup.Should().Contain("ms");
    }

    #endregion

    #region Status Icon Tests

    [Fact]
    public void Renders_Spinner_WhenStatusStarted()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Started, ToolCallType.WebSearch);

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert
        cut.Markup.Should().Contain("mud-progress-circular");
    }

    [Fact]
    public void Renders_CheckIcon_WhenStatusCompleted()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Completed, ToolCallType.WebSearch);

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert
        cut.Markup.Should().Contain("status-icon");
        cut.Markup.Should().Contain("completed");
    }

    [Fact]
    public void Renders_ErrorIcon_WhenStatusFailed()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Failed, ToolCallType.WebSearch);

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert
        cut.Markup.Should().Contain("status-icon");
        cut.Markup.Should().Contain("failed");
    }

    #endregion

    #region Color Coding Tests

    [Theory]
    [InlineData(ToolCallType.DocumentSearch, "tool-document-search")]
    [InlineData(ToolCallType.WebSearch, "tool-web-search")]
    [InlineData(ToolCallType.McpServer, "tool-mcp-server")]
    [InlineData(ToolCallType.ImageAnalysis, "tool-image-analysis")]
    [InlineData(ToolCallType.Summarization, "tool-summarization")]
    [InlineData(ToolCallType.Other, "tool-other")]
    public void Renders_CorrectColorClass_ForToolType(ToolCallType toolType, string expectedClass)
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Started, toolType);

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert
        cut.Markup.Should().Contain(expectedClass);
    }

    [Fact]
    public void Renders_StatusStartedClass_WhenInProgress()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Started, ToolCallType.WebSearch);

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert
        cut.Markup.Should().Contain("status-started");
    }

    [Fact]
    public void Renders_StatusFailedClass_WhenFailed()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Failed, ToolCallType.WebSearch);

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert
        cut.Markup.Should().Contain("status-failed");
    }

    #endregion

    #region Expand/Collapse Tests

    [Fact]
    public void IsCollapsed_ByDefault()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Completed, ToolCallType.WebSearch);
        toolCall.Description = "Test description";

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert - no element with tool-call-details class (style tags still render the CSS selector)
        var details = cut.FindAll(".tool-call-details");
        details.Should().BeEmpty();
    }

    [Fact]
    public void IsExpanded_WhenDefaultExpandedTrue()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Completed, ToolCallType.WebSearch);
        toolCall.Description = "Test description";

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall)
            .Add(p => p.DefaultExpanded, true));

        // Assert
        cut.Markup.Should().Contain("tool-call-details");
    }

    [Fact]
    public void TogglesExpansion_OnHeaderClick()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Completed, ToolCallType.WebSearch);
        toolCall.Description = "Test description";

        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert initial state - no element with tool-call-details class
        var detailsInitial = cut.FindAll(".tool-call-details");
        detailsInitial.Should().BeEmpty();

        // Act - click header
        var header = cut.Find(".tool-call-header");
        header.Click();

        // Assert expanded
        var detailsExpanded = cut.FindAll(".tool-call-details");
        detailsExpanded.Should().HaveCount(1);

        // Act - click again
        header.Click();

        // Assert collapsed - no element with tool-call-details class
        var detailsCollapsed = cut.FindAll(".tool-call-details");
        detailsCollapsed.Should().BeEmpty();
    }

    #endregion

    #region Details Content Tests

    [Fact]
    public void ShowsDescription_WhenExpanded()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Completed, ToolCallType.WebSearch);
        toolCall.Description = "This is a test description";

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall)
            .Add(p => p.DefaultExpanded, true));

        // Assert
        cut.Markup.Should().Contain("This is a test description");
    }

    [Fact]
    public void ShowsArguments_WhenExpanded()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Completed, ToolCallType.WebSearch);
        toolCall.Arguments = new Dictionary<string, string>
        {
            ["query"] = "test query",
            ["limit"] = "10"
        };

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall)
            .Add(p => p.DefaultExpanded, true));

        // Assert
        cut.Markup.Should().Contain("query");
        cut.Markup.Should().Contain("test query");
        cut.Markup.Should().Contain("limit");
        cut.Markup.Should().Contain("10");
    }

    [Fact]
    public void ShowsResult_WhenExpanded()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Completed, ToolCallType.WebSearch);
        toolCall.Result = "Search result content here";

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall)
            .Add(p => p.DefaultExpanded, true));

        // Assert
        cut.Markup.Should().Contain("result-section");
        cut.Markup.Should().Contain("Search result content here");
    }

    [Fact]
    public void ShowsError_WhenFailedAndExpanded()
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Failed, ToolCallType.WebSearch);
        toolCall.Error = "Something went wrong";

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall)
            .Add(p => p.DefaultExpanded, true));

        // Assert
        cut.Markup.Should().Contain("error-section");
        cut.Markup.Should().Contain("Something went wrong");
    }

    #endregion

    #region Display Name Transformation Tests

    [Theory]
    [InlineData("get_weather", "weather")]
    [InlineData("search_documents", "documents")]
    [InlineData("WebSearch", "Web Search")]
    [InlineData("HybridDocumentSearch", "Hybrid Document Search")]
    public void TransformsFunctionName_ToHumanReadable(string functionName, string expectedContains)
    {
        // Arrange
        var toolCall = CreateToolCall(ToolCallStatus.Started, ToolCallType.Other);
        toolCall.FunctionName = functionName;

        // Act
        var cut = RenderComponent<ToolCallCard>(parameters => parameters
            .Add(p => p.ToolCall, toolCall));

        // Assert
        cut.Markup.Should().Contain(expectedContains);
    }

    #endregion

    #region Helper Methods

    private static AesirToolCallInfo CreateToolCall(ToolCallStatus status, ToolCallType toolType)
    {
        return new AesirToolCallInfo
        {
            ToolCallId = Guid.NewGuid().ToString(),
            FunctionName = "TestFunction",
            PluginName = "TestPlugin",
            Status = status,
            ToolType = toolType,
            StartedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
