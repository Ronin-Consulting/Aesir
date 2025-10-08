# VA-013: Setup Real-time Analytics Stream (SignalR/WebSocket)

**Epic**: VISUAL_AGENT_UX
**Phase**: 4 - Visual Agent Integration
**Priority**: High
**Estimate**: 7 hours

## Description
Implement SignalR/WebSocket connection for receiving real-time analytics updates from the DeepStream backend, including detections, tracks, and events.

## Acceptance Criteria
- [ ] SignalR hub connection established
- [ ] Subscribe to visual agent analytics stream
- [ ] Receive real-time detection updates (30+ FPS)
- [ ] Handle connection interruptions and reconnection
- [ ] Backpressure handling for high-frequency updates
- [ ] Connection lifecycle management

## Technical Details
```csharp
public class VisualAgentHubConnection : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger _logger;

    public VisualAgentHubConnection(string hubUrl, ILogger logger)
    {
        _logger = logger;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<VisualAgentResult>(
            "AnalyticsUpdate",
            HandleAnalyticsUpdate);
    }

    public async Task StartAsync(Guid agentId)
    {
        await _hubConnection.StartAsync();
        await _hubConnection.InvokeAsync("SubscribeToAgent", agentId);
    }

    public async Task StopAsync()
    {
        await _hubConnection.InvokeAsync("UnsubscribeFromAgent");
        await _hubConnection.StopAsync();
    }

    private void HandleAnalyticsUpdate(VisualAgentResult result)
    {
        OnUpdate?.Invoke(result);
    }

    public event Action<VisualAgentResult>? OnUpdate;

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}
```

## Files to Create
- `Aesir.Client/Aesir.Client/Services/VisualAgentHubConnection.cs`

## Files to Modify
- `Aesir.Api.Server/Hubs/VisualAgentHub.cs` (create new hub)

## Dependencies
- VA-012 (IVisualAgentService)
- Microsoft.AspNetCore.SignalR.Client NuGet package

## Testing
- [ ] Connection establishes successfully
- [ ] Real-time updates received
- [ ] Reconnection works after disconnect
- [ ] Multiple clients supported
- [ ] Performance acceptable at 30 FPS
- [ ] Memory leaks prevented