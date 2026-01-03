using System.Text.Json.Serialization;
using Aesir.Common.Models;
using Aesir.Infrastructure.Data;

namespace Aesir.Infrastructure.Models;

/// <summary>
/// Server-side project model with soft delete support.
/// </summary>
public class AesirProject : AesirProjectBase, IEntity
{
    /// <summary>
    /// Gets or sets whether the project has been soft deleted.
    /// </summary>
    [JsonPropertyName("is_deleted")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the project was deleted.
    /// </summary>
    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who deleted the project.
    /// </summary>
    [JsonPropertyName("deleted_by")]
    public string? DeletedBy { get; set; }
}
