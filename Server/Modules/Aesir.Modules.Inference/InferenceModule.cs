using Aesir.Infrastructure.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Inference;

/// <summary>
/// Base module for inference services. Provides common infrastructure for AI model providers.
/// This module does not register any concrete implementations - those are provided by
/// provider-specific modules (Ollama, OpenAI, etc.)
/// </summary>
public class InferenceModule : ModuleBase
{
    public InferenceModule(ILogger<InferenceModule> logger) : base(logger)
    {
    }

    public override string Name => "Inference";

    public override string Version => "1.0.0";

    public override string Description => "Base infrastructure for AI inference services";
    
    public override Task RegisterServicesAsync(IServiceCollection services)
    {
        // This module only provides abstractions (interfaces and base classes)
        // No concrete services are registered here
        Logger.LogInformation("Base module services registered (abstractions only)");

        return Task.CompletedTask;
    }

    public override void Initialize(IApplicationBuilder app)
    {
        Logger.LogInformation("Base module initialized");
    }
}
