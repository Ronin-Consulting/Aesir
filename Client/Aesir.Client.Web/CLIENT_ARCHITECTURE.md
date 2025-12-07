# AESIR Web Client Architecture

## Overview

The AESIR Web Client is a cross-platform desktop application built with:
- **Blazor WebAssembly** - C# frontend running in the browser via WebAssembly
- **MudBlazor** - Material Design component library
- **Tauri** - Native desktop wrapper (Windows, macOS, Linux)

This architecture enables:
- Browser-based development with hot reload
- Native desktop distribution with small bundle size (~20-30MB)
- Shared C# codebase with the AESIR server
- Direct use of `Aesir.Common` models

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                           AESIR Web Client                          │
├─────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                    Aesir.Client.Web.App                       │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐   │   │
│  │  │   Layout/   │  │   Pages/    │  │      App.razor      │   │   │
│  │  │ MainLayout  │  │  Home.razor │  │  (MudBlazor setup)  │   │   │
│  │  └─────────────┘  └─────────────┘  └─────────────────────┘   │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                │                                     │
│                                ▼                                     │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │              Aesir.Client.Web.Infrastructure                  │   │
│  │  ┌─────────────────────┐  ┌──────────────────────────────┐   │   │
│  │  │        Http/        │  │          Modules/            │   │   │
│  │  │  - IApiClient       │  │  - IClientModule             │   │   │
│  │  │  - ApiClient        │  │  - ClientModuleBase          │   │   │
│  │  │  - Streaming        │  │  - INavigationRegistry       │   │   │
│  │  └─────────────────────┘  └──────────────────────────────┘   │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                │                                     │
│                                ▼                                     │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                      Feature Modules                          │   │
│  │  ┌───────────────────────────────────────────────────────┐   │   │
│  │  │           Aesir.Client.Web.Modules.Chat               │   │   │
│  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐   │   │   │
│  │  │  │ ChatModule  │  │   Pages/    │  │ Components/ │   │   │   │
│  │  │  │ (register)  │  │  ChatPage   │  │ ChatMessage │   │   │   │
│  │  │  └─────────────┘  │  HistoryPage│  └─────────────┘   │   │   │
│  │  │                   └─────────────┘                     │   │   │
│  │  └───────────────────────────────────────────────────────┘   │   │
│  │  ┌───────────────────────────────────────────────────────┐   │   │
│  │  │        Aesir.Client.Web.Modules.Settings              │   │   │
│  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐   │   │   │
│  │  │  │ SettingsModule│ │   Pages/   │  │ Components/ │   │   │   │
│  │  │  │ (register)  │  │ EnginesPage │  │ EditDialogs │   │   │   │
│  │  │  └─────────────┘  │ AgentsPage  │  └─────────────┘   │   │   │
│  │  │                   │ ToolsPage   │                     │   │   │
│  │  │                   │ McpServers  │                     │   │   │
│  │  │                   └─────────────┘                     │   │   │
│  │  └───────────────────────────────────────────────────────┘   │   │
│  │  ┌───────────────────────────────────────────────────────┐   │   │
│  │  │         Aesir.Client.Web.Modules.Wizard               │   │   │
│  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐   │   │   │
│  │  │  │WizardModule │  │   Pages/    │  │ Components/ │   │   │   │
│  │  │  │ (register)  │  │ SetupWizard │  │ WizardSteps │   │   │   │
│  │  │  └─────────────┘  └─────────────┘  └─────────────┘   │   │   │
│  │  │  ┌─────────────┐  ┌─────────────────────────────┐    │   │   │
│  │  │  │   Layout/   │  │        Services/            │    │   │   │
│  │  │  │WizardLayout │  │ IWizardStateService         │    │   │   │
│  │  │  └─────────────┘  └─────────────────────────────┘    │   │   │
│  │  └───────────────────────────────────────────────────────┘   │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                │                                     │
├────────────────────────────────┼────────────────────────────────────┤
│                                ▼                                     │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │                      Aesir.Common                             │   │
│  │              (Shared models, DTOs, utilities)                 │   │
│  └──────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
                                 │
                                 │ HTTP/SignalR
                                 ▼
                    ┌───────────────────────┐
                    │     AESIR Server      │
                    │    (REST API + Hubs)  │
                    └───────────────────────┘
```

## Project Structure

```
Client/Aesir.Client.Web/
├── Aesir.Client.Web.App/              # Main application
│   ├── Layout/
│   │   └── MainLayout.razor           # Shell with navigation
│   ├── Pages/
│   │   └── Home.razor                 # Home page
│   ├── wwwroot/
│   │   ├── index.html                 # Entry point
│   │   ├── appsettings.json           # Client configuration
│   │   └── appsettings.Development.json
│   ├── App.razor                      # Root component
│   ├── Program.cs                     # Service registration
│   ├── _Imports.razor                 # Global usings
│   └── Aesir.Client.Web.App.csproj
│
├── Aesir.Client.Web.Infrastructure/   # Shared infrastructure
│   ├── Http/
│   │   ├── IApiClient.cs              # API client interface
│   │   ├── ApiClient.cs               # Implementation with streaming
│   │   └── ApiClientExtensions.cs     # DI registration
│   ├── Modules/
│   │   ├── IClientModule.cs           # Module interface
│   │   ├── ClientModuleBase.cs        # Base class
│   │   ├── INavigationRegistry.cs     # Navigation interface
│   │   ├── NavigationRegistry.cs      # Implementation
│   │   └── ModuleServiceExtensions.cs # DI helpers
│   └── Aesir.Client.Web.Infrastructure.csproj
│
├── Modules/
│   ├── Aesir.Client.Web.Modules.Chat/    # Chat feature module
│   │   ├── Layout/
│   │   │   └── ChatLayout.razor          # Chat sidebar layout
│   │   ├── Pages/
│   │   │   ├── ChatPage.razor            # Main chat UI
│   │   │   └── ChatHistoryPage.razor     # History listing
│   │   ├── Components/
│   │   │   ├── ChatMessage.razor         # Message bubble
│   │   │   ├── AgentSelector.razor       # Agent dropdown
│   │   │   └── MessageInput.razor        # Chat input
│   │   ├── Services/
│   │   │   ├── IChatStateService.cs      # Chat state management
│   │   │   └── IChatHistoryService.cs    # History management
│   │   ├── ChatModule.cs                 # Module registration
│   │   └── Aesir.Client.Web.Modules.Chat.csproj
│   │
│   ├── Aesir.Client.Web.Modules.Settings/ # Settings feature module
│   │   ├── Pages/
│   │   │   ├── SettingsPage.razor        # Settings overview
│   │   │   ├── InferenceEnginesPage.razor# Inference engine config
│   │   │   ├── AgentsPage.razor          # Agent configuration
│   │   │   ├── ToolsPage.razor           # Tool configuration
│   │   │   ├── GeneralSettingsPage.razor # General settings (RAG config)
│   │   │   └── McpServersPage.razor      # MCP server config
│   │   ├── Components/
│   │   │   ├── InferenceEngineEditDialog.razor
│   │   │   ├── AgentEditDialog.razor
│   │   │   ├── ToolEditDialog.razor
│   │   │   ├── McpServerEditDialog.razor
│   │   │   └── ModelSelector.razor       # Reusable model dropdown
│   │   ├── Services/
│   │   │   ├── IInferenceEngineService.cs
│   │   │   ├── IAgentService.cs
│   │   │   ├── IToolService.cs
│   │   │   ├── IGeneralSettingsService.cs
│   │   │   └── IMcpServerService.cs
│   │   ├── SettingsModule.cs             # Module registration
│   │   └── Aesir.Client.Web.Modules.Settings.csproj
│   │
│   └── Aesir.Client.Web.Modules.Wizard/  # Setup wizard module
│       ├── Layout/
│       │   └── WizardLayout.razor        # Full-screen wizard layout
│       ├── Pages/
│       │   └── SetupWizardPage.razor     # Main wizard page
│       ├── Components/
│       │   ├── WizardWelcomeStep.razor   # Welcome & overview
│       │   ├── WizardInferenceEngineStep.razor # Engine config
│       │   ├── WizardGeneralSettingsStep.razor # RAG settings
│       │   ├── WizardAgentStep.razor     # Agent creation
│       │   └── WizardCompleteStep.razor  # Summary & finish
│       ├── Services/
│       │   ├── IWizardStateService.cs    # Wizard state management
│       │   └── WizardStateService.cs     # localStorage persistence
│       ├── WizardModule.cs               # Module registration
│       └── Aesir.Client.Web.Modules.Wizard.csproj
│
├── Aesir.Client.Web.Tests/               # Test project
│   ├── Unit/                             # Unit tests
│   │   ├── Services/                     # Service tests
│   │   ├── Components/                   # Component tests
│   │   └── Http/                         # API client tests
│   ├── Integration/                      # Integration tests
│   │   └── Flows/                        # Flow tests
│   │       ├── WizardFlowTests.cs        # Wizard step flow
│   │       ├── SettingsFlowTests.cs      # Settings CRUD flow
│   │       ├── ConfigurationFlowTests.cs # Config workflow
│   │       ├── ChatFlowTests.cs          # Chat workflow
│   │       └── ErrorScenarioTests.cs     # Error handling
│   └── Aesir.Client.Web.Tests.csproj
│
└── src-tauri/                            # Tauri desktop wrapper
    ├── src/
    │   ├── main.rs                    # Rust entry point
    │   └── lib.rs                     # Tauri app setup
    ├── icons/                         # App icons
    ├── tauri.conf.json                # Tauri configuration
    └── Cargo.toml                     # Rust dependencies
```

## Module System

### Design Philosophy

The client uses a hybrid module approach:

| Aspect | Mechanism | Reason |
|--------|-----------|--------|
| Component visibility | Explicit project references | Razor compilation is build-time |
| Service registration | Runtime discovery | DI is runtime |
| Route discovery | Runtime (Blazor built-in) | `@page` directives scanned |
| Navigation | Runtime via `INavigationRegistry` | Dynamic menu building |

### IClientModule Interface

```csharp
public interface IClientModule
{
    string Name { get; }
    string Version { get; }
    string Description { get; }

    void RegisterServices(IServiceCollection services);
    void RegisterNavigation(INavigationRegistry registry);
}
```

### Module Registration Flow

```
1. Program.cs
   └─> builder.Services.AddModule<ChatModule>()
       └─> new ChatModule().RegisterServices(services)
       └─> services.AddSingleton<IClientModule>(module)

2. After Build
   └─> app.Services.InitializeModuleNavigation()
       └─> foreach module: module.RegisterNavigation(registry)

3. Runtime
   └─> MainLayout.razor
       └─> @inject INavigationRegistry
       └─> NavigationRegistry.GetItems() → Build menu
```

## API Client

### Architecture

```
┌─────────────────────────────────────────────────────┐
│                   Component                          │
│  @inject IApiClient ApiClient                       │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────┐
│                   ApiClient                          │
│  - Uses HttpClient (from IHttpClientFactory)        │
│  - JSON serialization                               │
│  - Streaming via IAsyncEnumerable                   │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────┐
│              AESIR Server API                        │
│  - /api/chat/completions (streaming)                │
│  - /api/configuration/agents                        │
│  - /api/chat/history                                │
└─────────────────────────────────────────────────────┘
```

### Streaming Support

The API client supports server-sent streaming for chat responses:

```csharp
public async IAsyncEnumerable<T> StreamPostAsync<T>(
    string endpoint,
    object data,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
    {
        Content = JsonContent.Create(data)
    };

    using var response = await _httpClient.SendAsync(
        request,
        HttpCompletionOption.ResponseHeadersRead,  // Don't buffer
        ct);

    await using var stream = await response.Content.ReadAsStreamAsync(ct);

    await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(stream))
    {
        if (item is not null)
            yield return item;
    }
}
```

## State Management

### Recommended Patterns

1. **Component State** - For UI-only state
   ```csharp
   @code {
       private bool _isLoading;
       private List<Message> _messages = [];
   }
   ```

2. **Scoped Services** - For feature-level state
   ```csharp
   public class ChatStateService
   {
       public AesirConversation CurrentConversation { get; set; }
       public event Action? OnStateChanged;
   }
   ```

3. **Cascading Values** - For cross-component state
   ```razor
   <CascadingValue Value="_currentUser">
       @ChildContent
   </CascadingValue>
   ```

### When to Use Each

| Pattern | Use When |
|---------|----------|
| Component state | State is local to one component |
| Scoped service | State shared across a feature/module |
| Cascading value | State needed by many nested components |

## Tauri Integration

### How It Works

```
┌─────────────────────────────────────────────────────┐
│                 Tauri Desktop App                    │
│  ┌─────────────────────────────────────────────┐    │
│  │              Native Window                   │    │
│  │  ┌───────────────────────────────────────┐  │    │
│  │  │           OS WebView                   │  │    │
│  │  │  ┌─────────────────────────────────┐  │  │    │
│  │  │  │     Blazor WASM Application     │  │  │    │
│  │  │  │   (Running in WebAssembly)      │  │  │    │
│  │  │  └─────────────────────────────────┘  │  │    │
│  │  └───────────────────────────────────────┘  │    │
│  └─────────────────────────────────────────────┘    │
│                                                      │
│  Rust Backend (optional native features)            │
└─────────────────────────────────────────────────────┘
```

### Configuration

Key settings in `tauri.conf.json`:

```json
{
  "productName": "AESIR",
  "identifier": "com.aesir.client",
  "build": {
    "frontendDist": "../Aesir.Client.Web.App/bin/Release/net10.0/publish/wwwroot",
    "devUrl": "http://localhost:5173",
    "beforeBuildCommand": "dotnet publish Aesir.Client.Web.App -c Release"
  },
  "app": {
    "windows": [{ "width": 1200, "height": 800 }]
  }
}
```

## Development Workflow

### Browser Development (Primary)

```bash
cd Client/Aesir.Client.Web/Aesir.Client.Web.App
dotnet watch run --urls "http://localhost:5173"
```

Benefits:
- Hot reload for Razor components
- Browser DevTools for debugging
- Fast iteration cycle

### Desktop Testing

```bash
cd Client/Aesir.Client.Web
cargo tauri dev
```

The desktop window connects to the dev server, so changes are reflected in real-time.

### Production Build

```bash
cd Client/Aesir.Client.Web
cargo tauri build
```

Output:
- macOS: `.app` bundle and `.dmg` installer
- Windows: `.exe` installer
- Linux: `.AppImage` and `.deb`

## Testing Architecture

### Test Project Structure

The `Aesir.Client.Web.Tests` project uses:
- **bUnit** - Blazor component testing
- **xUnit** - Test framework
- **FluentAssertions** - Assertion library
- **RichardSzalay.MockHttp** - HTTP mocking

### Unit Tests

Unit tests cover individual services and components in isolation:

```csharp
public class ChatStateServiceTests
{
    [Fact]
    public void SelectAgent_ShouldUpdateSelectedAgent()
    {
        var service = new ChatStateService();
        var agent = new AesirAgentBase { Name = "Test" };

        service.SelectAgent(agent);

        service.SelectedAgent.Should().Be(agent);
    }
}
```

### Integration Tests

Integration tests verify complete workflows with mocked HTTP:

```csharp
public class WizardFlowTests : IntegrationTestBase
{
    [Fact]
    public async Task WizardFlow_NavigateForward_ThroughAllSteps()
    {
        var wizardState = Services.GetRequiredService<IWizardStateService>();

        // Verify initial state
        wizardState.CurrentStep.Should().Be(WizardStep.Welcome);

        // Navigate through steps
        await wizardState.GoToNextStepAsync();
        wizardState.CurrentStep.Should().Be(WizardStep.InferenceEngine);

        await wizardState.GoToNextStepAsync();
        wizardState.CurrentStep.Should().Be(WizardStep.GeneralSettings);

        // ... continue through all steps
    }
}

public class SettingsFlowTests : IntegrationTestBase
{
    [Fact]
    public async Task InferenceEngineLifecycle_CreateUpdateDelete()
    {
        var engineService = Services.GetRequiredService<IInferenceEngineService>();

        // Create
        var createResult = await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "Test Engine",
            Type = InferenceEngineType.Ollama
        });
        createResult.IsSuccess.Should().BeTrue();

        // Read
        var allEngines = await engineService.GetAllAsync();
        allEngines.Value.Should().ContainSingle(e => e.Name == "Test Engine");

        // Update
        var engine = allEngines.Value.First();
        engine.Name = "Updated Engine";
        var updateResult = await engineService.UpdateAsync(engine);
        updateResult.IsSuccess.Should().BeTrue();

        // Delete
        var deleteResult = await engineService.DeleteAsync(engine.Id!.Value);
        deleteResult.IsSuccess.Should().BeTrue();
    }
}
```

### IntegrationTestBase

The base class provides:
- Mock HTTP message handler with realistic API behavior
- Service registration matching production
- Test data collections (InferenceEngines, Agents, etc.)
- Helper methods for test data setup

## API Result Pattern

### ApiResult<T>

All API operations return `ApiResult<T>` for consistent error handling:

```csharp
public class ApiResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public HttpStatusCode? StatusCode { get; }

    public static ApiResult<T> Success(T value);
    public static ApiResult<T> Failure(string error);

    public ApiResult<TNew> Map<TNew>(Func<T, TNew> mapper);
    public ApiResult<T> OnSuccess(Action<T> action);
    public ApiResult<T> OnFailure(Action<string> action);
}
```

### Usage Pattern

```csharp
var result = await engineService.CreateAsync(engine);

result
    .OnSuccess(id => Snackbar.Add("Created successfully", Severity.Success))
    .OnFailure(error => Snackbar.Add($"Failed: {error}", Severity.Error));
```

## Performance Optimizations

### Virtualized Lists

Chat history uses Blazor's `<Virtualize>` component for performance with large datasets:

```razor
<Virtualize Items="@sessions" Context="session" ItemSize="56" OverscanCount="5">
    <ChatHistoryItem Session="session" />
</Virtualize>
```

### Loading Skeletons

Components show skeleton loaders during data fetching for better perceived performance:

```razor
@if (_isLoading)
{
    @for (var i = 0; i < 5; i++)
    {
        <MudSkeleton SkeletonType="SkeletonType.Text" Width="80%" Animation="Animation.Wave" />
    }
}
```

## Setup Wizard

### Overview

The Setup Wizard provides a guided first-run experience for new users. It forces unconfigured users through a full-screen setup flow before they can use the application.

### Flow

```
┌─────────────────────────────────────────────────────────────┐
│                        Setup Wizard                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐ │
│  │ Welcome  │ → │ Inference│ → │ Settings │ → │  Agent   │ │
│  │          │   │  Engine  │   │  (RAG)   │   │          │ │
│  └──────────┘   └──────────┘   └──────────┘   └──────────┘ │
│                                                     │        │
│                                                     ▼        │
│                                              ┌──────────┐    │
│                                              │ Complete │    │
│                                              └──────────┘    │
│                                                     │        │
│                                                     ▼        │
│                                              Navigate to /   │
└─────────────────────────────────────────────────────────────┘
```

### WizardLayout

The wizard uses a dedicated full-screen layout (`WizardLayout.razor`) that:
- Removes navigation chrome (no app bar, no drawer)
- Shows gradient background filling the viewport
- Centers the wizard card with dramatic shadow

### WizardStateService

Manages wizard state with localStorage persistence:

```csharp
public interface IWizardStateService
{
    event EventHandler? StateChanged;

    Task<bool> CheckWizardCompletedAsync();
    Task CompleteWizardAsync();
    Task ResetWizardAsync();
}
```

### Forced Redirect

The `MainLayout.razor` checks system readiness on first render:

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // Check if wizard completed (localStorage)
        var wizardCompleted = await WizardState.CheckWizardCompletedAsync();
        if (wizardCompleted) return;

        // Check system readiness from API
        var readinessResult = await ConfigurationService.GetSystemReadinessAsync();
        if (readinessResult.IsSuccess && readinessResult.Value?.IsReady == true)
        {
            await WizardState.CompleteWizardAsync();
            return;
        }

        // Not ready - force to wizard
        Navigation.NavigateTo("/setup");
    }
}
```

## Error Handling

### AppErrorBoundary

The application wraps the router with a global error boundary (`AppErrorBoundary.razor`) that catches unhandled exceptions:

```razor
<AppErrorBoundary>
    <Router AppAssembly="@typeof(App).Assembly" ...>
        ...
    </Router>
</AppErrorBoundary>
```

Features:
- Shows friendly error message
- "Try Again" button to recover
- "Go Home" button to navigate away
- Toggle to show/hide technical details

### Empty States

Components handle empty data gracefully:

| Component | State | Handling |
|-----------|-------|----------|
| ChatWelcome | No agents | Shows icon, message, buttons to configure or run wizard |
| ChatWelcome | No inference engines | Warning with setup links |
| ChatWelcome | API error | "Retry Connection" button |
| Settings pages | No records | Helpful message in data grid |
| App.razor | 404 | Styled not-found page with icon |

### API Result Pattern

All services return `ApiResult<T>` for consistent error handling:

```csharp
var result = await service.GetAllAsync();

result
    .OnSuccess(items => _items = items)
    .OnFailure(error => _errorMessage = error);
```

## Future Enhancements

### Planned Modules
- `Aesir.Client.Web.Modules.Documents` - Document management
- `Aesir.Client.Web.Modules.Prompts` - Prompt library

### Infrastructure Improvements
- JWT authentication in API client
- Polly resilience policies (retry, circuit breaker)
- SignalR hub connections for real-time updates
- Offline support with local storage

### Native Features via Tauri
- System tray integration
- Native notifications
- File system access (for document upload)
- Auto-updater
