using System.Text.Json.Serialization;

namespace Aesir.Tools.LegalValidator.Models;

/// <summary>
/// Represents a response from an AESIR agent to a legal question.
/// </summary>
public class AgentResponse
{
    /// <summary>
    /// The ID of the agent that provided the response.
    /// </summary>
    [JsonPropertyName("agent_id")]
    public Guid AgentId { get; set; }

    /// <summary>
    /// The display name of the agent.
    /// </summary>
    [JsonPropertyName("agent_name")]
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the question that was asked.
    /// </summary>
    [JsonPropertyName("question_id")]
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>
    /// The original question text.
    /// </summary>
    [JsonPropertyName("question_text")]
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// The response content from the agent.
    /// </summary>
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Time taken to generate the response in seconds.
    /// </summary>
    [JsonPropertyName("response_time_seconds")]
    public double ResponseTimeSeconds { get; set; }

    /// <summary>
    /// Timestamp when the response was received.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Error message if the request failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Number of prompt tokens used.
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>
    /// Number of completion tokens generated.
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Indicates whether this response contains an error.
    /// </summary>
    [JsonIgnore]
    public bool HasError => !string.IsNullOrEmpty(Error);
}
