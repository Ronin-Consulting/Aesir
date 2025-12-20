using Aesir.Client.Web.Infrastructure.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service for managing document uploads and retrieval for conversations.
/// </summary>
public interface IDocumentApiService
{
    /// <summary>
    /// Maximum allowed file size for uploads (100MB).
    /// </summary>
    const long MaxFileSize = 104857600;

    /// <summary>
    /// Supported file extensions for upload.
    /// </summary>
    static readonly string[] SupportedExtensions =
    [
        ".pdf", ".txt", ".md", ".json", ".xml", ".csv",
        ".png", ".jpg", ".jpeg", ".gif", ".webp"
    ];

    /// <summary>
    /// Uploads a file to a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID to attach the file to.</param>
    /// <param name="file">The browser file to upload.</param>
    /// <param name="progress">Optional progress callback (0-100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The upload response with file details.</returns>
    Task<FileUploadResponse> UploadFileAsync(
        Guid conversationId,
        IBrowserFile file,
        IProgress<int>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all files attached to a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of files attached to the conversation.</returns>
    Task<IReadOnlyList<ConversationFile>> GetFilesAsync(
        Guid conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all files for the current user's conversations.
    /// Server-side filtering ensures only the user's documents are returned.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of files from the user's conversations.</returns>
    Task<IReadOnlyList<ConversationFile>> GetAllFilesAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes a file from a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="filename">The filename to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deletion was successful.</returns>
    Task<bool> DeleteFileAsync(
        Guid conversationId,
        string filename,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the download URL for a conversation file.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="filename">The filename to download.</param>
    /// <returns>The URL to download the file.</returns>
    string GetDownloadUrl(Guid conversationId, string filename);

    /// <summary>
    /// Gets the thumbnail URL for a conversation file.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="filename">The filename to get the thumbnail for.</param>
    /// <returns>The URL to retrieve the thumbnail.</returns>
    string GetThumbnailUrl(Guid conversationId, string filename);

    /// <summary>
    /// Checks if a file extension is supported for upload.
    /// </summary>
    /// <param name="filename">The filename to check.</param>
    /// <returns>True if the file type is supported.</returns>
    bool IsFileTypeSupported(string filename);

    /// <summary>
    /// Checks if a file size is within the allowed limit.
    /// </summary>
    /// <param name="fileSize">The file size in bytes.</param>
    /// <returns>True if the file size is within limits.</returns>
    bool IsFileSizeValid(long fileSize);

    /// <summary>
    /// Moves all files from one conversation to another.
    /// Used when the session ID is assigned after files were uploaded.
    /// </summary>
    /// <param name="sourceConversationId">The source conversation ID.</param>
    /// <param name="targetConversationId">The target conversation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the migration was successful.</returns>
    Task<bool> MoveFilesAsync(
        Guid sourceConversationId,
        Guid targetConversationId,
        CancellationToken ct = default);

    #region Citation Viewer Methods

    /// <summary>
    /// Gets metadata about a citation file.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="filename">The filename.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>File metadata including size, mime type, and page count.</returns>
    Task<CitationFileMetadata?> GetCitationMetadataAsync(
        string conversationId,
        string filename,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the file content as a byte array.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="filename">The filename.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The file content as bytes, or null if not found.</returns>
    Task<byte[]?> GetCitationContentAsync(
        string conversationId,
        string filename,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the URL for viewing/downloading a citation file.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="filename">The filename.</param>
    /// <returns>The URL to access the file content.</returns>
    string GetCitationViewUrl(string conversationId, string filename);

    /// <summary>
    /// Gets the file content as a data URL (base64 encoded).
    /// Useful for embedding images and PDFs directly in the page.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="filename">The filename.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A data URL (e.g., "data:application/pdf;base64,..."), or null if not found.</returns>
    Task<string?> GetCitationDataUrlAsync(
        string conversationId,
        string filename,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a citation file exists.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="filename">The filename.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    Task<bool> CitationExistsAsync(
        string conversationId,
        string filename,
        CancellationToken ct = default);

    #endregion
}
