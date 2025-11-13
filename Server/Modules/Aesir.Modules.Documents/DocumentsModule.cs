using System.Diagnostics.CodeAnalysis;
using Aesir.Common.FileTypes;
using Aesir.Infrastructure.Models;
using Aesir.Infrastructure.Modules;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Documents.Models;
using Aesir.Modules.Documents.Services.DocumentCollections;
using Aesir.Modules.Documents.Services.DocumentLoaders;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;

namespace Aesir.Modules.Documents;

/// <summary>
/// Documents module providing document collection, processing, and RAG functionality.
/// </summary>
[Experimental("SKEXP0001")]
public class DocumentsModule : ModuleBase
{
    public DocumentsModule(ILogger<DocumentsModule> logger) : base(logger)
    {
    }

    public override string Name => "Documents";

    public override string Version => "1.0.0";

    public override string? Description => "Provides document collection management, document loaders (PDF, text, images), and RAG functionality";

    public override async Task RegisterServicesAsync(IServiceCollection services)
    {
        await Task.CompletedTask;
        
        Log("Registering document services...");

        // Set up Qdrant vector store
        Func<IServiceProvider, QdrantClient>? clientProvider = provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var configuration = provider.GetRequiredService<IConfiguration>();

            var host = configuration["Qdrant:Host"] ?? "qdrant";
            var port = int.Parse(configuration["Qdrant:Port"] ?? "6334");
            var apiKey = configuration["Qdrant:ApiKey"] ?? "aesir_3a087fa5640958985025b0a03d2f6b0c80253884c5bd7c05f65f2fdf2404d7ab";

            return new QdrantClient(
                host: host,
                port: port,
                https: false,
                apiKey: apiKey,
                loggerFactory: loggerFactory);
        };

        Func<IServiceProvider, QdrantCollectionOptions>? optionsProvider = provider => new QdrantCollectionOptions
        {
            EmbeddingGenerator = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>()
        };

        // Register Qdrant collections
        services.AddKeyedQdrantCollection<Guid, AesirConversationDocumentTextData<Guid>>(
            serviceKey: null,
            name: "aesir_conversation_document",
            clientProvider,
            optionsProvider);

        services.AddKeyedQdrantCollection<Guid, AesirGlobalDocumentTextData<Guid>>(
            serviceKey: null,
            name: "aesir_global_document",
            clientProvider,
            optionsProvider);

        // Register unique key generator
        services.AddSingleton(new UniqueKeyGenerator<Guid>(Guid.NewGuid));

        // Register PDF data loader for global documents
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
                        Key = Guid.Empty
                    };
                },
                serviceProvider.GetRequiredService<IVisionService>(),
                serviceProvider.GetRequiredService<IConfigurationService>(),
                serviceProvider,
                serviceProvider.GetRequiredService<ILogger<PdfDataLoaderService<Guid, AesirGlobalDocumentTextData<Guid>>>>()
            );
        });

        // Register PDF data loader for conversation documents
        services.AddSingleton<IPdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>(serviceProvider =>
        {
            return new PdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>(
                serviceProvider.GetRequiredService<UniqueKeyGenerator<Guid>>(),
                serviceProvider.GetRequiredService<VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>>>(),
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
                        Key = Guid.Empty
                    };
                },
                serviceProvider.GetRequiredService<IVisionService>(),
                serviceProvider.GetRequiredService<IConfigurationService>(),
                serviceProvider,
                serviceProvider.GetRequiredService<ILogger<PdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>>()
            );
        });

        // Register Image data loader
        services.AddSingleton<IImageDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>(serviceProvider =>
        {
            return new ImageDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>(
                serviceProvider.GetRequiredService<UniqueKeyGenerator<Guid>>(),
                serviceProvider.GetRequiredService<VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>>>(),
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
                        Key = Guid.Empty
                    };
                },
                serviceProvider.GetRequiredService<IVisionService>(),
                serviceProvider.GetRequiredService<IConfigurationService>(),
                serviceProvider,
                serviceProvider.GetRequiredService<ILogger<ImageDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>>()
            );
        });

        // Register Text file loader
        services.AddSingleton<ITextFileLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>(serviceProvider =>
        {
            return new TextFileLoaderService<Guid, AesirConversationDocumentTextData<Guid>>(
                serviceProvider.GetRequiredService<UniqueKeyGenerator<Guid>>(),
                serviceProvider.GetRequiredService<VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>>>(),
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
                            Key = Guid.Empty
                        };
                    }

                    if (request.TextFileFileName!.GetMimeType() == FileTypeManager.MimeTypes.Xml)
                    {
                        return new AesirConversationXmlTextData<Guid>()
                        {
                            ConversationId = conversationId,
                            Key = Guid.Empty
                        };
                    }

                    if (request.TextFileFileName!.GetMimeType() == FileTypeManager.MimeTypes.Csv)
                    {
                        return new AesirConversationCsvTextData<Guid>()
                        {
                            ConversationId = conversationId,
                            Key = Guid.Empty
                        };
                    }

                    return new AesirConversationDocumentTextData<Guid>
                    {
                        ConversationId = conversationId,
                        Key = Guid.Empty
                    };
                },
                serviceProvider.GetRequiredService<IConfigurationService>(),
                serviceProvider,
                serviceProvider.GetRequiredService<ILogger<TextFileLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>>()
            );
        });

        // Register vector store text search
        // Note: These need to be added to the kernel builder, which is done in the application
        // For now, we register them here and they'll be picked up by the kernel
        services.AddVectorStoreTextSearch<AesirConversationDocumentTextData<Guid>>();
        services.AddVectorStoreTextSearch<AesirGlobalDocumentTextData<Guid>>();

        // Register document collection services
        services.AddSingleton<IConversationDocumentCollectionService, ConversationDocumentCollectionService>();
        services.AddSingleton<IGlobalDocumentCollectionService, GlobalDocumentCollectionService>();
        services.AddSingleton<IDocumentCollectionService, AutoDocumentCollectionService>();

        services.AddSingleton<IConfiguredKernelFactory, ConfiguredKernelFactory>();
        
        Log("Document services registered successfully");
    }
}

public interface IConfiguredKernelFactory
{
    Task<Kernel> CreateKernelAsync();
}

internal class ConfiguredKernelFactory(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<ConfiguredKernelFactory> logger)
    : IConfiguredKernelFactory
{
    private readonly GlobalDocumentCollection[] _collections = 
        configuration.GetSection("GlobalDocumentCollections").Get<GlobalDocumentCollection[]>() ?? [];

    public async Task<Kernel> CreateKernelAsync()
    {
        // Get a fresh kernel from DI
        var kernel = serviceProvider.GetRequiredService<Kernel>();
        
        // Add global document collection plugins
        var kernalPluginService = serviceProvider.GetRequiredService<IKernelPluginService>();
        
        foreach (var collection in _collections.Where(c => c.IsEnabled))
        {
            try
            {
                var args = GlobalDocumentCollectionArgs.Default;
                args.SetCategoryId(collection.Name);
                args["PluginName"] = collection.PluginName;
                args["PluginDescription"] = collection.PluginDescription;

                var plugin = await kernalPluginService.GetKernelPluginAsync(args);
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
