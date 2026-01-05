using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aesir.Client.Web.Infrastructure.Http;

/// <summary>
/// Default implementation of the AESIR API client.
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            // Don't set PropertyNamingPolicy - rely on [JsonPropertyName] attributes on models
            // which use snake_case (e.g., "chat_inference_engine_id")
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync(endpoint, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse?> PostAsync<TResponse>(string endpoint, object data, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse?> PutAsync<TResponse>(string endpoint, object data, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        // Handle 204 No Content or empty response body
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent ||
            response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync(endpoint, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            // Try to read error details from response body
            try
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>(_jsonOptions, ct).ConfigureAwait(false);
                if (errorResponse?.Message != null)
                {
                    throw new HttpRequestException(errorResponse.Message, null, response.StatusCode);
                }
            }
            catch (JsonException)
            {
                // If we can't parse the error response, fall through to generic handling
            }

            // Fallback to generic error
            response.EnsureSuccessStatusCode();
        }

        return true;
    }

    /// <summary>
    /// Error response structure from the API.
    /// </summary>
    private record ErrorResponse(
        string? Error,
        string? Message,
        string? Suggested_Action,
        string? Constraint_Name,
        string? Entity_Name);


    /// <inheritdoc />
    public async IAsyncEnumerable<T> StreamAsync<T>(
        string endpoint,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

        await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(stream, _jsonOptions, ct)
                           .ConfigureAwait(false))
        {
            if (item is not null)
            {
                yield return item;
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<T> StreamPostAsync<T>(
        string endpoint,
        object data,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(data, null, _jsonOptions)
        };

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

        await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(stream, _jsonOptions, ct)
                           .ConfigureAwait(false))
        {
            if (item is not null)
            {
                yield return item;
            }
        }
    }

    /// <inheritdoc />
    public async Task<FileDownloadResult> DownloadFileAsync(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return FileDownloadResult.Failed($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            var data = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType?.MediaType;

            // Try to get filename from Content-Disposition header
            string? fileName = null;
            if (response.Content.Headers.ContentDisposition?.FileName is { } fn)
            {
                fileName = fn.Trim('"');
            }
            else if (response.Content.Headers.ContentDisposition?.FileNameStar is { } fns)
            {
                fileName = fns.Trim('"');
            }

            return FileDownloadResult.Succeeded(data, contentType, fileName);
        }
        catch (Exception ex)
        {
            return FileDownloadResult.Failed(ex.Message);
        }
    }
}
