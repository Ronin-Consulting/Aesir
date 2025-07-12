using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Data;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Services;
using Aesir.Api.Server.Services.Implementations.Standard;
using FluentMigrator.Runner;
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
            builder.Services.AddTransient<IChatService, AesirOllama.ChatService>();
            
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

                return new OllamaApiClient(httpClient);
            });
        }

        builder.Services.AddSingleton<IChatHistoryService, ChatHistoryService>();
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

        var app = builder.Build();

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
