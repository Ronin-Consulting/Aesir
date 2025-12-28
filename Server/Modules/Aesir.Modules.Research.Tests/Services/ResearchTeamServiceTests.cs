using Aesir.Common.Models;
using Aesir.Infrastructure.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Tests.Services;

public class ResearchTeamServiceTests
{
    private readonly Mock<ILogger<ResearchTeamService>> _loggerMock;
    private readonly Mock<IResearchTeamRepository> _repositoryMock;
    private readonly Mock<IConfigurationService> _configurationServiceMock;
    private readonly ResearchTeamService _service;

    public ResearchTeamServiceTests()
    {
        _loggerMock = new Mock<ILogger<ResearchTeamService>>();
        _repositoryMock = new Mock<IResearchTeamRepository>();
        _configurationServiceMock = new Mock<IConfigurationService>();

        _service = new ResearchTeamService(
            _loggerMock.Object,
            _repositoryMock.Object,
            _configurationServiceMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingTeam_ReturnsTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var expectedTeam = new ResearchTeam { Id = teamId, Name = "Test Team" };
        _repositoryMock.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(expectedTeam);

        // Act
        var result = await _service.GetByIdAsync(teamId);

        // Assert
        result.Should().Be(expectedTeam);
        _repositoryMock.Verify(r => r.GetByIdAsync(teamId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingTeam_ReturnsNull()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync((ResearchTeam?)null);

        // Act
        var result = await _service.GetByIdAsync(teamId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_ReturnsTeamsForUser()
    {
        // Arrange
        var userId = "test-user";
        var expectedTeams = new List<ResearchTeam>
        {
            new() { Id = Guid.NewGuid(), Name = "Team 1", UserId = userId },
            new() { Id = Guid.NewGuid(), Name = "Team 2", UserId = userId }
        };
        _repositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(expectedTeams);

        // Act
        var result = await _service.GetByUserIdAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(expectedTeams);
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_ReturnsOnlyActiveTeams()
    {
        // Arrange
        var userId = "test-user";
        var expectedTeams = new List<ResearchTeam>
        {
            new() { Id = Guid.NewGuid(), Name = "Active Team", UserId = userId, IsActive = true }
        };
        _repositoryMock.Setup(r => r.GetActiveByUserIdAsync(userId)).ReturnsAsync(expectedTeams);

        // Act
        var result = await _service.GetActiveByUserIdAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(expectedTeams);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidTeamWithoutMembers_CreatesTeam()
    {
        // Arrange
        var team = new ResearchTeam { Name = "New Team", UserId = "user1" };
        var createdTeam = new ResearchTeam { Id = Guid.NewGuid(), Name = "New Team", UserId = "user1" };
        _repositoryMock.Setup(r => r.AddAsync(team)).ReturnsAsync(createdTeam);

        // Act
        var result = await _service.CreateAsync(team);

        // Assert
        result.Should().Be(createdTeam);
        _repositoryMock.Verify(r => r.AddAsync(team), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidTeamWithMembers_ValidatesAgentIds()
    {
        // Arrange
        var agentId1 = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();
        var agentId3 = Guid.NewGuid();

        var team = CreateTeamWithAllRoles(agentId1, agentId2, agentId3);

        _configurationServiceMock.Setup(c => c.GetAgentsAsync()).ReturnsAsync(new List<AesirAgent>
        {
            new() { Id = agentId1 },
            new() { Id = agentId2 },
            new() { Id = agentId3 }
        });

        _repositoryMock.Setup(r => r.AddAsync(team)).ReturnsAsync(team);

        // Act
        var result = await _service.CreateAsync(team);

        // Assert
        result.Should().NotBeNull();
        _configurationServiceMock.Verify(c => c.GetAgentsAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidAgentId_ThrowsArgumentException()
    {
        // Arrange
        var team = CreateTeamWithAllRoles(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _configurationServiceMock.Setup(c => c.GetAgentsAsync())
            .ReturnsAsync(new List<AesirAgent>()); // No valid agents

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(team));
    }

    [Fact]
    public async Task CreateAsync_MissingRequiredRole_ThrowsArgumentException()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var team = new ResearchTeam
        {
            Name = "Incomplete Team",
            UserId = "user1",
            Members = new List<ResearchTeamMember>
            {
                new() { Role = ResearchRole.DeepDiver, AgentId = agentId, IsActive = true }
                // Missing Synthesizer and DevilsAdvocate
            }
        };

        _configurationServiceMock.Setup(c => c.GetAgentsAsync())
            .ReturnsAsync(new List<AesirAgent> { new() { Id = agentId } });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(team));
        exception.Message.Should().Contain("Missing required role assignments");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingTeam_UpdatesSuccessfully()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var existingTeam = new ResearchTeam { Id = teamId, Name = "Old Name" };
        var updatedTeam = new ResearchTeam { Id = teamId, Name = "New Name" };

        _repositoryMock.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(existingTeam);
        _repositoryMock.Setup(r => r.UpdateAsync(updatedTeam)).ReturnsAsync(true);

        // Act
        var result = await _service.UpdateAsync(updatedTeam);

        // Assert
        _repositoryMock.Verify(r => r.UpdateAsync(updatedTeam), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingTeam_ThrowsKeyNotFoundException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var team = new ResearchTeam { Id = teamId, Name = "Updated Team" };
        _repositoryMock.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync((ResearchTeam?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(team));
    }

    [Fact]
    public async Task UpdateAsync_RepositoryFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var existingTeam = new ResearchTeam { Id = teamId, Name = "Team" };
        var updatedTeam = new ResearchTeam { Id = teamId, Name = "Updated" };

        _repositoryMock.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(existingTeam);
        _repositoryMock.Setup(r => r.UpdateAsync(updatedTeam)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(updatedTeam));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingTeam_DeletesSuccessfully()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.RemoveAsync(teamId)).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(teamId);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.RemoveAsync(teamId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingTeam_ReturnsFalse()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.RemoveAsync(teamId)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(teamId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ValidateAgentIdsAsync Tests

    [Fact]
    public async Task ValidateAgentIdsAsync_AllValidAgents_ReturnsTrue()
    {
        // Arrange
        var agentId1 = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();

        _configurationServiceMock.Setup(c => c.GetAgentsAsync()).ReturnsAsync(new List<AesirAgent>
        {
            new() { Id = agentId1 },
            new() { Id = agentId2 }
        });

        // Act
        var result = await _service.ValidateAgentIdsAsync(new[] { agentId1, agentId2 });

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAgentIdsAsync_InvalidAgent_ReturnsFalse()
    {
        // Arrange
        var validAgentId = Guid.NewGuid();
        var invalidAgentId = Guid.NewGuid();

        _configurationServiceMock.Setup(c => c.GetAgentsAsync()).ReturnsAsync(new List<AesirAgent>
        {
            new() { Id = validAgentId }
        });

        // Act
        var result = await _service.ValidateAgentIdsAsync(new[] { validAgentId, invalidAgentId });

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region DuplicateAsync Tests

    [Fact]
    public async Task DuplicateAsync_ExistingTeam_CreatesDuplicate()
    {
        // Arrange
        var originalId = Guid.NewGuid();
        var newName = "Duplicated Team";
        var originalTeam = new ResearchTeam
        {
            Id = originalId,
            Name = "Original Team",
            UserId = "user1",
            Description = "Description",
            Members = new List<ResearchTeamMember>
            {
                new() { Role = ResearchRole.DeepDiver, AgentId = Guid.NewGuid(), IsActive = true }
            }
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(originalId)).ReturnsAsync(originalTeam);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<ResearchTeam>()))
            .ReturnsAsync((ResearchTeam t) => { t.Id = Guid.NewGuid(); return t; });

        // Act
        var result = await _service.DuplicateAsync(originalId, newName);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(newName);
        result.UserId.Should().Be(originalTeam.UserId);
        result.Description.Should().Be(originalTeam.Description);
        result.Id.Should().NotBe(originalId);
    }

    [Fact]
    public async Task DuplicateAsync_NonExistingTeam_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(nonExistentId)).ReturnsAsync((ResearchTeam?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.DuplicateAsync(nonExistentId, "New Name"));
    }

    #endregion

    #region Helper Methods

    private static ResearchTeam CreateTeamWithAllRoles(Guid deepDiverId, Guid synthesizerId, Guid devilsAdvocateId)
    {
        return new ResearchTeam
        {
            Name = "Complete Team",
            UserId = "user1",
            Members = new List<ResearchTeamMember>
            {
                new() { Role = ResearchRole.DeepDiver, AgentId = deepDiverId, IsActive = true },
                new() { Role = ResearchRole.Synthesizer, AgentId = synthesizerId, IsActive = true },
                new() { Role = ResearchRole.DevilsAdvocate, AgentId = devilsAdvocateId, IsActive = true }
            }
        };
    }

    #endregion
}
