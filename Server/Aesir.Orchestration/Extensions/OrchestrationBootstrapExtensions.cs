using System.Diagnostics.CodeAnalysis;
using Aesir.Infrastructure.Services;
using Aesir.Orchestration.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aesir.Orchestration.Extensions;

/// <summary>
/// Provides extension methods for bootstrapping orchestration services, inference engines,
/// and AI model lifecycle management in the application startup pipeline.
/// </summary>
public static class OrchestrationBootstrapExtensions
{
    /// <summary>
    /// Registers core orchestration services including kernel plugin management.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    [Experimental("SKEXP0001")]
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddAesirAIOrchestrationServices(this IServiceCollection services)
    {
        services.AddSingleton<IKernelPluginService, KernelPluginService>();
        
        return services;
    }
    
    /// <summary>
    /// Registers application lifetime hooks to unload AI models on shutdown.
    /// Ensures chat models, RAG embedding models, and RAG vision models are gracefully unloaded.
    /// </summary>
    /// <param name="app">The web application instance.</param>
    /// <param name="configurationService">Service for accessing application configuration.</param>
    /// <returns>The web application for method chaining.</returns>
    public static async Task<WebApplication> RegisterModelLifecycleAsync(
        this WebApplication app)
    {
        var configurationService = app.Services.GetRequiredService<IConfigurationService>();
        
        var generalSettings = await configurationService.GetGeneralSettingsAsync();
        var agents = (await configurationService.GetAgentsAsync()).ToList();

        var appLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();

        // This is an imperfect routine. Ideally the IModelsService would know what models have been loaded, but
        // they currently don't. We will assume the RAG embedding model, RAG vision model, and any model on any
        // agent have been loaded. They possibly have not, so we rely on the IModelsService implementations
        // to handle this gracefully. In the future we should track models that have been used/loaded.

        // Unload all chat models
        foreach (var agent in agents)
        {
            var chatInferenceEngineId = agent.ChatInferenceEngineId?.ToString() ??
                throw new InvalidOperationException($"ChatInferenceEngineId not configured for {agent.Name}");
            var chatModel = agent.ChatModel ??
                throw new InvalidOperationException($"ChatModel not configured for {agent.Name}");

            var chatModelsService = app.Services.GetKeyedService<IModelsService>(chatInferenceEngineId) ??
                throw new InvalidOperationException($"Missing expected ModelsService for {chatInferenceEngineId}");

            appLifetime.ApplicationStopping.Register(() => {
                chatModelsService.UnloadModelsAsync([chatModel]).Wait();
            });
        }

        // Unload RAG embedding model
        if (generalSettings.RagEmbeddingInferenceEngineId == null || generalSettings.RagEmbeddingModel == null)
        {
            logger.LogWarning("RAG embedding model configuration is not yet complete");
        }
        else
        {
            var ragEmbeddingInferenceEngineId = generalSettings.RagEmbeddingInferenceEngineId?.ToString() ??
                                                throw new InvalidOperationException($"RagEmbeddingInferenceEngineId not configured");
            var ragEmbeddingModelsService = app.Services.GetKeyedService<IModelsService>(ragEmbeddingInferenceEngineId) ??
                throw new InvalidOperationException($"Missing expected ModelsService for {ragEmbeddingInferenceEngineId}");
            appLifetime.ApplicationStopping.Register(() => {
                ragEmbeddingModelsService.UnloadModelsAsync([generalSettings.RagEmbeddingModel]).Wait();
            });
        }

        // Unload RAG vision model
        if (generalSettings.RagVisionInferenceEngineId == null || generalSettings.RagVisionModel == null)
        {
            logger.LogWarning("RAG vision model configuration is not yet complete");
        }
        else
        {
            var ragVisionInferenceEngineId = generalSettings.RagVisionInferenceEngineId?.ToString() ??
                                        throw new InvalidOperationException($"RagVisionInferenceEngineId not configured");
            var ragVisionModelsService = app.Services.GetKeyedService<IModelsService>(ragVisionInferenceEngineId) ??
                                            throw new InvalidOperationException($"Missing expected ModelsService for {ragVisionInferenceEngineId}");
            appLifetime.ApplicationStopping.Register(() => {
                ragVisionModelsService.UnloadModelsAsync([generalSettings.RagVisionModel]).Wait();
            });
        }

        return app;
    }
}
