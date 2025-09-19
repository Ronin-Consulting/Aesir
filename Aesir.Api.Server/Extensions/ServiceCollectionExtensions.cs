using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Configuration;
using Aesir.Api.Server.Models;
using Aesir.Common.FileTypes;
using Aesir.Api.Server.Services;
using Aesir.Api.Server.Services.Implementations.Standard;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using OllamaSharp;
using OpenAI;
using Qdrant.Client;

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
    /// <param name="aesirConfigProvider">Config provider.</param>
    /// <returns>The service collection for method chaining.</returns>
    [Experimental("SKEXP0070")]
    public static IServiceCollection SetupSemanticKernel(this IServiceCollection services, IConfiguration configuration, IAesirConfigProvider  aesirConfigProvider)
    {
        // TODO if some part of configuration is missing AND we are in db config mode, boot up app anyway
        // so user can complete setup

        // load general settings (from file or db)
        var generalSettings = aesirConfigProvider.GetGeneralSettings();
        var embeddingModel = generalSettings.RagEmbeddingModel ?? 
                             throw new InvalidOperationException("RagEmbeddingMode not configured");
        
        var kernelBuilder = services.AddKernel();
        
        // load inference engines (from file or db)
        var inferenceEngines = aesirConfigProvider.GetInferenceEngines();
        foreach (var inferenceEngine in inferenceEngines)
        {
            var inferenceEngineIdKey = inferenceEngine.Id.ToString();
            
            switch (inferenceEngine.Type)
            {
                case InferenceEngineType.Ollama:
                {
                    kernelBuilder.AddOllamaChatCompletion(serviceId: inferenceEngineIdKey);
                    
                    const string? serviceId = null;
                    services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>(serviceId, (serviceProvider, _) =>
                    {
                        var ollamaClient = services.BuildServiceProvider().GetKeyedService<OllamaApiClient>(inferenceEngineIdKey);
                        ollamaClient.SelectedModel = embeddingModel;
                
                        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

                        var builder = ((IEmbeddingGenerator<string, Embedding<float>>)ollamaClient)
                            .AsBuilder();

                        if (loggerFactory is not null)
                        {
                            builder.UseLogging(loggerFactory);
                        }

                        return builder.Build(serviceProvider);
                    });
                    
                    break;
                }
                case InferenceEngineType.OpenAICompatible:
                {
                    var openAiClient = services.BuildServiceProvider().GetKeyedService<OpenAIClient>(inferenceEngineIdKey);
                    
                    kernelBuilder.AddOpenAIChatCompletion("set-by-agent", openAiClient, inferenceEngineIdKey);
                    kernelBuilder.AddOpenAIEmbeddingGenerator(embeddingModel, openAiClient, 1024, inferenceEngineIdKey);
                    break;
                }
            }
        }
        
        Func<IServiceProvider, QdrantClient>? clientProvider = provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            
            return new QdrantClient(
                host: "qdrant", port: 6334,
                https: false, apiKey: "aesir_3a087fa5640958985025b0a03d2f6b0c80253884c5bd7c05f65f2fdf2404d7ab",
                loggerFactory: loggerFactory);
        };

        Func<IServiceProvider, QdrantCollectionOptions>? optionsProvider = provider => new QdrantCollectionOptions
        {
            EmbeddingGenerator = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>()
        };
        
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
            return new PdfDataLoaderService<Guid, AesirGlobalDocumentTextData<Guid>>(
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
                serviceProvider.GetRequiredService<IConfigurationService>(),
                serviceProvider,
                serviceProvider
                    .GetRequiredService<ILogger<PdfDataLoaderService<Guid, AesirGlobalDocumentTextData<Guid>>>>()
            );
        });

        services.AddSingleton<IPdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>(serviceProvider =>
        {
            return new PdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>(
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
                serviceProvider.GetRequiredService<IConfigurationService>(),
                serviceProvider,
                serviceProvider
                    .GetRequiredService<ILogger<PdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>>()
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
                serviceProvider.GetRequiredService<IConfigurationService>(),
                serviceProvider,
                serviceProvider
                    .GetRequiredService<ILogger<ImageDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>>()
            );
        });
        
        services.AddSingleton<ITextFileLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>(serviceProvider =>
        {
            return new TextFileLoaderService<Guid, AesirConversationDocumentTextData<Guid>>(
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

                    if (request.TextFileFileName!.GetMimeType() == FileTypeManager.MimeTypes.Json)
                    {
                        return new AesirConversationJsonTextData<Guid>
                        {
                            ConversationId = conversationId,
                            Key = Guid.Empty // This will be replaced in the service
                        };
                    }
                    
                    if (request.TextFileFileName!.GetMimeType() == FileTypeManager.MimeTypes.Xml)
                    {
                        return new AesirConversationXmlTextData<Guid>()
                        {
                            ConversationId = conversationId,
                            Key = Guid.Empty // This will be replaced in the service
                        };
                    }
                    
                    if (request.TextFileFileName!.GetMimeType() == FileTypeManager.MimeTypes.Csv)
                    {
                        return new AesirConversationCsvTextData<Guid>()
                        {
                            ConversationId = conversationId,
                            Key = Guid.Empty // This will be replaced in the service
                        };
                    }

                    return new AesirConversationDocumentTextData<Guid>
                    {
                        ConversationId = conversationId,
                        Key = Guid.Empty // This will be replaced in the service
                    };
                },
                serviceProvider.GetRequiredService<IConfigurationService>(),
                serviceProvider,
                serviceProvider
                    .GetRequiredService<ILogger<TextFileLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>>()
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
        services.AddSingleton<IConfiguredKernelFactory, ConfiguredKernelFactory>();
        
        return services;
    }
}


public interface IConfiguredKernelFactory
{
    Kernel CreateKernel();
}

internal class ConfiguredKernelFactory(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<ConfiguredKernelFactory> logger)
    : IConfiguredKernelFactory
{
    private readonly GlobalDocumentCollection[] _collections = 
        configuration.GetSection("GlobalDocumentCollections").Get<GlobalDocumentCollection[]>() ?? [];

    public Kernel CreateKernel()
    {
        // Get a fresh kernel from DI
        var kernel = serviceProvider.GetRequiredService<Kernel>();
        
        // Add global document collection plugins
        var globalDocumentService = serviceProvider.GetRequiredService<IGlobalDocumentCollectionService>();
        
        foreach (var collection in _collections.Where(c => c.IsEnabled))
        {
            try
            {
                var args = GlobalDocumentCollectionArgs.Default;
                args.SetCategoryId(collection.Name);
                args["PluginName"] = collection.PluginName;
                args["PluginDescription"] = collection.PluginDescription;

                var plugin = globalDocumentService.GetKernelPlugin(args);
                kernel.Plugins.Add(plugin);
                
                logger.LogDebug("Added plugin {PluginName} to kernel", collection.PluginName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to add plugin {PluginName} to kernel", collection.PluginName);
            }
        }

        return kernel;
    }
}


/// <summary>
/// Represents configuration for a global document collection.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class GlobalDocumentCollection
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