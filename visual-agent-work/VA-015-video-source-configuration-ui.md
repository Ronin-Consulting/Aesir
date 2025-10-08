# VA-015: Create Video Source Configuration UI

**Epic**: VISUAL_AGENT_UX
**Phase**: 5 - Configuration & Management
**Priority**: Medium
**Estimate**: 5 hours

## Description
Create user interface for adding, editing, and managing video sources (RTSP streams, video files) with validation and testing capabilities.

## Acceptance Criteria
- [ ] Add/Edit video source dialog
- [ ] Fields: Name, Type (RTSP/File), URL/Path, Description
- [ ] RTSP URL validation
- [ ] Test connection button
- [ ] Video source list view with CRUD operations
- [ ] Save/Load video source configurations
- [ ] Import/Export configurations (JSON)

## UI Components
- Video source list (DataGrid)
- Add/Edit dialog with form validation
- Test connection with progress indicator
- Delete confirmation dialog

## Files to Create
- `Aesir.Client/Aesir.Client/Views/VideoSourceConfigView.axaml`
- `Aesir.Client/Aesir.Client/ViewModels/VideoSourceConfigViewModel.cs`
- `Aesir.Client/Aesir.Client/Controls/VideoSourceDialog.axaml`

## Dependencies
- VA-012 (IVisualAgentService)

## Testing
- [ ] Add new video source
- [ ] Edit existing source
- [ ] Delete video source
- [ ] Test connection validates RTSP stream
- [ ] Validation prevents invalid URLs
- [ ] Import/Export preserves data