# WORK_PLAN_RELEASE_13.md

Work items for unifying observability logging with enhanced inference tracking and tool call visualization.

## Overview

This release unifies the two existing tool call capture systems (KernelLoggingFilterService and ToolCallStreamingFilter) into a cohesive observability architecture. Currently, the streaming filter captures rich data (timing, status, results) that is never persisted, while the logging filter persists limited metadata without execution details.

The new architecture introduces an "Inference Log" concept - each user query becomes a parent record containing the full context: the user's query, all tool calls made (with timing, status, and results), and the AI's response. The UI will show summaries by default with on-demand detail fetching for performance.

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Architecture | Keep separate filters (streaming vs persistence) | Better separation of concerns - streaming is per-request, persistence is aggregate |
| Data Model | Use `AesirToolCallInfo` as unified model | Already has richer data (status, timing, results); add session context to it |
| Logging Style | Single entry after completion | One row per inference with full timing data; simpler queries, less storage |
| Storage Strategy | API projection (Option D) | Single table with full data, API returns summary by default, details on demand |
| Data Structure | Parent-Child | One `InferenceLog` row per user query with `tool_calls` as nested JSONB array |
| Event Scope | AutoFunctionInvocation only | Focus on AI-triggered tool calls; FunctionInvocation and PromptRender removed |
| Response Capture | Include truncated AI response | Store first ~500 chars for context in observability view |

## Legend

- [ ] Not started
- [x] Completed
- [~] Skipped (with reason in comments)

---

## Sprint Plan

**Sprint 1: Data Model & Infrastructure**
- 1.1, 1.2, 1.3 (Epic 1 - Enhanced data models)
- 2.1, 2.2 (Epic 2 - Database schema and migration)

**Sprint 2: Service Layer Implementation**
- 3.1, 3.2, 3.3, 3.4 (Epic 3 - Inference logging service and filter updates)

**Sprint 3: API & Client Infrastructure**
- 4.1, 4.2, 4.3 (Epic 4 - API endpoints)
- 5.1, 5.2 (Epic 5 - Client service layer)

**Sprint 4: UI Implementation**
- 6.1, 6.2, 6.3, 6.4 (Epic 6 - Observability UI components)

**Sprint 5: Testing & Cleanup**
- 7.1, 7.2, 7.3 (Epic 7 - Testing)
- 8.1, 8.2 (Epic 8 - Cleanup and documentation)

---

## Epic 1: Enhanced Data Models

### 1.1 Enhance AesirToolCallInfo Model
- [x] Add `ChatSessionId` (Guid?) property to `AesirToolCallInfo`
- [x] Add `ConversationId` (Guid?) property to `AesirToolCallInfo`
- [x] Add `UnderlyingMethod` (string?) property to `AesirToolCallInfo`
- [x] Add appropriate `[JsonPropertyName]` attributes for snake_case serialization
- [x] File: `Common/Aesir.Common/Models/AesirToolCallInfo.cs`

### 1.2 Create AesirInferenceLog Model
- [x] Create new `AesirInferenceLog` class in `Common/Aesir.Common/Models/`
- [x] Properties:
  - `Id` (Guid) - Primary key
  - `ChatSessionId` (Guid?) - Reference to chat session
  - `ConversationId` (Guid?) - Reference to conversation
  - `UserQuery` (string) - The user's input that triggered inference
  - `UserQueryTruncated` (string?) - First 500 chars for list view
  - `AssistantResponse` (string?) - AI response (truncated to 500 chars)
  - `ToolCalls` (List<AesirToolCallInfo>) - Nested tool calls array
  - `ToolCallCount` (int) - Number of tool calls made
  - `TotalDurationMs` (long?) - Total inference duration
  - `StartedAt` (DateTimeOffset) - When inference started
  - `CompletedAt` (DateTimeOffset?) - When inference completed
  - `Status` (InferenceStatus enum) - Completed/Failed/Cancelled
  - `ErrorMessage` (string?) - Error if failed
- [x] Add `[JsonPropertyName]` attributes for all properties
- [x] File: `Common/Aesir.Common/Models/AesirInferenceLog.cs`

### 1.3 Create Supporting Enums and DTOs
- [x] Create `InferenceStatus` enum: `InProgress`, `Completed`, `Failed`, `Cancelled`
- [x] Create `AesirInferenceLogSummary` DTO for list views (excludes full ToolCalls details)
  - Include: Id, ChatSessionId, UserQueryTruncated, AssistantResponse, ToolCallCount, TotalDurationMs, StartedAt, Status
  - Exclude: Full UserQuery, ToolCalls with Results/Arguments
- [x] Create `InferenceLogFilterRequest` for search API
- [x] Create `PagedInferenceLogResponse` for paginated results
- [x] File: `Common/Aesir.Common/Models/AesirInferenceLog.cs` (or separate files)

---

## Epic 2: Database Schema

### 2.1 Create Migration for Inference Log Table
- [x] Create migration `Migration{timestamp}` in `Aesir.Modules.Logging/Migrations/`
- [x] Create table `aesir_log_inference` with columns:
  - `id` (UUID, PK)
  - `chat_session_id` (UUID, nullable, indexed)
  - `conversation_id` (UUID, nullable, indexed)
  - `user_query` (TEXT) - Full query
  - `user_query_truncated` (VARCHAR 500) - For list views
  - `assistant_response` (VARCHAR 500, nullable) - Truncated response
  - `tool_calls` (JSONB) - Full tool call array
  - `tool_call_count` (INT) - Denormalized count
  - `total_duration_ms` (BIGINT, nullable)
  - `started_at` (TIMESTAMPTZ)
  - `completed_at` (TIMESTAMPTZ, nullable)
  - `status` (VARCHAR 20) - Enum as string
  - `error_message` (VARCHAR 1000, nullable)
- [x] Create indexes:
  - `ix_aesir_log_inference_chat_session_id`
  - `ix_aesir_log_inference_conversation_id`
  - `ix_aesir_log_inference_started_at`
  - `ix_aesir_log_inference_status`
  - `ix_aesir_log_inference_tool_calls_gin` (GIN index on JSONB)

### 2.2 Drop Old Kernel Log Table
- [x] Drop old `aesir_log_kernel` table (no longer needed)
- [x] File: `Server/Modules/Aesir.Modules.Logging/Migrations/Migration20251214150001.cs`

---

## Epic 3: Service Layer

### 3.1 Create IInferenceLogService Interface
- [x] Create interface in `Aesir.Modules.Logging/Services/`
- [x] Methods:
  - `Task LogInferenceAsync(AesirInferenceLog log, CancellationToken ct = default)`
  - `Task<AesirInferenceLog?> GetByIdAsync(Guid id, CancellationToken ct = default)`
  - `Task<PagedInferenceLogResponse> SearchAsync(InferenceLogFilterRequest filter, CancellationToken ct = default)`
  - `Task<IEnumerable<AesirInferenceLogSummary>> GetSummariesAsync(InferenceLogFilterRequest filter, CancellationToken ct = default)`
  - `Task<AesirInferenceLog?> GetByChatSessionLatestAsync(Guid chatSessionId, CancellationToken ct = default)`

### 3.2 Implement InferenceLogService
- [x] Create `InferenceLogService` implementing `IInferenceLogService`
- [x] Use Dapper for database access
- [x] Register custom `JsonTypeHandler<List<AesirToolCallInfo>>` for JSONB serialization
- [x] Implement search with dynamic WHERE clause building (similar to existing KernelLogService)
- [x] Support filtering by: time range, chat session, conversation, status, tool call count
- [x] Support pagination and sorting
- [x] File: `Aesir.Modules.Logging/Services/InferenceLogService.cs`

### 3.3 Create InferenceLogCollector
- [x] Create `IInferenceLogCollector` interface for collecting tool calls during inference
- [x] Create `InferenceLogCollector` implementation:
  - `Start(Guid? chatSessionId, Guid? conversationId, string userQuery)` - Initialize collection
  - `AddToolCall(AesirToolCallInfo toolCall)` - Add tool call to collection
  - `Complete(string? assistantResponse)` - Finalize and return AesirInferenceLog
  - `Fail(string errorMessage)` - Mark as failed
- [x] Make it scoped per-request (store in Kernel.Data like broadcaster)
- [x] File: `Aesir.Modules.Inference/Services/InferenceLogCollector.cs`

### 3.4 Delete Old KernelLoggingFilterService
- [x] Deleted `KernelLoggingFilterService.cs` entirely (old logging approach removed)
- [x] Tool call capture now done in `ToolCallStreamingFilter` directly
- [~] Old IFunctionInvocationFilter, IPromptRenderFilter, IAutoFunctionInvocationFilter filters removed

### 3.5 Update BaseChatService to Integrate Logging
- [x] In `ChatCompletionsStreamedAsync`:
  - Create and start `InferenceLogCollector` at beginning
  - Store collector in Kernel.Data alongside broadcaster
  - After streaming completes, call `collector.Complete(assistantResponse)`
  - Persist via `IInferenceLogService.LogInferenceAsync()`
- [x] Handle errors: call `collector.Fail(errorMessage)` and still persist
- [x] Extract assistant response (truncated) from final streaming result
- [x] File: `Server/Modules/Aesir.Modules.Inference/Services/BaseChatService.cs`

---

## Epic 4: API Endpoints

### 4.1 Create InferenceLogsController
- [x] Create controller at `Aesir.Modules.Logging/Controllers/InferenceLogsController.cs`
- [x] Route: `[Route("logs/inference")]`
- [x] Endpoints:
  - `GET /logs/inference` - List with pagination (returns summaries)
  - `GET /logs/inference/{id}` - Get full details by ID (includes full ToolCalls)
  - `GET /logs/inference/search` - Advanced search with filters
  - `GET /logs/inference/chatsession/{chatSessionId}` - Get by chat session

### 4.2 Implement Summary vs Detail Response
- [x] List endpoint returns `AesirInferenceLogSummary` (light data)
- [x] Detail endpoint returns full `AesirInferenceLog` with complete ToolCalls
- [x] Summaries include: tool call count, function names only (no arguments/results)
- [x] Details include: full arguments, results, errors for each tool call

### 4.3 Delete Old Kernel Logs Endpoints
- [x] Deleted old `/logs/kernel/*` endpoints (LogsController.cs removed)
- [x] Old API no longer available - clean break

---

## Epic 5: Client Service Layer

### 5.1 Create Client Models
- [x] Updated client models to use server-shared Common models
- [x] Create `LogFilter` for client-side filtering (uses new properties: Statuses, HasToolCalls, SearchText)
- [x] Create `PagedLogResponse` with computed properties (TotalPages, HasNextPage, HasPreviousPage)
- [x] Create `TimeGroupedLogs` using `AesirInferenceLogSummary`
- [x] File: `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Observability/Models/`

### 5.2 Update ObservabilityService
- [x] Updated `IObservabilityService` interface with new methods
- [x] Updated `LoadLogsAsync` to call `/logs/inference` endpoints
- [x] Updated state management to use `AesirInferenceLogSummary`
- [x] Updated `GroupedLogs` to return `TimeGroupedLogs` using new model
- [x] File: `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Observability/Services/ObservabilityService.cs`

### 5.3 Delete Old Client Models
- [x] Deleted `AesirKernelLog.cs` and `AesirKernelLogDetails.cs` from client
- [x] Client now uses shared Common models

---

## Epic 6: Observability UI

### 6.1 Create InferenceLogCard Component
- [x] Create new component to display a single inference log entry
- [x] Show collapsed view:
  - User query (truncated)
  - Tool call count badge (e.g., "3 tools used")
  - Total duration
  - Status indicator (success/failed)
  - Timestamp
- [x] Show expanded view:
  - Full user query
  - Assistant response (truncated)
  - List of tool calls (as nested cards)
  - "View Details" button to fetch full data
- [x] File: `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Observability/Components/InferenceLogCard.razor`

### 6.2 Create StatusIndicator Component
- [x] Created `StatusIndicator.razor` to display inference status
- [x] Color-coded indicators: green (completed), red (failed), blue (in progress), gray (cancelled)
- [x] File: `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Observability/Components/StatusIndicator.razor`

### 6.3 Delete Old Components
- [x] Deleted `LogEntryCard.razor` (replaced by InferenceLogCard)
- [x] Deleted `LogLevelIndicator.razor` (no longer needed)
- [x] Deleted `LogTypeChip.razor` (no longer needed - reusing ToolCallTypeChip from Chat)

### 6.4 Update TimelineGroup and ObservabilityContent
- [x] Updated `TimelineGroup.razor` to use `InferenceLogCard` instead of `LogEntryCard`
- [x] Updated `ObservabilityContent.razor` with new `HasActiveFilters` logic
- [x] Keep time-based grouping (Today, Yesterday, etc.)
- [x] Files: `Components/TimelineGroup.razor`, `Components/ObservabilityContent.razor`

### 6.5 Update FilterBar
- [x] Filter options updated for new data model
- [x] Filter by: Status (Completed/Failed), Has tool calls, Search text
- [x] Removed old log type/level filters
- [x] File: `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Observability/Components/FilterBar.razor`

---

## Epic 7: Testing

### 7.1 Server Unit Tests
- [x] Delete old `KernelLogServiceTests.cs` (old service removed)
- [x] Delete old `LogsControllerTests.cs` (old controller removed)
- [x] Delete old `KernelLogFilterRequestTests.cs` (old model removed)
- [x] File: `Server/Modules/Aesir.Modules.Logging.Tests/`

### 7.2 Client Unit Tests
- [x] Updated `LogFilterTests.cs` with new filter properties (Statuses, HasToolCalls, SearchText)
- [x] Updated `ObservabilityServiceTests.cs` to use new models and computed properties
- [x] Updated `TimeGroupedLogsTests.cs` to use `AesirInferenceLogSummary`
- [x] Updated `SettingsPageTests.cs` to use new models
- [x] All 1087 client tests passing
- [x] File: `Client/Aesir.Client.Web/Aesir.Client.Web.Tests/Unit/Observability/`

### 7.3 Integration Tests
- [ ] Manual testing required: send chat → verify inference log appears in Observability UI
- [ ] Manual testing required: verify tool calls with timing are displayed
- [ ] All unit tests pass: `dotnet test` returns 1177 passing tests

---

## Epic 8: Cleanup & Documentation

### 8.1 Complete Code Removal
- [x] Deleted `IKernelLogService.cs` and `KernelLogService.cs`
- [x] Deleted `KernelLoggingFilterService.cs`
- [x] Deleted `KernelLog.cs`, `KernelLogDetails.cs`, `KernelLogFilterRequest.cs`, `PagedKernelLogResponse.cs`
- [x] Deleted `AesirKernelLogBase.cs`, `AesirKernelLogDetailsBase.cs` from Common
- [x] Deleted old client models: `AesirKernelLog.cs`, `AesirKernelLogDetails.cs`
- [x] Deleted old UI components: `LogEntryCard.razor`, `LogLevelIndicator.razor`, `LogTypeChip.razor`
- [x] Updated `LoggingModule.cs` to only register new `IInferenceLogService`
- [x] Removed old Avalonia client projects from solution (Aesir.Client, Aesir.Client.Browser, Aesir.Client.Desktop)

### 8.2 Documentation
- [x] Updated WORK_PLAN_RELEASE_13.md with completion status
- [ ] Update CLAUDE.md with new observability architecture (future task)
- [ ] Document new API endpoints (future task)

---

## Work Item Dependencies

```
1.x [Data Models]
 └── 2.x [Database Migration]
      └── 3.x [Service Layer] ──────────┬── 7.1 [Server Tests]
           ├── 4.x [API Endpoints] ─────┤
           │    └── 5.x [Client Services]
           │         └── 6.x [UI] ──────┴── 7.2 [Client Tests]
           └── 3.5 [BaseChatService Integration]
                └── 7.3 [Integration Tests]
                     └── 8.x [Cleanup]
```

---

## Configuration

### No New Environment Variables Required

This feature uses existing configuration:
- Database connection string (existing)
- API base URL (existing)

### No appsettings.json Changes Required

---

## Success Criteria

- [x] All epic tasks completed or explicitly skipped with reason
- [x] All tests passing (`dotnet test`) - 1177 tests pass
- [ ] Docker build successful (manual verification required)
- [ ] Manual verification:
  - [ ] Send a chat message that triggers tool calls
  - [ ] Verify inference log appears in Observability UI
  - [ ] Verify user query is shown
  - [ ] Verify tool calls with timing are displayed
  - [ ] Verify "View Details" fetches full data
  - [ ] Verify assistant response is captured
- [ ] CLAUDE.md updated with architectural changes (future task)
- [ ] No regressions in existing chat functionality (manual verification required)
- [ ] No regressions in existing tool call streaming to chat UI (manual verification required)

---

## Test Commands

```bash
# Run all tests
dotnet test

# Run logging module tests
dotnet test Server/Modules/Aesir.Modules.Logging.Tests/

# Run client observability tests
dotnet test Client/Aesir.Client.Web/Aesir.Client.Web.Tests/ --filter "FullyQualifiedName~Observability"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## Resources

- Microsoft Semantic Kernel Filters Documentation: https://learn.microsoft.com/en-us/semantic-kernel/concepts/enterprise-readiness/filters
- Existing implementation:
  - `Server/Modules/Aesir.Modules.Logging/Services/KernelLoggingFilterService.cs`
  - `Server/Modules/Aesir.Modules.Inference/Services/ToolCallStreamingFilter.cs`
  - `Common/Aesir.Common/Models/AesirToolCallInfo.cs`
  - `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Observability/`
