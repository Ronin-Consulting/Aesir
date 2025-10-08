# VA-016: Implement ROI Drawing Tool

**Epic**: VISUAL_AGENT_UX
**Phase**: 5 - Configuration & Management
**Priority**: Medium

## Description
Create an interactive tool for drawing Regions of Interest (ROI) on video streams, including polygon/rectangle drawing, editing, and zone configuration.

## Acceptance Criteria
- [ ] Draw polygon ROIs on video canvas
- [ ] Draw rectangle ROIs
- [ ] Edit ROI vertices (drag points)
- [ ] Delete ROI
- [ ] Name and configure ROI properties
- [ ] Associate alert rules with ROIs
- [ ] Save ROI configurations
- [ ] Visual feedback during drawing
- [ ] Snap-to-grid option

## Technical Details
- Mouse event handling for drawing
- Vertex manipulation for editing
- Serialization of polygon points
- Color coding for different ROI types
- Z-order management for overlapping ROIs

## Files to Create
- `Aesir.Client/Aesir.Client/Controls/VideoPlayer/ROIDrawingCanvas.cs`
- `Aesir.Client/Aesir.Client/ViewModels/ROIEditorViewModel.cs`
- `Aesir.Client/Aesir.Client/Models/RegionOfInterest.cs` (update)

## Dependencies
- VA-008 (BoundingBoxCanvas - similar rendering approach)

## Testing
- [ ] Draw polygon with multiple vertices
- [ ] Draw rectangle ROI
- [ ] Edit existing ROI vertices
- [ ] Delete ROI
- [ ] ROIs persist across sessions
- [ ] Coordinate accuracy maintained