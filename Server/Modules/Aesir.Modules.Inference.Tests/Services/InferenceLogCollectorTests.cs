using Aesir.Common.Models;
using Aesir.Modules.Inference.Services;
using Microsoft.SemanticKernel;

namespace Aesir.Modules.Inference.Tests.Services;

public class InferenceLogCollectorTests
{
    #region Start Tests

    [Fact]
    public void Start_SetsIsStarted()
    {
        // Arrange
        var collector = new InferenceLogCollector();

        // Act
        collector.Start(Guid.NewGuid(), Guid.NewGuid(), "test query");

        // Assert
        collector.IsStarted.Should().BeTrue();
        collector.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Start_ThrowsIfCalledTwice()
    {
        // Arrange
        var collector = new InferenceLogCollector();
        collector.Start(Guid.NewGuid(), Guid.NewGuid(), "test query");

        // Act & Assert
        var act = () => collector.Start(Guid.NewGuid(), Guid.NewGuid(), "second call");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Start_AcceptsNullChatSessionId()
    {
        // Arrange
        var collector = new InferenceLogCollector();

        // Act
        collector.Start(null, Guid.NewGuid(), "test query");

        // Assert
        collector.IsStarted.Should().BeTrue();
    }

    [Fact]
    public void Start_AcceptsNullConversationId()
    {
        // Arrange
        var collector = new InferenceLogCollector();

        // Act
        collector.Start(Guid.NewGuid(), null, "test query");

        // Assert
        collector.IsStarted.Should().BeTrue();
    }

    #endregion

    #region Complete Tests

    [Fact]
    public void Complete_ReturnsInferenceLog_WithCorrectStatus()
    {
        // Arrange
        var collector = new InferenceLogCollector();
        var chatSessionId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        collector.Start(chatSessionId, conversationId, "test query");

        // Act
        var log = collector.Complete("assistant response");

        // Assert
        log.Should().NotBeNull();
        log.Status.Should().Be(InferenceStatus.Completed);
        log.ChatSessionId.Should().Be(chatSessionId);
        log.ConversationId.Should().Be(conversationId);
        log.UserQuery.Should().Be("test query");
        log.AssistantResponse.Should().Be("assistant response");
        log.ErrorMessage.Should().BeNull();
        collector.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Complete_ThrowsIfNotStarted()
    {
        // Arrange
        var collector = new InferenceLogCollector();

        // Act & Assert
        var act = () => collector.Complete("response");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_ThrowsIfAlreadyCompleted()
    {
        // Arrange
        var collector = new InferenceLogCollector();
        collector.Start(Guid.NewGuid(), Guid.NewGuid(), "query");
        collector.Complete("response");

        // Act & Assert
        var act = () => collector.Complete("second response");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_TracksDuration()
    {
        // Arrange
        var collector = new InferenceLogCollector();
        collector.Start(Guid.NewGuid(), Guid.NewGuid(), "query");

        // Act
        var log = collector.Complete("response");

        // Assert
        log.TotalDurationMs.Should().BeGreaterThanOrEqualTo(0);
        log.CompletedAt.Should().NotBeNull();
        log.StartedAt.Should().BeOnOrBefore(log.CompletedAt!.Value);
    }

    #endregion

    #region Fail Tests

    [Fact]
    public void Fail_ReturnsInferenceLog_WithFailedStatus()
    {
        // Arrange
        var collector = new InferenceLogCollector();
        var chatSessionId = Guid.NewGuid();
        collector.Start(chatSessionId, null, "test query");

        // Act
        var log = collector.Fail("Something went wrong");

        // Assert
        log.Should().NotBeNull();
        log.Status.Should().Be(InferenceStatus.Failed);
        log.ChatSessionId.Should().Be(chatSessionId);
        log.ErrorMessage.Should().Be("Something went wrong");
        log.AssistantResponse.Should().BeNull();
        collector.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Fail_ThrowsIfNotStarted()
    {
        // Arrange
        var collector = new InferenceLogCollector();

        // Act & Assert
        var act = () => collector.Fail("error");
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region AddToolCall Tests

    [Fact]
    public void AddToolCall_IncludesToolCallInLog()
    {
        // Arrange
        var collector = new InferenceLogCollector();
        var chatSessionId = Guid.NewGuid();
        collector.Start(chatSessionId, null, "query");

        var toolCall = new AesirToolCallInfo
        {
            ToolCallId = Guid.NewGuid().ToString(),
            FunctionName = "WebSearch",
            Status = ToolCallStatus.Completed
        };

        // Act
        collector.AddToolCall(toolCall);
        var log = collector.Complete("response");

        // Assert
        log.ToolCalls.Should().HaveCount(1);
        log.ToolCallCount.Should().Be(1);
        log.ToolCalls[0].FunctionName.Should().Be("WebSearch");
        log.ToolCalls[0].ChatSessionId.Should().Be(chatSessionId);
    }

    [Fact]
    public void AddToolCall_SetsSessionContextOnToolCall()
    {
        // Arrange
        var collector = new InferenceLogCollector();
        var chatSessionId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        collector.Start(chatSessionId, conversationId, "query");

        var toolCall = new AesirToolCallInfo
        {
            ToolCallId = Guid.NewGuid().ToString(),
            FunctionName = "TestTool"
        };

        // Act
        collector.AddToolCall(toolCall);

        // Assert
        toolCall.ChatSessionId.Should().Be(chatSessionId);
        toolCall.ConversationId.Should().Be(conversationId);
    }

    [Fact]
    public void AddToolCall_ThrowsIfNotStarted()
    {
        // Arrange
        var collector = new InferenceLogCollector();

        // Act & Assert
        var act = () => collector.AddToolCall(new AesirToolCallInfo());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddToolCall_ThrowsIfCompleted()
    {
        // Arrange
        var collector = new InferenceLogCollector();
        collector.Start(Guid.NewGuid(), null, "query");
        collector.Complete("response");

        // Act & Assert
        var act = () => collector.AddToolCall(new AesirToolCallInfo());
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Kernel Storage Tests

    [Fact]
    public void StoreInKernel_MakesCollectorRetrievable()
    {
        // Arrange
        var kernel = new Kernel();
        var collector = new InferenceLogCollector();

        // Act
        InferenceLogCollector.StoreInKernel(kernel, collector);

        // Assert
        var retrieved = InferenceLogCollector.GetFromKernel(kernel);
        retrieved.Should().NotBeNull();
        retrieved.Should().BeSameAs(collector);
    }

    [Fact]
    public void RemoveFromKernel_RemovesCollector()
    {
        // Arrange
        var kernel = new Kernel();
        var collector = new InferenceLogCollector();
        InferenceLogCollector.StoreInKernel(kernel, collector);

        // Act
        InferenceLogCollector.RemoveFromKernel(kernel);

        // Assert
        var retrieved = InferenceLogCollector.GetFromKernel(kernel);
        retrieved.Should().BeNull();
    }

    [Fact]
    public void GetFromKernel_ReturnsNull_WhenNotStored()
    {
        // Arrange
        var kernel = new Kernel();

        // Act
        var retrieved = InferenceLogCollector.GetFromKernel(kernel);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public void RemoveFromKernel_DoesNotThrow_WhenNotStored()
    {
        // Arrange
        var kernel = new Kernel();

        // Act & Assert
        var act = () => InferenceLogCollector.RemoveFromKernel(kernel);
        act.Should().NotThrow();
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_ReturnsInferenceLog_WithCancelledStatus()
    {
        // Arrange
        var collector = new InferenceLogCollector();
        collector.Start(Guid.NewGuid(), null, "query");

        // Act
        var log = collector.Cancel();

        // Assert
        log.Status.Should().Be(InferenceStatus.Cancelled);
        log.AssistantResponse.Should().BeNull();
        log.ErrorMessage.Should().BeNull();
        collector.IsCompleted.Should().BeTrue();
    }

    #endregion

    #region Id Tests

    [Fact]
    public void Id_IsUnique_AcrossInstances()
    {
        // Arrange & Act
        var collector1 = new InferenceLogCollector();
        var collector2 = new InferenceLogCollector();

        // Assert
        collector1.Id.Should().NotBe(collector2.Id);
        collector1.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Complete_PreservesCollectorId_InLog()
    {
        // Arrange
        var collector = new InferenceLogCollector();
        collector.Start(Guid.NewGuid(), null, "query");

        // Act
        var log = collector.Complete("response");

        // Assert
        log.Id.Should().Be(collector.Id);
    }

    #endregion
}
