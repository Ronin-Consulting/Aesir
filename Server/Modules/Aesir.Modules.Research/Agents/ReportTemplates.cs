using System.Text;
using Aesir.Modules.Research.Models;

namespace Aesir.Modules.Research.Agents;

/// <summary>
/// Templates for generating markdown research reports.
/// </summary>
public static class ReportTemplates
{
    /// <summary>
    /// Generates the full markdown report from a ResearchReport entity.
    /// </summary>
    /// <param name="report">The research report.</param>
    /// <param name="session">The research session.</param>
    /// <returns>Complete markdown report.</returns>
    public static string GenerateFullMarkdown(ResearchReport report, ResearchSession session)
    {
        var sb = new StringBuilder();

        // Title
        sb.AppendLine($"# {report.Title}");
        sb.AppendLine();

        // Metadata header
        sb.AppendLine("---");
        sb.AppendLine($"**Research Query:** {session.Query}");
        if (!string.IsNullOrEmpty(session.RefinedQuery) && session.RefinedQuery != session.Query)
        {
            sb.AppendLine($"**Refined Query:** {session.RefinedQuery}");
        }
        sb.AppendLine($"**Generated:** {report.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Mode:** {session.Mode}");
        if (report.Metadata != null)
        {
            var duration = TimeSpan.FromMilliseconds(report.Metadata.TotalDurationMs);
            sb.AppendLine($"**Duration:** {FormatDuration(duration)}");
            sb.AppendLine($"**Sources:** {report.Metadata.SourceCount}");
        }
        sb.AppendLine("---");
        sb.AppendLine();

        // Executive Summary
        sb.AppendLine("## Executive Summary");
        sb.AppendLine();
        sb.AppendLine(report.ExecutiveSummary);
        sb.AppendLine();

        // Methodology
        if (!string.IsNullOrEmpty(report.MethodologySection))
        {
            sb.AppendLine("## Methodology");
            sb.AppendLine();
            sb.AppendLine(report.MethodologySection);
            sb.AppendLine();
        }

        // Key Findings
        if (report.Findings?.Count > 0)
        {
            sb.AppendLine("## Key Findings");
            sb.AppendLine();

            foreach (var finding in report.Findings)
            {
                sb.AppendLine($"### {finding.Title}");
                sb.AppendLine();
                sb.AppendLine($"**Confidence:** {GetConfidenceBadge(finding.Confidence)}");
                sb.AppendLine();
                sb.AppendLine(finding.Content);
                sb.AppendLine();

                if (finding.SupportingEvidence?.Count > 0)
                {
                    sb.AppendLine("**Supporting Evidence:**");
                    foreach (var evidence in finding.SupportingEvidence)
                    {
                        sb.AppendLine($"- {evidence}");
                    }
                    sb.AppendLine();
                }

                if (finding.ContributingRoles?.Count > 0)
                {
                    var roles = string.Join(", ", finding.ContributingRoles.Select(r => r.ToString()));
                    sb.AppendLine($"*Contributing perspectives: {roles}*");
                    sb.AppendLine();
                }
            }
        }

        // Alternative Perspectives
        if (!string.IsNullOrEmpty(report.AlternativePerspectives))
        {
            sb.AppendLine("## Alternative Perspectives");
            sb.AppendLine();
            sb.AppendLine(report.AlternativePerspectives);
            sb.AppendLine();
        }

        // Research Gaps
        if (!string.IsNullOrEmpty(report.ResearchGaps))
        {
            sb.AppendLine("## Research Gaps & Future Directions");
            sb.AppendLine();
            sb.AppendLine(report.ResearchGaps);
            sb.AppendLine();
        }

        // Bibliography
        if (report.Bibliography?.Count > 0)
        {
            sb.AppendLine("## Bibliography");
            sb.AppendLine();

            var sourceNumber = 1;
            foreach (var source in report.Bibliography)
            {
                if (!string.IsNullOrEmpty(source.Url))
                {
                    sb.AppendLine($"{sourceNumber}. [{source.Title}]({source.Url})");
                }
                else
                {
                    sb.AppendLine($"{sourceNumber}. {source.Title}");
                }
                sourceNumber++;
            }
            sb.AppendLine();
        }

        // Research Statistics (collapsible)
        if (report.Metadata != null)
        {
            sb.AppendLine("<details>");
            sb.AppendLine("<summary>Research Statistics</summary>");
            sb.AppendLine();
            sb.AppendLine("| Metric | Value |");
            sb.AppendLine("|--------|-------|");
            sb.AppendLine($"| Total Duration | {FormatDuration(TimeSpan.FromMilliseconds(report.Metadata.TotalDurationMs))} |");
            sb.AppendLine($"| Total Tokens | {report.Metadata.TotalTokensUsed:N0} |");
            sb.AppendLine($"| Sources Cited | {report.Metadata.SourceCount} |");
            sb.AppendLine($"| Key Findings | {report.Metadata.FindingCount} |");
            sb.AppendLine($"| Avg Peer Score | {report.Metadata.AveragePeerReviewScore:F1}/10 |");

            if (report.Metadata.AgentStatistics?.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("### Agent Contributions");
                sb.AppendLine();
                sb.AppendLine("| Agent | Tokens | Sources | Peer Score |");
                sb.AppendLine("|-------|--------|---------|------------|");

                foreach (var (agentName, stats) in report.Metadata.AgentStatistics)
                {
                    sb.AppendLine($"| {stats.Role} | {stats.TokensUsed:N0} | {stats.SourcesCited} | {stats.AverageReviewScore:F1} |");
                }
            }

            sb.AppendLine();
            sb.AppendLine("</details>");
            sb.AppendLine();
        }

        // Footer
        sb.AppendLine("---");
        sb.AppendLine("*Generated by AESIR Research Mode*");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a compact summary for display in chat.
    /// </summary>
    /// <param name="report">The research report.</param>
    /// <returns>Compact markdown summary.</returns>
    public static string GenerateCompactSummary(ResearchReport report)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"## {report.Title}");
        sb.AppendLine();
        sb.AppendLine(report.ExecutiveSummary);
        sb.AppendLine();

        if (report.Findings?.Count > 0)
        {
            sb.AppendLine("### Key Findings");
            sb.AppendLine();

            foreach (var finding in report.Findings.Take(3))
            {
                sb.AppendLine($"- **{finding.Title}** ({GetConfidenceBadge(finding.Confidence)})");
            }

            if (report.Findings.Count > 3)
            {
                sb.AppendLine($"- *...and {report.Findings.Count - 3} more findings*");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets a confidence badge string.
    /// </summary>
    private static string GetConfidenceBadge(ConfidenceLevel confidence)
    {
        return confidence switch
        {
            ConfidenceLevel.High => "High Confidence",
            ConfidenceLevel.Medium => "Medium Confidence",
            ConfidenceLevel.Low => "Low Confidence",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Formats a duration for display.
    /// </summary>
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMinutes < 1)
        {
            return $"{duration.TotalSeconds:F0} seconds";
        }

        if (duration.TotalHours < 1)
        {
            return $"{duration.TotalMinutes:F0} minutes";
        }

        return $"{duration.TotalHours:F1} hours";
    }
}
