using System.Text;
using System.Text.Json;
using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Constants;
using Aesir.Modules.Research.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Service for generating research reports from session data.
/// </summary>
public interface IReportGeneratorService
{
    /// <summary>
    /// Generates a complete research report from a session.
    /// </summary>
    /// <param name="session">The research session with submissions and reviews.</param>
    /// <param name="submissionScores">Calculated submission scores.</param>
    /// <param name="chairmanAgent">The Chairman agent for synthesis.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated research report.</returns>
    Task<ResearchReport> GenerateReportAsync(
        ResearchSession session,
        IReadOnlyList<SubmissionScore> submissionScores,
        ResearchAgent chairmanAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds report metadata from session data.
    /// </summary>
    /// <param name="session">The research session.</param>
    /// <param name="submissionScores">Calculated submission scores.</param>
    /// <returns>Report metadata.</returns>
    ReportMetadata BuildMetadata(ResearchSession session, IReadOnlyList<SubmissionScore> submissionScores);
}

/// <summary>
/// Implementation of report generator service.
/// Uses non-streaming IChatService for LLM-based report synthesis.
/// </summary>
public class ReportGeneratorService : IReportGeneratorService
{
    private readonly ILogger<ReportGeneratorService> _logger;
    private readonly IChatServiceResolver _chatServiceResolver;
    private readonly IChatRequestBuilder _chatRequestBuilder;
    private readonly IConfidenceCalculator _confidenceCalculator;
    private readonly IScoringCalculator _scoringCalculator;
    private readonly IResearchProgressBroadcaster _progressBroadcaster;

    public ReportGeneratorService(
        ILogger<ReportGeneratorService> logger,
        IChatServiceResolver chatServiceResolver,
        IChatRequestBuilder chatRequestBuilder,
        IConfidenceCalculator confidenceCalculator,
        IScoringCalculator scoringCalculator,
        IResearchProgressBroadcaster progressBroadcaster)
    {
        _logger = logger;
        _chatServiceResolver = chatServiceResolver;
        _chatRequestBuilder = chatRequestBuilder;
        _confidenceCalculator = confidenceCalculator;
        _scoringCalculator = scoringCalculator;
        _progressBroadcaster = progressBroadcaster;
    }

    /// <inheritdoc />
    public async Task<ResearchReport> GenerateReportAsync(
        ResearchSession session,
        IReadOnlyList<SubmissionScore> submissionScores,
        ResearchAgent chairmanAgent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[REPORT-GEN] === GenerateReportAsync START ===");
        _logger.LogDebug("[REPORT-GEN] SessionId: {SessionId}", session.Id);
        _logger.LogDebug("[REPORT-GEN] Chairman: {Role} ({RoleName})", chairmanAgent.Role, chairmanAgent.RoleName);
        _logger.LogDebug("[REPORT-GEN] Chairman InferenceEngineId: {EngineId}", chairmanAgent.InferenceEngineId);
        _logger.LogDebug("[REPORT-GEN] Chairman BaseAgentId: {BaseAgentId}", chairmanAgent.BaseAgentId);
        _logger.LogInformation("Generating report for session {SessionId} (non-streaming)", session.Id);

        var submissions = session.Submissions ?? [];
        var peerReviews = session.PeerReviews ?? [];
        _logger.LogDebug("[REPORT-GEN] Submissions: {Count}, PeerReviews: {Count}", submissions.Count, peerReviews.Count);

        // Notify phase change via unified progress broadcaster with active agent info
        _logger.LogDebug("[REPORT-GEN] Broadcasting synthesis phase progress...");
        await _progressBroadcaster.BroadcastProgressAsync(
            session.Id,
            ResearchPhase.Synthesis,
            ResearchProgressMilestones.PhaseStart,
            "Chairman is synthesizing the final report...",
            new List<Contracts.ActiveAgentInfo>
            {
                new()
                {
                    TeamMemberId = chairmanAgent.TeamMemberId,
                    Role = chairmanAgent.Role,
                    RoleName = chairmanAgent.RoleName,
                    Activity = "Synthesizing final report...",
                    AgentProgressPercent = null
                }
            }).ConfigureAwait(false);

        // Report initial progress
        await _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
        {
            Phase = ResearchPhase.Synthesis,
            AgentRole = chairmanAgent.Role,
            TeamMemberId = chairmanAgent.TeamMemberId,
            Message = "Preparing synthesis prompt...",
            PercentComplete = ResearchProgressMilestones.PhaseInitializing
        }).ConfigureAwait(false);

        // Get chat service for the Chairman
        _logger.LogDebug("[REPORT-GEN] Getting chat service for Chairman...");
        var chatService = _chatServiceResolver.GetChatService(chairmanAgent.InferenceEngineId);

        string executiveSummary;
        string? title;

        if (chatService != null)
        {
            _logger.LogDebug("[REPORT-GEN] Chat service available: {ServiceType}", chatService.GetType().Name);
            _logger.LogDebug("[REPORT-GEN] Using LLM to synthesize the report...");
            // Use LLM to synthesize the report
            (executiveSummary, title) = await SynthesizeReportWithLlmAsync(
                session, submissions, submissionScores, peerReviews,
                chairmanAgent, chatService, cancellationToken);
            _logger.LogDebug("[REPORT-GEN] LLM synthesis complete. Title: {Title}, SummaryLength: {Length}",
                title, executiveSummary?.Length ?? 0);
        }
        else
        {
            // Fallback to stub implementation
            _logger.LogWarning("[REPORT-GEN] No chat service available for Chairman!");
            _logger.LogWarning("[REPORT-GEN] Chairman InferenceEngineId: {EngineId}", chairmanAgent.InferenceEngineId);
            _logger.LogWarning("[REPORT-GEN] Using STUB synthesis - report will not contain real LLM content!");
            executiveSummary = GenerateStubExecutiveSummary(session, submissions, submissionScores);
            title = GenerateTitle(session.Query);
        }

        // Build report from submissions
        var report = new ResearchReport
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Title = title ?? GenerateTitle(session.Query),
            ExecutiveSummary = executiveSummary,
            MethodologySection = GenerateMethodology(session, submissions),
            Findings = GenerateFindings(submissions, peerReviews, submissionScores),
            AlternativePerspectives = ExtractAlternativePerspectives(submissions),
            ResearchGaps = ExtractResearchGaps(submissions),
            Bibliography = CollectBibliography(submissions),
            Metadata = BuildMetadata(session, submissionScores),
            CreatedAt = DateTime.UtcNow
        };

        // Generate full markdown
        report.FullMarkdown = ReportTemplates.GenerateFullMarkdown(report, session);

        // Report final progress
        await _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
        {
            Phase = ResearchPhase.Synthesis,
            AgentRole = chairmanAgent.Role,
            TeamMemberId = chairmanAgent.TeamMemberId,
            Message = "Report generated successfully",
            PercentComplete = ResearchProgressMilestones.PhaseComplete
        }).ConfigureAwait(false);

        // NOTE: ResearchCompleted event is broadcast by ResearchOrchestrator after this method returns.
        // This ensures all lifecycle events are managed by the orchestrator.

        _logger.LogInformation("Report generated with {FindingCount} findings", report.Findings?.Count ?? 0);

        return report;
    }

    /// <summary>
    /// Uses non-streaming LLM to synthesize the final research report.
    /// </summary>
    private async Task<(string ExecutiveSummary, string? Title)> SynthesizeReportWithLlmAsync(
        ResearchSession session,
        IReadOnlyList<ResearchSubmission> submissions,
        IReadOnlyList<SubmissionScore> submissionScores,
        IReadOnlyList<PeerReview> peerReviews,
        ResearchAgent chairmanAgent,
        IChatService chatService,
        CancellationToken cancellationToken)
    {
        // Build the synthesis prompt
        var synthesisPrompt = BuildSynthesisPrompt(session, submissions, submissionScores, peerReviews);

        // Report progress after prompt construction
        await _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
        {
            Phase = ResearchPhase.Synthesis,
            AgentRole = chairmanAgent.Role,
            TeamMemberId = chairmanAgent.TeamMemberId,
            Message = "Building synthesis request...",
            PercentComplete = ResearchProgressMilestones.LlmCallStarted
        }).ConfigureAwait(false);

        // Create chat request using the builder
        var systemPrompt = chairmanAgent.SynthesisPrompt ?? """
            You are the Chairman of a research team, responsible for synthesizing all research findings
            into a cohesive, professional report. You have access to all team members' work and their
            peer review scores. Your role is to:
            1. Weigh findings by quality (higher peer review scores = more weight)
            2. Integrate diverse perspectives fairly
            3. Highlight consensus and resolve contradictions
            4. Provide clear, actionable conclusions
            5. Maintain objectivity and acknowledge limitations
            """;

        var request = await _chatRequestBuilder.BuildAsync(
            chairmanAgent,
            systemPrompt,
            synthesisPrompt,
            new ChatRequestOptions
            {
                IncludeTools = true,
                User = "research-synthesis",
                Title = "Research Report Synthesis",
                ChatSessionId = session.ChatSessionId
            }).ConfigureAwait(false);

        _logger.LogDebug("Sending synthesis request to LLM for Chairman (non-streaming)");

        // Report progress before LLM call
        await _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
        {
            Phase = ResearchPhase.Synthesis,
            AgentRole = chairmanAgent.Role,
            TeamMemberId = chairmanAgent.TeamMemberId,
            Message = "Chairman generating executive summary...",
            PercentComplete = ResearchProgressMilestones.LlmProcessing
        }).ConfigureAwait(false);

        // Execute non-streaming chat completion
        // Note: CancellationToken is checked before call; ChatCompletionsAsync doesn't accept one
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("[REPORT-GEN] Calling ChatCompletionsAsync with model: {Model}", request.Model);
        var result = await chatService.ChatCompletionsAsync(request).ConfigureAwait(false);

        // Log detailed result information for debugging
        _logger.LogDebug("[REPORT-GEN] ChatCompletionsAsync returned:");
        _logger.LogDebug("[REPORT-GEN]   TotalTokens: {TotalTokens}, PromptTokens: {PromptTokens}, CompletionTokens: {CompletionTokens}",
            result.TotalTokens, result.PromptTokens, result.CompletionTokens);
        _logger.LogDebug("[REPORT-GEN]   Conversation is null: {IsNull}", result.AesirConversation == null);
        if (result.AesirConversation != null)
        {
            _logger.LogDebug("[REPORT-GEN]   Message count: {Count}", result.AesirConversation.Messages?.Count ?? 0);
            if (result.AesirConversation.Messages != null)
            {
                foreach (var msg in result.AesirConversation.Messages)
                {
                    _logger.LogDebug("[REPORT-GEN]   Message - Role: {Role}, ContentLength: {Length}",
                        msg.Role, msg.Content?.Length ?? 0);
                }
            }
        }

        // Report progress after LLM response
        await _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
        {
            Phase = ResearchPhase.Synthesis,
            AgentRole = chairmanAgent.Role,
            TeamMemberId = chairmanAgent.TeamMemberId,
            Message = "Processing synthesis results...",
            PercentComplete = ResearchProgressMilestones.LlmCallCompleted
        }).ConfigureAwait(false);

        // Extract the assistant response from the result
        var response = ExtractAssistantResponse(result);

        _logger.LogDebug("[REPORT-GEN] Chairman synthesis complete, response length: {Length}", response.Length);
        if (response.Length == 0)
        {
            _logger.LogWarning("[REPORT-GEN] WARNING: Chairman synthesis returned empty response!");
            _logger.LogWarning("[REPORT-GEN]   This may indicate the LLM didn't respond or there was an API error.");
        }

        // Parse the response - extract title if present
        var title = ExtractTitle(response);
        var executiveSummary = ExtractExecutiveSummary(response);

        return (executiveSummary ?? response, title);
    }

    /// <summary>
    /// Extracts the assistant's response content from a chat completion result.
    /// </summary>
    private static string ExtractAssistantResponse(AesirChatResult result)
    {
        var assistantMessage = result.AesirConversation?.Messages?
            .LastOrDefault(m => m.Role == "assistant");
        return assistantMessage?.Content ?? string.Empty;
    }

    /// <summary>
    /// Builds the synthesis prompt for the Chairman.
    /// </summary>
    private static string BuildSynthesisPrompt(
        ResearchSession session,
        IReadOnlyList<ResearchSubmission> submissions,
        IReadOnlyList<SubmissionScore> submissionScores,
        IReadOnlyList<PeerReview> peerReviews)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Research Synthesis Task");
        sb.AppendLine();
        sb.AppendLine("## Original Research Query");
        sb.AppendLine(session.Query);
        sb.AppendLine();

        sb.AppendLine("## Research Submissions");
        sb.AppendLine();
        foreach (var submission in submissions.Where(s => s.Role != ResearchRole.Chairman))
        {
            var score = submissionScores.FirstOrDefault(s => s.SubmissionId == submission.Id);
            sb.AppendLine($"### {submission.Role} (Peer Review Score: {score?.AverageScore:F1}/10)");
            sb.AppendLine();
            sb.AppendLine(submission.Content);
            sb.AppendLine();
        }

        sb.AppendLine("## Peer Review Summary");
        sb.AppendLine();
        foreach (var submission in submissions.Where(s => s.Role != ResearchRole.Chairman))
        {
            var reviews = peerReviews.Where(r => r.SubmissionId == submission.Id).ToList();
            if (reviews.Any())
            {
                sb.AppendLine($"### Reviews for {submission.Role}");
                foreach (var review in reviews)
                {
                    sb.AppendLine($"- {review.ReviewerRole}: Score {review.WeightedAverage:F1}/10");
                    if (!string.IsNullOrEmpty(review.Strengths))
                        sb.AppendLine($"  - Strengths: {review.Strengths}");
                    if (!string.IsNullOrEmpty(review.Improvements))
                        sb.AppendLine($"  - Improvements: {review.Improvements}");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Your Task");
        sb.AppendLine();
        sb.AppendLine("As the Chairman, synthesize all research findings into a cohesive executive summary.");
        sb.AppendLine();
        sb.AppendLine("Your response should include:");
        sb.AppendLine("1. **Title**: A concise title for this research report");
        sb.AppendLine("2. **Executive Summary**: A comprehensive synthesis that:");
        sb.AppendLine("   - Addresses the original research question");
        sb.AppendLine("   - Integrates key findings from all researchers");
        sb.AppendLine("   - Weighs findings based on peer review scores");
        sb.AppendLine("   - Highlights areas of consensus and disagreement");
        sb.AppendLine("   - States confidence level and limitations");
        sb.AppendLine("   - Provides actionable conclusions");
        sb.AppendLine();
        sb.AppendLine("Format your response as:");
        sb.AppendLine("```");
        sb.AppendLine("# [Title]");
        sb.AppendLine();
        sb.AppendLine("[Executive Summary content...]");
        sb.AppendLine("```");

        return sb.ToString();
    }

    /// <summary>
    /// Extracts the title from the LLM response.
    /// </summary>
    private static string? ExtractTitle(string response)
    {
        // Look for # Title pattern
        var lines = response.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("# ") && !trimmed.StartsWith("## "))
            {
                return trimmed[2..].Trim();
            }
        }
        return null;
    }

    /// <summary>
    /// Extracts the executive summary from the LLM response.
    /// </summary>
    private static string? ExtractExecutiveSummary(string response)
    {
        // Return everything after the title line
        var lines = response.Split('\n').ToList();
        var titleIndex = lines.FindIndex(l => l.Trim().StartsWith("# ") && !l.Trim().StartsWith("## "));

        if (titleIndex >= 0 && titleIndex < lines.Count - 1)
        {
            var summaryLines = lines.Skip(titleIndex + 1);
            return string.Join('\n', summaryLines).Trim();
        }

        return null;
    }

    /// <summary>
    /// Generates a stub executive summary when LLM is not available.
    /// </summary>
    private string GenerateStubExecutiveSummary(
        ResearchSession session,
        IReadOnlyList<ResearchSubmission> submissions,
        IReadOnlyList<SubmissionScore> scores)
    {
        var sb = new StringBuilder();

        sb.AppendLine("*Note: This is a fallback executive summary. LLM synthesis was not available.*");
        sb.AppendLine();

        sb.AppendLine($"This research investigated: **{session.Query}**");
        sb.AppendLine();

        if (submissions.Count > 0)
        {
            sb.AppendLine($"The research team of {submissions.Count} agents conducted parallel investigations, " +
                          $"each bringing their unique perspective:");
            sb.AppendLine();

            foreach (var submission in submissions)
            {
                var score = scores.FirstOrDefault(s => s.SubmissionId == submission.Id);
                var scoreStr = score != null ? $" (score: {score.AverageScore:F1}/10)" : "";
                sb.AppendLine($"- **{submission.Role}**: Contributed findings{scoreStr}");
            }
        }

        var overallConfidence = scores.Count > 0
            ? _confidenceCalculator.CalculateOverallConfidence(scores, session.PeerReviews ?? [])
            : ConfidenceLevel.Low;

        sb.AppendLine();
        sb.AppendLine($"**Overall Confidence Level:** {overallConfidence}");

        return sb.ToString();
    }

    /// <inheritdoc />
    public ReportMetadata BuildMetadata(ResearchSession session, IReadOnlyList<SubmissionScore> submissionScores)
    {
        var submissions = session.Submissions ?? [];
        var peerReviews = session.PeerReviews ?? [];

        var totalDuration = session.CompletedAt.HasValue && session.StartedAt.HasValue
            ? (long)(session.CompletedAt.Value - session.StartedAt.Value).TotalMilliseconds
            : 0;

        var totalTokens = submissions.Sum(s => s.TokensUsed ?? 0);
        var sourceCount = submissions.SelectMany(s => s.Sources ?? []).Distinct().Count();
        var avgReviewScore = peerReviews.Count > 0
            ? peerReviews.Average(r => r.WeightedAverage)
            : 0;

        var agentStats = new Dictionary<string, AgentStats>();
        foreach (var submission in submissions)
        {
            var roleKey = submission.Role.ToString();
            var reviewsForSubmission = peerReviews.Where(r => r.SubmissionId == submission.Id).ToList();
            var avgScore = reviewsForSubmission.Count > 0
                ? reviewsForSubmission.Average(r => r.WeightedAverage)
                : 0;

            agentStats[roleKey] = new AgentStats
            {
                Role = submission.Role,
                TokensUsed = submission.TokensUsed ?? 0,
                DurationMs = submission.DurationMs ?? 0,
                SourcesCited = submission.Sources?.Count ?? 0,
                ToolCallsMade = submission.ToolCalls?.Count ?? 0,
                AverageReviewScore = avgScore
            };
        }

        return new ReportMetadata
        {
            TotalDurationMs = totalDuration,
            TotalTokensUsed = totalTokens,
            SourceCount = sourceCount,
            FindingCount = 0, // Will be updated after findings are generated
            AveragePeerReviewScore = avgReviewScore,
            AgentStatistics = agentStats
        };
    }

    private static string GenerateTitle(string query)
    {
        // Simple title generation - will be replaced with LLM call
        var cleanQuery = query.Trim();
        if (cleanQuery.Length > 80)
        {
            cleanQuery = cleanQuery[..77] + "...";
        }

        // Capitalize first letter
        if (!string.IsNullOrEmpty(cleanQuery))
        {
            cleanQuery = char.ToUpper(cleanQuery[0]) + cleanQuery[1..];
        }

        return cleanQuery;
    }

    private static string GenerateMethodology(ResearchSession session, IReadOnlyList<ResearchSubmission> submissions)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"This research was conducted using AESIR's **{session.Mode} Mode** multi-agent research system.");
        sb.AppendLine();
        sb.AppendLine("### Research Team");
        sb.AppendLine();

        foreach (var submission in submissions)
        {
            var roleDescription = submission.Role switch
            {
                ResearchRole.DeepDiver => "Conducted exhaustive investigation into primary sources",
                ResearchRole.Synthesizer => "Identified patterns and connections across domains",
                ResearchRole.DevilsAdvocate => "Challenged assumptions and explored alternative hypotheses",
                ResearchRole.Chairman => "Synthesized findings and generated this report",
                _ => "Contributed to research"
            };

            sb.AppendLine($"- **{submission.Role}**  \n{roleDescription}");
        }

        sb.AppendLine();
        sb.AppendLine("### Process");
        sb.AppendLine();
        sb.AppendLine("1. Query refinement through clarification");
        sb.AppendLine("2. Parallel research by specialized agents");
        sb.AppendLine("3. Anonymized peer review with scoring");
        sb.AppendLine("4. Chairman synthesis of final report");

        return sb.ToString();
    }

    private List<ResearchFinding> GenerateFindings(
        IReadOnlyList<ResearchSubmission> submissions,
        IReadOnlyList<PeerReview> peerReviews,
        IReadOnlyList<SubmissionScore> scores)
    {
        var findings = new List<ResearchFinding>();

        // Extract findings from each submission (stub implementation)
        var findingNumber = 1;
        foreach (var submission in submissions.Where(s => s.Role != ResearchRole.Chairman))
        {
            var score = scores.FirstOrDefault(s => s.SubmissionId == submission.Id);
            var confidence = score != null
                ? _confidenceCalculator.ScoreToConfidence(score.AverageScore)
                : ConfidenceLevel.Medium;

            findings.Add(new ResearchFinding
            {
                Title = $"Finding {findingNumber}: {submission.Role} Perspective",
                Content = TruncateContent(submission.Content, 2000),
                Confidence = confidence,
                SupportingEvidence = submission.Sources?.Take(3).Select(s => s.Title).ToList() ?? [],
                Sources = submission.Sources?.Take(3).ToList(),
                ContributingRoles = [submission.Role]
            });

            findingNumber++;
        }

        return findings;
    }

    private static string? ExtractAlternativePerspectives(IReadOnlyList<ResearchSubmission> submissions)
    {
        var devilsAdvocate = submissions.FirstOrDefault(s => s.Role == ResearchRole.DevilsAdvocate);
        if (devilsAdvocate == null)
            return null;

        return $"The Devil's Advocate raised the following considerations:\n\n" +
               TruncateContent(devilsAdvocate.Content, 1500);
    }

    private static string? ExtractResearchGaps(IReadOnlyList<ResearchSubmission> submissions)
    {
        // Return null until LLM-based gap extraction is implemented
        // The UI will hide this section when null
        return null;
    }

    private static List<ResearchSource> CollectBibliography(IReadOnlyList<ResearchSubmission> submissions)
    {
        var allSources = submissions
            .SelectMany(s => s.Sources ?? [])
            .GroupBy(s => s.Title)
            .Select(g => g.First())
            .OrderBy(s => s.Title)
            .ToList();

        return allSources;
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content))
            return "[No content]";

        if (content.Length <= maxLength)
            return content;

        return content[..maxLength] + "...\n\n*[Content truncated]*";
    }
}
