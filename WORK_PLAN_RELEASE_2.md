# AESIR Web Client - Release 2 Work Plan
## Initial Chat Interface with Administration

**Created:** 2025-12-01
**Status:** Complete
**Completed:** 2025-12-02
**Branch:** `aesir-client-x`

---

## Overview

This release implements the initial AESIR web client chat interface with full administration capabilities. The client will connect to the existing AESIR server API (via `docker-compose-api-dev.yml`) and provide a Claude-like chat experience.

### Goals
1. Administration UI for configuring inference engines, MCP servers, tools, and agents
2. Chat interface modeled after Claude's UI layout
3. Comprehensive unit testing for all UI components and services

### Technical Stack
- **Frontend:** Blazor WebAssembly + MudBlazor 8.x
- **Desktop:** Tauri (from Release 1)
- **Testing:** bUnit (components) + xUnit/Moq (services)
- **API:** AESIR Server via Traefik at `https://aesir.localhost`

### User Context
- Hardcoded user ID: `blangford@gmail.com`

---

## UI Design Reference

Based on Claude's chat interface, the AESIR client will follow this layout:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ [AESIR Logo]                                              [Settings Gear]   │
├────────────────────┬────────────────────────────────────────────────────────┤
│                    │                                                        │
│  [+ New Chat]      │         ┌─────────────────────────┐                   │
│                    │         │  [Agent Selector Chip]   │                   │
│  ─────────────     │         └─────────────────────────┘                   │
│  CHATS             │                                                        │
│  ─────────────     │              Good morning, User                        │
│                    │                                                        │
│  Recent Chat 1     │    ┌─────────────────────────────────────────────┐    │
│  Recent Chat 2     │    │  How can I help you today?                   │    │
│  Recent Chat 3     │    │                                              │    │
│  Recent Chat 4     │    │  [+] [⚙] [🕐]              [Model] [Send→]  │    │
│  ...               │    └─────────────────────────────────────────────┘    │
│                    │                                                        │
│  ─────────────     │                                                        │
│  SETTINGS          │                                                        │
│  ─────────────     │                                                        │
│  Inference Engines │                                                        │
│  MCP Servers       │                                                        │
│  Tools             │                                                        │
│  Agents            │                                                        │
│                    │                                                        │
├────────────────────┴────────────────────────────────────────────────────────┤
│  [User: blangford@gmail.com]                                                │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## API Endpoints

### Configuration API (`/configuration`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/configuration/systemready` | Check if system is configured |
| GET | `/configuration/inferenceengines` | List inference engines |
| GET | `/configuration/inferenceengines/{id}` | Get inference engine |
| POST | `/configuration/inferenceengines` | Create inference engine |
| PUT | `/configuration/inferenceengines/{id}` | Update inference engine |
| DELETE | `/configuration/inferenceengines/{id}` | Delete inference engine |
| GET | `/configuration/mcpservers` | List MCP servers |
| GET | `/configuration/mcpservers/{id}` | Get MCP server |
| POST | `/configuration/mcpservers` | Create MCP server |
| PUT | `/configuration/mcpservers/{id}` | Update MCP server |
| DELETE | `/configuration/mcpservers/{id}` | Delete MCP server |
| GET | `/configuration/mcpservers/{id}/tools` | Get tools from MCP server |
| GET | `/configuration/tools` | List tools |
| GET | `/configuration/tools/{id}` | Get tool |
| POST | `/configuration/tools` | Create tool |
| PUT | `/configuration/tools/{id}` | Update tool |
| DELETE | `/configuration/tools/{id}` | Delete tool |
| GET | `/configuration/agents` | List agents |
| GET | `/configuration/agents/{id}` | Get agent |
| POST | `/configuration/agents` | Create agent |
| PUT | `/configuration/agents/{id}` | Update agent |
| DELETE | `/configuration/agents/{id}` | Delete agent |
| GET | `/configuration/agents/{id}/tools` | Get tools for agent |
| PUT | `/configuration/agents/{id}/tools` | Update tools for agent |

### Chat API (`/chat`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/chat/completions/agent/streamed` | Streaming chat with agent |
| GET | `/chat/history/user/{userId}` | Get user's chat sessions |
| GET | `/chat/history/{id}` | Get chat session |
| PUT | `/chat/history/{id}/{title}` | Update session title |
| DELETE | `/chat/history/{id}` | Delete chat session |
| GET | `/chat/history/user/{userId}/search/{term}` | Search sessions |

---

## Sprint Breakdown

### Sprint 1: Test Infrastructure & API Client Enhancement ✅ COMPLETE
**Goal:** Set up testing framework and enhance API client for admin endpoints
**Completed:** 2025-12-01

#### Tasks
- [x] **1.1** Add bUnit and xUnit test projects
  - Created `Aesir.Client.Web.Tests` project
  - Added bUnit 1.36.0, xUnit 2.9.2, Moq 4.20.72, FluentAssertions 8.0.1, RichardSzalay.MockHttp 7.0.0
  - Added project to solution
- [x] **1.2** Enhance IApiClient interface
  - Added `ApiResult<T>` and `ApiResult` types for Result pattern
  - Error handling with HttpStatusCode tracking
  - Map, OnSuccess, OnFailure fluent methods
- [x] **1.3** Create API client unit tests
  - ApiClientTests (8 tests) - GET/POST/PUT/DELETE, streaming
  - ApiResultTests (15 tests) - Success/Failure/Map/callbacks
- [x] **1.4** Create typed API service interfaces
  - `IConfigurationApiService` with `ConfigurationApiService` implementation
  - `IChatApiService` with `ChatApiService` implementation
  - ConfigurationApiServiceTests (14 tests)
  - ChatApiServiceTests (12 tests)

**Test Results:** 49 tests passed, 0 failed

**Deliverables:**
- Test project structure
- Enhanced API client with typed services
- API client unit tests (minimum 80% coverage)

---

### Sprint 2: Settings Module - Inference Engines ✅ COMPLETE
**Goal:** Create Settings module with Inference Engine management
**Completed:** 2025-12-01

#### Tasks
- [x] **2.1** Create Settings module structure
  - `Aesir.Client.Web.Modules.Settings` project
  - Module registration with navigation (SettingsModule.cs)
  - Settings index page with navigation cards
- [x] **2.2** Inference Engines list page
  - MudDataGrid with Name, Type, Description columns
  - Add/Edit/Delete buttons with icon buttons
  - Loading indicator and empty states
- [x] **2.3** Inference Engine edit dialog
  - Form with Name, Description, Type dropdown
  - Dynamic configuration fields (BaseUrl for Ollama, BaseUrl + ApiKey for OpenAI)
  - Required field validation
- [x] **2.4** Inference Engines service
  - `IInferenceEngineService` interface
  - `InferenceEngineService` implementation using `IConfigurationApiService`
- [x] **2.5** Unit tests
  - InferenceEnginesPageTests (12 tests) - bUnit component tests
  - InferenceEngineServiceTests (11 tests) - service tests with mocked API

**Test Results:** 72 tests passed (49 from Sprint 1 + 23 new), 0 failed

**Deliverables:**
- Settings module with Inference Engines CRUD
- Full test coverage for IE components/services

---

### Sprint 3: Settings Module - MCP Servers & Tools ✅ COMPLETE
**Goal:** Add MCP Server and Tool management to Settings
**Completed:** 2025-12-01

#### Tasks
- [x] **3.1** MCP Servers list page
  - Data grid with Name, Location, Connection columns
  - Add/Edit/Delete buttons
  - "Discover Tools" action button with info color
- [x] **3.2** MCP Server edit dialog
  - Location type selector (Local/Remote)
  - Local: Command, Arguments (list editor), Environment Variables (key-value editor)
  - Remote: URL, HTTP Headers (key-value editor)
- [x] **3.3** Tools list page
  - Data grid with Name, Type, Description, Source columns
  - Filter chips by type (All/Internal/MCP Server)
  - Link to MCP Server for MCP tools, "Built-in" for Internal
- [x] **3.4** Tool edit dialog (Internal tools only)
  - Form with Name, Description, Tool Name, Icon selector
  - MCP Server tools are read-only (discovered)
- [x] **3.5** MCP/Tools services
  - `IMcpServerService` interface and implementation
  - `IToolService` interface and implementation
  - Services registered in SettingsModule
- [x] **3.6** Unit tests
  - McpServersPageTests (14 tests) - bUnit component tests
  - ToolsPageTests (14 tests) - bUnit component tests
  - McpServerServiceTests (13 tests) - service tests with mocked API
  - ToolServiceTests (11 tests) - service tests with mocked API

**Test Results:** 124 tests passed (72 from Sprint 2 + 52 new), 0 failed

**Deliverables:**
- MCP Servers CRUD with tool discovery
- Tools CRUD (Internal) and viewing (MCP)
- Full test coverage

---

### Sprint 4: Settings Module - Agents ✅ COMPLETE
**Goal:** Add Agent management with tool assignment
**Completed:** 2025-12-01

#### Tasks
- [x] **4.1** Agents list page
  - Data grid with Name, Model, Inference Engine, Description, Tools count, Persona columns
  - Add/Edit/Delete buttons
  - Quick view of assigned tools count with chip
  - Persona type chips (Business, Military, OCR, Custom)
- [x] **4.2** Agent edit dialog - Basic Info tab
  - Name, Description
  - Inference Engine dropdown (from configured engines)
  - Model name input with helper text
- [x] **4.3** Agent edit dialog - Model Parameters tab
  - Temperature slider (0.0 - 1.0) with live value display
  - Top-P slider (0.0 - 1.0) with live value display
  - Max Tokens numeric input (1 - 128000)
- [x] **4.4** Agent edit dialog - Persona tab
  - Persona type selector (Default, Business, Military, OCR, Custom)
  - Custom prompt textarea (conditionally shown when Custom selected)
  - Thinking options (Allow Thinking switch, Think Level selector: Enabled, High, Medium, Low)
- [x] **4.5** Agent edit dialog - Tools tab
  - Multi-select list of available tools with checkboxes
  - Filter/search tools input
  - Tool type chips (Internal, MCP)
  - Selected tools count display
- [x] **4.6** Agent service
  - `IAgentService` interface with CRUD + tool assignment operations
  - `AgentService` implementation using `IConfigurationApiService`
  - Service registered in SettingsModule
- [x] **4.7** Unit tests
  - AgentsPageTests (14 tests) - bUnit component tests
  - AgentServiceTests (17 tests) - service tests with mocked API

**Test Results:** 155 tests passed (124 from Sprint 3 + 31 new), 0 failed

**Deliverables:**
- Agents CRUD with full configuration (4 tabs)
- Tool assignment UI with search/filter
- Full test coverage

---

### Sprint 5: Chat Module - Core UI ✅ COMPLETE
**Goal:** Implement Claude-like chat interface layout
**Completed:** 2025-12-01

#### Tasks
- [x] **5.1** Update MainLayout for chat-centric design
  - Created ChatLayout.razor with collapsible left sidebar
  - Header with AESIR logo, theme toggle, and settings gear
  - User display at bottom (blangford@gmail.com)
- [x] **5.2** Chat sidebar component
  - "New Chat" button in drawer header
  - Chat history list (placeholder for recent sessions)
  - Settings navigation links dynamically loaded from registry
- [x] **5.3** Agent selector component
  - AgentSelector.razor with MudMenu chip/dropdown
  - Agent list with model info
  - Auto-selects first available agent
- [x] **5.4** Chat welcome view
  - ChatWelcome.razor with time-based greeting
  - Agent selector prominently displayed
  - Suggestion cards for quick prompts
- [x] **5.5** Message input component
  - MessageInput.razor with multi-line text input
  - Send button with keyboard shortcut (Enter)
  - Model display from selected agent
  - Placeholder buttons for attachments and settings
- [x] **5.6** Unit tests
  - ChatStateServiceTests (15 tests)
  - AgentSelectorTests (10 tests)
  - MessageInputTests (11 tests)
  - ChatWelcomeTests (9 tests)
  - ChatPageTests (9 tests)

**Test Results:** 209 tests passed (155 from Sprint 4 + 54 new), 0 failed

**Deliverables:**
- Claude-like layout structure
- Agent selection working
- Input component ready
- Full test coverage

---

### Sprint 6: Chat Module - Messaging ✅ COMPLETE
**Goal:** Implement streaming chat with message display
**Completed:** 2025-12-01

#### Tasks
- [x] **6.1** Chat message components
  - UserMessage.razor - User message bubble (right-aligned) with file attachment support
  - AssistantMessage.razor - Assistant message bubble (left-aligned) with thinking section
  - Markdown rendering via MarkdownService using Markdig
  - ThinkingIndicator.razor - Animated thinking state with expandable content
- [x] **6.2** Chat conversation view
  - Updated ChatPage.razor with scrollable message list
  - Auto-scroll via JavaScript interop (chat-interop.js)
  - Loading states with streaming cursor animation
- [x] **6.3** Streaming message handling
  - Connected to `/chat/completions/agent/streamed` via IChatApiService
  - Real-time token display during streaming
  - Handles thinking vs content states from stream chunks
- [x] **6.4** Chat state service
  - Enhanced existing ChatStateService with session tracking
  - Message list management in ChatPage
  - Session ID tracking from stream response
- [x] **6.5** Error handling
  - Connection error display with MudAlert
  - API error handling with user-friendly messages
  - Cancellation token support for stream interruption
- [x] **6.6** Unit tests
  - UserMessageTests (9 tests) - message rendering, file attachments
  - AssistantMessageTests (14 tests) - markdown, thinking, timestamps
  - ThinkingIndicatorTests (11 tests) - indicator states, content toggle
  - MarkdownServiceTests (19 tests) - all markdown features
  - Updated ChatPageTests (11 tests) - streaming integration

**Test Results:** 262 tests passed (209 from Sprint 5 + 53 new), 0 failed

**Deliverables:**
- Working streaming chat
- Message display with markdown
- Error handling
- Full test coverage

---

### Sprint 7: Chat Module - History & Session Management ✅ COMPLETE
**Goal:** Implement chat history and session management
**Completed:** 2025-12-01

#### Tasks
- [x] **7.1** Chat history service
  - IChatHistoryService interface with caching and events
  - ChatHistoryService implementation wrapping IChatApiService
  - Load user's chat sessions with ordering by UpdatedAt
  - Delete sessions with local cache removal
  - Search sessions with filter state
- [x] **7.2** Chat history sidebar updates
  - ChatHistoryItem component for session display
  - Real-time list with OnSessionsChanged events
  - Click to load via SelectSession and navigation
  - Delete with ConfirmDialog confirmation
  - Search input with debounce (300ms)
- [x] **7.3** Session persistence
  - Load existing session messages via query parameter
  - Session ID tracking from stream response
  - URL update on new session creation
- [x] **7.4** New chat flow
  - Clear current conversation via StartNewChat
  - Create new session on first message (server-side)
  - Update sidebar via NotifySessionCreatedAsync
- [x] **7.5** Unit tests
  - ChatHistoryServiceTests (18 tests) - service tests
  - ChatHistoryItemTests (12 tests) - component tests
  - Updated ChatPageTests with IChatHistoryService mock

**Test Results:** 292 tests passed (262 from Sprint 6 + 30 new), 0 failed

**Deliverables:**
- Full chat history functionality
- Session create/load/delete
- Search capability
- Full test coverage

---

### Sprint 8: Integration Testing & Polish ✅ COMPLETE
**Goal:** End-to-end testing, bug fixes, and UI polish

#### Tasks
- [x] **8.1** Integration test setup
  - Test containers for API (using MockHttp for client-side)
  - Integration test project (IntegrationTestBase with realistic API mocks)
- [x] **8.2** Integration tests
  - Configuration flow (create engine → create agent → chat) - ConfigurationFlowTests
  - Chat flow (send message → receive stream → save history) - ChatFlowTests
  - Error scenarios - ErrorScenarioTests
- [x] **8.3** UI polish
  - Loading skeletons (ChatLayout sidebar)
  - Skeleton animations for history loading
  - Settings pages use MudDataGrid with built-in loading
- [x] **8.4** Performance optimization
  - Virtualized lists for history (Blazor Virtualize component)
  - ItemSize and OverscanCount configured for smooth scrolling
- [x] **8.5** Documentation
  - Updated CLIENT_ARCHITECTURE.md
  - Added Testing Architecture section
  - Added API Result Pattern documentation
  - Added Performance Optimizations section

**Deliverables:**
- 333 tests passing (unit + integration)
- Polished UI with loading skeletons
- Updated CLIENT_ARCHITECTURE.md documentation

---

## Data Models (from Aesir.Common)

### AesirInferenceEngineBase
```csharp
- Id: Guid?
- Name: string?
- Description: string?
- Type: InferenceEngineType (Ollama | OpenAICompatible)
- Configuration: IDictionary<string, string?>?
```

### AesirMcpServerBase
```csharp
- Id: Guid?
- Name: string?
- Description: string?
- Location: ServerLocation (Local | Remote)
- Command: string? (Local)
- Arguments: IList<string> (Local)
- EnvironmentVariables: IDictionary<string, string?> (Local)
- Url: string? (Remote)
- HttpHeaders: IDictionary<string, string?> (Remote)
```

### AesirToolBase
```csharp
- Id: Guid?
- Name: string?
- Type: ToolType (Internal | McpServer)
- Description: string?
- McpServerId: Guid? (if MCP)
- ToolName: string?
- IconName: string?
```

### AesirAgentBase
```csharp
- Id: Guid?
- Name: string?
- Description: string?
- ChatInferenceEngineId: Guid?
- ChatModel: string?
- ChatTemperature: double?
- ChatTopP: double?
- ChatMaxTokens: int?
- ChatPromptPersona: PromptPersona?
- ChatCustomPromptContent: string?
- AllowThinking: bool?
- ThinkValue: ThinkValue?
```

---

## Test Coverage Requirements

| Layer | Target Coverage | Framework |
|-------|-----------------|-----------|
| UI Components | 80% | bUnit |
| Services | 90% | xUnit + Moq |
| API Client | 85% | xUnit + Moq |
| Integration | Key flows | xUnit + TestContainers |

### Testing Patterns

**Component Tests (bUnit):**
```csharp
[Fact]
public void AgentSelector_DisplaysAgents_WhenLoaded()
{
    // Arrange
    var agents = new List<AesirAgent> { /* test data */ };
    Services.AddSingleton(Mock.Of<IAgentService>(s =>
        s.GetAgentsAsync() == Task.FromResult(agents)));

    // Act
    var cut = RenderComponent<AgentSelector>();

    // Assert
    cut.FindAll(".agent-item").Count.Should().Be(agents.Count);
}
```

**Service Tests (xUnit + Moq):**
```csharp
[Fact]
public async Task GetAgentsAsync_ReturnsAgents_FromApi()
{
    // Arrange
    var mockApi = new Mock<IConfigurationApiService>();
    mockApi.Setup(x => x.GetAgentsAsync(default))
        .ReturnsAsync(new List<AesirAgent> { /* test data */ });
    var service = new AgentService(mockApi.Object);

    // Act
    var result = await service.GetAgentsAsync();

    // Assert
    result.Should().NotBeEmpty();
}
```

---

## Development Environment Setup

### Prerequisites
1. Docker Desktop running
2. .NET 10.0 SDK
3. Rust + Cargo (for Tauri)

### Start Backend
```bash
cd /Users/ooartist/Src/Aesir
docker-compose -f docker-compose-api-dev.yml up -d
```

API available at: `https://aesir.localhost`

### Start Frontend (Development)
```bash
cd Client/Aesir.Client.Web/Aesir.Client.Web.App
dotnet watch run --urls "http://localhost:5173"
```

### Run Tests
```bash
cd Client/Aesir.Client.Web
dotnet test
```

---

## Definition of Done

### Per Feature
- [ ] Implementation complete
- [ ] Unit tests written and passing
- [ ] Code reviewed (self-review checklist)
- [ ] No compiler warnings
- [ ] Works in browser and Tauri desktop

### Per Sprint
- [ ] All features complete
- [ ] Test coverage meets targets
- [ ] Integration with API verified
- [ ] Documentation updated

### Release Complete
- [ ] All sprints complete
- [ ] Full integration test suite passing
- [ ] Performance acceptable
- [ ] Documentation complete
- [ ] Ready for production deployment

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| API changes during development | Use typed interfaces, mock early |
| Streaming complexity | Implement basic version first, enhance |
| Test flakiness | Use deterministic test data, avoid timing dependencies |
| MudBlazor limitations | Have fallback custom components |

---

## Notes

- This builds on Release 1 which established the project structure and module system
- The Settings module must be complete before Chat can be fully functional
- Testing is integrated into each sprint, not a separate phase
- UI follows Claude's chat interface as the design reference
