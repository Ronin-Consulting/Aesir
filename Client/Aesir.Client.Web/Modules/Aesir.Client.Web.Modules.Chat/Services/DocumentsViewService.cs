using System.Text.RegularExpressions;
using Aesir.Client.Web.Modules.Chat.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Implementation of IDocumentsViewService that manages the documents view with caching.
/// </summary>
public partial class DocumentsViewService : IDocumentsViewService
{
    private readonly IDocumentApiService _documentApiService;
    private readonly IChatHistoryService _chatHistoryService;
    private List<DocumentWithChatInfo> _documents = [];
    private List<DocumentWithChatInfo> _allDocuments = []; // Cache for filtering
    private bool _isLoading;
    private string? _searchTerm;

    public DocumentsViewService(
        IDocumentApiService documentApiService,
        IChatHistoryService chatHistoryService)
    {
        _documentApiService = documentApiService;
        _chatHistoryService = chatHistoryService;

        // Subscribe to chat session changes to refresh document metadata
        _chatHistoryService.OnSessionsChanged += HandleSessionsChanged;
    }

    /// <inheritdoc />
    public IReadOnlyList<DocumentWithChatInfo> Documents => _documents.AsReadOnly();

    /// <inheritdoc />
    public bool IsLoading => _isLoading;

    /// <inheritdoc />
    public string? SearchTerm => _searchTerm;

    /// <inheritdoc />
    public event Action? OnDocumentsChanged;

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentWithChatInfo>> LoadDocumentsAsync(CancellationToken ct = default)
    {
        _isLoading = true;
        _searchTerm = null;
        NotifyChanged();

        try
        {
            // Ensure we have the latest chat sessions for metadata
            if (_chatHistoryService.Sessions.Count == 0)
            {
                await _chatHistoryService.LoadSessionsAsync(ct).ConfigureAwait(false);
            }

            // Fetch files filtered by user's conversations (server-side filtering)
            var files = await _documentApiService.GetAllFilesAsync(ct).ConfigureAwait(false);

            // Build document list with chat metadata
            // Note: Server already filters by user's conversations, but we still need to
            // enrich with session metadata (title, starred status)
            _allDocuments = files
                .Select(file => BuildDocumentWithChatInfo(file))
                .OrderByDescending(d => d.IsChatStarred)
                .ThenByDescending(d => d.File.CreatedAt)
                .ToList();

            _documents = _allDocuments;

            return _documents.AsReadOnly();
        }
        finally
        {
            _isLoading = false;
            NotifyChanged();
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DocumentWithChatInfo>> SearchDocumentsAsync(string searchTerm, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            _searchTerm = null;
            _documents = _allDocuments;
        }
        else
        {
            _searchTerm = searchTerm;

            // Filter documents by filename or chat title (case-insensitive)
            var lowerSearchTerm = searchTerm.ToLowerInvariant();
            _documents = _allDocuments
                .Where(d =>
                    d.File.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (d.ChatTitle?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        NotifyChanged();
        return Task.FromResult<IReadOnlyList<DocumentWithChatInfo>>(_documents.AsReadOnly());
    }

    /// <inheritdoc />
    public async Task ClearSearchAsync(CancellationToken ct = default)
    {
        _searchTerm = null;
        _documents = _allDocuments;
        NotifyChanged();
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentAsync(
        DocumentWithChatInfo document,
        DocumentDeleteOption option,
        CancellationToken ct = default)
    {
        try
        {
            // Always delete the document first
            var documentDeleted = await _documentApiService.DeleteFileAsync(
                document.ConversationId,
                document.File.DisplayName,
                ct).ConfigureAwait(false);

            if (!documentDeleted)
            {
                return false;
            }

            // If user chose to delete the chat as well
            if (option == DocumentDeleteOption.DocumentAndChat && document.HasValidChat)
            {
                await _chatHistoryService.DeleteSessionAsync(document.ConversationId, ct).ConfigureAwait(false);
            }

            // Remove from local cache
            _allDocuments.RemoveAll(d => d.File.Id == document.File.Id);
            _documents.RemoveAll(d => d.File.Id == document.File.Id);
            NotifyChanged();

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public string GetDownloadUrl(DocumentWithChatInfo document)
    {
        return _documentApiService.GetDownloadUrl(document.ConversationId, document.File.DisplayName);
    }

    /// <inheritdoc />
    public string? GetThumbnailUrl(DocumentWithChatInfo document)
    {
        if (!document.File.HasThumbnail)
        {
            return null;
        }

        return _documentApiService.GetThumbnailUrl(document.ConversationId, document.File.DisplayName);
    }

    private DocumentWithChatInfo BuildDocumentWithChatInfo(ConversationFile file)
    {
        // Extract conversation ID from file path
        var conversationId = ExtractConversationId(file.FileName);

        // Find matching chat session
        var session = conversationId.HasValue
            ? _chatHistoryService.Sessions.FirstOrDefault(s => s.Id == conversationId.Value)
            : null;

        return new DocumentWithChatInfo
        {
            File = file,
            ConversationId = conversationId ?? Guid.Empty,
            ChatTitle = session?.Title,
            IsChatStarred = session?.IsStarred ?? false,
            ChatUpdatedAt = session?.UpdatedAt
        };
    }

    private static Guid? ExtractConversationId(string fileName)
    {
        // FileName format: "/{conversationId}/{filename}"
        var match = ConversationIdRegex().Match(fileName);
        if (match.Success && Guid.TryParse(match.Groups[1].Value, out var id))
        {
            return id;
        }
        return null;
    }

    [GeneratedRegex(@"^/([^/]+)/")]
    private static partial Regex ConversationIdRegex();

    private void HandleSessionsChanged()
    {
        // Refresh document metadata when chat sessions change
        // This updates starred status and titles without refetching files
        if (_allDocuments.Count > 0)
        {
            _allDocuments = _allDocuments
                .Select(d => BuildDocumentWithChatInfo(d.File))
                .OrderByDescending(d => d.IsChatStarred)
                .ThenByDescending(d => d.File.CreatedAt)
                .ToList();

            // Reapply search filter if active
            if (!string.IsNullOrEmpty(_searchTerm))
            {
                var lowerSearchTerm = _searchTerm.ToLowerInvariant();
                _documents = _allDocuments
                    .Where(d =>
                        d.File.DisplayName.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (d.ChatTitle?.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }
            else
            {
                _documents = _allDocuments;
            }

            NotifyChanged();
        }
    }

    private void NotifyChanged()
    {
        OnDocumentsChanged?.Invoke();
    }
}
