# AESIR Web Client - Release 3 Work Plan
## Setup Wizard & Administration Refinements

**Created:** 2025-12-02
**Status:** In Progress (Sprint 4 Complete, Sprint 5 Pending)
**Branch:** `aesir-client-x`
**Predecessor:** Release 2 (Chat Interface with Administration)

---

## Overview

This release focuses on first-time user experience and feature parity with the legacy Avalonia desktop client. The key deliverables are:

1. **Setup Wizard** - Detect unconfigured AESIR installations and guide users through required setup
2. **Model Service** - Dynamic model loading from inference engines (replacing text inputs with dropdowns)
3. **General Settings Page** - Complete RAG/Speech/Search configuration UI
4. **Agent Editor Enhancements** - Model dropdowns and improved UX
5. **Configuration Hot-Reload** - Server service refresh after creating/updating inference engines or general settings

### Goals
1. Seamless first-time setup experience
2. Feature parity with legacy Avalonia client
3. Intuitive administration with dynamic data loading
4. Collaborative step-by-step development

### Technical Stack
- **Frontend:** Blazor WebAssembly + MudBlazor 8.x
- **Desktop:** Tauri
- **API:** AESIR Server (existing endpoints)
- **Testing:** bUnit + xUnit + Moq

---

## Feature Parity Checklist

| Feature | Legacy Avalonia | Web Client (R2) | Web Client (R3) |
|---------|-----------------|-----------------|-----------------|
| Inference Engine CRUD | Yes | Yes | Yes |
| MCP Server CRUD | Yes | Yes | Yes |
| Tool Management | Yes | Yes | Yes |
| Agent CRUD | Yes | Yes | Yes |
| **Agent Model Dropdown** | Yes (dynamic) | No (text input) | **Sprint 2** |
| **General Settings** | Yes | No | **Sprint 3** |
| **RAG Embedding Config** | Yes | No | **Sprint 3** |
| **RAG Vision Config** | Yes | No | **Sprint 3** |
| **Setup Wizard** | No | No | **Sprint 1** |
| **Configuration Hot-Reload** | No | No | **Sprint 1 & 3** |
| Chat Streaming | Yes | Yes | Yes |
| Chat History | Yes | Yes | Yes |
| Thinking Display | Yes | Yes | Yes |

---

## API Endpoints (Existing)

### Configuration Readiness
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/configuration/systemready` | Check if system is configured |
| POST | `/configuration/reload` | Reload all configuration-dependent services (new) |

**Response:** `AesirConfigurationReadinessBase`
```json
{
  "isReady": false,
  "reasons": [
    "No inference engines configured",
    "RAG embedding model not set"
  ]
}
```

### General Settings
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/configuration/generalsettings` | Get general settings |
| PUT | `/configuration/generalsettings` | Update general settings |

**Model:** `AesirGeneralSettingsBase`
- `ragEmbeddingInferenceEngineId: Guid?`
- `ragEmbeddingModel: string?`
- `ragVisionInferenceEngineId: Guid?`
- `ragVisionModel: string?`
- `ttsModelPath: string?`
- `sttModelPath: string?`
- `vadModelPath: string?`
- `googleSearchEngineId: string?`
- `googleApiKey: string?`

### Models API
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/models/{inferenceEngineId}/{category}` | Get models by category |

**Categories:** `Embedding`, `Chat`, `Vision`

**Response:** `IEnumerable<AesirModelInfo>`

---

## Sprint Breakdown

### Sprint 1: Configuration Readiness & Wizard Foundation
**Goal:** Detect unconfigured system and redirect to setup wizard

#### Tasks
- [ ] **1.1** Add Configuration Readiness Service
  - Create `IConfigurationReadinessService` in Infrastructure
  - Create `ConfigurationReadinessService` implementation
  - Add `GetSystemReadyAsync()` method
  - Add `AesirConfigurationReadiness` model to client
  - Unit tests for service

- [ ] **1.2** Add Model Service
  - Create `IModelService` interface in Infrastructure
  - Create `ModelService` implementation
  - Add `GetModelsAsync(Guid inferenceEngineId, ModelCategory category)` method
  - Returns `IReadOnlyList<AesirModelInfo>`
  - Unit tests for service

- [ ] **1.3** Create Setup Wizard Module
  - New project: `Aesir.Client.Web.Modules.Wizard`
  - `WizardModule.cs` with module registration
  - `SetupWizardPage.razor` - main wizard container
  - `WizardStepIndicator.razor` - step progress component

- [ ] **1.4** Implement Wizard State Management
  - `IWizardStateService` - tracks wizard progress
  - Step definitions: Welcome, InferenceEngine, GeneralSettings, Agent, Complete
  - Navigation between steps with validation
  - Skip to specific step capability

- [ ] **1.5** App Startup Configuration Check
  - Modify `App.razor` or `MainLayout` to check readiness on load
  - Redirect to `/setup` if not ready
  - Store "wizard completed" flag in local storage
  - Allow manual access to wizard from settings

- [ ] **1.6** Configuration Hot-Reload (Server-Side)
  - **Problem:** Server registers keyed services (`IModelsService`, `IChatService`, `IEmbeddingGenerator`) at startup based on:
    - Inference engines (for models, chat, embeddings)
    - General settings (for RAG embedding/vision model selection)
  - **Solution:** Add unified endpoint to refresh all configuration-dependent services
  - Create `POST /configuration/reload` endpoint
  - Implement `IConfigurationReloadService` to handle dynamic service registration
  - Re-register keyed services for inference engines
  - Re-register embedding/vision services based on general settings
  - Update `ConfigurationController` with reload endpoint
  - Server-side unit tests

- [ ] **1.7** Configuration Hot-Reload (Client-Side)
  - Update `InferenceEngineService` to call reload endpoint after create/update/delete
  - Add loading indicator during reload
  - Show success/failure notification
  - Client-side unit tests

- [ ] **1.8** Unit Tests
  - ConfigurationReadinessServiceTests (8 tests)
  - ModelServiceTests (10 tests)
  - WizardStateServiceTests (12 tests)
  - ConfigurationReloadTests (6 tests)

**Deliverables:**
- Configuration readiness detection
- Model service for dynamic loading
- Wizard infrastructure ready
- Configuration hot-reload (no server restart required)

---

### Sprint 2: Agent Editor Enhancement - Model Dropdown
**Goal:** Replace model text input with dynamic dropdown

#### Tasks
- [ ] **2.1** Create ModelSelector Component
  - `ModelSelector.razor` component
  - Props: `InferenceEngineId`, `ModelCategory`, `SelectedModel`, `OnModelSelected`
  - Loads models when inference engine changes
  - Displays loading state during fetch
  - Shows model details (name, capabilities)

- [ ] **2.2** Update AgentEditDialog
  - Replace `MudTextField` for model with `ModelSelector`
  - Wire up inference engine change to reload models
  - Preserve selected model across category changes
  - Add "Model Details" button with modal (capabilities, parameters)

- [ ] **2.3** Add Thinking Support Detection
  - Query model capabilities from `AesirModelInfo.Details`
  - Enable/disable thinking options based on capabilities
  - Show thinking level options for supported models

- [ ] **2.4** Cascade Model Loading Pattern
  ```
  InferenceEngine Change
    └─> Load Available Models (Chat category)
        └─> Auto-select first model OR preserve selection
            └─> Update Thinking Options visibility
  ```

- [ ] **2.5** Unit Tests
  - ModelSelectorTests (14 tests)
  - Updated AgentEditDialogTests (8 new tests)

**Deliverables:**
- Dynamic model dropdown in Agent editor
- Thinking support auto-detection
- Model details display

---

### Sprint 3: General Settings Page
**Goal:** Implement complete General Settings UI with RAG configuration

#### Tasks
- [ ] **3.1** Create General Settings Service
  - `IGeneralSettingsService` interface
  - `GeneralSettingsService` implementation
  - `GetSettingsAsync()`, `UpdateSettingsAsync()` methods
  - Unit tests

- [ ] **3.2** General Settings Page Layout
  - `GeneralSettingsPage.razor` in Settings module
  - Add navigation item to Settings module
  - Card-based sections for different setting groups

- [ ] **3.3** RAG Embedding Configuration Section
  - Inference Engine dropdown (from configured engines)
  - Model dropdown (loads Embedding models when engine selected)
  - Uses `ModelSelector` component with `ModelCategory.Embedding`

- [ ] **3.4** RAG Vision Configuration Section
  - Inference Engine dropdown
  - Model dropdown (loads Vision models)
  - Uses `ModelSelector` component with `ModelCategory.Vision`

- [ ] **3.5** Speech Configuration Section (Read-only)
  - TTS Model Path display (disabled text field)
  - STT Model Path display (disabled text field)
  - VAD Model Path display (disabled text field)
  - Info text explaining these are server-configured

- [ ] **3.6** Google Search Configuration Section
  - Google Search Engine ID input
  - Google API Key input (password field with visibility toggle)
  - Test connection button (optional)

- [ ] **3.7** Save/Cancel Actions
  - Form validation
  - Save with success/error notification
  - **Call configuration reload endpoint after save** (re-registers embedding/vision services)
  - Cancel with unsaved changes warning

- [ ] **3.8** Unit Tests
  - GeneralSettingsServiceTests (12 tests)
  - GeneralSettingsPageTests (16 tests)

**Deliverables:**
- Complete General Settings page
- RAG configuration with dynamic model loading
- Google Search configuration
- Configuration hot-reload on settings change

---

### Sprint 4: Setup Wizard - Steps Implementation ✅
**Goal:** Implement all wizard step pages
**Completed:** 2025-12-02

#### Tasks
- [x] **4.1** Welcome Step
  - `WizardWelcomeStep.razor`
  - Greeting message and setup overview
  - "Get Started" button
  - Shows what will be configured

- [x] **4.2** Inference Engine Step
  - `WizardInferenceEngineStep.razor`
  - Embeds simplified inference engine form
  - At least one engine required to proceed
  - "Add Another" option
  - Quick presets: Ollama Local, OpenAI, Custom

- [x] **4.3** General Settings Step
  - `WizardGeneralSettingsStep.razor`
  - RAG Embedding configuration (required for chat)
  - RAG Vision configuration (optional)
  - Uses ModelSelector component from Settings module

- [x] **4.4** Agent Step
  - `WizardAgentStep.razor`
  - Create first agent with model selection
  - Uses ModelSelector from Settings module
  - Basic configuration: name, description, engine, model
  - Advanced options expandable (persona, temperature, max tokens)

- [x] **4.5** Completion Step
  - `WizardCompleteStep.razor`
  - Summary of configured items (engines, settings, agents)
  - "Start Chatting" button with OnFinish callback
  - Links to settings for future changes

- [x] **4.6** Wizard Navigation
  - Back/Next buttons with callbacks
  - Validation before proceeding (requires engines/agents)
  - Progress indicator via MudStepper
  - State management via WizardStateService

- [x] **4.7** Unit Tests (74 tests)
  - WizardWelcomeStepTests (6 tests)
  - WizardInferenceEngineStepTests (10 tests)
  - WizardGeneralSettingsStepTests (11 tests)
  - WizardAgentStepTests (14 tests)
  - WizardCompleteStepTests (12 tests)
  - Plus existing wizard state/service tests (21 tests)

**Note:** Component rendering tests refactored to test business logic and service interactions due to MudBlazor 8.x popover service complexity in bUnit.

**Deliverables:**
- Complete wizard flow
- All step pages implemented
- Full test coverage (74 tests)

---

### Sprint 5: Integration Testing & Polish
**Goal:** End-to-end wizard testing and UI polish

#### Tasks
- [ ] **5.1** Wizard Integration Tests
  - Full wizard flow test (all steps)
  - Skip optional steps test
  - Back navigation test
  - Validation failure handling

- [ ] **5.2** Settings Integration Tests
  - General Settings save/load test
  - Model dropdown cascade test
  - Agent with model selection test

- [ ] **5.3** UI Polish
  - Loading states in wizard
  - Transition animations between steps
  - Mobile-responsive wizard layout
  - Error state styling

- [ ] **5.4** Edge Case Handling
  - No inference engines configured - helpful message
  - Inference engine offline - graceful degradation
  - Model loading failure - retry option
  - API errors - user-friendly messages

- [ ] **5.5** Documentation
  - Update CLIENT_ARCHITECTURE.md
  - Add Wizard module documentation
  - Update Settings module documentation

- [ ] **5.6** E2E Testing
  - Browser: Full wizard completion
  - Tauri: Desktop wizard flow
  - Settings modification after wizard

**Deliverables:**
- Integration test suite
- Polished UI
- Updated documentation
- E2E validation

---

## Data Models

### AesirConfigurationReadinessBase (Existing)
```csharp
public class AesirConfigurationReadinessBase
{
    public bool IsReady { get; set; }
    public IList<string> Reasons { get; set; }
}
```

### AesirModelInfo (Existing)
```csharp
public class AesirModelInfo
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public ModelDetails? Details { get; set; }
}

public class ModelDetails
{
    public IList<string>? Capabilities { get; set; }
    public long? ParameterSize { get; set; }
    // ... other properties
}
```

### ModelCategory (Existing)
```csharp
public enum ModelCategory
{
    Embedding,
    Chat,
    Vision
}
```

---

## Component Architecture

### ModelSelector Component
```
┌─────────────────────────────────────────┐
│ ModelSelector                           │
│ ┌─────────────────────────────────────┐ │
│ │ [Select Model        ▼]             │ │
│ │                                     │ │
│ │ ┌─────────────────────────────────┐ │ │
│ │ │ 🔘 llama3.1:8b                  │ │ │
│ │ │    8B parameters, Chat          │ │ │
│ │ ├─────────────────────────────────┤ │ │
│ │ │ ○ qwen2.5:14b                   │ │ │
│ │ │    14B parameters, Chat, Think  │ │ │
│ │ └─────────────────────────────────┘ │ │
│ └─────────────────────────────────────┘ │
│ [ℹ️ Model Details]                      │
└─────────────────────────────────────────┘
```

### Wizard Flow
```
┌──────────┐   ┌─────────────────┐   ┌────────────────┐   ┌─────────┐   ┌──────────┐
│ Welcome  │──>│InferenceEngine  │──>│GeneralSettings │──>│  Agent  │──>│ Complete │
│          │   │  (required)     │   │  (required)    │   │(required│   │          │
└──────────┘   └─────────────────┘   └────────────────┘   └─────────┘   └──────────┘
     │                                                                        │
     └────────────────────── Can restart wizard ──────────────────────────────┘
```

---

## Test Coverage Requirements

| Layer | Target Coverage | Framework |
|-------|-----------------|-----------|
| UI Components | 80% | bUnit |
| Services | 90% | xUnit + Moq |
| Integration | Key flows | xUnit |

### Estimated Test Counts by Sprint
- Sprint 1: ~36 tests (readiness, model service, wizard state, inference engine reload)
- Sprint 2: ~22 tests (model selector, agent dialog)
- Sprint 3: ~28 tests (general settings)
- Sprint 4: ~42 tests (wizard steps)
- Sprint 5: ~20 tests (integration)

**Total Estimated: ~148 new tests**

---

## Development Approach

### Collaborative Step-by-Step
Each sprint will be developed collaboratively:
1. Review existing code together
2. Plan implementation approach
3. Implement in small increments
4. Test and verify before moving on
5. Get user approval at checkpoints

### Checkpoints
- [ ] Sprint 1 Complete: Wizard foundation ready
- [ ] Sprint 2 Complete: Model dropdown working
- [ ] Sprint 3 Complete: General Settings page done
- [ ] Sprint 4 Complete: Wizard fully functional
- [ ] Sprint 5 Complete: Release 3 ready

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Model API unavailable | Graceful fallback to text input |
| Inference engine offline | Show cached models, allow manual entry |
| Wizard state lost | Persist to localStorage |
| Complex form validation | Use MudBlazor validation features |
| Hot-reload fails | Show notification with manual restart instructions |
| Keyed service conflicts | Clear and re-register all services atomically |

---

## Definition of Done

### Per Feature
- [ ] Implementation complete
- [ ] Unit tests written and passing
- [ ] Works in browser and Tauri desktop
- [ ] No compiler warnings

### Per Sprint
- [ ] All features complete
- [ ] Test coverage meets targets
- [ ] Integration with API verified

### Release Complete
- [ ] All sprints complete
- [ ] Full wizard flow working
- [ ] General Settings functional
- [ ] Model dropdowns working
- [ ] Configuration hot-reload working (inference engines + general settings)
- [ ] Documentation updated

---

## Notes

- This release builds on Release 2's foundation
- The wizard is essential for first-time user experience
- Model dropdowns significantly improve UX over text input
- General Settings enables RAG functionality required for chat
- **Configuration Hot-Reload:** Server registers keyed services (`IModelsService`, `IChatService`, `IEmbeddingGenerator`, etc.) at startup based on inference engines and general settings. Without hot-reload, users would need to restart the server after:
  - Adding/updating/deleting inference engines
  - Changing RAG embedding model in general settings
  - Changing RAG vision model in general settings

  This release implements dynamic service registration so configuration changes are immediately effective.
