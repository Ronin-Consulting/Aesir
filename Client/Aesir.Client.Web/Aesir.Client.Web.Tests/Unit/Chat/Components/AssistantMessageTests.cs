using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class AssistantMessageTests : TestContext
{
    public AssistantMessageTests()
    {
        Services.AddSingleton<IMarkdownService, MarkdownService>();
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_AssistantMessage_WithContent()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "Hello! How can I help you today?"
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("Hello! How can I help you today?");
    }

    [Fact]
    public void HasAssistantMessageClass()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "Test message"
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("assistant-message");
    }

    [Fact]
    public void HasMessageContentClass()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "Test message"
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("message-content");
    }

    [Fact]
    public void AcceptsAgentNameParameter()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "Test message"
        };

        // Act - AgentName parameter exists but is not displayed in Claude-style UI
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.AgentName, "Claude"));

        // Assert - Component renders successfully with AgentName parameter
        cut.Markup.Should().Contain("assistant-message");
    }

    [Fact]
    public void RendersMarkdown_InContent()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "This is **bold** text"
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert - Markdown should be converted to HTML
        cut.Markup.Should().Contain("<strong>bold</strong>");
    }

    [Fact]
    public void RendersCodeBlocks()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "```csharp\nvar x = 1;\n```"
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("<code");
    }

    [Fact]
    public void ShowsThinkingSection_WhenThoughtsPresent_AndShowThinkingEnabled()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "The answer is 42.",
            ThoughtsContent = "Let me think about this..."
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.ShowThinking, true));

        // Assert - Claude-style collapsible thinking section
        cut.Markup.Should().Contain("thinking-section");
        cut.Markup.Should().Contain("thinking-toggle");
    }

    [Fact]
    public void HidesThinkingSection_WhenShowThinkingDisabled()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "The answer is 42.",
            ThoughtsContent = "Let me think about this..."
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.ShowThinking, false));

        // Assert - Check that no thinking-section div element exists
        cut.FindAll(".thinking-section").Should().BeEmpty();
    }

    [Fact]
    public void HidesThinkingSection_WhenNoThoughtsContent()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "The answer is 42.",
            ThoughtsContent = null
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.ShowThinking, true));

        // Assert - Check that no thinking-section div element exists
        cut.FindAll(".thinking-section").Should().BeEmpty();
    }

    [Fact]
    public void HasMessageMetadata()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "Test message"
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("message-metadata");
    }

    [Fact]
    public void HasResponseActions()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "Test message"
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("response-actions");
    }

    [Fact]
    public void RendersWithoutError_WhenEmptyContent()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = ""
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().NotBeEmpty();
    }

    [Fact]
    public void HasMarkdownBodyClass()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "Test message"
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("markdown-body");
    }

    [Fact]
    public void RendersList_InMarkdown()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "- Item 1\n- Item 2\n- Item 3"
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("<li>");
    }

    [Fact]
    public void WhenStreamingAndLatestMessage_ShouldNotShowBrandIcon()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "Streaming content..."
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.IsLatestMessage, true)
            .Add(p => p.IsStreaming, true));

        // Assert - Brand icon section should NOT be present during streaming
        cut.FindAll(".brand-section-large").Should().BeEmpty();
    }

    [Fact]
    public void WhenNotStreamingAndLatestMessage_ShouldShowBrandIcon()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "Completed message"
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.IsLatestMessage, true)
            .Add(p => p.IsStreaming, false));

        // Assert - Brand icon section should be present when not streaming and is latest message
        cut.FindAll(".brand-section-large").Should().NotBeEmpty();
    }

    [Fact]
    public void WhenStreamingAndNotLatestMessage_ShouldNotShowBrandIcon()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "Some content"
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.IsLatestMessage, false)
            .Add(p => p.IsStreaming, true));

        // Assert - Brand icon section should NOT be present when not latest message
        cut.FindAll(".brand-section-large").Should().BeEmpty();
    }

    [Fact]
    public void WhenNotLatestMessage_ShouldNotShowBrandIcon()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "assistant",
            Content = "Earlier message"
        };

        // Act
        var cut = RenderComponent<AssistantMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.IsLatestMessage, false)
            .Add(p => p.IsStreaming, false));

        // Assert - Brand icon section should NOT be present for non-latest messages
        cut.FindAll(".brand-section-large").Should().BeEmpty();
    }
}
