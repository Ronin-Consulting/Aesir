using System.Text.Json.Serialization;

namespace Aesir.Tools.LegalValidator.Models;

/// <summary>
/// Represents the evaluation result of an agent's response to a legal question.
/// </summary>
public class EvaluationResult
{
    /// <summary>
    /// The ID of the question that was evaluated.
    /// </summary>
    [JsonPropertyName("question_id")]
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the agent whose response was evaluated.
    /// </summary>
    [JsonPropertyName("agent_id")]
    public Guid AgentId { get; set; }

    /// <summary>
    /// The name of the agent whose response was evaluated.
    /// </summary>
    [JsonPropertyName("agent_name")]
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Overall letter grade for the response.
    /// </summary>
    [JsonPropertyName("overall_grade")]
    public OverallGrade OverallGrade { get; set; }

    /// <summary>
    /// Accuracy score (1-10) - legal correctness and citation accuracy.
    /// </summary>
    [JsonPropertyName("accuracy_score")]
    public int AccuracyScore { get; set; }

    /// <summary>
    /// Completeness score (1-10) - coverage of expected elements.
    /// </summary>
    [JsonPropertyName("completeness_score")]
    public int CompletenessScore { get; set; }

    /// <summary>
    /// Clarity score (1-10) - organization and readability.
    /// </summary>
    [JsonPropertyName("clarity_score")]
    public int ClarityScore { get; set; }

    /// <summary>
    /// Whether appropriate legal disclaimers are present.
    /// </summary>
    [JsonPropertyName("disclaimer_present")]
    public bool DisclaimerPresent { get; set; }

    /// <summary>
    /// Source quality score (1-10) - authority and relevance of citations.
    /// </summary>
    [JsonPropertyName("source_quality_score")]
    public int SourceQualityScore { get; set; }

    /// <summary>
    /// Issues identified in the response.
    /// </summary>
    [JsonPropertyName("issues_found")]
    public List<string> IssuesFound { get; set; } = [];

    /// <summary>
    /// Strengths identified in the response.
    /// </summary>
    [JsonPropertyName("strengths")]
    public List<string> Strengths { get; set; } = [];

    /// <summary>
    /// Additional notes from the evaluation.
    /// </summary>
    [JsonPropertyName("evaluation_notes")]
    public string EvaluationNotes { get; set; } = string.Empty;

    /// <summary>
    /// Converts the overall grade to a GPA value.
    /// </summary>
    [JsonIgnore]
    public double GpaValue => OverallGrade switch
    {
        OverallGrade.APlus => 4.3,
        OverallGrade.A => 4.0,
        OverallGrade.AMinus => 3.7,
        OverallGrade.BPlus => 3.3,
        OverallGrade.B => 3.0,
        OverallGrade.BMinus => 2.7,
        OverallGrade.CPlus => 2.3,
        OverallGrade.C => 2.0,
        OverallGrade.CMinus => 1.7,
        OverallGrade.D => 1.0,
        OverallGrade.F => 0.0,
        _ => 0.0
    };
}
