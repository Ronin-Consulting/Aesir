using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Aesir.Api.Server.Services.Implementations.Standard;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.PgVector;
using Npgsql;
using OllamaSharp;

namespace Aesir.Api.Server.Extensions;

public static class ServiceCollectionExtensions
{
    [Experimental("SKEXP0070")]
    public static IServiceCollection SetupSemanticKernel(this IServiceCollection services, IConfiguration configuration)
    {
        var useOpenAi = configuration.GetValue<bool>("Inference:UseOpenAICompatible");
        var embeddingModelId = useOpenAi
            ? configuration.GetSection("Inference:OpenAI:EmbeddingModel").Value
            : configuration.GetSection("Inference:Ollama:EmbeddingModel").Value;

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();

        services.AddSingleton(dataSourceBuilder.Build());

        var kernelBuilder = services.AddKernel();

        if (useOpenAi)
        {
            kernelBuilder
                .AddOpenAIChatCompletion(
                    modelId: configuration.GetSection("Inference:OpenAI:ChatModels").Get<string[]>()
                        ?.FirstOrDefault() ?? "gpt-4o"
                );

            kernelBuilder.AddOpenAIEmbeddingGenerator(embeddingModelId ?? "text-embedding-3-small");
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

        var vsOptions = new PostgresVectorStoreOptions
        {
            Schema = "aesir",
            EmbeddingGenerator = embeddingGenerator
        };
        services.AddPostgresVectorStore(vsOptions);

        var rcOptions = new PostgresCollectionOptions
        {
            Schema = "aesir",
            EmbeddingGenerator = embeddingGenerator
        };


        services.AddPostgresCollection<Guid, AesirConversationDocumentTextData<Guid>>("aesir_conversation_document",
            rcOptions);
        services.AddPostgresCollection<Guid, AesirGlobalDocumentTextData<Guid>>("aesir_global_document", rcOptions);

        services.AddSingleton(new UniqueKeyGenerator<Guid>(Guid.NewGuid));


        services.AddSingleton<IPdfDataLoaderService<Guid, AesirGlobalDocumentTextData<Guid>>>(serviceProvider =>
        {
            return new PdfDataLoaderService<Guid, AesirGlobalDocumentTextData<Guid>>(
                serviceProvider.GetRequiredService<UniqueKeyGenerator<Guid>>(),
                serviceProvider.GetRequiredService<VectorStoreCollection<Guid, AesirGlobalDocumentTextData<Guid>>>(),
                serviceProvider.GetRequiredService<IChatCompletionService>(),
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
                serviceProvider.GetRequiredService<IChatCompletionService>(),
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
                serviceProvider
                    .GetRequiredService<ILogger<PdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>>>>()
            );
        });

        kernelBuilder.AddVectorStoreTextSearch<AesirConversationDocumentTextData<Guid>>();
        kernelBuilder.AddVectorStoreTextSearch<AesirGlobalDocumentTextData<Guid>>();

        services.AddSingleton<IConversationDocumentCollectionService, ConversationDocumentCollectionService>();
        services.AddSingleton<IGlobalDocumentCollectionService, GlobalDocumentCollectionService>();
        services.AddSingleton<IDocumentCollectionService, AutoDocumentCollectionService>();

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

        return services;
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
internal class GlobalDocumentCollection
{
    public string Name { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public string PluginName { get; set; } = null!;
    public string PluginDescription { get; set; } = null!;
}