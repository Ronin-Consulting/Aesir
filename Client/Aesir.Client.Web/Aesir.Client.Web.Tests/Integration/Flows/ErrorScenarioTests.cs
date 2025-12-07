using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using RichardSzalay.MockHttp;
using System.Net;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Integration.Flows;

/// <summary>
/// Integration tests for error handling scenarios.
/// Tests API result handling patterns.
/// </summary>
public class ErrorScenarioTests : IntegrationTestBase
{
    [Fact]
    public async Task GetTools_ReturnsEmptyList_OnNoContent()
    {
        // Arrange - Tools are empty by default
        var toolService = Services.GetRequiredService<IToolService>();

        // Act
        var result = await toolService.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInferenceEngines_ReturnsEmptyList_Initially()
    {
        // Arrange
        var engineService = Services.GetRequiredService<IInferenceEngineService>();

        // Act
        var result = await engineService.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAgents_ReturnsEmptyList_Initially()
    {
        // Arrange
        var agentService = Services.GetRequiredService<IAgentService>();

        // Act
        var result = await agentService.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMcpServers_ReturnsEmptyList_Initially()
    {
        // Arrange
        var mcpService = Services.GetRequiredService<IMcpServerService>();

        // Act
        var result = await mcpService.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void ApiResult_Success_HasCorrectState()
    {
        // Arrange & Act
        var result = ApiResult<string>.Success("test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test");
    }

    [Fact]
    public void ApiResult_Failure_HasCorrectState()
    {
        // Arrange & Act
        var result = ApiResult<string>.Failure("error");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be("error");
    }

    [Fact]
    public void ApiResult_Map_TransformsValue()
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
    public void ApiResult_Map_PropagatesFailure()
    {
        // Arrange
        var result = ApiResult<int>.Failure("error");

        // Act
        var mapped = result.Map(x => x * 2);

        // Assert
        mapped.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ApiResult_OnSuccess_ExecutesCallback()
    {
        // Arrange
        var result = ApiResult<string>.Success("test");
        string? captured = null;

        // Act
        result.OnSuccess(v => captured = v);

        // Assert
        captured.Should().Be("test");
    }

    [Fact]
    public void ApiResult_OnSuccess_DoesNotExecuteOnFailure()
    {
        // Arrange
        var result = ApiResult<string>.Failure("error");
        string? captured = null;

        // Act
        result.OnSuccess(v => captured = v);

        // Assert
        captured.Should().BeNull();
    }

    [Fact]
    public void ApiResult_OnFailure_ExecutesCallback()
    {
        // Arrange
        var result = ApiResult<string>.Failure("test error");
        string? captured = null;

        // Act
        result.OnFailure(msg => captured = msg);

        // Assert
        captured.Should().Be("test error");
    }

    [Fact]
    public void ApiResult_OnFailure_DoesNotExecuteOnSuccess()
    {
        // Arrange
        var result = ApiResult<string>.Success("test");
        string? captured = null;

        // Act
        result.OnFailure(msg => captured = msg);

        // Assert
        captured.Should().BeNull();
    }

    [Fact]
    public void NonGenericApiResult_Success_HasCorrectState()
    {
        // Arrange & Act
        var result = ApiResult.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void NonGenericApiResult_Failure_HasCorrectState()
    {
        // Arrange & Act
        var result = ApiResult.Failure("error");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("error");
    }
}
