# VA-009: Implement Detection Rendering Logic

**Epic**: VISUAL_AGENT_UX
**Phase**: 3 - Detection Overlay
**Priority**: High
**Estimate**: 5 hours

## Description
Implement rendering logic for various detection types including bounding boxes, track trajectories, zone overlays, and confidence scores.

## Acceptance Criteria
- [ ] Render bounding boxes with class-specific colors
- [ ] Display confidence scores as percentages
- [ ] Draw track trajectories (lines connecting past positions)
- [ ] Render ROI/zone boundaries
- [ ] Add fade-out effect for old tracks
- [ ] Configurable rendering options (show/hide labels, trajectories, etc.)

## Technical Features
- Color palette for object classes
- Anti-aliasing for smooth edges
- Text shadow for label readability
- Track history length configuration (last N frames)
- Zone violation highlighting (red flash)

## Files to Create
- `Aesir.Client/Aesir.Client/Services/DetectionRendererService.cs`
- `Aesir.Client/Aesir.Client/Models/RenderOptions.cs`

## Dependencies
- VA-008 (BoundingBoxCanvas)

## Testing
- [ ] Multiple detection types render simultaneously
- [ ] Track trajectories animate smoothly
- [ ] Zone violations highlight correctly
- [ ] Rendering options toggle work
- [ ] Performance with 100+ objects acceptable