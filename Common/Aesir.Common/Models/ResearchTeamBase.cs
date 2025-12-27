using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Base model for research team configuration.
/// </summary>
public class ResearchTeamBase
{
    /// <summary>
    /// Unique identifier for the research team.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// User who owns this research team.
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// Display name for the research team.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Optional description of the team configuration.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this team is active.
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Team member assignments with optional overrides.
    /// </summary>
    [JsonPropertyName("members")]
    public List<ResearchTeamMemberBase>? Members { get; set; }

    /// <summary>
    /// When the team was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// When the team was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Base model for a research team member assignment.
/// </summary>
public class ResearchTeamMemberBase
{
    /// <summary>
    /// Unique identifier for this team member assignment.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// The team this member belongs to.
    /// </summary>
    [JsonPropertyName("research_team_id")]
    public Guid? ResearchTeamId { get; set; }

    /// <summary>
    /// The research role assigned to this member.
    /// </summary>
    [JsonPropertyName("role")]
    public ResearchRoleBase Role { get; set; }

    /// <summary>
    /// Reference to the base agent providing infrastructure.
    /// </summary>
    [JsonPropertyName("agent_id")]
    public Guid AgentId { get; set; }

    /// <summary>
    /// Whether this team member is active.
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Override: Custom temperature for this role.
    /// </summary>
    [JsonPropertyName("override_temperature")]
    public double? OverrideTemperature { get; set; }

    /// <summary>
    /// Override: Custom persona/system prompt.
    /// </summary>
    [JsonPropertyName("override_persona")]
    public string? OverridePersona { get; set; }

    /// <summary>
    /// Override: Custom planning phase prompt.
    /// </summary>
    [JsonPropertyName("override_planning_prompt")]
    public string? OverridePlanningPrompt { get; set; }

    /// <summary>
    /// Override: Custom research phase prompt.
    /// </summary>
    [JsonPropertyName("override_research_prompt")]
    public string? OverrideResearchPrompt { get; set; }

    /// <summary>
    /// Override: Specific thinking mode.
    /// </summary>
    [JsonPropertyName("override_thinking_mode")]
    public string? OverrideThinkingMode { get; set; }

    /// <summary>
    /// Override: Specific tool IDs to use.
    /// </summary>
    [JsonPropertyName("override_tools")]
    public List<string>? OverrideTools { get; set; }
}

/// <summary>
/// Research role types.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResearchRoleBase
{
    /// <summary>
    /// Deep researcher with extended context.
    /// </summary>
    DeepDiver = 0,

    /// <summary>
    /// Pattern finder and connector.
    /// </summary>
    Synthesizer = 1,

    /// <summary>
    /// Critical analyzer and skeptic.
    /// </summary>
    DevilsAdvocate = 2,

    /// <summary>
    /// Final report compiler (optional in Quick mode).
    /// </summary>
    Chairman = 3
}

/// <summary>
/// Research mode options.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResearchModeBase
{
    /// <summary>
    /// Single round, no peer review.
    /// </summary>
    Quick = 0,

    /// <summary>
    /// Single round with peer review.
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Multiple rounds with iterative improvement.
    /// </summary>
    Deep = 2
}
