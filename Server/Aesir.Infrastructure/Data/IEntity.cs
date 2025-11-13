namespace Aesir.Infrastructure.Data;

/// <summary>
/// Base interface for all entities in the AESIR system.
/// All entities must have a Guid identifier as the primary key.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity.
    /// Uses Guid to ensure distributed system compatibility and avoid auto-increment issues.
    /// </summary>
    Guid Id { get; set; }
}
