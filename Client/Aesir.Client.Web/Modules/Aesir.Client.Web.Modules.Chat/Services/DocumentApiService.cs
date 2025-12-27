using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Aesir.Client.Web.Infrastructure.Models;
using Aesir.Client.Web.Infrastructure.Services;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing document uploads and retrieval for conversations.
/// </summary>
public class DocumentApiService : IDocumentApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DocumentApiService> _logger;

    /// <summary>
    /// Polling interval for checking indexing status.
    /// </summary>
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Maximum time to wait for indexing to complete.
    /// </summary>
    private static readonly TimeSpan MaxPollDuration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Special progress value to signal that the file is being processed in the background.
    /// The UI should show an indeterminate spinner when this value is received.
    /// </summary>
    public const int ProcessingProgressSignal = -1;

    public DocumentApiService(HttpClient httpClient, ILogger<DocumentApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
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

        // Handle 202 Accepted - file stored, indexing in background
        if (response.StatusCode == HttpStatusCode.Accepted)
        {
            var acceptedResult = await response.Content.ReadFromJsonAsync<FileUploadAcceptedResponse>(ct);

            if (acceptedResult?.OperationId != null)
            {
                _logger.LogDebug("File stored, waiting for indexing. OperationId: {OperationId}", acceptedResult.OperationId);

                // Report 100% to indicate upload is complete
                progress?.Report(100);

                // Brief pause to show the completed upload progress bar
                await Task.Delay(300, ct);

                // Signal that we're now in processing mode - UI should show indeterminate spinner
                progress?.Report(ProcessingProgressSignal);

                // Poll for indexing completion (no progress updates during polling - just wait)
                await PollForIndexingCompletionAsync(acceptedResult.OperationId.Value, file.Name, ct);
            }

            progress?.Report(100);
            return new FileUploadResponse
            {
                Message = acceptedResult?.Message ?? "Upload and indexing complete",
                FileName = file.Name
            };
        }

        // Handle other success responses
        response.EnsureSuccessStatusCode();
        progress?.Report(100);

        var result = await response.Content.ReadFromJsonAsync<FileUploadResponse>(ct);
        return result ?? new FileUploadResponse { Message = "Upload complete", FileName = file.Name };
    }

    /// <summary>
    /// Polls the status endpoint until indexing is complete or fails.
    /// During this time, the UI shows an indeterminate spinner (no progress updates).
    /// </summary>
    private async Task PollForIndexingCompletionAsync(
        Guid operationId,
        string fileName,
        CancellationToken ct)
    {
        var elapsed = TimeSpan.Zero;

        while (elapsed < MaxPollDuration)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var statusEndpoint = $"/document/collections/operations/{operationId}/status";
                var statusResponse = await _httpClient.GetAsync(statusEndpoint, ct);

                if (statusResponse.IsSuccessStatusCode)
                {
                    var status = await statusResponse.Content.ReadFromJsonAsync<OperationStatusResponse>(ct);

                    if (status != null)
                    {
                        switch (status.Status?.ToLowerInvariant())
                        {
                            case "completed":
                                _logger.LogDebug("Indexing completed for {FileName}", fileName);
                                return;

                            case "failed":
                                var errorMessage = status.ErrorMessage ?? "Unknown error during indexing";
                                _logger.LogWarning("Indexing failed for {FileName}: {Error}", fileName, errorMessage);
                                throw new InvalidOperationException($"Document indexing failed: {errorMessage}");
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Error polling indexing status for {FileName}", fileName);
            }

            await Task.Delay(PollInterval, ct);
            elapsed += PollInterval;
        }

        _logger.LogWarning("Indexing timed out for {FileName} after {Duration}", fileName, MaxPollDuration);
        throw new TimeoutException($"Document indexing did not complete within {MaxPollDuration.TotalMinutes} minutes");
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
    public async Task<IReadOnlyList<ConversationFile>> GetAllFilesAsync(CancellationToken ct = default)
    {
        // Server-side filtering ensures only the current user's documents are returned
        var endpoint = "/document/collections/conversations/files";

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
    /// <exception cref="HttpRequestException">
    /// Thrown when the request fails. Check StatusCode property:
    /// - NotFound (404): Document doesn't exist
    /// - Other status codes: Server or network error
    /// </exception>
    public async Task<CitationFileMetadata?> GetCitationMetadataAsync(
        string conversationId,
        string filename,
        CancellationToken ct = default)
    {
        var encodedFilename = Uri.EscapeDataString(filename);
        var endpoint = $"/document/collections/conversations/{conversationId}/files/{encodedFilename}/info";

        var response = await _httpClient.GetAsync(endpoint, ct);

        if (!response.IsSuccessStatusCode)
        {
            // Throw with status code so caller can distinguish 404 (not found) from other errors
            throw new HttpRequestException(
                $"Failed to get citation metadata: {response.StatusCode}",
                inner: null,
                statusCode: response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<CitationFileMetadata>(ct);
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
