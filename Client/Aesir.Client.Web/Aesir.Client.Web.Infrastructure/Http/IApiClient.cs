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
}
