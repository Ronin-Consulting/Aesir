# Claude Code Guidelines

Guidelines and instructions for Claude Code when working in this project.

## Table of Contents

1. [Related Documentation](#related-documentation)
2. [About AESIR](#about-aesir)
3. [Project Configuration](#project-configuration)
4. [Code Generation](#code-generation)
5. [Data Access Guidelines](#data-access-guidelines)
6. [API Documentation](#api-documentation)
7. [Code Style Guidelines](#code-style-guidelines)
8. [Error Handling](#error-handling)
9. [Logging Guidelines](#logging-guidelines)
10. [Docker & Kubernetes](#docker--kubernetes)
11. [Blazor WebAssembly Client](#blazor-webassembly-client)
12. [Testing Guidelines](#testing-guidelines)
13. [Development Environment](#development-environment)
14. [Planning & Workflow](#planning--workflow)

---

## Related Documentation

Detailed docs exist for specific domains — read these on-demand when working in those areas:

| Document | Location | Covers |
|----------|----------|--------|
| Data Access | `DATA_ACCESS.md` | Full Dapper patterns, connection management, query examples |
| Repository Pattern | `REPOSITORY_PATTERN.md` | Base repository, CRUD, custom queries, soft deletes |
| Module System | `MODULE_SYSTEM.md` | Server module architecture, registration, DI patterns |
| Client Architecture | `Client/Aesir.Client.Web/CLIENT_ARCHITECTURE.md` | Blazor client structure, component patterns, state management |
| Active Issues | `ISSUES.md` | Known issues and bugs being tracked |

---

## About AESIR

AESIR is an AI-powered chat orchestration platform with:
- **Server**: .NET 10 modular API with PostgreSQL and Qdrant vector database
- **Client**: Blazor WebAssembly with Tauri desktop support
- **Features**: Multi-model inference (OpenAI, Ollama), Research mode, document handling, speech-to-text, MCP tools, vector search

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Client Applications                       │
├───────────────────┬───────────────────┬─────────────────────┤
│   Blazor WASM     │   Tauri Desktop   │   (Future: Mobile)  │
│  - Chat           │                   │                     │
│  - Research       │                   │                     │
│  - Settings       │                   │                     │
│  - HandsFree      │                   │                     │
│  - Observability  │                   │                     │
│  - Wizard         │                   │                     │
└─────────┬─────────┴─────────┬─────────┴──────────┬──────────┘
          │                   │                    │
          └───────────────────┼────────────────────┘
                              │ HTTPS (Traefik)
          ┌───────────────────▼───────────────────┐
          │           Aesir.Api.Server            │
          │        https://aesir.localhost        │
          └───────────────────┬───────────────────┘
                              │
   ┌──────────┬───────┬───────┼───────┬─────────┬──────────┬──────┐
   │          │       │       │       │         │          │      │
┌──▼──┐  ┌───▼───┐ ┌─▼──┐ ┌──▼───┐ ┌─▼───┐ ┌───▼────┐ ┌──▼──┐ ┌▼────┐
│Chat │  │Research│ │Mcp │ │Config│ │Infer│ │Storage │ │Orch │ │ ... │
└─────┘  └────────┘ └────┘ └──────┘ └──┬──┘ └────────┘ └─────┘ └─────┘
                                       │
          ┌────────────────────────────┼────────────────────┐
          │                            │                    │
    ┌─────▼─────┐              ┌───────▼───────┐   ┌───────▼───────┐
    │PostgreSQL │              │ Qdrant Vector │   │ External LLMs │
    │ (pgvector)│              │   Database    │   │(OpenAI/Ollama)│
    └───────────┘              └───────────────┘   └───────────────┘
```

### Module Overview

**Client Modules** (Blazor WebAssembly):
- **Chat** - Conversational AI interface with multi-model support
- **Research** - AI research mode with team collaboration UI
- **Settings** - Configuration management (agents, engines, MCP servers)
- **HandsFree** - Voice-activated chat and speech interaction
- **Observability** - Real-time logging and monitoring dashboard
- **Wizard** - Initial setup and onboarding flow

**Server Modules** (ASP.NET Core):
- **Chat** - Chat session management and message handling
- **Research** - Multi-agent research orchestration with peer review
- **Configuration** - Agent, engine, and MCP server configuration
- **Inference** - Multi-model inference orchestration (OpenAI, Ollama)
- **Orchestration** - Semantic Kernel plugin management, vector search, MCP tool orchestration
- **Mcp** - Model Context Protocol tool integration
- **Storage** - File and document storage management
- **Logging** - Centralized logging and kernel event tracking
- **Documents** - Document processing and ingestion
- **Speech** - Speech-to-text and audio processing

**Tools:**
- **LegalValidator** (`Tools/Aesir.Tools.LegalValidator/`) - License/legal compliance validation

### Quick Start

#### Development Scripts

| Script | Purpose | Options |
|--------|---------|---------|
| `./run-server.sh` | Start API server (Docker) | `--rebuild` `--prune` |
| `./run-blazor.sh` | Start Blazor dev server | `--clean-build` |
| `./run-tauri.sh` | Start Tauri desktop app | Requires Blazor running |
| `./launch_ollama.sh` | Start local Ollama instance | |

#### Getting Started

1. **Start API**: `./run-server.sh`
   - Builds Docker image, starts PostgreSQL/Traefik/Qdrant
   - Auto-tails API logs (Ctrl+C to stop following)

2. **Start Client**: `./run-blazor.sh`
   - Auto-kills existing processes on port 5173
   - Starts dev server (no hot reload - rebuild required for changes)

3. **Open**: http://localhost:5173/ or https://aesir.localhost

#### Script Options

- **run-server.sh**
  - `--rebuild`: Force full rebuild (no Docker cache)
  - `--prune`: Clean up Docker resources before building

- **run-blazor.sh**
  - `--clean-build`: Clean and rebuild before starting

- **run-tauri.sh**
  - Requires Blazor dev server running first in separate terminal

---

## Project Configuration

- **Target Framework**: .NET 10.0
- **C# Version**: C# 13
- All projects must target `net10.0` framework

---

## Code Generation

### Context7 Documentation

When generating code involving external libraries, you MUST fetch up-to-date documentation first:

```
User Request → Identify Libraries → Resolve Library ID → Fetch Docs → Generate Code
```

**Tools:**
- `mcp__context7__resolve-library-id` - Find library ID
- `mcp__context7__get-library-docs` - Retrieve documentation

**When to use Context7:**
- Adding FluentMigrator migrations → Fetch FluentMigrator docs
- Creating MudBlazor components → Fetch MudBlazor docs
- Implementing SignalR hubs → Fetch SignalR docs
- Writing Dapper queries → Fetch Dapper docs
- Configuring NLog → Fetch NLog docs

**Query parameters:**
- `topic`: Focus on relevant sections (e.g., "hooks", "routing")
- `tokens`: Default 5000, use 10000+ for complex topics

### Enforcement

Do not generate library-specific code without first consulting Context7 documentation. If documentation is unavailable, inform the user and proceed with caution, noting limitations.

---

## Data Access Guidelines

> **Detailed docs**: See `DATA_ACCESS.md` and `REPOSITORY_PATTERN.md` for full patterns and examples.

### Key Rules (Quick Reference)

- **ORM**: Dapper + Dapper.Contrib (NOT Entity Framework)
- **Database**: PostgreSQL 16 with pgvector extension
- **ALL tables**: `aesir_` prefix with snake_case (e.g., `aesir_chat_session`)
- **ALL columns**: snake_case (e.g., `first_name`, `is_active`)
- **C# properties**: PascalCase — `DapperColumnMapper` handles conversion
- **Primary Keys**: ALL entities use `Guid` type
- **Schema**: All tables in `aesir` schema, NOT `public`
- **SQL**: Always use `aesir_` prefix — `SELECT * FROM aesir_product WHERE is_deleted = false`
- **Connections**: Use `IDbConnectionFactory`, always wrap in `using`, use `.ConfigureAwait(false)`
- **Soft deletes**: Filter `WHERE is_deleted = false`, override `RemoveAsync` to UPDATE
- **Audit fields**: Set `CreatedAt`/`UpdatedAt` etc. in service layer, not repository

### Migrations

- **Tool**: FluentMigrator 7.1.0
- **Location**: `[ModuleProject]/Migrations/`
- **Naming**: `[Migration(YYYYMMDDHHMMSS)]`
- **Requirements**: Always implement both `Up()` and `Down()` methods
- **Primary Keys**: Use `.AsGuid()` (NOT `.AsInt32().Identity()`)

---

## API Documentation

### Swagger

- **Endpoint**: `/swagger` (Development only)
- **URL**: https://aesir.localhost/swagger
- **OpenAPI Version**: v1

### API Routes

**IMPORTANT**: API routes do NOT use an `/api/` prefix. Use the module route directly:

| Module | Route | Examples |
|--------|-------|----------|
| Chat | `/chat/...` | `/chat/sessions`, `/chat/messages` |
| Research | `/research/...` | `/research/sessions`, `/research/hub` (SignalR) |
| Configuration | `/configuration/...` | `/configuration/agents`, `/configuration/engines` |
| Inference | `/inference/...` | `/inference/chat`, `/inference/models` |
| Mcp | `/mcp/...` | `/mcp/servers`, `/mcp/tools` |
| Storage | `/storage/...` | `/storage/upload`, `/storage/files` |
| Logging | `/logging/...` | `/logging/kernel-logs`, `/logging/filters` |

```bash
# CORRECT
curl https://aesir.localhost/configuration/agents
curl https://aesir.localhost/research/sessions
curl https://aesir.localhost/chat/sessions

# WRONG - will return 404
curl https://aesir.localhost/api/configuration/agents
```

---

## Code Style Guidelines

- **Naming**: PascalCase for classes/interfaces/methods, camelCase for locals
- **Interfaces**: Prefix with "I" (e.g., `IChatService`, `IDbConnectionFactory`)
- **Nullable**: Enable `<Nullable>enable</Nullable>`
- **Async**: Use async/await with `Task<T>`, suffix methods with "Async"
- **ConfigureAwait**: Use `.ConfigureAwait(false)` in libraries

### Dependency Injection

| Service Type | Lifetime |
|--------------|----------|
| `IDbConnectionFactory` | Singleton |
| Repositories | Scoped |
| `IUnitOfWork` | Scoped |

### Blazor Components

- Use code-behind pattern for complex logic
- See [Blazor WebAssembly Client](#blazor-webassembly-client) section

### JSON Serialization (CRITICAL)

**Client-Server JSON communication uses `snake_case` property names.**

The Blazor client models in `Aesir.Common` use `[JsonPropertyName("snake_case")]` attributes. Server models in `Aesir.Modules.*` **MUST also use matching `[JsonPropertyName]` attributes** to ensure proper deserialization.

**Common Issue**: 400 Bad Request errors when saving data often indicate a JSON property name mismatch between client and server.

**Required for all API models:**

```csharp
using System.Text.Json.Serialization;

public class MyEntity : IEntity
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation properties should be ignored
    [JsonIgnore]
    public ParentEntity? Parent { get; set; }
}
```

**Enum Serialization**: Enums shared between client/server MUST have `[JsonConverter(typeof(JsonStringEnumConverter))]`:

```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MyStatus
{
    Pending,
    Active,
    Completed
}
```

**Checklist for new API models:**
1. Add `[JsonPropertyName("snake_case")]` to ALL public properties
2. Add `[JsonIgnore]` to navigation properties
3. Add `[JsonConverter(typeof(JsonStringEnumConverter))]` to enums
4. Match property names exactly with corresponding `Aesir.Common` models

---

## Error Handling

- **Controllers**: Return `Ok()`, `BadRequest()`, `NotFound()` — log errors with correlation ID
- **Services**: Catch specific exceptions, throw custom exceptions for business rules, let infra exceptions bubble up
- **Blazor client**: Try/catch around API calls, show user-friendly messages via `Snackbar.Add()`, log errors for debugging

---

## Logging Guidelines

- **Framework**: NLog 6.x (`NLog.Web.AspNetCore` for server, `NLog.Extensions.Logging` elsewhere)
- **Config**: `nlog.config` at `Server/Aesir.Api.Server/nlog.config`
- **ALWAYS use structured logging**: `_logger.LogInformation("Creating: {Username}", name)` — NEVER use string interpolation
- **DO NOT LOG**: Passwords, API keys, credit card numbers, SSNs
- **Correlation IDs**: `CorrelationIdMiddleware` in `Aesir.Infrastructure.Middleware`, header `X-Correlation-Id`

---

## Docker & Kubernetes

### Docker Configuration

- **Dockerfile**: `Server/Aesir.Api.Server/Dockerfile`
- **Base Images**: `mcr.microsoft.com/dotnet/sdk:10.0` (build), `aspnet:10.0` (runtime)
- **User**: Run as `appuser` (UID 1000)
- **Health Check**: `/health` endpoint

### Docker Compose

- **Development**: `docker-compose-api-dev.yml`
- **Services**:
  - **aesir-api**: API server
  - **pgdb**: PostgreSQL 16 with pgvector extension
  - **reverse-proxy**: Traefik for HTTPS routing
  - **qdrant**: Qdrant vector database for semantic search
- **Environment**: Configure via `.env` file (never commit!)

```bash
# Build and start
./run-server.sh

# Or manually
docker compose -f docker-compose-api-dev.yml up -d
```

### Docker Resource Limits

Resource limits configured in `docker-compose-api-dev.yml`. API: 4 CPU/4GB, PostgreSQL: 2 CPU/2GB, Qdrant: 2 CPU/2GB, Traefik: 1 CPU/512MB.

### Qdrant Vector Database

**Purpose**: Semantic search, embeddings storage, and vector similarity search

**Configuration**: API key and connection details in `.claude.local.md` (gitignored).
- **Port**: 6333 (HTTP API), 6334 (gRPC)
- **Storage**: Persistent volume (`qdrant_storage`)

**Usage**: Embedding storage for document search, semantic similarity queries, vector-based retrieval for RAG systems.

### Health Check

- **Path**: `/health`
- **200 OK**: Healthy
- **503**: Unhealthy

### Kubernetes (K3s)

Namespace `aesir`. PostgreSQL as StatefulSet (10Gi PV), API as Deployment (2 replicas). Uses ConfigMap for config, Secrets for credentials. Run containers as non-root, use read-only root filesystem where possible.

---

## Blazor WebAssembly Client

### Project Structure

```
Client/Aesir.Client.Web/
├── Aesir.Client.Web.App/           # Main WASM application
├── Aesir.Client.Web.Infrastructure/ # Shared services, API client
├── Modules/                         # Feature modules
│   ├── Aesir.Client.Web.Modules.Chat/         # Chat interface
│   ├── Aesir.Client.Web.Modules.Research/     # Research mode UI
│   ├── Aesir.Client.Web.Modules.Settings/     # Configuration UI
│   ├── Aesir.Client.Web.Modules.HandsFree/    # Voice interaction
│   ├── Aesir.Client.Web.Modules.Observability/# Logging dashboard
│   └── Aesir.Client.Web.Modules.Wizard/       # Setup wizard
└── src-tauri/                       # Tauri desktop config
```

### UI Framework

- **MudBlazor 8.x** (Material Design)
- Required providers in App.razor: `MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider`

### Module System

> **Detailed docs**: See `MODULE_SYSTEM.md` and `Client/Aesir.Client.Web/CLIENT_ARCHITECTURE.md`.

**Modules should NOT depend on each other.** Allowed dependencies: `Aesir.Client.Web.Infrastructure`, `Aesir.Common`, external packages (MudBlazor).

**Cross-module communication**: Use event notifier pattern (e.g., `IConfigurationChangedNotifier`, `IChatSessionNotifier`).

| Module | Purpose |
|--------|---------|
| Chat | Conversational AI interface, multi-model support, tool call visualization |
| Research | Multi-agent research orchestration, real-time SignalR updates, peer review |
| Settings | Agent/engine/MCP server configuration, dynamic tab registration |
| HandsFree | Voice-activated chat, speech-to-text, audio playback |
| Observability | Real-time kernel log viewing, filtering, debug dashboard |
| Wizard | First-time setup, engine config wizard, onboarding |

### Creating a New Module

1. Create project at `Modules/Aesir.Client.Web.Modules.{Name}/`
2. Add project reference in `Aesir.Client.Web.App.csproj`
3. Implement `ClientModuleBase`
4. Register in `Program.cs`: `builder.Services.AddModule<MyModule>()`
5. Add namespace to `_Imports.razor`
6. Add assembly to router in `App.razor`

### API Client

```csharp
@inject IApiClient ApiClient

@code {
    private List<Agent>? _agents;

    protected override async Task OnInitializedAsync()
    {
        _agents = await ApiClient.GetAsync<List<Agent>>("/configuration/agents");
    }
}
```

### SignalR Real-Time Communication

**Research Hub** (`/research/hub`):
- Real-time research progress updates
- Team member status broadcasts
- Peer review notifications
- Report generation updates

**Usage**:
```csharp
@inject HubConnection HubConnection

protected override async Task OnInitializedAsync()
{
    HubConnection.On<ResearchProgressDto>("ReceiveResearchProgress", HandleProgress);
    await HubConnection.StartAsync();
}
```

### Tauri Desktop

- Config: `src-tauri/tauri.conf.json`
- Dev: `./run-tauri.sh` (requires Blazor running)
- Build: `cargo tauri build`

---

## Testing Guidelines

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test Server/Modules/Aesir.Modules.Chat.Tests/

# With timeout
dotnet test --timeout 180000
```

### Test Projects

| Project | Location |
|---------|----------|
| Chat.Tests | `Server/Modules/Aesir.Modules.Chat.Tests/` |
| Configuration.Tests | `Server/Modules/Aesir.Modules.Configuration.Tests/` |
| Inference.Tests | `Server/Modules/Aesir.Modules.Inference.Tests/` |
| Logging.Tests | `Server/Modules/Aesir.Modules.Logging.Tests/` |
| Storage.Tests | `Server/Modules/Aesir.Modules.Storage.Tests/` |
| Speech.Tests | `Server/Modules/Aesir.Modules.Speech.Tests/` |
| Research.Tests | `Server/Modules/Aesir.Modules.Research.Tests/` |
| Infrastructure.Tests | `Server/Aesir.Infrastructure.Tests/` |
| Client.Web.Tests | `Client/Aesir.Client.Web/Aesir.Client.Web.Tests/` |

### Test Patterns

- Use xUnit for all tests
- Mock dependencies with Moq or NSubstitute
- Name tests: `{Method}_{Scenario}_{ExpectedResult}`

### Browser Testing

- **URL**: http://localhost:5173/
- Use `./run-blazor.sh` to start dev server
- Always stop existing processes before restarting

### After Code Changes

- Run unit tests after each change
- Update or create tests as needed
- Include appropriate timeout for test runs

---

## Development Environment

### API Server

**CRITICAL**: Always run API from Docker, never locally.

Connection strings use Docker service names (`pgdb`) which only resolve within Docker network.

### URLs

| Service | URL |
|---------|-----|
| API | https://aesir.localhost |
| Blazor Dev | http://localhost:5173 |
| Swagger | https://aesir.localhost/swagger |
| Traefik Dashboard | http://localhost:8080 |
| PostgreSQL | localhost:5432 |
| Qdrant API | http://localhost:6333 |
| Qdrant (Traefik) | https://qdrant.localhost |

### Database Access

**Connection details and credentials**: See `.claude.local.md` (gitignored).

**IMPORTANT**: All application tables are in the `aesir` schema, not `public`. Always prefix table names with `aesir.` in SQL queries.

**Configuration Mode:**
- `LoadFromDatabase: true` → Uses database tables (`aesir_agent`, etc.)
- `LoadFromDatabase: false` → Uses `appsettings.json` configuration

---

## Planning & Workflow

### Work Plans

- Create detailed plan and wait for approval before implementing
- Add work plans to Solution Items in `Aesir.sln`
- Naming: `WORK_PLAN_RELEASE_{N}.md`

### Workflow Rules

**Before Implementation:**
- Create detailed plan
- Wait for user approval

**After Implementation:**
- Run unit tests after each code change
- Update or create tests if needed

**Testing:**
- Always stop existing `dotnet` processes before restarting Blazor (use `./run-blazor.sh` which handles this)
- Do NOT use `dotnet watch` - it is unstable; use `dotnet run` instead
- Include appropriate timeout for test runs

### Branding

- Application name: "Aesir" or "AESIR" (stylized)
