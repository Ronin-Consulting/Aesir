using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Aesir.Api.Server.Services.Implementations.Standard;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Data;
using Npgsql;
using OllamaSharp;

namespace Aesir.Api.Server.Extensions;

public static class ServiceCollectionExtensions
{
    [Experimental("SKEXP0070")]
    public static IServiceCollection SetupSemanticKernel(this IServiceCollection services, IConfiguration configuration)
    {
        // Enable model diagnostics with sensitive data.
        AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);
        
        // Choose the appropriate embedding model based on configuration
        var useOpenAi = configuration.GetValue<bool>("Inference:UseOpenAICompatible");
        var embeddingModelId = useOpenAi
            ? configuration.GetSection("Inference:OpenAI:EmbeddingModel").Value
            : configuration.GetSection("Inference:Ollama:EmbeddingModel").Value;
        
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        
        services.AddSingleton(dataSourceBuilder.Build());
        
        // Put back in once the bug is fixed in semantic kernel
        // 
        // var vsOptions = new PostgresVectorStoreOptions
        // {
        //     Schema = "aesir"
        // };
        services.AddPostgresVectorStore();

        // Put back in once the bug is fixed in semantic kernel
        // 
        // var rcOptions = new PostgresVectorStoreRecordCollectionOptions<AesirTextData<Guid>>
        // {
        //     Schema = "aesir"
        // };
        
        services.AddPostgresVectorStoreRecordCollection<Guid, AesirTextData<Guid>>("aesir-documents");
        
        services.AddSingleton(new UniqueKeyGenerator<Guid>(Guid.NewGuid));
        services.AddSingleton<IPdfDataLoader, PdfDataLoader<Guid>>();

        // Configure kernel based on backend
        var kernelBuilder = services.AddKernel();
        
        if (useOpenAi)
        {
            var apiKey = configuration["Inference:OpenAI:ApiKey"] ?? 
                         throw new InvalidOperationException("OpenAI API key not configured");
            
            // Configure OpenAI for embeddings and chat
            kernelBuilder
                .AddOpenAIChatCompletion(
                    modelId: configuration.GetSection("Inference:OpenAI:ChatModels").Get<string[]>()?.FirstOrDefault() ?? "gpt-4o"
                )
                .AddOpenAITextEmbeddingGeneration(
                    modelId: embeddingModelId ?? "text-embedding-3-small");
        }
        else
        {
            // Configure Ollama for embeddings and chat
            var ollamaClient = services.BuildServiceProvider().GetRequiredService<OllamaApiClient>();
            ollamaClient.SelectedModel = embeddingModelId!;
            
            kernelBuilder
                .AddOllamaChatCompletion()
                .AddOllamaTextEmbeddingGeneration(ollamaClient);
        }
        
        // Add vector search
        kernelBuilder.AddVectorStoreTextSearch<AesirTextData<Guid>>();

        var vectorStoreTextSearch = services.BuildServiceProvider().GetRequiredService<VectorStoreTextSearch<AesirTextData<Guid>>>();
        var missionPlanRagPlugin = vectorStoreTextSearch.CreateWithGetTextSearchResults(
            "SearchMissionPlanDetails",
            "A natural language search returning details about mission plans."
        );
        kernelBuilder.Plugins.Add(missionPlanRagPlugin);
        
        return services;
    }
}