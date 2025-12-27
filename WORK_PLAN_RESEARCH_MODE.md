# Work Plan: AESIR Research Mode

## Executive Summary

**Feature Name**: Research Mode
**Version**: 1.0.0 (MVP)
**Author**: Claude Code
**Created**: 2025-12-27
**Updated**: 2025-12-27
**Status**: Awaiting Approval

Research Mode transforms AESIR into a multi-agent research orchestration platform. Users configure **Research Teams** in Settings, assigning existing agents to specialized research roles. When a user selects a Research Team from the Agent Selector in a conversation, the conversation enters "research mode" - the user's message becomes a research query, and a **Team Message** bubble displays real-time progress and the final report.

---

## Table of Contents

1. [Feature Overview](#1-feature-overview)
2. [Research Team Configuration](#2-research-team-configuration)
3. [Chat Integration & UX Flow](#3-chat-integration--ux-flow)
4. [Architecture Design](#4-architecture-design)
5. [Database Schema](#5-database-schema)
6. [Backend Implementation](#6-backend-implementation)
7. [Frontend Implementation](#7-frontend-implementation)
8. [Implementation Phases](#8-implementation-phases)
9. [Testing Strategy](#9-testing-strategy)
10. [Future Enhancements](#10-future-enhancements)
11. [Risk Assessment](#11-risk-assessment)

---

## 1. Feature Overview

### 1.1 Core Concept

Research Mode orchestrates 4 AI agents to collaboratively research a user query:

| Role | Expertise | Default Temperature | Tools Access |
|------|-----------|---------------------|--------------|
| **Deep Diver** | Exhaustive investigation | 0.3 | Inherits from base agent |
| **Synthesizer** | Pattern recognition | 0.5 | Inherits from base agent |
| **Devil's Advocate** | Critical analysis | 0.4 | Inherits from base agent |
| **Chairman** | Evaluate & synthesize | 0.2 | None (evaluates only) |

### 1.2 Key Architectural Insight

**Research Teams are configured separately from agents.** Each team member:
- **References an existing agent** (provides infrastructure: inference engine, model, tools)
- **Has a ResearchAgentConfig override** (provides research behavior: persona, temperature, prompts)

This separation allows users to:
- Reuse their existing, configured agents
- Customize research behavior without modifying the base agent
- Create multiple specialized research teams

### 1.3 MVP Scope (Standard Mode)

- **Duration**: 30-45 minutes
- **Agents**: 3 researchers + 1 Chairman
- **Rounds**: 1 research round + peer review
- **Output**: Interactive markdown report with PDF/Word export
- **Entry Point**: Agent Selector dropdown in Chat

### 1.4 Workflow Summary

```
User selects Research Team → Types query → Clarification → Planning → Research → Anonymize → Peer Review → Synthesis → Team Message shows Report
```

### 1.5 Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Team Configuration | Settings > Research Teams | Separate from Agents, after Agents in nav |
| Base Agent | Provides infrastructure | Inference engine, model, tools inherited |
| Research Override | Customizes behavior | Persona, temperature, prompts can be overridden |
| Entry Point | Agent Selector dropdown | Research teams appear alongside agents |
| Chat Integration | Team Message bubble | Research results appear in conversation history |
| Mixed Mode | Allowed | Can switch between agents and teams in same conversation |
| Document Access | Conversation documents | Attached documents become RAG corpus |
| Chairman Role | Evaluate/synthesize only | Always uses larger model, no primary research |
| User Control | Clarifying questions upfront | Autonomous after clarification |
| Scoring | 5 criteria (1-10) + critiques | Comprehensive but not overwhelming |
| Output | Markdown → HTML + PDF/Word | Interactive web view primary |

---

## 2. Research Team Configuration

### 2.1 Configuration Hierarchy

```
┌─────────────────────────────────────────────────────────────────┐
│                     Research Team                                │
│  "Legal Research Team"                                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Chairman Role                                            │   │
│  │ ├─ Base Agent: "Claude Opus 4" (via OpenAI engine)      │   │
│  │ └─ Override: temperature=0.2, persona=Chairman default   │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Deep Diver Role                                          │   │
│  │ ├─ Base Agent: "GPT-4 Turbo" (via OpenAI engine)        │   │
│  │ └─ Override: temperature=0.3, persona=DeepDiver default  │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Synthesizer Role                                         │   │
│  │ ├─ Base Agent: "Claude Sonnet" (via Anthropic engine)   │   │
│  │ └─ Override: temperature=0.5, custom persona             │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Devil's Advocate Role                                    │   │
│  │ ├─ Base Agent: "Claude Sonnet" (via Anthropic engine)   │   │
│  │ └─ Override: temperature=0.4, persona=DevilsAdvocate     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 What the Base Agent Provides (Infrastructure)

When a team member references an existing agent, it inherits:

| From Base Agent | Description |
|-----------------|-------------|
| Inference Engine | OpenAI, Ollama, Anthropic, etc. |
| Model | gpt-4-turbo, claude-3-opus, llama3, etc. |
| Tools | RAG, Web Search, MCP tools configured for that agent |
| Thinking Mode | Extended thinking capability (unless overridden) |

### 2.3 What the Research Override Provides (Behavior)

The `ResearchAgentConfig` allows customization of:

| Override | Default Source | User Can Customize? |
|----------|---------------|---------------------|
| Temperature | Role default (0.3, 0.5, 0.4, 0.2) | Yes |
| Persona | System-defined role persona | Yes |
| Planning Prompt | System-defined | Yes |
| Research Prompt | System-defined | Yes |
| Thinking Mode | Inherit from base agent | Yes (override to specific value) |
| Tools | Inherit from base agent | Yes (select subset) |

### 2.4 Settings UI Design

**Location**: Settings > Research Teams (after Agents)

```
┌─────────────────────────────────────────────────────────────────┐
│ Settings > Research Teams                                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ [+ Create New Team]                                             │
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 📊 Legal Research Team                           [Edit] [X] │ │
│ │ ──────────────────────────────────────────────────────────── │ │
│ │ Chairman:        Claude Opus 4                               │ │
│ │ Deep Diver:      GPT-4 Turbo                                │ │
│ │ Synthesizer:     Claude Sonnet                              │ │
│ │ Devil's Advocate: Claude Sonnet                             │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ 📊 Technical Analysis Team                       [Edit] [X] │ │
│ │ ──────────────────────────────────────────────────────────── │ │
│ │ Chairman:        Claude Opus 4                               │ │
│ │ Deep Diver:      GPT-4 Turbo                                │ │
│ │ Synthesizer:     GPT-4 Turbo                                │ │
│ │ Devil's Advocate: Claude Sonnet                             │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 2.5 Team Edit Dialog

```
┌─────────────────────────────────────────────────────────────────┐
│ Edit Research Team                                        [X]  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ Team Name: [Legal Research Team________________]                │
│                                                                 │
│ Description: [Specialized for legal research and case analysis] │
│                                                                 │
│ ═══════════════════════════════════════════════════════════════ │
│                                                                 │
│ CHAIRMAN                                                        │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Base Agent: [▼ Select Agent_____]                           │ │
│ │             ┌─────────────────────────────────────────────┐ │ │
│ │             │ Claude Opus 4 (OpenAI)                      │ │ │
│ │             │ GPT-4 Turbo (OpenAI)                        │ │ │
│ │             │ Claude Sonnet (Anthropic)                   │ │ │
│ │             └─────────────────────────────────────────────┘ │ │
│ │                                                             │ │
│ │ [Configure Override ▼]                                      │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ DEEP DIVER                                                      │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Base Agent: [GPT-4 Turbo__________▼]                        │ │
│ │ [Configure Override ▼]                                      │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ SYNTHESIZER                                                     │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Base Agent: [Claude Sonnet________▼]                        │ │
│ │ [Configure Override ▼]                                      │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ DEVIL'S ADVOCATE                                                │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Base Agent: [Claude Sonnet________▼]                        │ │
│ │ [Configure Override ▼]                                      │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│                                        [Cancel]  [Save Team]   │
└─────────────────────────────────────────────────────────────────┘
```

### 2.6 Configure Override Panel (Expanded)

```
┌─────────────────────────────────────────────────────────────────┐
│ Configure Override: Deep Diver                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ Base Agent: GPT-4 Turbo                                        │
│ Inference Engine: OpenAI                                        │
│ Model: gpt-4-turbo                                             │
│ Tools: RAG, Web Search, Code Interpreter                       │
│                                                                 │
│ ═══════════════════ Research Behavior Override ═════════════════│
│                                                                 │
│ Temperature:                                                    │
│ [● Use Role Default (0.3)] [○ Custom: [____]]                  │
│                                                                 │
│ Persona:                                                        │
│ [● Use Role Default] [○ Custom]                                │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ You are a meticulous research specialist known for          │ │
│ │ exhaustive, thorough investigation...                       │ │
│ │                                                             │ │
│ │ [Edit if Custom selected]                                   │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│ Planning Prompt: [● Use Default] [○ Custom]                    │
│ Research Prompt: [● Use Default] [○ Custom]                    │
│                                                                 │
│ Thinking Mode:                                                  │
│ [● Inherit from Agent] [○ Override: [▼ High___]]               │
│                                                                 │
│ Tools:                                                          │
│ [● Inherit All from Agent] [○ Select Specific]                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ [✓] RAG (Document Search)                                   │ │
│ │ [✓] Web Search                                              │ │
│ │ [✓] Code Interpreter                                        │ │
│ │ [ ] Image Generation (disabled for research)                │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│                              [Reset to Defaults]  [Apply]      │
└─────────────────────────────────────────────────────────────────┘
```

### 2.7 Validation Rules

| Rule | Description |
|------|-------------|
| All 4 roles required | Team must have Chairman, Deep Diver, Synthesizer, Devil's Advocate |
| Same agent allowed | User can assign same agent to multiple roles |
| Active agents only | Can only select from active (non-deleted) agents |
| Unique team names | No duplicate team names per user |

---

## 3. Chat Integration & UX Flow

### 3.1 Agent Selector Integration

Research Teams appear in the Agent Selector dropdown, grouped separately:

```
┌─────────────────────────────────────────────────────────────────┐
│ Agent Selector Dropdown                                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ AGENTS                                                          │
│ ├─ 🤖 Claude Opus 4                                            │
│ ├─ 🤖 GPT-4 Turbo                                              │
│ ├─ 🤖 Claude Sonnet                                            │
│ └─ 🤖 Gemini Pro                                               │
│                                                                 │
│ ─────────────────────────────────────────────────────────────── │
│                                                                 │
│ RESEARCH TEAMS                                                  │
│ ├─ 📊 Legal Research Team                                      │
│ ├─ 📊 Technical Analysis Team                                  │
│ └─ 📊 Market Research Team                                     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Complete UX Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ STEP 1: User in Chat, selects Research Team                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ Agent: [📊 Legal Research Team ▼]                               │
│                                                                 │
│ [User can drag-drop documents here - they become RAG corpus]    │
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ What are the key legal precedents for AI liability in...   │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                [Send Message]   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ STEP 2: User Message appears, Team Message shows progress       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ ┌─ User ────────────────────────────────────────────────────┐  │
│ │ What are the key legal precedents for AI liability in...  │  │
│ └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│ ┌─ 📊 Legal Research Team ──────────────────────────────────┐  │
│ │                                                            │  │
│ │  Research in Progress                                      │  │
│ │  ═══════════════════════════════════════════════════════   │  │
│ │  Phase: Research [████████░░] 80%                         │  │
│ │                                                            │  │
│ │  ┌─────────┐  ┌─────────┐  ┌─────────┐                    │  │
│ │  │Deep Diver│  │Synthesizer│ │Devil's  │                    │  │
│ │  │ ⟳ Active │  │ ✓ Done   │ │Advocate │                    │  │
│ │  │ 12 docs  │  │ 8 docs   │ │ ⟳ Active│                    │  │
│ │  └─────────┘  └─────────┘  └─────────┘                    │  │
│ │                                                            │  │
│ │  Recent Activity:                                          │  │
│ │  • Deep Diver: Searching "AI liability cases 2024"        │  │
│ │  • Devil's Advocate: Analyzing counter-arguments          │  │
│ │                                                            │  │
│ │                                          [Cancel Research] │  │
│ └────────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ STEP 3: Research complete, Team Message shows report            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ ┌─ User ────────────────────────────────────────────────────┐  │
│ │ What are the key legal precedents for AI liability in...  │  │
│ └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│ ┌─ 📊 Legal Research Team ──────────────────────────────────┐  │
│ │                                                            │  │
│ │  Research Complete ✓                     [Export ▼] 📄    │  │
│ │  ═══════════════════════════════════════════════════════   │  │
│ │                                                            │  │
│ │  ## AI Liability Legal Precedents                         │  │
│ │                                                            │  │
│ │  ### Executive Summary                                     │  │
│ │  This research examined key legal precedents for AI...    │  │
│ │                                                            │  │
│ │  ### Key Findings                                          │  │
│ │                                                            │  │
│ │  #### Finding 1: [HIGH CONFIDENCE]                        │  │
│ │  Courts have consistently held that...                    │  │
│ │  > "The manufacturer bears responsibility..." - Smith v.  │  │
│ │                                                            │  │
│ │  [▼ Show More]                                            │  │
│ │                                                            │  │
│ │  ─────────────────────────────────────────────────────    │  │
│ │  📊 View Details: [Report] [Peer Reviews] [Trail]         │  │
│ │                                                            │  │
│ └────────────────────────────────────────────────────────────┘  │
│                                                                 │
│ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─  │
│                                                                 │
│ Agent: [🤖 Claude Opus 4 ▼]  ← User switches back to agent     │
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Can you explain the Smith v. case in more detail?          │ │
│ └─────────────────────────────────────────────────────────────┘ │
│                                                [Send Message]   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.3 Mixed Mode Conversations

A single conversation can contain:
- Regular user → assistant exchanges
- Research query → team response exchanges
- User can switch between agents and research teams freely

```
Conversation Timeline:
├─ [User] "What is contract law?"
├─ [Claude Opus] "Contract law is a body of law..."
├─ [User] "Research the key cases for breach of contract in tech industry"
├─ [📊 Legal Research Team] { Research Report... }
├─ [User] "Can you summarize finding #3?"
├─ [Claude Opus] "Finding #3 discusses the Oracle v. Google case..."
└─ [User] "Thanks!"
```

### 3.4 Document Attachment

Documents attached to the conversation become the RAG corpus for research:

1. User attaches documents (existing drag-drop behavior)
2. User selects Research Team
3. User types research query
4. Research agents search attached documents via RAG
5. Sources from documents appear in citations

### 3.5 Conversation Storage

Research appears in conversation history as a special message type:

```csharp
// AesirChatMessage with research extension
public class AesirChatMessage
{
    public string Role { get; set; }  // "user", "assistant", "research_team"
    public string Content { get; set; }

    // Research-specific (when Role == "research_team")
    public Guid? ResearchSessionId { get; set; }  // Links to full research data
    public string? ResearchTeamName { get; set; }
    public ResearchStatus? ResearchStatus { get; set; }
}
```

When `Role == "research_team"`:
- Frontend renders `TeamMessage` component instead of `AssistantMessage`
- `ResearchSessionId` links to full research session data
- Report markdown stored in `Content` for quick display

---

## 4. Architecture Design

### 4.1 Module Structure

#### Backend: `Aesir.Modules.Research`

```
Server/Modules/Aesir.Modules.Research/
├── Aesir.Modules.Research.csproj
├── ResearchModule.cs
│
├── Controllers/
│   ├── ResearchController.cs          # Session management & reports
│   └── ResearchTeamController.cs      # Team configuration CRUD
│
├── Hubs/
│   └── ResearchHub.cs
│
├── Models/
│   ├── ResearchTeam.cs                # Team configuration
│   ├── ResearchTeamMember.cs          # Role assignment with overrides
│   ├── ResearchSession.cs
│   ├── ResearchSubmission.cs
│   ├── PeerReview.cs
│   ├── ResearchReport.cs
│   ├── ResearchPhase.cs
│   ├── ResearchRole.cs
│   ├── AgentActivity.cs
│   ├── ScoringCriteria.cs
│   ├── ConfidenceLevel.cs
│   └── ResearchTrailEntry.cs
│
├── Services/
│   ├── IResearchOrchestrator.cs
│   ├── ResearchOrchestrator.cs
│   ├── IResearchPhaseExecutor.cs
│   ├── ResearchPhaseExecutor.cs
│   ├── IResearchTeamService.cs        # Team CRUD operations
│   ├── ResearchTeamService.cs
│   ├── IAnonymizationService.cs
│   ├── AnonymizationService.cs
│   ├── IPeerReviewService.cs
│   ├── PeerReviewService.cs
│   ├── IReportGeneratorService.cs
│   ├── ReportGeneratorService.cs
│   ├── IResearchAgentFactory.cs       # Builds agents with overrides
│   ├── ResearchAgentFactory.cs
│   ├── IClarificationService.cs
│   ├── ClarificationService.cs
│   ├── IResearchSessionRepository.cs
│   ├── ResearchSessionRepository.cs
│   ├── IResearchTeamRepository.cs     # Team persistence
│   ├── ResearchTeamRepository.cs
│   ├── IResearchProgressBroadcaster.cs
│   └── ResearchProgressBroadcaster.cs
│
├── Agents/
│   ├── ResearchRoleDefinitions.cs     # Default personas/prompts
│   └── ResearchPromptTemplates.cs
│
├── Export/
│   ├── IReportExporter.cs
│   ├── PdfReportExporter.cs
│   └── WordReportExporter.cs
│
└── Migrations/
    └── Migration20250127000001.cs
```

#### Frontend: `Aesir.Client.Web.Modules.Research`

```
Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Research/
├── Aesir.Client.Web.Modules.Research.csproj
├── ResearchModule.cs
├── _Imports.razor
│
├── Pages/
│   └── ResearchTeamsPage.razor        # Settings > Research Teams
│
├── Components/
│   │
│   │ # Team Configuration (Settings)
│   ├── ResearchTeamCard.razor         # Team summary card
│   ├── ResearchTeamEditDialog.razor   # Create/edit team dialog
│   ├── TeamMemberConfig.razor         # Role assignment section
│   ├── OverrideConfigPanel.razor      # Configure override panel
│   │
│   │ # Chat Integration
│   ├── TeamMessage.razor              # Team message bubble (replaces AssistantMessage for research)
│   ├── TeamMessageProgress.razor      # Progress view within TeamMessage
│   ├── TeamMessageReport.razor        # Report view within TeamMessage
│   ├── TeamMessageDetails.razor       # Expandable details (peer reviews, trail)
│   │
│   │ # Shared Components
│   ├── AgentActivityCard.razor
│   ├── PhaseProgressBar.razor
│   ├── FindingCard.razor
│   ├── PeerReviewScoreChart.razor
│   ├── ResearchTrailTimeline.razor
│   ├── ConfidenceBadge.razor
│   └── ExportMenu.razor
│
├── Services/
│   ├── IResearchApiService.cs
│   ├── ResearchApiService.cs
│   ├── IResearchTeamApiService.cs     # Team configuration API
│   ├── ResearchTeamApiService.cs
│   ├── IResearchSignalRService.cs
│   ├── ResearchSignalRService.cs
│   ├── IResearchStateService.cs
│   └── ResearchStateService.cs
│
└── Models/
    ├── ResearchTeamModel.cs
    ├── TeamMemberModel.cs
    └── ResearchProgressModel.cs
```

#### Modifications to Existing Modules

**Chat Module (Frontend)** - `Aesir.Client.Web.Modules.Chat`:
```
Components/
├── AgentSelectorCompact.razor         # MODIFY: Add Research Teams section
├── ChatPage.razor                     # MODIFY: Handle research team selection
└── MessageList.razor                  # MODIFY: Render TeamMessage for research_team role
```

**Infrastructure (Frontend)** - `Aesir.Client.Web.Infrastructure`:
```
Services/
└── IAgentService.cs                   # MODIFY: Add method to get research teams
```

**Common** - `Aesir.Common`:
```
Models/
├── AesirChatMessage.cs                # MODIFY: Add ResearchSessionId, ResearchTeamName, ResearchStatus
└── AesirResearchTeam.cs               # NEW: Shared research team model
```

### 4.2 Module Dependencies

#### Backend Dependencies

```xml
<!-- Aesir.Modules.Research.csproj -->
<ItemGroup>
  <!-- Infrastructure (base services, DB, logging) -->
  <ProjectReference Include="..\..\Aesir.Infrastructure\Aesir.Infrastructure.csproj" />

  <!-- Common models -->
  <ProjectReference Include="..\..\..\Common\Aesir.Common\Aesir.Common.csproj" />
</ItemGroup>

<!-- Runtime dependencies resolved via DI (not project references) -->
<!-- - IInferenceServiceResolver from Aesir.Modules.Inference -->
<!-- - IDocumentCollectionService from Aesir.Modules.Documents -->
<!-- - IConfigurationService from Aesir.Modules.Configuration -->
```

#### Frontend Dependencies

```xml
<!-- Aesir.Client.Web.Modules.Research.csproj -->
<ItemGroup>
  <!-- Infrastructure only -->
  <ProjectReference Include="..\..\Aesir.Client.Web.Infrastructure\Aesir.Client.Web.Infrastructure.csproj" />

  <!-- Shared models -->
  <ProjectReference Include="..\..\..\..\Common\Aesir.Common\Aesir.Common.csproj" />
</ItemGroup>

<ItemGroup>
  <!-- UI components -->
  <PackageReference Include="MudBlazor" Version="8.5.0" />

  <!-- PDF export -->
  <PackageReference Include="QuestPDF" Version="2024.12.0" />
</ItemGroup>
```

### 4.3 Cross-Module Communication

Research Mode needs to invoke inference (for agents) and access documents (RAG). Following AESIR's patterns:

```csharp
// ResearchOrchestrator uses interfaces from Infrastructure, not module references
public class ResearchOrchestrator : IResearchOrchestrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ResearchOrchestrator> _logger;

    // Resolved at runtime, not compile-time
    public async Task<ResearchSubmission> ExecuteAgentResearchAsync(
        ResearchRole role,
        string query,
        Guid sessionId)
    {
        // Get agent configuration based on role
        var agentConfig = await GetAgentForRoleAsync(role);

        // Resolve inference service dynamically
        var chatService = _serviceProvider.GetKeyedService<IChatService>(agentConfig.InferenceEngineId);

        // Execute research...
    }
}
```

### 4.4 SignalR Integration

Research sessions use SignalR for real-time progress updates:

```csharp
// ResearchHub.cs - Auto-discovered and mapped to /hubs/research
public class ResearchHub : Hub
{
    public async Task SubscribeToSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"research-{sessionId}");
    }

    public async Task UnsubscribeFromSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"research-{sessionId}");
    }
}

// ResearchProgressBroadcaster.cs - Used by orchestrator
public class ResearchProgressBroadcaster : IResearchProgressBroadcaster
{
    private readonly IHubContext<ResearchHub> _hubContext;

    public async Task BroadcastPhaseChangeAsync(Guid sessionId, ResearchPhase phase)
    {
        await _hubContext.Clients.Group($"research-{sessionId}")
            .SendAsync("PhaseChanged", new { Phase = phase.ToString(), Timestamp = DateTime.UtcNow });
    }

    public async Task BroadcastAgentActivityAsync(Guid sessionId, AgentActivity activity)
    {
        await _hubContext.Clients.Group($"research-{sessionId}")
            .SendAsync("AgentActivity", activity);
    }

    public async Task BroadcastResearchCompleteAsync(Guid sessionId, Guid reportId)
    {
        await _hubContext.Clients.Group($"research-{sessionId}")
            .SendAsync("ResearchComplete", new { ReportId = reportId });
    }
}
```

---

## 5. Database Schema

### 5.1 Migration File

```csharp
// Migration20250127000001.cs
using FluentMigrator;

namespace Aesir.Modules.Research.Migrations;

[Migration(20250127000001)]
public class AddResearchTables : Migration
{
    public override void Up()
    {
        // ============================================
        // Research Teams (configured in Settings)
        // ============================================
        Create.Table("aesir_research_team")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsString(255).NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(1000).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_aesir_research_team_user_id")
            .OnTable("aesir_research_team")
            .OnColumn("user_id");

        Create.UniqueConstraint("ux_aesir_research_team_user_name")
            .OnTable("aesir_research_team")
            .Columns("user_id", "name");

        // ============================================
        // Research Team Members (role assignments)
        // ============================================
        Create.Table("aesir_research_team_member")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("team_id").AsGuid().NotNullable()
                .ForeignKey("fk_research_team_member_team", "aesir_research_team", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("role").AsString(50).NotNullable() // Chairman, DeepDiver, Synthesizer, DevilsAdvocate
            .WithColumn("agent_id").AsGuid().NotNullable() // References aesir_agent
            // Research Behavior Overrides (nullable = inherit from base agent)
            .WithColumn("override_temperature").AsDouble().Nullable()
            .WithColumn("override_persona").AsString(int.MaxValue).Nullable()
            .WithColumn("override_planning_prompt").AsString(int.MaxValue).Nullable()
            .WithColumn("override_research_prompt").AsString(int.MaxValue).Nullable()
            .WithColumn("override_thinking_mode").AsString(50).Nullable()
            .WithColumn("override_tools_json").AsString(int.MaxValue).Nullable() // JSON array of tool IDs
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_aesir_research_team_member_team_id")
            .OnTable("aesir_research_team_member")
            .OnColumn("team_id");

        Create.UniqueConstraint("ux_aesir_research_team_member_team_role")
            .OnTable("aesir_research_team_member")
            .Columns("team_id", "role");

        // ============================================
        // Research Sessions
        // ============================================
        Create.Table("aesir_research_session")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsString(255).NotNullable()
            .WithColumn("research_team_id").AsGuid().Nullable() // Links to configured team
                .ForeignKey("fk_research_session_team", "aesir_research_team", "id")
                .OnDelete(System.Data.Rule.SetNull)
            .WithColumn("conversation_id").AsGuid().Nullable() // Links to chat conversation
            .WithColumn("query").AsString(4000).NotNullable()
            .WithColumn("refined_query").AsString(4000).Nullable()
            .WithColumn("mode").AsString(50).NotNullable().WithDefaultValue("Standard")
            .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("Created")
            .WithColumn("current_phase").AsString(50).Nullable()
            .WithColumn("document_collection_ids").AsString(int.MaxValue).Nullable() // JSON array
            .WithColumn("clarification_questions").AsString(int.MaxValue).Nullable() // JSON
            .WithColumn("clarification_answers").AsString(int.MaxValue).Nullable() // JSON
            .WithColumn("error_message").AsString(2000).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("started_at").AsDateTime().Nullable()
            .WithColumn("completed_at").AsDateTime().Nullable();

        Create.Index("ix_aesir_research_session_user_id")
            .OnTable("aesir_research_session")
            .OnColumn("user_id");

        Create.Index("ix_aesir_research_session_status")
            .OnTable("aesir_research_session")
            .OnColumn("status");

        Create.Index("ix_aesir_research_session_created_at")
            .OnTable("aesir_research_session")
            .OnColumn("created_at").Descending();

        // ============================================
        // Research Submissions (Agent Work)
        // ============================================
        Create.Table("aesir_research_submission")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("session_id").AsGuid().NotNullable()
                .ForeignKey("fk_research_submission_session", "aesir_research_session", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("agent_id").AsGuid().NotNullable()
            .WithColumn("role").AsString(100).NotNullable() // DeepDiver, Synthesizer, DevilsAdvocate
            .WithColumn("round_number").AsInt32().NotNullable().WithDefaultValue(1)
            .WithColumn("anonymized_id").AsString(10).Nullable() // A, B, C
            .WithColumn("plan").AsString(int.MaxValue).Nullable() // Chain of thought plan
            .WithColumn("content").AsString(int.MaxValue).NotNullable()
            .WithColumn("thinking_trace").AsString(int.MaxValue).Nullable()
            .WithColumn("sources_json").AsString(int.MaxValue).Nullable() // JSON array of citations
            .WithColumn("tool_calls_json").AsString(int.MaxValue).Nullable() // JSON array of tool calls
            .WithColumn("tokens_used").AsInt32().Nullable()
            .WithColumn("duration_ms").AsInt64().Nullable()
            .WithColumn("status").AsString(50).NotNullable().WithDefaultValue("Pending")
            .WithColumn("error_message").AsString(2000).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("completed_at").AsDateTime().Nullable();

        Create.Index("ix_aesir_research_submission_session_id")
            .OnTable("aesir_research_submission")
            .OnColumn("session_id");

        Create.Index("ix_aesir_research_submission_role")
            .OnTable("aesir_research_submission")
            .OnColumn("role");

        // ============================================
        // Peer Reviews
        // ============================================
        Create.Table("aesir_research_peer_review")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("session_id").AsGuid().NotNullable()
                .ForeignKey("fk_research_peer_review_session", "aesir_research_session", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("submission_id").AsGuid().NotNullable()
                .ForeignKey("fk_research_peer_review_submission", "aesir_research_submission", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("reviewer_agent_id").AsGuid().NotNullable()
            .WithColumn("reviewer_role").AsString(100).NotNullable()
            .WithColumn("score_depth").AsDouble().NotNullable()
            .WithColumn("score_accuracy").AsDouble().NotNullable()
            .WithColumn("score_source_quality").AsDouble().NotNullable()
            .WithColumn("score_novelty").AsDouble().NotNullable()
            .WithColumn("score_coherence").AsDouble().NotNullable()
            .WithColumn("weighted_average").AsDouble().NotNullable()
            .WithColumn("strengths").AsString(int.MaxValue).Nullable()
            .WithColumn("improvements").AsString(int.MaxValue).Nullable()
            .WithColumn("critique").AsString(int.MaxValue).NotNullable()
            .WithColumn("endorses").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("tokens_used").AsInt32().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.Index("ix_aesir_research_peer_review_session_id")
            .OnTable("aesir_research_peer_review")
            .OnColumn("session_id");

        Create.Index("ix_aesir_research_peer_review_submission_id")
            .OnTable("aesir_research_peer_review")
            .OnColumn("submission_id");

        // ============================================
        // Research Reports
        // ============================================
        Create.Table("aesir_research_report")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("session_id").AsGuid().NotNullable().Unique()
                .ForeignKey("fk_research_report_session", "aesir_research_session", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("title").AsString(500).NotNullable()
            .WithColumn("executive_summary").AsString(int.MaxValue).NotNullable()
            .WithColumn("methodology_section").AsString(int.MaxValue).NotNullable()
            .WithColumn("findings_json").AsString(int.MaxValue).NotNullable() // JSON array of findings
            .WithColumn("alternative_perspectives").AsString(int.MaxValue).Nullable()
            .WithColumn("research_gaps").AsString(int.MaxValue).Nullable()
            .WithColumn("bibliography_json").AsString(int.MaxValue).Nullable() // JSON array of citations
            .WithColumn("full_markdown").AsString(int.MaxValue).NotNullable()
            .WithColumn("metadata_json").AsString(int.MaxValue).Nullable() // Duration, token counts, etc.
            .WithColumn("tokens_used").AsInt32().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.Index("ix_aesir_research_report_session_id")
            .OnTable("aesir_research_report")
            .OnColumn("session_id");

        // ============================================
        // Research Trail (Audit Log)
        // ============================================
        Create.Table("aesir_research_trail")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("session_id").AsGuid().NotNullable()
                .ForeignKey("fk_research_trail_session", "aesir_research_session", "id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("submission_id").AsGuid().Nullable()
                .ForeignKey("fk_research_trail_submission", "aesir_research_submission", "id")
                .OnDelete(System.Data.Rule.SetNull)
            .WithColumn("event_type").AsString(100).NotNullable() // ToolCall, RAGQuery, WebSearch, PhaseChange
            .WithColumn("agent_role").AsString(100).Nullable()
            .WithColumn("description").AsString(1000).NotNullable()
            .WithColumn("input_json").AsString(int.MaxValue).Nullable()
            .WithColumn("output_json").AsString(int.MaxValue).Nullable()
            .WithColumn("duration_ms").AsInt64().Nullable()
            .WithColumn("timestamp").AsDateTime().NotNullable();

        Create.Index("ix_aesir_research_trail_session_id")
            .OnTable("aesir_research_trail")
            .OnColumn("session_id");

        Create.Index("ix_aesir_research_trail_timestamp")
            .OnTable("aesir_research_trail")
            .OnColumn("timestamp");
    }

    public override void Down()
    {
        Delete.Table("aesir_research_trail");
        Delete.Table("aesir_research_report");
        Delete.Table("aesir_research_peer_review");
        Delete.Table("aesir_research_submission");
        Delete.Table("aesir_research_session");
        Delete.Table("aesir_research_team_member");
        Delete.Table("aesir_research_team");
    }
}
```

### 5.2 Entity Models

```csharp
// ResearchTeam.cs
public class ResearchTeam : IEntity
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public List<ResearchTeamMember>? Members { get; set; }
}

// ResearchTeamMember.cs
public class ResearchTeamMember : IEntity
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public ResearchRole Role { get; set; }
    public Guid AgentId { get; set; } // References base agent

    // Behavior Overrides (null = inherit from base agent)
    public double? OverrideTemperature { get; set; }
    public string? OverridePersona { get; set; }
    public string? OverridePlanningPrompt { get; set; }
    public string? OverrideResearchPrompt { get; set; }
    public string? OverrideThinkingMode { get; set; }
    public List<string>? OverrideTools { get; set; } // Tool IDs to use (subset of agent tools)

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ResearchTeam? Team { get; set; }
}
```

```csharp
// ResearchSession.cs
public class ResearchSession : IEntity
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid? ResearchTeamId { get; set; }  // Links to configured team
    public Guid? ConversationId { get; set; }  // Links to chat conversation
    public string Query { get; set; } = string.Empty;
    public string? RefinedQuery { get; set; }
    public ResearchMode Mode { get; set; } = ResearchMode.Standard;
    public ResearchStatus Status { get; set; } = ResearchStatus.Created;
    public ResearchPhase? CurrentPhase { get; set; }
    public List<Guid>? DocumentCollectionIds { get; set; }
    public List<string>? ClarificationQuestions { get; set; }
    public Dictionary<string, string>? ClarificationAnswers { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation properties (not stored in DB, populated by repository)
    public ResearchTeam? ResearchTeam { get; set; }
    public List<ResearchSubmission>? Submissions { get; set; }
    public List<PeerReview>? PeerReviews { get; set; }
    public ResearchReport? Report { get; set; }
}

public enum ResearchMode
{
    Quick,      // Future: 2 agents, no peer review
    Standard,   // MVP: 3 agents + peer review
    Deep        // Future: Multi-round with gap filling
}

public enum ResearchStatus
{
    Created,
    AwaitingClarification,
    Planning,
    Researching,
    Anonymizing,
    PeerReviewing,
    Synthesizing,
    Completed,
    Failed,
    Cancelled
}

public enum ResearchPhase
{
    Clarification,
    Planning,
    Research,
    Anonymization,
    PeerReview,
    Synthesis
}
```

```csharp
// ResearchSubmission.cs
public class ResearchSubmission : IEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid AgentId { get; set; }
    public ResearchRole Role { get; set; }
    public int RoundNumber { get; set; } = 1;
    public string? AnonymizedId { get; set; } // A, B, C
    public string? Plan { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ThinkingTrace { get; set; }
    public List<ResearchSource>? Sources { get; set; }
    public List<ResearchToolCall>? ToolCalls { get; set; }
    public int? TokensUsed { get; set; }
    public long? DurationMs { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum ResearchRole
{
    DeepDiver,
    Synthesizer,
    DevilsAdvocate,
    Chairman
}

public enum SubmissionStatus
{
    Pending,
    Planning,
    Researching,
    Completed,
    Failed
}
```

```csharp
// PeerReview.cs
public class PeerReview : IEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid ReviewerAgentId { get; set; }
    public ResearchRole ReviewerRole { get; set; }

    // Scores (1-10)
    public double ScoreDepth { get; set; }
    public double ScoreAccuracy { get; set; }
    public double ScoreSourceQuality { get; set; }
    public double ScoreNovelty { get; set; }
    public double ScoreCoherence { get; set; }
    public double WeightedAverage { get; set; }

    // Qualitative feedback
    public List<string>? Strengths { get; set; }
    public List<string>? Improvements { get; set; }
    public string Critique { get; set; } = string.Empty;
    public bool Endorses { get; set; } = true;

    public int? TokensUsed { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

```csharp
// ResearchReport.cs
public class ResearchReport : IEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ExecutiveSummary { get; set; } = string.Empty;
    public string MethodologySection { get; set; } = string.Empty;
    public List<ResearchFinding> Findings { get; set; } = new();
    public string? AlternativePerspectives { get; set; }
    public string? ResearchGaps { get; set; }
    public List<ResearchSource>? Bibliography { get; set; }
    public string FullMarkdown { get; set; } = string.Empty;
    public ResearchReportMetadata? Metadata { get; set; }
    public int? TokensUsed { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ResearchFinding
{
    public string Title { get; set; } = string.Empty;
    public ConfidenceLevel Confidence { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<ResearchSource> Evidence { get; set; } = new();
    public string? PeerReviewNotes { get; set; }
    public string? DissentingView { get; set; }
}

public enum ConfidenceLevel
{
    High,       // 90-100%: Multiple sources, unanimous peer support
    Medium,     // 60-89%: Majority support, some dissent
    Low,        // 40-59%: Limited evidence, significant uncertainty
    Speculative // <40%: Hypothetical, requires validation
}
```

---

## 6. Backend Implementation

### 6.1 Research Module Registration

```csharp
// ResearchModule.cs
using Aesir.Infrastructure.Modules;
using Aesir.Modules.Research.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research;

public class ResearchModule : ModuleBase
{
    public ResearchModule(ILogger logger) : base(logger) { }

    public override string Name => "Research";
    public override string Version => "1.0.0";
    public override string? Description =>
        "Multi-agent research orchestration with peer review and professional report generation";

    public override Task RegisterServicesAsync(IServiceCollection services)
    {
        Log("Registering Research services...");

        // Core orchestration
        services.AddScoped<IResearchOrchestrator, ResearchOrchestrator>();
        services.AddScoped<IResearchPhaseExecutor, ResearchPhaseExecutor>();

        // Agent services
        services.AddScoped<IResearchAgentFactory, ResearchAgentFactory>();
        services.AddScoped<IClarificationService, ClarificationService>();

        // Processing services
        services.AddScoped<IAnonymizationService, AnonymizationService>();
        services.AddScoped<IPeerReviewService, PeerReviewService>();
        services.AddScoped<IReportGeneratorService, ReportGeneratorService>();

        // Repository
        services.AddScoped<IResearchSessionRepository, ResearchSessionRepository>();

        // SignalR broadcaster
        services.AddSingleton<IResearchProgressBroadcaster, ResearchProgressBroadcaster>();

        // Export services
        services.AddScoped<IReportExporter, PdfReportExporter>();
        services.AddKeyedScoped<IReportExporter, WordReportExporter>("word");

        Log("Research services registered successfully");
        return Task.CompletedTask;
    }

    public override void Initialize(IApplicationBuilder app)
    {
        Log("Research module initialized");
        // Hub mapping handled automatically by Program.cs hub discovery
    }
}
```

### 6.2 API Controller

```csharp
// ResearchController.cs
using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aesir.Modules.Research.Controllers;

[ApiController]
[Route("api/research")]
public class ResearchController : ControllerBase
{
    private readonly IResearchOrchestrator _orchestrator;
    private readonly IResearchSessionRepository _repository;
    private readonly IReportExporter _pdfExporter;
    private readonly ILogger<ResearchController> _logger;

    public ResearchController(
        IResearchOrchestrator orchestrator,
        IResearchSessionRepository repository,
        IReportExporter pdfExporter,
        ILogger<ResearchController> logger)
    {
        _orchestrator = orchestrator;
        _repository = repository;
        _pdfExporter = pdfExporter;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new research session.
    /// </summary>
    [HttpPost("sessions")]
    public async Task<ActionResult<ResearchSession>> CreateSession(
        [FromBody] CreateResearchSessionRequest request)
    {
        var session = await _orchestrator.CreateSessionAsync(request);
        return CreatedAtAction(nameof(GetSession), new { sessionId = session.Id }, session);
    }

    /// <summary>
    /// Gets a research session by ID.
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}")]
    public async Task<ActionResult<ResearchSession>> GetSession(Guid sessionId)
    {
        var session = await _repository.GetByIdAsync(sessionId);
        if (session == null)
            return NotFound();
        return Ok(session);
    }

    /// <summary>
    /// Lists research sessions for a user.
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<List<ResearchSessionSummary>>> ListSessions(
        [FromQuery] string userId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var sessions = await _repository.GetByUserIdAsync(userId, skip, take);
        return Ok(sessions);
    }

    /// <summary>
    /// Submits clarification answers and starts research.
    /// </summary>
    [HttpPost("sessions/{sessionId:guid}/clarify")]
    public async Task<ActionResult> SubmitClarification(
        Guid sessionId,
        [FromBody] SubmitClarificationRequest request)
    {
        await _orchestrator.SubmitClarificationAsync(sessionId, request.Answers);
        return Accepted();
    }

    /// <summary>
    /// Starts the research process (if no clarification needed).
    /// </summary>
    [HttpPost("sessions/{sessionId:guid}/start")]
    public async Task<ActionResult> StartResearch(Guid sessionId)
    {
        await _orchestrator.StartResearchAsync(sessionId);
        return Accepted();
    }

    /// <summary>
    /// Cancels an in-progress research session.
    /// </summary>
    [HttpPost("sessions/{sessionId:guid}/cancel")]
    public async Task<ActionResult> CancelResearch(Guid sessionId)
    {
        await _orchestrator.CancelAsync(sessionId);
        return Ok();
    }

    /// <summary>
    /// Gets the current status of a research session.
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}/status")]
    public async Task<ActionResult<ResearchSessionStatus>> GetStatus(Guid sessionId)
    {
        var status = await _orchestrator.GetStatusAsync(sessionId);
        if (status == null)
            return NotFound();
        return Ok(status);
    }

    /// <summary>
    /// Gets the final report for a completed research session.
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}/report")]
    public async Task<ActionResult<ResearchReport>> GetReport(Guid sessionId)
    {
        var report = await _repository.GetReportAsync(sessionId);
        if (report == null)
            return NotFound();
        return Ok(report);
    }

    /// <summary>
    /// Exports the report in the specified format.
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}/export")]
    public async Task<IActionResult> ExportReport(
        Guid sessionId,
        [FromQuery] string format = "pdf")
    {
        var report = await _repository.GetReportAsync(sessionId);
        if (report == null)
            return NotFound();

        var exporter = format.ToLowerInvariant() switch
        {
            "word" or "docx" => HttpContext.RequestServices
                .GetKeyedService<IReportExporter>("word"),
            _ => _pdfExporter
        };

        var (bytes, contentType, fileName) = await exporter!.ExportAsync(report);
        return File(bytes, contentType, fileName);
    }

    /// <summary>
    /// Gets the research trail (audit log) for a session.
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}/trail")]
    public async Task<ActionResult<List<ResearchTrailEntry>>> GetResearchTrail(Guid sessionId)
    {
        var trail = await _repository.GetTrailAsync(sessionId);
        return Ok(trail);
    }

    /// <summary>
    /// Gets all submissions for a research session.
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}/submissions")]
    public async Task<ActionResult<List<ResearchSubmission>>> GetSubmissions(Guid sessionId)
    {
        var submissions = await _repository.GetSubmissionsAsync(sessionId);
        return Ok(submissions);
    }

    /// <summary>
    /// Gets all peer reviews for a research session.
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}/reviews")]
    public async Task<ActionResult<List<PeerReview>>> GetPeerReviews(Guid sessionId)
    {
        var reviews = await _repository.GetPeerReviewsAsync(sessionId);
        return Ok(reviews);
    }

    /// <summary>
    /// Deletes a research session and all related data.
    /// </summary>
    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<ActionResult> DeleteSession(Guid sessionId)
    {
        await _repository.DeleteAsync(sessionId);
        return NoContent();
    }
}
```

### 6.3 Research Orchestrator

```csharp
// IResearchOrchestrator.cs
public interface IResearchOrchestrator
{
    Task<ResearchSession> CreateSessionAsync(CreateResearchSessionRequest request);
    Task SubmitClarificationAsync(Guid sessionId, Dictionary<string, string> answers);
    Task StartResearchAsync(Guid sessionId);
    Task CancelAsync(Guid sessionId);
    Task<ResearchSessionStatus?> GetStatusAsync(Guid sessionId);
}

// ResearchOrchestrator.cs (core orchestration logic)
public class ResearchOrchestrator : IResearchOrchestrator
{
    private readonly IResearchSessionRepository _repository;
    private readonly IResearchPhaseExecutor _phaseExecutor;
    private readonly IClarificationService _clarificationService;
    private readonly IResearchProgressBroadcaster _broadcaster;
    private readonly ILogger<ResearchOrchestrator> _logger;

    public async Task<ResearchSession> CreateSessionAsync(CreateResearchSessionRequest request)
    {
        var session = new ResearchSession
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Query = request.Query,
            Mode = ResearchMode.Standard, // MVP: Standard only
            Status = ResearchStatus.Created,
            DocumentCollectionIds = request.DocumentCollectionIds,
            ChairmanAgentId = request.ChairmanAgentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(session);

        // Generate clarification questions
        var questions = await _clarificationService.GenerateClarificationsAsync(session);

        if (questions.Any())
        {
            session.ClarificationQuestions = questions;
            session.Status = ResearchStatus.AwaitingClarification;
            await _repository.UpdateAsync(session);
        }
        else
        {
            // No clarification needed, start immediately
            _ = Task.Run(() => ExecuteResearchAsync(session.Id));
        }

        return session;
    }

    public async Task SubmitClarificationAsync(Guid sessionId, Dictionary<string, string> answers)
    {
        var session = await _repository.GetByIdAsync(sessionId)
            ?? throw new InvalidOperationException($"Session {sessionId} not found");

        session.ClarificationAnswers = answers;
        session.RefinedQuery = await _clarificationService.RefineQueryAsync(session, answers);
        session.Status = ResearchStatus.Planning;
        session.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(session);

        // Start research in background
        _ = Task.Run(() => ExecuteResearchAsync(sessionId));
    }

    public async Task StartResearchAsync(Guid sessionId)
    {
        var session = await _repository.GetByIdAsync(sessionId)
            ?? throw new InvalidOperationException($"Session {sessionId} not found");

        if (session.Status != ResearchStatus.Created)
            throw new InvalidOperationException($"Session is in {session.Status} status, cannot start");

        session.Status = ResearchStatus.Planning;
        session.StartedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(session);

        // Start research in background
        _ = Task.Run(() => ExecuteResearchAsync(sessionId));
    }

    private async Task ExecuteResearchAsync(Guid sessionId)
    {
        try
        {
            // Phase 1: Planning
            await _phaseExecutor.ExecutePlanningPhaseAsync(sessionId);

            // Phase 2: Research (parallel agent execution)
            await _phaseExecutor.ExecuteResearchPhaseAsync(sessionId);

            // Phase 3: Anonymization
            await _phaseExecutor.ExecuteAnonymizationPhaseAsync(sessionId);

            // Phase 4: Peer Review
            await _phaseExecutor.ExecutePeerReviewPhaseAsync(sessionId);

            // Phase 5: Synthesis
            await _phaseExecutor.ExecuteSynthesisPhaseAsync(sessionId);

            // Mark complete
            var session = await _repository.GetByIdAsync(sessionId);
            session!.Status = ResearchStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(session);

            var report = await _repository.GetReportAsync(sessionId);
            await _broadcaster.BroadcastResearchCompleteAsync(sessionId, report!.Id);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Research session {SessionId} was cancelled", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Research session {SessionId} failed", sessionId);

            var session = await _repository.GetByIdAsync(sessionId);
            if (session != null)
            {
                session.Status = ResearchStatus.Failed;
                session.ErrorMessage = ex.Message;
                session.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(session);
            }

            await _broadcaster.BroadcastErrorAsync(sessionId, ex.Message);
        }
    }

    // ... other methods
}
```

### 6.4 Agent Role Definitions

```csharp
// ResearchRoleDefinitions.cs
namespace Aesir.Modules.Research.Agents;

public static class ResearchRoleDefinitions
{
    public static ResearchAgentConfig GetConfig(ResearchRole role) => role switch
    {
        ResearchRole.DeepDiver => new ResearchAgentConfig
        {
            Role = ResearchRole.DeepDiver,
            Name = "Deep Diver",
            Temperature = 0.3,
            Persona = @"You are a meticulous research specialist known for exhaustive, thorough investigation.
Your approach is methodical and comprehensive - you leave no stone unturned.

Your strengths:
- Deep primary source analysis
- Extracting every relevant detail from documents
- Following citation chains to original sources
- Identifying subtle nuances others might miss

Your research style:
- Start broad, then go deep on each promising lead
- Document everything you find with precise citations
- Note confidence levels for each finding
- Flag areas requiring further investigation",

            PlanningPrompt = @"Create a detailed chain-of-thought research plan for the following query:

{{QUERY}}

Your plan should:
1. Break down the query into specific sub-questions
2. Identify the types of sources needed
3. Define success criteria for each sub-question
4. Outline your investigation sequence
5. Note potential challenges and how to address them

Format your plan as a numbered list with clear, actionable steps.",

            ResearchPrompt = @"Execute your research plan for the following query:

{{QUERY}}

{{REFINED_CONTEXT}}

Available tools: Document search (RAG), Web search, MCP tools

Instructions:
1. Follow your plan systematically
2. Document every source with full citation
3. Extract relevant quotes and data
4. Note your reasoning process
5. Be exhaustive - find everything relevant

Produce a comprehensive research report with:
- Key findings (with confidence levels)
- Supporting evidence for each finding
- Direct quotes from sources
- Any gaps or limitations in available information"
        },

        ResearchRole.Synthesizer => new ResearchAgentConfig
        {
            Role = ResearchRole.Synthesizer,
            Name = "Synthesizer",
            Temperature = 0.5,
            Persona = @"You are an interdisciplinary research synthesizer with a talent for connecting disparate ideas.
Your unique skill is seeing patterns and relationships that others miss.

Your strengths:
- Cross-domain pattern recognition
- Finding unexpected connections between sources
- Building coherent narratives from diverse information
- Identifying emerging trends and implications

Your research style:
- Cast a wide net across domains
- Look for structural similarities between different areas
- Build mental models that explain multiple phenomena
- Always ask 'what does this connect to?'",

            PlanningPrompt = @"Create a synthesis-focused research plan for:

{{QUERY}}

Your plan should:
1. Identify multiple domains/perspectives to explore
2. Define connection points to investigate
3. Outline how you'll build a unified understanding
4. Note potential cross-domain insights to seek
5. Plan for unexpected discoveries

Format as a numbered plan with exploration pathways.",

            ResearchPrompt = @"Conduct synthesis-focused research on:

{{QUERY}}

{{REFINED_CONTEXT}}

Your mission: Find connections, patterns, and insights that emerge from combining multiple sources.

Instructions:
1. Explore broadly across available sources
2. Actively seek cross-domain connections
3. Build a coherent narrative from diverse inputs
4. Highlight unexpected insights
5. Connect findings to broader implications

Produce a synthesis report with:
- Integrated findings (showing how pieces connect)
- Pattern analysis
- Cross-domain insights
- Implications and applications"
        },

        ResearchRole.DevilsAdvocate => new ResearchAgentConfig
        {
            Role = ResearchRole.DevilsAdvocate,
            Name = "Devil's Advocate",
            Temperature = 0.4,
            Persona = @"You are a critical analyst whose role is to challenge assumptions and find weaknesses.
You approach every claim with healthy skepticism and seek alternative explanations.

Your strengths:
- Identifying logical fallacies and weak arguments
- Finding contradictory evidence
- Proposing alternative hypotheses
- Stress-testing conclusions

Your research style:
- Question everything, especially 'obvious' claims
- Actively seek disconfirming evidence
- Consider what could make findings wrong
- Propose the strongest counter-arguments",

            PlanningPrompt = @"Create a critical analysis plan for:

{{QUERY}}

Your plan should:
1. Identify assumptions to challenge
2. Define what would disprove likely conclusions
3. Outline alternative hypotheses to investigate
4. Plan for finding contradictory evidence
5. Note potential biases to account for

Format as a numbered critical investigation plan.",

            ResearchPrompt = @"Conduct critical analysis research on:

{{QUERY}}

{{REFINED_CONTEXT}}

Your mission: Challenge assumptions, find weaknesses, and propose alternatives.

Instructions:
1. Identify and question key assumptions
2. Actively seek contradictory evidence
3. Propose alternative explanations
4. Find limitations in sources
5. Stress-test any conclusions

Produce a critical analysis report with:
- Challenged assumptions and their validity
- Contradictory evidence found
- Alternative hypotheses
- Weaknesses in the overall research question
- Recommendations for more robust conclusions"
        },

        ResearchRole.Chairman => new ResearchAgentConfig
        {
            Role = ResearchRole.Chairman,
            Name = "Research Chairman",
            Temperature = 0.2,
            Persona = @"You are a senior research director responsible for synthesizing team findings into
authoritative, professional reports. You evaluate evidence quality, resolve contradictions,
and produce clear, actionable insights.

Your strengths:
- Meta-analysis and synthesis
- Evaluating evidence quality
- Resolving contradictory findings
- Professional report writing
- Identifying consensus and dissent",

            ClarificationPrompt = @"Analyze the following research query and generate 2-4 clarifying questions
that would help focus the research:

Query: {{QUERY}}

Consider:
- Scope ambiguity (too broad? unclear boundaries?)
- Missing context (time period? geography? industry?)
- Success criteria (what would a good answer look like?)
- Prioritization (if limited time, what's most important?)

Return questions as a JSON array of strings, or empty array if query is clear enough.",

            SynthesisPrompt = @"You are synthesizing research from multiple agents on:

{{QUERY}}

## Agent Submissions and Peer Reviews

{{SUBMISSIONS_WITH_REVIEWS}}

## Your Task

Create a comprehensive, professional research report that:

1. **Executive Summary** (2-3 paragraphs)
   - Key findings and their confidence levels
   - Most important insights
   - Any significant disagreements

2. **Methodology**
   - How the research was conducted
   - Tools and sources used
   - Limitations of the approach

3. **Key Findings** (organized by theme)
   - Each finding with confidence level (High/Medium/Low/Speculative)
   - Supporting evidence with citations
   - Note peer review consensus/dissent

4. **Alternative Perspectives**
   - Devil's Advocate findings that merit consideration
   - Minority views from peer review

5. **Research Gaps**
   - Areas requiring further investigation
   - Limitations of current findings

6. **Bibliography**
   - All sources in consistent citation format

Format the entire report in clean, professional Markdown suitable for PDF export."
        },

        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
}

public class ResearchAgentConfig
{
    public ResearchRole Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public string Persona { get; set; } = string.Empty;
    public string? PlanningPrompt { get; set; }
    public string? ResearchPrompt { get; set; }
    public string? ClarificationPrompt { get; set; }
    public string? SynthesisPrompt { get; set; }
}
```

### 6.5 Peer Review Prompt Template

```csharp
// ResearchPromptTemplates.cs (partial)
public static class ResearchPromptTemplates
{
    public static string PeerReviewPrompt => @"
You are evaluating research submission ""{{SUBMISSION_ID}}"" on the topic:

{{QUERY}}

## Submission Content

{{SUBMISSION_CONTENT}}

## Evaluation Criteria

Rate each criterion from 1-10:

### 1. Depth (How thoroughly was the topic explored?)
Score: [1-10]
Justification: [2-3 sentences]

### 2. Accuracy (How well are claims supported by evidence?)
Score: [1-10]
Justification: [2-3 sentences]

### 3. Source Quality (Are sources authoritative and relevant?)
Score: [1-10]
Justification: [2-3 sentences]

### 4. Novelty (Were unique or unexpected insights uncovered?)
Score: [1-10]
Justification: [2-3 sentences]

### 5. Coherence (Is the argument well-structured and clear?)
Score: [1-10]
Justification: [2-3 sentences]

## Qualitative Assessment

### Strengths
[3-5 bullet points on what this submission does well]

### Suggested Improvements
[3-5 bullet points on how this could be improved]

### Overall Critique
[2-3 paragraph assessment of the submission's contribution]

### Endorsement
Do you endorse including these findings in the final report? [Yes/No]
If No, explain why.

Format your response as valid JSON matching this structure:
{
    ""scoreDepth"": 8.5,
    ""scoreAccuracy"": 9.0,
    ""scoreSourceQuality"": 7.5,
    ""scoreNovelty"": 8.0,
    ""scoreCoherence"": 8.5,
    ""strengths"": [""..."", ""...""],
    ""improvements"": [""..."", ""...""],
    ""critique"": ""..."",
    ""endorses"": true
}";
}
```

---

## 7. Frontend Implementation

### 7.1 Research Module Registration

```csharp
// ResearchModule.cs
using Aesir.Client.Web.Infrastructure.Modules;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Research.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aesir.Client.Web.Modules.Research;

public class ResearchModule : ClientModuleBase
{
    public override string Name => "Research";
    public override string Version => "1.0.0";
    public override string Description => "Multi-agent research orchestration with peer review";

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IResearchApiService, ResearchApiService>();
        services.AddScoped<IResearchSignalRService, ResearchSignalRService>();
        services.AddSingleton<IResearchStateService, ResearchStateService>();
    }

    public override void RegisterNavigation(INavigationRegistry registry)
    {
        registry.Register(new NavigationItem
        {
            Title = "Research",
            Href = "/research",
            Icon = "Science",
            Priority = 20,
            Group = "Main"
        });
    }
}
```

### 7.2 Main Research Page

```razor
@* ResearchPage.razor *@
@page "/research"
@page "/research/{SessionId:guid}"
@using Aesir.Client.Web.Modules.Research.Services
@using Aesir.Client.Web.Modules.Research.Components
@inject IResearchApiService ResearchApi
@inject IResearchSignalRService SignalR
@inject IResearchStateService StateService
@inject ISnackbar Snackbar
@inject NavigationManager Navigation

<PageTitle>Research Mode - AESIR</PageTitle>

<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="py-4">
    @if (_loading)
    {
        <MudProgressLinear Color="Color.Primary" Indeterminate="true" />
    }
    else if (_session == null)
    {
        <ResearchWelcome OnStartResearch="@ShowSetupDialog" />
    }
    else
    {
        @switch (_session.Status)
        {
            case ResearchStatus.AwaitingClarification:
                <ClarificationDialog
                    Session="@_session"
                    OnSubmit="@HandleClarificationSubmit"
                    OnCancel="@HandleCancel" />
                break;

            case ResearchStatus.Planning:
            case ResearchStatus.Researching:
            case ResearchStatus.Anonymizing:
            case ResearchStatus.PeerReviewing:
            case ResearchStatus.Synthesizing:
                <ResearchProgressView
                    Session="@_session"
                    Activities="@_activities"
                    OnCancel="@HandleCancel" />
                break;

            case ResearchStatus.Completed:
                <ResearchReportView
                    Session="@_session"
                    Report="@_report"
                    OnExport="@HandleExport"
                    OnNewResearch="@ShowSetupDialog" />
                break;

            case ResearchStatus.Failed:
                <ResearchErrorView
                    Session="@_session"
                    OnRetry="@HandleRetry"
                    OnNewResearch="@ShowSetupDialog" />
                break;
        }
    }
</MudContainer>

<ResearchSetupDialog
    @bind-IsVisible="@_showSetupDialog"
    OnStart="@HandleStartResearch" />

@code {
    [Parameter]
    public Guid? SessionId { get; set; }

    private ResearchSession? _session;
    private ResearchReport? _report;
    private List<AgentActivity> _activities = new();
    private bool _loading = true;
    private bool _showSetupDialog;

    protected override async Task OnInitializedAsync()
    {
        if (SessionId.HasValue)
        {
            await LoadSessionAsync(SessionId.Value);
        }
        else
        {
            _loading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _session != null)
        {
            await SubscribeToUpdatesAsync(_session.Id);
        }
    }

    private async Task LoadSessionAsync(Guid sessionId)
    {
        _loading = true;
        try
        {
            _session = await ResearchApi.GetSessionAsync(sessionId);
            if (_session?.Status == ResearchStatus.Completed)
            {
                _report = await ResearchApi.GetReportAsync(sessionId);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to load session: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SubscribeToUpdatesAsync(Guid sessionId)
    {
        await SignalR.SubscribeAsync(sessionId);

        SignalR.OnPhaseChanged += HandlePhaseChanged;
        SignalR.OnAgentActivity += HandleAgentActivity;
        SignalR.OnResearchComplete += HandleResearchComplete;
        SignalR.OnError += HandleError;
    }

    private void HandlePhaseChanged(ResearchPhase phase)
    {
        if (_session != null)
        {
            _session.CurrentPhase = phase;
            _session.Status = phase switch
            {
                ResearchPhase.Planning => ResearchStatus.Planning,
                ResearchPhase.Research => ResearchStatus.Researching,
                ResearchPhase.Anonymization => ResearchStatus.Anonymizing,
                ResearchPhase.PeerReview => ResearchStatus.PeerReviewing,
                ResearchPhase.Synthesis => ResearchStatus.Synthesizing,
                _ => _session.Status
            };
            InvokeAsync(StateHasChanged);
        }
    }

    private void HandleAgentActivity(AgentActivity activity)
    {
        _activities.Add(activity);
        InvokeAsync(StateHasChanged);
    }

    private async void HandleResearchComplete(Guid reportId)
    {
        if (_session != null)
        {
            _session.Status = ResearchStatus.Completed;
            _report = await ResearchApi.GetReportAsync(_session.Id);
            await InvokeAsync(StateHasChanged);
        }
    }

    private void HandleError(string message)
    {
        Snackbar.Add(message, Severity.Error);
        if (_session != null)
        {
            _session.Status = ResearchStatus.Failed;
            _session.ErrorMessage = message;
            InvokeAsync(StateHasChanged);
        }
    }

    private void ShowSetupDialog()
    {
        _showSetupDialog = true;
    }

    private async Task HandleStartResearch(CreateResearchSessionRequest request)
    {
        _showSetupDialog = false;
        _loading = true;

        try
        {
            _session = await ResearchApi.CreateSessionAsync(request);
            Navigation.NavigateTo($"/research/{_session.Id}");
            await SubscribeToUpdatesAsync(_session.Id);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to start research: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task HandleClarificationSubmit(Dictionary<string, string> answers)
    {
        if (_session == null) return;

        try
        {
            await ResearchApi.SubmitClarificationAsync(_session.Id, answers);
            _session.Status = ResearchStatus.Planning;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to submit clarification: {ex.Message}", Severity.Error);
        }
    }

    private async Task HandleCancel()
    {
        if (_session == null) return;

        try
        {
            await ResearchApi.CancelSessionAsync(_session.Id);
            _session.Status = ResearchStatus.Cancelled;
            Snackbar.Add("Research cancelled", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to cancel: {ex.Message}", Severity.Error);
        }
    }

    private async Task HandleExport(string format)
    {
        if (_session == null) return;

        try
        {
            var bytes = await ResearchApi.ExportReportAsync(_session.Id, format);
            // Trigger download via JS interop
            await StateService.DownloadFileAsync(bytes, $"research-report.{format}");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task HandleRetry()
    {
        if (_session == null) return;
        await ResearchApi.StartResearchAsync(_session.Id);
        _session.Status = ResearchStatus.Planning;
    }

    public async ValueTask DisposeAsync()
    {
        if (_session != null)
        {
            await SignalR.UnsubscribeAsync(_session.Id);
        }

        SignalR.OnPhaseChanged -= HandlePhaseChanged;
        SignalR.OnAgentActivity -= HandleAgentActivity;
        SignalR.OnResearchComplete -= HandleResearchComplete;
        SignalR.OnError -= HandleError;
    }
}
```

### 7.3 Research Progress View Component

```razor
@* ResearchProgressView.razor *@
<MudPaper Class="pa-4 mb-4">
    <MudText Typo="Typo.h5" Class="mb-2">Research in Progress</MudText>
    <MudText Typo="Typo.body2" Color="Color.Secondary">@Session.Query</MudText>
</MudPaper>

<PhaseProgressBar CurrentPhase="@Session.CurrentPhase" />

<MudGrid Class="mt-4">
    @foreach (var role in new[] { ResearchRole.DeepDiver, ResearchRole.Synthesizer, ResearchRole.DevilsAdvocate })
    {
        <MudItem xs="12" md="4">
            <AgentActivityCard
                Role="@role"
                Activities="@GetActivitiesForRole(role)"
                IsActive="@IsRoleActive(role)" />
        </MudItem>
    }
</MudGrid>

<MudStack Row="true" Justify="Justify.Center" Class="mt-4">
    <MudButton
        Variant="Variant.Outlined"
        Color="Color.Error"
        OnClick="@OnCancel">
        Cancel Research
    </MudButton>
</MudStack>

@code {
    [Parameter] public ResearchSession Session { get; set; } = null!;
    [Parameter] public List<AgentActivity> Activities { get; set; } = new();
    [Parameter] public EventCallback OnCancel { get; set; }

    private IEnumerable<AgentActivity> GetActivitiesForRole(ResearchRole role)
        => Activities.Where(a => a.Role == role).TakeLast(5);

    private bool IsRoleActive(ResearchRole role)
        => Session.CurrentPhase == ResearchPhase.Research ||
           Session.CurrentPhase == ResearchPhase.PeerReview;
}
```

### 7.4 Research Report View Component

```razor
@* ResearchReportView.razor *@
@using Markdig
@inject IMarkdownService MarkdownService

<MudPaper Class="pa-4 mb-4">
    <MudStack Row="true" Justify="Justify.SpaceBetween" AlignItems="AlignItems.Center">
        <div>
            <MudText Typo="Typo.h4">@Report.Title</MudText>
            <MudText Typo="Typo.caption" Color="Color.Secondary">
                Completed @Report.CreatedAt.ToLocalTime().ToString("MMM d, yyyy 'at' h:mm tt")
            </MudText>
        </div>
        <MudStack Row="true" Spacing="2">
            <ExportMenu OnExport="@OnExport" />
            <MudButton
                Variant="Variant.Filled"
                Color="Color.Primary"
                StartIcon="@Icons.Material.Filled.Add"
                OnClick="@OnNewResearch">
                New Research
            </MudButton>
        </MudStack>
    </MudStack>
</MudPaper>

<MudTabs Elevation="2" Rounded="true" ApplyEffectsToContainer="true" PanelClass="pa-4">
    <MudTabPanel Text="Report">
        <div class="research-report-content">
            @((MarkupString)MarkdownService.RenderToHtml(Report.FullMarkdown))
        </div>
    </MudTabPanel>

    <MudTabPanel Text="Peer Reviews">
        <PeerReviewScoreChart SessionId="@Session.Id" />
    </MudTabPanel>

    <MudTabPanel Text="Research Trail">
        <ResearchTrailTimeline SessionId="@Session.Id" />
    </MudTabPanel>

    <MudTabPanel Text="Raw Submissions">
        @foreach (var submission in _submissions)
        {
            <MudExpansionPanel Text="@($"{submission.Role}: {submission.AnonymizedId}")">
                <div class="submission-content">
                    @((MarkupString)MarkdownService.RenderToHtml(submission.Content))
                </div>
            </MudExpansionPanel>
        }
    </MudTabPanel>
</MudTabs>

<style>
    .research-report-content {
        max-width: 900px;
        margin: 0 auto;
        line-height: 1.7;
    }

    .research-report-content h1 { font-size: 2rem; margin-top: 2rem; }
    .research-report-content h2 { font-size: 1.5rem; margin-top: 1.5rem; border-bottom: 1px solid #e0e0e0; padding-bottom: 0.5rem; }
    .research-report-content h3 { font-size: 1.25rem; margin-top: 1rem; }
    .research-report-content blockquote { border-left: 4px solid #1976d2; padding-left: 1rem; margin: 1rem 0; }
    .research-report-content table { width: 100%; border-collapse: collapse; margin: 1rem 0; }
    .research-report-content th, .research-report-content td { border: 1px solid #e0e0e0; padding: 0.5rem; }
    .research-report-content code { background: #f5f5f5; padding: 0.2rem 0.4rem; border-radius: 4px; }
</style>

@code {
    [Parameter] public ResearchSession Session { get; set; } = null!;
    [Parameter] public ResearchReport Report { get; set; } = null!;
    [Parameter] public EventCallback<string> OnExport { get; set; }
    [Parameter] public EventCallback OnNewResearch { get; set; }

    private List<ResearchSubmission> _submissions = new();

    protected override async Task OnInitializedAsync()
    {
        // Load submissions for the raw view tab
        // _submissions = await ResearchApi.GetSubmissionsAsync(Session.Id);
    }
}
```

---

## 8. Implementation Phases

### Phase 1: Foundation (Backend Scaffolding)
**Estimated Effort: 4-6 hours**

| Task | Description | Files |
|------|-------------|-------|
| 1.1 | Create `Aesir.Modules.Research` project | `.csproj`, `ResearchModule.cs` |
| 1.2 | Create database migration (includes Team tables) | `Migration20250127000001.cs` |
| 1.3 | Create entity models (ResearchTeam, ResearchTeamMember, ResearchSession, etc.) | `Models/*.cs` |
| 1.4 | Create repositories | `ResearchSessionRepository.cs`, `ResearchTeamRepository.cs` |
| 1.5 | Create SignalR hub | `ResearchHub.cs` |
| 1.6 | Register module in solution | `Aesir.sln` |
| 1.7 | Add module to API server | `Program.cs` module discovery |

**Deliverable**: Module scaffolding with database tables (including Research Teams) and basic CRUD.

---

### Phase 2: Research Team Configuration (Backend + Frontend)
**Estimated Effort: 6-8 hours**

| Task | Description | Files |
|------|-------------|-------|
| 2.1 | Create ResearchTeamService | `IResearchTeamService.cs`, `ResearchTeamService.cs` |
| 2.2 | Create ResearchTeamController | `ResearchTeamController.cs` |
| 2.3 | Create ResearchTeamApiService (frontend) | `IResearchTeamApiService.cs`, `ResearchTeamApiService.cs` |
| 2.4 | Create ResearchTeamsPage in Settings module | `ResearchTeamsPage.razor` |
| 2.5 | Create ResearchTeamCard component | `ResearchTeamCard.razor` |
| 2.6 | Create ResearchTeamEditDialog | `ResearchTeamEditDialog.razor` |
| 2.7 | Create TeamMemberConfig component | `TeamMemberConfig.razor` |
| 2.8 | Create OverrideConfigPanel component | `OverrideConfigPanel.razor` |
| 2.9 | Add Research Teams to Settings navigation | `SettingsPage.razor` modification |
| 2.10 | Add settings tab provider | `ResearchSettingsTabProvider.cs` |

**Deliverable**: Users can create, edit, and delete Research Teams in Settings.

---

### Phase 3: Agent Orchestration
**Estimated Effort: 8-10 hours**

| Task | Description | Files |
|------|-------------|-------|
| 3.1 | Create role definitions and prompts | `ResearchRoleDefinitions.cs`, `ResearchPromptTemplates.cs` |
| 3.2 | Create agent factory (applies overrides from team config) | `ResearchAgentFactory.cs` |
| 3.3 | Implement clarification service | `ClarificationService.cs` |
| 3.4 | Implement phase executor - Planning | `ResearchPhaseExecutor.cs` (partial) |
| 3.5 | Implement phase executor - Research | `ResearchPhaseExecutor.cs` (parallel agent execution) |
| 3.6 | Integrate with `IChatService` | Inference service resolution |
| 3.7 | Integrate with RAG (conversation documents) | Document collection tool access |

**Deliverable**: Agents can execute research with tools, respecting team configuration overrides.

---

### Phase 4: Anonymization & Peer Review
**Estimated Effort: 4-6 hours**

| Task | Description | Files |
|------|-------------|-------|
| 4.1 | Implement anonymization service | `AnonymizationService.cs` |
| 4.2 | Create peer review prompts | `ResearchPromptTemplates.cs` |
| 4.3 | Implement peer review service | `PeerReviewService.cs` |
| 4.4 | Implement phase executor - Anonymization | `ResearchPhaseExecutor.cs` |
| 4.5 | Implement phase executor - Peer Review | `ResearchPhaseExecutor.cs` |
| 4.6 | Calculate weighted scores | `ScoringCalculator.cs` |

**Deliverable**: Anonymized peer review with scores.

---

### Phase 5: Chairman Synthesis & Report Generation
**Estimated Effort: 6-8 hours**

| Task | Description | Files |
|------|-------------|-------|
| 5.1 | Create synthesis prompt | `ResearchPromptTemplates.cs` |
| 5.2 | Implement report generator | `ReportGeneratorService.cs` |
| 5.3 | Implement phase executor - Synthesis | `ResearchPhaseExecutor.cs` |
| 5.4 | Create report markdown templates | `ReportTemplates.cs` |
| 5.5 | Implement confidence calculation | `ConfidenceCalculator.cs` |
| 5.6 | Implement research trail logging | `ResearchTrailService.cs` |

**Deliverable**: Professional markdown reports with confidence levels.

---

### Phase 6: API & Controller
**Estimated Effort: 3-4 hours**

| Task | Description | Files |
|------|-------------|-------|
| 6.1 | Create API controller | `ResearchController.cs` |
| 6.2 | Create request/response DTOs | `CreateResearchSessionRequest.cs`, etc. |
| 6.3 | Implement progress broadcaster | `ResearchProgressBroadcaster.cs` |
| 6.4 | Add controller tests | `ResearchControllerTests.cs` |

**Deliverable**: Full REST API for research sessions.

---

### Phase 7: Chat Integration
**Estimated Effort: 6-8 hours**

| Task | Description | Files |
|------|-------------|-------|
| 7.1 | Add Research Teams to Agent Selector dropdown | `AgentSelectorCompact.razor` modification |
| 7.2 | Create TeamMessage component (replaces AssistantMessage for research) | `TeamMessage.razor` |
| 7.3 | Create TeamMessageProgress component | `TeamMessageProgress.razor` |
| 7.4 | Create TeamMessageReport component | `TeamMessageReport.razor` |
| 7.5 | Create TeamMessageDetails component | `TeamMessageDetails.razor` |
| 7.6 | Modify ChatPage to handle research team selection | `ChatPage.razor` modification |
| 7.7 | Modify MessageList to render TeamMessage for research_team role | `MessageList.razor` modification |
| 7.8 | Add research_team role to AesirChatMessage | `AesirChatMessage.cs` modification |
| 7.9 | Create ResearchStateService for chat integration | `ResearchStateService.cs` |

**Deliverable**: Research Teams appear in Agent Selector, research results display as Team Message bubbles in conversation.

---

### Phase 8: Frontend Module (Core Research UI)
**Estimated Effort: 6-8 hours**

| Task | Description | Files |
|------|-------------|-------|
| 8.1 | Create `Aesir.Client.Web.Modules.Research` project | `.csproj`, `ResearchModule.cs` |
| 8.2 | Create API service | `ResearchApiService.cs` |
| 8.3 | Create SignalR service | `ResearchSignalRService.cs` |
| 8.4 | Create clarification dialog | `ClarificationDialog.razor` |
| 8.5 | Create progress view components | `ResearchProgressView.razor` |
| 8.6 | Create agent activity cards | `AgentActivityCard.razor` |
| 8.7 | Create phase progress bar | `PhaseProgressBar.razor` |
| 8.8 | Register module in app | `Program.cs`, `App.razor` |

**Deliverable**: Research progress UI components integrated with chat.

---

### Phase 9: Report Viewing & Export
**Estimated Effort: 6-8 hours**

| Task | Description | Files |
|------|-------------|-------|
| 9.1 | Create report view | `ResearchReportView.razor` |
| 9.2 | Create finding cards | `FindingCard.razor` |
| 9.3 | Create confidence badges | `ConfidenceBadge.razor` |
| 9.4 | Create peer review chart | `PeerReviewScoreChart.razor` |
| 9.5 | Create research trail timeline | `ResearchTrailTimeline.razor` |
| 9.6 | Implement PDF export | `PdfReportExporter.cs` |
| 9.7 | Implement Word export | `WordReportExporter.cs` |
| 9.8 | Create export menu | `ExportMenu.razor` |

**Deliverable**: Professional report display with export.

---

### Phase 10: Testing & Polish
**Estimated Effort: 6-8 hours**

| Task | Description | Files |
|------|-------------|-------|
| 10.1 | Unit tests for orchestrator | `ResearchOrchestratorTests.cs` |
| 10.2 | Unit tests for services (including ResearchTeamService) | `*ServiceTests.cs` |
| 10.3 | Integration tests | `ResearchIntegrationTests.cs` |
| 10.4 | Frontend component tests | `*Tests.razor` |
| 10.5 | End-to-end testing (Settings → Agent Selector → Chat) | Manual testing |
| 10.6 | Error handling polish | All files |
| 10.7 | Loading states & UX polish | UI components |
| 10.8 | Documentation | README, API docs |

**Deliverable**: Production-ready feature with complete team configuration and chat integration.

---

## 9. Testing Strategy

### 9.1 Unit Tests

```csharp
// ResearchOrchestratorTests.cs
public class ResearchOrchestratorTests
{
    [Fact]
    public async Task CreateSessionAsync_GeneratesClarificationQuestions_WhenQueryIsAmbiguous()
    {
        // Arrange
        var mockClarificationService = new Mock<IClarificationService>();
        mockClarificationService
            .Setup(x => x.GenerateClarificationsAsync(It.IsAny<ResearchSession>()))
            .ReturnsAsync(new List<string> { "What time period?", "Which industry?" });

        var orchestrator = CreateOrchestrator(mockClarificationService.Object);

        // Act
        var session = await orchestrator.CreateSessionAsync(new CreateResearchSessionRequest
        {
            UserId = "test-user",
            Query = "Analyze market trends"
        });

        // Assert
        Assert.Equal(ResearchStatus.AwaitingClarification, session.Status);
        Assert.Equal(2, session.ClarificationQuestions?.Count);
    }

    [Fact]
    public async Task ExecuteResearchAsync_RunsAllPhases_InCorrectOrder()
    {
        // Test that phases execute in order: Planning → Research → Anonymization → PeerReview → Synthesis
    }
}

// AnonymizationServiceTests.cs
public class AnonymizationServiceTests
{
    [Fact]
    public void Anonymize_AssignsRandomLabels_ToSubmissions()
    {
        var service = new AnonymizationService();
        var submissions = new List<ResearchSubmission>
        {
            new() { Id = Guid.NewGuid(), Role = ResearchRole.DeepDiver },
            new() { Id = Guid.NewGuid(), Role = ResearchRole.Synthesizer },
            new() { Id = Guid.NewGuid(), Role = ResearchRole.DevilsAdvocate }
        };

        var anonymized = service.AnonymizeSubmissions(submissions);

        Assert.Contains(anonymized, s => s.AnonymizedId == "A");
        Assert.Contains(anonymized, s => s.AnonymizedId == "B");
        Assert.Contains(anonymized, s => s.AnonymizedId == "C");
    }

    [Fact]
    public void Anonymize_StripsPersonaMarkers_FromContent()
    {
        var service = new AnonymizationService();
        var submission = new ResearchSubmission
        {
            Content = "As a deep researcher, I found that..."
        };

        var anonymized = service.AnonymizeSubmission(submission);

        Assert.DoesNotContain("deep researcher", anonymized.Content.ToLowerInvariant());
    }
}
```

### 9.2 Integration Tests

```csharp
// ResearchIntegrationTests.cs
public class ResearchIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task FullResearchSession_CompletesSuccessfully()
    {
        // Arrange
        var client = CreateAuthenticatedClient();

        // Act - Create session
        var createResponse = await client.PostAsJsonAsync("/api/research/sessions", new
        {
            UserId = "test-user",
            Query = "What are the best practices for API design?"
        });

        var session = await createResponse.Content.ReadFromJsonAsync<ResearchSession>();

        // Wait for completion (with timeout)
        var timeout = TimeSpan.FromMinutes(5);
        var completed = await WaitForStatusAsync(session.Id, ResearchStatus.Completed, timeout);

        // Assert
        Assert.True(completed);

        var report = await client.GetFromJsonAsync<ResearchReport>(
            $"/api/research/sessions/{session.Id}/report");

        Assert.NotNull(report);
        Assert.NotEmpty(report.FullMarkdown);
        Assert.True(report.Findings.Count > 0);
    }
}
```

### 9.3 Frontend Tests

```razor
@* ResearchProgressViewTests.razor *@
@inherits BunitTestContext

@code {
    [Fact]
    public void ResearchProgressView_ShowsAllAgentCards()
    {
        var session = new ResearchSession
        {
            CurrentPhase = ResearchPhase.Research,
            Status = ResearchStatus.Researching
        };

        var cut = Render(@<ResearchProgressView Session="@session" />);

        Assert.Equal(3, cut.FindAll(".agent-activity-card").Count);
    }

    [Fact]
    public void ResearchProgressView_UpdatesPhase_OnSignalREvent()
    {
        // Test SignalR integration
    }
}
```

---

## 10. Future Enhancements

### 10.1 Quick Mode (Post-MVP)
- 2 agents only (Synthesizer + Critic)
- No peer review phase
- Direct Chairman synthesis
- 10-15 minute duration

### 10.2 Deep Mode (Post-MVP)
- Multi-round research with gap filling
- Chairman can request clarifications from agents
- Extended peer review with revision requests
- 60-90 minute duration

### 10.3 Additional Features
- **Dynamic Role Selection**: Topic-aware agent specialization
- **User Checkpoints**: Mid-research guidance injection
- **Research Session Forking**: Re-run with modified parameters
- **Collaborative Research**: Multiple users, shared research pool
- **Domain Templates**: Legal, Medical, Market research presets

---

## 11. Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Long research times frustrate users | High | Medium | Show detailed progress, allow cancellation |
| Agent produces low-quality output | High | Low | Peer review catches issues, confidence levels |
| Token costs exceed expectations | Medium | Medium | Cost estimation before start, budgets |
| SignalR disconnections during research | Medium | Medium | Polling fallback, reconnection logic |
| Inference service errors | High | Low | Retry logic, graceful degradation |
| PDF export performance | Low | Medium | Background generation, progress indicator |

---

## Approval Checklist

Before implementation begins:

- [ ] Architecture approved by user
- [ ] Database schema reviewed
- [ ] API endpoints confirmed
- [ ] UI mockups approved (if needed)
- [ ] Phase priorities confirmed
- [ ] Testing requirements understood

---

## Notes

- All development follows AESIR's modular architecture guidelines
- Module has **no direct dependencies** on other feature modules
- Uses runtime service resolution for inference and document access
- SignalR hub auto-discovered by Program.cs
- Migrations auto-discovered from module assembly

---

*Document Version: 1.0*
*Last Updated: 2025-12-27*
