using System.Text.Json.Serialization;

namespace Aesir.Client.Web.Infrastructure.Models;

/// <summary>
/// Represents a file attached to a conversation.
/// </summary>
public class ConversationFile
{
    /// <summary>
    /// Gets or sets the unique identifier for the file.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME type of the file.
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the file was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the file was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether a thumbnail is available for this file.
    /// </summary>
    [JsonPropertyName("hasThumbnail")]
    public bool HasThumbnail { get; set; }

    /// <summary>
    /// Gets the display name of the file (without the conversation path prefix).
    /// </summary>
    public string DisplayName => GetDisplayName();

    private string GetDisplayName()
    {
        // FileName format is "/{conversationId}/{filename}" - extract just the filename
        if (string.IsNullOrEmpty(FileName))
            return string.Empty;

        var lastSlashIndex = FileName.LastIndexOf('/');
        return lastSlashIndex >= 0 ? FileName[(lastSlashIndex + 1)..] : FileName;
    }

    /// <summary>
    /// Gets a human-readable file size.
    /// </summary>
    public string FormattedSize => FormatSize(FileSize);

    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        var order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Response from a successful file upload.
/// </summary>
public class FileUploadResponse
{
    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the uploaded file name.
    /// </summary>
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversation ID the file was uploaded to.
    /// </summary>
    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;
}
