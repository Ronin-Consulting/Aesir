namespace Aesir.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when a database operation violates a foreign key constraint.
/// This typically occurs when attempting to delete an entity that is referenced by other entities.
/// </summary>
public class ForeignKeyViolationException : DatabaseConstraintException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForeignKeyViolationException"/> class.
    /// </summary>
    /// <param name="constraintName">The name of the violated foreign key constraint (e.g., "FK_aesir_research_team_member_agent_id_aesir_agent_id").</param>
    /// <param name="entityName">The name of the entity that cannot be deleted (e.g., agent name, engine name).</param>
    /// <param name="message">A user-friendly error message explaining why the deletion failed.</param>
    /// <param name="suggestedAction">The action the user should take to resolve the issue (e.g., "Remove the agent from all research teams before deleting.").</param>
    public ForeignKeyViolationException(
        string constraintName,
        string? entityName,
        string message,
        string? suggestedAction)
        : base(constraintName, entityName, message, suggestedAction)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForeignKeyViolationException"/> class with an inner exception.
    /// </summary>
    /// <param name="constraintName">The name of the violated foreign key constraint.</param>
    /// <param name="entityName">The name of the entity that cannot be deleted.</param>
    /// <param name="message">A user-friendly error message explaining why the deletion failed.</param>
    /// <param name="suggestedAction">The action the user should take to resolve the issue.</param>
    /// <param name="innerException">The exception that is the cause of the current exception (typically an Npgsql.PostgresException).</param>
    public ForeignKeyViolationException(
        string constraintName,
        string? entityName,
        string message,
        string? suggestedAction,
        Exception innerException)
        : base(constraintName, entityName, message, suggestedAction, innerException)
    {
    }
}
