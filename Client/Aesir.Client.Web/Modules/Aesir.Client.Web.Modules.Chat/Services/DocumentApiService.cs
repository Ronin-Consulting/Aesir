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
    public string GetThumbnailUrl(Guid conversationId, string filename)
    {
        var encodedFilename = Uri.EscapeDataString(filename);
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "";
        return $"{baseUrl}/document/collections/conversations/{conversationId}/files/{encodedFilename}/thumbnail";
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

    #region Citation Viewer Methods

    /// <inheritdoc />
    public async Task<CitationFileMetadata?> GetCitationMetadataAsync(
        string conversationId,
        string filename,
        CancellationToken ct = default)
    {
        try
        {
            var encodedFilename = Uri.EscapeDataString(filename);
            var endpoint = $"/document/collections/conversations/{conversationId}/files/{encodedFilename}/info";

            var response = await _httpClient.GetAsync(endpoint, ct);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CitationFileMetadata>(ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetCitationContentAsync(
        string conversationId,
        string filename,
        CancellationToken ct = default)
    {
        try
        {
            var encodedFilename = Uri.EscapeDataString(filename);
            var endpoint = $"/document/collections/conversations/{conversationId}/files/{encodedFilename}/content";

            var response = await _httpClient.GetAsync(endpoint, ct);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public string GetCitationViewUrl(string conversationId, string filename)
    {
        var encodedFilename = Uri.EscapeDataString(filename);
        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "";
        // Use the inline endpoint that sets Content-Disposition: inline
        // This allows browsers to display PDFs and images directly instead of downloading
        return $"{baseUrl}/document/collections/file/{conversationId}/{encodedFilename}";
    }

    /// <inheritdoc />
    public async Task<string?> GetCitationDataUrlAsync(
        string conversationId,
        string filename,
        CancellationToken ct = default)
    {
        try
        {
            var content = await GetCitationContentAsync(conversationId, filename, ct);
            if (content == null)
            {
                return null;
            }

            // Determine MIME type from extension
            var mimeType = GetMimeTypeFromExtension(filename);
            var base64 = Convert.ToBase64String(content);

            return $"data:{mimeType};base64,{base64}";
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CitationExistsAsync(
        string conversationId,
        string filename,
        CancellationToken ct = default)
    {
        try
        {
            var encodedFilename = Uri.EscapeDataString(filename);
            var endpoint = $"/document/collections/conversations/{conversationId}/files/{encodedFilename}/content";

            // Use HEAD request to check existence without downloading content
            var request = new HttpRequestMessage(HttpMethod.Head, endpoint);
            var response = await _httpClient.SendAsync(request, ct);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the MIME type for a file based on its extension.
    /// </summary>
    private static string GetMimeTypeFromExtension(string filename)
    {
        var extension = Path.GetExtension(filename).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".tiff" or ".tif" => "image/tiff",
            ".txt" or ".log" => "text/plain",
            ".md" or ".markdown" => "text/markdown",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".csv" => "text/csv",
            ".html" or ".htm" => "text/html",
            _ => "application/octet-stream"
        };
    }

    #endregion
}
