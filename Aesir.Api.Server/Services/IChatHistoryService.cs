using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

public interface IChatHistoryService
{
    Task UpsertChatSessionAsync(AesirChatSession chatSession);

    Task<AesirChatSession?> GetChatSessionAsync(Guid id);

    Task<IEnumerable<AesirChatSession>> GetChatSessionsAsync(string userId);

    Task<IEnumerable<AesirChatSession>> GetChatSessionsAsync(string userId, DateTimeOffset from, DateTimeOffset to);

    Task<IEnumerable<AesirChatSession>> SearchChatSessionsAsync(string searchTerm, string userId);

    Task DeleteChatSessionAsync(Guid id);
}
