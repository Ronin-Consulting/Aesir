using Aesir.Api.Server.Models;
using Aesir.Common.Models;
using AesirKernelLogDetails = Aesir.Api.Server.Models.AesirKernelLogDetails;

namespace Aesir.Api.Server.Services;

public interface IKernelLogService
{
    Task LogAsync(KernelLogLevel logLevel, string message, AesirKernelLogDetails details);
    
    Task<IEnumerable<AesirKernelLog>> GetLogsAsync(DateTimeOffset from, DateTimeOffset to);
    
    Task<IEnumerable<AesirKernelLog>> GetLogsByChatSessionAsync(Guid chatSessionId);
    
    Task<IEnumerable<AesirKernelLog>> GetLogsByConversationAsync(Guid conversationId);
}
