# VA-014: Integrate Mock Visual Agent Service

**Epic**: VISUAL_AGENT_UX
**Phase**: 4 - Visual Agent Integration
**Priority**: High
**Estimate**: 3 hours

## Description
Connect the client UI to the mock visual agent service (from DEEPSTREAM_VISUAL_AGENT.md) to enable end-to-end testing without physical hardware.

## Acceptance Criteria
- [ ] Client configured to use mock service
- [ ] Synthetic detection data displays in UI
- [ ] Real-time stream simulation works
- [ ] Detections render on video overlay
- [ ] Event history populates
- [ ] Performance metrics display
- [ ] Switch between mock and real service via configuration

## Technical Details
```csharp
// In App.axaml.cs or DI setup
services.AddTransient<IVisualAgentService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var useMock = config.GetValue<bool>("VisualAgent:UseMock");

    if (useMock)
        return new MockVisualAgentService(sp.GetRequiredService<ILogger<MockVisualAgentService>>());
    else
        return new VisualAgentService(/* real implementation */);
});
```

## Configuration (appsettings.json)
```json
{
  "VisualAgent": {
    "UseMock": true,
    "MockUpdateFrequency": 33,
    "MockDetectionCount": 5
  }
}
```

## Files to Modify
- `Aesir.Client/Aesir.Client.Desktop/App.axaml.cs` (DI registration)
- `Aesir.Client/Aesir.Client.Desktop/appsettings.json` (configuration)

## Dependencies
- VA-011 (VisualAgentViewModel)
- VA-012 (IVisualAgentService)
- DEEPSTREAM_VISUAL_AGENT.md (MockVisualAgentService implementation)

## Testing
- [ ] Mock service returns synthetic data
- [ ] UI updates with mock detections
- [ ] Real-time simulation runs at 30 FPS
- [ ] Can switch to real service via config
- [ ] No errors with mock data