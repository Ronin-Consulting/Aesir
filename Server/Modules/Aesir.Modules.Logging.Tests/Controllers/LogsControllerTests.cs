using Aesir.Common.Models;
using Aesir.Modules.Logging.Controllers;
using Aesir.Modules.Logging.Models;
using Aesir.Modules.Logging.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Logging.Tests.Controllers;

public class LogsControllerTests
{
    private readonly Mock<IKernelLogService> _serviceMock;
    private readonly Mock<ILogger<LogsController>> _loggerMock;
    private readonly LogsController _controller;

    public LogsControllerTests()
    {
        _serviceMock = new Mock<IKernelLogService>();
        _loggerMock = new Mock<ILogger<LogsController>>();
        _controller = new LogsController(_serviceMock.Object, _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new LogsController(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("kernelLogService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new LogsController(_serviceMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region SearchKernelLogs Tests

    [Fact]
    public async Task SearchKernelLogs_WithValidFilter_ReturnsOkResult()
    {
        // Arrange
        var filter = new KernelLogFilterRequest();
        var expectedResponse = new PagedKernelLogResponse
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 50
        };

        _serviceMock
            .Setup(x => x.SearchLogsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SearchKernelLogs(filter);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task SearchKernelLogs_CallsServiceWithCorrectFilter()
    {
        // Arrange
        var filter = new KernelLogFilterRequest
        {
            Page = 2,
            PageSize = 25,
            Levels = [KernelLogLevel.Error]
        };

        _serviceMock
            .Setup(x => x.SearchLogsAsync(It.IsAny<KernelLogFilterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedKernelLogResponse());

        // Act
        await _controller.SearchKernelLogs(filter);

        // Assert
        _serviceMock.Verify(x => x.SearchLogsAsync(
            It.Is<KernelLogFilterRequest>(f =>
                f.Page == 2 &&
                f.PageSize == 25 &&
                f.Levels!.Contains(KernelLogLevel.Error)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchKernelLogs_WithAllFilterOptions_CallsServiceCorrectly()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;

        var filter = new KernelLogFilterRequest
        {
            Page = 1,
            PageSize = 50,
            From = from,
            To = to,
            ChatSessionId = chatSessionId,
            Levels = [KernelLogLevel.Error, KernelLogLevel.Warning],
            Types = [KernelLogType.FunctionInvocation, KernelLogType.AutoFunctionInvocation],
            FunctionName = "test_func",
            PluginName = "test_plugin",
            MessageSearch = "error message"
        };

        var expectedResponse = new PagedKernelLogResponse
        {
            Items = new List<KernelLog>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Level = KernelLogLevel.Error,
                    Message = "Test error message",
                    CreatedAt = DateTimeOffset.UtcNow,
                    Details = new KernelLogDetails
                    {
                        Type = KernelLogType.FunctionInvocation,
                        FunctionName = "test_func",
                        PluginName = "test_plugin",
                        ChatSessionId = chatSessionId
                    }
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 50
        };

        _serviceMock
            .Setup(x => x.SearchLogsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SearchKernelLogs(filter);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PagedKernelLogResponse>().Subject;
        response.TotalCount.Should().Be(1);
        response.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchKernelLogs_WithEmptyResult_ReturnsEmptyItems()
    {
        // Arrange
        var filter = new KernelLogFilterRequest
        {
            Levels = [KernelLogLevel.Error]
        };

        var expectedResponse = new PagedKernelLogResponse
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 50
        };

        _serviceMock
            .Setup(x => x.SearchLogsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SearchKernelLogs(filter);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PagedKernelLogResponse>().Subject;
        response.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchKernelLogs_WithPagination_ReturnsCorrectPaginationMetadata()
    {
        // Arrange
        var filter = new KernelLogFilterRequest
        {
            Page = 2,
            PageSize = 10
        };

        var expectedResponse = new PagedKernelLogResponse
        {
            Items = Enumerable.Range(0, 10).Select(_ => new KernelLog
            {
                Id = Guid.NewGuid(),
                Level = KernelLogLevel.Info,
                Message = "Test",
                CreatedAt = DateTimeOffset.UtcNow,
                Details = new KernelLogDetails()
            }),
            TotalCount = 25,
            Page = 2,
            PageSize = 10
        };

        _serviceMock
            .Setup(x => x.SearchLogsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.SearchKernelLogs(filter);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PagedKernelLogResponse>().Subject;

        response.Page.Should().Be(2);
        response.PageSize.Should().Be(10);
        response.TotalCount.Should().Be(25);
        response.TotalPages.Should().Be(3);
        response.HasNextPage.Should().BeTrue();
        response.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task SearchKernelLogs_PassesCancellationToken()
    {
        // Arrange
        var filter = new KernelLogFilterRequest();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _serviceMock
            .Setup(x => x.SearchLogsAsync(filter, token))
            .ReturnsAsync(new PagedKernelLogResponse());

        // Act
        await _controller.SearchKernelLogs(filter, token);

        // Assert
        _serviceMock.Verify(x => x.SearchLogsAsync(filter, token), Times.Once);
    }

    #endregion

    #region GetKernelLogs Tests

    [Fact]
    public async Task GetKernelLogs_WithValidTimeRange_ReturnsLogs()
    {
        // Arrange
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;
        var expectedLogs = new List<KernelLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Level = KernelLogLevel.Info,
                Message = "Test",
                CreatedAt = DateTimeOffset.UtcNow,
                Details = new KernelLogDetails()
            }
        };

        _serviceMock
            .Setup(x => x.GetLogsAsync(from, to))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetKernelLogs(from, to);

        // Assert
        result.Should().BeEquivalentTo(expectedLogs);
    }

    #endregion

    #region GetKernelLogsBySession Tests

    [Fact]
    public async Task GetKernelLogsBySession_WithValidSessionId_ReturnsLogs()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expectedLogs = new List<KernelLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Level = KernelLogLevel.Info,
                Message = "Test",
                CreatedAt = DateTimeOffset.UtcNow,
                Details = new KernelLogDetails { ChatSessionId = sessionId }
            }
        };

        _serviceMock
            .Setup(x => x.GetLogsByChatSessionAsync(sessionId))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetKernelLogsBySession(sessionId);

        // Assert
        result.Should().BeEquivalentTo(expectedLogs);
    }

    #endregion

    #region GetKernelLogsByConversation Tests

    [Fact]
    public async Task GetKernelLogsByConversation_WithValidConversationId_ReturnsLogs()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var expectedLogs = new List<KernelLog>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Level = KernelLogLevel.Info,
                Message = "Test",
                CreatedAt = DateTimeOffset.UtcNow,
                Details = new KernelLogDetails { ConversationId = conversationId }
            }
        };

        _serviceMock
            .Setup(x => x.GetLogsByConversationAsync(conversationId))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetKernelLogsByConversation(conversationId);

        // Assert
        result.Should().BeEquivalentTo(expectedLogs);
    }

    #endregion
}
