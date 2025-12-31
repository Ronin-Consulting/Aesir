using System.Text;
using System.Text.Json;
using Aesir.Tools.LegalValidator.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Tools.LegalValidator.Services;

/// <summary>
/// Implementation of report generation in multiple formats.
/// </summary>
public class ReportGenerator : IReportGenerator
{
    private readonly ILogger<ReportGenerator> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public ReportGenerator(ILogger<ReportGenerator> logger)
    {
        _logger = logger;
    }

    public Task<string> GenerateJsonReportAsync(ValidationReport report, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(report, JsonOptions);
        return Task.FromResult(json);
    }

    public Task<string> GenerateMarkdownReportAsync(ValidationReport report, CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Legal AI Validation Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {report.Timestamp:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Total Questions:** {report.TotalQuestions}");
        sb.AppendLine($"**Agents Tested:** {string.Join(", ", report.AgentsTested)}");
        sb.AppendLine($"**Duration:** {report.ValidationDurationSeconds:F2} seconds");
        sb.AppendLine();

        // Executive Summary
        sb.AppendLine("## Executive Summary");
        sb.AppendLine();
        sb.AppendLine("| Agent | GPA | Accuracy | Completeness | Clarity | Source Quality | Disclaimer Rate |");
        sb.AppendLine("|-------|-----|----------|--------------|---------|----------------|-----------------|");

        foreach (var summary in report.AgentSummaries.Values.OrderByDescending(s => s.AverageGpa))
        {
            sb.AppendLine($"| {summary.AgentName} | {summary.AverageGpa:F2} | {summary.AverageAccuracy:F1} | {summary.AverageCompleteness:F1} | {summary.AverageClarity:F1} | {summary.AverageSourceQuality:F1} | {summary.DisclaimerRate:F1}% |");
        }

        sb.AppendLine();

        // Grade Distribution per Agent
        sb.AppendLine("## Grade Distribution");
        sb.AppendLine();

        foreach (var summary in report.AgentSummaries.Values)
        {
            sb.AppendLine($"### {summary.AgentName}");
            sb.AppendLine();
            sb.AppendLine("| Grade | Count |");
            sb.AppendLine("|-------|-------|");

            foreach (var (grade, count) in summary.GradeDistribution.OrderByDescending(kv => kv.Key))
            {
                sb.AppendLine($"| {grade} | {count} |");
            }

            sb.AppendLine();
        }

        // Detailed Results
        sb.AppendLine("## Detailed Results by Agent");
        sb.AppendLine();

        foreach (var (agentId, results) in report.ResultsByAgent)
        {
            var agentName = report.AgentSummaries.TryGetValue(agentId, out var summary)
                ? summary.AgentName
                : agentId.ToString();

            sb.AppendLine($"### {agentName}");
            sb.AppendLine();
            sb.AppendLine("| Question | Category | Grade | Accuracy | Completeness | Issues |");
            sb.AppendLine("|----------|----------|-------|----------|--------------|--------|");

            foreach (var result in results.OrderBy(r => r.QuestionId))
            {
                var issues = result.IssuesFound.Count > 0
                    ? string.Join("; ", result.IssuesFound.Take(2))
                    : "-";

                sb.AppendLine($"| {result.QuestionId} | - | {GradeToString(result.OverallGrade)} | {result.AccuracyScore} | {result.CompletenessScore} | {issues} |");
            }

            sb.AppendLine();
        }

        // Prompt Adjustments
        if (report.PromptAdjustments.Count > 0)
        {
            sb.AppendLine("## Recommended Prompt Adjustments");
            sb.AppendLine();

            var adjustmentNum = 1;
            foreach (var adj in report.PromptAdjustments.OrderBy(a => PriorityOrder(a.Priority)))
            {
                sb.AppendLine($"### {adjustmentNum}. [{adj.Priority.ToUpperInvariant()}] {adj.Category}");
                sb.AppendLine();
                sb.AppendLine($"**Issue:** {adj.IssueDescription}");
                sb.AppendLine();
                sb.AppendLine($"**Current Behavior:** {adj.CurrentBehavior}");
                sb.AppendLine();
                sb.AppendLine($"**Desired Behavior:** {adj.DesiredBehavior}");
                sb.AppendLine();
                sb.AppendLine("**Suggested Addition:**");
                sb.AppendLine("```");
                sb.AppendLine(adj.SuggestedPromptAddition);
                sb.AppendLine("```");
                sb.AppendLine();

                if (adj.AffectedQuestionIds.Count > 0)
                {
                    sb.AppendLine($"**Affected Questions:** {string.Join(", ", adj.AffectedQuestionIds)}");
                    sb.AppendLine();
                }

                adjustmentNum++;
            }
        }

        return Task.FromResult(sb.ToString());
    }

    public Task<string> GenerateClaudeCodeInstructionsAsync(ValidationReport report, CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Claude Code Instructions for Legal AI Prompt Improvements");
        sb.AppendLine();
        sb.AppendLine($"Generated: {report.Timestamp:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("## Overview");
        sb.AppendLine();
        sb.AppendLine("The following changes are recommended based on legal validation testing.");
        sb.AppendLine("Apply these changes in priority order (critical first).");
        sb.AppendLine();

        var adjustmentNum = 1;
        foreach (var adj in report.PromptAdjustments.OrderBy(a => PriorityOrder(a.Priority)))
        {
            sb.AppendLine($"---");
            sb.AppendLine();
            sb.AppendLine($"## Adjustment {adjustmentNum}: {adj.Category} [{adj.Priority.ToUpperInvariant()}]");
            sb.AppendLine();
            sb.AppendLine("### Problem");
            sb.AppendLine(adj.IssueDescription);
            sb.AppendLine();
            sb.AppendLine("### Location");
            sb.AppendLine(adj.ImplementationInstructions);
            sb.AppendLine();
            sb.AppendLine("### Add This Content");
            sb.AppendLine("```");
            sb.AppendLine(adj.SuggestedPromptAddition);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("### Verification");
            sb.AppendLine($"After making this change, the model should: {adj.DesiredBehavior}");
            sb.AppendLine();

            adjustmentNum++;
        }

        return Task.FromResult(sb.ToString());
    }

    public AgentSummary CalculateAgentSummary(
        Guid agentId,
        string agentName,
        IReadOnlyList<EvaluationResult> results,
        IReadOnlyList<AgentResponse> responses)
    {
        var validResults = results.Where(r => r.AccuracyScore > 0).ToList();

        var gradeDistribution = validResults
            .GroupBy(r => GradeToString(r.OverallGrade))
            .ToDictionary(g => g.Key, g => g.Count());

        return new AgentSummary
        {
            AgentId = agentId,
            AgentName = agentName,
            TotalQuestions = results.Count,
            AverageGpa = validResults.Count > 0 ? validResults.Average(r => r.GpaValue) : 0,
            AverageAccuracy = validResults.Count > 0 ? validResults.Average(r => r.AccuracyScore) : 0,
            AverageCompleteness = validResults.Count > 0 ? validResults.Average(r => r.CompletenessScore) : 0,
            AverageClarity = validResults.Count > 0 ? validResults.Average(r => r.ClarityScore) : 0,
            AverageSourceQuality = validResults.Count > 0 ? validResults.Average(r => r.SourceQualityScore) : 0,
            DisclaimerRate = validResults.Count > 0
                ? validResults.Count(r => r.DisclaimerPresent) * 100.0 / validResults.Count
                : 0,
            GradeDistribution = gradeDistribution,
            ErrorCount = responses.Count(r => r.HasError),
            AverageResponseTimeSeconds = responses.Count > 0
                ? responses.Average(r => r.ResponseTimeSeconds)
                : 0
        };
    }

    private static string GradeToString(OverallGrade grade)
    {
        return grade switch
        {
            OverallGrade.APlus => "A+",
            OverallGrade.A => "A",
            OverallGrade.AMinus => "A-",
            OverallGrade.BPlus => "B+",
            OverallGrade.B => "B",
            OverallGrade.BMinus => "B-",
            OverallGrade.CPlus => "C+",
            OverallGrade.C => "C",
            OverallGrade.CMinus => "C-",
            OverallGrade.D => "D",
            OverallGrade.F => "F",
            _ => "?"
        };
    }

    private static int PriorityOrder(string priority)
    {
        return priority.ToLowerInvariant() switch
        {
            "critical" => 0,
            "high" => 1,
            "medium" => 2,
            "low" => 3,
            _ => 4
        };
    }
}
