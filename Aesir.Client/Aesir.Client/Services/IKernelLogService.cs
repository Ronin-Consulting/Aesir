using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Aesir.Common.Models;

namespace Aesir.Client.Services;

public interface IKernelLogService
{
    
    
    Task<IEnumerable<AesirKernelLog>> GetKernelLogsAsync(DateTimeOffset from, DateTimeOffset to);

    Task<IEnumerable<AesirKernelLog>> GetKernelLogsByChatSessionAsync(Guid? chatSessionId);

    Task<IEnumerable<AesirKernelLog>> GetKernelLogsByConversationAsync(Guid? conversationId);
}