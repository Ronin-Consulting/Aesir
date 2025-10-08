# VA-021: Add Performance Metrics Display

**Epic**: VISUAL_AGENT_UX
**Phase**: 6 - Analytics Dashboard
**Priority**: Medium

## Description
Create real-time performance monitoring displays showing FPS, latency, resource usage, and system health metrics.

## Acceptance Criteria
- [ ] Real-time FPS gauge per stream
- [ ] Latency graph (line chart)
- [ ] Resource usage indicators (CPU/Memory)
- [ ] Network bandwidth monitor
- [ ] Stream health status (green/yellow/red)
- [ ] Historical performance data
- [ ] Performance alerts (low FPS, high latency)

## Metrics to Track
- **Per Stream:**
  - FPS (frames per second)
  - Latency (end-to-end ms)
  - Bitrate (Mbps)
  - Dropped frames count

- **System:**
  - CPU usage (%)
  - Memory usage (MB)
  - Network bandwidth (Mbps)
  - DeepStream pipeline status

## Files to Create
- `Aesir.Client/Aesir.Client/Controls/PerformanceMetricsControl.axaml`
- `Aesir.Client/Aesir.Client/ViewModels/PerformanceMetricsViewModel.cs`
- `Aesir.Client/Aesir.Client/Services/PerformanceMonitorService.cs`

## Dependencies
- VA-019 (Analytics Dashboard)

## Testing
- [ ] Metrics update every second
- [ ] Gauges show correct values
- [ ] Graphs render smoothly
- [ ] Alerts trigger correctly
- [ ] Historical data accurate