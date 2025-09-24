using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

public interface IKernelLogService
{
    Task LogAsync(KernelLogLevel logLevel, string message, AesirKernelLogDetails details);
}

public enum KernelLogLevel
{
    Info,
    Warning,
    Error
}