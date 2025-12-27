using System.Text.Json;
using System.Text.RegularExpressions;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Service for conducting peer reviews of research submissions.
/// </summary>
public interface IPeerReviewService
{
    /// <summary>
    /// Conducts peer reviews for all submissions in a session.
    /// Each agent (except Chairman) reviews all other agents' submissions.
    /// </summary>
    /// <param name="session">The research session.</param>
    /// <param name="agents">The research agents (excluding Chairman).</param>
    /// <param name="anonymizedSubmissions">The anonymized submissions to review.</param>
    /// <param name="progressCallback">Optional callback for progress updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of peer reviews.</returns>
    Task<List<PeerReview>> ConductPeerReviewsAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        Dictionary<string, AnonymizedSubmission> anonymizedSubmissions,
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses peer review scores from an LLM response.
    /// </summary>
    /// <param name="response">The LLM response containing scores.</param>
    /// <returns>Parsed peer review scores.</returns>
    PeerReviewScores ParseReviewScores(string response);
}

/// <summary>
/// Parsed scores from a peer review response.
/// </summary>
public class PeerReviewScores
{
    public double Depth { get; set; }
    public double Accuracy { get; set; }
    public double SourceQuality { get; set; }
    public double Novelty { get; set; }
    public double Coherence { get; set; }
    public double WeightedAverage { get; set; }
    public string? Strengths { get; set; }
    public string? Improvements { get; set; }
    public string FullCritique { get; set; } = string.Empty;
    public bool Endorses { get; set; } = true;
}

/// <summary>
/// Implementation of the peer review service.
/// Note: Full chat integration will be added when wiring to the inference module.
/// </summary>
public class PeerReviewService : IPeerReviewService
{
    private readonly ILogger<PeerReviewService> _logger;
    private readonly IScoringCalculator _scoringCalculator;

    public PeerReviewService(
        ILogger<PeerReviewService> logger,
        IScoringCalculator scoringCalculator)
    {
        _logger = logger;
        _scoringCalculator = scoringCalculator;
    }

    /// <inheritdoc />
    public async Task<List<PeerReview>> ConductPeerReviewsAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        Dictionary<string, AnonymizedSubmission> anonymizedSubmissions,
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting peer review for session {SessionId} with {AgentCount} reviewers and {SubmissionCount} submissions",
            session.Id, agents.Count, anonymizedSubmissions.Count);

        var reviews = new List<PeerReview>();
        var reviewTasks = new List<Task<List<PeerReview>>>();

        // Each agent reviews all submissions EXCEPT their own
        foreach (var agent in agents.Where(a => !a.IsChairman))
        {
            // Find this agent's own submission to exclude it
            var ownSubmission = anonymizedSubmissions.Values
                .FirstOrDefault(s => s.OriginalAgentId == agent.BaseAgentId);

            var submissionsToReview = anonymizedSubmissions
                .Where(kv => ownSubmission == null || kv.Value.OriginalSubmissionId != ownSubmission.OriginalSubmissionId)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            reviewTasks.Add(ConductAgentReviewsAsync(
                session,
                agent,
                submissionsToReview,
                progressCallback,
                cancellationToken));
        }

        // Wait for all reviews to complete
        var allReviewResults = await Task.WhenAll(reviewTasks);

        foreach (var agentReviews in allReviewResults)
        {
            reviews.AddRange(agentReviews);
        }

        _logger.LogInformation("Peer review complete. Generated {Count} reviews", reviews.Count);

        return reviews;
    }

    /// <inheritdoc />
    public PeerReviewScores ParseReviewScores(string response)
    {
        var scores = new PeerReviewScores
        {
            FullCritique = response
        };

        // Try to extract JSON block from response
        var jsonMatch = Regex.Match(response, @"```json\s*(\{[\s\S]*?\})\s*```", RegexOptions.IgnoreCase);
        if (jsonMatch.Success)
        {
            try
            {
                var jsonStr = jsonMatch.Groups[1].Value;
                var parsed = JsonSerializer.Deserialize<JsonElement>(jsonStr);

                scores.Depth = GetJsonDouble(parsed, "depth", 5);
                scores.Accuracy = GetJsonDouble(parsed, "accuracy", 5);
                scores.SourceQuality = GetJsonDouble(parsed, "source_quality", 5);
                scores.Novelty = GetJsonDouble(parsed, "novelty", 5);
                scores.Coherence = GetJsonDouble(parsed, "coherence", 5);

                if (parsed.TryGetProperty("weighted_average", out var wa))
                {
                    scores.WeightedAverage = wa.GetDouble();
                }
                else
                {
                    scores.WeightedAverage = _scoringCalculator.CalculateWeightedAverage(
                        scores.Depth, scores.Accuracy, scores.SourceQuality,
                        scores.Novelty, scores.Coherence);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse peer review JSON, using defaults");
                SetDefaultScores(scores);
            }
        }
        else
        {
            // Try to parse inline scores like "Score: 7" or "Depth: 8/10"
            scores.Depth = ExtractInlineScore(response, "depth", 5);
            scores.Accuracy = ExtractInlineScore(response, "accuracy", 5);
            scores.SourceQuality = ExtractInlineScore(response, "source", 5);
            scores.Novelty = ExtractInlineScore(response, "novelty", 5);
            scores.Coherence = ExtractInlineScore(response, "coherence", 5);

            scores.WeightedAverage = _scoringCalculator.CalculateWeightedAverage(
                scores.Depth, scores.Accuracy, scores.SourceQuality,
                scores.Novelty, scores.Coherence);
        }

        // Extract qualitative sections
        scores.Strengths = ExtractSection(response, "Strengths", "Key Strengths");
        scores.Improvements = ExtractSection(response, "Improvements", "Areas for Improvement");

        // Check for negative endorsement signals
        scores.Endorses = !response.Contains("do not endorse", StringComparison.OrdinalIgnoreCase) &&
                          !response.Contains("cannot endorse", StringComparison.OrdinalIgnoreCase) &&
                          !response.Contains("not recommended", StringComparison.OrdinalIgnoreCase);

        return scores;
    }

    private async Task<List<PeerReview>> ConductAgentReviewsAsync(
        ResearchSession session,
        ResearchAgent reviewer,
        Dictionary<string, AnonymizedSubmission> submissions,
        Func<ResearchPhaseProgress, Task>? progressCallback,
        CancellationToken cancellationToken)
    {
        var reviews = new List<PeerReview>();

        // Report start
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.PeerReview,
                AgentRole = reviewer.Role,
                TeamMemberId = reviewer.TeamMemberId,
                Message = $"{reviewer.RoleName} is reviewing submissions...",
                PercentComplete = 0
            });
        }

        var completedCount = 0;
        foreach (var (anonymizedId, submission) in submissions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // TODO: Integrate with IChatService when available
            // For now, create a stub review
            var scores = GenerateStubReviewScores(reviewer.Role, submission);

            var review = new PeerReview
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                SubmissionId = submission.OriginalSubmissionId,
                ReviewerAgentId = reviewer.BaseAgentId,
                ReviewerRole = reviewer.Role,
                ScoreDepth = scores.Depth,
                ScoreAccuracy = scores.Accuracy,
                ScoreSourceQuality = scores.SourceQuality,
                ScoreNovelty = scores.Novelty,
                ScoreCoherence = scores.Coherence,
                WeightedAverage = scores.WeightedAverage,
                Strengths = scores.Strengths,
                Improvements = scores.Improvements,
                Critique = scores.FullCritique,
                Endorses = scores.Endorses,
                CreatedAt = DateTime.UtcNow
            };

            reviews.Add(review);
            completedCount++;

            _logger.LogDebug(
                "{Reviewer} reviewed submission {AnonymizedId} with score {Score}",
                reviewer.Role, anonymizedId, review.WeightedAverage);

            // Simulate work
            await Task.Delay(50, cancellationToken);

            // Report progress
            if (progressCallback != null)
            {
                var percent = (int)((double)completedCount / submissions.Count * 100);
                await progressCallback(new ResearchPhaseProgress
                {
                    Phase = ResearchPhase.PeerReview,
                    AgentRole = reviewer.Role,
                    TeamMemberId = reviewer.TeamMemberId,
                    Message = $"{reviewer.RoleName} reviewed {completedCount}/{submissions.Count} submissions",
                    PercentComplete = percent
                });
            }
        }

        // Report completion
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.PeerReview,
                AgentRole = reviewer.Role,
                TeamMemberId = reviewer.TeamMemberId,
                Message = $"{reviewer.RoleName} completed all reviews",
                PercentComplete = 100,
                IsComplete = true
            });
        }

        return reviews;
    }

    /// <summary>
    /// Generates stub review scores for testing.
    /// Will be replaced with actual LLM-generated reviews.
    /// </summary>
    private PeerReviewScores GenerateStubReviewScores(ResearchRole reviewerRole, AnonymizedSubmission submission)
    {
        // Generate scores based on reviewer perspective
        var baseScore = 7.0; // Default good score

        var scores = new PeerReviewScores
        {
            Depth = baseScore + (reviewerRole == ResearchRole.DeepDiver ? 0.5 : 0),
            Accuracy = baseScore + 0.3,
            SourceQuality = baseScore - 0.2,
            Novelty = baseScore + (reviewerRole == ResearchRole.DevilsAdvocate ? -0.5 : 0.2),
            Coherence = baseScore + 0.4,
            Strengths = $"[Stub] Well-structured research approach. Good use of sources.",
            Improvements = $"[Stub] Could explore alternative perspectives. Additional citations would strengthen claims.",
            FullCritique = $"""
                # Peer Review of Submission {submission.AnonymizedId}

                *Note: This is a stub review. Full LLM integration pending.*

                ## Overall Assessment
                This submission demonstrates solid research methodology with room for improvement.

                ## Scores
                - Depth: {baseScore + 0.5}/10
                - Accuracy: {baseScore + 0.3}/10
                - Source Quality: {baseScore - 0.2}/10
                - Novelty: {baseScore + 0.2}/10
                - Coherence: {baseScore + 0.4}/10

                ## Recommendation
                Endorsed with minor revisions suggested.
                """,
            Endorses = true
        };

        scores.WeightedAverage = _scoringCalculator.CalculateWeightedAverage(
            scores.Depth, scores.Accuracy, scores.SourceQuality,
            scores.Novelty, scores.Coherence);

        return scores;
    }

    private static void SetDefaultScores(PeerReviewScores scores)
    {
        scores.Depth = 5;
        scores.Accuracy = 5;
        scores.SourceQuality = 5;
        scores.Novelty = 5;
        scores.Coherence = 5;
        scores.WeightedAverage = 5;
    }

    private static double GetJsonDouble(JsonElement element, string propertyName, double defaultValue)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.TryGetDouble(out var value))
                return Math.Clamp(value, 1, 10);
        }
        return defaultValue;
    }

    private static double ExtractInlineScore(string text, string criterion, double defaultValue)
    {
        // Look for patterns like "Depth: 8" or "depth: 8/10" or "Score: 7"
        var patterns = new[]
        {
            $@"{criterion}\s*[:\-]\s*(\d+(?:\.\d+)?)",
            $@"{criterion}.*?(\d+(?:\.\d+)?)\s*/\s*10"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && double.TryParse(match.Groups[1].Value, out var score))
            {
                return Math.Clamp(score, 1, 10);
            }
        }

        return defaultValue;
    }

    private static string? ExtractSection(string text, params string[] sectionNames)
    {
        foreach (var name in sectionNames)
        {
            // Look for section headers like "### Key Strengths" or "## Strengths"
            var pattern = $@"#+\s*{name}\s*\n([\s\S]*?)(?=\n#+\s|\n##|\z)";
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var content = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(content))
                    return content;
            }
        }

        return null;
    }
}
