using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Client.Web.Modules.Wizard.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Integration;

/// <summary>
/// Base class for integration tests that test component flows with realistic API interactions
/// </summary>
public abstract class IntegrationTestBase : TestContext
{
    protected MockHttpMessageHandler MockHttp { get; }
    protected JsonSerializerOptions JsonOptions { get; }

    // Test data that simulates API state
    protected List<AesirInferenceEngineBase> InferenceEngines { get; } = new();
    protected List<AesirMcpServerBase> McpServers { get; } = new();
    protected List<AesirToolBase> Tools { get; } = new();
    protected List<AesirAgentBase> Agents { get; } = new();
    protected List<AesirChatSessionItem> ChatSessions { get; } = new();
    protected AesirGeneralSettingsBase GeneralSettings { get; set; } = new();

    protected IntegrationTestBase()
    {
        MockHttp = new MockHttpMessageHandler();
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        SetupServices();
        SetupDefaultApiResponses();
    }

    private void SetupServices()
    {
        // Create HttpClient with mock handler
        var httpClient = MockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://aesir.localhost/");

        // Register services
        Services.AddSingleton(httpClient);
        Services.AddSingleton<IApiClient, ApiClient>();
        Services.AddSingleton<IConfigurationApiService, ConfigurationApiService>();
        Services.AddSingleton<IChatApiService, ChatApiService>();
        Services.AddSingleton<IInferenceEngineService, InferenceEngineService>();
        Services.AddSingleton<IMcpServerService, McpServerService>();
        Services.AddSingleton<IToolService, ToolService>();
        Services.AddSingleton<IAgentService, AgentService>();
        Services.AddSingleton<IChatStateService, ChatStateService>();
        Services.AddSingleton<IChatHistoryService, ChatHistoryService>();
        Services.AddSingleton<IMarkdownService, MarkdownService>();
        Services.AddSingleton<IGeneralSettingsService, GeneralSettingsService>();
        Services.AddSingleton<IWizardStateService, WizardStateService>();

        // MudBlazor services
        Services.AddMudServices();

        // JSInterop setup
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("scrollToBottom", _ => true);
    }

    private void SetupDefaultApiResponses()
    {
        // Setup dynamic API responses based on test data collections
        SetupInferenceEngineEndpoints();
        SetupMcpServerEndpoints();
        SetupToolEndpoints();
        SetupAgentEndpoints();
        SetupChatEndpoints();
        SetupGeneralSettingsEndpoints();
        SetupConfigurationEndpoints();
    }

    protected void SetupInferenceEngineEndpoints()
    {
        // GET all
        MockHttp.When(HttpMethod.Get, "https://aesir.localhost/configuration/inferenceengines")
            .Respond(() => Task.FromResult(CreateJsonResponse(InferenceEngines)));

        // GET by ID
        MockHttp.When(HttpMethod.Get, "https://aesir.localhost/configuration/inferenceengines/*")
            .Respond(request =>
            {
                var id = Guid.Parse(request.RequestUri!.Segments.Last());
                var engine = InferenceEngines.FirstOrDefault(e => e.Id == id);
                return Task.FromResult(engine != null
                    ? CreateJsonResponse(engine)
                    : new HttpResponseMessage(HttpStatusCode.NotFound));
            });

        // POST - Returns Guid
        MockHttp.When(HttpMethod.Post, "https://aesir.localhost/configuration/inferenceengines")
            .Respond(async request =>
            {
                var content = await request.Content!.ReadAsStringAsync();
                var engine = JsonSerializer.Deserialize<AesirInferenceEngineBase>(content, JsonOptions)!;
                var newId = Guid.NewGuid();
                engine.Id = newId;
                InferenceEngines.Add(engine);
                return CreateJsonResponse(newId);
            });

        // PUT
        MockHttp.When(HttpMethod.Put, "https://aesir.localhost/configuration/inferenceengines/*")
            .Respond(async request =>
            {
                var id = Guid.Parse(request.RequestUri!.Segments.Last());
                var content = await request.Content!.ReadAsStringAsync();
                var updated = JsonSerializer.Deserialize<AesirInferenceEngineBase>(content, JsonOptions)!;
                var index = InferenceEngines.FindIndex(e => e.Id == id);
                if (index >= 0)
                {
                    updated.Id = id;
                    InferenceEngines[index] = updated;
                    return CreateJsonResponse<object?>(null);
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

        // DELETE
        MockHttp.When(HttpMethod.Delete, "https://aesir.localhost/configuration/inferenceengines/*")
            .Respond(request =>
            {
                var id = Guid.Parse(request.RequestUri!.Segments.Last());
                InferenceEngines.RemoveAll(e => e.Id == id);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            });
    }

    protected void SetupMcpServerEndpoints()
    {
        MockHttp.When(HttpMethod.Get, "https://aesir.localhost/configuration/mcpservers")
            .Respond(() => Task.FromResult(CreateJsonResponse(McpServers)));

        MockHttp.When(HttpMethod.Post, "https://aesir.localhost/configuration/mcpservers")
            .Respond(async request =>
            {
                var content = await request.Content!.ReadAsStringAsync();
                var server = JsonSerializer.Deserialize<AesirMcpServerBase>(content, JsonOptions)!;
                var newId = Guid.NewGuid();
                server.Id = newId;
                McpServers.Add(server);
                return CreateJsonResponse(newId);
            });

        MockHttp.When(HttpMethod.Delete, "https://aesir.localhost/configuration/mcpservers/*")
            .Respond(request =>
            {
                var id = Guid.Parse(request.RequestUri!.Segments.Last());
                McpServers.RemoveAll(s => s.Id == id);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            });
    }

    protected void SetupToolEndpoints()
    {
        MockHttp.When(HttpMethod.Get, "https://aesir.localhost/configuration/tools")
            .Respond(() => Task.FromResult(CreateJsonResponse(Tools)));

        MockHttp.When(HttpMethod.Post, "https://aesir.localhost/configuration/tools")
            .Respond(async request =>
            {
                var content = await request.Content!.ReadAsStringAsync();
                var tool = JsonSerializer.Deserialize<AesirToolBase>(content, JsonOptions)!;
                var newId = Guid.NewGuid();
                tool.Id = newId;
                Tools.Add(tool);
                return CreateJsonResponse(newId);
            });

        MockHttp.When(HttpMethod.Delete, "https://aesir.localhost/configuration/tools/*")
            .Respond(request =>
            {
                var id = Guid.Parse(request.RequestUri!.Segments.Last());
                Tools.RemoveAll(t => t.Id == id);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            });
    }

    protected void SetupAgentEndpoints()
    {
        MockHttp.When(HttpMethod.Get, "https://aesir.localhost/configuration/agents")
            .Respond(() => Task.FromResult(CreateJsonResponse(Agents)));

        MockHttp.When(HttpMethod.Get, "https://aesir.localhost/configuration/agents/*/tools")
            .Respond(() => Task.FromResult(CreateJsonResponse(new List<AesirToolBase>())));

        MockHttp.When(HttpMethod.Get, "https://aesir.localhost/configuration/agents/*")
            .Respond(request =>
            {
                var segments = request.RequestUri!.Segments;
                var idSegment = segments.Last().TrimEnd('/');
                if (idSegment == "tools") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                var id = Guid.Parse(idSegment);
                var agent = Agents.FirstOrDefault(a => a.Id == id);
                return Task.FromResult(agent != null
                    ? CreateJsonResponse(agent)
                    : new HttpResponseMessage(HttpStatusCode.NotFound));
            });

        MockHttp.When(HttpMethod.Post, "https://aesir.localhost/configuration/agents")
            .Respond(async request =>
            {
                var content = await request.Content!.ReadAsStringAsync();
                var agent = JsonSerializer.Deserialize<AesirAgentBase>(content, JsonOptions)!;
                var newId = Guid.NewGuid();
                agent.Id = newId;
                Agents.Add(agent);
                return CreateJsonResponse(newId);
            });

        MockHttp.When(HttpMethod.Put, "https://aesir.localhost/configuration/agents/*/tools")
            .Respond(() => Task.FromResult(CreateJsonResponse<object?>(null)));

        MockHttp.When(HttpMethod.Put, "https://aesir.localhost/configuration/agents/*")
            .Respond(async request =>
            {
                var segments = request.RequestUri!.Segments;
                var idSegment = segments[^1].TrimEnd('/');
                if (idSegment == "tools") return CreateJsonResponse<object?>(null);
                var id = Guid.Parse(idSegment);
                var content = await request.Content!.ReadAsStringAsync();
                var updated = JsonSerializer.Deserialize<AesirAgentBase>(content, JsonOptions)!;
                var index = Agents.FindIndex(a => a.Id == id);
                if (index >= 0)
                {
                    updated.Id = id;
                    Agents[index] = updated;
                    return CreateJsonResponse<object?>(null);
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

        MockHttp.When(HttpMethod.Delete, "https://aesir.localhost/configuration/agents/*")
            .Respond(request =>
            {
                var id = Guid.Parse(request.RequestUri!.Segments.Last());
                Agents.RemoveAll(a => a.Id == id);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            });
    }

    protected void SetupChatEndpoints()
    {
        // Chat history
        MockHttp.When(HttpMethod.Get, "https://aesir.localhost/chat/history/user/*")
            .Respond(() => Task.FromResult(CreateJsonResponse(ChatSessions.OrderByDescending(s => s.UpdatedAt).ToList())));

        // Get session - match pattern without user path
        MockHttp.When(HttpMethod.Get, "https://aesir.localhost/chat/history/*")
            .Respond(request =>
            {
                var segments = request.RequestUri!.Segments;
                // Skip if it's a user path
                if (segments.Any(s => s.Contains("user")))
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

                var idStr = segments.Last().TrimEnd('/');
                if (Guid.TryParse(idStr, out var id))
                {
                    var session = ChatSessions.FirstOrDefault(s => s.Id == id);
                    if (session != null)
                    {
                        var fullSession = new AesirChatSession
                        {
                            Id = session.Id,
                            Title = session.Title,
                            UpdatedAt = session.UpdatedAt,
                            Conversation = new AesirConversation()
                        };
                        return Task.FromResult(CreateJsonResponse(fullSession));
                    }
                }
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });

        // Delete session
        MockHttp.When(HttpMethod.Delete, "https://aesir.localhost/chat/history/*")
            .Respond(request =>
            {
                var id = Guid.Parse(request.RequestUri!.Segments.Last());
                ChatSessions.RemoveAll(s => s.Id == id);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            });

        // Update title
        MockHttp.When(HttpMethod.Put, "https://aesir.localhost/chat/history/*")
            .Respond(() => Task.FromResult(CreateJsonResponse<object?>(null)));
    }

    protected void SetupGeneralSettingsEndpoints()
    {
        // GET general settings
        MockHttp.When(HttpMethod.Get, "https://aesir.localhost/configuration/generalsettings")
            .Respond(() => Task.FromResult(CreateJsonResponse(GeneralSettings)));

        // PUT general settings
        MockHttp.When(HttpMethod.Put, "https://aesir.localhost/configuration/generalsettings")
            .Respond(async request =>
            {
                var content = await request.Content!.ReadAsStringAsync();
                var settings = JsonSerializer.Deserialize<AesirGeneralSettingsBase>(content, JsonOptions)!;
                GeneralSettings = settings;
                return CreateJsonResponse<object?>(null);
            });
    }

    protected void SetupConfigurationEndpoints()
    {
        // POST configuration reload
        MockHttp.When(HttpMethod.Post, "https://aesir.localhost/configuration/reload")
            .Respond(() => Task.FromResult(CreateJsonResponse<object?>(null)));

        // GET system ready
        MockHttp.When(HttpMethod.Get, "https://aesir.localhost/configuration/systemready")
            .Respond(() =>
            {
                var isReady = InferenceEngines.Count > 0 && Agents.Count > 0;
                var reasons = new List<string>();
                if (InferenceEngines.Count == 0)
                    reasons.Add("No inference engines configured");
                if (Agents.Count == 0)
                    reasons.Add("No agents configured");
                if (string.IsNullOrEmpty(GeneralSettings.RagEmbeddingModel))
                    reasons.Add("RAG embedding model not set");

                var readiness = new AesirConfigurationReadinessBase
                {
                    IsReady = isReady && !string.IsNullOrEmpty(GeneralSettings.RagEmbeddingModel),
                    Reasons = reasons.AsEnumerable()
                };
                return Task.FromResult(CreateJsonResponse(readiness));
            });
    }

    protected HttpResponseMessage CreateJsonResponse<T>(T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(json);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return response;
    }

    protected void AddTestInferenceEngine(string name = "Test Engine", InferenceEngineType type = InferenceEngineType.Ollama)
    {
        InferenceEngines.Add(new AesirInferenceEngineBase
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test engine for integration testing",
            Type = type,
            Configuration = new Dictionary<string, string?> { { "BaseUrl", "http://localhost:11434" } }
        });
    }

    protected void AddTestAgent(string name = "Test Agent", string model = "llama3.2")
    {
        var engine = InferenceEngines.FirstOrDefault();
        Agents.Add(new AesirAgentBase
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test agent for integration testing",
            ChatInferenceEngineId = engine?.Id,
            ChatModel = model,
            ChatTemperature = 0.7,
            ChatTopP = 0.9,
            ChatMaxTokens = 4096
        });
    }

    protected void AddTestChatSession(string title = "Test Chat")
    {
        ChatSessions.Add(new AesirChatSessionItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            UpdatedAt = DateTimeOffset.Now
        });
    }

    protected void SetupTestGeneralSettings(Guid? embeddingEngineId = null, string? embeddingModel = null)
    {
        GeneralSettings = new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = embeddingEngineId ?? InferenceEngines.FirstOrDefault()?.Id,
            RagEmbeddingModel = embeddingModel ?? "nomic-embed-text:latest"
        };
    }

    protected void AddTestTool(string name = "Test Tool", ToolType type = ToolType.Internal)
    {
        Tools.Add(new AesirToolBase
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Description = "Test tool for integration testing"
        });
    }
}
