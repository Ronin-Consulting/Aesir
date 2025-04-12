using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Aesir.Api.Server.Services.Implementations.Standard;
using Microsoft.SemanticKernel;
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
        
        var embeddingModelId = configuration.GetSection("Inference:EmbeddingModel").Value;
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

        var ollamaClient = services.BuildServiceProvider().GetRequiredService<OllamaApiClient>();
        ollamaClient.SelectedModel = embeddingModelId!;
        
        var kernelBuilder = services.AddKernel()
            .AddOllamaChatCompletion()
            .AddOllamaTextEmbeddingGeneration(ollamaClient)
            .AddVectorStoreTextSearch<AesirTextData<Guid>>();

        var vectorStoreTextSearch = services.BuildServiceProvider().GetRequiredService<VectorStoreTextSearch<AesirTextData<Guid>>>();
        var missionPlanRagPlugin =  vectorStoreTextSearch.CreateWithGetTextSearchResults(
            "SearchMissionPlanDetails",
            "A natural language search returning details about mission plans."
        );
        kernelBuilder.Plugins.Add(missionPlanRagPlugin);
        
        return services;
    }
}