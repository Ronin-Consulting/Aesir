using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;
using Microsoft.Extensions.DependencyInjection;
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
/// Uses IChatService for LLM-based peer reviews.
/// </summary>
public class PeerReviewService : IPeerReviewService
{
    private readonly ILogger<PeerReviewService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationService _configurationService;
    private readonly IScoringCalculator _scoringCalculator;
    private readonly IResearchProgressBroadcaster _progressBroadcaster;

    public PeerReviewService(
        ILogger<PeerReviewService> logger,
        IServiceProvider serviceProvider,
        IConfigurationService configurationService,
        IScoringCalculator scoringCalculator,
        IResearchProgressBroadcaster progressBroadcaster)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configurationService = configurationService;
        _scoringCalculator = scoringCalculator;
        _progressBroadcaster = progressBroadcaster;
    }

    /// <inheritdoc />
    public async Task<List<PeerReview>> ConductPeerReviewsAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        Dictionary<string, AnonymizedSubmission> anonymizedSubmissions,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting peer review for session {SessionId} with {AgentCount} reviewers and {SubmissionCount} submissions (sequential, non-streaming)",
            session.Id, agents.Count, anonymizedSubmissions.Count);

        var reviews = new List<PeerReview>();
        var reviewers = agents.Where(a => !a.IsChairman).ToList();

        // Execute reviewers sequentially to avoid overloading the inference engine
        foreach (var agent in reviewers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Find this agent's own submission to exclude it
            // Use Role for matching instead of BaseAgentId, because the same base agent
            // can be reused across multiple research roles, but Role is unique per team
            var ownSubmission = anonymizedSubmissions.Values
                .FirstOrDefault(s => s.OriginalRole == agent.Role);

            _logger.LogDebug(
                "Peer review assignment: {ReviewerRole} will exclude submission {ExcludedId} (found={Found})",
                agent.Role,
                ownSubmission?.AnonymizedId ?? "none",
                ownSubmission != null);

            var submissionsToReview = anonymizedSubmissions
                .Where(kv => ownSubmission == null || kv.Value.OriginalSubmissionId != ownSubmission.OriginalSubmissionId)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            _logger.LogDebug(
                "Peer review assignment: {ReviewerRole} will review {Count} submissions: {Ids}",
                agent.Role,
                submissionsToReview.Count,
                string.Join(", ", submissionsToReview.Keys));

            // Conduct this agent's reviews sequentially
            var agentReviews = await ConductAgentReviewsAsync(
                session,
                agent,
                submissionsToReview,
                cancellationToken).ConfigureAwait(false);

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
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("[PEER-REVIEW] === ConductAgentReviewsAsync START ===");
        _logger.LogDebug("[PEER-REVIEW] Reviewer: {Role} ({RoleName})", reviewer.Role, reviewer.RoleName);
        _logger.LogDebug("[PEER-REVIEW] SessionId: {SessionId}", session.Id);
        _logger.LogDebug("[PEER-REVIEW] Submissions to review: {Count}", submissions.Count);

        var reviews = new List<PeerReview>();

        // Notify phase start via broadcaster with active agent info
        await _progressBroadcaster.BroadcastProgressAsync(
            session.Id,
            ResearchPhase.PeerReview,
            0,
            $"{reviewer.RoleName} is conducting peer reviews...",
            new List<Contracts.ActiveAgentInfo>
            {
                new()
                {
                    TeamMemberId = reviewer.TeamMemberId,
                    Role = reviewer.Role,
                    RoleName = reviewer.RoleName,
                    Activity = "Conducting peer reviews...",
                    AgentProgressPercent = null
                }
            }).ConfigureAwait(false);

        // Get chat service for this reviewer
        _logger.LogDebug("[PEER-REVIEW] Getting chat service for reviewer...");
        var chatService = GetChatServiceForAgent(reviewer);

        var completedCount = 0;
        foreach (var (anonymizedId, submission) in submissions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            PeerReviewScores scores;

            if (chatService != null)
            {
                // Use LLM to conduct peer review
                scores = await ConductLlmPeerReviewAsync(
                    session, reviewer, anonymizedId, submission, chatService, cancellationToken).ConfigureAwait(false);
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

            reviews.Add(review);
            completedCount++;

            _logger.LogDebug(
                "[PEER-REVIEW] {Reviewer} reviewed submission {AnonymizedId} with score {Score}",
                reviewer.Role, anonymizedId, review.WeightedAverage);

            // Report progress (consolidating per-review updates into unified progress broadcast)
            var percent = Math.Min(100, (int)((double)completedCount / submissions.Count * 100));
            await _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
            {
                Phase = ResearchPhase.PeerReview,
                AgentRole = reviewer.Role,
                TeamMemberId = reviewer.TeamMemberId,
                Message = $"{reviewer.RoleName} reviewed {completedCount}/{submissions.Count} submissions",
                PercentComplete = percent
            }).ConfigureAwait(false);
        }

        // NOTE: We don't send agent-level completion here because
        // PercentComplete=100 for a single reviewer would incorrectly set overall progress to 100%.
        // The phase-level progress (in ExecutePeerReviewPhaseAsync) handles overall progress.

        _logger.LogDebug("[PEER-REVIEW] === ConductAgentReviewsAsync COMPLETE ===");
        _logger.LogDebug("[PEER-REVIEW] Reviews generated: {Count}", reviews.Count);

        return reviews;
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

        // Create chat request
        var request = await CreatePeerReviewRequestAsync(reviewer, reviewPrompt);

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
    /// Creates a chat request for peer review.
    /// </summary>
    private async Task<AesirChatRequestBase> CreatePeerReviewRequestAsync(ResearchAgent reviewer, string userPrompt)
    {
        var systemMessage = new AesirChatMessage
        {
            Role = "system",
            Content = $"""
                You are {reviewer.RoleName}, an expert peer reviewer.
                Your role is to critically evaluate research submissions with fairness and objectivity.
                Provide constructive feedback that helps improve research quality.
                Score submissions honestly based on their actual merit.
                """,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var userMessage = new AesirChatMessage
        {
            Role = "user",
            Content = userPrompt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var conversation = new AesirConversation
        {
            Id = Guid.NewGuid().ToString(),
            Messages = [systemMessage, userMessage]
        };

        // Get thinking settings from base agent
        bool? enableThinking = null;
        ThinkValue? thinkValue = null;

        try
        {
            var baseAgent = await _configurationService.GetAgentAsync(reviewer.BaseAgentId);
            enableThinking = baseAgent.AllowThinking;
            thinkValue = baseAgent.ThinkValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get base agent settings for reviewer {Role}", reviewer.Role);
        }

        return new AesirChatRequestBase
        {
            Model = reviewer.Model ?? "gpt-4",
            Temperature = reviewer.Temperature,
            MaxTokens = reviewer.MaxTokens ?? 4096,
            Conversation = conversation,
            User = "research-peer-review",
            Title = $"Peer Review: {reviewer.RoleName}",
            Tools = [], // Peer review doesn't need tools
            EnableThinking = enableThinking,
            ThinkValue = thinkValue
        };
    }

    /// <summary>
    /// Resolves the IChatService for a research agent based on its inference engine ID.
    /// Uses the injected IServiceProvider to resolve keyed services directly.
    /// </summary>
    /// <returns>The chat service, or null if service cannot be resolved.</returns>
    private IChatService? GetChatServiceForAgent(ResearchAgent agent)
    {
        _logger.LogDebug("[PEER-REVIEW] GetChatServiceForAgent called for {Role}", agent.Role);

        if (!agent.InferenceEngineId.HasValue)
        {
            _logger.LogWarning("[PEER-REVIEW] Reviewer {Role} has no inference engine ID configured", agent.Role);
            return null;
        }

        var engineIdKey = agent.InferenceEngineId.Value.ToString();
        _logger.LogDebug("[PEER-REVIEW] Attempting to get keyed service IChatService with key: '{EngineIdKey}'", engineIdKey);

        var chatService = _serviceProvider.GetKeyedService<IChatService>(engineIdKey);

        if (chatService == null)
        {
            _logger.LogWarning("[PEER-REVIEW] No IChatService found for inference engine ID: {EngineId}", agent.InferenceEngineId);
            return null;
        }

        _logger.LogDebug("[PEER-REVIEW] SUCCESS: IChatService resolved: {ServiceType}", chatService.GetType().Name);
        return chatService;
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
