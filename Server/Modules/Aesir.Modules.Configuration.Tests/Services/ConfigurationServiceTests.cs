using Aesir.Common.Models;
using Aesir.Infrastructure.Data;
using Aesir.Infrastructure.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Configuration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Configuration.Tests.Services;

public class ConfigurationServiceTests
{
    private readonly Mock<ILogger<ConfigurationService>> _loggerMock;
    private readonly Mock<IDbContext> _dbContextMock;

    public ConfigurationServiceTests()
    {
        _loggerMock = new Mock<ILogger<ConfigurationService>>();
        _dbContextMock = new Mock<IDbContext>();
    }

    private ConfigurationService CreateService(Dictionary<string, string?>? configValues = null)
    {
        var config = new Dictionary<string, string?>
        {
            ["Configuration:LoadFromDatabase"] = "false"
        };

        if (configValues != null)
        {
            foreach (var kvp in configValues)
            {
                config[kvp.Key] = kvp.Value;
            }
        }

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(config);
        var configuration = configurationBuilder.Build();

        return new ConfigurationService(
            _loggerMock.Object,
            _dbContextMock.Object,
            configuration);
    }

    #region ComputeIsThinkingAvailableAsync Tests - Edge Cases (No Engine Lookup)

    [Fact]
    public async Task ComputeIsThinkingAvailable_WhenAllowThinkingFalse_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var agent = CreateAgent(Guid.NewGuid(), allowThinking: false);

        // Act
        var result = await service.ComputeIsThinkingAvailableAsync(agent);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ComputeIsThinkingAvailable_WhenAllowThinkingNull_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var agent = CreateAgent(Guid.NewGuid(), allowThinking: null);

        // Act
        var result = await service.ComputeIsThinkingAvailableAsync(agent);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ComputeIsThinkingAvailable_WhenNoInferenceEngineId_ReturnsAllowThinking()
    {
        // Arrange
        var service = CreateService();
        var agent = new AesirAgent
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            AllowThinking = true,
            ChatInferenceEngineId = null
        };

        // Act
        var result = await service.ComputeIsThinkingAvailableAsync(agent);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ComputeIsThinkingAvailable_WhenNoInferenceEngineIdAndNotAllowed_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var agent = new AesirAgent
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            AllowThinking = false,
            ChatInferenceEngineId = null
        };

        // Act
        var result = await service.ComputeIsThinkingAvailableAsync(agent);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static AesirAgent CreateAgent(Guid inferenceEngineId, bool? allowThinking = true)
    {
        return new AesirAgent
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            ChatInferenceEngineId = inferenceEngineId,
            ChatModel = "test-model",
            AllowThinking = allowThinking
        };
    }

    private static AesirInferenceEngine CreateInferenceEngine(
        Guid id,
        InferenceEngineType type,
        bool? enableThinking = null)
    {
        var config = new Dictionary<string, string?>();

        if (enableThinking.HasValue)
        {
            config["EnableChatModelThinking"] = enableThinking.Value.ToString().ToLower();
        }

        return new AesirInferenceEngine
        {
            Id = id,
            Name = "Test Engine",
            Type = type,
            Configuration = config
        };
    }

    #endregion
}
