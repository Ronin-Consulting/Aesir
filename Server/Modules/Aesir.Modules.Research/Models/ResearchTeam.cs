using Aesir.Infrastructure.Data;

namespace Aesir.Modules.Research.Models;

/// <summary>
/// Represents a configured research team.
/// Teams are created in Settings and referenced when starting research.
/// </summary>
public class ResearchTeam : IEntity
{
    /// <summary>
    /// Unique identifier for the team.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User who owns this team configuration.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the team (e.g., "Legal Research Team").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the team's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the team is active and can be selected.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the team was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the team was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property: team members with their role assignments.
    /// </summary>
    public List<ResearchTeamMember>? Members { get; set; }
}
