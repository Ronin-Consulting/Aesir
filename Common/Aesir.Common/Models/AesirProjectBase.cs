using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Base class for projects containing common properties shared between client and server.
/// </summary>
public class AesirProjectBase
{
    /// <summary>
    /// Gets or sets the unique identifier for the project.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who owns the project.
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the project. Must be globally unique.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the project.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the project-specific instructions that will be appended to the agent's persona prompt.
    /// </summary>
    [JsonPropertyName("instructions")]
    public string? Instructions { get; set; }

    /// <summary>
    /// Gets or sets whether the project is active.
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp when the project was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the project.
    /// </summary>
    [JsonPropertyName("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the project was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last updated the project.
    /// </summary>
    [JsonPropertyName("updated_by")]
    public string? UpdatedBy { get; set; }

    protected bool Equals(AesirProjectBase other)
    {
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((AesirProjectBase)obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
