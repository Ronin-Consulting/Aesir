# WORK_PLAN_RELEASE_12: Observability Feature

## Overview
Create a comprehensive Observability view for viewing AI operation logs (tool executions, prompt renders, function invocations) with timeline-based UI, full filtering capabilities, and navigation to related chat sessions.

---

## User Requirements Summary
- **Use Case**: Both debugging/troubleshooting AND monitoring overview
- **Refresh**: Manual refresh only (no auto-refresh)
- **Layout**: Timeline feed with expandable log cards grouped by time
- **API Changes**: Full enhancement with pagination and advanced filtering
- **Filters**: All four - Log Level, Log Type, Time Range, Function/Plugin Name
- **Chat Link**: In details panel only (keeps cards clean)

---

## Implementation Phases

### Phase 1: Server API Enhancements

#### 1.1 New Models
**Files to create:**
- `Server/Modules/Aesir.Modules.Logging/Models/KernelLogFilterRequest.cs`
- `Server/Modules/Aesir.Modules.Logging/Models/PagedKernelLogResponse.cs`

```csharp
// KernelLogFilterRequest.cs
public class KernelLogFilterRequest
{
    public int Page { get; set; } = 1;           // 1-based
    public int PageSize { get; set; } = 50;       // Max 200
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public Guid? ChatSessionId { get; set; }
    public List<KernelLogLevel>? Levels { get; set; }
    public List<KernelLogType>? Types { get; set; }
    public string? FunctionName { get; set; }     // Partial match
    public string? PluginName { get; set; }       // Partial match
    public string? MessageSearch { get; set; }    // Partial match
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}

// PagedKernelLogResponse.cs
public class PagedKernelLogResponse
{
    public IEnumerable<KernelLog> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
}
```

#### 1.2 Service Layer Updates
**Files to modify:**
- `Server/Modules/Aesir.Modules.Logging/Services/IKernelLogService.cs` - Add `SearchLogsAsync` method
- `Server/Modules/Aesir.Modules.Logging/Services/KernelLogService.cs` - Implement with dynamic WHERE clause builder

**Key SQL patterns:**
```sql
-- JSONB filtering for type, function name, plugin name
WHERE details->>'type' = ANY(@Types)
AND details->>'function_name' ILIKE @FunctionName
AND level = ANY(@Levels)
ORDER BY created_at DESC
LIMIT @PageSize OFFSET @Offset
```

#### 1.3 Controller Update
**File to modify:**
- `Server/Modules/Aesir.Modules.Logging/Controllers/LogsController.cs`

**New endpoint:**
```
GET /logs/kernel/search?page=1&pageSize=50&levels=Error,Warning&types=FunctionInvocation&from=2024-01-01&functionName=chat
```

#### 1.4 Database Migration
**File to create:**
- `Server/Modules/Aesir.Modules.Logging/Migrations/Migration20251214000001.cs`

**Indexes to add:**
- `ix_aesir_log_kernel_created_at` (BTREE, DESC)
- `ix_aesir_log_kernel_level` (BTREE)
- `ix_aesir_log_kernel_details_gin` (GIN on details JSONB)
- `ix_aesir_log_kernel_type` (BTREE on details->>'type')
- `ix_aesir_log_kernel_function_name` (pattern_ops for ILIKE)

#### 1.5 Server Unit Tests
**Files to create:**
- `Server/Modules/Aesir.Modules.Logging.Tests/Aesir.Modules.Logging.Tests.csproj`
- `Server/Modules/Aesir.Modules.Logging.Tests/Services/KernelLogServiceTests.cs`
- `Server/Modules/Aesir.Modules.Logging.Tests/Controllers/LogsControllerTests.cs`
- `Server/Modules/Aesir.Modules.Logging.Tests/Models/KernelLogFilterRequestTests.cs`

---

### Phase 2: Client Module Structure

#### 2.1 Module Setup
**Files to create:**
```
Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Observability/
├── Aesir.Client.Web.Modules.Observability.csproj
├── ObservabilityModule.cs
├── _Imports.razor
├── Pages/
│   └── ObservabilityPage.razor
├── Components/
│   ├── LogEntryCard.razor
│   ├── FilterBar.razor
│   ├── TimelineGroup.razor
│   ├── LogLevelIndicator.razor
│   ├── LogTypeChip.razor
│   └── ArgumentsViewer.razor
├── Services/
│   ├── IObservabilityService.cs
│   └── ObservabilityService.cs
└── Models/
    ├── LogFilter.cs
    └── TimeGroupedLogs.cs
```

#### 2.2 Module Registration (ObservabilityModule.cs)
```csharp
public class ObservabilityModule : ClientModuleBase
{
    public override string Name => "Observability";
    public override string Version => "1.0.0";
    public override string Description => "AI operation log viewing and analysis.";

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IObservabilityService, ObservabilityService>();
    }

    public override void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register(new NavigationItem
        {
            Title = "Observability",
            Href = "/observability",
            Icon = "Timeline",
            Priority = 30,
            Group = "Main"
        });
    }
}
```

#### 2.3 Integration Points
**Files to modify:**
- `Client/Aesir.Client.Web/Aesir.Client.Web.App/Program.cs` - Add `builder.Services.AddModule<ObservabilityModule>()`
- `Client/Aesir.Client.Web/Aesir.Client.Web.App/App.razor` - Add assembly to router
- `Client/Aesir.Client.Web/Aesir.Client.Web.App/_Imports.razor` - Add namespace

---

### Phase 3: Client Components

#### 3.1 ObservabilityPage.razor
**Layout**: Full-page timeline with filter bar at top
**Pattern**: Similar to ChatPage.razor but uses MainLayout (full width)
**Features**:
- FilterBar component at top
- TimelineGroup components for date groupings (Today, Yesterday, This Week, etc.)
- Empty state when no logs
- Loading state with MudProgressCircular
- Manual refresh button

#### 3.2 FilterBar.razor
**Pattern**: Based on ToolsPage.razor chip filtering
**Contains**:
- Log Level chip toggles (Info=blue, Warning=amber, Error=red)
- Log Type chip toggles (Function=green, Auto=orange, Prompt=purple)
- MudDateRangePicker for time range
- MudTextField for function/plugin search
- Refresh button (MudIconButton)

#### 3.3 LogEntryCard.razor
**Pattern**: Based on ToolCallCard.razor collapsible pattern
**Header (always visible)**:
- Timestamp (relative, e.g., "2:34 PM")
- LogLevelIndicator (colored dot/badge)
- LogTypeChip
- Function name with plugin badge
- Message summary (truncated)
- Duration if available
- Expand/collapse icon

**Details (when expanded)**:
- Full arguments with ArgumentsViewer (JSON formatting)
- Result (if available)
- Error (if available, red styling)
- "View Chat" button (only if ChatSessionId exists)

#### 3.4 Color Scheme
Following existing ToolCallCard.razor patterns:
- **Info**: #54A9FF (Aesir Blue)
- **Warning**: #F59E0B (Amber)
- **Error**: #EF4444 (Red)
- **FunctionInvocation**: #10B981 (Green/Tertiary)
- **AutoFunction**: #F59E0B (Orange)
- **PromptRender**: #9C27B0 (Purple)

#### 3.5 ObservabilityService
**Interface:**
```csharp
public interface IObservabilityService
{
    bool IsLoading { get; }
    LogFilter CurrentFilter { get; }
    Task<PagedKernelLogResponse> LoadEntriesAsync(LogFilter filter, CancellationToken ct = default);
    Task RefreshAsync(CancellationToken ct = default);
}
```

---

### Phase 4: Client Unit Tests

**Files to create:**
- `Client/Aesir.Client.Web/Aesir.Client.Web.Tests/Unit/Observability/Services/ObservabilityServiceTests.cs`
- `Client/Aesir.Client.Web/Aesir.Client.Web.Tests/Unit/Observability/Components/LogEntryCardTests.cs`
- `Client/Aesir.Client.Web/Aesir.Client.Web.Tests/Unit/Observability/Components/FilterBarTests.cs`
- `Client/Aesir.Client.Web/Aesir.Client.Web.Tests/Unit/Observability/Components/ObservabilityPageTests.cs`

**Test coverage:**
- Service: Filter application, pagination, API calls, error handling
- Components: Rendering states, user interactions, filter changes, expand/collapse

---

## Critical Files Reference

### Server (to modify/create)
| File | Action | Purpose |
|------|--------|---------|
| `Server/Modules/Aesir.Modules.Logging/Models/KernelLogFilterRequest.cs` | Create | Filter request model |
| `Server/Modules/Aesir.Modules.Logging/Models/PagedKernelLogResponse.cs` | Create | Paginated response |
| `Server/Modules/Aesir.Modules.Logging/Services/IKernelLogService.cs` | Modify | Add SearchLogsAsync |
| `Server/Modules/Aesir.Modules.Logging/Services/KernelLogService.cs` | Modify | Implement search with dynamic WHERE |
| `Server/Modules/Aesir.Modules.Logging/Controllers/LogsController.cs` | Modify | Add search endpoint |
| `Server/Modules/Aesir.Modules.Logging/Migrations/Migration20251214000001.cs` | Create | Add indexes |

### Client (to create)
| File | Purpose |
|------|---------|
| `Modules/Aesir.Client.Web.Modules.Observability/ObservabilityModule.cs` | Module registration |
| `Modules/Aesir.Client.Web.Modules.Observability/Pages/ObservabilityPage.razor` | Main page |
| `Modules/Aesir.Client.Web.Modules.Observability/Components/LogEntryCard.razor` | Timeline entry |
| `Modules/Aesir.Client.Web.Modules.Observability/Components/FilterBar.razor` | Filters |
| `Modules/Aesir.Client.Web.Modules.Observability/Services/ObservabilityService.cs` | API communication |

### Client (to modify)
| File | Change |
|------|--------|
| `Aesir.Client.Web.App/Program.cs` | Register module |
| `Aesir.Client.Web.App/App.razor` | Add assembly to router |
| `Aesir.Client.Web.App/_Imports.razor` | Add namespaces |

---

## Implementation Order

1. **Server API** (required first for client to consume)
   - Create filter/response models
   - Add SearchLogsAsync to service
   - Add controller endpoint
   - Create migration for indexes
   - Write server unit tests

2. **Client Module Foundation**
   - Create project structure
   - Create ObservabilityModule.cs
   - Register in Program.cs, App.razor, _Imports.razor
   - Create service interface and implementation

3. **Client Components**
   - FilterBar.razor
   - LogLevelIndicator.razor, LogTypeChip.razor
   - ArgumentsViewer.razor
   - LogEntryCard.razor
   - TimelineGroup.razor
   - ObservabilityPage.razor

4. **Client Tests**
   - Service tests
   - Component tests

5. **Integration Testing**
   - Browser testing with Playwright
   - Test all filters
   - Test navigation to chat

---

## Success Criteria

- [ ] Server API returns paginated, filtered logs
- [ ] Client displays logs in timeline grouped by date
- [ ] All four filters work correctly (Level, Type, Time Range, Function)
- [ ] Logs can be expanded to show full details
- [ ] "View Chat" link navigates to correct chat session
- [ ] Manual refresh works
- [ ] Empty state displays properly
- [ ] Loading state displays properly
- [ ] All unit tests pass
- [ ] Browser integration tests pass

---

## Notes

- The Observability page uses full width (no side margins) per user requirement
- Navigation icon: Material "Timeline"
- Follow ToolCallCard.razor patterns for consistent look and feel
- Use MainLayout (not ChatLayout) for full-width display
