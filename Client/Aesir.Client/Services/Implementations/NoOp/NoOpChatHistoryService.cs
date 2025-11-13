using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Aesir.Common.Models;

namespace Aesir.Client.Services.Implementations.NoOp;

public class NoOpChatHistoryService : IChatHistoryService
{
    public async Task<IEnumerable<AesirChatSessionItem>?> GetChatSessionsAsync(string userId = "Unknown")
    {
        return await Task.FromResult(new List<AesirChatSessionItem>());
    }

    public async Task<IEnumerable<AesirChatSessionItem>?> GetChatSessionsByFileAsync(string fileName = "Unknown")
    {
        return await Task.FromResult(new List<AesirChatSessionItem>());
    }

    public async Task<IEnumerable<AesirChatSessionItem>?> SearchChatSessionsAsync(string userId = "Unknown", string searchTerm = "")
    {
        return await Task.FromResult(new List<AesirChatSessionItem>());
    }

    public async Task<AesirChatSession?> GetChatSessionAsync(Guid id)
    {
        return await Task.FromResult<AesirChatSession?>(null);
    }

    public async Task UpdateChatSessionTitleAsync(Guid id, string title)
    {
        await Task.CompletedTask;
    }

    public async Task DeleteChatSessionAsync(Guid id)
    {
        await Task.CompletedTask;
    }
}