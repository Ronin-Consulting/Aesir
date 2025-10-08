# Visual Agent Work - Ticket Index

This directory contains the epic and all individual tickets for implementing the Visual Agent UX feature in AESIR.

## Epic

- **[VISUAL_AGENT_UX.md](VISUAL_AGENT_UX.md)** - Main epic document with architecture overview and goals

## Tickets by Phase

### Phase 1: Foundation (Tickets 1-4)
Core infrastructure for single-stream video playback

- [VA-001](VA-001-setup-libvlcsharp-dependencies.md) - Setup LibVLCSharp Dependencies (2h)
- [VA-002](VA-002-create-nativevideoplayercontrol-base.md) - Create NativeVideoPlayerControl Base Implementation (6h)
- [VA-003](VA-003-implement-platform-video-handles.md) - Implement Platform-Specific Video Handles (8h)
- [VA-004](VA-004-create-videoplayerviewmodel.md) - Create VideoPlayerViewModel (4h)

**Phase 1 Total: 20 hours**

### Phase 2: Multi-Stream Support (Tickets 5-7)
Grid layout system for multiple concurrent streams

- [VA-005](VA-005-create-videostreamgrid-control.md) - Create VideoStreamGrid Control (5h)
- [VA-006](VA-006-implement-videostreamgridviewmodel.md) - Implement VideoStreamGridViewModel (4h)
- [VA-007](VA-007-dynamic-grid-layout-configuration.md) - Add Dynamic Grid Layout Configuration (3h)

**Phase 2 Total: 12 hours**

### Phase 3: Detection Overlay (Tickets 8-10)
Visual rendering of object detection and tracking data

- [VA-008](VA-008-create-boundingboxcanvas-control.md) - Create BoundingBoxCanvas Control (6h)
- [VA-009](VA-009-detection-rendering-logic.md) - Implement Detection Rendering Logic (5h)
- [VA-010](VA-010-coordinate-transformation-system.md) - Add Coordinate Transformation System (4h)

**Phase 3 Total: 15 hours**

### Phase 4: Visual Agent Integration (Tickets 11-14)
Connect to DeepStream backend and handle real-time analytics

- [VA-011](VA-011-create-visualagentviewmodel.md) - Create VisualAgentViewModel (5h)
- [VA-012](VA-012-implement-visualagentservice-client.md) - Implement IVisualAgentService Client (6h)
- [VA-013](VA-013-setup-realtime-analytics-stream.md) - Setup Real-time Analytics Stream (7h)
- [VA-014](VA-014-integrate-mock-visualagent-service.md) - Integrate Mock Visual Agent Service (3h)

**Phase 4 Total: 21 hours**

### Phase 5: Configuration & Management (Tickets 15-18)
User interface for configuring visual agents and video sources

- [VA-015](VA-015-video-source-configuration-ui.md) - Create Video Source Configuration UI (5h)
- [VA-016](VA-016-roi-drawing-tool.md) - Implement ROI Drawing Tool (8h)
- [VA-017](VA-017-model-selection-interface.md) - Add Model Selection Interface (4h)
- [VA-018](VA-018-alert-rule-configuration.md) - Create Alert Rule Configuration (6h)

**Phase 5 Total: 23 hours**

### Phase 6: Analytics Dashboard (Tickets 19-21)
Visualization of detection events and performance metrics

- [VA-019](VA-019-analytics-dashboard-view.md) - Create Analytics Dashboard View (7h)
- [VA-020](VA-020-event-timeline-visualization.md) - Implement Event Timeline Visualization (5h)
- [VA-021](VA-021-performance-metrics-display.md) - Add Performance Metrics Display (4h)

**Phase 6 Total: 16 hours**

### Phase 7: Testing & Polish (Tickets 22-24)
Quality assurance and optimization

- [VA-022](VA-022-performance-testing-multistream.md) - Performance Testing (Multi-stream Load) (6h)
- [VA-023](VA-023-memory-leak-testing-optimization.md) - Memory Leak Testing & Optimization (6h)
- [VA-024](VA-024-cross-platform-compatibility-testing.md) - Cross-Platform Compatibility Testing (8h)

**Phase 7 Total: 20 hours**

## Summary

**Total Tickets**: 24
**Total Estimated Hours**: 127 hours
**Estimated Duration**: 16-20 working days (assuming 6-8 hours/day)

## Dependencies

### External Packages
- LibVLCSharp (v3.8.5+)
- VideoLAN.LibVLC platform packages
- CommunityToolkit.Mvvm
- Material.Icons.Avalonia
- Microsoft.AspNetCore.SignalR.Client

### Related Documentation
- [DEEPSTREAM_VISUAL_AGENT.md](../DEEPSTREAM_VISUAL_AGENT.md) - DeepStream integration guide
- [VIDEO_STREAMING.md](../VIDEO_STREAMING.md) - Video streaming technical notes

## Getting Started

1. Review the [VISUAL_AGENT_UX.md](VISUAL_AGENT_UX.md) epic document
2. Start with Phase 1 tickets (VA-001 through VA-004)
3. Follow the dependencies listed in each ticket
4. Test each component before moving to the next phase
5. Use FFmpeg for generating test RTSP streams
6. Integrate with mock service before real DeepStream hardware

## Progress Tracking

Create a tracking board (GitHub Projects, Jira, etc.) with columns:
- Backlog
- In Progress
- In Review
- Done

Mark tickets as you complete them and update estimates based on actual time spent.

## Notes

- Each ticket is designed to be independently testable
- Phases can be partially parallelized if multiple developers work on the epic
- Mock service enables development without DeepStream hardware
- Cross-platform testing should be done continuously, not just in Phase 7