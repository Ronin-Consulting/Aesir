using Aesir.Client.Web.Infrastructure.Models;
using Aesir.Client.Web.Modules.Chat.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing the documents view, providing all documents across conversations
/// with their associated chat metadata.
/// </summary>
public interface IDocumentsViewService
{
    /// <summary>
    /// Gets the current list of documents with chat info.
    /// </summary>
    IReadOnlyList<DocumentWithChatInfo> Documents { get; }

    /// <summary>
    /// Gets whether documents are currently being loaded.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets the current search term.
    /// </summary>
    string? SearchTerm { get; }

    /// <summary>
    /// Event raised when the documents list changes.
    /// </summary>
    event Action? OnDocumentsChanged;

    /// <summary>
    /// Loads or refreshes all documents across all conversations.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of documents with chat info.</returns>
    Task<IReadOnlyList<DocumentWithChatInfo>> LoadDocumentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Searches documents by filename.
    /// </summary>
    /// <param name="searchTerm">The search term to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The filtered list of documents.</returns>
    Task<IReadOnlyList<DocumentWithChatInfo>> SearchDocumentsAsync(string searchTerm, CancellationToken ct = default);

    /// <summary>
    /// Clears the search filter and reloads all documents.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task ClearSearchAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes a document based on the selected option.
    /// </summary>
    /// <param name="document">The document to delete.</param>
    /// <param name="option">Whether to delete just the document or also the chat.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deletion was successful.</returns>
    Task<bool> DeleteDocumentAsync(
        DocumentWithChatInfo document,
        DocumentDeleteOption option,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the download URL for a document.
    /// </summary>
    /// <param name="document">The document to get the URL for.</param>
    /// <returns>The download URL.</returns>
    string GetDownloadUrl(DocumentWithChatInfo document);

    /// <summary>
    /// Gets the thumbnail URL for a document.
    /// </summary>
    /// <param name="document">The document to get the thumbnail URL for.</param>
    /// <returns>The thumbnail URL, or null if no thumbnail available.</returns>
    string? GetThumbnailUrl(DocumentWithChatInfo document);
}
