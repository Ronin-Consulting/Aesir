using System.Text.Json.Serialization;
using Aesir.Infrastructure.Data;

namespace Aesir.Modules.Research.Models;

/// <summary>
/// An entry in the research audit trail.
/// Tracks all significant events during research for transparency and debugging.
/// </summary>
public class ResearchTrailEntry : IEntity
{
    /// <summary>
    /// Unique identifier for the trail entry.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The research session this entry belongs to.
    /// </summary>
    [JsonPropertyName("session_id")]
    public Guid SessionId { get; set; }

    /// <summary>
    /// The submission this entry relates to (if applicable).
    /// </summary>
    [JsonPropertyName("submission_id")]
    public Guid? SubmissionId { get; set; }

    /// <summary>
    /// Type of event.
    /// </summary>
    [JsonPropertyName("event_type")]
    public ResearchTrailEventType EventType { get; set; }

    /// <summary>
    /// Role of the agent involved (if applicable).
    /// </summary>
    [JsonPropertyName("agent_role")]
    public ResearchRole? AgentRole { get; set; }

    /// <summary>
    /// Human-readable description of the event.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Input data for the event (JSON).
    /// </summary>
    [JsonPropertyName("input_json")]
    public string? InputJson { get; set; }

    /// <summary>
    /// Output data from the event (JSON).
    /// </summary>
    [JsonPropertyName("output_json")]
    public string? OutputJson { get; set; }

    /// <summary>
    /// Duration of the event in milliseconds.
    /// </summary>
    [JsonPropertyName("duration_ms")]
    public long? DurationMs { get; set; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}
