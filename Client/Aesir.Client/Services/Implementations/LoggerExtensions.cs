using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations;

/// <summary>
/// Provides extension methods for logging to enhance ILogger functionality.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs details of a <see cref="FlurlHttpException"/> including the URL and error response.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> instance used to log the error details.</param>
    /// <param name="ex">The <see cref="FlurlHttpException"/> instance containing information about the HTTP error.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task LogFlurlExceptionAsync(this ILogger logger, FlurlHttpException ex)
    {
        var error = await ex.GetResponseStringAsync();
        logger.LogError("Error returned from {Url}: {Error}", ex.Call.Request.Url, error);
    }
}