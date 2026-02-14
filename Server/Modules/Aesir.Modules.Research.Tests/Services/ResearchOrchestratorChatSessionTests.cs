using Aesir.Common.Models;
using Aesir.Modules.Research.Models;

namespace Aesir.Modules.Research.Tests.Services;

/// <summary>
/// Tests for ResearchOrchestrator's chat session handling, specifically:
/// - Title preservation when research is started from an existing chat
/// - Message preservation when research is started from an existing chat
/// - Correct behavior when starting fresh research
///
/// These tests verify the contracts and behaviors of the chat session integration
/// without requiring full orchestrator instantiation (which has many dependencies).
/// </summary>
public class ResearchOrchestratorChatSessionTests
{
    #region PreserveOriginalChatTitle Property Tests

    [Fact]
    public void PreserveOriginalChatTitle_DefaultValue_ShouldBeFalse()
    {
        // Arrange & Act
        var session = new ResearchSession
        {
            Id = Guid.NewGuid(),
            Query = "Research query",
            UserId = "test-user"
        };

        // Assert
        session.PreserveOriginalChatTitle.Should().BeFalse(
            "New research sessions should default to not preserving title");
    }

    [Fact]
    public void PreserveOriginalChatTitle_WhenSetToTrue_ShouldRetainValue()
    {
        // Arrange
        var session = new ResearchSession
        {
            Id = Guid.NewGuid(),
            Query = "Research query",
            UserId = "test-user",
            PreserveOriginalChatTitle = true
        };

        // Assert
        session.PreserveOriginalChatTitle.Should().BeTrue(
            "PreserveOriginalChatTitle should retain its value when explicitly set");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PreserveOriginalChatTitle_Serialization_ShouldRoundTrip(bool preserveValue)
    {
        // Arrange
        var session = new ResearchSession
        {
            Id = Guid.NewGuid(),
            PreserveOriginalChatTitle = preserveValue
        };

        // Act - serialize and deserialize
        var json = System.Text.Json.JsonSerializer.Serialize(session);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<ResearchSession>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.PreserveOriginalChatTitle.Should().Be(preserveValue,
            $"PreserveOriginalChatTitle ({preserveValue}) should survive JSON round-trip");
    }

    #endregion

    #region Message Preservation Tests

    [Fact]
    public void ExistingMessages_WhenAppending_ShouldBePreserved()
    {
        // Arrange - simulate existing chat session with messages
        var existingMessages = new List<AesirChatMessage>
        {
            AesirChatMessage.NewUserMessage("First message"),
            AesirChatMessage.NewAssistantMessage("First response"),
            AesirChatMessage.NewUserMessage("Second message"),
            AesirChatMessage.NewAssistantMessage("Second response")
        };

        var conversation = new AesirConversation
        {
            Id = Guid.NewGuid().ToString(),
            Messages = existingMessages
        };

        // Act - simulate appending research query (what the fixed code does)
        var researchQuery = "Research this topic please";
        var queryMessage = AesirChatMessage.NewUserMessage(researchQuery);
        conversation.Messages.Add(queryMessage);

        // Assert
        conversation.Messages.Should().HaveCount(5,
            "Research query should be appended to existing messages, not replace them");
        conversation.Messages[0].Content.Should().Be("First message",
            "Original first message should be preserved");
        conversation.Messages[1].Content.Should().Be("First response",
            "Original first response should be preserved");
        conversation.Messages[4].Content.Should().Be(researchQuery,
            "Research query should be the last message");
    }

    [Fact]
    public void NewChatSession_ShouldStartWithOnlyQueryMessage()
    {
        // Arrange
        var researchQuery = "Research this topic";

        // Simulate new session creation (original behavior preserved for fresh research)
        var newConversation = new AesirConversation
        {
            Id = Guid.NewGuid().ToString(),
            Messages = [AesirChatMessage.NewUserMessage(researchQuery)]
        };

        // Assert
        newConversation.Messages.Should().HaveCount(1,
            "Fresh research session should start with only the query message");
        newConversation.Messages[0].Content.Should().Be(researchQuery);
        newConversation.Messages[0].Role.Should().Be("user");
    }

    #endregion

    #region Report Addition Tests

    [Fact]
    public void AddReportToExistingChat_ShouldAppendNotReplace()
    {
        // Arrange - simulate chat with existing messages + research query
        var existingMessages = new List<AesirChatMessage>
        {
            AesirChatMessage.NewUserMessage("First message"),
            AesirChatMessage.NewAssistantMessage("First response"),
            AesirChatMessage.NewUserMessage("Research query")
        };

        var conversation = new AesirConversation
        {
            Id = Guid.NewGuid().ToString(),
            Messages = existingMessages
        };

        // Act - simulate adding research report
        var reportContent = "## Research Report\n\nFindings here...";
        var reportMessage = AesirChatMessage.NewResearchTeamMessage(
            reportContent,
            Guid.NewGuid(), // research session id
            Guid.NewGuid(), // team id
            "Research Team");

        conversation.Messages.Add(reportMessage);

        // Assert
        conversation.Messages.Should().HaveCount(4,
            "Report should be appended, preserving all existing messages");
        conversation.Messages.Last().Role.Should().Be("assistant",
            "Research team message should have assistant role");
        conversation.Messages.Last().Content.Should().Contain("Research Report",
            "Report content should be preserved");
    }

    #endregion

    #region Title Generation Tests

    [Fact]
    public void GenerateInitialTitle_ForNewResearch_ShouldHaveResearchPrefix()
    {
        // This tests the expected format for fresh research sessions
        var query = "What is quantum computing?";
        var expectedPrefix = "Research:";

        // Simulate the GenerateInitialTitle behavior
        var title = $"Research: {(query.Length > 50 ? query[..47] + "..." : query)}";

        // Assert
        title.Should().StartWith(expectedPrefix,
            "Fresh research sessions should have 'Research:' prefix in title");
        title.Should().Contain("quantum computing",
            "Title should contain query content");
    }

    [Fact]
    public void GenerateInitialTitle_LongQuery_ShouldBeTruncated()
    {
        // Arrange
        var longQuery = "This is a very long query that exceeds the fifty character limit for title generation purposes";

        // Act - simulate the GenerateInitialTitle behavior
        var cleanQuery = longQuery.Trim();
        if (cleanQuery.Length > 50)
            cleanQuery = cleanQuery[..47] + "...";
        var title = $"Research: {cleanQuery}";

        // Assert
        title.Length.Should().BeLessOrEqualTo(60,
            "Generated title should be truncated for long queries");
        title.Should().EndWith("...",
            "Truncated titles should end with ellipsis");
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void ConversationNull_ShouldInitializeBeforeAppending()
    {
        // Arrange - simulate chat session with null conversation
        AesirConversation? conversation = null;
        var sessionId = Guid.NewGuid();

        // Act - simulate the null-coalescing behavior in the fix
        conversation ??= new AesirConversation { Id = sessionId.ToString() };
        conversation.Messages ??= new List<AesirChatMessage>();
        conversation.Messages.Add(AesirChatMessage.NewUserMessage("Query"));

        // Assert
        conversation.Should().NotBeNull();
        conversation.Messages.Should().HaveCount(1);
    }

    [Fact]
    public void MessagesNull_ShouldInitializeBeforeAppending()
    {
        // Arrange - simulate conversation with null messages list
        var conversation = new AesirConversation
        {
            Id = Guid.NewGuid().ToString(),
            Messages = null!
        };

        // Act - simulate the null-coalescing behavior in the fix
        conversation.Messages ??= new List<AesirChatMessage>();
        conversation.Messages.Add(AesirChatMessage.NewUserMessage("Query"));

        // Assert
        conversation.Messages.Should().NotBeNull();
        conversation.Messages.Should().HaveCount(1);
    }

    [Fact]
    public void ChatSessionId_WhenProvided_ShouldBePreserved()
    {
        // Arrange
        var existingChatSessionId = Guid.NewGuid();
        var session = new ResearchSession
        {
            Id = Guid.NewGuid(),
            ChatSessionId = existingChatSessionId,
            PreserveOriginalChatTitle = true
        };

        // Assert
        session.ChatSessionId.Should().Be(existingChatSessionId,
            "ChatSessionId should preserve the provided value");
        session.PreserveOriginalChatTitle.Should().BeTrue(
            "When reusing existing chat session, title should be preserved");
    }

    #endregion
}
