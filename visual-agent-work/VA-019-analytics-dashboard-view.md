# VA-019: Create Analytics Dashboard View

**Epic**: VISUAL_AGENT_UX
**Phase**: 6 - Analytics Dashboard
**Priority**: Medium

## Description
Create a comprehensive analytics dashboard showing real-time metrics, detection statistics, event history, and performance indicators.

## Acceptance Criteria
- [ ] Real-time detection count by class
- [ ] Active track count
- [ ] FPS and latency metrics
- [ ] Event timeline visualization
- [ ] Detection heatmap (stretch goal)
- [ ] Historical data charts
- [ ] Export analytics data (CSV/JSON)

## Dashboard Sections
1. **Live Metrics**
   - Current FPS
   - Latency (ms)
   - Active objects
   - Detections per second

2. **Detection Statistics**
   - Count by class (pie/bar chart)
   - Detection confidence distribution
   - Track duration histogram

3. **Event Timeline**
   - Chronological event list
   - Event type filtering
   - Event details on click

4. **Performance Monitors**
   - CPU/GPU usage (if available)
   - Network bandwidth
   - Memory usage

## Files to Create
- `Aesir.Client/Aesir.Client/Views/AnalyticsDashboardView.axaml`
- `Aesir.Client/Aesir.Client/ViewModels/AnalyticsDashboardViewModel.cs`
- `Aesir.Client/Aesir.Client/Controls/MetricCard.axaml`
- `Aesir.Client/Aesir.Client/Controls/EventTimeline.axaml`

## Dependencies
- VA-011 (VisualAgentViewModel)
- LiveChartsCore (for charts - optional)

## Testing
- [ ] Metrics update in real-time
- [ ] Charts render correctly
- [ ] Event timeline scrolls/filters
- [ ] Export data successfully
- [ ] Dashboard performs well with large datasets