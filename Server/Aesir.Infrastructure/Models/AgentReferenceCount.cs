namespace Aesir.Infrastructure.Models;

/// <summary>
/// Data transfer object for counting agent references across research module tables.
/// Used to validate whether an agent can be safely deleted.
/// </summary>
public class AgentReferenceCount
{
    /// <summary>
    /// Gets or sets the number of research team memberships for this agent.
    /// Maps to aesir_research_team_member.agent_id references.
    /// </summary>
    public int TeamMemberCount { get; set; }

    /// <summary>
    /// Gets or sets the number of research submissions by this agent.
    /// Maps to aesir_research_submission.agent_id references.
    /// </summary>
    public int SubmissionCount { get; set; }

    /// <summary>
    /// Gets or sets the number of peer reviews authored by this agent.
    /// Maps to aesir_research_peer_review.reviewer_agent_id references.
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// Gets a value indicating whether the agent has any references that would prevent deletion.
    /// </summary>
    public bool HasReferences => TeamMemberCount > 0 || SubmissionCount > 0 || ReviewCount > 0;
}
