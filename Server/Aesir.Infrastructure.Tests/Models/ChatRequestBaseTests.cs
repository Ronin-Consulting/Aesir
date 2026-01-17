using Aesir.Common.Models;
using FluentAssertions;
using Xunit;

namespace Aesir.Infrastructure.Tests.Models;

public class ChatRequestBaseTests
{
    #region ShouldPersistChatSession Tests

    [Fact]
    public void ShouldPersistChatSession_DefaultsToTrue()
    {
        // Arrange & Act
        var request = new ChatRequestBase
        {
            User = "test-user",
            Conversation = new AesirConversation()
        };

        // Assert
        request.ShouldPersistChatSession.Should().BeTrue(
            "ShouldPersistChatSession should default to true for normal chat requests");
    }

    [Fact]
    public void ShouldPersistChatSession_CanBeSetToFalse()
    {
        // Arrange & Act
        var request = new ChatRequestBase
        {
            User = "test-user",
            Conversation = new AesirConversation(),
            ShouldPersistChatSession = false
        };

        // Assert
        request.ShouldPersistChatSession.Should().BeFalse();
    }

    [Fact]
    public void ShouldPersistChatSession_CanBeSetToTrue()
    {
        // Arrange
        var request = new ChatRequestBase
        {
            User = "test-user",
            Conversation = new AesirConversation(),
            ShouldPersistChatSession = false
        };

        // Act
        request.ShouldPersistChatSession = true;

        // Assert
        request.ShouldPersistChatSession.Should().BeTrue();
    }

    #endregion

    #region AesirChatRequestBase Inheritance Tests

    [Fact]
    public void AesirChatRequestBase_InheritsShouldPersistChatSession_DefaultsToTrue()
    {
        // Arrange & Act
        var request = new AesirChatRequestBase
        {
            User = "test-user",
            Conversation = new AesirConversation(),
            Model = "gpt-4"
        };

        // Assert
        request.ShouldPersistChatSession.Should().BeTrue(
            "AesirChatRequestBase should inherit the default true value from ChatRequestBase");
    }

    [Fact]
    public void AesirChatRequestBase_ShouldPersistChatSession_CanBeSetToFalse()
    {
        // Arrange & Act
        var request = new AesirChatRequestBase
        {
            User = "test-user",
            Conversation = new AesirConversation(),
            Model = "gpt-4",
            ShouldPersistChatSession = false
        };

        // Assert
        request.ShouldPersistChatSession.Should().BeFalse();
    }

    #endregion
}
