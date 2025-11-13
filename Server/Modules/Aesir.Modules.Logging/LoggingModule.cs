using Aesir.Infrastructure.Modules;
using Aesir.Modules.Logging.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aesir.Modules.Logging;

/// <summary>
/// Logging module providing kernel execution log management.
/// Tracks and queries kernel execution activity including function calls, model interactions, and errors.
/// </summary>
public class LoggingModule : ModuleBase
{
    public LoggingModule(ILogger<LoggingModule> logger) : base(logger)
    {
    }

    public override string Name => "Logging";

    public override string Version => "1.0.0";

    public override string? Description => "Provides kernel execution logging and querying functionality for tracking AI operations";

    public override Task RegisterServicesAsync(IServiceCollection services)
    {
        Log("Registering logging services...");

        // Register services
        services.AddScoped<IKernelLogService, KernelLogService>();

        services.AddSingleton<KernelLoggingFilterService>();
        services.AddSingleton<IFunctionInvocationFilter, KernelLoggingFilterService>();
        services.AddSingleton<IPromptRenderFilter, KernelLoggingFilterService>();
        services.AddSingleton<IAutoFunctionInvocationFilter, KernelLoggingFilterService>();
        services.AddSingleton<IKernelLogService, KernelLogService>();
        
        Log("Logging services registered successfully");
        
        return Task.CompletedTask;
    }
}
