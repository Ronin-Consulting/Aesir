using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class ChatHistoryItemTests : TestContext
{
    public ChatHistoryItemTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_WithSessionTitle()
    {
        // Arrange
        var session = new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = "Test Chat",
            UpdatedAt = DateTimeOffset.Now
        };

        // Act
        var cut = RenderComponent<ChatHistoryItem>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("Test Chat");
    }

    [Fact]
    public void DisplaysNewChat_WhenTitleIsEmpty()
    {
        // Arrange
        var session = new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = "",
            UpdatedAt = DateTimeOffset.Now
        };

        // Act
        var cut = RenderComponent<ChatHistoryItem>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("New Chat");
    }

    [Fact]
    public void DisplaysNewChat_WhenTitleIsNull()
    {
        // Arrange
        var session = new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = null!,
            UpdatedAt = DateTimeOffset.Now
        };

        // Act
        var cut = RenderComponent<ChatHistoryItem>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("New Chat");
    }

    [Fact]
    public void HasChatHistoryItemClass()
    {
        // Arrange
        var session = new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTimeOffset.Now
        };

        // Act
        var cut = RenderComponent<ChatHistoryItem>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("chat-history-item");
    }

    [Fact]
    public void ShowsTimestamp_WhenEnabled()
    {
        // Arrange
        var session = new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = new DateTimeOffset(2024, 1, 15, 14, 30, 0, TimeSpan.Zero)
        };

        // Act
        var cut = RenderComponent<ChatHistoryItem>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ShowTimestamp, true));

        // Assert
        cut.Markup.Should().Contain("item-timestamp");
    }

    [Fact]
    public void HidesTimestamp_WhenDisabled()
    {
        // Arrange
        var session = new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTimeOffset.Now
        };

        // Act
        var cut = RenderComponent<ChatHistoryItem>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ShowTimestamp, false));

        // Assert
        cut.FindAll(".item-timestamp").Should().BeEmpty();
    }

    [Fact]
    public void AppliesSelectedClass_WhenIsSelectedTrue()
    {
        // Arrange
        var session = new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTimeOffset.Now
        };

        // Act
        var cut = RenderComponent<ChatHistoryItem>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.IsSelected, true));

        // Assert
        cut.Markup.Should().Contain("selected");
    }

    [Fact]
    public void DoesNotApplySelectedClass_WhenIsSelectedFalse()
    {
        // Arrange
        var session = new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTimeOffset.Now
        };

        // Act
        var cut = RenderComponent<ChatHistoryItem>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.IsSelected, false));

        // Assert
        cut.Find(".chat-history-item").ClassList.Should().NotContain("selected");
    }

    [Fact]
    public void InvokesOnClick_WhenClicked()
    {
        // Arrange
        var session = new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTimeOffset.Now
        };
        AesirChatSessionItem? clickedSession = null;

        var cut = RenderComponent<ChatHistoryItem>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.OnClick, EventCallback.Factory.Create<AesirChatSessionItem>(this, s => clickedSession = s)));

        // Act
        cut.Find(".chat-history-item").Click();

        // Assert
        clickedSession.Should().NotBeNull();
        clickedSession!.Id.Should().Be(session.Id);
    }

    [Fact]
    public void HasActionsMenuWhenSelected()
    {
        // Arrange
        var session = new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTimeOffset.Now
        };

        var cut = RenderComponent<ChatHistoryItem>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.IsSelected, true));

        // Assert - Component has actions menu when selected
        cut.Markup.Should().Contain("item-actions");
    }

    [Fact]
    public void HasChatIcon()
    {
        // Arrange
        var session = new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTimeOffset.Now
        };

        // Act
        var cut = RenderComponent<ChatHistoryItem>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("item-icon");
    }

    [Fact]
    public void HasItemContentSection()
    {
        // Arrange
        var session = new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            UpdatedAt = DateTimeOffset.Now
        };

        // Act
        var cut = RenderComponent<ChatHistoryItem>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("item-content");
    }

    [Fact]
    public void TruncatesLongTitles()
    {
        // Arrange
        var session = new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = "This is a very long chat title that should be truncated in the display",
            UpdatedAt = DateTimeOffset.Now
        };

        // Act
        var cut = RenderComponent<ChatHistoryItem>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert - Check for CSS class that handles truncation
        cut.Markup.Should().Contain("item-title");
    }
}
