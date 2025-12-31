using System.Text.Json.Serialization;

namespace Aesir.Tools.LegalValidator.Models;

/// <summary>
/// Summary statistics for an agent's validation results.
/// </summary>
public class AgentSummary
{
    /// <summary>
    /// The ID of the agent.
    /// </summary>
    [JsonPropertyName("agent_id")]
    public Guid AgentId { get; set; }

    /// <summary>
    /// The name of the agent.
    /// </summary>
    [JsonPropertyName("agent_name")]
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Total number of questions evaluated.
    /// </summary>
    [JsonPropertyName("total_questions")]
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Average GPA across all evaluations.
    /// </summary>
    [JsonPropertyName("average_gpa")]
    public double AverageGpa { get; set; }

    /// <summary>
    /// Average accuracy score (1-10).
    /// </summary>
    [JsonPropertyName("average_accuracy")]
    public double AverageAccuracy { get; set; }

    /// <summary>
    /// Average completeness score (1-10).
    /// </summary>
    [JsonPropertyName("average_completeness")]
    public double AverageCompleteness { get; set; }

    /// <summary>
    /// Average clarity score (1-10).
    /// </summary>
    [JsonPropertyName("average_clarity")]
    public double AverageClarity { get; set; }

    /// <summary>
    /// Average source quality score (1-10).
    /// </summary>
    [JsonPropertyName("average_source_quality")]
    public double AverageSourceQuality { get; set; }

    /// <summary>
    /// Percentage of responses that included proper disclaimers.
    /// </summary>
    [JsonPropertyName("disclaimer_rate")]
    public double DisclaimerRate { get; set; }

    /// <summary>
    /// Distribution of grades.
    /// </summary>
    [JsonPropertyName("grade_distribution")]
    public Dictionary<string, int> GradeDistribution { get; set; } = new();

    /// <summary>
    /// Number of questions that had errors.
    /// </summary>
    [JsonPropertyName("error_count")]
    public int ErrorCount { get; set; }

    /// <summary>
    /// Average response time in seconds.
    /// </summary>
    [JsonPropertyName("average_response_time_seconds")]
    public double AverageResponseTimeSeconds { get; set; }
}
