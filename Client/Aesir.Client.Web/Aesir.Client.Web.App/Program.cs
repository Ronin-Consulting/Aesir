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

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add MudBlazor services
builder.Services.AddMudServices();

// Add theme service
builder.Services.AddScoped<IThemeService, ThemeService>();

// Add platform services (Tauri detection, native file operations)
builder.Services.AddPlatformServices();

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

// Build the app
var app = builder.Build();

// Initialize module navigation
app.Services.InitializeModuleNavigation();

await app.RunAsync();
