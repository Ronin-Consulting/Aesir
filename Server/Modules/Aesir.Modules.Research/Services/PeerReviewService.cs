using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Execution;
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of peer reviews.</returns>
    Task<List<PeerReview>> ConductPeerReviewsAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        Dictionary<string, AnonymizedSubmission> anonymizedSubmissions,
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
/// Uses IChatService for LLM-based peer reviews with parallel execution.
/// </summary>
public class PeerReviewService : IPeerReviewService
{
    private readonly ILogger<PeerReviewService> _logger;
    private readonly IChatServiceResolver _chatServiceResolver;
    private readonly IChatRequestBuilder _chatRequestBuilder;
    private readonly IScoringCalculator _scoringCalculator;
    private readonly IResearchProgressBroadcaster _progressBroadcaster;
    private readonly IPhaseExecutionStrategyFactory _strategyFactory;

    public PeerReviewService(
        ILogger<PeerReviewService> logger,
        IChatServiceResolver chatServiceResolver,
        IChatRequestBuilder chatRequestBuilder,
        IScoringCalculator scoringCalculator,
        IResearchProgressBroadcaster progressBroadcaster,
        IPhaseExecutionStrategyFactory strategyFactory)
    {
        _logger = logger;
        _chatServiceResolver = chatServiceResolver;
        _chatRequestBuilder = chatRequestBuilder;
        _scoringCalculator = scoringCalculator;
        _progressBroadcaster = progressBroadcaster;
        _strategyFactory = strategyFactory;
    }

    /// <inheritdoc />
    public async Task<List<PeerReview>> ConductPeerReviewsAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        Dictionary<string, AnonymizedSubmission> anonymizedSubmissions,
        CancellationToken cancellationToken = default)
    {
        var reviewers = agents.Where(a => !a.IsChairman).ToList();

        // Build work items: each reviewer reviews each submission (except their own)
        var workItems = BuildReviewWorkItems(reviewers, anonymizedSubmissions);

        _logger.LogInformation(
            "Starting peer review for session {SessionId} with {AgentCount} reviewers and {SubmissionCount} submissions, {WorkItemCount} total reviews (parallel execution: max 2 concurrent)",
            session.Id, reviewers.Count, anonymizedSubmissions.Count, workItems.Count);

        // Get the peer review execution strategy (uses parallel execution with throttling)
        var strategy = _strategyFactory.CreatePeerReviewStrategy();

        // Execute peer reviews in parallel with agent tracking
        var results = await strategy.ExecutePhaseWithAgentTrackingAsync(
            session,
            workItems,
            async (sess, input, ct) => await ConductSingleReviewAsync(sess, input.Reviewer, input.Submission, ct).ConfigureAwait(false),
            input => new Contracts.ActiveAgentInfo
            {
                TeamMemberId = input.Reviewer.TeamMemberId,
                Role = input.Reviewer.Role,
                RoleName = input.Reviewer.RoleName,
                Activity = $"{input.Reviewer.RoleName} reviewing {input.Submission.AnonymizedId}...",
                AgentProgressPercent = null
            },
            cancellationToken).ConfigureAwait(false);

        var reviews = results.ToList();

        _logger.LogInformation("Peer review complete. Generated {Count} reviews", reviews.Count);

        return reviews;
    }

    /// <summary>
    /// Builds work items for parallel peer review execution.
    /// Each reviewer reviews each submission except their own.
    /// </summary>
    private List<(ResearchAgent Reviewer, AnonymizedSubmission Submission)> BuildReviewWorkItems(
        List<ResearchAgent> reviewers,
        Dictionary<string, AnonymizedSubmission> anonymizedSubmissions)
    {
        var workItems = new List<(ResearchAgent Reviewer, AnonymizedSubmission Submission)>();

        foreach (var reviewer in reviewers)
        {
            // Find this agent's own submission to exclude it
            // Use Role for matching instead of BaseAgentId, because the same base agent
            // can be reused across multiple research roles, but Role is unique per team
            var ownSubmission = anonymizedSubmissions.Values
                .FirstOrDefault(s => s.OriginalRole == reviewer.Role);

            _logger.LogDebug(
                "Peer review assignment: {ReviewerRole} will exclude submission {ExcludedId} (found={Found})",
                reviewer.Role,
                ownSubmission?.AnonymizedId ?? "none",
                ownSubmission != null);

            var submissionsToReview = anonymizedSubmissions.Values
                .Where(s => ownSubmission == null || s.OriginalSubmissionId != ownSubmission.OriginalSubmissionId)
                .ToList();

            _logger.LogDebug(
                "Peer review assignment: {ReviewerRole} will review {Count} submissions: {Ids}",
                reviewer.Role,
                submissionsToReview.Count,
                string.Join(", ", submissionsToReview.Select(s => s.AnonymizedId)));

            foreach (var submission in submissionsToReview)
            {
                workItems.Add((reviewer, submission));
            }
        }

        return workItems;
    }

    /// <summary>
    /// Conducts a single peer review for one reviewer-submission pair.
    /// </summary>
    private async Task<PeerReview> ConductSingleReviewAsync(
        ResearchSession session,
        ResearchAgent reviewer,
        AnonymizedSubmission submission,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "[PEER-REVIEW] {Reviewer} reviewing submission {SubmissionId}",
            reviewer.Role, submission.AnonymizedId);

        PeerReviewScores scores;

        // Get chat service for this reviewer
        var chatService = _chatServiceResolver.GetChatService(reviewer.InferenceEngineId);

        if (chatService != null)
        {
            // Use LLM to conduct peer review
            scores = await ConductLlmPeerReviewAsync(
                session, reviewer, submission.AnonymizedId, submission, chatService, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Fallback to stub if no chat service available
            _logger.LogWarning("[PEER-REVIEW] No chat service available for reviewer {Role}, using stub scores", reviewer.Role);
            scores = GenerateFallbackScores(submission);
        }

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

        _logger.LogDebug(
            "[PEER-REVIEW] {Reviewer} reviewed submission {AnonymizedId} with score {Score}",
            reviewer.Role, submission.AnonymizedId, review.WeightedAverage);

        return review;
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

    /// <summary>
    /// Conducts a peer review using non-streaming LLM chat completion.
    /// </summary>
    private async Task<PeerReviewScores> ConductLlmPeerReviewAsync(
        ResearchSession session,
        ResearchAgent reviewer,
        string anonymizedId,
        AnonymizedSubmission submission,
        IChatService chatService,
        CancellationToken cancellationToken)
    {
        // Build peer review prompt
        var reviewPrompt = BuildPeerReviewPrompt(session, anonymizedId, submission);

        // Create chat request using the builder
        var systemPrompt = $"""
            You are {reviewer.RoleName}, an expert peer reviewer.
            Your role is to critically evaluate research submissions with fairness and objectivity.
            Provide constructive feedback that helps improve research quality.
            Score submissions honestly based on their actual merit.
            """;

        var request = await _chatRequestBuilder.BuildAsync(
            reviewer,
            systemPrompt,
            reviewPrompt,
            new ChatRequestOptions
            {
                IncludeTools = false,
                MaxTokensOverride = 4096,
                User = "research-peer-review",
                Title = $"Peer Review: {reviewer.RoleName}"
            }).ConfigureAwait(false);

        _logger.LogDebug("Sending non-streaming peer review request to LLM for reviewer {Role} on submission {SubmissionId}",
            reviewer.Role, anonymizedId);

        // Execute non-streaming LLM call
        var result = await chatService.ChatCompletionsAsync(request);

        // Extract the assistant's response from the conversation
        var response = result.AesirConversation?.Messages?
            .LastOrDefault(m => m.Role == "assistant")?.Content ?? string.Empty;

        _logger.LogDebug("Peer review response received for reviewer {Role}, length: {Length} chars",
            reviewer.Role, response.Length);

        // Parse the scores from the LLM response
        return ParseReviewScores(response);
    }

    /// <summary>
    /// Builds the peer review prompt for an LLM.
    /// </summary>
    private static string BuildPeerReviewPrompt(
        ResearchSession session,
        string anonymizedId,
        AnonymizedSubmission submission)
    {
        return $$"""
            You are conducting a blind peer review of research submission "{{anonymizedId}}".

            ## Research Query
            {{session.Query}}

            ## Submission Content
            {{submission.AnonymizedContent}}

            ## Your Task
            Evaluate this research submission objectively. You do not know who wrote it.

            Provide scores (1-10) and detailed feedback in the following JSON format:
            ```json
            {
              "depth": <1-10>,
              "accuracy": <1-10>,
              "source_quality": <1-10>,
              "novelty": <1-10>,
              "coherence": <1-10>,
              "weighted_average": <calculated average>
            }
            ```

            Then provide:
            ### Key Strengths
            [What does this submission do well?]

            ### Areas for Improvement
            [What could be improved?]

            ### Overall Assessment
            [Your summary evaluation and whether you endorse this research]

            Be specific and constructive in your feedback. Focus on the quality of the research, accuracy of claims, use of sources, and clarity of presentation.
            """;
    }

    /// <summary>
    /// Generates fallback scores when LLM is not available.
    /// </summary>
    private PeerReviewScores GenerateFallbackScores(AnonymizedSubmission submission)
    {
        var scores = new PeerReviewScores
        {
            Depth = 5,
            Accuracy = 5,
            SourceQuality = 5,
            Novelty = 5,
            Coherence = 5,
            Strengths = "[Fallback] Unable to generate LLM-based review.",
            Improvements = "[Fallback] Unable to generate LLM-based review.",
            FullCritique = $"[Fallback] Peer review for submission {submission.AnonymizedId} could not be completed via LLM. Default scores assigned.",
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
