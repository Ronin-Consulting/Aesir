using Aesir.Infrastructure.Modules;
using Aesir.Modules.Inference.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aesir.Modules.Inference;

/// <summary>
/// Base module for inference services. Provides common infrastructure for AI model providers.
/// This module does not register any concrete implementations - those are provided by
/// provider-specific modules (Ollama, OpenAI, etc.)
/// </summary>
public class InferenceModule : ModuleBase
{
    public InferenceModule(ILogger logger) : base(logger)
    {
    }

    public override string Name => "Inference";

    public override string Version => "1.0.0";

    public override string Description => "Base infrastructure for AI inference services";

    public override Task RegisterServicesAsync(IServiceCollection services)
    {
        // Register tool call broadcasting infrastructure
        // The broadcaster uses AsyncLocal<T> for per-request scoping, so it can be Singleton
        services.AddSingleton<IToolCallBroadcaster, ToolCallBroadcaster>();

        // Filter must be Singleton to be picked up by Semantic Kernel's AddKernel()
        // The filter accesses the current scope via ToolCallBroadcaster.Current (AsyncLocal)
        services.AddSingleton<ToolCallStreamingFilter>();
        services.AddSingleton<IAutoFunctionInvocationFilter>(sp => sp.GetRequiredService<ToolCallStreamingFilter>());

        Logger.LogInformation("Tool call broadcasting services registered");

        return Task.CompletedTask;
    }

    public override void Initialize(IApplicationBuilder app)
    {
        Logger.LogInformation("Base module initialized");
    }
}
