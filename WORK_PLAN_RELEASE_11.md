# WORK_PLAN_RELEASE_11.md

> **STATUS: IMPLEMENTATION COMPLETE - READY FOR TESTING**
>
> Tool Call Surfacing - Real-time AI Tool Usage Display
>
> **Scope:** Server (Aesir.Common, Aesir.Modules.Inference) + Client (Blazor WebAssembly)
> **Priority:** High - Improves transparency and user understanding of AI behavior

## Overview

### Goal
Surface Semantic Kernel tool calls (Document Search, Web Search, MCP tools) to the AESIR chat UI in real-time during streaming, providing users with full visibility into what tools the AI is using and their results.

### User Requirements
1. **Full Details**: Show tool name, input parameters, output/result, execution time
2. **Real-time**: Show tool calls as they happen during streaming, before final answer
3. **Collapsible Section**: Similar to existing Thinking section - expandable panel
4. **Color coded**: Different colors/icons per tool type (Web Search, Document Search, MCP tools)

### Technical Approach
- Use `IAutoFunctionInvocationFilter` (already pattern exists in `KernelLoggingFilterService`) to intercept tool calls
- Extend streaming protocol with new event types for tool calls
- Create channel-based broadcaster for thread-safe communication between filter and streaming
- Build UI components following existing ThinkingSection pattern in AssistantMessage.razor

## Legend

- [ ] Not started
- [x] Completed
- [~] Skipped (with reason in comments)

---

## Epic 1: Common Models & Contracts

> **PRIORITY: HIGH** - Foundation for server-client communication

### 1.1 Create Tool Call Models

**Goal:** Define shared models for tool call information.

**New File:** `Common/Aesir.Common/Models/AesirToolCallInfo.cs`

**Work Items:**
- [x] 1.1.1 Create `AesirToolCallInfo` class with properties:
  - ToolCallId, FunctionName, PluginName, Description
  - Arguments (Dictionary<string, string>)
  - Status (Started/Completed/Failed)
  - StartedAt, CompletedAt, DurationMs
  - Result (truncated string), Error
- [x] 1.1.2 Create `ToolCallType` enum (DocumentSearch, WebSearch, McpServer, ImageAnalysis, Summarization, Other)
- [x] 1.1.3 Create `ToolCallStatus` enum (Started, Completed, Failed)

---

### 1.2 Extend Streaming Protocol

**Goal:** Add event type discrimination to streaming results.

**Modify:** `Common/Aesir.Common/Models/AesirChatStreamedResultBase.cs`

**Work Items:**
- [x] 1.2.1 Add `StreamEventType` enum (Text, Thinking, ToolCallStart, ToolCallResult)
- [x] 1.2.2 Add `EventType` property to `AesirChatStreamedResultBase` (default: Text for backward compat)
- [x] 1.2.3 Add nullable `ToolCall` property of type `AesirToolCallInfo`

---

## Epic 2: Server-Side Tool Call Broadcasting

> **PRIORITY: HIGH** - Intercept and stream tool calls

### 2.1 Create Tool Call Broadcaster Service

**Goal:** Thread-safe communication between SK filter and streaming.

**New Files:**
- `Server/Modules/Aesir.Modules.Inference/Services/IToolCallBroadcaster.cs`
- `Server/Modules/Aesir.Modules.Inference/Services/ToolCallBroadcaster.cs`

**Work Items:**
- [x] 2.1.1 Create `IToolCallBroadcaster` interface with:
  - `CreateScope()` - creates per-request scope
  - `BroadcastStartAsync(AesirToolCallInfo)`
  - `BroadcastCompletionAsync(AesirToolCallInfo)`
- [x] 2.1.2 Implement `ToolCallBroadcaster` using `Channel<T>` for thread-safety
- [x] 2.1.3 Use `AsyncLocal<T>` for per-request scoping
- [x] 2.1.4 Create `IToolCallBroadcasterScope` with `ChannelReader<AesirToolCallInfo>`

---

### 2.2 Create Tool Call Streaming Filter

**Goal:** Intercept SK auto function invocations and broadcast them.

**New File:** `Server/Modules/Aesir.Modules.Inference/Services/ToolCallStreamingFilter.cs`

**Work Items:**
- [x] 2.2.1 Implement `IAutoFunctionInvocationFilter` interface
- [x] 2.2.2 Extract tool call info from `AutoFunctionInvocationContext`:
  - ToolCallId, Function.Name, Function.PluginName, Function.Description
  - Arguments (serialize with JSON)
- [x] 2.2.3 Determine `ToolCallType` from function name patterns:
  - "hybriddocumentsearch", "semanticdocumentsearch" -> DocumentSearch
  - "websearch" -> WebSearch
  - PluginName starts with "MCP" -> McpServer
  - "analyzeimage" -> ImageAnalysis
  - "summarize" -> Summarization
- [x] 2.2.4 Track timing with `Stopwatch` for duration calculation
- [x] 2.2.5 Broadcast start event before `await next(context)`
- [x] 2.2.6 Broadcast completion/failure event after execution
- [x] 2.2.7 Truncate result to 500 chars for UI preview

---

### 2.3 Integrate with Streaming

**Goal:** Merge tool call events with text streaming.

**Modify:** `Server/Modules/Aesir.Modules.Inference/Services/BaseChatService.cs`

**Work Items:**
- [x] 2.3.1 Inject `IToolCallBroadcaster` into `BaseChatService`
- [x] 2.3.2 Create broadcaster scope at start of `ChatCompletionsStreamedAsync`
- [x] 2.3.3 Create `MergeToolCallsAndContent` method to interleave:
  - Check `ChannelReader.TryRead()` for pending tool calls (non-blocking)
  - Yield tool call events with appropriate EventType
  - Continue with text content streaming
- [x] 2.3.4 Drain remaining tool call events after content stream completes
- [x] 2.3.5 Complete/dispose broadcaster scope when done

---

### 2.4 Service Registration

**Goal:** Wire up DI for tool call services.

**Modify:** `Server/Modules/Aesir.Modules.Inference/InferenceModule.cs`

**Work Items:**
- [x] 2.4.1 Register `IToolCallBroadcaster` as Singleton
- [x] 2.4.2 Register `ToolCallStreamingFilter` as Scoped
- [x] 2.4.3 Register filter with `IAutoFunctionInvocationFilter` interface
- [x] 2.4.4 Ensure filter is added to Kernel in OpenAI/Ollama implementations

---

## Epic 3: Client-Side State Management

> **PRIORITY: HIGH** - Handle tool call events in Blazor

### 3.1 Create Client Models

**Goal:** Client-side models for tool call state.

**New File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Chat/Models/ToolCallState.cs`

**Work Items:**
- [~] 3.1.1 Create `ToolCallState` class (mirrors server `AesirToolCallInfo`) - **Skipped: Using shared Aesir.Common models directly**
- [~] 3.1.2 Create client-side enums: `ToolCallType`, `ToolCallStatus`, `StreamEventType` - **Skipped: Using shared Aesir.Common enums directly**
- [~] 3.1.3 Add mapping methods from server models - **Skipped: Using shared models directly, no mapping needed**

---

### 3.2 Create Tool Call State Service

**Goal:** Accumulate and manage tool call state during streaming.

**New Files:**
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Chat/Services/IToolCallStateService.cs`
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Chat/Services/ToolCallStateService.cs`

**Work Items:**
- [x] 3.2.1 Create `IToolCallStateService` interface:
  - `CurrentToolCalls` readonly list
  - `OnToolCallsChanged` event
  - `ProcessToolCallEvent(AesirToolCallInfo)`
  - `Clear()`
- [x] 3.2.2 Implement state accumulation:
  - Track tool calls by ToolCallId in dictionary
  - Update existing on completion, add new on start
  - Fire change event on updates
- [x] 3.2.3 Register service as Scoped in `ChatModule.cs`

---

## Epic 4: UI Components

> **PRIORITY: HIGH** - Visual representation of tool calls

### 4.1 Create Tool Call Card Component

**Goal:** Individual tool call display with expandable details.

**New File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Chat/Components/ToolCallCard.razor`

**Work Items:**
- [x] 4.1.1 Create card layout:
  - Header: Icon, tool name, type badge, status indicator, duration
  - Expandable details: arguments, result preview, error message
- [x] 4.1.2 Implement color coding by ToolCallType:
  - DocumentSearch: Green (#4CAF50)
  - WebSearch: Blue (#2196F3)
  - McpServer: Orange (#FF9800)
  - ImageAnalysis: Pink (#E91E63)
  - Summarization: Purple (#9C27B0)
  - Other: Gray (#607D8B)
- [x] 4.1.3 Add status icons:
  - Started: Spinning progress indicator
  - Completed: Green checkmark
  - Failed: Red error icon
- [x] 4.1.4 Add expand/collapse for details section
- [x] 4.1.5 Add human-readable function name transformation
- [x] 4.1.6 Add pulse animation for active tool calls

---

### 4.2 Create Tool Calls Section Component

**Goal:** Collapsible container for all tool calls (like Thinking section).

**New File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Chat/Components/ToolCallsSection.razor`

**Work Items:**
- [x] 4.2.1 Create collapsible header with:
  - Expand/collapse icon
  - Tools icon
  - Summary text (e.g., "Using 2 tools...", "Used 3 tools (2 completed, 1 failed)")
  - Active indicator (spinner when tools running)
- [x] 4.2.2 Create content area with list of `ToolCallCard` components
- [x] 4.2.3 Auto-expand when tools are active during streaming
- [x] 4.2.4 Style with purple accent color to match tools theme
- [x] 4.2.5 Add fade-in animation for new tool calls

---

### 4.3 Integrate with AssistantMessage

**Goal:** Add tool calls section to assistant message display.

**Modify:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Chat/Components/AssistantMessage.razor`

**Work Items:**
- [x] 4.3.1 Add `ToolCalls` parameter (IReadOnlyList<AesirToolCallInfo>)
- [x] 4.3.2 Add `ShowToolCalls` parameter (bool, default true)
- [x] 4.3.3 Add `IsStreaming` parameter for auto-expand behavior
- [x] 4.3.4 Render `ToolCallsSection` before Thinking section (when tools exist)
- [x] 4.3.5 Pass tool calls from parent component

---

### 4.4 Integrate with ChatPage

**Goal:** Handle tool call events in streaming loop.

**Modify:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Chat/Pages/ChatPage.razor`

**Work Items:**
- [x] 4.4.1 Inject `IToolCallStateService`
- [x] 4.4.2 Add `_currentToolCalls` list field
- [x] 4.4.3 In streaming loop, check `chunk.EventType`:
  - If ToolCallStart or ToolCallResult: process via ToolCallStateService
  - Update `_currentToolCalls` from service
- [x] 4.4.4 Pass tool calls to `AssistantMessage` component
- [x] 4.4.5 Clear tool call state when starting new message
- [x] 4.4.6 Handle tool calls in streaming message render

---

## Epic 5: Testing

> **PRIORITY: MEDIUM** - Ensure quality

### 5.1 Unit Tests

**Work Items:**
- [ ] 5.1.1 Test `ToolCallType` determination logic
- [ ] 5.1.2 Test argument serialization
- [ ] 5.1.3 Test result truncation
- [ ] 5.1.4 Test `ToolCallStateService` state accumulation
- [ ] 5.1.5 Test event type mapping

---

### 5.2 Component Tests

**Work Items:**
- [ ] 5.2.1 Test `ToolCallCard` rendering for each status
- [ ] 5.2.2 Test `ToolCallCard` color coding
- [ ] 5.2.3 Test `ToolCallsSection` summary text generation
- [ ] 5.2.4 Test expand/collapse behavior

---

### 5.3 Manual Testing Checklist

**Document Search:**
- [ ] Tool call appears when searching documents
- [ ] Shows query argument
- [ ] Shows result preview with search results
- [ ] Duration displays correctly

**Web Search:**
- [ ] Tool call appears when web search enabled
- [ ] Shows search query
- [ ] Shows result summary

**MCP Tools:**
- [ ] Tool call appears for MCP server tools
- [ ] Shows correct tool name and arguments
- [ ] Handles complex input/output schemas

**Streaming:**
- [ ] Tool calls appear in real-time during streaming
- [ ] Multiple tool calls display correctly
- [ ] Section auto-expands during streaming
- [ ] Section collapses after completion

**Error Handling:**
- [ ] Failed tool calls display error message
- [ ] UI remains stable on tool call errors

---

## Architecture Decisions

### 1. Channel-Based Broadcasting
**Decision:** Use `Channel<T>` with `AsyncLocal<T>` for per-request scoping.

**Rationale:**
- Thread-safe communication between filter and streaming
- Non-blocking reads don't slow down text streaming
- AsyncLocal provides natural request scoping

### 2. Event Type Discrimination
**Decision:** Add `EventType` enum to streaming results instead of separate endpoints.

**Rationale:**
- Backward compatible (default to Text)
- Single streaming connection handles all event types
- Simpler client implementation

### 3. Filter vs. Hook Pattern
**Decision:** Use `IAutoFunctionInvocationFilter` to intercept tool calls.

**Rationale:**
- SK already provides this pattern
- `KernelLoggingFilterService` proves it works
- Clean separation of concerns

---

## Critical Files

### Server (Create/Modify)
| File | Action | Purpose |
|------|--------|---------|
| `Common/Aesir.Common/Models/AesirToolCallInfo.cs` | Create | Tool call data model |
| `Common/Aesir.Common/Models/AesirChatStreamedResultBase.cs` | Modify | Add EventType, ToolCall properties |
| `Server/Modules/Aesir.Modules.Inference/Services/IToolCallBroadcaster.cs` | Create | Broadcaster interface |
| `Server/Modules/Aesir.Modules.Inference/Services/ToolCallBroadcaster.cs` | Create | Channel-based implementation |
| `Server/Modules/Aesir.Modules.Inference/Services/ToolCallStreamingFilter.cs` | Create | SK auto function filter |
| `Server/Modules/Aesir.Modules.Inference/Services/BaseChatService.cs` | Modify | Merge tool calls with streaming |
| `Server/Modules/Aesir.Modules.Inference/InferenceModule.cs` | Modify | Register services |

### Client (Create/Modify)
| File | Action | Purpose |
|------|--------|---------|
| `Client/.../Chat/Models/ToolCallState.cs` | Create | Client-side models |
| `Client/.../Chat/Services/IToolCallStateService.cs` | Create | State service interface |
| `Client/.../Chat/Services/ToolCallStateService.cs` | Create | State accumulation |
| `Client/.../Chat/Components/ToolCallCard.razor` | Create | Individual tool display |
| `Client/.../Chat/Components/ToolCallsSection.razor` | Create | Collapsible container |
| `Client/.../Chat/Components/AssistantMessage.razor` | Modify | Add tool calls section |
| `Client/.../Chat/Pages/ChatPage.razor` | Modify | Handle tool call events |
| `Client/.../Chat/ChatModule.cs` | Modify | Register services |

---

## Implementation Sequence

1. **Epic 1** (Common Models) - Foundation, must be first
2. **Epic 2** (Server Broadcasting) - Server-side infrastructure
3. **Epic 3** (Client State) - Client-side infrastructure
4. **Epic 4** (UI Components) - Visual implementation
5. **Epic 5** (Testing) - Quality assurance

---

## Success Criteria

- [ ] Tool calls display in real-time during AI response streaming
- [ ] Each tool type has distinct color and icon
- [ ] Users can expand/collapse tool call details
- [ ] Tool execution time is displayed
- [ ] Input arguments are visible
- [ ] Result previews are shown (truncated)
- [ ] Failed tool calls show error messages
- [ ] Backward compatible - existing clients continue to work
- [ ] No performance regression in streaming
