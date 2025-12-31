using System.Text.Json.Serialization;

namespace Aesir.Tools.LegalValidator.Models;

/// <summary>
/// Represents a legal question used for validation testing.
/// </summary>
public class LegalQuestion
{
    /// <summary>
    /// Unique identifier for the question (e.g., "FD001", "PR002").
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Category of the legal question.
    /// </summary>
    [JsonPropertyName("category")]
    public QuestionCategory Category { get; set; }

    /// <summary>
    /// The question text.
    /// </summary>
    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Expected elements that should be present in a correct answer.
    /// </summary>
    [JsonPropertyName("expected_elements")]
    public List<string> ExpectedElements { get; set; } = [];

    /// <summary>
    /// Difficulty level of the question.
    /// </summary>
    [JsonPropertyName("difficulty")]
    public DifficultyLevel Difficulty { get; set; }
}
