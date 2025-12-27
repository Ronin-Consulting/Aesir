using Microsoft.Extensions.DependencyInjection;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Integration.Flows;

/// <summary>
/// Integration tests for the chat flow:
/// Select Agent → Send Message → Handle Streaming → Save to History
/// </summary>
public class ChatFlowTests : IntegrationTestBase
{
    public ChatFlowTests()
    {
        // Pre-populate with test data
        AddTestInferenceEngine("Test Engine", InferenceEngineType.Ollama);
        AddTestAgent("Chat Assistant", "llama3.2");
    }

    [Fact]
    public async Task ChatStateService_SelectAgent_SetsSelectedAgent()
    {
        // Arrange
        var chatStateService = Services.GetRequiredService<IChatStateService>();
        var configApiService = Services.GetRequiredService<IConfigurationApiService>();

        // Load agents from API
        var agentsResult = await configApiService.GetAgentsAsync();

        // Act
        var agent = agentsResult.Value!.First();
        chatStateService.SelectAgent(agent);

        // Assert
        chatStateService.SelectedAgent.Should().NotBeNull();
        chatStateService.SelectedAgent!.Name.Should().Be("Chat Assistant");
    }

    [Fact]
    public void ChatStateService_SelectAgent_RaisesOnAgentChanged()
    {
        // Arrange
        var chatStateService = Services.GetRequiredService<IChatStateService>();
        var agent = Agents.First();
        var eventRaised = false;

        chatStateService.OnAgentChanged += () => eventRaised = true;

        // Act
        chatStateService.SelectAgent(agent);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void ChatStateService_StartNewChat_ClearsSession()
    {
        // Arrange
        var chatStateService = Services.GetRequiredService<IChatStateService>();
        chatStateService.SetCurrentSession(Guid.NewGuid());
        chatStateService.CurrentSessionId.Should().NotBeNull();

        var eventRaised = false;
        chatStateService.OnNewChat += () => eventRaised = true;

        // Act
        chatStateService.StartNewChat();

        // Assert
        chatStateService.CurrentSessionId.Should().BeNull();
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task ChatHistoryService_LoadSessions_ReturnsOrderedSessions()
    {
        // Arrange
        AddTestChatSession("Old Chat");
        AddTestChatSession("New Chat");
        ChatSessions[0].UpdatedAt = DateTimeOffset.Now.AddDays(-1);
        ChatSessions[1].UpdatedAt = DateTimeOffset.Now;

        var historyService = Services.GetRequiredService<IChatHistoryService>();

        // Act
        await historyService.LoadSessionsAsync();

        // Assert
        historyService.Sessions.Should().HaveCount(2);
        historyService.Sessions.First().Title.Should().Be("New Chat"); // Most recent first
    }

    [Fact]
    public async Task ChatHistoryService_DeleteSession_RemovesFromList()
    {
        // Arrange
        AddTestChatSession("To Delete");
        AddTestChatSession("Keep This");
        var sessionToDelete = ChatSessions.First();

        var historyService = Services.GetRequiredService<IChatHistoryService>();
        await historyService.LoadSessionsAsync();
        historyService.Sessions.Should().HaveCount(2);

        // Act
        await historyService.DeleteSessionAsync(sessionToDelete.Id);

        // Assert
        historyService.Sessions.Should().HaveCount(1);
        historyService.Sessions.Should().NotContain(s => s.Title == "To Delete");
    }

    [Fact]
    public async Task ChatHistoryService_LoadSessions_ContainsAllSessions()
    {
        // Arrange - Add sessions
        AddTestChatSession("Python Tutorial");
        AddTestChatSession("JavaScript Guide");
        AddTestChatSession("Python Advanced");

        var historyService = Services.GetRequiredService<IChatHistoryService>();

        // Act
        await historyService.LoadSessionsAsync();

        // Assert
        historyService.Sessions.Should().HaveCount(3);
    }

    [Fact]
    public void ChatStateService_SetCurrentSession_TracksSessionId()
    {
        // Arrange
        var chatStateService = Services.GetRequiredService<IChatStateService>();
        var sessionId = Guid.NewGuid();

        // Act
        chatStateService.SetCurrentSession(sessionId);

        // Assert
        chatStateService.CurrentSessionId.Should().Be(sessionId);
    }

    [Fact]
    public async Task ChatHistoryService_GetSession_ReturnsFullSession()
    {
        // Arrange
        AddTestChatSession("Test Session");
        var session = ChatSessions.First();

        var historyService = Services.GetRequiredService<IChatHistoryService>();

        // Act
        var result = await historyService.GetSessionAsync(session.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(session.Id);
    }

    [Fact]
    public void ChatStateService_NotifyConversationChanged_RaisesEvent()
    {
        // Arrange
        var chatStateService = Services.GetRequiredService<IChatStateService>();
        var eventRaised = false;

        chatStateService.OnConversationChanged += () => eventRaised = true;

        // Act
        chatStateService.NotifyConversationChanged();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task ChatHistoryService_NotifySessionCreated_RefreshesSessions()
    {
        // Arrange
        var historyService = Services.GetRequiredService<IChatHistoryService>();
        await historyService.LoadSessionsAsync();
        var initialCount = historyService.Sessions.Count;

        // Simulate server-side session creation
        AddTestChatSession("Newly Created Session");

        // Act
        await historyService.NotifySessionCreatedAsync(ChatSessions.Last().Id);

        // Assert
        historyService.Sessions.Should().HaveCount(initialCount + 1);
    }

    [Fact]
    public void ChatHistoryService_SelectSession_RaisesEvent()
    {
        // Arrange
        var historyService = Services.GetRequiredService<IChatHistoryService>();
        var sessionId = Guid.NewGuid();
        Guid? selectedId = null;

        historyService.OnSessionSelected += id => selectedId = id;

        // Act
        historyService.SelectSession(sessionId);

        // Assert
        selectedId.Should().Be(sessionId);
    }

    [Fact]
    public async Task CompleteFlow_SelectAgent_StartChat_LoadHistory()
    {
        // Arrange
        var chatStateService = Services.GetRequiredService<IChatStateService>();
        var historyService = Services.GetRequiredService<IChatHistoryService>();
        var configApiService = Services.GetRequiredService<IConfigurationApiService>();

        // Add some history
        AddTestChatSession("Previous Chat 1");
        AddTestChatSession("Previous Chat 2");

        // Act 1: Load agents and select one
        var agentsResult = await configApiService.GetAgentsAsync();
        var agent = agentsResult.Value!.First();
        chatStateService.SelectAgent(agent);

        // Act 2: Load chat history
        await historyService.LoadSessionsAsync();

        // Assert: Ready for chat
        chatStateService.SelectedAgent.Should().NotBeNull();
        historyService.Sessions.Should().HaveCount(2);
    }

    [Fact]
    public async Task CompleteFlow_NewChat_SendMessage_SessionCreated()
    {
        // Arrange
        var chatStateService = Services.GetRequiredService<IChatStateService>();
        var historyService = Services.GetRequiredService<IChatHistoryService>();
        var configApiService = Services.GetRequiredService<IConfigurationApiService>();

        // Select agent
        var agentsResult = await configApiService.GetAgentsAsync();
        chatStateService.SelectAgent(agentsResult.Value!.First());

        // Act 1: Start new chat
        chatStateService.StartNewChat();
        chatStateService.CurrentSessionId.Should().BeNull();

        // Act 2: Simulate session creation (would happen during streaming)
        var newSessionId = Guid.NewGuid();
        chatStateService.SetCurrentSession(newSessionId);

        // Add to mock data (simulating server response)
        ChatSessions.Add(new AesirChatSessionItem
        {
            Id = newSessionId,
            Title = "New Conversation",
            UpdatedAt = DateTimeOffset.Now
        });

        // Act 3: Notify history service
        await historyService.NotifySessionCreatedAsync(newSessionId);

        // Assert: Session tracked
        chatStateService.CurrentSessionId.Should().Be(newSessionId);
        historyService.Sessions.Should().Contain(s => s.Id == newSessionId);
    }

    [Fact]
    public async Task ChatHistoryService_UpdateSessionTitle_UpdatesCache()
    {
        // Arrange
        AddTestChatSession("Original Title");
        var session = ChatSessions.First();

        var historyService = Services.GetRequiredService<IChatHistoryService>();
        await historyService.LoadSessionsAsync();

        // Act
        await historyService.UpdateSessionTitleAsync(session.Id, "Updated Title");

        // Assert
        historyService.Sessions.First(s => s.Id == session.Id).Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task ChatHistoryService_ClearSearch_ReloadsAllSessions()
    {
        // Arrange
        AddTestChatSession("Session A");
        AddTestChatSession("Session B");
        AddTestChatSession("Session C");

        var historyService = Services.GetRequiredService<IChatHistoryService>();
        await historyService.LoadSessionsAsync();

        // Act
        await historyService.ClearSearchAsync();

        // Assert
        historyService.SearchTerm.Should().BeNull();
        historyService.Sessions.Should().HaveCount(3);
    }

    [Fact]
    public void MarkdownService_RendersMarkdown_Correctly()
    {
        // Arrange
        var markdownService = Services.GetRequiredService<IMarkdownService>();

        // Act
        var html = markdownService.ToHtml("**bold** and _italic_");

        // Assert
        html.Should().Contain("<strong>bold</strong>");
        html.Should().Contain("<em>italic</em>");
    }

    [Fact]
    public void MarkdownService_RendersCodeBlocks_WithLanguage()
    {
        // Arrange
        var markdownService = Services.GetRequiredService<IMarkdownService>();
        var markdown = "```csharp\nvar x = 1;\n```";

        // Act
        var html = markdownService.ToHtml(markdown);

        // Assert
        html.Should().Contain("<code");
        html.Should().Contain("var x = 1;");
    }

    [Fact]
    public void MarkdownService_HandlesEmptyInput()
    {
        // Arrange
        var markdownService = Services.GetRequiredService<IMarkdownService>();

        // Act
        var html = markdownService.ToHtml("");

        // Assert
        html.Should().BeEmpty();
    }
}
