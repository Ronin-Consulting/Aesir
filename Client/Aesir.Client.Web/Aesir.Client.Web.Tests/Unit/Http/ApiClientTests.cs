using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aesir.Client.Web.Infrastructure.Http;
using RichardSzalay.MockHttp;

namespace Aesir.Client.Web.Tests.Unit.Http;

public class ApiClientTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly ApiClient _apiClient;

    public ApiClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api.example.com");
        _apiClient = new ApiClient(httpClient);
    }

    public record TestEntity(int Id, string Name);

    [Fact]
    public async Task GetAsync_ReturnsDeserializedResponse()
    {
        // Arrange
        var expected = new TestEntity(1, "Test");
        _mockHttp.When("/test/1")
            .Respond("application/json", JsonSerializer.Serialize(expected));

        // Act
        var result = await _apiClient.GetAsync<TestEntity>("/test/1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _mockHttp.When("/test/999")
            .Respond(HttpStatusCode.NotFound);

        // Act
        var result = await _apiClient.GetAsync<TestEntity>("/test/999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task PostAsync_SendsDataAndReturnsResponse()
    {
        // Arrange
        var input = new TestEntity(0, "New Item");
        _mockHttp.When(HttpMethod.Post, "/test")
            .Respond("application/json", "42");

        // Act
        var result = await _apiClient.PostAsync<int>("/test", input);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task PostAsync_ThrowsOnError()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Post, "/test")
            .Respond(HttpStatusCode.BadRequest);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _apiClient.PostAsync<int>("/test", new { }));
    }

    [Fact]
    public async Task PutAsync_SendsDataAndReturnsResponse()
    {
        // Arrange
        var input = new TestEntity(1, "Updated");
        _mockHttp.When(HttpMethod.Put, "/test/1")
            .Respond("application/json", "true");

        // Act
        var result = await _apiClient.PutAsync<bool>("/test/1", input);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Delete, "/test/1")
            .Respond(HttpStatusCode.NoContent);

        // Act
        var result = await _apiClient.DeleteAsync("/test/1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_OnNotFound()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Delete, "/test/999")
            .Respond(HttpStatusCode.NotFound);

        // Act
        var result = await _apiClient.DeleteAsync("/test/999");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task StreamAsync_YieldsItems()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new(1, "First"),
            new(2, "Second")
        };
        _mockHttp.When("/stream")
            .Respond("application/json", JsonSerializer.Serialize(items));

        // Act
        var results = new List<TestEntity>();
        await foreach (var item in _apiClient.StreamAsync<TestEntity>("/stream"))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(2);
        results[0].Name.Should().Be("First");
        results[1].Name.Should().Be("Second");
    }

    [Fact]
    public async Task StreamPostAsync_SendsDataAndYieldsItems()
    {
        // Arrange
        var items = new List<TestEntity>
        {
            new(1, "Response")
        };
        _mockHttp.When(HttpMethod.Post, "/stream")
            .Respond("application/json", JsonSerializer.Serialize(items));

        // Act
        var results = new List<TestEntity>();
        await foreach (var item in _apiClient.StreamPostAsync<TestEntity>("/stream", new { query = "test" }))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Response");
    }
}
