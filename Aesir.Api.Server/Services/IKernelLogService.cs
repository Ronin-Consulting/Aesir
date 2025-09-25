using Aesir.Api.Server.Models;
using Aesir.Common.Models;

namespace Aesir.Api.Server.Services;

public interface IKernelLogService
{
    Task LogAsync(KernelLogLevel logLevel, string message, AesirKernelLogDetails details);
}
