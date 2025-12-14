using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Modules.Observability.Models;
using Aesir.Client.Web.Modules.Observability.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Observability.Services;

public class ObservabilityServiceTests
{
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly ObservabilityService _service;

    public ObservabilityServiceTests()
    {
        _apiClientMock = new Mock<IApiClient>();
        _service = new ObservabilityService(_apiClientMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullApiClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ObservabilityService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiClient");
    }

    [Fact]
    public void Constructor_WithValidApiClient_CreatesInstance()
    {
        // Assert
        _service.Should().NotBeNull();
    }

    #endregion

    #region Initial State Tests

    [Fact]
    public void IsLoading_IsFalse_Initially()
    {
        // Assert
        _service.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void CurrentResponse_IsNull_Initially()
    {
        // Assert
        _service.CurrentResponse.Should().BeNull();
    }

    [Fact]
    public void GroupedLogs_IsEmpty_Initially()
    {
        // Assert
        _service.GroupedLogs.Should().BeEmpty();
    }

    [Fact]
    public void CurrentFilter_HasDefaultValues_Initially()
    {
        // Assert
        _service.CurrentFilter.Should().NotBeNull();
        _service.CurrentFilter.Page.Should().Be(1);
        _service.CurrentFilter.PageSize.Should().Be(50);
    }

    #endregion

    #region LoadLogsAsync Tests

    [Fact]
    public async Task LoadLogsAsync_WithValidResponse_ReturnsSuccess()
    {
        // Arrange
        var filter = new LogFilter();
        var response = CreateSampleResponse();
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _service.LoadLogsAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(response);
    }

    [Fact]
    public async Task LoadLogsAsync_WithNullResponse_ReturnsFailure()
    {
        // Arrange
        var filter = new LogFilter();
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedLogResponse?)null);

        // Act
        var result = await _service.LoadLogsAsync(filter);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to load logs");
    }

    [Fact]
    public async Task LoadLogsAsync_WithException_ReturnsFailure()
    {
        // Arrange
        var filter = new LogFilter();
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.LoadLogsAsync(filter);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Network error");
    }

    [Fact]
    public async Task LoadLogsAsync_UpdatesCurrentResponse()
    {
        // Arrange
        var filter = new LogFilter();
        var response = CreateSampleResponse();
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _service.LoadLogsAsync(filter);

        // Assert
        _service.CurrentResponse.Should().Be(response);
    }

    [Fact]
    public async Task LoadLogsAsync_UpdatesGroupedLogs()
    {
        // Arrange
        var filter = new LogFilter();
        var response = CreateSampleResponseWithLogs();
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _service.LoadLogsAsync(filter);

        // Assert
        _service.GroupedLogs.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LoadLogsAsync_UpdatesCurrentFilter()
    {
        // Arrange
        var filter = new LogFilter { Page = 2, PageSize = 25 };
        var response = CreateSampleResponse();
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _service.LoadLogsAsync(filter);

        // Assert
        _service.CurrentFilter.Page.Should().Be(2);
        _service.CurrentFilter.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task LoadLogsAsync_RaisesOnLogsChangedEvent()
    {
        // Arrange
        var filter = new LogFilter();
        var response = CreateSampleResponse();
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var eventCount = 0;
        _service.OnLogsChanged += () => eventCount++;

        // Act
        await _service.LoadLogsAsync(filter);

        // Assert
        eventCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task LoadLogsAsync_CallsApiWithCorrectQueryString()
    {
        // Arrange
        var filter = new LogFilter
        {
            Page = 2,
            PageSize = 25,
            Levels = [KernelLogLevel.Error],
            Types = [KernelLogType.FunctionInvocation]
        };
        var response = CreateSampleResponse();
        string? capturedUrl = null;
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((url, _) => capturedUrl = url)
            .ReturnsAsync(response);

        // Act
        await _service.LoadLogsAsync(filter);

        // Assert
        capturedUrl.Should().Contain("page=2");
        capturedUrl.Should().Contain("pageSize=25");
        capturedUrl.Should().Contain("levels=Error");
        capturedUrl.Should().Contain("types=FunctionInvocation");
    }

    #endregion

    #region RefreshAsync Tests

    [Fact]
    public async Task RefreshAsync_UsesCurrentFilter()
    {
        // Arrange
        var initialFilter = new LogFilter { Page = 3, PageSize = 100 };
        var response = CreateSampleResponse();
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        await _service.LoadLogsAsync(initialFilter);

        // Act
        await _service.RefreshAsync();

        // Assert
        _apiClientMock.Verify(
            x => x.GetAsync<PagedLogResponse>(
                It.Is<string>(url => url.Contains("page=3") && url.Contains("pageSize=100")),
                It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    #endregion

    #region ApplyFilterAsync Tests

    [Fact]
    public async Task ApplyFilterAsync_ResetsPageToOne()
    {
        // Arrange
        var filter = new LogFilter { Page = 5, PageSize = 25 };
        var response = CreateSampleResponse();
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _service.ApplyFilterAsync(filter);

        // Assert
        _service.CurrentFilter.Page.Should().Be(1);
    }

    #endregion

    #region ClearFilterAsync Tests

    [Fact]
    public async Task ClearFilterAsync_ResetsFilterToDefaults()
    {
        // Arrange
        var initialFilter = new LogFilter
        {
            Page = 3,
            PageSize = 100,
            Levels = [KernelLogLevel.Error],
            FunctionName = "test"
        };
        var response = CreateSampleResponse();
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        await _service.LoadLogsAsync(initialFilter);

        // Act
        await _service.ClearFilterAsync();

        // Assert
        _service.CurrentFilter.Page.Should().Be(1);
        _service.CurrentFilter.PageSize.Should().Be(50);
        _service.CurrentFilter.Levels.Should().BeEmpty();
        _service.CurrentFilter.FunctionName.Should().BeNull();
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task LoadNextPageAsync_IncreasesPage()
    {
        // Arrange
        var response = new PagedLogResponse
        {
            Items = [],
            Page = 1,
            PageSize = 50,
            TotalCount = 100,
            TotalPages = 2,
            HasNextPage = true
        };
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        await _service.LoadLogsAsync(new LogFilter());

        // Act
        await _service.LoadNextPageAsync();

        // Assert
        _service.CurrentFilter.Page.Should().Be(2);
    }

    [Fact]
    public async Task LoadNextPageAsync_ReturnsFailure_WhenNoNextPage()
    {
        // Arrange
        var response = new PagedLogResponse
        {
            Items = [],
            Page = 2,
            PageSize = 50,
            TotalCount = 100,
            TotalPages = 2,
            HasNextPage = false
        };
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        await _service.LoadLogsAsync(new LogFilter { Page = 2 });

        // Act
        var result = await _service.LoadNextPageAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("No next page");
    }

    [Fact]
    public async Task LoadPreviousPageAsync_DecreasesPage()
    {
        // Arrange
        var response = new PagedLogResponse
        {
            Items = [],
            Page = 2,
            PageSize = 50,
            TotalCount = 100,
            TotalPages = 2,
            HasPreviousPage = true
        };
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        await _service.LoadLogsAsync(new LogFilter { Page = 2 });

        // Act
        await _service.LoadPreviousPageAsync();

        // Assert
        _service.CurrentFilter.Page.Should().Be(1);
    }

    [Fact]
    public async Task LoadPreviousPageAsync_ReturnsFailure_WhenNoPreviousPage()
    {
        // Arrange
        var response = new PagedLogResponse
        {
            Items = [],
            Page = 1,
            PageSize = 50,
            TotalCount = 100,
            TotalPages = 2,
            HasPreviousPage = false
        };
        _apiClientMock
            .Setup(x => x.GetAsync<PagedLogResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        await _service.LoadLogsAsync(new LogFilter());

        // Act
        var result = await _service.LoadPreviousPageAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("No previous page");
    }

    #endregion

    #region Helper Methods

    private static PagedLogResponse CreateSampleResponse()
    {
        return new PagedLogResponse
        {
            Items = [],
            Page = 1,
            PageSize = 50,
            TotalCount = 0,
            TotalPages = 0,
            HasNextPage = false,
            HasPreviousPage = false
        };
    }

    private static PagedLogResponse CreateSampleResponseWithLogs()
    {
        return new PagedLogResponse
        {
            Items =
            [
                new AesirKernelLogBase
                {
                    Id = Guid.NewGuid(),
                    Level = KernelLogLevel.Info,
                    Message = "Test log",
                    CreatedAt = DateTimeOffset.Now,
                    Details = new AesirKernelLogDetailsBase
                    {
                        Type = KernelLogType.FunctionInvocation,
                        FunctionName = "test_function"
                    }
                }
            ],
            Page = 1,
            PageSize = 50,
            TotalCount = 1,
            TotalPages = 1,
            HasNextPage = false,
            HasPreviousPage = false
        };
    }

    #endregion
}
