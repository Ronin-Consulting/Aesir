using Aesir.Common.Models;
using FluentAssertions;
using Xunit;

namespace Aesir.Infrastructure.Tests.Models;

/// <summary>
/// Tests for AesirChatMessage to ensure research team messages work correctly.
/// These tests verify that research team messages use the standard "assistant" role
/// while still being identifiable via IsResearchTeamMessage property.
/// </summary>
public class AesirChatMessageTests
{
    [Fact]
    public void NewResearchTeamMessage_SetsRoleToAssistant()
    {
        // Arrange
        var content = "Research report content";
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var teamName = "Test Team";

        // Act
        var message = AesirChatMessage.NewResearchTeamMessage(content, sessionId, teamId, teamName);

        // Assert - Role should be "assistant" for LLM API compatibility
        message.Role.Should().Be("assistant");
    }

    [Fact]
    public void NewResearchTeamMessage_SetsResearchTeamId()
    {
        // Arrange
        var content = "Research report content";
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var teamName = "Test Team";

        // Act
        var message = AesirChatMessage.NewResearchTeamMessage(content, sessionId, teamId, teamName);

        // Assert
        message.ResearchTeamId.Should().Be(teamId);
    }

    [Fact]
    public void NewResearchTeamMessage_SetsResearchSessionId()
    {
        // Arrange
        var content = "Research report content";
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var teamName = "Test Team";

        // Act
        var message = AesirChatMessage.NewResearchTeamMessage(content, sessionId, teamId, teamName);

        // Assert
        message.ResearchSessionId.Should().Be(sessionId);
    }

    [Fact]
    public void NewResearchTeamMessage_SetsResearchTeamName()
    {
        // Arrange
        var content = "Research report content";
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var teamName = "Test Team";

        // Act
        var message = AesirChatMessage.NewResearchTeamMessage(content, sessionId, teamId, teamName);

        // Assert
        message.ResearchTeamName.Should().Be(teamName);
    }

    [Fact]
    public void IsResearchTeamMessage_ReturnsTrue_WhenResearchTeamIdIsSet()
    {
        // Arrange
        var message = AesirChatMessage.NewResearchTeamMessage(
            "content",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Team");

        // Act & Assert
        message.IsResearchTeamMessage.Should().BeTrue();
    }

    [Fact]
    public void IsResearchTeamMessage_ReturnsFalse_WhenResearchTeamIdIsNull()
    {
        // Arrange
        var message = AesirChatMessage.NewAssistantMessage("Regular assistant message");

        // Act & Assert
        message.IsResearchTeamMessage.Should().BeFalse();
        message.ResearchTeamId.Should().BeNull();
    }

    [Fact]
    public void IsResearchTeamMessage_ReturnsFalse_ForUserMessage()
    {
        // Arrange
        var message = AesirChatMessage.NewUserMessage("User question");

        // Act & Assert
        message.IsResearchTeamMessage.Should().BeFalse();
    }

    /// <summary>
    /// CRITICAL: Research team messages must have Role = "assistant" AND IsResearchTeamMessage = true.
    /// UI rendering logic should check IsResearchTeamMessage BEFORE checking Role == "assistant"
    /// to ensure research team messages are rendered as TeamMessage, not AssistantMessage.
    /// </summary>
    [Fact]
    public void ResearchTeamMessage_HasAssistantRole_AndIsResearchTeamMessageTrue()
    {
        // Arrange & Act
        var message = AesirChatMessage.NewResearchTeamMessage(
            "# Research Report\n\nFindings...",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Research Team");

        // Assert - This is the key invariant that UI rendering depends on
        message.Role.Should().Be("assistant",
            "Research team messages must use 'assistant' role for LLM API compatibility");
        message.IsResearchTeamMessage.Should().BeTrue(
            "Research team messages must be identifiable via IsResearchTeamMessage for proper UI rendering");
    }

    [Fact]
    public void RegularAssistantMessage_HasAssistantRole_ButIsNotResearchTeamMessage()
    {
        // Arrange & Act
        var message = AesirChatMessage.NewAssistantMessage("Here is my response...");

        // Assert - Regular assistant messages should NOT be identified as research team messages
        message.Role.Should().Be("assistant");
        message.IsResearchTeamMessage.Should().BeFalse(
            "Regular assistant messages should not be identified as research team messages");
    }

    [Fact]
    public void NewAssistantMessage_DoesNotSetResearchProperties()
    {
        // Arrange & Act
        var message = AesirChatMessage.NewAssistantMessage("Regular response");

        // Assert
        message.ResearchTeamId.Should().BeNull();
        message.ResearchSessionId.Should().BeNull();
        message.ResearchTeamName.Should().BeNull();
    }
}
