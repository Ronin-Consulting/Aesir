using System.Text.Json.Serialization;
using Aesir.Infrastructure.Data;

namespace Aesir.Modules.Research.Models;

/// <summary>
/// Represents a team member assignment with optional behavior overrides.
/// Each member references a base agent and can override research-specific settings.
/// </summary>
public class ResearchTeamMember : IEntity
{
    /// <summary>
    /// Unique identifier for this team member assignment.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The team this member belongs to.
    /// </summary>
    [JsonPropertyName("research_team_id")]
    public Guid ResearchTeamId { get; set; }

    /// <summary>
    /// The research role assigned to this member.
    /// </summary>
    [JsonPropertyName("role")]
    public ResearchRole Role { get; set; }

    /// <summary>
    /// Whether this team member assignment is active.
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Reference to the base agent providing infrastructure (inference engine, model, tools).
    /// </summary>
    [JsonPropertyName("agent_id")]
    public Guid AgentId { get; set; }

    /// <summary>
    /// Override: Custom temperature/creativity for this role. Null = inherit from base agent.
    /// </summary>
    [JsonPropertyName("override_temperature")]
    public double? OverrideTemperature { get; set; }

    /// <summary>
    /// Navigation property: the parent team.
    /// </summary>
    [JsonIgnore]
    public ResearchTeam? Team { get; set; }
}
