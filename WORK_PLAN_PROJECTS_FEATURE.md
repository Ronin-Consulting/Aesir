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

CREATE INDEX ix_aesir_project_user_id ON aesir.aesir_project(user_id);
CREATE INDEX ix_aesir_project_name ON aesir.aesir_project(name);
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

### 2.1 Vector Storage Model

**File: `Server/Aesir.Infrastructure/Models/AesirProjectDocumentTextData.cs`**
```csharp
public class AesirProjectDocumentTextData<TKey> : AesirTextData<TKey>
{
    [VectorStoreRecordData]
    public string ProjectId { get; set; } = string.Empty;
}
```

### 2.2 Project Document Collection Service

**Interface: `IProjectDocumentCollectionService`**
```csharp
public interface IProjectDocumentCollectionService
{
    Task<bool> LoadDocumentAsync(
        string filePath,
        IDictionary<string, object> fileMetaData,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDocumentAsync(
        string projectId,
        string filename,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDocumentsAsync(
        string projectId,
        CancellationToken cancellationToken = default);

    Task<List<KernelFunction>> GetKernelPluginFunctionsAsync(
        string projectId,
        CancellationToken cancellationToken = default);
}
```

### 2.3 Document Upload Endpoints

**Add to DocumentCollectionController:**
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/document/collections/projects/{projectId}/upload/file` | Upload document to project |
| GET | `/document/collections/projects/{projectId}/files` | List project documents |
| GET | `/document/collections/projects/{projectId}/files/{filename}/content` | Get document content |
| DELETE | `/document/collections/projects/{projectId}/files/{filename}` | Delete document |

### 2.4 Qdrant Collection

Register new Qdrant collection:
```csharp
services.AddKeyedQdrantCollection<Guid, AesirProjectDocumentTextData<Guid>>(
    serviceKey: null,
    name: "aesir_project_document",
    clientProvider,
    optionsProvider);
```

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

**Update `KernelPluginService`** to include project document search when project ID is present:
```csharp
if (args is ProjectDocumentCollectionArgs projectArgs)
{
    var projectId = projectArgs.GetProjectId();
    var projectFunctions = await _projectDocumentCollectionService
        .GetKernelPluginFunctionsAsync(projectId, cancellationToken);
    kernelFunctions.AddRange(projectFunctions);
}
```

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
| `Server/Modules/Aesir.Modules.Projects/` | New server module |
| `Server/Aesir.Infrastructure/Models/AesirProject.cs` | Server model |
| `Server/Aesir.Infrastructure/Models/AesirProjectDocumentTextData.cs` | Vector data model |
| `Server/Aesir.Infrastructure/Services/IProjectDocumentCollectionService.cs` | Interface |
| `Common/Aesir.Common/Models/AesirProjectBase.cs` | Shared model |
| `Client/Modules/Aesir.Client.Web.Modules.Projects/` | New client module |

### Modified Files
| File | Change |
|------|--------|
| `Server/Aesir.Infrastructure/Models/AesirChatSession.cs` | Add `ProjectId` |
| `Server/Modules/Aesir.Modules.Documents/DocumentsModule.cs` | Register project collection |
| `Server/Modules/Aesir.Modules.Documents/Controllers/DocumentCollectionController.cs` | Add project endpoints |
| `Server/Modules/Aesir.Modules.Inference/Services/BaseChatService.cs` | Append project instructions |
| `Common/Aesir.Common/Models/AesirChatRequestBase.cs` | Add `ProjectId` |
| `Client/Aesir.Client.Web.App/Program.cs` | Register Projects module |
| `Client/Aesir.Client.Web.App/App.razor` | Add module assembly |

---

## Questions for Clarification

1. **Project Naming**: Should project names be unique per user, or globally unique?
2. **Default Agent**: Should projects have a default agent, or always use the user's selected agent?
3. **Project Deletion**: When deleting a project, what happens to associated conversations?
   - Option A: Conversations become project-less (orphaned)
   - Option B: Conversations are also deleted
   - Option C: User chooses during deletion
4. **Project Sharing** (Future): Any consideration for future multi-user project support?

---

## Approval Checklist

- [ ] Phase 1: Core Project Infrastructure approved
- [ ] Phase 2: Project Knowledge Base approved
- [ ] Phase 3: Project Instructions Integration approved
- [ ] Phase 4: Client UI Module approved
- [ ] Phase 5: Integration & Polish approved
- [ ] Implementation order confirmed
