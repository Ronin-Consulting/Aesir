# Testing Plan: Tool Call Surfacing (Release 11)

> **Feature:** Real-time AI Tool Usage Display
> **Status:** Ready for Manual Testing

## Prerequisites

1. **Start the API server** (via Docker):
   ```bash
   docker-compose -f docker-compose-dev.yml up -d
   ```

2. **Start the Blazor client**:
   ```bash
   cd Client/Aesir.Client.Web/Aesir.Client.Web.App
   dotnet watch run --urls "http://localhost:5173"
   ```

3. **Verify API is accessible** at `https://aesir.localhost`

---

## Test Scenarios

### Test 1: Document Search Tool Calls

**Setup:** Upload a document to a conversation first, then ask a question about it.

**Steps:**
1. Go to the Chat page
2. Upload a PDF or text document to the conversation
3. Ask: "What does this document say about [topic from doc]?"

**Expected Results:**
- [ ] A purple "Using X tools..." section appears during streaming
- [ ] A green Document Search card appears with:
  - Search icon
  - Function name (e.g., "Hybrid Document Search" or "Semantic Document Search")
  - Spinning progress indicator while running
  - Green checkmark when complete
  - Duration displayed (e.g., "245ms" or "1.2s")
- [ ] Expanding the card shows:
  - Query argument with the search terms
  - Result preview with matched document content
- [ ] Section summary updates to "Used X tools" when complete

---

### Test 2: Web Search Tool Calls

**Setup:** Ensure web search is enabled for the conversation/agent.

**Steps:**
1. Go to the Chat page
2. Select an agent with web search capability
3. Ask: "What are the latest news about [current event]?"

**Expected Results:**
- [ ] A blue Web Search card appears with:
  - Globe/language icon
  - Function name
  - Progress indicator while searching
  - Duration when complete
- [ ] Expanding shows search query and results summary

---

### Test 3: MCP Server Tool Calls

**Setup:** Have an MCP server configured with available tools.

**Steps:**
1. Go to the Chat page
2. Select an agent with MCP tools enabled
3. Trigger an action that uses an MCP tool

**Expected Results:**
- [ ] An orange MCP card appears with:
  - Extension/plugin icon
  - Tool name and plugin badge
  - Input arguments displayed
  - Output result when complete

---

### Test 4: Multiple Tool Calls

**Steps:**
1. Ask a complex question that triggers multiple tools
2. Example: "Search my documents for project requirements and also look up current best practices online"

**Expected Results:**
- [ ] Multiple tool cards appear in the section
- [ ] Summary shows correct count (e.g., "Using 2 tools...")
- [ ] Each tool has its distinct color based on type
- [ ] All tools show individual status and timing
- [ ] Summary updates when all complete (e.g., "Used 2 tools")

---

### Test 5: Real-time Streaming Behavior

**Steps:**
1. Trigger any tool call
2. Watch the UI during the streaming response

**Expected Results:**
- [ ] Tool calls section auto-expands when tools become active
- [ ] Tool cards appear immediately when tool starts (not after completion)
- [ ] Progress spinners animate while tools are running
- [ ] Cards show pulse animation while active
- [ ] Text response continues to stream while tools are displayed
- [ ] Section can be manually collapsed during streaming

---

### Test 6: Expand/Collapse Behavior

**Steps:**
1. Complete a chat that used tools
2. Test the expand/collapse controls

**Expected Results:**
- [ ] Clicking the section header toggles expand/collapse
- [ ] Clicking individual card headers expands card details
- [ ] Failed or in-progress cards are auto-expanded
- [ ] Completed cards start collapsed (details hidden)
- [ ] Chevron icons rotate appropriately

---

### Test 7: Error Handling

**Setup:** This may require simulating a tool failure (e.g., network issue, invalid tool input).

**Steps:**
1. Trigger a tool call that fails
2. Observe the error display

**Expected Results:**
- [ ] Card shows red error icon
- [ ] Card has red border accent
- [ ] Summary shows failure count (e.g., "Used 3 tools (2 completed, 1 failed)")
- [ ] Expanding card shows error message
- [ ] UI remains stable, no crashes

---

### Test 8: Timing and Duration Display

**Steps:**
1. Trigger various tool calls
2. Observe the timing display

**Expected Results:**
- [ ] Duration appears after tool completes
- [ ] Short durations show milliseconds (e.g., "125ms")
- [ ] Long durations show seconds (e.g., "2.3s")
- [ ] Duration is accurate (matches actual execution time)

---

### Test 9: Arguments Display

**Steps:**
1. Trigger a tool with complex arguments
2. Expand the card to see arguments

**Expected Results:**
- [ ] Arguments section shows all input parameters
- [ ] Key-value pairs are formatted clearly
- [ ] Long values are truncated with "..."
- [ ] Monospace font for code-like values

---

### Test 10: Result Preview

**Steps:**
1. Complete a tool call with substantial output
2. Expand the card to see results

**Expected Results:**
- [ ] Result shows in a scrollable area
- [ ] Long results are truncated (max 500 chars from server)
- [ ] Result area has max height with scroll
- [ ] Whitespace/formatting preserved

---

## Visual Verification

### Color Coding
Verify each tool type displays with correct colors:

| Tool Type | Background | Icon Color |
|-----------|------------|------------|
| Document Search | Green tint | #4CAF50 |
| Web Search | Blue tint | #2196F3 |
| MCP Server | Orange tint | #FF9800 |
| Image Analysis | Pink tint | #E91E63 |
| Summarization | Purple tint | #9C27B0 |
| Other | Gray tint | #607D8B |

### Status Icons
- [ ] Started: Spinning circular progress
- [ ] Completed: Green checkmark
- [ ] Failed: Red error circle

### Animations
- [ ] Pulse animation on active (started) cards
- [ ] Fade-in animation when section expands
- [ ] Slide-down animation for card details

---

## Edge Cases

### No Tool Calls
- [ ] When AI responds without using tools, no tool section appears

### Tool Calls with Empty Results
- [ ] Cards handle tools that return empty/null results gracefully

### Rapid Tool Calls
- [ ] Multiple tools called in quick succession display correctly
- [ ] No race conditions or duplicate entries

### Long Function Names
- [ ] Function names truncate with ellipsis if too long
- [ ] Tooltip or expansion shows full name

---

## Performance

- [ ] Tool call UI does not cause noticeable lag during streaming
- [ ] Animations are smooth (60fps)
- [ ] Memory usage remains stable with many tool calls

---

## Browser Compatibility

Test in:
- [ ] Chrome/Edge (Chromium)
- [ ] Firefox
- [ ] Safari (if applicable)

---

## Notes

Record any issues found:

| Issue | Severity | Description | Steps to Reproduce |
|-------|----------|-------------|-------------------|
| | | | |
| | | | |
| | | | |

---

## Sign-off

- [ ] All test scenarios passed
- [ ] No critical issues found
- [ ] Feature ready for release

Tested by: _________________ Date: _________________
