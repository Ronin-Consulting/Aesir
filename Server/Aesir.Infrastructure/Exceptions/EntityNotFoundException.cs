namespace Aesir.Infrastructure.Exceptions;

/// <summary>
/// Exception thrown when an entity is not found in the database.
/// This maps to HTTP 404 Not Found responses.
/// </summary>
public class EntityNotFoundException : Exception
{
    /// <summary>
    /// Gets the type of entity that was not found (e.g., "Agent", "InferenceEngine").
    /// </summary>
    public string? EntityType { get; }

    /// <summary>
    /// Gets the identifier of the entity that was not found.
    /// </summary>
    public Guid? EntityId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public EntityNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with entity details.
    /// </summary>
    /// <param name="entityType">The type of entity that was not found.</param>
    /// <param name="entityId">The identifier of the entity that was not found.</param>
    public EntityNotFoundException(string entityType, Guid entityId)
        : base($"{entityType} with ID {entityId} not found")
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public EntityNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
