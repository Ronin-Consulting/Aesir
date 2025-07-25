using System.Net.Http.Headers;
using System.Text;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides HTTP message logging functionality for intercepting and logging HTTP requests and responses.
/// </summary>
/// <param name="logger">The logger instance for recording HTTP messages.</param>
public class LoggingHttpMessageHandler(ILogger<LoggingHttpMessageHandler> logger) : DelegatingHandler
{
    /// <summary>
    /// Intercepts HTTP requests and responses to log their details.
    /// </summary>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns the HTTP response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var id = Guid.NewGuid().ToString();
        var requestContent = string.Empty;

        if (request.Content != null)
        {
            requestContent = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        logger.LogInformation(
            "[{Id}] HTTP Request: {Method} {Uri} {Headers} {Content}", 
            id, request.Method, request.RequestUri, 
            FormatHeaders(request.Headers), requestContent);

        try
        {
            // Continue processing the request
            var response = await base.SendAsync(request, cancellationToken);

            var responseContent = string.Empty;
            if (response.Content != null)
            {
                // Clone the response content to not consume the original stream
                responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            }

            logger.LogInformation(
                "[{Id}] HTTP Response: {StatusCode} {Headers} {Content}", 
                id, response.StatusCode, 
                FormatHeaders(response.Headers), responseContent);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Id}] HTTP Request failed: {Message}", id, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Formats HTTP headers into a readable string format.
    /// </summary>
    /// <param name="headers">The HTTP headers to format.</param>
    /// <returns>A formatted string representation of the headers.</returns>
    private static string FormatHeaders(HttpHeaders headers)
    {
        if (headers == null || !headers.Any())
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var header in headers)
        {
            sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }

        return sb.ToString();
    }
}
