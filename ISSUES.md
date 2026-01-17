# AESIR Issues

Tracking issues and improvements for the AESIR project.

---

## Issue #1: Research Team Query Observability Logging

**Priority**: Medium
**Module**: Research
**Status**: Open

### Description

When running a research team query, we need to ensure proper logging is implemented for observability purposes. Currently, research team operations may not have sufficient logging coverage to diagnose issues or monitor performance.

### Requirements

- Add structured logging throughout the research team execution flow
- Log key events: phase transitions, agent invocations, tool calls, completion status
- Include correlation IDs for tracing across research phases
- Ensure logs are visible in the Observability module dashboard
- Log timing/duration for performance monitoring

### Affected Areas

- `Aesir.Modules.Research` - Research orchestration and agent execution
- `Aesir.Client.Web.Modules.Observability` - Log viewing and filtering

---
