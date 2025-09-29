using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

public class KernelLogService(
    ILogger<KernelLogService> logger,
    IConfiguration configuration,
    IFlurlClientCache flurlClientCache): IKernelLogService
{
    private readonly IFlurlClient _flurlClient = flurlClientCache
        .GetOrAdd("LogCollectionClient",
            configuration.GetValue<string>("Inference:Logs"));

    public async Task<IEnumerable<AesirKernelLog>> GetKernelLogsAsync(DateTimeOffset from, DateTimeOffset to)
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment("kernel")
                .AppendQueryParam("from",from)
                .AppendQueryParam("to",to)
                .GetJsonAsync<IEnumerable<AesirKernelLog>>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }   
    }
    
    public async Task<IEnumerable<AesirKernelLog>> GetKernelLogsByChatSessionAsync(Guid? chatSessionId)
    {
        if (chatSessionId == null)
            return [];
        
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment("kernel")
                .AppendPathSegment(chatSessionId)
                .GetJsonAsync<IEnumerable<AesirKernelLog>>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }   
    }
}