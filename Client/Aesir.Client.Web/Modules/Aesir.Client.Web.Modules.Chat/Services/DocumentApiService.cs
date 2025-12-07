using System.Net.Http.Headers;
using System.Net.Http.Json;
using Aesir.Client.Web.Modules.Chat.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing document uploads and retrieval for conversations.
/// </summary>
public class DocumentApiService : IDocumentApiService
{
    private readonly HttpClient _httpClient;

    public DocumentApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<FileUploadResponse> UploadFileAsync(
        Guid conversationId,
        IBrowserFile file,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        if (!IsFileTypeSupported(file.Name))
            throw new InvalidOperationException($"File type not supported: {Path.GetExtension(file.Name)}");

        if (!IsFileSizeValid(file.Size))
            throw new InvalidOperationException($"File size exceeds the maximum allowed ({IDocumentApiService.MaxFileSize / (1024 * 1024)}MB)");

        var endpoint = $"/document/collections/conversations/{conversationId}/upload/file";

        using var content = new MultipartFormDataContent();

        // Open the file stream with the max size limit
        await using var fileStream = file.OpenReadStream(IDocumentApiService.MaxFileSize, ct);

        // Create a stream content for the file
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);

        content.Add(streamContent, "file", file.Name);

        // Report initial progress
        progress?.Report(0);

        var response = await _httpClient.PostAsync(endpoint, content, ct);

        // Report completion
        progress?.Report(100);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FileUploadResponse>(ct);
        return result ?? new FileUploadResponse { Message = "Upload complete", FileName = file.Name };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationFile>> GetFilesAsync(
        Guid conversationId,
        CancellationToken ct = default)
    {
        var endpoint = $"/document/collections/conversations/{conversationId}/files";

        var response = await _httpClient.GetAsync(endpoint, ct);
        response.EnsureSuccessStatusCode();

        var files = await response.Content.ReadFromJsonAsync<List<ConversationFile>>(ct);
        return files?.AsReadOnly() ?? (IReadOnlyList<ConversationFile>)Array.Empty<ConversationFile>();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(
        Guid conversationId,
        string filename,
        CancellationToken ct = default)
    {
        // URL-encode the filename in case it contains special characters
        var encodedFilename = Uri.EscapeDataString(filename);
        var endpoint = $"/document/collections/conversations/{conversationId}/files/{encodedFilename}";

        var response = await _httpClient.DeleteAsync(endpoint, ct);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public string GetDownloadUrl(Guid conversationId, string filename)
    {
        var encodedFilename = Uri.EscapeDataString(filename);
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "";
        return $"{baseUrl}/document/collections/conversations/{conversationId}/files/{encodedFilename}/content";
    }

    /// <inheritdoc />
    public bool IsFileTypeSupported(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return false;

        var extension = Path.GetExtension(filename).ToLowerInvariant();
        return IDocumentApiService.SupportedExtensions.Contains(extension);
    }

    /// <inheritdoc />
    public bool IsFileSizeValid(long fileSize)
    {
        return fileSize > 0 && fileSize <= IDocumentApiService.MaxFileSize;
    }

    /// <inheritdoc />
    public async Task<bool> MoveFilesAsync(
        Guid sourceConversationId,
        Guid targetConversationId,
        CancellationToken ct = default)
    {
        var endpoint = $"/document/collections/conversations/{sourceConversationId}/move/{targetConversationId}";

        var response = await _httpClient.PostAsync(endpoint, null, ct);
        return response.IsSuccessStatusCode;
    }
}
