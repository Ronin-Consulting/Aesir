using System.Text;
using System.Text.Json;
using Aesir.Modules.Research.Agents;
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
/// Note: Full chat integration will be added when wiring to the inference module.
/// </summary>
public class ReportGeneratorService : IReportGeneratorService
{
    private readonly ILogger<ReportGeneratorService> _logger;
    private readonly IConfidenceCalculator _confidenceCalculator;
    private readonly IScoringCalculator _scoringCalculator;

    public ReportGeneratorService(
        ILogger<ReportGeneratorService> logger,
        IConfidenceCalculator confidenceCalculator,
        IScoringCalculator scoringCalculator)
    {
        _logger = logger;
        _confidenceCalculator = confidenceCalculator;
        _scoringCalculator = scoringCalculator;
    }

    /// <inheritdoc />
    public async Task<ResearchReport> GenerateReportAsync(
        ResearchSession session,
        IReadOnlyList<SubmissionScore> submissionScores,
        ResearchAgent chairmanAgent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating report for session {SessionId}", session.Id);

        // TODO: Integrate with IChatService when available
        // For now, create a stub report from the submission data

        var submissions = session.Submissions ?? [];
        var peerReviews = session.PeerReviews ?? [];

        // Build report from submissions
        var report = new ResearchReport
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Title = GenerateTitle(session.Query),
            ExecutiveSummary = GenerateExecutiveSummary(session, submissions, submissionScores),
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

        // Simulate processing
        await Task.Delay(100, cancellationToken);

        _logger.LogInformation("Report generated with {FindingCount} findings", report.Findings?.Count ?? 0);

        return report;
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

    private string GenerateExecutiveSummary(
        ResearchSession session,
        IReadOnlyList<ResearchSubmission> submissions,
        IReadOnlyList<SubmissionScore> scores)
    {
        var sb = new StringBuilder();

        sb.AppendLine("*Note: This is a stub executive summary. Full LLM synthesis pending integration.*");
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

            sb.AppendLine($"- **{submission.Role}**: {roleDescription}");
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
                Content = $"[Stub finding from {submission.Role}]\n\n{TruncateContent(submission.Content, 500)}",
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

        return $"*Note: Full alternative perspective extraction pending LLM integration.*\n\n" +
               $"The Devil's Advocate raised the following considerations:\n\n" +
               TruncateContent(devilsAdvocate.Content, 500);
    }

    private static string? ExtractResearchGaps(IReadOnlyList<ResearchSubmission> submissions)
    {
        // Stub implementation - will be replaced with LLM extraction
        return """
            *Note: Research gap extraction pending LLM integration.*

            Potential areas for further investigation:
            - Deeper analysis of primary sources
            - Cross-validation with additional experts
            - Temporal analysis of trends
            """;
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
