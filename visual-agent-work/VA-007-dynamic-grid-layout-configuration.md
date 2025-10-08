# VA-007: Add Dynamic Grid Layout Configuration

**Epic**: VISUAL_AGENT_UX
**Phase**: 2 - Multi-Stream Support
**Priority**: Medium

## Description
Add UI controls for users to manually select grid layouts and configure stream positioning within the grid.

## Acceptance Criteria
- [ ] Layout selector (dropdown or button group) for 1x1, 2x2, 3x3, 4x4
- [ ] Visual feedback showing current layout
- [ ] Drag-and-drop to reorder streams (stretch goal)
- [ ] Keyboard shortcuts for layout switching
- [ ] Layout selection persists across sessions

## Dependencies
- VA-005, VA-006

## Testing
- [ ] Layout selector changes grid correctly
- [ ] Layout persists after restart
- [ ] Keyboard shortcuts work