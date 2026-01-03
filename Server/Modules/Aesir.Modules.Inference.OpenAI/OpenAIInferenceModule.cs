using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics.CodeAnalysis;
using Aesir.Common.Models;
using Aesir.Infrastructure.Models;
using Aesir.Infrastructure.Modules;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Inference.OpenAI.Services;
using Aesir.Modules.Inference.OpenAI.Stopgap;
using Aesir.Modules.Inference.Services;
using Aesir.Modules.Logging.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using OpenAI;

namespace Aesir.Modules.Inference.OpenAI;

/// <summary>
/// Represents a module providing OpenAI-specific services for AI inference.
/// This module is responsible for registering and initializing services related
/// to inference engines and configurations specific to OpenAI or compatible types.
/// </summary>
[Experimental("SKEXP0070")]
// ReSharper disable once InconsistentNaming
public class OpenAIInferenceModule : ModuleBase
{
    public OpenAIInferenceModule(ILogger logger) : base(logger)
    {
    }

    /// <summary>
    /// Gets the name of the module.
    /// </summary>
    /// <remarks>
    /// The <c>Name</c> property represents the unique name or identifier of a module.
    /// Each module should define this property to provide a meaningful and clear
    /// label that can be used for discovery, logging, and other identification purposes.
    /// </remarks>
    public override string Name => "OpenAI Inference Engine";

    /// Gets the version of the module as a string representation.
    /// This property defines the specific version of the module implementation.
    /// It is used to identify the module's release or iteration, which may help
    /// during deployment, debugging, or support scenarios.
    /// Derived classes must override this property to specify their version.
    public override string Version => "1.0.0";

    /// <summary>
    /// Gets a brief description of the module.
    /// Provides specific details about the module's functionality or purpose.
    /// </summary>
    public override string? Description => "Provides OpenAi specific services for AI inference";

    /// <summary>
    /// Asynchronously registers the required services specific to the OpenAI inference module,
    /// based on the configured inference engines and their readiness at boot time.
    /// </summary>
    /// <param name="services">The service collection to which dependencies are registered.</param>
    /// <return>A <see cref="Task"/> representing the asynchronous operation.</return>
    public override async Task RegisterServicesAsync(IServiceCollection services)
    {
        Logger.LogInformation("[OpenAI] Starting OpenAI Inference Module registration");

        var inferenceEngines = await ConfigurationService!.GetInferenceEnginesAsync();
        var generalSettings = await ConfigurationService!.GetGeneralSettingsAsync();

        Logger.LogInformation("[OpenAI] Found {Count} inference engines", inferenceEngines.Count());

        foreach (var inferenceEngine in inferenceEngines)
        {
            Logger.LogInformation("[OpenAI] Processing inference engine: {Name} (Id={Id}, Type={Type})",
                inferenceEngine.Name, inferenceEngine.Id, inferenceEngine.Type);

            if (!ConfigurationReadinessService!.IsInferenceEngineReadyAtBoot(inferenceEngine.Id!.Value))
            {
                Logger.LogWarning("[OpenAI] Configuration for Inference Engine {EngineName} is not ready and being skipped", inferenceEngine.Name);
                continue;
            }

            switch (inferenceEngine.Type)
            {
                case InferenceEngineType.OpenAICompatible:
                {
                    Logger.LogInformation("[OpenAI] Registering services for OpenAI-compatible engine: {Name}", inferenceEngine.Name);
                    RegisterSemanticKernelInferenceServices(services, inferenceEngine, generalSettings, Logger);
                    RegisterInferenceEngine(services, inferenceEngine);
                    break;
                }
                case InferenceEngineType.Ollama:
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    private static void RegisterSemanticKernelInferenceServices(
        IServiceCollection services,
        AesirInferenceEngine inferenceEngine,
        AesirGeneralSettings generalSettings,
        ILogger logger)
    {
        var inferenceEngineIdKey = inferenceEngine.Id.ToString();

        var ragEmbeddingInferenceEngineId = generalSettings.RagEmbeddingInferenceEngineId;
        var embeddingModel = generalSettings.RagEmbeddingModel;

        if (ragEmbeddingInferenceEngineId == null)
        {
            logger.LogWarning("[OpenAI] RAG embedding inference engine ID is null - skipping embedding generator registration");
        }
        
        services.AddKeyedSingleton<IChatCompletionServiceFactory>(inferenceEngineIdKey,
            (sp, key) =>
            {
                var openAiClient = sp.GetKeyedService<OpenAIClient>(inferenceEngineIdKey);

                return new ChatCompletionServiceFactory(
                    openAiClient!, sp.GetService<ILoggerFactory>()!);
            });

        if (inferenceEngine.Id == ragEmbeddingInferenceEngineId)
        {
            const string? serviceId = null;
            services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>(serviceId, (serviceProvider, _) =>
            {
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

                var builder = serviceProvider.GetKeyedService<OpenAIClient>(inferenceEngineIdKey)
                    !.GetEmbeddingClient(embeddingModel)
                    .AsIEmbeddingGenerator(1024)
                    .AsBuilder()
                    .UseOpenTelemetry(loggerFactory, null, null);

                if (loggerFactory is not null)
                {
                    builder.UseLogging(loggerFactory);
                }

                return builder.Build();
            });
        }
    }

    /// <summary>
    /// Registers the necessary services for the given inference engine within the service collection.
    /// </summary>
    /// <param name="services">The service collection where the services will be registered.</param>
    /// <param name="inferenceEngine">The inference engine containing configuration and settings required for service registration.</param>
    private static void RegisterInferenceEngine(
        IServiceCollection services,
        AesirInferenceEngine inferenceEngine)
    {
        var inferenceEngineIdKey = inferenceEngine.Id!.Value.ToString();

        // Register Models Service
        services.AddKeyedTransient<IModelsService>(inferenceEngineIdKey, (sp, key) =>
            new ModelsService(
                inferenceEngineIdKey,
                sp.GetRequiredService<ILogger<ModelsService>>(),
                sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
                sp));

        // Register Chat Service
        services.AddKeyedTransient<IChatService>(inferenceEngineIdKey, (sp, key) =>
        {
            var logger = sp.GetRequiredService<ILogger<ChatService>>();
            var kernel = sp.GetRequiredService<Kernel>();
            var kernelPluginService = sp.GetRequiredService<IKernelPluginService>();
            var chatHistoryService = sp.GetRequiredService<IChatHistoryService>();
            var toolCallBroadcaster = sp.GetRequiredService<IToolCallBroadcaster>();
            var inferenceLogService = sp.GetService<IInferenceLogService>();
            var conversationDocumentCollectionService =
                sp.GetService<IConversationDocumentCollectionService>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return new ChatService(
                logger,
                loggerFactory,
                kernel,
                kernelPluginService,
                sp,
                toolCallBroadcaster,
                inferenceLogService,
                inferenceEngineIdKey,
                chatHistoryService,
                conversationDocumentCollectionService
            );
        });

        // Register Vision Service (keyed by engine ID)
        services.AddKeyedTransient<IVisionService>(inferenceEngineIdKey, (sp, key) =>
            new VisionService(
                sp.GetRequiredService<ILogger<VisionService>>(),
                sp));

        // Register OpenAI Client
        var apiKey = inferenceEngine.Configuration!["ApiKey"] ??
                     throw new InvalidOperationException("OpenAI API key not configured");

        var apiCreds = new ApiKeyCredential(apiKey);
        var endPoint = inferenceEngine.Configuration["Endpoint"] ??
                       throw new InvalidOperationException("OpenAI Endpoint not configured");

        if (string.IsNullOrEmpty(endPoint))
        {
            services.AddKeyedSingleton(inferenceEngineIdKey, new OpenAIClient(apiCreds));
        }
        else
        {
            // STOPGAP: Create OpenAI client with custom handler to intercept reasoning_content
            // from llama.cpp/TTRA responses. Remove when OpenAI SDK or SK adds native support.
            services.AddKeyedSingleton(inferenceEngineIdKey, (sp, _) =>
            {
                var logger = sp.GetService<ILogger<ReasoningContentHandler_Stopgap>>();
                var handler = new ReasoningContentHandler_Stopgap(logger)
                {
                    InnerHandler = new HttpClientHandler()
                };

                var httpClient = new HttpClient(handler);
                var transport = new HttpClientPipelineTransport(httpClient);

                return new OpenAIClient(apiCreds, new OpenAIClientOptions
                {
                    Endpoint = new Uri(endPoint),
                    Transport = transport
                });
            });
        }
    }
}
