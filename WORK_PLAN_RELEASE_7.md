# WORK_PLAN_RELEASE_7.md

> **STATUS: READY FOR TESTING** - Started 2025-12-04, Updated 2025-12-06
>
> RAG Document Upload & File Attachment Feature
>
> **Completed:** Epic 0-5 (Core RAG upload + Documents panel + Progress/Error handling)
> **Remaining:** Epic 6 (Manual E2E testing)
> **Known Issues:** See ISSUES.md #1 - Multiple document RAG search issue

Work items for implementing RAG document upload and file attachment functionality in the AESIR Blazor WebAssembly client.

## Overview

The server-side RAG infrastructure is fully implemented with Qdrant vector store, document chunking, embedding generation, and semantic search. This release focuses on building the client-side UI to allow users to upload files to conversations and leverage RAG capabilities.

**Server Infrastructure Status:** 85% complete - all backend services ready

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| File Scope | Conversation-scoped first | Users need to attach files to specific chats before global knowledge base |
| Upload Location | Chat input area | Follows Claude/ChatGPT UX pattern - attach files where you type |
| Supported Formats | PDF, Text, Images, JSON, XML, CSV | Server already supports all these via document loaders |
| Max File Size | 100MB | Server already configured for this limit |

## Legend

- [ ] Not started
- [x] Completed
- [~] Skipped (with reason in comments)

---

## 🐛 Bug Fixes Required (Found During Testing)

> **PRIORITY: HIGH** - These must be fixed before continuing with Epic 4+

### BUG-1: Immediate File Upload on Selection
**Status:** [x] Fixed (2025-12-05)
**Issue:** Files are not uploaded immediately after selection. The old Avalonia app would:
1. Show the file chip immediately (grayed out with spinner overlay)
2. Begin upload and indexing in background immediately
3. Transition to "ready" state when upload/indexing completes

**Current Behavior:** Files wait until message send to upload.
**Expected Behavior:** Upload starts immediately on file selection.

**Fix Location:** `MessageInput.razor` / `ChatPage.razor` - file selection handler

**Solution Implemented:**
- Added `GetConversationId` callback parameter to `MessageInput` and `ChatWelcome` components
- Added `GetOrCreateConversationId()` method in `ChatPage.razor` to provide consistent conversation IDs
- Added `UploadFileImmediatelyAsync()` method in `MessageInput.razor` that:
  1. Gets conversation ID from parent via callback
  2. Sets file status to `Uploading` immediately
  3. Starts background upload with progress tracking
  4. Updates status to `Completed` or `Failed` on completion
- Updated `HandleFileSelected()` to call `UploadFileImmediatelyAsync()` as a fire-and-forget task
- Updated `SendMessageWithFilesAsync()` to skip already-uploaded files (checks for `Pending` status only)

---

### BUG-2: Drop Zone Not Working
**Status:** [x] Fixed (2025-12-05)
**Issue:** The drag-and-drop functionality is not working properly.

**Requirements:**
1. Drop zone overlay should appear when dragging files over chat area
2. Dropping files should trigger immediate upload (same as BUG-1 fix)
3. Visual feedback during drag operation

**Fix Location:** `DropZoneOverlay.razor`, `ChatPage.razor` drag handlers

**Solution Implemented:**
- Removed `@ondrop` and `@ondrop:preventDefault` from `ChatPage.razor` parent div
- The parent div's `@ondrop:preventDefault` was preventing the `InputFile` in `DropZoneOverlay` from receiving drop events
- Now the drop event properly bubbles to the `InputFile` component which handles file capture
- `HandleFileDropped` still cleans up drag state and delegates to `MessageInput.AddFileAsync()`
- Files dropped will trigger the same immediate upload flow as BUG-1 fix

---

### BUG-3: RAG Tool Not Included in Chat Request
**Status:** [x] Fixed (2025-12-05)
**Issue:** The chat request must include the RagTool in the tools collection, otherwise the backend will not search the RAG vector store for context.

**Requirements:**
1. When sending a message with attached files, ensure `RagTool` is in the request's tools list
2. Backend uses `ToolRequest.IsRagToolRequest` to determine if RAG search should happen
3. Verify the old Avalonia client's approach and replicate

**Fix Location:**
- Client: `ChatPage.razor` - message send logic
- Verify: `BaseChatService.cs` - how tools trigger RAG search

**Solution Implemented:**
- Added `IAgentToolsService` and `IConfigurationApiService` injection to `ChatPage.razor`
- Added `_agentToolRequests` HashSet to store converted `ToolRequest` objects
- Added `LoadAgentToolsAsync()` method that:
  1. Fetches agent tools via `AgentToolsService.GetAgentToolsAsync()`
  2. Fetches MCP servers via `ConfigurationApiService.GetMcpServersAsync()`
  3. Converts `AesirToolBase` to `ToolRequest` (matching Avalonia client logic)
- Tools are loaded when agent is selected (both via `HandleAgentSelected` and `HandleAgentChanged`)
- Tools are added to the chat request using `WithTool()` before streaming

---

### Bug Fix Implementation Order
1. **BUG-3** first - Ensure RAG actually works when files are uploaded
2. **BUG-1** second - Fix immediate upload behavior
3. **BUG-2** third - Fix drop zone (will use same upload logic as BUG-1)

---

## Sprint Plan

**Sprint 0: Agent RAG Configuration**
- Epic 0: Add AllowRag property to Agent (model, migration, UI)

**Sprint 1: Foundation**
- Epic 1: Document API Service (client-side wrapper)
- Epic 2: File Upload Component

**Sprint 2: Integration**
- Epic 3: Chat Input Integration
- Epic 4: Document Display & Management

**Sprint 3: Polish & Testing**
- Epic 5: Error Handling & Progress
- Epic 6: End-to-End Testing

---

## Epic 0: Agent RAG Tool Check

RAG is controlled by whether the agent has "RagTool" assigned (same as old Avalonia client).
No new database properties needed - uses existing tool assignment infrastructure.

### 0.1 Understand Existing Tool System
- [x] Review `AesirTools.RagToolName` constant in `Common/Aesir.Common/Models/`
- [x] Review `GetToolsForAgentAsync(agentId)` API endpoint
- [x] Verify RagTool can be assigned to agents in Settings UI

### 0.2 Create Agent Tools Service (Client)
- [x] Create `IAgentToolsService` interface in Chat module
- [x] Method: `GetAgentToolsAsync(agentId)` → returns list of tools
- [x] Method: `HasRagTool(tools)` → checks if "RagTool" is in collection
- [x] Cache tools per agent to avoid repeated API calls

### 0.3 Integrate Tool Check in Chat UI
- [x] On agent selection, fetch agent's tools
- [x] Store tools in chat state (similar to old client's `AllToolsAvailable`)
- [x] Create computed property: `IsRagEnabled` = tools.Contains("RagTool")
- [x] Disable/hide file upload button when `IsRagEnabled` is false
- [x] Disable drag-and-drop when `IsRagEnabled` is false
- [x] Show tooltip on disabled button: "Assign RagTool to this agent to enable file uploads"

---

## Epic 1: Document API Service

Create client-side service to communicate with document endpoints.

### 1.1 Create IDocumentApiService Interface
- [x] Define interface in `Aesir.Client.Web.Modules.Chat/Services/`
- [x] `UploadConversationFileAsync(conversationId, file, cancellationToken)`
- [x] `GetConversationFilesAsync(conversationId)`
- [x] `DeleteConversationFileAsync(conversationId, filename)`
- [x] `DownloadConversationFileAsync(conversationId, filename)`

### 1.2 Implement DocumentApiService
- [x] Create implementation using `HttpClient` for multipart form data
- [x] Handle streaming upload for large files
- [x] Handle streaming download for large files
- [x] Add proper error handling and `ApiResult<T>` return types

### 1.3 Register Service
- [x] Register in `Aesir.Client.Web.Modules.Chat/ChatModule.cs`
- [x] Add HttpClient configuration for document endpoint base URL

---

## Epic 2: File Upload Components

Create reusable file upload components with drag-and-drop support.

### 2.1 Create FileUploadButton Component
- [x] Create `Components/FileUploadButton.razor` in Chat module
- [x] Paperclip/attachment icon button using MudBlazor
- [x] Opens native file picker on click via `InputFile`
- [x] Support multiple file selection
- [x] Filter by supported file types (PDF, txt, md, json, xml, csv, images)

### 2.2 Create FileDropZone Component
- [x] Create `Components/DropZoneOverlay.razor` in Chat module
- [x] Invisible overlay that activates on drag enter
- [x] Visual feedback when dragging files over (border highlight, icon)
- [x] Drop handler that triggers upload callback

### 2.3 Create AttachedFileChip Component
- [x] Create `Components/FileAttachment.razor` in Chat module
- [x] Show file icon based on type (PDF icon, image icon, text icon, etc.)
- [x] Display filename (truncated with ellipsis if too long)
- [x] Remove button (X) to detach file before sending
- [x] Loading state during upload (spinner)
- [x] Error state if upload fails (red border, retry option)
- [x] Success state (checkmark)

---

## Epic 3: Chat Input Integration

Integrate file upload into the chat input area.

### 3.1 Add Upload Button to ChatInput
- [x] Add FileUploadButton to left of text input
- [x] Position similar to Claude's attachment button
- [x] Style consistently with existing input area theme

### 3.2 Add Attached Files Display Area
- [x] Show AttachedFileChip components above input area when files selected
- [x] Horizontal scrollable list if many files
- [x] Allow removal before sending message

### 3.3 Implement File Upload Flow
- [x] On file selection: add to pending files list, show as "pending" chips
- [x] On message send: upload all pending files first
- [x] Associate uploaded files with conversation ID
- [x] Clear attached files after successful message send
- [x] Handle upload failures gracefully (show error, allow retry)

### 3.4 Add Drag-and-Drop to Chat Area
- [x] Wrap entire chat content area with FileDropZone
- [x] Show visual overlay when dragging files ("Drop files here")
- [x] Files dropped anywhere in chat area get attached to input

---

## Epic 4: Document Display & Management

Show uploaded documents in the conversation.

### 4.1 Create ConversationDocumentsList Component
- [x] Create `Components/ConversationDocumentsList.razor`
- [x] List all files attached to current conversation
- [x] Show: file name, type icon, size, upload date
- [x] Actions per file: download, delete

### 4.2 Add Documents Panel to Chat
- [x] Add documents button in chat header (or use existing sidebar)
- [x] Toggle shows/hides ConversationDocumentsList
- [x] Panel slides in from right or appears as overlay
- [x] Close button to dismiss

### 4.3 Document Context in Messages (Future Enhancement)
- [~] When RAG retrieves document chunks, show source attribution (Future)
- [~] Display "Based on: filename.pdf" in assistant responses (Future)
- [~] Link to view/download source document (Future)

---

## Epic 5: Error Handling & Progress

Provide clear feedback during file operations.

### 5.1 Upload Progress Indicator
- [x] Show progress bar during upload for files > 1MB
- [x] Display percentage for large files
- [x] Indeterminate spinner for small files
- [x] Cancel upload option (via remove button during upload)

### 5.2 Error States & Messages
- [x] File too large error (>100MB) - show specific message
- [x] Unsupported file type error - list supported types
- [x] Upload failed error with retry option
- [x] Network error handling with user-friendly message

### 5.3 Success Feedback
- [x] Brief snackbar notification on successful upload
- [x] File chip transitions from loading spinner to checkmark
- [x] Smooth CSS animations for state transitions

---

## Epic 6: End-to-End Testing

Verify complete file upload and RAG functionality.

### 6.1 Manual Testing Checklist
- [ ] Upload PDF file via button click
- [ ] Upload text file via drag-and-drop
- [ ] Upload image file (PNG, JPG)
- [ ] Upload JSON/XML/CSV files
- [ ] Verify files appear in documents list
- [ ] Send message asking about uploaded document
- [ ] Verify RAG retrieval works (agent references document content)
- [ ] Delete uploaded file from documents list
- [ ] Test in browser (Chrome, Firefox)
- [ ] Test in Tauri desktop app

### 6.2 Edge Cases
- [ ] Upload multiple files at once (3-5 files)
- [ ] Upload very large file (50MB, near limit)
- [ ] Upload then cancel before complete
- [ ] Network interruption during upload
- [ ] Invalid file type rejection
- [ ] Empty file upload attempt
- [ ] Duplicate filename upload

### 6.3 Integration Tests
- [ ] Add tests for DocumentApiService methods
- [ ] Add tests for FileUploadButton rendering
- [ ] Add tests for AttachedFileChip states

---

## Work Item Dependencies

```
1.1 [API Interface] ─── 1.2 [API Implementation] ─── 1.3 [Register Service]
                                    │
                                    ▼
2.1 [Upload Button] ────────────────┼─── 3.1 [Input Integration]
2.2 [Drop Zone] ────────────────────┼─── 3.4 [Drag-Drop Chat]
2.3 [File Chip] ────────────────────┴─── 3.2 [Attached Display]
                                              │
                                              ▼
                                         3.3 [Upload Flow]
                                              │
                                              ▼
4.1 [Documents List] ─── 4.2 [Documents Panel]
                                              │
                                              ▼
5.1 [Progress] ───┬─── 5.3 [Success Feedback]
5.2 [Errors] ─────┘           │
                              ▼
                         6.1 [E2E Testing]
```

---

## Existing Server Endpoints (Reference)

### Conversation Files API
```
POST   /document/collections/conversations/{conversationId}/upload/file
       - Accepts multipart/form-data
       - Field name: "file"
       - Returns: FileInfo (filename, size, mimeType)

GET    /document/collections/conversations/{conversationId}/files
       - Returns: List<FileInfo>

GET    /document/collections/conversations/{conversationId}/files/{filename}/content
       - Returns: File stream
       - Content-Disposition: attachment

DELETE /document/collections/conversations/{conversationId}/files/{filename}
       - Returns: 204 No Content on success
```

### Server Configuration
- **Max File Size:** 100MB (configured in server)
- **Vector Store:** Qdrant (collection: `aesir_conversation_document`)
- **Embedding Model:** `mxbai-embed-large:latest` via Ollama (1024 dimensions)
- **Chunk Size:** 384 tokens per paragraph, 128 tokens per line, 20% overlap

### Supported File Types
| Type | Extensions | Loader |
|------|------------|--------|
| PDF | .pdf | PdfDataLoaderService |
| Text | .txt, .md | TextFileLoaderService |
| JSON | .json | TextFileLoaderService (with JSON parsing) |
| XML | .xml | TextFileLoaderService (with XML parsing) |
| CSV | .csv | TextFileLoaderService (with CSV parsing) |
| Images | .png, .jpg, .jpeg, .gif, .webp | ImageDataLoaderService |

---

## Success Criteria

- [ ] File upload button checks if agent has "RagTool" assigned
- [ ] File upload button is disabled when agent does NOT have RagTool
- [ ] File upload button is enabled when agent HAS RagTool assigned
- [ ] Users can upload files via button click in chat input area
- [ ] Users can drag-and-drop files anywhere in chat area
- [ ] Uploaded files display as chips with loading/complete/error states
- [ ] Users can remove attached files before sending
- [ ] Files are stored in PostgreSQL and embedded in Qdrant for RAG
- [ ] Users can view list of all documents attached to conversation
- [ ] Users can delete documents from conversation
- [ ] RAG works: asking questions about uploaded documents returns relevant answers
- [ ] Upload progress shown for large files
- [ ] Clear error messages for failures
- [ ] Works in both browser and Tauri desktop
- [ ] All existing tests (499+) still pass

---

## Technical Notes

### File Upload in Blazor WASM

**Using MudBlazor MudFileUpload:**
```razor
<MudFileUpload T="IReadOnlyList<IBrowserFile>"
               FilesChanged="OnFilesSelected"
               Accept=".pdf,.txt,.md,.json,.xml,.csv,.png,.jpg,.jpeg">
    <ActivatorContent>
        <MudIconButton Icon="@Icons.Material.Filled.AttachFile" />
    </ActivatorContent>
</MudFileUpload>
```

**Converting to MultipartFormDataContent:**
```csharp
var content = new MultipartFormDataContent();
var fileContent = new StreamContent(file.OpenReadStream(maxFileSize));
fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
content.Add(fileContent, "file", file.Name);
```

### Drag-and-Drop

**MudBlazor's MudFileUpload has built-in drag-and-drop:**
```razor
<MudFileUpload T="IReadOnlyList<IBrowserFile>"
               FilesChanged="OnFilesDropped"
               DropTarget="@dropZone">
```

**Or use JS interop for custom behavior:**
```javascript
element.addEventListener('dragover', e => e.preventDefault());
element.addEventListener('drop', e => { /* handle files */ });
```

### Large File Handling
- Use `IBrowserFile.OpenReadStream(maxAllowedSize)` with appropriate limit
- Stream to server rather than loading entire file into memory
- Show progress via HttpClient's `IProgress<T>` parameter

---

## Resources

- Server Documents Controller: `Server/Modules/Aesir.Modules.Documents/Controllers/DocumentCollectionController.cs`
- Server Document Loaders: `Server/Modules/Aesir.Modules.Documents/Services/DocumentLoaders/`
- Server Document Collections: `Server/Modules/Aesir.Modules.Documents/Services/DocumentCollections/`
- Client Chat Module: `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Chat/`
- MudBlazor FileUpload Docs: https://mudblazor.com/components/fileupload

---

## Test Commands

```bash
# Run all tests
dotnet test

# Test document upload endpoint directly
curl -X POST http://localhost:5000/document/collections/conversations/{conversationId}/upload/file \
  -F "file=@test.pdf"

# List conversation files
curl http://localhost:5000/document/collections/conversations/{conversationId}/files

# Download a file
curl http://localhost:5000/document/collections/conversations/{conversationId}/files/test.pdf/content \
  -o downloaded.pdf

# Delete a file
curl -X DELETE http://localhost:5000/document/collections/conversations/{conversationId}/files/test.pdf
```
