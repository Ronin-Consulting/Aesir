using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Inference.Models;
using Aesir.Modules.Inference.Services;
using Aesir.Modules.Logging.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0070 // Experimental Semantic Kernel API

namespace Aesir.Modules.Inference.Tests.Services;

/// <summary>
/// Tests for the non-streaming ChatCompletionsAsync() path's observability infrastructure.
/// Uses a concrete test subclass of the abstract BaseChatService.
/// </summary>
public class BaseChatServiceObservabilityTests
{
    private readonly Mock<IChatHistoryService> _chatHistoryServiceMock;
    private readonly Mock<IToolCallBroadcaster> _toolCallBroadcasterMock;
    private readonly Mock<IInferenceLogService> _inferenceLogServiceMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Kernel _kernel;

    public BaseChatServiceObservabilityTests()
    {
        _chatHistoryServiceMock = new Mock<IChatHistoryService>();
        _toolCallBroadcasterMock = new Mock<IToolCallBroadcaster>();
        _inferenceLogServiceMock = new Mock<IInferenceLogService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _kernel = new Kernel();
    }

    #region InferenceLogCollector Lifecycle Tests

    [Fact]
    public async Task ChatCompletionsAsync_CreatesAndPersistsInferenceLog_OnSuccess()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        AesirInferenceLog? capturedLog = null;

        _inferenceLogServiceMock
            .Setup(s => s.LogInferenceAsync(It.IsAny<AesirInferenceLog>(), It.IsAny<CancellationToken>()))
            .Callback<AesirInferenceLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        var service = CreateService(
            completionContent: "Test response",
            inferenceLogService: _inferenceLogServiceMock.Object);

        var request = CreateRequest(chatSessionId);

        // Act
        await service.ChatCompletionsAsync(request);

        // Assert
        _inferenceLogServiceMock.Verify(
            s => s.LogInferenceAsync(It.IsAny<AesirInferenceLog>(), It.IsAny<CancellationToken>()),
            Times.Once);

        capturedLog.Should().NotBeNull();
        capturedLog!.Status.Should().Be(InferenceStatus.Completed);
        capturedLog.ChatSessionId.Should().Be(chatSessionId);
        capturedLog.AssistantResponse.Should().Be("Test response");
        capturedLog.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ChatCompletionsAsync_PersistsInferenceLog_OnFailure()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        AesirInferenceLog? capturedLog = null;

        _inferenceLogServiceMock
            .Setup(s => s.LogInferenceAsync(It.IsAny<AesirInferenceLog>(), It.IsAny<CancellationToken>()))
            .Callback<AesirInferenceLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        var service = CreateService(
            completionException: new InvalidOperationException("LLM call failed"),
            inferenceLogService: _inferenceLogServiceMock.Object);

        var request = CreateRequest(chatSessionId);

        // Act
        var result = await service.ChatCompletionsAsync(request);

        // Assert
        _inferenceLogServiceMock.Verify(
            s => s.LogInferenceAsync(It.IsAny<AesirInferenceLog>(), It.IsAny<CancellationToken>()),
            Times.Once);

        capturedLog.Should().NotBeNull();
        capturedLog!.Status.Should().Be(InferenceStatus.Failed);
        capturedLog.ChatSessionId.Should().Be(chatSessionId);
        capturedLog.ErrorMessage.Should().Be("LLM call failed");
        capturedLog.AssistantResponse.Should().BeNull();
    }

    [Fact]
    public async Task ChatCompletionsAsync_CleansUpKernelData_OnSuccess()
    {
        // Arrange
        var service = CreateService(
            completionContent: "response",
            inferenceLogService: _inferenceLogServiceMock.Object);

        var request = CreateRequest();

        // Act
        await service.ChatCompletionsAsync(request);

        // Assert - collector should be removed from Kernel.Data after completion
        _kernel.Data.ContainsKey(InferenceLogCollector.KernelDataKey).Should().BeFalse(
            "InferenceLogCollector should be removed from Kernel.Data after completion");
    }

    [Fact]
    public async Task ChatCompletionsAsync_CleansUpKernelData_OnFailure()
    {
        // Arrange
        var service = CreateService(
            completionException: new Exception("error"),
            inferenceLogService: _inferenceLogServiceMock.Object);

        var request = CreateRequest();

        // Act
        await service.ChatCompletionsAsync(request);

        // Assert - collector should be removed from Kernel.Data even after failure
        _kernel.Data.ContainsKey(InferenceLogCollector.KernelDataKey).Should().BeFalse(
            "InferenceLogCollector should be removed from Kernel.Data even after failure");
    }

    [Fact]
    public async Task ChatCompletionsAsync_SkipsLogging_WhenInferenceLogServiceIsNull()
    {
        // Arrange
        var service = CreateService(
            completionContent: "response",
            inferenceLogService: null);

        var request = CreateRequest();

        // Act
        var result = await service.ChatCompletionsAsync(request);

        // Assert - should complete normally without logging
        result.Should().NotBeNull();
        result.AesirConversation.Messages.Should().Contain(m => m.Content == "response");
    }

    [Fact]
    public async Task ChatCompletionsAsync_CapturesUserQuery_InLog()
    {
        // Arrange
        AesirInferenceLog? capturedLog = null;

        _inferenceLogServiceMock
            .Setup(s => s.LogInferenceAsync(It.IsAny<AesirInferenceLog>(), It.IsAny<CancellationToken>()))
            .Callback<AesirInferenceLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        var service = CreateService(
            completionContent: "response",
            inferenceLogService: _inferenceLogServiceMock.Object);

        var request = CreateRequest();
        request.Conversation.Messages.Add(new AesirChatMessage
        {
            Role = "user",
            Content = "What is the meaning of life?"
        });

        // Act
        await service.ChatCompletionsAsync(request);

        // Assert
        capturedLog.Should().NotBeNull();
        capturedLog!.UserQuery.Should().Be("What is the meaning of life?");
    }

    [Fact]
    public async Task ChatCompletionsAsync_CapturesConversationId_InLog()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        AesirInferenceLog? capturedLog = null;

        _inferenceLogServiceMock
            .Setup(s => s.LogInferenceAsync(It.IsAny<AesirInferenceLog>(), It.IsAny<CancellationToken>()))
            .Callback<AesirInferenceLog, CancellationToken>((log, _) => capturedLog = log)
            .Returns(Task.CompletedTask);

        var service = CreateService(
            completionContent: "response",
            inferenceLogService: _inferenceLogServiceMock.Object);

        var request = CreateRequest();
        request.Conversation.Id = conversationId.ToString();

        // Act
        await service.ChatCompletionsAsync(request);

        // Assert
        capturedLog.Should().NotBeNull();
        capturedLog!.ConversationId.Should().Be(conversationId);
    }

    [Fact]
    public async Task ChatCompletionsAsync_DoesNotFail_WhenLoggingThrows()
    {
        // Arrange
        _inferenceLogServiceMock
            .Setup(s => s.LogInferenceAsync(It.IsAny<AesirInferenceLog>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var service = CreateService(
            completionContent: "response",
            inferenceLogService: _inferenceLogServiceMock.Object);

        var request = CreateRequest();

        // Act - should not throw even if logging fails
        var result = await service.ChatCompletionsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AesirConversation.Messages.Should().Contain(m => m.Content == "response");
    }

    #endregion

    #region Helper Methods

    private TestChatService CreateService(
        string? completionContent = null,
        Exception? completionException = null,
        IInferenceLogService? inferenceLogService = null)
    {
        return new TestChatService(
            NullLogger.Instance,
            _chatHistoryServiceMock.Object,
            _kernel,
            _serviceProviderMock.Object,
            _toolCallBroadcasterMock.Object,
            inferenceLogService,
            completionContent ?? "default response",
            completionException);
    }

    private static AesirChatRequestBase CreateRequest(Guid? chatSessionId = null)
    {
        return new AesirChatRequestBase
        {
            ChatSessionId = chatSessionId,
            Model = "test-model",
            User = "test-user",
            Title = "Test",
            ShouldPersistChatSession = false,
            Conversation = new AesirConversation
            {
                Id = Guid.NewGuid().ToString(),
                Messages = new List<AesirChatMessage>()
            }
        };
    }

    #endregion

    #region Test Double

    /// <summary>
    /// Minimal concrete implementation of BaseChatService for testing.
    /// Allows controlling ExecuteChatCompletionAsync behavior via constructor parameters.
    /// </summary>
    internal class TestChatService : BaseChatService
    {
        private readonly string _completionContent;
        private readonly Exception? _completionException;

        public TestChatService(
            ILogger logger,
            IChatHistoryService chatHistoryService,
            Kernel kernel,
            IServiceProvider serviceProvider,
            IToolCallBroadcaster toolCallBroadcaster,
            IInferenceLogService? inferenceLogService,
            string completionContent,
            Exception? completionException = null)
            : base(logger, chatHistoryService, kernel, serviceProvider,
                   toolCallBroadcaster, inferenceLogService, "test-engine")
        {
            _completionContent = completionContent;
            _completionException = completionException;
        }

        protected override Task<string> GetTitleForUserMessageAsync(AesirChatRequestBase request)
        {
            return Task.FromResult("Test Title");
        }

        protected override Task<(string content, int promptTokens, int completionTokens)> ExecuteChatCompletionAsync(
            AesirChatRequestBase request)
        {
            if (_completionException != null)
                throw _completionException;

            return Task.FromResult((_completionContent, 10, 20));
        }

        protected override async IAsyncEnumerable<(string content, bool isThinking, bool isComplete)>
            ExecuteStreamingChatCompletionAsync(AesirChatRequestBase request,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return (_completionContent, false, true);
            await Task.CompletedTask;
        }
    }

    #endregion
}
