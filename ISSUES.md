# Known Issues

---

## Issue #1: RAG search returns wrong document results when multiple files uploaded in conversation

**Date:** 2025-12-06

**Severity:** Medium

**Query/Input:** Upload multiple documents to a conversation, then ask about the second document.

**Symptom:**
When a user uploads multiple documents in a single conversation and asks about a document that was uploaded after the first one, the RAG search returns results from the first document instead of the requested document.

Example conversation flow:
1. User uploads `CompareDevinAI.pdf` and asks for summary - works correctly
2. User uploads `GenAI-Workplace.pdf` and asks about its main theme
3. AI responds: "I couldn't locate the file **GenAI-Workplace.pdf**"

The AI's thinking log reveals: *"The tool returned a bunch of results for CompareDevinAI.pdf, not GenAI-Workplace.pdf"*

**Expected Behavior:**
When a user asks about a specific uploaded document, the RAG search should return relevant chunks from that document, not from previously uploaded documents in the conversation.

**Root Cause:**
Suspected causes (needs investigation):
1. **Indexing timing**: The second document may not have finished indexing in Qdrant before the search is executed
2. **Search scope**: The RAG search may not be properly filtering by filename when searching conversation documents
3. **Vector similarity**: The search may be returning semantically similar content from other documents rather than exact filename matches
4. **Collection scoping**: Documents may not be properly scoped to the conversation collection

**Relevant Code/Data:**
- RAG Tool: `Server/Modules/Aesir.Modules.Documents/`
- Document search: `ConversationDocumentCollectionService.cs`
- Vector store: Qdrant collection `aesir_conversation_document`
- Conversation ID: `5152df10-0247-4488-a53f-1b522be105b9`

**Tool Call (for second document - failed):**
```json
{
  "Role": {
    "Label": "assistant"
  },
  "Items": [
    {
      "$type": "FunctionCallContent",
      "Id": "853428ab",
      "FunctionName": "ChatTools_PerformHybridDocumentSearch",
      "Arguments": {
        "files": [
          "GenAI-Workplace.pdf"
        ],
        "query": "main theme"
      }
    }
  ]
}
```
Note: The tool call correctly specifies `"files": ["GenAI-Workplace.pdf"]` but the search returned results from `CompareDevinAI.pdf` instead. This indicates the file filtering in the search is not working correctly.

**Impact:**
- Users cannot effectively use multiple documents in a single conversation
- Second/subsequent document uploads appear to be ignored
- Reduces the usefulness of RAG for multi-document research workflows

**Workaround:**
Start a new conversation for each document upload.

**Proposed Fix:**
1. Investigate if there's a race condition between upload completion and search execution
2. Add filename filtering to the RAG search when user query contains `<file>` tags
3. Consider adding a "document ready" indicator before allowing queries
4. Verify Qdrant metadata includes filename and conversation ID for proper filtering

**Status:** OPEN

---

## Issue #2: Tauri window title bar should match app theme

**Date:** 2025-12-07

**Severity:** Low (Cosmetic)

**Symptom:**
The Tauri desktop app window title bar uses the default macOS system color instead of matching the current theme (dark/light) of the AESIR application.

**Expected Behavior:**
The window title bar color should match the app's current theme:
- Dark mode: Dark title bar
- Light mode: Light title bar

**Relevant Code:**
- `Client/Aesir.Client.Web/src-tauri/tauri.conf.json` - Window configuration
- `Client/Aesir.Client.Web/src-tauri/src/main.rs` - Rust app setup

**Possible Solutions:**
1. Use Tauri's `decorations: false` and implement custom title bar in Blazor
2. Use `window-vibrancy` crate (already in dependencies) to set title bar appearance
3. Use Tauri's window theme API: `set_theme()` method
4. Configure `titleBarStyle` in tauri.conf.json for macOS

**References:**
- Tauri Window Customization: https://tauri.app/reference/config/#windowconfig
- window-vibrancy crate: https://github.com/nicholascioli/window-vibrancy

**Status:** OPEN

---

## Issue #3: New Chat button requires double-click when in existing conversation

**Date:** 2025-12-07

**Severity:** Low

**Symptom:**
When the user is in an existing conversation and clicks the "New Chat" plus button, nothing happens on the first click. The user must click the button a second time for the new chat to be created.

**Steps to Reproduce:**
1. Open an existing conversation (click on a chat in the history sidebar)
2. Click the "New Chat" plus button once
3. Observe: nothing happens
4. Click the "New Chat" plus button again
5. Observe: new chat is created

**Expected Behavior:**
A single click on the "New Chat" button should immediately create a new chat, regardless of whether the user is currently in an existing conversation or not.

**Relevant Code:**
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Chat/Layout/ChatLayout.razor` - New Chat button handler
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Chat/Pages/ChatPage.razor` - Chat state management

**Suspected Cause:**
Likely a state synchronization issue where the first click triggers a state change but the navigation or UI update doesn't complete until triggered again.

**Status:** OPEN

---
