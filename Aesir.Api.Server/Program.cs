using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Data;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;
using Aesir.Api.Server.Services.Implementations.Onnx;
using Aesir.Api.Server.Services.Implementations.Standard;
using Aesir.Common.Models;
using FluentMigrator.Runner;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Npgsql;
using OllamaSharp;
using OpenAI;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using AesirOllama = Aesir.Api.Server.Services.Implementations.Ollama;
using AesirOpenAI = Aesir.Api.Server.Services.Implementations.OpenAI;

namespace Aesir.Api.Server;

public class Program
{
    [Experimental("SKEXP0070")]
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        var npgsqlDataSource = CreateNpgsqlDataSource(builder);
        var dbContext = new PgDbContext(npgsqlDataSource);
        var configurationReadinessService = new ConfigurationReadinessService();
        
        // boot up a configuration service so we can use it now
        using var loggerFactory = LoggerFactory.Create(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(builder.Configuration.GetSection("Logging"));
            loggingBuilder.AddConsole();
            if (builder.Environment.IsDevelopment())
            {
                loggingBuilder.AddDebug();
            }
        });
        var configurationServiceLogger = loggerFactory.CreateLogger<ConfigurationService>();
        var configurationService = new ConfigurationService(configurationServiceLogger, dbContext, builder.Configuration);

        builder.Services.AddSingleton(npgsqlDataSource);
        builder.Services.AddSingleton<IDbContext>(dbContext);
        builder.Services.AddSingleton<IConfigurationService>(configurationService);
        builder.Services.AddSingleton<IConfigurationReadinessService>(configurationReadinessService);

        // Check database configuration or fix up and check file configuration, prior to being used by ConfigurationService
        await PrepareConfigurationForUse(builder, configurationService, configurationReadinessService);
        
        #region AI Backend Clients

        // load entites from file config or db config
        var inferenceEngines = (await configurationService.GetInferenceEnginesAsync()).ToList();
        var generalSettings = await configurationService.GetGeneralSettingsAsync();
        var agents = (await configurationService.GetAgentsAsync()).ToList();
        
        foreach (var inferenceEngine in inferenceEngines)
        {
            if (!configurationReadinessService.IsInferenceEngineReadyAtBoot(inferenceEngine.Id.Value))
            {
                Console.Write($"Configuration for Inference Engine `{inferenceEngine.Name}` is not ready and being skipped for initialization");
                continue;
            }

            var inferenceEngineIdKey = inferenceEngine.Id.Value.ToString();
            
            switch (inferenceEngine.Type)
            {
                case InferenceEngineType.Ollama:
                {
                    // should be transient to always get fresh kernel
                    builder.Services.AddKeyedTransient<IModelsService>(inferenceEngineIdKey, (serviceProvider, key) =>
                        new AesirOllama.ModelsService(
                            inferenceEngineIdKey,
                            serviceProvider.GetRequiredService<ILogger<AesirOllama.ModelsService>>(),
                            serviceProvider.GetRequiredService<IConfiguration>(),
                            serviceProvider));
                    builder.Services.AddKeyedTransient<IChatService>(inferenceEngineIdKey, (serviceProvider, key) =>
                    {
                        // only one of these
                        var logger = serviceProvider.GetRequiredService<ILogger<AesirOllama.ChatService>>();
                        var kernel = serviceProvider.GetRequiredService<Kernel>();
                        var chatHistoryService = serviceProvider.GetRequiredService<IChatHistoryService>();
                        var conversationDocumentCollectionService =
                            serviceProvider.GetRequiredService<IConversationDocumentCollectionService>();

                        // these are keyed by inference engine id
                        var ollamApiClient = serviceProvider.GetRequiredKeyedService<OllamaApiClient>(inferenceEngineIdKey);
                        var chatCompletionService = serviceProvider.GetRequiredKeyedService<IChatCompletionService>(inferenceEngineIdKey);

                        var enableThinking = Boolean.Parse(inferenceEngine.Configuration["EnableChatModelThinking"] ?? "false");

                        return new AesirOllama.ChatService(
                            logger,
                            ollamApiClient,
                            kernel,
                            chatCompletionService,
                            chatHistoryService,
                            conversationDocumentCollectionService,
                            enableThinking
                        );
                    });
                    // Changed to scoped for better performance while maintaining model lifecycle
                    builder.Services.AddTransient<IVisionService, AesirOllama.VisionService>();

                    var ollamaClientName = $"OllamaApiClient-{inferenceEngineIdKey}";
                    builder.Services.AddHttpClient(ollamaClientName, client =>
                        {
                            var endpoint = inferenceEngine.Configuration["Endpoint"] ??
                                           throw new InvalidOperationException("Ollama Endpoint not configured");
                            client.BaseAddress = new Uri($"{endpoint}/api");
                        }).SetHandlerLifetime(TimeSpan.FromMinutes(5))
                        .AddPolicyHandler(HttpPolicyExtensions
                            .HandleTransientHttpError()
                            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                            .WaitAndRetryAsync(
                                Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromMilliseconds(500),
                                    retryCount: 5))
                        ).AddHttpMessageHandler<LoggingHttpMessageHandler>();

                    builder.Services.AddTransient<LoggingHttpMessageHandler>();

                    builder.Services.AddKeyedTransient<OllamaApiClient>(inferenceEngineIdKey, (serviceProvider, key) =>
                    {
                        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

                        var httpClient = httpClientFactory.CreateClient(ollamaClientName);

                        return new OllamaApiClient(httpClient);
                    });
                    break;
                }
                case InferenceEngineType.OpenAICompatible:
                {
                    // should be transient to always get fresh kernel
                    builder.Services.AddKeyedTransient<IModelsService>(inferenceEngineIdKey, (serviceProvider, key) =>
                        new AesirOpenAI.ModelsService(
                            inferenceEngineIdKey,
                            serviceProvider.GetRequiredService<ILogger<AesirOpenAI.ModelsService>>(),
                            serviceProvider.GetRequiredService<IConfiguration>(),
                            serviceProvider));
                    builder.Services.AddKeyedTransient<IChatService>(inferenceEngineIdKey, (serviceProvider, key) =>
                    {
                        // only one of these
                        var logger = serviceProvider.GetRequiredService<ILogger<AesirOpenAI.ChatService>>();
                        var kernel = serviceProvider.GetRequiredService<Kernel>();
                        var chatHistoryService = serviceProvider.GetRequiredService<IChatHistoryService>();
                        var conversationDocumentCollectionService =
                            serviceProvider.GetRequiredService<IConversationDocumentCollectionService>();

                        // these are keyed by inference engine id
                        var chatCompletionService = serviceProvider.GetRequiredKeyedService<IChatCompletionService>(inferenceEngineIdKey);

                        return new AesirOpenAI.ChatService(
                            logger,
                            kernel,
                            chatCompletionService,
                            chatHistoryService,
                            conversationDocumentCollectionService
                        );
                    });
                    
                    // should be transient so during dispose we unload model
                    builder.Services.AddTransient<IVisionService, AesirOpenAI.VisionService>();
                    
                    var apiKey = inferenceEngine.Configuration["ApiKey"] ??
                                 throw new InvalidOperationException("OpenAI API key not configured");

                    var apiCreds = new ApiKeyCredential(apiKey);
                    var endPoint = inferenceEngine.Configuration["Endpoint"] ??
                                   throw new InvalidOperationException("OpenAI Endpoint not configured");

                    if (string.IsNullOrEmpty(endPoint))
                        builder.Services.AddKeyedSingleton(inferenceEngineIdKey, new OpenAIClient(apiCreds));
                    else
                    {
                        builder.Services.AddKeyedSingleton(inferenceEngineIdKey, new OpenAIClient(apiCreds, new OpenAIClientOptions()
                        {
                            Endpoint = new Uri(endPoint)
                        }));
                    }
                    break;
                }
            }
        }
        
        // load speech settings
        var ttsModelPath = generalSettings.TtsModelPath ?? 
                           throw new InvalidOperationException("TtsModelPath not configured");
        var sttModelPath = generalSettings.SttModelPath ?? 
                           throw new InvalidOperationException("SttModelPath not configured");
        var vadModelPath = generalSettings.VadModelPath ?? 
                           throw new InvalidOperationException("VadModelPath not configured");

        builder.Services.AddSingleton<ITtsService>(sp =>
        {
            var useCudaValue = Environment.GetEnvironmentVariable("USE_CUDA");
            _ = bool.TryParse(useCudaValue, out var useCuda);

            var ttsConfig = TtsConfig.Default;
            ttsConfig.ModelPath = ttsModelPath ?? throw new InvalidOperationException();
            ttsConfig.CudaEnabled = useCuda;

            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new TtsService(loggerFactory.CreateLogger<TtsService>(), ttsConfig);
        });
        builder.Services.AddSingleton<ISttService>(sp =>
        {
            var useCudaValue = Environment.GetEnvironmentVariable("USE_CUDA");
            _ = bool.TryParse(useCudaValue, out var useCuda);

            var sttConfig = SttConfig.Default;
            sttConfig.WhisperModelPath = sttModelPath ?? throw new InvalidOperationException();
            sttConfig.VadModelPath = vadModelPath ?? throw new InvalidOperationException();
            sttConfig.CudaEnabled = useCuda;

            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new SttService(loggerFactory.CreateLogger<SttService>(), sttConfig);
        });

        #endregion

        builder.Services.AddSingleton<IMcpServerService, McpServerService>();
        builder.Services.AddSingleton<IChatHistoryService, ChatHistoryService>();
        builder.Services.AddSingleton<IFileStorageService, FileStorageService>();

        await builder.Services.SetupSemanticKernelAsync(configurationService, configurationReadinessService);

        RegisterMigratorServices(builder, builder.Services);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAvaloniaApp", policy =>
            {
                policy.WithOrigins("http://aesir.localhost:5236")
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
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

        // Let Traffic do this for us
        //app.UseHttpsRedirection();

        app.UseCors("AllowAvaloniaApp");

        app.UseAuthorization();

        app.MapControllers();

        app.MigrateDatabase();

        RegisterModelUnload(app, generalSettings, agents);
        
        app.Run();
    }

    private static NpgsqlDataSource CreateNpgsqlDataSource(WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("DefaultConnection connection string not configured");
            
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            
        // Configure connection pool parameters for optimal performance
        dataSourceBuilder.ConnectionStringBuilder.MaxPoolSize = 100;
        dataSourceBuilder.ConnectionStringBuilder.MinPoolSize = 10;
        dataSourceBuilder.ConnectionStringBuilder.ConnectionIdleLifetime = 300; // 5 minutes
        dataSourceBuilder.ConnectionStringBuilder.ConnectionPruningInterval = 10; // 10 seconds
        dataSourceBuilder.ConnectionStringBuilder.CommandTimeout = 30; // 30 seconds
            
        return dataSourceBuilder.Build();
    }

    /// <summary>
    /// Registers services required for database migrations, including FluentMigrator core,
    /// PostgreSQL runner configuration, and logging services.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="WebApplicationBuilder"/> instance containing configuration and application setup data.
    /// </param>
    /// <param name="services">
    /// The service collection to which migration-related services will be added.
    /// </param>
    /// <returns>
    /// The modified <see cref="IServiceCollection"/> containing the registered migration services.
    /// </returns>
    private static IServiceCollection RegisterMigratorServices(WebApplicationBuilder builder, IServiceCollection services)
    {
        services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(builder.Configuration.GetConnectionString("DefaultConnection"))
                .ScanIn(typeof(Program).Assembly)
                .For.Migrations())
            .AddLogging(lb => lb.AddConsole().SetMinimumLevel(LogLevel.Information));

        return services;
    }

    /// <summary>
    /// Prepares the application configuration for use by ensuring the database is ready when in database mode
    /// or by validating and preparing file-based configuration when not in database mode.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="WebApplicationBuilder"/> instance used to configure the application.
    /// </param>
    /// <param name="configurationService">
    /// The service responsible for handling application configuration and its preparation.
    /// </param>
    /// <param name="configurationReadinessService">
    /// The service responsible for determining the readiness of the configuration and providing necessary operations for preparation.
    /// </param>
    private static async Task PrepareConfigurationForUse(WebApplicationBuilder builder, 
        ConfigurationService configurationService, ConfigurationReadinessService configurationReadinessService)
    {
        if (configurationService.DatabaseMode)
        {
            // Ensure database and tables exist before trying to load config
            EnsureDatabaseMigrations(builder);
            
            // Check database configuration for "fully ready" vs booting into "setup only" state
            await configurationService.PrepareDatabaseConfigurationAsync(configurationReadinessService);
        }
        else
        {
            // Validate settings and add/fix up ids ...
            configurationService.PrepareFileConfigurationAsync();
        }
    }

    /// <summary>
    /// Ensures that all pending database migrations are applied, preparing the database for application use.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="WebApplicationBuilder"/> instance containing the application's configuration, services,
    /// and other setup settings required to execute the migrations.
    /// </param>
    private static void EnsureDatabaseMigrations(WebApplicationBuilder builder)
    {
        // Create a temporary service collection just for migrations
        var migrationServices = new ServiceCollection();

        RegisterMigratorServices(builder, migrationServices);

        using var migrationServiceProvider = migrationServices.BuildServiceProvider(false);
        using var scope = migrationServiceProvider.CreateScope();
    
        try
        {
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
            runner.MigrateUp();
        
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Database migrations completed successfully");
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Failed to run database migrations");
            throw; // Re-throw to prevent starting with potentially missing tables
        }
    }

    private static void RegisterModelUnload(WebApplication app, AesirGeneralSettings generalSettings, IList<AesirAgent> agents)
    {
        var appLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        // This is an imperfect routine. Ideally the IModelsService would know what models have been loaded, but 
        // they currently don't. We will assume the RAG embedding model, RAG vision model, and any model on any
        // agent have been loaded. They possibly have not, so we rely on the IModelsService implementations
        // to handle this gracefully. In the future we should track models that have been used/loaded.
        
        // unload all chat models
        foreach (var agent in agents)
        {
            var chatInferenceEngineId = agent.ChatInferenceEngineId?.ToString() ??
                throw new InvalidOperationException($"ChatInferenceEngineId not configured for {agent.Name}");
            var chatModel = agent.ChatModel ??
                throw new InvalidOperationException($"ChatModel not configured for {agent.Name}");

            var chatModelsService = app.Services.GetKeyedService<IModelsService>(chatInferenceEngineId) ??
                throw new InvalidOperationException($"Missing expected ModelsService for {chatInferenceEngineId}");
            
            appLifetime.ApplicationStopping.Register(() => {
                chatModelsService.UnloadModelsAsync([chatModel]).Wait();
            });
        }
        
        // unload RAG embedding model
        if (generalSettings.RagEmbeddingInferenceEngineId == null || generalSettings.RagEmbeddingModel == null)
        {
            logger.LogWarning("RAG embedding model configuration is not yet complete");
        }
        else
        {
            var ragEmbeddingInferenceEngineId = generalSettings.RagEmbeddingInferenceEngineId?.ToString() ??
                                                throw new InvalidOperationException($"RagEmbeddingInferenceEngineId not configured");
            var ragEmbeddingModelsService = app.Services.GetKeyedService<IModelsService>(ragEmbeddingInferenceEngineId) ??
                throw new InvalidOperationException($"Missing expected ModelsService for {ragEmbeddingInferenceEngineId}");
            appLifetime.ApplicationStopping.Register(() => {
                ragEmbeddingModelsService.UnloadModelsAsync([generalSettings.RagEmbeddingModel]).Wait();
            });
        }

        // unload RAG vision model
        if (generalSettings.RagVisionInferenceEngineId == null || generalSettings.RagVisionModel == null)
        {
            logger.LogWarning("RAG vision model configuration is not yet complete");
        }
        else
        {
            var ragVisionInferenceEngineId = generalSettings.RagVisionInferenceEngineId?.ToString() ??
                                        throw new InvalidOperationException($"RagVisionInferenceEngineId not configured");
            var ragVisionModelsService = app.Services.GetKeyedService<IModelsService>(ragVisionInferenceEngineId) ??
                                            throw new InvalidOperationException($"Missing expected ModelsService for {ragVisionInferenceEngineId}");
            appLifetime.ApplicationStopping.Register(() => {
                ragVisionModelsService.UnloadModelsAsync([generalSettings.RagVisionModel]).Wait();
            });
        }
    }
}