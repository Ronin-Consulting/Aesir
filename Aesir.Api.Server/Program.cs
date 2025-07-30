using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Data;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Services;
using Aesir.Api.Server.Services.Implementations.Onnx;
using Aesir.Api.Server.Services.Implementations.Standard;
using FluentMigrator.Runner;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OllamaSharp;
using OpenAI;
using AesirOllama = Aesir.Api.Server.Services.Implementations.Ollama;
using AesirOpenAI = Aesir.Api.Server.Services.Implementations.OpenAI;

namespace Aesir.Api.Server;

public class Program
{
    [Experimental("SKEXP0070")]
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        var useOpenAi = builder.Configuration.GetValue<bool>("Inference:UseOpenAICompatible");

        if (useOpenAi)
        {
            // should be transient to always get fresh kernel
            builder.Services.AddTransient<IModelsService, AesirOpenAI.ModelsService>();
            builder.Services.AddTransient<IChatService, AesirOpenAI.ChatService>();

            builder.Services.AddTransient<AesirOpenAI.VisionModelConfig>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                return new AesirOpenAI.VisionModelConfig
                {
                    ModelId = configuration.GetValue<string>("Inference:OpenAI:VisionModel") ?? "NoVisionModel",
                };
            });
            // should be transient so during dispose we unload model
            builder.Services.AddTransient<IVisionService, AesirOpenAI.VisionService>();
            
            var apiKey = builder.Configuration["Inference:OpenAI:ApiKey"] ??
                throw new InvalidOperationException("OpenAI API key not configured");

            var apiCreds = new ApiKeyCredential(apiKey);
            var endPoint = builder.Configuration["Inference:OpenAI:Endpoint"];

            if (string.IsNullOrEmpty(endPoint))
                builder.Services.AddSingleton(new OpenAIClient(apiCreds));
            else
            {
                builder.Services.AddSingleton(new OpenAIClient(apiCreds, new OpenAIClientOptions()
                {
                    Endpoint = new Uri(endPoint)
                }));
            }
        }
        else
        {
            // should be transient to always get fresh kernel
            builder.Services.AddTransient<IModelsService, AesirOllama.ModelsService>();
            builder.Services.AddTransient<IChatService>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<AesirOllama.ChatService>>();
                var ollamApiClient = serviceProvider.GetRequiredService<OllamaApiClient>();
                var kernel = serviceProvider.GetRequiredService<Kernel>();
                var chatCompletionService = serviceProvider.GetRequiredService<IChatCompletionService>();
                var chatHistoryService = serviceProvider.GetRequiredService<IChatHistoryService>();
                var conversationDocumentCollectionService = serviceProvider.GetRequiredService<IConversationDocumentCollectionService>();

                var enableThinking = builder.Configuration.GetValue<bool?>("Inference:Ollama:EnableChatModelThinking");
                
                return new AesirOllama.ChatService(
                    logger, 
                    ollamApiClient, 
                    kernel, 
                    chatCompletionService, 
                    chatHistoryService,
                    conversationDocumentCollectionService,
                    enableThinking ?? false
                );
            });
            
            builder.Services.AddTransient<AesirOllama.VisionModelConfig>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                return new AesirOllama.VisionModelConfig
                {
                    ModelId = configuration.GetValue<string>("Inference:Ollama:VisionModel") ?? "NoVisionModel",
                };
            });
            // should be transient so during dispose we unload model
            builder.Services.AddTransient<IVisionService, AesirOllama.VisionService>();
            
            const string ollamaClientName = "OllamaApiClient";
            builder.Services.AddHttpClient(ollamaClientName, client =>
            {
                var endpoint = builder.Configuration["Inference:Ollama:Endpoint"] ??
                              throw new InvalidOperationException();
                client.BaseAddress = new Uri($"{endpoint}/api");
                
                client.Timeout = TimeSpan.FromSeconds(240);
            })
            .AddHttpMessageHandler<LoggingHttpMessageHandler>();

            builder.Services.AddTransient<LoggingHttpMessageHandler>();
            
            builder.Services.AddTransient<OllamaApiClient>(p =>
            {
                var httpClientFactory = p.GetRequiredService<IHttpClientFactory>();

                var httpClient = httpClientFactory.CreateClient(ollamaClientName);

                // I don't like this... but need default model to do chat history summarization later..
                // because Semantic Kernel is inflexible in this area
                var modelNames = 
                    builder.Configuration.GetSection("Inference:OpenAI:ChatModels").Get<string[]>();
                
                return new OllamaApiClient(httpClient, modelNames?.FirstOrDefault() ?? string.Empty);
            });
        }

        builder.Services.AddSingleton<ITtsService>(sp =>
        {
            var ttsModelPath = builder.Configuration.GetValue<string>("Inference:Onnx:Tts");
            var useCudaValue = Environment.GetEnvironmentVariable("USE_CUDA");
            _ = bool.TryParse(useCudaValue, out var useCuda);
            
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new TtsService(loggerFactory.CreateLogger<TtsService>(), ttsModelPath, useCuda);
        });
        builder.Services.AddSingleton<IChatHistoryService, ChatHistoryService>();
        builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
        builder.Services.AddSingleton<IDbContext, PgDbContext>(p =>
            new PgDbContext(builder.Configuration.GetConnectionString("DefaultConnection")!)
        );
        builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
        
        builder.Services.SetupSemanticKernel(builder.Configuration);

        builder.Services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(
                    builder.Configuration.GetConnectionString("DefaultConnection")
                )
                .ScanIn(typeof(Program).Assembly)
                .For.Migrations())
            .AddLogging(lb =>
            {
                lb.AddFluentMigratorConsole();
                lb.AddConsole().SetMinimumLevel(LogLevel.Trace);
            });

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddHealthChecks();

        builder.Services.AddSignalR();
        
        var app = builder.Build();

        app.MapHub<TtsHub>("/ttshub");
        app.MapHub<SttHub>("/stthub");

        app.MapHealthChecks("/healthz");

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();
        
        app.MapControllers();

        app.MigrateDatabase();

        if (!useOpenAi)
        {
            app.EnsureOllamaBackend();
        }

        app.Run();
    }
}
