using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

public class ChatHistoryService : IChatHistoryService
{
    private readonly ILogger<ChatService> _logger;
    private readonly IFlurlClient _flurlClient;

    public ChatHistoryService(ILogger<ChatService> logger,
        IConfiguration configuration, IFlurlClientCache flurlClientCache)
    {
        _logger = logger;
        _flurlClient = flurlClientCache
            .GetOrAdd("ChatHistoryClient",
                configuration.GetValue<string>("Inference:ChatHistory"));
    }
    
    public async Task<IEnumerable<AesirChatSessionItem>> GetChatSessionsAsync(string userId)
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment("user")
                .AppendPathSegment(userId)
                .GetJsonAsync<IEnumerable<AesirChatSessionItem>>());
        }
        catch (FlurlHttpException ex)
        {
            await _logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }
    
    public async Task<IEnumerable<AesirChatSessionItem>> SearchChatSessionsAsync(string userId, string searchTerm)
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment("user")
                .AppendPathSegment(userId)
                .AppendPathSegment("search")
                .AppendPathSegment(searchTerm)
                .GetJsonAsync<IEnumerable<AesirChatSessionItem>>());
        }
        catch (FlurlHttpException ex)
        {
            await _logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }
    
    public async Task<AesirChatSession> GetChatSessionAsync(Guid id)
    {
        try
        {
            return await _flurlClient.Request()
                .AppendPathSegment(id)
                .GetJsonAsync<AesirChatSession>();
        }
        catch (FlurlHttpException ex)
        {
            await _logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    public async Task UpdateChatSessionTitleAsync(Guid id, string title)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment(id)
                .AppendPathSegment(title)
                .PutAsync();
        }
        catch (FlurlHttpException ex)
        {
            await _logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    public async Task DeleteChatSessionAsync(Guid id)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment(id)
                .DeleteAsync();
        }
        catch (FlurlHttpException ex)
        {
            await _logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }
}