using Aesir.Common.Models;
using Aesir.Infrastructure.Data;
using Aesir.Modules.Logging.Models;
using Aesir.Modules.Logging.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Logging.Tests.Services;

public class KernelLogServiceTests
{
    private readonly Mock<IDbContext> _dbContextMock;
    private readonly Mock<ILogger<KernelLogService>> _loggerMock;

    public KernelLogServiceTests()
    {
        _dbContextMock = new Mock<IDbContext>();
        _loggerMock = new Mock<ILogger<KernelLogService>>();
    }

    private KernelLogService CreateService()
    {
        return new KernelLogService(_dbContextMock.Object, _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullDbContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new KernelLogService(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dbContext");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new KernelLogService(_dbContextMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region SearchLogsAsync Filter Request Tests

    [Fact]
    public void KernelLogFilterRequest_DefaultValues_AreCorrect()
    {
        // Act
        var filter = new KernelLogFilterRequest();

        // Assert
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(50);
        filter.SortDirection.Should().Be(SortDirection.Descending);
        filter.From.Should().BeNull();
        filter.To.Should().BeNull();
        filter.Levels.Should().BeNull();
        filter.Types.Should().BeNull();
        filter.FunctionName.Should().BeNull();
        filter.PluginName.Should().BeNull();
        filter.MessageSearch.Should().BeNull();
        filter.ChatSessionId.Should().BeNull();
        filter.ConversationId.Should().BeNull();
    }

    [Fact]
    public void KernelLogFilterRequest_WithAllFilters_SetsAllProperties()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;

        // Act
        var filter = new KernelLogFilterRequest
        {
            Page = 2,
            PageSize = 25,
            From = from,
            To = to,
            ChatSessionId = chatSessionId,
            ConversationId = conversationId,
            Levels = [KernelLogLevel.Error, KernelLogLevel.Warning],
            Types = [KernelLogType.FunctionInvocation],
            FunctionName = "test_function",
            PluginName = "test_plugin",
            MessageSearch = "error",
            SortDirection = SortDirection.Ascending
        };

        // Assert
        filter.Page.Should().Be(2);
        filter.PageSize.Should().Be(25);
        filter.From.Should().Be(from);
        filter.To.Should().Be(to);
        filter.ChatSessionId.Should().Be(chatSessionId);
        filter.ConversationId.Should().Be(conversationId);
        filter.Levels.Should().HaveCount(2);
        filter.Levels.Should().Contain(KernelLogLevel.Error);
        filter.Levels.Should().Contain(KernelLogLevel.Warning);
        filter.Types.Should().HaveCount(1);
        filter.Types.Should().Contain(KernelLogType.FunctionInvocation);
        filter.FunctionName.Should().Be("test_function");
        filter.PluginName.Should().Be("test_plugin");
        filter.MessageSearch.Should().Be("error");
        filter.SortDirection.Should().Be(SortDirection.Ascending);
    }

    #endregion

    #region PagedKernelLogResponse Tests

    [Fact]
    public void PagedKernelLogResponse_TotalPages_CalculatesCorrectly()
    {
        // Arrange
        var response = new PagedKernelLogResponse
        {
            TotalCount = 150,
            PageSize = 50,
            Page = 1
        };

        // Assert
        response.TotalPages.Should().Be(3);
    }

    [Fact]
    public void PagedKernelLogResponse_TotalPages_RoundsUp()
    {
        // Arrange
        var response = new PagedKernelLogResponse
        {
            TotalCount = 151,
            PageSize = 50,
            Page = 1
        };

        // Assert
        response.TotalPages.Should().Be(4);
    }

    [Fact]
    public void PagedKernelLogResponse_TotalPages_ZeroPageSize_ReturnsZero()
    {
        // Arrange
        var response = new PagedKernelLogResponse
        {
            TotalCount = 100,
            PageSize = 0,
            Page = 1
        };

        // Assert
        response.TotalPages.Should().Be(0);
    }

    [Fact]
    public void PagedKernelLogResponse_HasNextPage_WhenOnFirstPage_ReturnsTrue()
    {
        // Arrange
        var response = new PagedKernelLogResponse
        {
            TotalCount = 100,
            PageSize = 50,
            Page = 1
        };

        // Assert
        response.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedKernelLogResponse_HasNextPage_WhenOnLastPage_ReturnsFalse()
    {
        // Arrange
        var response = new PagedKernelLogResponse
        {
            TotalCount = 100,
            PageSize = 50,
            Page = 2
        };

        // Assert
        response.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PagedKernelLogResponse_HasPreviousPage_WhenOnFirstPage_ReturnsFalse()
    {
        // Arrange
        var response = new PagedKernelLogResponse
        {
            TotalCount = 100,
            PageSize = 50,
            Page = 1
        };

        // Assert
        response.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void PagedKernelLogResponse_HasPreviousPage_WhenOnSecondPage_ReturnsTrue()
    {
        // Arrange
        var response = new PagedKernelLogResponse
        {
            TotalCount = 100,
            PageSize = 50,
            Page = 2
        };

        // Assert
        response.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void PagedKernelLogResponse_DefaultItems_IsEmptyCollection()
    {
        // Arrange
        var response = new PagedKernelLogResponse();

        // Assert
        response.Items.Should().NotBeNull();
        response.Items.Should().BeEmpty();
    }

    #endregion
}
