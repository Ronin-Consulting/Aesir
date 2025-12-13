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

        // Assert - Check that the content is preserved with newlines
        cut.Markup.Should().Contain("Line 1");
        cut.Markup.Should().Contain("Line 2");
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
    public void TimestampAlwaysIncludesDate()
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

        // Assert - Should contain year in timestamp even for today's date
        var year = DateTime.Now.Year.ToString();
        cut.Markup.Should().Contain(year);
    }
}
