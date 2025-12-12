using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class ToolCallsSectionTests : TestContext
{
    public ToolCallsSectionTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Rendering Tests

    [Fact]
    public void DoesNotRender_MainContent_WhenToolCallsNull()
    {
        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, null));

        // Assert - the component renders no elements with this class (style tags still render the CSS)
        var elements = cut.FindAll(".tool-calls-section");
        elements.Should().BeEmpty();
    }

    [Fact]
    public void DoesNotRender_MainContent_WhenToolCallsEmpty()
    {
        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, new List<AesirToolCallInfo>()));

        // Assert - the component renders no elements with this class (style tags still render the CSS)
        var elements = cut.FindAll(".tool-calls-section");
        elements.Should().BeEmpty();
    }

    [Fact]
    public void Renders_WhenToolCallsExist()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Completed)
        };

        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Assert
        cut.Markup.Should().Contain("tool-calls-section");
    }

    [Fact]
    public void Renders_ToolsIcon()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Completed)
        };

        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Assert
        cut.Markup.Should().Contain("tools-icon");
    }

    #endregion

    #region Summary Text Tests

    [Fact]
    public void ShowsSummary_Using1Tool_WhenSingleToolInProgress()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Started)
        };

        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Assert
        cut.Markup.Should().Contain("Using 1 tool...");
    }

    [Fact]
    public void ShowsSummary_UsingNTools_WhenMultipleToolsInProgress()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Started),
            CreateToolCall("tc-2", ToolCallStatus.Started),
            CreateToolCall("tc-3", ToolCallStatus.Completed)
        };

        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Assert
        cut.Markup.Should().Contain("Using 3 tools...");
    }

    [Fact]
    public void ShowsSummary_Used1Tool_WhenSingleToolCompleted()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Completed)
        };

        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Assert
        cut.Markup.Should().Contain("Used 1 tool");
    }

    [Fact]
    public void ShowsSummary_UsedNTools_WhenMultipleToolsCompleted()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Completed),
            CreateToolCall("tc-2", ToolCallStatus.Completed)
        };

        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Assert
        cut.Markup.Should().Contain("Used 2 tools");
    }

    [Fact]
    public void ShowsSummary_WithFailedCount_WhenSomeFailed()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Completed),
            CreateToolCall("tc-2", ToolCallStatus.Completed),
            CreateToolCall("tc-3", ToolCallStatus.Failed)
        };

        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Assert
        cut.Markup.Should().Contain("Used 3 tools");
        cut.Markup.Should().Contain("2 completed");
        cut.Markup.Should().Contain("1 failed");
    }

    #endregion

    #region Active Spinner Tests

    [Fact]
    public void ShowsSpinner_WhenToolsActive()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Started)
        };

        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Assert
        cut.Markup.Should().Contain("active-spinner");
    }

    [Fact]
    public void HidesSpinner_WhenNoToolsActive()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Completed)
        };

        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Assert - no element with active-spinner class
        var spinners = cut.FindAll(".active-spinner");
        spinners.Should().BeEmpty();
    }

    #endregion

    #region Expand/Collapse Tests

    [Fact]
    public void IsCollapsed_ByDefault()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Completed)
        };

        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Assert - no content element rendered
        var content = cut.FindAll(".tool-calls-content");
        content.Should().BeEmpty();
    }

    [Fact]
    public void Expands_OnToggleClick()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Completed)
        };

        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Act
        var toggle = cut.Find(".tool-calls-toggle");
        toggle.Click();

        // Assert
        var content = cut.FindAll(".tool-calls-content");
        content.Should().HaveCount(1);
    }

    [Fact]
    public void Collapses_OnSecondToggleClick()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Completed)
        };

        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Act - expand
        var toggle = cut.Find(".tool-calls-toggle");
        toggle.Click();

        // Assert expanded
        var contentExpanded = cut.FindAll(".tool-calls-content");
        contentExpanded.Should().HaveCount(1);

        // Act - collapse
        toggle.Click();

        // Assert collapsed
        var contentCollapsed = cut.FindAll(".tool-calls-content");
        contentCollapsed.Should().BeEmpty();
    }

    [Fact]
    public void AutoExpands_WhenActiveToolCallsAndStreaming()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Started)
        };

        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls)
            .Add(p => p.AutoExpandWhenActive, true)
            .Add(p => p.IsStreaming, true));

        // Assert
        var content = cut.FindAll(".tool-calls-content");
        content.Should().HaveCount(1);
    }

    [Fact]
    public void DoesNotAutoExpand_WhenNotStreaming()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Started)
        };

        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls)
            .Add(p => p.AutoExpandWhenActive, true)
            .Add(p => p.IsStreaming, false));

        // Assert
        var content = cut.FindAll(".tool-calls-content");
        content.Should().BeEmpty();
    }

    [Fact]
    public void DoesNotAutoExpand_WhenAutoExpandDisabled()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Started)
        };

        // Act
        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls)
            .Add(p => p.AutoExpandWhenActive, false)
            .Add(p => p.IsStreaming, true));

        // Assert
        var content = cut.FindAll(".tool-calls-content");
        content.Should().BeEmpty();
    }

    #endregion

    #region Tool Call Cards Tests

    [Fact]
    public void Renders_ToolCallCards_WhenExpanded()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Completed),
            CreateToolCall("tc-2", ToolCallStatus.Completed)
        };

        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Act
        var toggle = cut.Find(".tool-calls-toggle");
        toggle.Click();

        // Assert
        var cards = cut.FindAll(".tool-call-card");
        cards.Should().HaveCount(2);
    }

    [Fact]
    public void AutoExpandsCards_ForActiveToolCalls()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Started),
            CreateToolCall("tc-2", ToolCallStatus.Completed)
        };
        toolCalls[0].Description = "Active tool description";
        toolCalls[1].Description = "Completed tool description";

        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls)
            .Add(p => p.IsStreaming, true)
            .Add(p => p.AutoExpandWhenActive, true));

        // The section should auto-expand when active
        // And the active tool card should be expanded by default (ShouldExpandCard returns true for Started)
        cut.Markup.Should().Contain("Active tool description");
    }

    [Fact]
    public void AutoExpandsCards_ForFailedToolCalls()
    {
        // Arrange
        var toolCalls = new List<AesirToolCallInfo>
        {
            CreateToolCall("tc-1", ToolCallStatus.Failed)
        };
        toolCalls[0].Error = "Something went wrong";

        var cut = RenderComponent<ToolCallsSection>(parameters => parameters
            .Add(p => p.ToolCalls, toolCalls));

        // Act - expand section
        var toggle = cut.Find(".tool-calls-toggle");
        toggle.Click();

        // Assert - failed card should be auto-expanded
        cut.Markup.Should().Contain("Something went wrong");
    }

    #endregion

    #region Helper Methods

    private static AesirToolCallInfo CreateToolCall(string id, ToolCallStatus status)
    {
        return new AesirToolCallInfo
        {
            ToolCallId = id,
            FunctionName = "TestFunction",
            PluginName = "TestPlugin",
            Status = status,
            ToolType = ToolCallType.Other,
            StartedAt = DateTimeOffset.UtcNow
        };
    }

    #endregion
}
