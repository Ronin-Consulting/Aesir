using Aesir.Common.Models;
using Aesir.Modules.Logging.Controllers;
using Aesir.Modules.Logging.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Logging.Tests.Controllers;

public class InferenceLogsControllerTests
{
    private readonly Mock<IInferenceLogService> _serviceMock;
    private readonly Mock<ILogger<InferenceLogsController>> _loggerMock;
    private readonly InferenceLogsController _controller;

    public InferenceLogsControllerTests()
    {
        _serviceMock = new Mock<IInferenceLogService>();
        _loggerMock = new Mock<ILogger<InferenceLogsController>>();
        _controller = new InferenceLogsController(_serviceMock.Object, _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullService_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new InferenceLogsController(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("inferenceLogService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new InferenceLogsController(_serviceMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenLogExists_ReturnsOkWithLog()
    {
        // Arrange
        var id = Guid.NewGuid();
        var log = CreateTestInferenceLog(id);
        _serviceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(log);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLog = okResult.Value.Should().BeOfType<AesirInferenceLog>().Subject;
        returnedLog.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetById_WhenLogDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AesirInferenceLog?)null);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetByChatSession Tests

    [Fact]
    public async Task GetByChatSession_ReturnsSummaries()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        var summaries = new List<AesirInferenceLogSummary>
        {
            CreateTestSummary(),
            CreateTestSummary()
        };
        _serviceMock.Setup(s => s.GetByChatSessionAsync(chatSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summaries);

        // Act
        var result = await _controller.GetByChatSession(chatSessionId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByChatSession_WithNoLogs_ReturnsEmptyList()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetByChatSessionAsync(chatSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AesirInferenceLogSummary>());

        // Act
        var result = await _controller.GetByChatSession(chatSessionId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetLatestByChatSession Tests

    [Fact]
    public async Task GetLatestByChatSession_WhenLogExists_ReturnsOkWithLog()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        var log = CreateTestInferenceLog(chatSessionId: chatSessionId);
        _serviceMock.Setup(s => s.GetLatestByChatSessionAsync(chatSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(log);

        // Act
        var result = await _controller.GetLatestByChatSession(chatSessionId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLog = okResult.Value.Should().BeOfType<AesirInferenceLog>().Subject;
        returnedLog.ChatSessionId.Should().Be(chatSessionId);
    }

    [Fact]
    public async Task GetLatestByChatSession_WhenNoLogs_ReturnsNotFound()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        _serviceMock.Setup(s => s.GetLatestByChatSessionAsync(chatSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AesirInferenceLog?)null);

        // Act
        var result = await _controller.GetLatestByChatSession(chatSessionId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task Search_ReturnsPagedResponse()
    {
        // Arrange
        var filter = new InferenceLogFilterRequest { Page = 1, PageSize = 25 };
        var pagedResponse = new PagedInferenceLogResponse
        {
            Items = new List<AesirInferenceLogSummary> { CreateTestSummary() },
            TotalCount = 1,
            Page = 1,
            PageSize = 25
        };
        _serviceMock.Setup(s => s.SearchAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResponse);

        // Act
        var result = await _controller.Search(filter);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PagedInferenceLogResponse>().Subject;
        response.TotalCount.Should().Be(1);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task Search_WithFilters_PassesFiltersToService()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        var filter = new InferenceLogFilterRequest
        {
            Page = 2,
            PageSize = 10,
            ChatSessionId = chatSessionId,
            Statuses = new List<InferenceStatus> { InferenceStatus.Completed },
            HasToolCalls = true
        };

        _serviceMock.Setup(s => s.SearchAsync(It.Is<InferenceLogFilterRequest>(f =>
                f.Page == 2 &&
                f.PageSize == 10 &&
                f.ChatSessionId == chatSessionId &&
                f.Statuses!.Contains(InferenceStatus.Completed) &&
                f.HasToolCalls == true),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedInferenceLogResponse());

        // Act
        await _controller.Search(filter);

        // Assert
        _serviceMock.Verify(s => s.SearchAsync(It.Is<InferenceLogFilterRequest>(f =>
                f.Page == 2 &&
                f.PageSize == 10 &&
                f.ChatSessionId == chatSessionId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Search_WithNoResults_ReturnsEmptyPagedResponse()
    {
        // Arrange
        var filter = new InferenceLogFilterRequest();
        var pagedResponse = new PagedInferenceLogResponse
        {
            Items = Enumerable.Empty<AesirInferenceLogSummary>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 50
        };
        _serviceMock.Setup(s => s.SearchAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResponse);

        // Act
        var result = await _controller.Search(filter);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PagedInferenceLogResponse>().Subject;
        response.TotalCount.Should().Be(0);
        response.Items.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static AesirInferenceLog CreateTestInferenceLog(
        Guid? id = null,
        Guid? chatSessionId = null,
        InferenceStatus status = InferenceStatus.Completed)
    {
        return new AesirInferenceLog
        {
            Id = id ?? Guid.NewGuid(),
            ChatSessionId = chatSessionId ?? Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            UserQuery = "Test user query",
            UserQueryTruncated = "Test user query",
            AssistantResponse = "Test assistant response",
            ToolCalls = new List<AesirToolCallInfo>(),
            ToolCallCount = 0,
            TotalDurationMs = 1000,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CompletedAt = DateTimeOffset.UtcNow,
            Status = status
        };
    }

    private static AesirInferenceLogSummary CreateTestSummary()
    {
        return new AesirInferenceLogSummary
        {
            Id = Guid.NewGuid(),
            ChatSessionId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            UserQueryTruncated = "Test user query",
            AssistantResponse = "Test assistant response",
            ToolCallCount = 0,
            TotalDurationMs = 1000,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CompletedAt = DateTimeOffset.UtcNow,
            Status = InferenceStatus.Completed
        };
    }

    #endregion
}
