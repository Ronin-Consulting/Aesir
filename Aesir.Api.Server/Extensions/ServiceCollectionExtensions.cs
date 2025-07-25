using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Aesir.Api.Server.Services.Implementations.Standard;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Web.Google;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Plugins.Web;
using OllamaSharp;
using Qdrant.Client;
using SamurAI = Aesir.Api.Server.Services.Implementations.Samurai;

namespace Aesir.Api.Server.Extensions;

/// <summary>
/// Provides extension methods for configuring dependency injection services including Semantic Kernel and vector stores.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures Semantic Kernel services including chat completions, embeddings, vector stores, and document collections.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    [Experimental("SKEXP0070")]
    public static IServiceCollection SetupSemanticKernel(this IServiceCollection services, IConfiguration configuration)
    {
        var useOpenAi = configuration.GetValue<bool>("Inference:UseOpenAICompatible");
        var embeddingModelId = useOpenAi
            ? configuration.GetSection("Inference:OpenAI:EmbeddingModel").Value
            : configuration.GetSection("Inference:Ollama:EmbeddingModel").Value;
        
        var kernelBuilder = services.AddKernel();

        if (useOpenAi)
        {
            kernelBuilder
                .AddOpenAIChatCompletion(
                    modelId: configuration.GetSection("Inference:OpenAI:ChatModels").Get<string[]>()
                        ?.FirstOrDefault() ?? "gpt-4o"
                );

            kernelBuilder.AddOpenAIEmbeddingGenerator(embeddingModelId ?? "text-embedding-3-large", dimensions: 1024);
        }
        else
        {
            var ollamaClient = services.BuildServiceProvider().GetRequiredService<OllamaApiClient>();
            ollamaClient.SelectedModel = embeddingModelId!;

            kernelBuilder
                .AddOllamaChatCompletion();

            kernelBuilder.AddOllamaEmbeddingGenerator(ollamaClient);
        }
        
        var embeddingGenerator = services.BuildServiceProvider()
            .GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        
        var collectionOptions = new QdrantCollectionOptions
        {
            EmbeddingGenerator = embeddingGenerator
        };
        
        Func<IServiceProvider, QdrantClient>? clientProvider = provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            
            return new QdrantClient(
                host: "qdrant", port: 6334,
                https: false, apiKey: "aesir_3a087fa5640958985025b0a03d2f6b0c80253884c5bd7c05f65f2fdf2404d7ab",
                loggerFactory: loggerFactory);
        };

        Func<IServiceProvider, QdrantCollectionOptions>? optionsProvider = provider => collectionOptions;
        
        kernelBuilder.Services.AddKeyedQdrantCollection<Guid, AesirConversationDocumentTextData<Guid>>(
            serviceKey: null,
            name: "aesir_conversation_document",
            clientProvider,
            optionsProvider);
        
        kernelBuilder.Services.AddKeyedQdrantCollection<Guid, AesirGlobalDocumentTextData<Guid>>(
            serviceKey: null,
            name: "aesir_global_document",
            clientProvider,
            optionsProvider);
        
        services.AddSingleton(new UniqueKeyGenerator<Guid>(Guid.NewGuid));
        
        services.AddSingleton<IPdfDataLoaderService<Guid, AesirGlobalDocumentTextData<Guid>>>(serviceProvider =>
        {
            return new SamurAI.PdfDataLoaderService<Guid, AesirGlobalDocumentTextData<Guid>>(
                serviceProvider.GetRequiredService<UniqueKeyGenerator<Guid>>(),
                serviceProvider.GetRequiredService<VectorStoreCollection<Guid, AesirGlobalDocumentTextData<Guid>>>(),
                serviceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
                (rawContent, request) =>
                {
                    var metadata = request.Metadata ?? new Dictionary<string, object>();
                    var category = metadata.TryGetValue("CategoryId", out var categoryObj)
                        ? categoryObj.ToString()
                        : null;

                    return new AesirGlobalDocumentTextData<Guid>
                    {
                        Category = category,
                        Key = Guid.Empty // This will be replaced in the service
                    };
                },
                serviceProvider.GetRequiredService<IVisionService>(),
                serviceProvider.GetRequiredService<IModelsService>(),
                serviceProvider
                    .GetRequiredService<ILogger<SamurAI.PdfDataLoaderService<Guid, AesirGlobalDocumentTextData<Guid>>>>()
            );
        });

        services.AddSingleton<IPdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>(serviceProvider =>
        {
            return new SamurAI.PdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>(
                serviceProvider.GetRequiredService<UniqueKeyGenerator<Guid>>(),
                serviceProvider
                    .GetRequiredService<VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>>>(),
                serviceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
                (rawContent, request) =>
                {
                    var metadata = request.Metadata ?? new Dictionary<string, object>();
                    var conversationId = metadata.TryGetValue("ConversationId", out var conversationIdObj)
                        ? conversationIdObj.ToString()
                        : null;

                    return new AesirConversationDocumentTextData<Guid>
                    {
                        ConversationId = conversationId,
                        Key = Guid.Empty // This will be replaced in the service
                    };
                },
                serviceProvider.GetRequiredService<IVisionService>(),
                serviceProvider.GetRequiredService<IModelsService>(),
                serviceProvider
                    .GetRequiredService<ILogger<SamurAI.PdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>>()
            );
        });

        services.AddSingleton<IImageDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>(serviceProvider =>
        {
            return new ImageDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>(
                serviceProvider.GetRequiredService<UniqueKeyGenerator<Guid>>(),
                serviceProvider
                    .GetRequiredService<VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>>>(),
                serviceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
                (rawContent, request) =>
                {
                    var metadata = request.Metadata ?? new Dictionary<string, object>();
                    var conversationId = metadata.TryGetValue("ConversationId", out var conversationIdObj)
                        ? conversationIdObj.ToString()
                        : null;

                    return new AesirConversationDocumentTextData<Guid>
                    {
                        ConversationId = conversationId,
                        Key = Guid.Empty // This will be replaced in the service
                    };
                },
                serviceProvider.GetRequiredService<IVisionService>(),
                serviceProvider.GetRequiredService<IModelsService>(),
                serviceProvider
                    .GetRequiredService<ILogger<ImageDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>>()
            );
        });
        
        kernelBuilder.AddVectorStoreTextSearch<AesirConversationDocumentTextData<Guid>>();
        kernelBuilder.AddVectorStoreTextSearch<AesirGlobalDocumentTextData<Guid>>();

        services.AddSingleton<IConversationDocumentCollectionService, ConversationDocumentCollectionService>();
        services.AddSingleton<IGlobalDocumentCollectionService, GlobalDocumentCollectionService>();
        services.AddSingleton<IDocumentCollectionService, AutoDocumentCollectionService>();
        
        services.AddSingleton<IFunctionInvocationFilter, InferenceLoggingService>();
        services.AddSingleton<IPromptRenderFilter, InferenceLoggingService>();
        services.AddSingleton<IAutoFunctionInvocationFilter, InferenceLoggingService>();
        
        // global documents setup
        var globalDocumentCollections =
            configuration.GetSection("GlobalDocumentCollections").Get<GlobalDocumentCollection[]>() ?? [];

        var globalDocumentCollectionService =
            services.BuildServiceProvider().GetRequiredService<IGlobalDocumentCollectionService>();

        foreach (var globalDocumentCollection in globalDocumentCollections)
        {
            if(!globalDocumentCollection.IsEnabled) continue;
            
            var args = GlobalDocumentCollectionArgs.Default;

            args.SetCategoryId(globalDocumentCollection.Name);

            args["PluginName"] = globalDocumentCollection.PluginName;
            args["PluginDescription"] = globalDocumentCollection.PluginDescription;

            var plugin = globalDocumentCollectionService.GetKernelPlugin(args);

            kernelBuilder.Plugins.Add(plugin);
        }

        var googleConnector = new GoogleConnector(
            searchEngineId: "64cf6ca85e9454a44", //Environment.GetEnvironmentVariable("CSE_ID"),
            apiKey: "AIzaSyByEQBfXtNjdxIGlpeLRz0C1isORMnsHNU"); //Environment.GetEnvironmentVariable("GOOGLE_KEY"))

        var webSearchPlugin = new WebSearchEnginePlugin(googleConnector);
        
        var methods = webSearchPlugin.GetType().GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        
        // only use the Search Results one because it has metadata...
        var functions = 
            (from method in methods where method.Name.StartsWith("GetSearchResults") 
                select KernelFunctionFactory.CreateFromMethod(method, webSearchPlugin)).ToList();

        kernelBuilder.Plugins.Add(
            KernelPluginFactory.CreateFromFunctions("Web", functions)
        );

        return services;
    }
}

/// <summary>
/// Represents configuration for a global document collection.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
internal class GlobalDocumentCollection
{
    /// <summary>
    /// Gets or sets the name of the document collection.
    /// </summary>
    public string Name { get; set; } = null!;
    /// <summary>
    /// Gets or sets a value indicating whether the document collection is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
    /// <summary>
    /// Gets or sets the plugin name for the document collection.
    /// </summary>
    public string PluginName { get; set; } = null!;
    /// <summary>
    /// Gets or sets the plugin description for the document collection.
    /// </summary>
    public string PluginDescription { get; set; } = null!;
}