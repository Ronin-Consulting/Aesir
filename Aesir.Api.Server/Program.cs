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

        // Add services to the container.
        
        // Choose which backend to use based on configuration
        var useOpenAi = builder.Configuration.GetValue<bool>("Inference:UseOpenAICompatible");
        
        if (useOpenAi)
        {
            // Register OpenAI services
            builder.Services.AddSingleton<IModelsService, AesirOpenAI.ModelsService>();
            builder.Services.AddSingleton<IChatService, AesirOpenAI.ChatService>();
            
            // Configure OpenAI client
            var apiKey = builder.Configuration["Inference:OpenAI:ApiKey"] ?? 
                throw new InvalidOperationException("OpenAI API key not configured");
            
            var apiCreds = new ApiKeyCredential(apiKey);
            var endPoint = builder.Configuration["Inference:OpenAI:Endpoint"];
            
            if(string.IsNullOrEmpty(endPoint))
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
            // Register Ollama services
            builder.Services.AddSingleton<IModelsService, AesirOllama.ModelsService>();
            builder.Services.AddSingleton<IChatService, AesirOllama.ChatService>();
            
            // Create a named client for the Ollama API
            const string ollamaClientName = "OllamaApiClient";
            builder.Services.AddHttpClient(ollamaClientName, client =>
            {
                var endpoint = builder.Configuration["Inference:Ollama:Endpoint"] ?? 
                              throw new InvalidOperationException();
                client.BaseAddress = new Uri($"{endpoint}/api");
            });
            builder.Services.AddTransient<OllamaApiClient>(p =>
            {
                var httpClientFactory = p.GetRequiredService<IHttpClientFactory>();
                
                var httpClient = httpClientFactory.CreateClient(ollamaClientName);
                
                return new OllamaApiClient(httpClient);
            });
        }
        
        builder.Services.AddSingleton<IChatHistoryService, ChatHistoryService>();
        builder.Services.AddSingleton<IDbContext,PgDbContext>(p => 
            new PgDbContext(builder.Configuration.GetConnectionString("DefaultConnection")!)
        );

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
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddHealthChecks();
        
        var app = builder.Build();

        app.MapHealthChecks("/healthz");
        
        // Configure the HTTP request pipeline.
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
            app.InitializeOllamaBackend();
        }
        
        app.Run();
    }
}