# VA-012: Implement IVisualAgentService Client

**Epic**: VISUAL_AGENT_UX
**Phase**: 4 - Visual Agent Integration
**Priority**: High
**Estimate**: 6 hours

## Description
Create the client service for communicating with the AESIR API Server's visual agent endpoints, including HTTP requests and real-time streaming.

## Acceptance Criteria
- [ ] `IVisualAgentService` interface defined
- [ ] `VisualAgentService` implementation with HttpClient
- [ ] Methods: `AnalyzeVideoAsync()`, `GetStreamAnalyticsAsync()`, `ConfigureAgentAsync()`, `GetVideoSourcesAsync()`
- [ ] Error handling and retry logic
- [ ] Request/response serialization
- [ ] Integration with existing AESIR API infrastructure

## Technical Details
```csharp
public interface IVisualAgentService
{
    Task<VisualAgentResult> AnalyzeVideoAsync(
        VisualAgentRequest request,
        CancellationToken cancellationToken = default);

    Task<IDisposable> SubscribeToAnalyticsAsync(
        Guid agentId,
        Action<VisualAgentResult> onUpdate);

    Task<List<VideoSource>> GetVideoSourcesAsync();

    Task<VideoSource> CreateVideoSourceAsync(VideoSource source);

    Task UpdateAgentConfigurationAsync(
        Guid agentId,
        VisualAgentConfiguration config);
}

public class VisualAgentService : IVisualAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VisualAgentService> _logger;

    public async Task<VisualAgentResult> AnalyzeVideoAsync(
        VisualAgentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/visualagent/analyze",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisualAgentResult>();
    }

    // Additional implementations...
}
```

## Files to Create
- `Aesir.Client/Aesir.Client/Services/IVisualAgentService.cs`
- `Aesir.Client/Aesir.Client/Services/Implementations/Standard/VisualAgentService.cs`

## Dependencies
- DEEPSTREAM_VISUAL_AGENT.md (API contract)

## Testing
- [ ] HTTP requests succeed with valid data
- [ ] Error responses handled gracefully
- [ ] Retry logic works for transient failures
- [ ] Serialization/deserialization correct
- [ ] Integration test with API server