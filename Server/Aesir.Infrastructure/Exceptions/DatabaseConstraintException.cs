namespace Aesir.Infrastructure.Exceptions;

/// <summary>
/// Abstract base exception for database constraint violations.
/// Provides structured information about constraint violations including
/// constraint name, entity name, and suggested user actions.
/// </summary>
public abstract class DatabaseConstraintException : Exception
{
    /// <summary>
    /// Gets the name of the database constraint that was violated.
    /// </summary>
    public string ConstraintName { get; }

    /// <summary>
    /// Gets the name of the entity involved in the constraint violation (e.g., agent name, engine name).
    /// </summary>
    public string? EntityName { get; }

    /// <summary>
    /// Gets the suggested action the user should take to resolve the constraint violation.
    /// </summary>
    public string? SuggestedAction { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseConstraintException"/> class.
    /// </summary>
    /// <param name="constraintName">The name of the violated constraint.</param>
    /// <param name="entityName">The name of the entity involved (optional).</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="suggestedAction">The suggested action to resolve the constraint violation (optional).</param>
    protected DatabaseConstraintException(
        string constraintName,
        string? entityName,
        string message,
        string? suggestedAction)
        : base(message)
    {
        ConstraintName = constraintName ?? throw new ArgumentNullException(nameof(constraintName));
        EntityName = entityName;
        SuggestedAction = suggestedAction;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseConstraintException"/> class with an inner exception.
    /// </summary>
    /// <param name="constraintName">The name of the violated constraint.</param>
    /// <param name="entityName">The name of the entity involved (optional).</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="suggestedAction">The suggested action to resolve the constraint violation (optional).</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    protected DatabaseConstraintException(
        string constraintName,
        string? entityName,
        string message,
        string? suggestedAction,
        Exception innerException)
        : base(message, innerException)
    {
        ConstraintName = constraintName ?? throw new ArgumentNullException(nameof(constraintName));
        EntityName = entityName;
        SuggestedAction = suggestedAction;
    }
}
