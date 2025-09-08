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
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Register basic services needed for database configuration loading
        builder.Services.AddSingleton<IDbContext, PgDbContext>(p =>
            new PgDbContext(builder.Configuration.GetConnectionString("DefaultConnection")!)
        );
        builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
        
        // Load configuration from database or fix up / check file configuration
        NormalizeFileOrDatabaseConfiguration(builder);

        #region AI Backend Clients
        
        // load inference engines (from file or db)
        var inferenceEngines = builder.Configuration.GetSection("InferenceEngines")
            .Get<AesirInferenceEngine[]>() ?? [];
        foreach (var inferenceEngine in inferenceEngines)
        {
            switch (inferenceEngine.Type)
            {
                case InferenceEngineType.Ollama:
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
                        var conversationDocumentCollectionService =
                            serviceProvider.GetRequiredService<IConversationDocumentCollectionService>();

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

                    builder.Services.AddTransient<AesirOllama.VisionModelConfig>(serviceProvider =>
                    {
                        // TODO do we need this config anymore?
                        return new AesirOllama.VisionModelConfig
                        {
                            ModelId = "set-by-agent",
                        };
                    });
                    // should be transient so during dispose we unload model
                    builder.Services.AddTransient<IVisionService, AesirOllama.VisionService>();

                    const string ollamaClientName = "OllamaApiClient";
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

                    builder.Services.AddTransient<OllamaApiClient>(p =>
                    {
                        var httpClientFactory = p.GetRequiredService<IHttpClientFactory>();

                        var httpClient = httpClientFactory.CreateClient(ollamaClientName);

                        return new OllamaApiClient(httpClient);
                    });
                    break;
                }
                case InferenceEngineType.OpenAICompatible:
                {
                    // should be transient to always get fresh kernel
                    builder.Services.AddTransient<IModelsService, AesirOpenAI.ModelsService>();
                    builder.Services.AddTransient<IChatService, AesirOpenAI.ChatService>();

                    builder.Services.AddTransient<AesirOpenAI.VisionModelConfig>(serviceProvider =>
                    {
                        // TODO do we need this config anymore?
                        return new AesirOpenAI.VisionModelConfig
                        {
                            ModelId = "set-by-agent",
                        };
                    });
                    // should be transient so during dispose we unload model
                    builder.Services.AddTransient<IVisionService, AesirOpenAI.VisionService>();

                    var apiKey = inferenceEngine.Configuration["ApiKey"] ??
                                 throw new InvalidOperationException("OpenAI API key not configured");

                    var apiCreds = new ApiKeyCredential(apiKey);
                    var endPoint = inferenceEngine.Configuration["Endpoint"] ??
                                   throw new InvalidOperationException("OpenAI Endpoint not configured");

                    if (string.IsNullOrEmpty(endPoint))
                        builder.Services.AddSingleton(new OpenAIClient(apiCreds));
                    else
                    {
                        builder.Services.AddSingleton(new OpenAIClient(apiCreds, new OpenAIClientOptions()
                        {
                            Endpoint = new Uri(endPoint)
                        }));
                    }
                    break;
                }
            }
        }
        
        // load general settings (from file or db)
        var generalSettings = builder.Configuration.GetSection("GeneralSettings")
            .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
        var ttsModelPath = generalSettings["TtsModelPath"] ?? 
                           throw new InvalidOperationException("TtsModelPath not configured");
        var sttModelPath = generalSettings["SttModelPath"] ?? 
                           throw new InvalidOperationException("SttModelPath not configured");
        var vadModelPath = generalSettings["VadModelPath"] ?? 
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

        builder.Services.SetupSemanticKernel(builder.Configuration);

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

        var modelsService = app.Services.GetRequiredService<IModelsService>();
        var appLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        appLifetime.ApplicationStopping.Register(() => { modelsService.UnloadAllModelsAsync().Wait(); });
        
        app.Run();
    }

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
    /// Configures the application's settings by normalizing the primary configuration
    /// source to either a file-based or database-based configuration, as specified by the user.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="WebApplicationBuilder"/> instance used to configure the web application,
    /// including services, logging, and configuration settings.
    /// </param>
    private static void NormalizeFileOrDatabaseConfiguration(WebApplicationBuilder builder)
    {
        // Read the configuration source setting from the default file-based configuration
        var useDbConfig = builder.Configuration.GetValue<bool>("Configuration:LoadFromDatabase", false);

        if (useDbConfig)
        {   
            // Ensure database and tables exist before trying to load config
            EnsureDatabaseMigrations(builder);

            // create an initial scope so we can use the database loading
            var tempServiceProvider = builder.Services.BuildServiceProvider();
            using var scope = tempServiceProvider.CreateScope();
            var configurationService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {   
                // Load our application-specific config sections from database
                var dbConfig = LoadConfigFromDatabaseAsync(configurationService)
                    .GetAwaiter().GetResult();
                
                // Clear sections we will populate from database so we don't end up with file values leaked in
                var sectionToReplace = new string[]
                {
                    "GeneralSettings", 
                    "InferenceEngines", 
                    "Agents,", 
                    "Tools", 
                    "McpServers"
                };
                var keysToRemove = builder.Configuration.AsEnumerable()
                    .Where(kvp => kvp.Key != null && sectionToReplace.Any(section => 
                        kvp.Key.StartsWith($"{section}:", StringComparison.OrdinalIgnoreCase) || 
                        kvp.Key.Equals(section, StringComparison.OrdinalIgnoreCase)))
                    .Select(kvp => kvp.Key)
                    .ToList();
                foreach (var key in keysToRemove)
                    builder.Configuration[key] = null;
                
                // now populate sections from the database loaded values
                foreach (var kvp in dbConfig)
                    builder.Configuration[kvp.Key] = kvp.Value;
        
                logger.LogInformation("Successfully loaded application configuration from database");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load configuration from database, using file configuration");
            }
        
            tempServiceProvider.Dispose();
        }
        else
        {
            // give each inference engine an Id
            var inferenceEngines = builder.Configuration.GetSection("InferenceEngines")
                .Get<AesirInferenceEngine[]>() ?? [];
            if (inferenceEngines.Length == 0)
                throw new InvalidOperationException("InferenceEngines configuration is missing or empty");
            for (var i = 0; i < inferenceEngines.Length; i++)
            {
                var inferenceEngine = inferenceEngines[i];
                
                // this logic assumes we are given the inference engines in order of their idx
                inferenceEngine.Id = Guid.NewGuid();
                builder.Configuration[$"InferenceEngines:{i}:Id"] = inferenceEngine.Id.ToString();
            }

            // update the rag inference engine id
            var generalSettings = builder.Configuration.GetSection("GeneralSettings")
                .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
            var ragInferenceEngineName = generalSettings["RagInferenceEngineName"] ??
                                         throw new InvalidOperationException("RagInferenceEngineName not configured");
            var ragInferenceEngine = inferenceEngines.FirstOrDefault(ie => ie.Name == ragInferenceEngineName) ?? 
                                     throw new InvalidOperationException("RagInferenceEngineName does not match a configured Inference Engine");
            builder.Configuration[$"GeneralSettings:RagInferenceEngineId"] = ragInferenceEngine?.Id?.ToString() ?? null;
            
            // give each agent an id
            var agents = builder.Configuration.GetSection("Agents")
                .Get<AesirAgent[]>() ?? [];
            if (inferenceEngines.Length == 0)
                throw new InvalidOperationException("Agents configuration is missing or empty");
            for (var i = 0; i < agents.Length; i++)
            {
                var agent = agents[i];
                
                // this logic assumes we are given the agents in order of their idx
                agent.Id = Guid.NewGuid();
                builder.Configuration[$"Agents:{i}:Id"] = agent.Id.ToString();
                
                // determine chat inference engine id
                var chatInferenceEngineName = builder.Configuration[$"Agents:{i}:ChatInferenceEngineName"] ??
                                             throw new InvalidOperationException("ChatInferenceEngineName not configured");
                var chatInferenceEngineId = inferenceEngines.FirstOrDefault(ie => ie.Name == chatInferenceEngineName)?.Id ?? 
                                            throw new InvalidOperationException("ChatInferenceEngineName does not match a configured Inference Engine");
                builder.Configuration[$"Agents:{i}:ChatInferenceEngineId"] = chatInferenceEngineId.ToString();
                
                // determine vision inference engine id
                var visionInferenceEngineName = builder.Configuration[$"Agents:{i}:VisionInferenceEngineName"] ??
                                              throw new InvalidOperationException("VisionInferenceEngineName not configured");
                var visionInferenceEngineId = inferenceEngines.FirstOrDefault(ie => ie.Name == visionInferenceEngineName)?.Id ?? 
                                            throw new InvalidOperationException("VisionInferenceEngineName does not match a configured Inference Engine");
                builder.Configuration[$"Agents:{i}:VisionInferenceEngineId"] = visionInferenceEngineId.ToString();
            }
        }
    }
    
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
    
    private static async Task<Dictionary<string, string?>> LoadConfigFromDatabaseAsync(IConfigurationService configurationService)
    {
        var config = new Dictionary<string, string?>();

        var generalSettingsTask = configurationService.GetGeneralSettingsAsync();
        var inferenceEnginesTask = configurationService.GetInferenceEnginesAsync();
        var agentsTask = configurationService.GetAgentsAsync();
        var toolsTask = configurationService.GetToolsAsync();
        var mcpServersTask = configurationService.GetMcpServersAsync();
        await Task.WhenAll(inferenceEnginesTask, agentsTask, toolsTask, mcpServersTask);

        var generalSettings = await generalSettingsTask;
        var inferenceEngines = await inferenceEnginesTask;
        var agents = await agentsTask;
        var tools = await toolsTask;
        var mcpServers = await mcpServersTask;

        // load general settings
        var aesirInferenceEngines = inferenceEngines as AesirInferenceEngine[] ?? inferenceEngines.ToArray();
        config["GeneralSettings:RagInferenceEngineId"] = generalSettings.RagInferenceEngineId?.ToString();
        config["GeneralSettings:RagInferenceEngineName"] = aesirInferenceEngines.FirstOrDefault(ie => ie.Id == generalSettings.RagInferenceEngineId)?.Name;
        config["GeneralSettings:RagEmbeddingModel"] = generalSettings.RagEmbeddingModel;
        config["GeneralSettings:TtsModelPath"] = generalSettings.TtsModelPath;
        config["GeneralSettings:SttModelPath"] = generalSettings.SttModelPath;
        config["GeneralSettings:VadModelPath"] = generalSettings.VadModelPath;
            
        // load inference engines
        for (var idx = 0; idx < aesirInferenceEngines.Length; idx++)
        {
            var inferenceEngine = aesirInferenceEngines[idx];
            config[$"InferenceEngines:{idx}:Id"] = inferenceEngine.Id.ToString();
            config[$"InferenceEngines:{idx}:Name"] = inferenceEngine.Name;
            config[$"InferenceEngines:{idx}:Type"] = inferenceEngine.Type!.ToString();

            foreach (var entry in inferenceEngine.Configuration)
                config[$"InferenceEngines:{idx}:Configuration:{entry.Key}"] = entry.Value;
        }
        
        // load agents
        var aesirAgents = agents as AesirAgent[] ?? agents.ToArray();
        for (var idx = 0; idx < aesirAgents.Length; idx++)
        {
            var agent = aesirAgents[idx];
            config[$"Agents:{idx}:Name"] = agent.Name;
            config[$"Agents:{idx}:ChatInferenceEngineId"] = agent.ChatInferenceEngineId.ToString();
            config[$"Agents:{idx}:ChatInferenceEngineName"] = aesirInferenceEngines.FirstOrDefault(ie => ie.Id == agent.ChatInferenceEngineId)?.Name;;
            config[$"Agents:{idx}:ChatModel"] = agent.ChatModel;
            config[$"Agents:{idx}:VisionInferenceEngineId"] = agent.VisionInferenceEngineId.ToString();
            config[$"Agents:{idx}:VisionInferenceEngineName"] = aesirInferenceEngines.FirstOrDefault(ie => ie.Id == agent.VisionInferenceEngineId)?.Name;;
            config[$"Agents:{idx}:VisionModel"] = agent.VisionModel;
            config[$"Agents:{idx}:PromptPersona"] = agent.PromptPersona.ToString();
            config[$"Agents:{idx}:CustomPromptContent"] = agent.CustomPromptContent;
        }
        
        // load tools
        // TODO
        
        // load mcp servers
        // TODO
        
        return config;
    }

}