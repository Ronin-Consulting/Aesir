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
    public Guid Id { get; set; }

    /// <summary>
    /// The team this member belongs to.
    /// </summary>
    public Guid ResearchTeamId { get; set; }

    /// <summary>
    /// The research role assigned to this member.
    /// </summary>
    public ResearchRole Role { get; set; }

    /// <summary>
    /// Whether this team member assignment is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Reference to the base agent providing infrastructure (inference engine, model, tools).
    /// </summary>
    public Guid AgentId { get; set; }

    /// <summary>
    /// Override: Custom temperature for this role. Null = inherit from base agent.
    /// </summary>
    public double? OverrideTemperature { get; set; }

    /// <summary>
    /// Override: Custom persona/system prompt. Null = use role default.
    /// </summary>
    public string? OverridePersona { get; set; }

    /// <summary>
    /// Override: Custom planning phase prompt. Null = use role default.
    /// </summary>
    public string? OverridePlanningPrompt { get; set; }

    /// <summary>
    /// Override: Custom research phase prompt. Null = use role default.
    /// </summary>
    public string? OverrideResearchPrompt { get; set; }

    /// <summary>
    /// Override: Specific thinking mode (Low, Medium, High). Null = inherit from base agent.
    /// </summary>
    public string? OverrideThinkingMode { get; set; }

    /// <summary>
    /// Override: Specific tool IDs to use (subset of agent's tools). Null = inherit all from base agent.
    /// </summary>
    public List<string>? OverrideTools { get; set; }

    /// <summary>
    /// Navigation property: the parent team.
    /// </summary>
    public ResearchTeam? Team { get; set; }
}
