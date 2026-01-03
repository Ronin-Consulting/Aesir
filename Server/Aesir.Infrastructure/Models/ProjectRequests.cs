using System.Text.Json.Serialization;

namespace Aesir.Infrastructure.Models;

/// <summary>
/// Request model for creating a new project.
/// </summary>
public class CreateProjectRequest
{
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
}

/// <summary>
/// Request model for updating an existing project.
/// </summary>
public class UpdateProjectRequest
{
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
}

/// <summary>
/// Request model for deleting a project.
/// </summary>
public class DeleteProjectRequest
{
    /// <summary>
    /// Gets or sets whether to delete associated conversations.
    /// If false, conversations will be orphaned (project_id set to null).
    /// </summary>
    [JsonPropertyName("delete_conversations")]
    public bool DeleteConversations { get; set; }
}
