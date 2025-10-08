# VA-020: Implement Event Timeline Visualization

**Epic**: VISUAL_AGENT_UX
**Phase**: 6 - Analytics Dashboard
**Priority**: Medium

## Description
Create an interactive timeline control for visualizing analytics events over time, with filtering, search, and drill-down capabilities.

## Acceptance Criteria
- [ ] Timeline displays events chronologically
- [ ] Color-coded event types
- [ ] Filter by event type, time range, zone
- [ ] Search events
- [ ] Click event for details
- [ ] Auto-scroll to latest events
- [ ] Pagination for historical events
- [ ] Export filtered events

## Visual Design
- Vertical timeline with event markers
- Event icons for different types
- Timestamp labels
- Expandable event details
- Severity indicators (info/warning/alert)

## Files to Create
- `Aesir.Client/Aesir.Client/Controls/EventTimeline.axaml`
- `Aesir.Client/Aesir.Client/ViewModels/EventTimelineViewModel.cs`
- `Aesir.Client/Aesir.Client/Controls/EventCard.axaml`

## Dependencies
- VA-019 (Analytics Dashboard)

## Testing
- [ ] Events display in order
- [ ] Filtering works correctly
- [ ] Search finds events
- [ ] Details show on click
- [ ] Pagination loads more events
- [ ] Auto-scroll toggles correctly