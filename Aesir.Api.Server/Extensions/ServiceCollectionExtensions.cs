using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Aesir.Api.Server.Services.Implementations.Standard;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.PgVector;
using Microsoft.SemanticKernel.Data;
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

        var embeddingGenerator = services.BuildServiceProvider().GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

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

        
        services.AddPostgresCollection<Guid, AesirConversationDocumentTextData<Guid>>("aesir_conversation_documents", rcOptions);
        services.AddPostgresCollection<Guid, AesirGlobalDocumentTextData<Guid>>("aesir_global_documents", rcOptions);
        
        services.AddSingleton(new UniqueKeyGenerator<Guid>(Guid.NewGuid));
        services.AddSingleton<IPdfDataLoaderService, PdfDataLoaderService<Guid>>();
        
        kernelBuilder.AddVectorStoreTextSearch<AesirConversationDocumentTextData<Guid>>();
        kernelBuilder.AddVectorStoreTextSearch<AesirGlobalDocumentTextData<Guid>>();

        // global documents setup
        var globalDocumentCollections = 
            configuration.GetSection("GlobalDocumentCollections").Get<GlobalDocumentCollection[]>() ?? [];
       
        var globalDocumentTextSearch = services.BuildServiceProvider()
            .GetRequiredService<VectorStoreTextSearch<AesirGlobalDocumentTextData<Guid>>>();
        
        foreach (var globalDocumentCollection in globalDocumentCollections)
        {
            var categoryFilter = new TextSearchFilter();
            categoryFilter.Equality(nameof(AesirGlobalDocumentTextData<Guid>.Category), globalDocumentCollection.Name);
            var globalDocumentTextSearchOptions = new TextSearchOptions
            {
                Top = 5,
                Filter = categoryFilter
            };
            
            var globalDocumentSearchPlugin = globalDocumentTextSearch
                .CreateGetTextSearchResults(searchOptions: globalDocumentTextSearchOptions);
            
            kernelBuilder.Plugins.Add(KernelPluginFactory.CreateFromFunctions(
                globalDocumentCollection.PluginName, 
                globalDocumentCollection.PluginDescription, 
                [globalDocumentSearchPlugin]
            ));
        }
        
        return services;
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
internal class GlobalDocumentCollection
{
    public string Name { get; set; } = null!;
    public string PluginName { get; set; } = null!;
    public string PluginDescription { get; set; } = null!;
}
