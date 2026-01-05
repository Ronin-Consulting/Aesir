# Code Cleanup Work Plan: Aesir.Modules.Research

## Summary
- **Review Date**: 2026-01-05
- **Files Reviewed**: 18 core service files
- **Total Findings**: 29 (Critical: 3 ✅ ALL FIXED, High: 8, Medium: 12, Low: 6)
- **Estimated Effort**: 16-24 hours

## Key Files Reviewed
1. `Services/ResearchOrchestrator.cs` (771 lines)
2. `Services/ResearchPhaseExecutor.cs` (726 lines)
3. `Services/ChairmanPlanningService.cs` (276 lines)
4. `Services/ClarificationService.cs` (556 lines)
5. `Services/PeerReviewService.cs` (561 lines)
6. `Services/ReportGeneratorService.cs` (729 lines)
7. `Services/ResearchProgressBroadcaster.cs` (239 lines)
8. `Services/AnonymizationService.cs` (235 lines)
9. `Services/ScoringCalculator.cs` (254 lines)
10. `Services/ConfidenceCalculator.cs` (248 lines)
11. `Execution/ParallelPhaseExecutionStrategy.cs` (192 lines)
12. `Execution/PhaseExecutionStrategyFactory.cs` (87 lines)
13. `Hubs/ResearchHub.cs` (125 lines)
14. `Agents/ResearchAgentFactory.cs` (278 lines)

---

## Critical Findings

### ~~CRIT-01: Race Condition with CancellationToken in Fire-and-Forget Tasks~~ [FIXED]
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/ResearchOrchestrator.cs`
**Status**: FIXED on 2026-01-05

**Fix Applied**:
- Added `IHostApplicationLifetime` dependency for graceful shutdown support
- Added `ConcurrentDictionary<Guid, CancellationTokenSource>` to track active sessions
- Updated both fire-and-forget locations to use session-specific CTS linked to `ApplicationStopping`
- `Task.Run` now uses `CancellationToken.None` so it always starts
- Proper `OperationCanceledException` handling with session status update
- `CancelResearchAsync` now actually cancels running workflows via stored CTS
- Cleanup in `finally` block removes CTS from dictionary and disposes

---

### ~~CRIT-02: AnonymizationService Stores State in Instance Fields (Non-Thread-Safe)~~ [FIXED]
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/AnonymizationService.cs`
**Status**: FIXED on 2026-01-05

**Fix Applied**:
- Created immutable `AnonymizationResult` class containing submissions and bidirectional mappings
- Updated `IAnonymizationService` interface to return `AnonymizationResult`
- Removed instance-level dictionaries from the service class
- All state is now local to `AnonymizeSubmissionsAsync` method
- Mappings returned as `IReadOnlyDictionary` for immutability
- Helper methods `GetAnonymizedId()` and `GetOriginalId()` moved to result class
- Updated `ResearchPhaseExecutor` to use new result type

---

### ~~CRIT-03: ResearchProgressBroadcaster Session ID is Instance State (Scoped Service Issues)~~ [FIXED]
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/ResearchProgressBroadcaster.cs`
**Status**: FIXED on 2026-01-05

**Fix Applied**:
- Removed `_currentSessionId` instance field and `SetCurrentSession()` method
- Updated `IResearchProgressBroadcaster` interface to require `sessionId` as first parameter on all methods
- Updated all 7 caller files to pass `session.Id` explicitly:
  - `ResearchOrchestrator.cs` - Removed 5 `SetCurrentSession` calls, updated 5 broadcast calls
  - `ResearchPhaseExecutor.cs` - Updated 8 broadcast calls + helper method
  - `ClarificationService.cs` - Updated 2 broadcast calls
  - `PeerReviewService.cs` - Updated 2 broadcast calls
  - `ReportGeneratorService.cs` - Updated 6 broadcast calls
  - `ChairmanPlanningService.cs` - Updated 2 broadcast calls
  - `ParallelPhaseExecutionStrategy.cs` - Updated 2 broadcast calls
- Service is now completely stateless and thread-safe
- No more fragile scope management required

---

## High Priority Findings

### HIGH-01: Duplicate Code Pattern - GetChatServiceForAgent Repeated 5 Times
**Files**:
- `ResearchPhaseExecutor.cs` (Lines 521-550)
- `ClarificationService.cs` (Lines 466-482)
- `PeerReviewService.cs` (Lines 453-476)
- `ReportGeneratorService.cs` (Lines 452-481)
- `ChairmanPlanningService.cs` (Lines 120-137)

**Problem**: The exact same logic for resolving `IChatService` via keyed service is copy-pasted across 5 different service files, with only logging message differences.

**Impact**: Maintenance burden; if resolution logic needs to change, 5 files need updating.

**Recommended Fix**: Extract to shared infrastructure:

```csharp
// In Aesir.Infrastructure.Services
public interface IChatServiceResolver
{
    IChatService? GetChatServiceForAgent(Guid? inferenceEngineId);
}

public class ChatServiceResolver : IChatServiceResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatServiceResolver> _logger;

    public IChatService? GetChatServiceForAgent(Guid? inferenceEngineId)
    {
        if (!inferenceEngineId.HasValue)
        {
            _logger.LogWarning("Agent has no inference engine ID configured");
            return null;
        }
        return _serviceProvider.GetKeyedService<IChatService>(inferenceEngineId.Value.ToString());
    }
}
```

---

### HIGH-02: Duplicate Code Pattern - CreateChatRequestAsync Repeated 4 Times
**Files**:
- `ResearchPhaseExecutor.cs` (Lines 555-636)
- `ClarificationService.cs` (Lines 327-395, 400-461)
- `PeerReviewService.cs` (Lines 392-446)
- `ReportGeneratorService.cs` (Lines 361-445)
- `ChairmanPlanningService.cs` (Lines 215-264)

**Problem**: Similar but slightly different chat request creation logic is duplicated across services.

**Impact**: Inconsistent handling of base agent settings, tools, and thinking configuration.

**Recommended Fix**: Create a `ChatRequestBuilder` class:

```csharp
public interface IChatRequestBuilder
{
    Task<AesirChatRequestBase> BuildAsync(
        ResearchAgent agent,
        string userPrompt,
        string? systemPromptOverride = null,
        bool includeTools = false);
}
```

---

### HIGH-03: Magic Numbers in Progress Broadcasting
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/ResearchProgressBroadcaster.cs`
**Lines**: Referenced from helper class, also in ReportGeneratorService.cs Lines 94-118, 201-257

**Problem**: Progress percentages (10%, 30%, 40%, 50%, 85%, 100%) are hardcoded throughout the codebase without clear documentation of what they represent.

**Impact**: Difficult to maintain consistent progress reporting; values seem arbitrary.

**Recommended Fix**: Define progress milestones as constants:

```csharp
public static class ResearchProgressMilestones
{
    public const int PhaseStart = 0;
    public const int PromptBuilt = 30;
    public const int LlmCallStarted = 40;
    public const int LlmCallCompleted = 85;
    public const int PhaseComplete = 100;
}
```

---

### HIGH-04: Long Method - ExecuteResearchWorkflowAsync (210+ lines)
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/ResearchOrchestrator.cs`
**Lines**: 363-573

**Problem**: This method is 210 lines long with high cyclomatic complexity. It handles 5 phases, updates session state, broadcasts progress, and handles errors all in one method.

**Impact**: Difficult to test, maintain, and understand; violates Single Responsibility Principle.

**Recommended Fix**: Extract each phase to separate method or use Strategy/Pipeline pattern:

```csharp
private async Task ExecuteResearchWorkflowAsync(...)
{
    var phases = new IResearchPhaseHandler[]
    {
        new PlanningPhaseHandler(_phaseExecutor, _progressBroadcaster),
        new ResearchPhaseHandler(_phaseExecutor, _progressBroadcaster),
        new AnonymizationPhaseHandler(_phaseExecutor, _progressBroadcaster),
        new PeerReviewPhaseHandler(_phaseExecutor, _progressBroadcaster),
        new SynthesisPhaseHandler(_phaseExecutor, _progressBroadcaster)
    };

    foreach (var phase in phases)
    {
        await phase.ExecuteAsync(session, researchAgents, cancellationToken);
    }
}
```

---

### HIGH-05: Exception Swallowing in ChatSession Updates
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/ResearchOrchestrator.cs`
**Lines**: 709-713, 764-768

**Problem**: Exceptions from `UpdateChatSessionTitleAsync` and `AddReportToChatSessionAsync` are caught, logged, and swallowed. While this prevents research failure from chat updates, it may hide persistent data issues.

**Impact**: Silent failures could leave chat sessions in inconsistent state.

**Recommended Fix**: Add structured error tracking:

```csharp
try
{
    await UpdateChatSessionTitleAsync(...);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to update ChatSession title - adding to session errors");
    session.Warnings ??= new List<string>();
    session.Warnings.Add($"Chat title update failed: {ex.Message}");
    await _sessionRepository.UpdateAsync(session);
}
```

---

### HIGH-06: Hardcoded Model Default "gpt-4"
**Files**:
- `ResearchPhaseExecutor.cs` Line 626
- `ClarificationService.cs` Lines 383, 449
- `PeerReviewService.cs` Line 436
- `ReportGeneratorService.cs` Line 435
- `ChairmanPlanningService.cs` Line 254

**Problem**: Multiple files hardcode `"gpt-4"` as the default model fallback. This should be configurable.

**Impact**: If OpenAI deprecates gpt-4, code changes are needed in 5+ locations.

**Recommended Fix**: Define in configuration:

```csharp
public class ResearchModuleOptions
{
    public string DefaultModel { get; set; } = "gpt-4";
    public double DefaultTemperature { get; set; } = 0.7;
    public int DefaultMaxTokens { get; set; } = 8192;
}
```

---

### HIGH-07: Potential Null Reference in ChairmanPlanningService.ParseUnifiedPlan
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/ChairmanPlanningService.cs`
**Lines**: 146-213

**Problem**: The parsing logic assumes LLM output will follow the expected format. If LLM returns unexpected format, plans may be empty or generic fallbacks are used without proper error reporting.

**Impact**: Research quality degrades silently if Chairman produces unexpected output.

**Recommended Fix**: Add validation and structured logging:

```csharp
if (plans.Count < teamAgents.Count)
{
    _logger.LogWarning(
        "Chairman plan parsing incomplete: expected {Expected} plans, got {Actual}. " +
        "Using generic fallbacks for missing agents. Response sample: {Sample}",
        teamAgents.Count, plans.Count, planResponse.Substring(0, Math.Min(500, planResponse.Length)));
}
```

---

### HIGH-08: Missing ConfigureAwait(false) in Multiple Locations
**Files**: Various

**Problem**: While most async calls use `.ConfigureAwait(false)`, some locations are inconsistent:
- `PeerReviewService.cs` Line 330: `await chatService.ChatCompletionsAsync(request);` (no ConfigureAwait)
- `ChairmanPlanningService.cs` Line 139: Missing ConfigureAwait

**Impact**: Potential deadlocks in non-ASP.NET contexts; inconsistent patterns.

**Recommended Fix**: Add `.ConfigureAwait(false)` consistently to all await calls.

---

## Medium Priority Findings

### MED-01: ResearchPhaseExecutor Has Too Many Dependencies (11 Dependencies)
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/ResearchPhaseExecutor.cs`
**Lines**: 145-169

**Problem**: The constructor takes 11 dependencies, indicating the class does too much.

**Dependencies**:
1. `ILogger<ResearchPhaseExecutor>`
2. `IServiceProvider`
3. `IHubContext<ResearchHub>`
4. `IConfigurationService`
5. `IAnonymizationService`
6. `IPeerReviewService`
7. `IReportGeneratorService`
8. `IScoringCalculator`
9. `IResearchProgressBroadcaster`
10. `IChairmanPlanningService`
11. `IPhaseExecutionStrategyFactory`

**Impact**: Violates Single Responsibility Principle; difficult to test.

**Recommended Fix**: Consider splitting into phase-specific executors that implement a common interface.

---

### MED-02: Unused Parameter sessionId in Multiple Methods
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/ClarificationService.cs`
**Lines**: 75, 139

**Problem**: The `sessionId` parameter is passed to `GenerateClarificationQuestionsAsync` and `RefineQueryAsync` but is never used (broadcasters get session from their state).

**Impact**: Misleading API; dead code.

**Recommended Fix**: Either use the sessionId (preferred, see CRIT-03) or remove it from the signature.

---

### MED-03: Data Clumps - Repeated (ResearchSession, ResearchAgent) Parameter Pairs
**Files**: Multiple phase executor methods

**Problem**: Many methods take `(ResearchSession session, ResearchAgent agent, ...)` which suggests these should be bundled.

**Recommended Fix**: Create a `ResearchContext` class:

```csharp
public class ResearchContext
{
    public ResearchSession Session { get; }
    public ResearchAgent Agent { get; }
    public string RefinedQuery => Session.RefinedQuery ?? Session.Query;
    public CancellationToken CancellationToken { get; }
}
```

---

### MED-04: Stringly-Typed Role Matching
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/ChairmanPlanningService.cs`
**Lines**: 153-175

**Problem**: Role parsing relies on string matching (`"### Deep Diver"`, `"**Synthesizer**"`) which is fragile.

**Impact**: LLM format changes could break plan parsing.

**Recommended Fix**: Use structured output (JSON) with retry on parse failure:

```csharp
var prompt = """
Return the plan as JSON:
{
  "deep_diver": "...",
  "synthesizer": "...",
  "devils_advocate": "..."
}
""";
```

---

### MED-05: Excessive Debug Logging
**Files**: All service files

**Problem**: The codebase contains extensive `_logger.LogDebug` calls with prefixes like `[RESEARCH]`, `[PEER-REVIEW]`, `[REPORT-GEN]`. While useful for debugging, this adds noise and performance overhead.

**Impact**: Log pollution; performance overhead in production.

**Recommended Fix**: Consider using LoggerMessage source generators for performance:

```csharp
public static partial class ResearchLoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "[RESEARCH] Starting phase {Phase} for session {SessionId}")]
    public static partial void LogPhaseStart(this ILogger logger, ResearchPhase phase, Guid sessionId);
}
```

---

### MED-06: PeerReviewService Uses Sequential Execution Despite Parallel Capability
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/PeerReviewService.cs`
**Lines**: 98-138

**Problem**: The `ConductPeerReviewsAsync` method executes reviewers sequentially with the comment "to avoid overloading the inference engine", but `ResearchPhaseExecutor.ExecuteResearchPhaseAsync` uses parallel execution with throttling.

**Impact**: Inconsistent execution patterns; peer review phase is slower than necessary.

**Recommended Fix**: Use the same parallel execution strategy as research phase:

```csharp
// Use the existing ParallelPhaseExecutionStrategy with maxParallelism=2
var strategy = _strategyFactory.CreatePeerReviewStrategy();
```

---

### MED-07: ReportGeneratorService Broadcasts ResearchCompleted Twice
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/ReportGeneratorService.cs`
**Line**: 178

**Problem**: `ReportGeneratorService.GenerateReportAsync` calls `_hubContext.SendResearchCompletedAsync()`, but so does `ResearchOrchestrator.ExecuteResearchWorkflowAsync` (line 544).

**Impact**: Clients receive duplicate completion events.

**Recommended Fix**: Remove the broadcast from ReportGeneratorService; let orchestrator handle all lifecycle events.

---

### MED-08: Feature Envy - ExtractAssistantResponse Repeated
**Files**: `ResearchPhaseExecutor.cs` (Line 334), `ReportGeneratorService.cs` (Line 279)

**Problem**: The same helper method `ExtractAssistantResponse` is defined as a static method in two files.

**Recommended Fix**: Move to a shared extension or utility class:

```csharp
public static class AesirChatResultExtensions
{
    public static string GetAssistantContent(this AesirChatResult result)
    {
        return result.AesirConversation?.Messages?
            .LastOrDefault(m => m.Role == "assistant")?.Content ?? string.Empty;
    }
}
```

---

### MED-09: Inconsistent Error Handling in Parallel Execution
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Execution/ParallelPhaseExecutionStrategy.cs`
**Lines**: 145-160

**Problem**: In `ExecutePhaseWithAgentTrackingAsync`, the catch block logs the warning and rethrows, but the caller handles errors differently in different contexts.

**Impact**: Inconsistent error propagation.

**Recommended Fix**: Standardize error handling with a result type:

```csharp
public class PhaseResult<T>
{
    public bool Success { get; }
    public T? Value { get; }
    public Exception? Error { get; }
    public static PhaseResult<T> FromSuccess(T value) => new(true, value, null);
    public static PhaseResult<T> FromError(Exception ex) => new(false, default, ex);
}
```

---

### MED-10: ResearchHub Has No Authorization
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Hubs/ResearchHub.cs`
**Lines**: 37-47

**Problem**: The `SubscribeToSession` method accepts any session ID without verifying the user owns that session.

**Impact**: Users could subscribe to other users' research sessions.

**Recommended Fix**: Add authorization:

```csharp
public async Task SubscribeToSession(Guid sessionId)
{
    var userId = Context.UserIdentifier;
    var session = await _sessionRepository.GetByIdAsync(sessionId);

    if (session == null || session.UserId != userId)
    {
        await Clients.Caller.SendAsync("Error", "Not authorized to subscribe to this session");
        return;
    }

    await Groups.AddToGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionId));
}
```

---

### MED-11: Anonymization Shuffle Not Cryptographically Secure
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/AnonymizationService.cs`
**Line**: 110

**Problem**: `OrderBy(_ => Guid.NewGuid())` is used for shuffling, which is not guaranteed to produce uniform distribution.

**Impact**: Minor - anonymization order might be predictable.

**Recommended Fix**: Use Fisher-Yates shuffle or `Random.Shared.Shuffle()` in .NET 8+:

```csharp
var shuffled = submissions.ToList();
Random.Shared.Shuffle(CollectionsMarshal.AsSpan(shuffled));
```

---

### MED-12: ParseQuestionsFromResponse Uses Hardcoded Number Detection
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/ClarificationService.cs`
**Lines**: 541-546

**Problem**: The fallback parsing only checks for patterns starting with `1.`, `2.`, `3.`, `4.` - will miss questions 5+.

**Code**:
```csharp
if (trimmed.StartsWith("1.") || trimmed.StartsWith("2.") ||
    trimmed.StartsWith("3.") || trimmed.StartsWith("4.") ||
    trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
```

**Recommended Fix**: Use regex:

```csharp
if (Regex.IsMatch(trimmed, @"^\d+\.\s+") ||
    trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
```

---

## Low Priority Findings

### LOW-01: Commented Debug Code Style
**File**: Multiple files

**Problem**: Extensive `_logger.LogDebug` statements with `[RESEARCH]`, `[PHASE-EXEC]` prefixes suggest these were added for debugging and could be cleaned up.

**Recommended Fix**: Review and consolidate logging; consider using structured logging consistently.

---

### LOW-02: Inconsistent String Interpolation in Prompts
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/ChairmanPlanningService.cs`
**Lines**: 87-117

**Problem**: Uses `$$"""` raw string with `{{variable}}` interpolation, which is correct but different from other prompt templates that use `$"""`.

**Recommended Fix**: Standardize on one approach across all prompt templates.

---

### LOW-03: Unused IResearchTrailService Registration
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/ResearchModule.cs`
**Line**: 61

**Problem**: `IResearchTrailService` is registered but I did not find it used in the orchestration flow.

**Recommended Fix**: Verify if trail service is needed; remove if unused.

---

### LOW-04: SubmissionScore.CriterionAverages Could Be Read-Only
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Services/ScoringCalculator.cs`
**Line**: 102

**Problem**: `CriterionAverages` is a `Dictionary<string, double>` with a mutable default value.

**Recommended Fix**: Use `IReadOnlyDictionary<string, double>`:

```csharp
public IReadOnlyDictionary<string, double> CriterionAverages { get; set; } =
    new Dictionary<string, double>();
```

---

### LOW-05: ResearchAgent Class Could Use Records
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Agents/ResearchAgentFactory.cs`
**Lines**: 206-278

**Problem**: `ResearchAgent` is a mutable class with many properties that could benefit from record semantics.

**Recommended Fix**: Convert to record for immutability:

```csharp
public record ResearchAgent(
    Guid TeamMemberId,
    ResearchRole Role,
    string RoleName,
    Guid BaseAgentId,
    Guid? InferenceEngineId,
    string? Model,
    double Temperature,
    int? MaxTokens,
    string Persona,
    string? PlanningPrompt,
    string? ResearchPrompt,
    string? ClarificationPrompt,
    string? SynthesisPrompt)
{
    public bool IsChairman => Role == ResearchRole.Chairman;
}
```

---

### LOW-06: MaxDegreeOfParallelism Hardcoded to 2
**File**: `/Users/ooartist/Src/Aesir/Server/Modules/Aesir.Modules.Research/Execution/PhaseExecutionStrategyFactory.cs`
**Line**: 58

**Problem**: The parallelism is hardcoded to 2 with comment "Fixed: 2 concurrent tasks".

**Recommended Fix**: Make configurable via `ResearchModuleOptions`:

```csharp
public class ResearchModuleOptions
{
    public int MaxParallelAgents { get; set; } = 2;
}
```

---

## Recommended Action Plan

### Phase 1: Critical Fixes (Priority: Immediate)
1. **CRIT-01**: Fix CancellationToken handling in fire-and-forget tasks
2. **CRIT-02**: Make AnonymizationService stateless or thread-safe
3. **CRIT-03**: Refactor ResearchProgressBroadcaster to pass session ID explicitly

### Phase 2: Code Deduplication (Priority: High)
1. **HIGH-01, HIGH-02**: Create shared `IChatServiceResolver` and `IChatRequestBuilder`
2. **HIGH-08**: Add missing `ConfigureAwait(false)` calls
3. **MED-08**: Extract `ExtractAssistantResponse` to shared extension

### Phase 3: Architecture Improvements (Priority: Medium)
1. **HIGH-04**: Refactor `ExecuteResearchWorkflowAsync` into phase handlers
2. **MED-01**: Consider splitting `ResearchPhaseExecutor` into phase-specific executors
3. **MED-06**: Use parallel execution strategy for peer review phase

### Phase 4: Configuration & Constants (Priority: Medium)
1. **HIGH-03, HIGH-06**: Create `ResearchModuleOptions` for configurable values
2. **LOW-06**: Make parallelism configurable

### Phase 5: Error Handling & Logging (Priority: Low)
1. **HIGH-05**: Add structured error tracking for silent failures
2. **MED-05**: Consider LoggerMessage source generators for performance
3. **LOW-01**: Clean up debug logging

### Phase 6: Security & Validation (Priority: Medium)
1. **MED-10**: Add authorization to ResearchHub.SubscribeToSession
2. **HIGH-07**: Improve Chairman plan parsing validation

---

## Notes

### Patterns Observed
1. **Good**: Consistent use of async/await with ConfigureAwait(false) in most places
2. **Good**: Well-structured interface segregation (each service has its interface)
3. **Good**: Comprehensive logging for debugging research flows
4. **Concern**: Heavy reliance on IServiceProvider for resolving keyed services (service locator pattern)
5. **Concern**: Scoped services storing instance state creates fragile scope dependencies

### Architecture Recommendations
1. Consider using MediatR or similar for phase orchestration to reduce coupling
2. Consider event-driven architecture for progress broadcasting to decouple from orchestrator
3. Consider using a state machine (Stateless library) for research session lifecycle management

### Testing Recommendations
1. Current architecture makes unit testing difficult due to many dependencies
2. Extract pure business logic (scoring, parsing) from I/O-dependent code
3. Add integration tests for the complete research workflow

---

**Document Status**: Complete
**Last Updated**: 2026-01-05
**Author**: Code Quality Review (Claude)
