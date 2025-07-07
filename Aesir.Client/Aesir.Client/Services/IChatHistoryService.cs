using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Models;

namespace Aesir.Client.Services;

public interface IChatHistoryService
{
    Task<IEnumerable<AesirChatSessionItem>?> GetChatSessionsAsync(string userId);

    Task<IEnumerable<AesirChatSessionItem>?> SearchChatSessionsAsync(string userId, string searchTerm);
    
    Task<AesirChatSession?> GetChatSessionAsync(Guid id);

    Task UpdateChatSessionTitleAsync(Guid id, string title);
    
    Task DeleteChatSessionAsync(Guid id);
}