namespace Aesir.Client.Web.Infrastructure.Http;

/// <summary>
/// Interface for the AESIR API client.
/// Provides typed methods for communicating with the backend API.
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Performs a GET request to the specified endpoint.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="endpoint">The API endpoint (relative to base URL).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The deserialized response, or null if not found.</returns>
    Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default);

    /// <summary>
    /// Performs a POST request to the specified endpoint.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="endpoint">The API endpoint (relative to base URL).</param>
    /// <param name="data">The data to send in the request body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    Task<TResponse?> PostAsync<TResponse>(string endpoint, object data, CancellationToken ct = default);

    /// <summary>
    /// Performs a PUT request to the specified endpoint.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="endpoint">The API endpoint (relative to base URL).</param>
    /// <param name="data">The data to send in the request body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    Task<TResponse?> PutAsync<TResponse>(string endpoint, object data, CancellationToken ct = default);

    /// <summary>
    /// Performs a DELETE request to the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The API endpoint (relative to base URL).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<bool> DeleteAsync(string endpoint, CancellationToken ct = default);

    /// <summary>
    /// Performs a streaming GET request to the specified endpoint.
    /// </summary>
    /// <typeparam name="T">The expected item type in the stream.</typeparam>
    /// <param name="endpoint">The API endpoint (relative to base URL).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of streamed items.</returns>
    IAsyncEnumerable<T> StreamAsync<T>(string endpoint, CancellationToken ct = default);

    /// <summary>
    /// Performs a streaming POST request to the specified endpoint.
    /// </summary>
    /// <typeparam name="T">The expected item type in the stream.</typeparam>
    /// <param name="endpoint">The API endpoint (relative to base URL).</param>
    /// <param name="data">The data to send in the request body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of streamed items.</returns>
    IAsyncEnumerable<T> StreamPostAsync<T>(string endpoint, object data, CancellationToken ct = default);

    /// <summary>
    /// Downloads a file from the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The API endpoint (relative to base URL).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The file download result containing the file bytes, content type, and filename.</returns>
    Task<FileDownloadResult> DownloadFileAsync(string endpoint, CancellationToken ct = default);
}

/// <summary>
/// Result of a file download operation.
/// </summary>
public class FileDownloadResult
{
    /// <summary>
    /// Whether the download was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The file content as bytes.
    /// </summary>
    public byte[]? Data { get; init; }

    /// <summary>
    /// The content type of the file.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// The suggested filename from the Content-Disposition header.
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Error message if the download failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful file download result.
    /// </summary>
    public static FileDownloadResult Succeeded(byte[] data, string? contentType, string? fileName) => new()
    {
        Success = true,
        Data = data,
        ContentType = contentType,
        FileName = fileName
    };

    /// <summary>
    /// Creates a failed file download result.
    /// </summary>
    public static FileDownloadResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
