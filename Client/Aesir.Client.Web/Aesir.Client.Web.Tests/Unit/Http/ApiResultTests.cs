using System.Net;
using Aesir.Client.Web.Infrastructure.Http;

namespace Aesir.Client.Web.Tests.Unit.Http;

public class ApiResultTests
{
    [Fact]
    public void Success_WithValue_CreatesSuccessfulResult()
    {
        // Arrange
        const string value = "test value";

        // Act
        var result = ApiResult<string>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
        result.Error.Should().BeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void Success_WithCustomStatusCode_UsesProvidedCode()
    {
        // Arrange
        const int value = 42;

        // Act
        var result = ApiResult<int>.Success(value, HttpStatusCode.Created);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public void Failure_WithError_CreatesFailedResult()
    {
        // Arrange
        const string error = "Something went wrong";

        // Act
        var result = ApiResult<string>.Failure(error, HttpStatusCode.BadRequest);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be(error);
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void FromException_CreatesFailedResultWithMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = ApiResult<string>.FromException(exception, HttpStatusCode.InternalServerError);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Test exception");
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void Map_OnSuccess_TransformsValue()
    {
        // Arrange
        var result = ApiResult<int>.Success(5);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(10);
    }

    [Fact]
    public void Map_OnFailure_PropagatesError()
    {
        // Arrange
        var result = ApiResult<int>.Failure("error", HttpStatusCode.BadRequest);

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsSuccess.Should().BeFalse();
        mapped.Error.Should().Be("error");
    }

    [Fact]
    public void OnSuccess_WhenSuccessful_ExecutesAction()
    {
        // Arrange
        var result = ApiResult<string>.Success("test");
        var executed = false;

        // Act
        result.OnSuccess(_ => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void OnSuccess_WhenFailed_DoesNotExecuteAction()
    {
        // Arrange
        var result = ApiResult<string>.Failure("error");
        var executed = false;

        // Act
        result.OnSuccess(_ => executed = true);

        // Assert
        executed.Should().BeFalse();
    }

    [Fact]
    public void OnFailure_WhenFailed_ExecutesAction()
    {
        // Arrange
        var result = ApiResult<string>.Failure("error message");
        string? capturedError = null;

        // Act
        result.OnFailure(e => capturedError = e);

        // Assert
        capturedError.Should().Be("error message");
    }

    [Fact]
    public void OnFailure_WhenSuccessful_DoesNotExecuteAction()
    {
        // Arrange
        var result = ApiResult<string>.Success("test");
        var executed = false;

        // Act
        result.OnFailure(_ => executed = true);

        // Assert
        executed.Should().BeFalse();
    }

    [Fact]
    public void GetValueOrDefault_WhenSuccessful_ReturnsValue()
    {
        // Arrange
        var result = ApiResult<int>.Success(42);

        // Act
        var value = result.GetValueOrDefault(-1);

        // Assert
        value.Should().Be(42);
    }

    [Fact]
    public void GetValueOrDefault_WhenFailed_ReturnsDefault()
    {
        // Arrange
        var result = ApiResult<int>.Failure("error");

        // Act
        var value = result.GetValueOrDefault(-1);

        // Assert
        value.Should().Be(-1);
    }

    // Non-generic ApiResult tests

    [Fact]
    public void NonGeneric_Success_CreatesSuccessfulResult()
    {
        // Act
        var result = ApiResult.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public void NonGeneric_Failure_CreatesFailedResult()
    {
        // Act
        var result = ApiResult.Failure("error", HttpStatusCode.NotFound);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("error");
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public void NonGeneric_OnSuccess_WhenSuccessful_ExecutesAction()
    {
        // Arrange
        var result = ApiResult.Success();
        var executed = false;

        // Act
        result.OnSuccess(() => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public void NonGeneric_OnFailure_WhenFailed_ExecutesAction()
    {
        // Arrange
        var result = ApiResult.Failure("error");
        var executed = false;

        // Act
        result.OnFailure(_ => executed = true);

        // Assert
        executed.Should().BeTrue();
    }
}
