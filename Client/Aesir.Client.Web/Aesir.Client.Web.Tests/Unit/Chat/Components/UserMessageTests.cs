using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Common.Models;
using Microsoft.JSInterop;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class UserMessageTests : TestContext
{
    private readonly Mock<IDocumentApiService> _mockDocumentApiService;

    public UserMessageTests()
    {
        _mockDocumentApiService = new Mock<IDocumentApiService>();

        Services.AddSingleton(_mockDocumentApiService.Object);
        Services.AddSingleton<IMarkdownService, MarkdownService>();
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Render MudPopoverProvider first (required for MudBlazor popover components)
        RenderComponent<MudPopoverProvider>();
    }

    [Fact]
    public void Renders_UserMessage_WithContent()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Hello, how are you?"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("Hello, how are you?");
    }

    [Fact]
    public void HasUserMessageClass()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Test message"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("user-message");
    }

    [Fact]
    public void HasMessageBubbleClass()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Test message"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("message-bubble");
    }

    [Fact]
    public void ShowsFileAttachment_WhenMessageHasFile()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "<file>document.pdf</file>What is in this file?"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("document.pdf");
        cut.Markup.Should().Contain("file-card");
    }

    [Fact]
    public void HidesFileTag_InDisplayedContent()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "<file>document.pdf</file>What is in this file?"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert - Should show file name and content, but not the raw file tag
        cut.Markup.Should().Contain("What is in this file?");
        cut.Markup.Should().NotContain("<file>");
    }

    [Fact]
    public void DoesNotShowFileAttachment_WhenNoFile()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Just a regular message"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert - Check that no file-card div element exists
        cut.FindAll(".file-card").Should().BeEmpty();
    }

    [Fact]
    public void PreservesWhitespace_InContent()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Line 1\nLine 2"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert - Newlines should become <br> tags for visual preservation
        cut.Markup.Should().Contain("Line 1");
        cut.Markup.Should().Contain("Line 2");
        cut.Markup.Should().Contain("<br");
    }

    [Fact]
    public void RendersWithoutError_WhenEmptyContent()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = ""
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().NotBeEmpty();
    }

    [Fact]
    public void ShowsTimestamp_WhenCreatedAtIsSet()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Test message",
            CreatedAt = DateTimeOffset.Now.AddMinutes(-5)
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("message-timestamp");
    }

    [Fact]
    public void DoesNotShowTimestamp_WhenCreatedAtIsNull()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Test message",
            CreatedAt = null
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.FindAll(".message-timestamp").Should().BeEmpty();
    }

    [Fact]
    public void HasEditButton()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Test message"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("message-actions");
        cut.Markup.Should().Contain("Edit");
    }

    [Fact]
    public void HasCopyButton()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Test message"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("message-actions");
        cut.Markup.Should().Contain("Copy");
    }

    [Fact]
    public void DisablesEditButton_WhenStreaming()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Test message"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.IsStreaming, true));

        // Assert - The edit button should be disabled
        var editButton = cut.FindAll("button").FirstOrDefault(b => b.GetAttribute("title")?.Contains("Edit") == true);
        editButton.Should().NotBeNull();
        editButton!.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void HasRerunButton()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Test message"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("Re-run");
    }

    [Fact]
    public void HasMessageMetadata()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Test message"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("message-metadata");
    }

    [Fact]
    public void TimestampShowsTimeOnly_ForTodayMessages()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Test message",
            CreatedAt = DateTimeOffset.Now // Today's date
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert - Today's messages should show time only (AM or PM)
        cut.Markup.Should().Match("*message-timestamp*");
        // Should contain AM or PM for time-only format
        var hasTimeFormat = cut.Markup.Contains("AM") || cut.Markup.Contains("PM");
        hasTimeFormat.Should().BeTrue();
    }

    [Fact]
    public void TimestampShowsYesterday_ForYesterdayMessages()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Test message",
            CreatedAt = DateTimeOffset.Now.AddDays(-1) // Yesterday
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert - Yesterday's messages should show "Yesterday" with time
        cut.Markup.Should().Contain("Yesterday");
    }

    [Fact]
    public void TimestampShowsDateOnly_ForOlderMessages()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Test message",
            CreatedAt = DateTimeOffset.Now.AddDays(-5) // 5 days ago (same year)
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert - Older messages should show month and day (e.g., "Dec 8")
        var expectedMonth = DateTimeOffset.Now.AddDays(-5).ToString("MMM");
        cut.Markup.Should().Contain(expectedMonth);
    }

    #region Markdown Rendering Tests

    [Fact]
    public void RendersMarkdown_BoldText()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "This is **bold** text"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("<strong>bold</strong>");
    }

    [Fact]
    public void RendersMarkdown_ItalicText()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "This is *italic* text"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("<em>italic</em>");
    }

    [Fact]
    public void RendersMarkdown_InlineCode()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Use `code` here"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("<code>code</code>");
    }

    [Fact]
    public void RendersMarkdown_CodeBlock()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "```python\nprint('hello')\n```"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("<pre>");
        cut.Markup.Should().Contain("<code");
    }

    [Fact]
    public void RendersMarkdown_UnorderedList()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "- Item 1\n- Item 2"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("<ul>");
        cut.Markup.Should().Contain("<li>");
    }

    [Fact]
    public void RendersMarkdown_Link()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Check [this link](https://example.com)"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("<a href=\"https://example.com\"");
    }

    [Fact]
    public void HasUserMarkdownClass()
    {
        // Arrange
        var message = new AesirChatMessage
        {
            Role = "user",
            Content = "Test message"
        };

        // Act
        var cut = RenderComponent<UserMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("user-markdown");
    }

    #endregion
}
