using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations;

public static class LoggerExtensions
{
    public static async Task LogFlurlExceptionAsync(this ILogger logger, FlurlHttpException ex)
    {
        var error = await ex.GetResponseStringAsync();
        logger.LogError("Error returned from {Url}: {Error}", ex.Call.Request.Url, error);
    }
}