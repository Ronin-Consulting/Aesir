using System.Net.Http.Headers;
using System.Text;

namespace Aesir.Api.Server.Services;

public class LoggingHttpMessageHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHttpMessageHandler> _logger;

    public LoggingHttpMessageHandler(ILogger<LoggingHttpMessageHandler> logger)
    {
        _logger = logger;
    }

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

        _logger.LogInformation(
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

            _logger.LogInformation(
                "[{Id}] HTTP Response: {StatusCode} {Headers} {Content}", 
                id, response.StatusCode, 
                FormatHeaders(response.Headers), responseContent);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Id}] HTTP Request failed: {Message}", id, ex.Message);
            throw;
        }
    }

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
