using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Common.Models;

namespace Aesir.Client.Services;

public interface IKernelLogService
{
    
    
    Task<IEnumerable<AesirKernelLogBase>> GetKernelLogsAsync(DateTimeOffset from, DateTimeOffset to);

    Task<IEnumerable<AesirKernelLogBase>> GetKernelLogsByChatSessionAsync(Guid? chatSessionId);
}