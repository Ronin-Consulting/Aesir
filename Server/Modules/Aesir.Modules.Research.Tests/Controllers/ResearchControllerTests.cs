using Aesir.Infrastructure.Documents;
using Aesir.Modules.Research.Contracts;
using Aesir.Modules.Research.Controllers;
using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aesir.Modules.Research.Tests.Controllers;

public class ResearchControllerTests
{
    private readonly Mock<ILogger<ResearchController>> _logger;
    private readonly Mock<IResearchOrchestrator> _orchestrator;
    private readonly Mock<IResearchSessionRepository> _sessionRepository;
    private readonly Mock<IResearchReportExporter> _reportExporter;
    private readonly ResearchController _controller;

    public ResearchControllerTests()
    {
        _logger = new Mock<ILogger<ResearchController>>();
        _orchestrator = new Mock<IResearchOrchestrator>();
        _sessionRepository = new Mock<IResearchSessionRepository>();
        _reportExporter = new Mock<IResearchReportExporter>();

        _controller = new ResearchController(
            _logger.Object,
            _orchestrator.Object,
            _sessionRepository.Object,
            _reportExporter.Object);
    }

    #region GetSessionsByConversation Tests

    [Fact]
    public async Task GetSessionsByConversation_WithSessions_ReturnsOkWithList()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var sessions = new List<ResearchSession>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = "test@example.com",
                Query = "Test query 1",
                Status = ResearchStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                UserId = "test@example.com",
                Query = "Test query 2",
                Status = ResearchStatus.Researching,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        _sessionRepository
            .Setup(x => x.GetByConversationIdAsync(conversationId))
            .ReturnsAsync(sessions);

        // Act
        var result = await _controller.GetSessionsByConversation(conversationId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ResearchSessionListResponse>().Subject;
        response.Sessions.Should().HaveCount(2);
        response.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetSessionsByConversation_NoSessions_ReturnsOkWithEmptyList()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        _sessionRepository
            .Setup(x => x.GetByConversationIdAsync(conversationId))
            .ReturnsAsync(new List<ResearchSession>());

        // Act
        var result = await _controller.GetSessionsByConversation(conversationId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ResearchSessionListResponse>().Subject;
        response.Sessions.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetSessionsByConversation_RepositoryThrows_Returns500()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        _sessionRepository
            .Setup(x => x.GetByConversationIdAsync(conversationId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetSessionsByConversation(conversationId);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetSessionsByConversation_MapsSessionsCorrectly()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var session = new ResearchSession
        {
            Id = sessionId,
            ConversationId = conversationId,
            ResearchTeamId = teamId,
            UserId = "test@example.com",
            Query = "Test query",
            RefinedQuery = "Refined query",
            Status = ResearchStatus.Completed,
            CurrentPhase = ResearchPhase.Synthesis,
            Mode = ResearchMode.Standard,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            StartedAt = DateTime.UtcNow.AddMinutes(-50),
            CompletedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        _sessionRepository
            .Setup(x => x.GetByConversationIdAsync(conversationId))
            .ReturnsAsync(new List<ResearchSession> { session });

        // Act
        var result = await _controller.GetSessionsByConversation(conversationId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ResearchSessionListResponse>().Subject;
        response.Sessions.Should().HaveCount(1);

        var mappedSession = response.Sessions[0];
        mappedSession.Id.Should().Be(sessionId);
        mappedSession.ConversationId.Should().Be(conversationId);
        mappedSession.ResearchTeamId.Should().Be(teamId);
        mappedSession.Query.Should().Be("Test query");
        mappedSession.RefinedQuery.Should().Be("Refined query");
        mappedSession.Status.Should().Be(ResearchStatus.Completed);
        mappedSession.CurrentPhase.Should().Be(ResearchPhase.Synthesis);
    }

    [Fact]
    public async Task GetSessionsByConversation_CompletedSession_IncludesReport()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var report = new ResearchReport
        {
            Id = reportId,
            SessionId = sessionId,
            Title = "Test Report",
            ExecutiveSummary = "Test summary",
            Findings = new List<ResearchFinding>
            {
                new() { Title = "Finding 1", Content = "Content 1", Confidence = ConfidenceLevel.High }
            },
            Bibliography = new List<ResearchSource>
            {
                new() { Title = "Source 1", Url = "https://example.com" }
            },
            CreatedAt = DateTime.UtcNow
        };

        var session = new ResearchSession
        {
            Id = sessionId,
            ConversationId = conversationId,
            UserId = "test@example.com",
            Query = "Test query",
            Status = ResearchStatus.Completed,
            CurrentPhase = ResearchPhase.Synthesis,
            Report = report
        };

        _sessionRepository
            .Setup(x => x.GetByConversationIdAsync(conversationId))
            .ReturnsAsync(new List<ResearchSession> { session });

        // Act
        var result = await _controller.GetSessionsByConversation(conversationId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ResearchSessionListResponse>().Subject;
        response.Sessions.Should().HaveCount(1);

        var mappedSession = response.Sessions[0];
        mappedSession.Status.Should().Be(ResearchStatus.Completed);
        mappedSession.Report.Should().NotBeNull("Completed sessions should include their report");
        mappedSession.Report!.Id.Should().Be(reportId);
        mappedSession.Report.Title.Should().Be("Test Report");
        mappedSession.Report.ExecutiveSummary.Should().Be("Test summary");
        mappedSession.Report.FindingCount.Should().Be(1);
        mappedSession.Report.SourceCount.Should().Be(1);
    }

    #endregion
}
