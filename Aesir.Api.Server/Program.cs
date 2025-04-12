using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Data;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Services;
using Aesir.Api.Server.Services.Implementations.Standard;
using FluentMigrator.Runner;
using OllamaSharp;
using AesirOllama = Aesir.Api.Server.Services.Implementations.Ollama;

namespace Aesir.Api.Server;

public class Program
{
    [Experimental("SKEXP0070")]
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // builder.Services.AddSingleton<IModelsService,AesirOpenAI.ModelsService>();
        // builder.Services.AddSingleton<IChatService,AesirOpenAI.ChatService>();
        builder.Services.AddSingleton<IModelsService, AesirOllama.ModelsService>();
        builder.Services.AddSingleton<IChatService, AesirOllama.ChatService>();
        builder.Services.AddSingleton<IChatHistoryService, ChatHistoryService>();
        builder.Services.AddSingleton<IDbContext,PgDbContext>(p => 
            new PgDbContext(builder.Configuration.GetConnectionString("DefaultConnection")!)
        );
        
        // create a named client for the Ollama API
        const string ollamaClientName = "OllamaApiClient";
        builder.Services.AddHttpClient(ollamaClientName, client =>
        {
            var endpoint = builder.Configuration["Inference:Endpoint"] ?? 
                           throw new InvalidOperationException();
            client.BaseAddress = new Uri($"{endpoint}/api");
        });
        builder.Services.AddTransient<OllamaApiClient>(p =>
        {
            var httpClientFactory = p.GetRequiredService<IHttpClientFactory>();
            
            var httpClient = httpClientFactory.CreateClient(ollamaClientName);
            
            return new OllamaApiClient(httpClient);
        });

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

        app.InitializeOllamaBackend();
        
        app.Run();
    }
}