using System.Diagnostics.CodeAnalysis;
using Aesir.Common.Models;
using Aesir.Infrastructure.Models;
using Aesir.Infrastructure.Modules;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Inference.Ollama.Services;
using Aesir.Modules.Inference.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using OllamaSharp;

namespace Aesir.Modules.Inference.Ollama;

/// <summary>
/// Represents a module that provides Ollama-specific services for AI inference.
/// </summary>
/// <remarks>
/// Extends the <see cref="ModuleBase"/> to allow the registration and initialization
/// of services related to the Ollama inference engine. This module checks the configuration
/// readiness before proceeding with its service registrations to ensure proper setup and avoid dependency errors.
/// </remarks>
[Experimental("SKEXP0070")]
public class OllamaInferenceModule : ModuleBase
{
    public OllamaInferenceModule(ILogger<OllamaInferenceModule> logger) : base(logger)
    {
    }

    /// <summary>
    /// Gets the name of the module.
    /// </summary>
    /// <remarks>
    /// Represents the name uniquely identifying the module within the application.
    /// This is typically used for module discovery, logging, or configuration purposes.
    /// </remarks>
    public override string Name => "Ollama Inference Engine";

    /// <summary>
    /// Gets the version of the module.
    /// </summary>
    /// <remarks>
    /// The Version property specifies the current version of the module as a string.
    /// This can be used to track compatibility and ensure the correct module version is in use.
    /// </remarks>
    public override string Version => "1.0.0";

    /// <summary>
    /// Provides a description of the module.
    /// </summary>
    /// <remarks>
    /// The Description property gives a brief overview of the module's functionality or purpose.
    /// It is a concise summary intended for identifying the module's role within the system.
    /// </remarks>
    public override string? Description => "Provides Ollama-specific services for AI inference";

    /// <summary>
    /// Asynchronously registers the services required for the initialization of the inference module.
    /// </summary>
    /// <param name="services">The service collection to which the necessary services are added.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task RegisterServicesAsync(IServiceCollection services)
    {
        // registering any of these things is pointless if we are not fully ready with inference engines and embedding
        // setup, and causes more weirdo dependency errors
        if (!ConfigurationReadinessService!.IsReadyAtBoot)
            return;

        var inferenceEngines = await ConfigurationService!.GetInferenceEnginesAsync();
        var generalSettings = await ConfigurationService!.GetGeneralSettingsAsync();

        foreach (var inferenceEngine in inferenceEngines)
        {
            if (!ConfigurationReadinessService!.IsInferenceEngineReadyAtBoot(inferenceEngine.Id!.Value))
            {
                Logger.LogWarning("Configuration for Inference Engine {EngineName} is not ready and being skipped for initialization", inferenceEngine.Name);
                continue;
            }
            
            switch (inferenceEngine.Type)
            {
                case InferenceEngineType.Ollama:
                {
                    RegisterSemanticKernelInferenceServices(services, inferenceEngine, generalSettings);
                    RegisterInferenceEngine(services, inferenceEngine);
                    
                    break;
                }
                case InferenceEngineType.OpenAICompatible:
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// Registers Semantic Kernel-specific inference services for a given inference engine
    /// and general settings configuration.
    /// </summary>
    /// <param name="services">The service collection in which to register the inference services.</param>
    /// <param name="inferenceEngine">The configuration of the inference engine to initialize services for.</param>
    /// <param name="generalSettings">The general settings utilized for configuring the inference engine.</param>
    private static void RegisterSemanticKernelInferenceServices(IServiceCollection services,
        AesirInferenceEngine inferenceEngine,
        AesirGeneralSettings generalSettings)
    {
        var inferenceEngineIdKey = inferenceEngine.Id.ToString();

        var ragEmbeddingInferenceEngineId = generalSettings.RagEmbeddingInferenceEngineId;
        var embeddingModel = generalSettings.RagEmbeddingModel;

        if (ragEmbeddingInferenceEngineId == null)
        {
            // Note: This is a static method, so we can't use instance Logger. This will be addressed in Phase 4.
            Console.Write("Configuration for RAG embedding inference engine is not ready and being skipped for initialization");
        }

        services.AddOllamaChatCompletion(null, inferenceEngineIdKey);

        services.AddKeyedSingleton<IChatCompletionServiceFactory>(inferenceEngineIdKey,
            (sp, key) => new ChatCompletionServiceFactory(sp, inferenceEngineIdKey!));

        if (inferenceEngine.Id == ragEmbeddingInferenceEngineId)
        {
            const string? serviceId = null;
            services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>(serviceId, (serviceProvider, _) =>
            {
                var ollamaClient = serviceProvider.GetKeyedService<OllamaApiClient>(inferenceEngineIdKey);
                ollamaClient!.SelectedModel = embeddingModel!;

                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

                var builder = ((IEmbeddingGenerator<string, Embedding<float>>)ollamaClient)
                    .AsBuilder();

                if (loggerFactory is not null)
                {
                    builder.UseLogging(loggerFactory);
                }

                return builder.Build(serviceProvider);
            });
        }
    }

    /// <summary>
    /// Configures and registers services specific to the provided inference engine
    /// in the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection used for dependency injection.</param>
    /// <param name="inferenceEngine">The configuration settings for the inference engine.</param>
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
                sp.GetRequiredService<IConfiguration>(),
                sp));

        // Register Chat Service
        services.AddKeyedTransient<IChatService>(inferenceEngineIdKey, (sp, key) =>
        {
            var logger = sp.GetRequiredService<ILogger<ChatService>>();
            var kernel = sp.GetRequiredService<Kernel>();
            var kernelPluginService = sp.GetRequiredService<IKernelPluginService>();
            var chatHistoryService = sp.GetRequiredService<IChatHistoryService>();
            var conversationDocumentCollectionService =
                sp.GetRequiredService<IConversationDocumentCollectionService>();

            var ollamaApiClient = sp.GetRequiredKeyedService<OllamaApiClient>(inferenceEngineIdKey);

            var enableThinking = bool.Parse(inferenceEngine.Configuration["EnableChatModelThinking"] ?? "false");

            return new ChatService(
                logger,
                ollamaApiClient,
                kernel,
                kernelPluginService,
                sp,
                inferenceEngineIdKey,
                chatHistoryService,
                conversationDocumentCollectionService,
                enableThinking
            );
        });

        // Register Vision Service
        services.AddTransient<IVisionService, VisionService>();

        // Register HTTP Client for Ollama API
        var ollamaClientName = $"OllamaApiClient-{inferenceEngineIdKey}";
        services.AddHttpClient(ollamaClientName, client =>
            {
                var endpoint = inferenceEngine.Configuration["Endpoint"] ??
                               throw new InvalidOperationException("Ollama Endpoint not configured");
                client.BaseAddress = new Uri($"{endpoint}/api");
                client.Timeout = TimeSpan.FromMinutes(10); // Long timeout for model operations
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        services.AddTransient<LoggingHttpMessageHandler>();

        // Register Ollama API Client
        services.AddKeyedTransient<OllamaApiClient>(inferenceEngineIdKey, (sp, key) =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(ollamaClientName);
            return new OllamaApiClient(httpClient);
        });
    }
}

/// <summary>
/// A logging HTTP message handler that intercepts and logs HTTP requests and responses
/// traversing through the HTTP message pipeline.
/// </summary>
/// <remarks>
/// This class is designed to assist in debugging and monitoring by capturing key details,
/// such as the HTTP method, request URI, and response status code. The captured information
/// is logged using the application's logging infrastructure.
/// </remarks>
public class LoggingHttpMessageHandler : DelegatingHandler
{
    /// <summary>
    /// Captures and writes log messages about the HTTP requests and responses handled
    /// by the <see cref="LoggingHttpMessageHandler"/>.
    /// </summary>
    /// <remarks>
    /// This logger is used to record information such as the HTTP request method, URI, and the response status code.
    /// It helps in debugging and monitoring the behavior of HTTP communications within the application.
    /// </remarks>
    private readonly ILogger<LoggingHttpMessageHandler> _logger;

    /// <summary>
    /// A logging HTTP message handler that intercepts outbound HTTP requests and responses
    /// to log related information, aiding in debugging and monitoring.
    /// </summary>
    /// <remarks>
    /// Logs the HTTP request method and URI before sending the request,
    /// and the response status code after receiving the response. Integrates with
    /// the application's logging infrastructure by leveraging the provided
    /// <see cref="ILogger"/> implementation.
    /// </remarks>
    public LoggingHttpMessageHandler(ILogger<LoggingHttpMessageHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sends an HTTP request asynchronously and logs both the request and response details.
    /// </summary>
    /// <param name="request">The HTTP request message to send.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, containing the HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Sending HTTP request: {Method} {Uri}", request.Method, request.RequestUri);

        var response = await base.SendAsync(request, cancellationToken);

        _logger.LogDebug("Received HTTP response: {StatusCode}", response.StatusCode);

        return response;
    }
}