# VA-018: Create Alert Rule Configuration

**Epic**: VISUAL_AGENT_UX
**Phase**: 5 - Configuration & Management
**Priority**: Medium

## Description
Create interface for configuring alert rules based on detection events, including zone intrusion, object counting, loitering detection, and custom conditions.

## Acceptance Criteria
- [ ] Alert rule builder UI
- [ ] Rule types: Zone Intrusion, Counting, Loitering, Object Left/Removed
- [ ] Condition configuration (class filter, time thresholds, count limits)
- [ ] Alert action configuration (notification, logging, webhook)
- [ ] Enable/Disable rules
- [ ] Rule priority/ordering
- [ ] Test rule with sample data

## Alert Rule Types
- **Zone Intrusion**: Trigger when object enters/exits ROI
- **Object Counting**: Alert when count exceeds threshold
- **Loitering**: Alert when object stays in zone for duration
- **Object Left**: Detect abandoned objects
- **Object Removed**: Detect object removal

## Files to Create
- `Aesir.Client/Aesir.Client/Views/AlertRuleConfigView.axaml`
- `Aesir.Client/Aesir.Client/ViewModels/AlertRuleConfigViewModel.cs`
- `Aesir.Client/Aesir.Client/Models/AlertRule.cs`

## Dependencies
- VA-016 (ROI Drawing Tool)

## Testing
- [ ] Create new alert rule
- [ ] Edit existing rule
- [ ] Delete rule
- [ ] Enable/disable rule
- [ ] Test rule with mock data
- [ ] Rules save to agent configuration