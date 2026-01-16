using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Represents a lightweight chat session item with basic metadata for listing purposes.
/// </summary>
public class AesirChatSessionItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the chat session.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who owns the chat session.
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the title of the chat session.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp when the chat session was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets whether the chat session is starred/pinned.
    /// </summary>
    [JsonPropertyName("is_starred")]
    public bool IsStarred { get; set; }

    /// <summary>
    /// Gets or sets whether the chat session has research in progress.
    /// </summary>
    [JsonPropertyName("has_research_in_progress")]
    public bool HasResearchInProgress { get; set; }

    public string UpdatedAtDisplay=> UpdatedAt.ToString("g");
}