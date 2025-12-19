using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Aesir.Client.Web.App;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Modules;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Chat;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Client.Web.Modules.Settings;
using Aesir.Client.Web.Modules.Wizard;
using Aesir.Client.Web.Modules.Observability;
using Aesir.Client.Web.Modules.HandsFree;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add MudBlazor services
builder.Services.AddMudServices();

// Add theme service
builder.Services.AddScoped<IThemeService, ThemeService>();

// Add configuration change notifier (singleton to share across all components)
builder.Services.AddSingleton<IConfigurationChangedNotifier, ConfigurationChangedNotifier>();

// Add chat session notifier (singleton to share session events across modules)
builder.Services.AddSingleton<IChatSessionNotifier, ChatSessionNotifier>();

// Add chat preferences service (shared between Chat and HandsFree modules)
builder.Services.AddSingleton<IChatPreferencesService, ChatPreferencesService>();

// Add agent tools service (shared between Chat and HandsFree modules)
builder.Services.AddSingleton<IAgentToolsService, AgentToolsService>();

// Add markdown service (shared between Chat and HandsFree modules)
builder.Services.AddSingleton<IMarkdownService, MarkdownService>();

// Add platform services (Tauri detection, native file operations)
builder.Services.AddPlatformServices();

// Add audio services for hands-free mode
builder.Services.AddAudioServices();

// Add module infrastructure
builder.Services.AddModuleInfrastructure();

// Add API client
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
builder.Services.AddAesirApiClient(apiBaseUrl);

// Register DocumentApiService with typed HttpClient (same base URL as IApiClient)
builder.Services.AddHttpClient<IDocumentApiService, DocumentApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Register modules (explicit registration for compile-time component visibility)
builder.Services.AddModule<ChatModule>();
builder.Services.AddModule<SettingsModule>();
builder.Services.AddModule<WizardModule>();
builder.Services.AddModule<ObservabilityModule>();
builder.Services.AddModule<HandsFreeModule>();

// Build the app
var app = builder.Build();

// Initialize module navigation
app.Services.InitializeModuleNavigation();

await app.RunAsync();
