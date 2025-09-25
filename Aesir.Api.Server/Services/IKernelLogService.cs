using Aesir.Api.Server.Models;
using Aesir.Common.Models;

namespace Aesir.Api.Server.Services;

public interface IKernelLogService
{
    Task LogAsync(KernelLogLevel logLevel, string message, AesirKernelLogDetails details);
    
    Task<IEnumerable<AesirKernelLogBase>> GetLogsAsync(DateTimeOffset from, DateTimeOffset to);
    
    Task<IEnumerable<AesirKernelLogBase>> GetLogsAsync(Guid conversationId);
}
