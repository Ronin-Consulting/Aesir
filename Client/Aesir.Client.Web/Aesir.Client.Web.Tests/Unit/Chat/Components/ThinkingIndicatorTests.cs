using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Client.Web.Infrastructure.Services;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class ThinkingIndicatorTests : TestContext
{
    public ThinkingIndicatorTests()
    {
        Services.AddSingleton<IMarkdownService, MarkdownService>();
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_WithAgentName()
    {
        // Act
        var cut = RenderComponent<ThinkingIndicator>(parameters => parameters
            .Add(p => p.AgentName, "Claude"));

        // Assert
        cut.Markup.Should().Contain("Claude is thinking...");
    }

    [Fact]
    public void Renders_WithDefaultAgentName()
    {
        // Act
        var cut = RenderComponent<ThinkingIndicator>();

        // Assert
        cut.Markup.Should().Contain("Agent is thinking...");
    }

    [Fact]
    public void HasThinkingIndicatorClass()
    {
        // Act
        var cut = RenderComponent<ThinkingIndicator>();

        // Assert
        cut.Markup.Should().Contain("thinking-indicator");
    }

    [Fact]
    public void HasThinkingBubbleClass()
    {
        // Act
        var cut = RenderComponent<ThinkingIndicator>();

        // Assert
        cut.Markup.Should().Contain("thinking-bubble");
    }

    [Fact]
    public void HasProgressIndicator()
    {
        // Act
        var cut = RenderComponent<ThinkingIndicator>();

        // Assert
        cut.Markup.Should().Contain("mud-progress-circular");
    }

    [Fact]
    public void ShowsThinkingContent_WhenProvided()
    {
        // Act
        var cut = RenderComponent<ThinkingIndicator>(parameters => parameters
            .Add(p => p.ThinkingContent, "Let me analyze this problem..."));

        // Assert
        cut.Markup.Should().Contain("Let me analyze this problem...");
    }

    [Fact]
    public void HasShowHideButton_WhenThinkingContentProvided()
    {
        // Act
        var cut = RenderComponent<ThinkingIndicator>(parameters => parameters
            .Add(p => p.ThinkingContent, "Some thinking content"));

        // Assert
        cut.Markup.Should().Contain("Show");
        cut.Markup.Should().Contain("thinking");
    }

    [Fact]
    public void DoesNotShowButton_WhenNoThinkingContent()
    {
        // Act
        var cut = RenderComponent<ThinkingIndicator>(parameters => parameters
            .Add(p => p.ThinkingContent, null));

        // Assert
        // Should not have the show/hide button when no thinking content
        var buttons = cut.FindAll("button");
        buttons.Should().NotContain(b => b.TextContent.Contains("thinking"));
    }

    [Fact]
    public void RendersMarkdown_InThinkingContent()
    {
        // Act
        var cut = RenderComponent<ThinkingIndicator>(parameters => parameters
            .Add(p => p.ThinkingContent, "This is **important**"));

        // Assert
        cut.Markup.Should().Contain("<strong>important</strong>");
    }

    [Fact]
    public void RendersWithoutError_WhenEmptyThinkingContent()
    {
        // Act
        var cut = RenderComponent<ThinkingIndicator>(parameters => parameters
            .Add(p => p.ThinkingContent, ""));

        // Assert
        cut.Markup.Should().NotBeEmpty();
    }

    [Fact]
    public void HasThinkingHeader()
    {
        // Act
        var cut = RenderComponent<ThinkingIndicator>();

        // Assert
        cut.Markup.Should().Contain("thinking-header");
    }
}
