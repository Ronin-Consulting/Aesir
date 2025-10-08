# Visual Agent Work - Ticket Index

This directory contains the epic and all individual tickets for implementing the Visual Agent UX feature in AESIR.

## Epic

- **[VISUAL_AGENT_UX.md](VISUAL_AGENT_UX.md)** - Main epic document with architecture overview and goals

## Tickets by Phase

### Phase 1: Foundation (Tickets 1-4)
Core infrastructure for single-stream video playback

- [VA-001](VA-001-setup-libvlcsharp-dependencies.md) - Setup LibVLCSharp Dependencies
- [VA-002](VA-002-create-nativevideoplayercontrol-base.md) - Create NativeVideoPlayerControl Base Implementation
- [VA-003](VA-003-implement-platform-video-handles.md) - Implement Platform-Specific Video Handles
- [VA-004](VA-004-create-videoplayerviewmodel.md) - Create VideoPlayerViewModel

### Phase 2: Multi-Stream Support (Tickets 5-7)
Grid layout system for multiple concurrent streams

- [VA-005](VA-005-create-videostreamgrid-control.md) - Create VideoStreamGrid Control
- [VA-006](VA-006-implement-videostreamgridviewmodel.md) - Implement VideoStreamGridViewModel
- [VA-007](VA-007-dynamic-grid-layout-configuration.md) - Add Dynamic Grid Layout Configuration

### Phase 3: Detection Overlay (Tickets 8-10)
Visual rendering of object detection and tracking data

- [VA-008](VA-008-create-boundingboxcanvas-control.md) - Create BoundingBoxCanvas Control
- [VA-009](VA-009-detection-rendering-logic.md) - Implement Detection Rendering Logic
- [VA-010](VA-010-coordinate-transformation-system.md) - Add Coordinate Transformation System

### Phase 4: Visual Agent Integration (Tickets 11-14)
Connect to DeepStream backend and handle real-time analytics

- [VA-011](VA-011-create-visualagentviewmodel.md) - Create VisualAgentViewModel
- [VA-012](VA-012-implement-visualagentservice-client.md) - Implement IVisualAgentService Client
- [VA-013](VA-013-setup-realtime-analytics-stream.md) - Setup Real-time Analytics Stream
- [VA-014](VA-014-integrate-mock-visualagent-service.md) - Integrate Mock Visual Agent Service

### Phase 5: Configuration & Management (Tickets 15-18)
User interface for configuring visual agents and video sources

- [VA-015](VA-015-video-source-configuration-ui.md) - Create Video Source Configuration UI
- [VA-016](VA-016-roi-drawing-tool.md) - Implement ROI Drawing Tool
- [VA-017](VA-017-model-selection-interface.md) - Add Model Selection Interface
- [VA-018](VA-018-alert-rule-configuration.md) - Create Alert Rule Configuration

### Phase 6: Analytics Dashboard (Tickets 19-21)
Visualization of detection events and performance metrics

- [VA-019](VA-019-analytics-dashboard-view.md) - Create Analytics Dashboard View
- [VA-020](VA-020-event-timeline-visualization.md) - Implement Event Timeline Visualization
- [VA-021](VA-021-performance-metrics-display.md) - Add Performance Metrics Display

### Phase 7: Testing & Polish (Tickets 22-24)
Quality assurance and optimization

- [VA-022](VA-022-performance-testing-multistream.md) - Performance Testing (Multi-stream Load)
- [VA-023](VA-023-memory-leak-testing-optimization.md) - Memory Leak Testing & Optimization
- [VA-024](VA-024-cross-platform-compatibility-testing.md) - Cross-Platform Compatibility Testing

## Summary

**Total Tickets**: 24

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

Mark tickets as you complete them to track progress.

## Notes

- Each ticket is designed to be independently testable
- Phases can be partially parallelized if multiple developers work on the epic
- Mock service enables development without DeepStream hardware
- Cross-platform testing should be done continuously, not just in Phase 7