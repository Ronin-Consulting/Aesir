# AESIR Projects Feature - Implementation Plan

## Overview

Projects allow users to create self-contained workspaces with their own chat histories and knowledge bases. Within each project, users can upload documents, provide context, and have focused chats with AESIR.

## Feature Requirements

1. **Project Management**: Create, edit, delete projects
2. **Project Knowledge Base**: Upload documents to project-level storage with RAG
3. **Project Instructions**: Custom instructions appended to agent persona prompts
4. **Chat Association**: Associate conversations with projects
5. **UI Integration**: Projects in sidebar with drill-down navigation

---

## Phase 1: Core Project Infrastructure

### 1.1 Database Schema

**New Table: `aesir_project`**
```sql
CREATE TABLE aesir.aesir_project (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id VARCHAR(255) NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    instructions TEXT,                    -- Project-specific instructions for AI
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP,
    deleted_by VARCHAR(255),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255) NOT NULL,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by VARCHAR(255)
);

CREATE UNIQUE INDEX ix_aesir_project_name ON aesir.aesir_project(name);
CREATE INDEX ix_aesir_project_user_id ON aesir.aesir_project(user_id);
```

**Modify Table: `aesir_chat_session`**
```sql
ALTER TABLE aesir.aesir_chat_session
ADD COLUMN project_id UUID REFERENCES aesir.aesir_project(id);

CREATE INDEX ix_aesir_chat_session_project_id ON aesir.aesir_chat_session(project_id);
```

### 1.2 Server Models

**File: `Server/Aesir.Infrastructure/Models/AesirProject.cs`**
```csharp
public class AesirProject : IEntity
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("instructions")]
    public string? Instructions { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    // Audit fields
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("updated_by")]
    public string? UpdatedBy { get; set; }

    // Soft delete
    [JsonPropertyName("is_deleted")]
    public bool IsDeleted { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [JsonPropertyName("deleted_by")]
    public string? DeletedBy { get; set; }
}
```

**File: `Common/Aesir.Common/Models/AesirProjectBase.cs`**
```csharp
public class AesirProjectBase
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("instructions")]
    public string? Instructions { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
```

### 1.3 Server Module: Aesir.Modules.Projects

**New Module Structure:**
```
Server/Modules/Aesir.Modules.Projects/
├── Controllers/
│   └── ProjectsController.cs
├── Migrations/
│   ├── 20250103000001_CreateProjectsTable.cs
│   └── 20250103000002_AddProjectIdToChatSession.cs
├── Repositories/
│   ├── IProjectRepository.cs
│   └── ProjectRepository.cs
├── Services/
│   ├── IProjectService.cs
│   └── ProjectService.cs
├── ProjectsModule.cs
└── Aesir.Modules.Projects.csproj
```

**API Endpoints:**
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/projects` | List user's projects |
| GET | `/projects/{id}` | Get project details |
| POST | `/projects` | Create new project |
| PUT | `/projects/{id}` | Update project |
| DELETE | `/projects/{id}` | Delete project (soft) |
| GET | `/projects/{id}/conversations` | List project conversations |
| POST | `/projects/{id}/conversations/{conversationId}` | Associate conversation with project |
| DELETE | `/projects/{id}/conversations/{conversationId}` | Remove conversation from project |

### 1.4 Update Chat Session

**Modify `AesirChatSession`** to include project association:
```csharp
[JsonPropertyName("project_id")]
public Guid? ProjectId { get; set; }
```

---

## Phase 2: Project Knowledge Base (RAG)

### 2.1 Leverage Existing GlobalDocumentCollectionService

**No new services or controllers needed!** We reuse the existing infrastructure:

- **Service**: `GlobalDocumentCollectionService` (already exists)
- **Vector Storage**: `aesir_global_document` Qdrant collection (already exists)
- **Scoping**: Use `ProjectId` as the `CategoryId` parameter

### 2.2 Existing Endpoints (Reused)

The existing `/document/collections/globals/{categoryId}/...` endpoints work as-is:

| Method | Route | Usage for Projects |
|--------|-------|-------------------|
| POST | `/document/collections/globals/{projectId}/upload/file` | Upload document to project |
| GET | `/document/collections/globals/{projectId}/files` | List project documents |
| GET | `/document/collections/globals/{projectId}/files/{filename}/content` | Get document content |
| DELETE | `/document/collections/globals/{projectId}/files/{filename}` | Delete document |

**Key insight**: The `categoryId` parameter in the global endpoints is just a string identifier. By passing the `ProjectId` (as string), we get project-scoped document storage without any new code.

### 2.3 How It Works

1. **Upload**: Client calls `/document/collections/globals/{projectId}/upload/file`
2. **Storage**: Document stored with `Category = projectId` in Qdrant
3. **Search**: `GlobalDocumentCollectionService.GetKernelPluginFunctionsAsync(projectId)` returns search functions filtered by project
4. **Inference**: Project documents exposed as tools during chat (same as global documents)

---

## Phase 3: Project Instructions Integration

### 3.1 Modify Chat Request

**Update `AesirChatRequestBase`:**
```csharp
[JsonPropertyName("project_id")]
public Guid? ProjectId { get; set; }
```

### 3.2 Prompt Composition

**Modify `BaseChatService.RenderSystemPrompt()`** to append project instructions:

```csharp
protected void RenderSystemPrompt(
    AesirConversation conversation,
    Dictionary<string, object> arguments,
    PromptPersona? persona = null,
    string? customPromptContent = null,
    string? projectInstructions = null)  // NEW PARAMETER
{
    // ... existing logic to get promptContent ...

    // APPEND PROJECT INSTRUCTIONS
    if (!string.IsNullOrWhiteSpace(projectInstructions))
    {
        promptContent += $"\n\n## Project-Specific Instructions\n\n{projectInstructions}";
    }

    // ... continue with message creation and template rendering ...
}
```

### 3.3 Update Chat Controller

Fetch project instructions when project ID is provided:
```csharp
string? projectInstructions = null;
if (request.ProjectId.HasValue)
{
    var project = await _projectService.GetByIdAsync(request.ProjectId.Value);
    projectInstructions = project?.Instructions;
}
```

### 3.4 Expose Project Documents as Tool

**Update inference flow** to include project document search when project ID is present:

```csharp
// In BasePromptExecutionSettingsBuilder or ChatController
if (request.ProjectId.HasValue)
{
    // Use existing GlobalDocumentCollectionService with ProjectId as CategoryId
    var globalDocArgs = GlobalDocumentCollectionArgs.Default;
    globalDocArgs.SetCategoryId(request.ProjectId.Value.ToString());

    var projectDocFunctions = await _globalDocumentCollectionService
        .GetKernelPluginFunctionsAsync(
            request.ProjectId.Value.ToString(),
            cancellationToken);

    // Add to kernel plugins for this chat session
    kernelFunctions.AddRange(projectDocFunctions);
}
```

This reuses the existing `GlobalDocumentCollectionService` - no new service needed.

---

## Phase 4: Client UI Module

### 4.1 Module Structure

```
Client/Modules/Aesir.Client.Web.Modules.Projects/
├── Pages/
│   ├── ProjectsPage.razor           # List all projects
│   ├── ProjectDetailsPage.razor     # Project detail view
│   └── ProjectSettingsPage.razor    # Edit project settings
├── Components/
│   ├── ProjectCard.razor            # Project card for list view
│   ├── ProjectSidebar.razor         # Sidebar with project nav
│   ├── ProjectDocumentList.razor    # Document management
│   └── ProjectConversationList.razor # Conversations in project
├── Layout/
│   └── ProjectLayout.razor          # Layout for project pages
├── Services/
│   ├── IProjectApiService.cs
│   └── ProjectApiService.cs
├── ProjectModule.cs
├── _Imports.razor
└── Aesir.Client.Web.Modules.Projects.csproj
```

### 4.2 Navigation Registration

```csharp
public class ProjectModule : ClientModuleBase
{
    public override void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register(new NavigationItem
        {
            Title = "Projects",
            Href = "/projects",
            Icon = "FolderSpecial",
            Priority = 20,  // After Chat (10), before Settings (100)
            Group = "Main"
        });
    }
}
```

### 4.3 UI Pages

**ProjectsPage.razor** (`/projects`)
- Grid/list of user's projects
- "New Project" button
- Search/filter projects
- Click to drill down

**ProjectDetailsPage.razor** (`/projects/{id}`)
- Project header (name, description)
- Tabs:
  - **Conversations**: List of project chats, "New Chat" button
  - **Knowledge**: Document upload/management
  - **Settings**: Edit name, description, instructions

**ProjectSettingsPage.razor** (`/projects/{id}/settings`)
- Edit project name, description
- Edit project instructions (textarea/editor)
- Delete project option

### 4.4 Chat Integration

**Modify ChatPage.razor:**
- Accept optional `projectId` query parameter
- Display project name in header when in project context
- Pass `projectId` to chat requests

**Route:** `/chat?projectId={id}` or `/projects/{projectId}/chat`

---

## Phase 5: Integration & Polish

### 5.1 Chat Session Updates

- Update chat history service to filter by project
- Show project badge on chat items in sidebar
- Allow moving chats between projects

### 5.2 Project Context in UI

- Show current project name in chat header
- Project knowledge indicator (number of documents)
- Quick switch between project chats

### 5.3 Security

- Ensure users can only access their own projects
- Validate project ownership on all operations
- Audit logging for project operations

---

## Implementation Order

### MVP (Phase 1 + Basic Phase 4)
1. Database migrations for projects table
2. Server models and repository
3. Projects API endpoints (CRUD)
4. Client module with basic pages
5. Project list and create/edit UI

### Full Feature (All Phases)
1. **Week 1**: Phase 1 (Core Infrastructure)
2. **Week 2**: Phase 2 (Knowledge Base/RAG)
3. **Week 3**: Phase 3 (Instructions Integration)
4. **Week 4**: Phase 4 (Client UI)
5. **Week 5**: Phase 5 (Integration & Polish)

---

## Files to Create/Modify

### New Files
| File | Description |
|------|-------------|
| `Server/Modules/Aesir.Modules.Projects/` | New server module (controller, service, repository, migrations) |
| `Server/Aesir.Infrastructure/Models/AesirProject.cs` | Server model |
| `Common/Aesir.Common/Models/AesirProjectBase.cs` | Shared model for client/server |
| `Client/Modules/Aesir.Client.Web.Modules.Projects/` | New client module |

### Modified Files
| File | Change |
|------|--------|
| `Server/Aesir.Infrastructure/Models/AesirChatSession.cs` | Add `ProjectId` property |
| `Server/Modules/Aesir.Modules.Inference/Services/BaseChatService.cs` | Append project instructions to system prompt |
| `Common/Aesir.Common/Models/AesirChatRequestBase.cs` | Add `ProjectId` property |
| `Client/Aesir.Client.Web.App/Program.cs` | Register Projects module |
| `Client/Aesir.Client.Web.App/App.razor` | Add module assembly reference |

### Reused (No Changes Needed)
| File | Reused For |
|------|-----------|
| `GlobalDocumentCollectionService` | Project document storage (using ProjectId as CategoryId) |
| `DocumentCollectionController` | Existing `/globals/{categoryId}/*` endpoints work for projects |
| `aesir_global_document` (Qdrant) | Vector storage for project documents |

---

## Design Decisions

| Decision | Choice | Notes |
|----------|--------|-------|
| **Project Naming** | Globally unique | Enforced via unique constraint on `name` column |
| **Default Agent** | User's selected agent | No project-level agent override; uses conversation's agent |
| **Project Deletion** | User chooses | Prompt user: orphan conversations or delete them |
| **Future Sharing** | Design for extensibility | Include `user_id` field; future: add `aesir_project_member` table |

---

## Approval Status

✅ **Plan Approved** - Ready for implementation
