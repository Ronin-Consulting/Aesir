# WORK_PLAN_RELEASE_5.md

> **STATUS: ✅ CLOSED** - Completed 2025-12-04
>
> All success criteria met. Chat functionality working, tests passing, Docker build verified, Tauri desktop tested.

Work items for fixing chat functionality issues in the ÆSIR Blazor WebAssembly client.

## Overview

The chat functionality in the Blazor WebAssembly client is currently broken due to API issues between the client and server. Users cannot send messages to agents (400 Bad Request) and chat history fails to load (500 Internal Server Error). This release focuses on debugging and fixing the communication issues to enable end-to-end chat functionality.

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Investigation Approach | Server-first | Server logs will reveal exact validation errors |
| Request Format | Align client models with server expectations | Ensure DTOs match between client and server |

## Legend

- [ ] Not started
- [x] Completed
- [~] Skipped (with reason in comments)

---

## Sprint Plan

**Sprint 1: Investigation & Debugging**
- 1.1, 1.2 (Epic 1 - Root Cause Analysis)
- 2.1, 2.2 (Epic 2 - Server-Side Fixes)

**Sprint 2: Client Fixes & Verification**
- 3.1, 3.2 (Epic 3 - Client-Side Fixes)
- 4.1 (Epic 4 - End-to-End Testing)

---

## Epic 1: Root Cause Analysis

### 1.1 Investigate 400 Bad Request on Chat Streaming Endpoint
- [ ] Check server logs for detailed validation error on `POST /chat/completions/agent/streamed`
- [ ] Compare `AesirAgentChatRequestBase` model between client and server
- [ ] Verify JSON serialization format matches server expectations
- [ ] Identify missing or incorrectly typed fields in request payload
- [ ] Document exact cause of 400 error

### 1.2 Investigate 500 Internal Server Error on Chat History
- [ ] Check server logs for exception details on `GET /chat/history/user/{userId}`
- [ ] Verify ChatHistory controller and service implementation
- [ ] Check database schema and migrations for chat session tables
- [ ] Test endpoint directly via curl or Swagger
- [ ] Document exact cause of 500 error

---

## Epic 2: Server-Side Fixes

### 2.1 Fix Chat Streaming Endpoint
- [ ] Update request validation to provide clear error messages
- [ ] Fix any model binding issues in `ChatController`
- [ ] Ensure agent lookup works correctly
- [ ] Verify Ollama/LLM connection configuration
- [ ] Add logging for better debugging

### 2.2 Fix Chat History Endpoint
- [ ] Fix exception in history retrieval logic
- [ ] Ensure database queries work correctly
- [ ] Handle edge cases (no sessions, invalid user, etc.)
- [ ] Return proper empty collection if no history exists

### 2.3 Unit Tests for Server Fixes
- [ ] Test chat request validation with valid input
- [ ] Test chat request validation with missing fields
- [ ] Test history endpoint with existing sessions
- [ ] Test history endpoint with no sessions

---

## Epic 3: Client-Side Fixes

### 3.1 Fix Request Model Alignment
- [ ] Update `AesirAgentChatRequestBase` to match server model
- [ ] Ensure `AesirConversation` serializes correctly
- [ ] Verify `AgentId` is properly set (Guid? vs Guid)
- [ ] Check that all required fields are populated

### 3.2 Improve Error Handling in Chat UI
- [ ] Display specific error messages from server (not generic connection error)
- [ ] Handle 400/500 errors gracefully with user-friendly messages
- [ ] Add retry logic for transient failures
- [ ] Ensure UI recovers properly after errors

---

## Epic 4: End-to-End Testing

### 4.1 Verify Chat Functionality
- [ ] Test sending message with OpenAI OSS Agent
- [ ] Verify streaming response displays correctly
- [ ] Test conversation history persists
- [ ] Test loading existing chat sessions
- [ ] Test creating new chat after existing conversation
- [ ] Verify in both browser and Tauri desktop

---

## Work Item Dependencies

```
1.1 [Investigate 400] ─────┬── 2.1 [Fix Streaming] ───┬── 3.1 [Client Request Fix]
                           │                          │
1.2 [Investigate 500] ─────┴── 2.2 [Fix History] ─────┴── 4.1 [E2E Testing]
```

---

## Known Issues

| Endpoint | Error | Status |
|----------|-------|--------|
| `POST /chat/completions/agent/streamed` | 400 Bad Request | To investigate |
| `GET /chat/history/user/{userId}` | 500 Internal Server Error | To investigate |

---

## Success Criteria (Phase 1 - Chat Functionality)

- [x] Chat messages can be sent to agents without errors
- [x] Agent responses stream correctly to the UI
- [x] Chat history loads without errors
- [x] Existing sessions can be resumed
- [x] All tests passing (`dotnet test`) - 499 tests passing
- [x] Docker build successful
- [x] Works in both browser and Tauri desktop

## Additional Completed Items (This Session)

- [x] Normalized AESIR brand icon to blue (#54A9FF) throughout the app
- [x] Fixed 23 failing tests due to UI component changes
- [x] Updated test mocks and assertions for new component structure

---

## Phase 2: Chat UI Enhancement (Claude Desktop Style)

### Epic 5: Message Styling & Avatars
- [ ] Update UserMessage component with avatar showing user initials
- [ ] Style user messages with dark bubble like Claude
- [ ] Simplify assistant message styling
- [ ] Ensure responsive design for both styles

### Epic 6: Chat Title Bar
- [ ] Add title bar showing conversation title at top of chat
- [ ] Add dropdown menu with rename/delete/star options
- [ ] Implement rename functionality (update session title)
- [ ] Implement star/favorite functionality
- [ ] Connect delete to existing delete functionality

### Epic 7: Response Actions
- [ ] Add copy button to assistant messages
- [ ] Add retry button to assistant messages
- [ ] Implement copy-to-clipboard functionality
- [ ] Implement retry (regenerate response) functionality

### Epic 8: Collapsible Thinking Section
- [ ] Create expandable/collapsible thinking display
- [ ] Show summary text when collapsed (like "Marshaled established knowledge...")
- [ ] Expand to show full thinking content
- [ ] Animate expand/collapse transition

### Epic 9: Sidebar Chat History Menu
- [ ] Add three-dot menu icon on hover for history items
- [ ] Add rename option to menu
- [ ] Add delete option to menu
- [ ] Add star/favorite option to menu
- [ ] Implement inline rename functionality

### Epic 10: Agent Selector in Input Area
- [ ] Move agent selector from sidebar to input area
- [ ] Style like Claude's model selector
- [ ] Show current agent name with dropdown arrow
- [ ] Update conversation when agent changes

### Epic 11: AESIR Processing Animation
- [ ] Create animated AESIR geometric icon component
- [ ] Implement throbbing/spinning animation
- [ ] Add shimmering status text ("Thinking...", "Ruminating...", "Hibernating...")
- [ ] Implement left-to-right shimmer effect on text
- [ ] Vary animation speeds for organic feel
- [ ] Display during agent processing

### Epic 12: AESIR Branding
- [ ] Add AESIR icon at bottom of assistant responses
- [ ] Style consistently with Claude's asterisk placement
- [ ] Add "ÆSIR can make mistakes" disclaimer

---

## Test Commands

```bash
# Run all tests
dotnet test

# Test API directly
curl -X POST https://aesir.localhost/chat/completions/agent/streamed \
  -H "Content-Type: application/json" \
  -d '{"agentId": "...", "conversation": {...}}'

# Check chat history
curl https://aesir.localhost/chat/history/user/blangford%40gmail.com
```

---

## Resources

- Server Chat Controller: `Server/Modules/Aesir.Modules.Chat/Controllers/`
- Client Chat Service: `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Chat/Services/`
- Client Chat Page: `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Chat/Pages/ChatPage.razor`
