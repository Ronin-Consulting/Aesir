using System.Data;
using Aesir.Common.Models;
using Aesir.Infrastructure.Data;
using Aesir.Infrastructure.Exceptions;
using Aesir.Infrastructure.Models;
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

    #region DeleteAgentAsync Foreign Key Violation Tests

    [Fact]
    public async Task DeleteAgentAsync_WhenAgentNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Configuration:LoadFromDatabase"] = "true"
        });

        var agentId = Guid.NewGuid();

        // Mock GetAgentAsync to return null - use callback to invoke the func with null connection
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<AesirAgent?>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<AesirAgent?>> _, bool _) => Task.FromResult<AesirAgent?>(null));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.DeleteAgentAsync(agentId));

        exception.EntityType.Should().Be("Agent");
        exception.EntityId.Should().Be(agentId);
        exception.Message.Should().Contain(agentId.ToString());
    }

    [Fact]
    public async Task DeleteAgentAsync_WhenReferencedByResearchTeamMember_ThrowsForeignKeyViolationException()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Configuration:LoadFromDatabase"] = "true"
        });

        var agentId = Guid.NewGuid();
        var agentName = "Research Assistant";

        // Mock GetAgentAsync to return an agent (no inference engine to avoid ComputeIsThinkingAvailableAsync lookup)
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<AesirAgent?>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<AesirAgent?>> _, bool _) =>
                Task.FromResult<AesirAgent?>(new AesirAgent { Id = Guid.NewGuid(), Name = agentName, ChatInferenceEngineId = null }));

        // Mock reference count check to return TeamMemberCount > 0
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<AgentReferenceCount>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<AgentReferenceCount>> _, bool _) =>
                Task.FromResult(new AgentReferenceCount
                {
                    TeamMemberCount = 3,
                    SubmissionCount = 0,
                    ReviewCount = 0
                }));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForeignKeyViolationException>(
            () => service.DeleteAgentAsync(agentId));

        exception.ConstraintName.Should().Be("FK_aesir_research_team_member_agent_id_aesir_agent_id");
        exception.EntityName.Should().Be(agentName);
        exception.Message.Should().Contain(agentName);
        exception.Message.Should().Contain("3 research team(s)");
        exception.SuggestedAction.Should().Contain("Remove the agent from all research teams");
    }

    [Fact]
    public async Task DeleteAgentAsync_WhenReferencedByResearchSubmission_ThrowsForeignKeyViolationException()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Configuration:LoadFromDatabase"] = "true"
        });

        var agentId = Guid.NewGuid();
        var agentName = "Data Analyst";

        // Mock GetAgentAsync to return an agent (no inference engine to avoid ComputeIsThinkingAvailableAsync lookup)
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<AesirAgent?>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<AesirAgent?>> _, bool _) =>
                Task.FromResult<AesirAgent?>(new AesirAgent { Id = Guid.NewGuid(), Name = agentName, ChatInferenceEngineId = null }));

        // Mock reference count check to return SubmissionCount > 0
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<AgentReferenceCount>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<AgentReferenceCount>> _, bool _) =>
                Task.FromResult(new AgentReferenceCount
                {
                    TeamMemberCount = 0,
                    SubmissionCount = 5,
                    ReviewCount = 0
                }));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForeignKeyViolationException>(
            () => service.DeleteAgentAsync(agentId));

        exception.ConstraintName.Should().Be("FK_aesir_research_submission_agent_id_aesir_agent_id");
        exception.EntityName.Should().Be(agentName);
        exception.Message.Should().Contain(agentName);
        exception.Message.Should().Contain("5 research contribution(s)");
        exception.SuggestedAction.Should().Contain("historical data");
        exception.SuggestedAction.Should().Contain("deactivating");
    }

    [Fact]
    public async Task DeleteAgentAsync_WhenReferencedByPeerReview_ThrowsForeignKeyViolationException()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Configuration:LoadFromDatabase"] = "true"
        });

        var agentId = Guid.NewGuid();
        var agentName = "Reviewer Bot";

        // Mock GetAgentAsync to return an agent (no inference engine to avoid ComputeIsThinkingAvailableAsync lookup)
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<AesirAgent?>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<AesirAgent?>> _, bool _) =>
                Task.FromResult<AesirAgent?>(new AesirAgent { Id = Guid.NewGuid(), Name = agentName, ChatInferenceEngineId = null }));

        // Mock reference count check to return ReviewCount > 0
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<AgentReferenceCount>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<AgentReferenceCount>> _, bool _) =>
                Task.FromResult(new AgentReferenceCount
                {
                    TeamMemberCount = 0,
                    SubmissionCount = 0,
                    ReviewCount = 10
                }));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForeignKeyViolationException>(
            () => service.DeleteAgentAsync(agentId));

        exception.ConstraintName.Should().Be("FK_aesir_research_peer_review_reviewer_agent_id_aesir_agent_id");
        exception.EntityName.Should().Be(agentName);
        exception.Message.Should().Contain(agentName);
        exception.Message.Should().Contain("10 research submission(s)");
        exception.SuggestedAction.Should().Contain("historical data");
        exception.SuggestedAction.Should().Contain("deactivating");
    }

    #endregion

    #region DeleteInferenceEngineAsync Foreign Key Violation Tests

    [Fact]
    public async Task DeleteInferenceEngineAsync_WhenEngineNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Configuration:LoadFromDatabase"] = "true"
        });

        var engineId = Guid.NewGuid();

        // Mock GetInferenceEngineAsync to return null
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<AesirInferenceEngine?>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<AesirInferenceEngine?>> _, bool _) => Task.FromResult<AesirInferenceEngine?>(null));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.DeleteInferenceEngineAsync(engineId));

        exception.EntityType.Should().Be("InferenceEngine");
        exception.EntityId.Should().Be(engineId);
        exception.Message.Should().Contain(engineId.ToString());
    }

    [Fact]
    public async Task DeleteInferenceEngineAsync_WhenReferencedByAgents_ThrowsForeignKeyViolationException()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Configuration:LoadFromDatabase"] = "true"
        });

        var engineId = Guid.NewGuid();
        var engineName = "GPT-4 Engine";

        // Mock GetInferenceEngineAsync to return an engine
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<AesirInferenceEngine?>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<AesirInferenceEngine?>> _, bool _) =>
                Task.FromResult<AesirInferenceEngine?>(CreateInferenceEngine(engineId, InferenceEngineType.OpenAICompatible, name: engineName)));

        // Mock agent count check to return 7
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<int>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<int>> _, bool _) => Task.FromResult(7));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForeignKeyViolationException>(
            () => service.DeleteInferenceEngineAsync(engineId));

        exception.ConstraintName.Should().Be("FK_aesir_agent_chat_inference_engine_id_aesir_inference_engine_id");
        exception.EntityName.Should().Be(engineName);
        exception.Message.Should().Contain(engineName);
        exception.Message.Should().Contain("7 agent(s)");
        exception.SuggestedAction.Should().Contain("Reassign the agents");
    }

    [Fact]
    public async Task DeleteInferenceEngineAsync_WhenConfiguredAsRagEmbedding_ThrowsForeignKeyViolationException()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Configuration:LoadFromDatabase"] = "true"
        });

        var engineId = Guid.NewGuid();
        var engineName = "Embedding Engine";

        // Mock GetInferenceEngineAsync to return an engine
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<AesirInferenceEngine?>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<AesirInferenceEngine?>> _, bool _) =>
                Task.FromResult<AesirInferenceEngine?>(CreateInferenceEngine(engineId, InferenceEngineType.OpenAICompatible, name: engineName)));

        // Mock agent count check to return 0 (no agents)
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<int>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<int>> _, bool _) => Task.FromResult(0));

        // Mock GetGeneralSettingsAsync to return engine as RAG embedding
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<AesirGeneralSettings?>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<AesirGeneralSettings?>> _, bool _) =>
                Task.FromResult<AesirGeneralSettings?>(new AesirGeneralSettings
                {
                    RagEmbeddingInferenceEngineId = engineId,
                    RagVisionInferenceEngineId = null
                }));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForeignKeyViolationException>(
            () => service.DeleteInferenceEngineAsync(engineId));

        exception.ConstraintName.Should().Be("FK_AppSettings_RagEmbedding");
        exception.EntityName.Should().Be(engineName);
        exception.Message.Should().Contain(engineName);
        exception.Message.Should().Contain("RAG embedding engine");
        exception.SuggestedAction.Should().Contain("General Settings");
    }

    [Fact]
    public async Task DeleteInferenceEngineAsync_WhenConfiguredAsRagVision_ThrowsForeignKeyViolationException()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["Configuration:LoadFromDatabase"] = "true"
        });

        var engineId = Guid.NewGuid();
        var engineName = "Vision Engine";

        // Mock GetInferenceEngineAsync to return an engine
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<AesirInferenceEngine?>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<AesirInferenceEngine?>> _, bool _) =>
                Task.FromResult<AesirInferenceEngine?>(CreateInferenceEngine(engineId, InferenceEngineType.OpenAICompatible, name: engineName)));

        // Mock agent count check to return 0 (no agents)
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<int>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<int>> _, bool _) => Task.FromResult(0));

        // Mock GetGeneralSettingsAsync to return engine as RAG vision
        _dbContextMock.Setup(x => x.UnitOfWorkAsync(It.IsAny<Func<IDbConnection, Task<AesirGeneralSettings?>>>(), It.IsAny<bool>()))
            .Returns((Func<IDbConnection, Task<AesirGeneralSettings?>> _, bool _) =>
                Task.FromResult<AesirGeneralSettings?>(new AesirGeneralSettings
                {
                    RagEmbeddingInferenceEngineId = null,
                    RagVisionInferenceEngineId = engineId
                }));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForeignKeyViolationException>(
            () => service.DeleteInferenceEngineAsync(engineId));

        exception.ConstraintName.Should().Be("FK_AppSettings_RagVision");
        exception.EntityName.Should().Be(engineName);
        exception.Message.Should().Contain(engineName);
        exception.Message.Should().Contain("RAG vision engine");
        exception.SuggestedAction.Should().Contain("General Settings");
    }

    #endregion

    #region Helper Methods

    private static AesirAgent CreateAgent(Guid inferenceEngineId, bool? allowThinking = true, string name = "Test Agent")
    {
        return new AesirAgent
        {
            Id = Guid.NewGuid(),
            Name = name,
            ChatInferenceEngineId = inferenceEngineId,
            ChatModel = "test-model",
            AllowThinking = allowThinking
        };
    }

    private static AesirInferenceEngine CreateInferenceEngine(
        Guid id,
        InferenceEngineType type,
        bool? enableThinking = null,
        string name = "Test Engine")
    {
        var config = new Dictionary<string, string?>();

        if (enableThinking.HasValue)
        {
            config["enable_chat_model_thinking"] = enableThinking.Value.ToString().ToLower();
        }

        return new AesirInferenceEngine
        {
            Id = id,
            Name = name,
            Type = type,
            Configuration = config
        };
    }

    #endregion
}
