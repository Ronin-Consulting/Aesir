# AESIR Product Requirements Document (PRD)
## Reverse-Engineered from Codebase

**Document Version:** 1.0
**Last Updated:** January 2026
**Product Name:** AESIR (AI Enterprise System for Information Retrieval)
**Vendor:** Ronin Consulting
**License:** Apache 2.0

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Product Vision & Goals](#2-product-vision--goals)
3. [Target Users & Markets](#3-target-users--markets)
4. [System Architecture](#4-system-architecture)
5. [Functional Requirements](#5-functional-requirements)
6. [Non-Functional Requirements](#6-non-functional-requirements)
7. [Data Models & Schema](#7-data-models--schema)
8. [API Specification](#8-api-specification)
9. [Client Applications](#9-client-applications)
10. [Deployment & Infrastructure](#10-deployment--infrastructure)
11. [Technology Stack](#11-technology-stack)
12. [Module System](#12-module-system)
13. [Security Requirements](#13-security-requirements)
14. [Future Roadmap](#14-future-roadmap)

---

## 1. Executive Summary

### 1.1 Product Overview

AESIR is an enterprise-grade AI platform designed for intelligent document processing, semantic search, and domain-specific AI assistance. The platform supports both cloud-based and air-gapped edge deployments, providing powerful AI capabilities while maintaining data security and operational flexibility.

### 1.2 Key Value Propositions

| Value Proposition | Description |
|-------------------|-------------|
| **Cost Flexibility** | Choose between local AI models (cost-effective) or cloud services (maximum performance) |
| **Data Security** | On-premises deployment ensures sensitive information never leaves infrastructure |
| **Domain Expertise** | Specialized AI personas for business and military contexts |
| **Cross-Platform Access** | Native desktop applications and browser-based access |
| **Advanced Document Intelligence** | Transform documents into searchable, conversational knowledge bases |
| **Voice-Enabled Operations** | Hands-free interaction with STT/TTS capabilities |
| **Open-Source** | Apache 2.0 License |

### 1.3 Core Capabilities

- **Dual AI Backend Support** - Local (Ollama) and Cloud (OpenAI-compatible)
- **Advanced RAG (Retrieval-Augmented Generation)** - Document processing with semantic search
- **Voice Integration** - Real-time speech-to-text and text-to-speech
- **Modular Architecture** - Plugin-based system built on Microsoft Semantic Kernel
- **Enterprise Deployment** - Docker, Kubernetes (K3s) support

---

## 2. Product Vision & Goals

### 2.1 Vision Statement

Transform static documents and telemetry data into searchable, actionable, and conversational knowledge bases while maintaining enterprise-grade security and operational flexibility.

### 2.2 Strategic Goals

1. **Enable Secure AI Processing** - Support air-gapped and edge deployments without compromising AI capabilities
2. **Maximize User Adoption** - Provide intuitive cross-platform interfaces (desktop, browser, voice)
3. **Ensure Extensibility** - Modular plugin architecture for custom integrations
4. **Optimize Performance** - Sub-500 token response targets for edge deployment
5. **Maintain Compliance** - Complete audit trails with citation tracking

### 2.3 Success Metrics

| Metric | Target |
|--------|--------|
| Edge Response Time | < 500 tokens per response |
| Document Processing | Support PDFs, images, text with OCR |
| Uptime | 99.9% availability |
| Voice Latency | Real-time streaming |
| Platform Support | Windows, macOS, Linux, Browser |

---

## 3. Target Users & Markets

### 3.1 Primary Markets

#### Enterprise & Business
- Document analysis (contracts, reports, policies)
- Decision support from corporate documentation
- Compliance and audit with traceable citations
- Knowledge management repositories

#### Military & Defense
- Mission planning with OPSEC compliance
- Intelligence analysis of classified materials
- Edge deployment in disconnected environments
- Voice-controlled field operations

#### Healthcare & Professional Services
- Research and analysis of medical literature
- Policy compliance navigation
- Clinical decision support

#### Manufacturing & Industrial
- Technical documentation access
- Quality assurance and compliance
- Troubleshooting support

### 3.2 User Personas

| Persona | Description | Primary Needs |
|---------|-------------|---------------|
| **Knowledge Worker** | Office professional needing document insights | Fast search, accurate citations |
| **Field Operator** | Military/industrial user in hands-busy scenarios | Voice control, offline capability |
| **System Administrator** | IT professional managing deployments | Easy configuration, monitoring |
| **Security Officer** | Compliance/security professional | Audit trails, data sovereignty |

---

## 4. System Architecture

### 4.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER                              │
├──────────────────┬──────────────────┬───────────────────────────┤
│  Desktop Client  │  Browser Client  │      Voice Interface      │
│   (Avalonia)     │    (WASM)        │     (SignalR Hubs)        │
└────────┬─────────┴────────┬─────────┴─────────────┬─────────────┘
         │                  │                       │
         ▼                  ▼                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                      API GATEWAY LAYER                          │
│                  (Traefik Reverse Proxy)                        │
│              HTTPS/TLS Termination & Routing                    │
└─────────────────────────────┬───────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                            │
│                   (ASP.NET Core API)                            │
├──────────┬──────────┬──────────┬──────────┬──────────┬─────────┤
│   Chat   │  Config  │   Docs   │ Inference│  Speech  │   MCP   │
│  Module  │  Module  │  Module  │  Module  │  Module  │  Module │
└────┬─────┴────┬─────┴────┬─────┴────┬─────┴────┬─────┴────┬────┘
     │          │          │          │          │          │
     ▼          ▼          ▼          ▼          ▼          ▼
┌─────────────────────────────────────────────────────────────────┐
│                  ORCHESTRATION LAYER                            │
│            (Microsoft Semantic Kernel 1.67+)                    │
│     Plugins │ Function Calling │ Memory │ Prompt Templates      │
└─────────────────────────────┬───────────────────────────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         ▼                    ▼                    ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────────┐
│   AI BACKENDS   │ │   DATA LAYER    │ │    VECTOR STORE         │
├─────────────────┤ ├─────────────────┤ ├─────────────────────────┤
│ Ollama (Local)  │ │ PostgreSQL 16+  │ │  Qdrant                 │
│ OpenAI (Cloud)  │ │ pgvector ext    │ │  (Semantic Search)      │
│                 │ │ Dapper ORM      │ │  1024-dim embeddings    │
└─────────────────┘ └─────────────────┘ └─────────────────────────┘
```

### 4.2 Module System Architecture

The system uses convention-based module discovery:

```
Application Startup
        ↓
DapperColumnMapper.Initialize() → Configure PascalCase to snake_case
        ↓
ModuleDiscovery.DiscoverModules() → Find all Aesir.Modules.* assemblies
        ↓
For each module: Module.RegisterServicesAsync(IServiceCollection)
        ↓
FluentMigrator discovers migrations from module assemblies
        ↓
Database migrations run automatically
        ↓
For each module: Module.Initialize(IApplicationBuilder)
        ↓
Application Running
```

### 4.3 Current Modules

| Module | Purpose | Key Components |
|--------|---------|----------------|
| **Aesir.Modules.Chat** | Chat conversations & completions | Controllers, Services, History |
| **Aesir.Modules.Configuration** | System settings management | Agents, Tools, Inference Engines |
| **Aesir.Modules.Documents** | Document processing & RAG | Upload, Chunking, Collections |
| **Aesir.Modules.Inference** | AI model abstraction | Base interfaces |
| **Aesir.Modules.Inference.Ollama** | Local LLM integration | Ollama connector |
| **Aesir.Modules.Inference.OpenAI** | Cloud LLM integration | OpenAI-compatible API |
| **Aesir.Modules.Logging** | Centralized logging | Kernel logs, diagnostics |
| **Aesir.Modules.Mcp** | Model Context Protocol | Tool integration |
| **Aesir.Modules.Speech** | Voice processing | STT/TTS via SignalR |
| **Aesir.Modules.Storage** | File storage | Binary storage, retrieval |

---

## 5. Functional Requirements

### 5.1 Chat & Conversation Management

#### FR-CHAT-001: Chat Completions
- **Description:** Process user messages and return AI-generated responses
- **Priority:** P0 (Critical)
- **Features:**
  - Streaming responses with real-time token delivery
  - Support for conversation history context
  - Temperature and max tokens configuration
  - Agent-specific prompts and personas
  - Thinking/reasoning mode support (e.g., Qwen3)

#### FR-CHAT-002: Chat History
- **Description:** Persist and retrieve conversation sessions
- **Priority:** P0 (Critical)
- **Features:**
  - Session persistence per user
  - Full-text search across chat history
  - Session title management
  - Cascade deletion (session → documents → files)

#### FR-CHAT-003: Agent Configuration
- **Description:** Configure AI agents with specific behaviors
- **Priority:** P1 (High)
- **Features:**
  - Named agents with custom personas
  - Model selection per agent
  - Tool assignment per agent
  - Temperature/TopP/MaxTokens configuration
  - Custom prompt templates

### 5.2 Document Processing & RAG

#### FR-DOC-001: Document Upload
- **Description:** Upload and process documents for RAG
- **Priority:** P0 (Critical)
- **Features:**
  - Maximum file size: 100MB
  - Supported formats: PDF, TIFF, PNG, JPG, TXT, MD, JSON
  - Multipart/form-data upload
  - Progress tracking

#### FR-DOC-002: Document Processing
- **Description:** Extract and chunk document content
- **Priority:** P0 (Critical)
- **Features:**
  - PDF text extraction with Aspose.PDF
  - OCR for image-based documents
  - Vision model processing (Gemma3, GPT-4 Vision)
  - Smart chunking with Semantic Kernel
  - Token counting and optimization

#### FR-DOC-003: Document Collections
- **Description:** Organize documents into searchable collections
- **Priority:** P1 (High)
- **Features:**
  - Global collections (organization-wide)
  - Conversation-scoped collections
  - Category-based organization
  - Metadata tracking

#### FR-DOC-004: Semantic Search
- **Description:** Vector-based document retrieval
- **Priority:** P0 (Critical)
- **Features:**
  - Qdrant vector database integration
  - 1024-dimensional embeddings
  - Hybrid search (keyword + semantic)
  - Citation generation with page references

### 5.3 Voice & Speech

#### FR-SPEECH-001: Speech-to-Text (STT)
- **Description:** Convert voice input to text
- **Priority:** P1 (High)
- **Features:**
  - Real-time streaming via SignalR
  - SherpaOnnx-based recognition
  - Silence detection (RMS 0.03f threshold)
  - Speaker diarization support
  - 16kHz mono audio input

#### FR-SPEECH-002: Text-to-Speech (TTS)
- **Description:** Generate natural voice output
- **Priority:** P1 (High)
- **Features:**
  - VITS-Piper model support
  - Real-time streaming output
  - Configurable speech speed
  - 22,050 Hz stereo output

#### FR-SPEECH-003: Hands-Free Mode
- **Description:** Complete voice-controlled interaction
- **Priority:** P2 (Medium)
- **Features:**
  - Continuous listening mode
  - Pause-on-silence functionality
  - Voice activation/deactivation
  - Audio level feedback

### 5.4 Configuration Management

#### FR-CONFIG-001: Inference Engines
- **Description:** Configure AI backend providers
- **Priority:** P0 (Critical)
- **Features:**
  - Ollama (local) configuration
  - OpenAI-compatible API configuration
  - Endpoint and API key management
  - Model listing and selection

#### FR-CONFIG-002: Tools Management
- **Description:** Configure AI tools and plugins
- **Priority:** P1 (High)
- **Features:**
  - Internal tools (built-in)
  - MCP server tools (external)
  - Tool assignment to agents
  - Tool metadata (name, description, icon)

#### FR-CONFIG-003: MCP Server Integration
- **Description:** Model Context Protocol server management
- **Priority:** P2 (Medium)
- **Features:**
  - Local server configuration (command + arguments)
  - Remote server configuration (URL + headers)
  - Environment variable management
  - Tool discovery from MCP servers

#### FR-CONFIG-004: General Settings
- **Description:** System-wide configuration
- **Priority:** P1 (High)
- **Features:**
  - RAG embedding model selection
  - RAG vision model selection
  - TTS/STT model paths
  - Google Search integration (API key, engine ID)

### 5.5 Logging & Monitoring

#### FR-LOG-001: Kernel Logging
- **Description:** Track AI operations and diagnostics
- **Priority:** P2 (Medium)
- **Features:**
  - Time-range queries
  - Chat session filtering
  - Conversation-level filtering
  - Structured log details (JSONB)

---

## 6. Non-Functional Requirements

### 6.1 Performance

| Requirement | Target | Measurement |
|-------------|--------|-------------|
| **NFR-PERF-001** | API response time < 200ms (non-AI) | P95 latency |
| **NFR-PERF-002** | AI response streaming start < 2s | Time to first token |
| **NFR-PERF-003** | Edge deployment < 500 tokens/response | Token count |
| **NFR-PERF-004** | File upload throughput > 10MB/s | Transfer rate |
| **NFR-PERF-005** | Voice latency < 500ms | End-to-end STT/TTS |

### 6.2 Scalability

| Requirement | Target |
|-------------|--------|
| **NFR-SCALE-001** | Support 100+ concurrent users per instance |
| **NFR-SCALE-002** | Horizontal scaling via Kubernetes |
| **NFR-SCALE-003** | Document collections up to 1M documents |
| **NFR-SCALE-004** | Chat history retention unlimited |

### 6.3 Reliability

| Requirement | Target |
|-------------|--------|
| **NFR-REL-001** | System availability 99.9% |
| **NFR-REL-002** | Automatic container restart on failure |
| **NFR-REL-003** | Database connection pooling |
| **NFR-REL-004** | Graceful degradation on AI backend failure |

### 6.4 Security

| Requirement | Description |
|-------------|-------------|
| **NFR-SEC-001** | HTTPS/TLS encryption for all communications |
| **NFR-SEC-002** | JWT Bearer token authentication (planned) |
| **NFR-SEC-003** | API key protection for external services |
| **NFR-SEC-004** | Non-root container execution |
| **NFR-SEC-005** | Data sovereignty for air-gapped deployments |

### 6.5 Maintainability

| Requirement | Description |
|-------------|-------------|
| **NFR-MAINT-001** | Modular architecture with plugin system |
| **NFR-MAINT-002** | Structured logging with NLog |
| **NFR-MAINT-003** | Correlation IDs for request tracing |
| **NFR-MAINT-004** | Health check endpoints |
| **NFR-MAINT-005** | Database migrations with FluentMigrator |

---

## 7. Data Models & Schema

### 7.1 Database Conventions

**CRITICAL:** All database identifiers use `aesir_` prefix with lowercase snake_case:
- Tables: `aesir_user`, `aesir_product`, `aesir_agent`
- Columns: `first_name`, `is_active`, `created_at`
- Indexes: `ix_aesir_user_username`
- Primary Keys: UUID (Guid) type

### 7.2 Core Entities

#### Chat Session (`aesir_chat_session`)
```sql
CREATE TABLE aesir_chat_session (
    id UUID PRIMARY KEY,
    user_id VARCHAR NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    conversation JSONB NOT NULL,
    title VARCHAR
);
```

#### Agent (`aesir_agent`)
```sql
CREATE TABLE aesir_agent (
    id UUID PRIMARY KEY,
    name VARCHAR UNIQUE NOT NULL,
    description VARCHAR,
    chat_inference_engine_id UUID REFERENCES aesir_inference_engine(id),
    chat_model VARCHAR,
    chat_temperature DOUBLE PRECISION,
    chat_top_p DOUBLE PRECISION,
    chat_max_tokens INTEGER,
    chat_prompt_persona SMALLINT,
    chat_custom_prompt_content VARCHAR,
    allow_thinking BOOLEAN,
    think_value VARCHAR
);
```

#### Tool (`aesir_tool`)
```sql
CREATE TABLE aesir_tool (
    id UUID PRIMARY KEY,
    name VARCHAR UNIQUE NOT NULL,
    type SMALLINT NOT NULL, -- 0=Internal, 1=McpServer
    description VARCHAR,
    mcp_server_id UUID REFERENCES aesir_mcp_server(id),
    tool_name VARCHAR,
    icon_name VARCHAR
);
```

#### Inference Engine (`aesir_inference_engine`)
```sql
CREATE TABLE aesir_inference_engine (
    id UUID PRIMARY KEY,
    name VARCHAR UNIQUE NOT NULL,
    description VARCHAR,
    type SMALLINT NOT NULL, -- 0=Ollama, 1=OpenAICompatible
    configuration JSONB
);
```

#### MCP Server (`aesir_mcp_server`)
```sql
CREATE TABLE aesir_mcp_server (
    id UUID PRIMARY KEY,
    name VARCHAR UNIQUE NOT NULL,
    description VARCHAR,
    location SMALLINT NOT NULL, -- 0=Local, 1=Remote
    command VARCHAR,
    arguments JSONB,
    environment_variables JSONB,
    url VARCHAR,
    http_headers JSONB
);
```

#### General Settings (`aesir_general_settings`)
```sql
CREATE TABLE aesir_general_settings (
    id INTEGER PRIMARY KEY CHECK (id = 1), -- Single row only
    rag_emb_inf_eng_id UUID,
    rag_emb_model VARCHAR,
    rag_vis_inf_eng_id UUID,
    rag_vis_model VARCHAR,
    tts_model_path VARCHAR,
    stt_model_path VARCHAR,
    vad_model_path VARCHAR,
    google_search_engine_id VARCHAR,
    google_api_key VARCHAR
);
```

#### File Storage (`aesir_file_storage`)
```sql
CREATE TABLE aesir_file_storage (
    id UUID PRIMARY KEY,
    file_name VARCHAR UNIQUE NOT NULL,
    mime_type VARCHAR NOT NULL,
    file_size BIGINT NOT NULL,
    file_content BYTEA NOT NULL, -- Max 1GB
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP
);
```

#### Kernel Log (`aesir_log_kernel`)
```sql
CREATE TABLE aesir_log_kernel (
    id UUID PRIMARY KEY,
    level VARCHAR NOT NULL,
    message VARCHAR NOT NULL,
    created_at TIMESTAMP NOT NULL,
    details JSONB
);
```

### 7.3 Entity Relationships

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  aesir_agent    │────▶│aesir_agent_tool │◀────│   aesir_tool    │
└────────┬────────┘     └─────────────────┘     └────────┬────────┘
         │                                               │
         │ FK: chat_inference_engine_id                  │ FK: mcp_server_id
         ▼                                               ▼
┌─────────────────┐                            ┌─────────────────┐
│aesir_inference_ │                            │ aesir_mcp_      │
│    engine       │                            │    server       │
└─────────────────┘                            └─────────────────┘
```

---

## 8. API Specification

### 8.1 REST Endpoints Summary

| Controller | Base Route | Endpoints | Auth Required |
|------------|------------|-----------|---------------|
| **Chat** | `/chat/completions` | 4 | No (planned) |
| **ChatHistory** | `/chat/history` | 6 | No (planned) |
| **Configuration** | `/configuration` | 25+ | No (planned) |
| **DocumentCollection** | `/document/collections` | 10 | No (planned) |
| **Models** | `/models` | 1 | No (planned) |
| **Logs** | `/logs` | 3 | No (planned) |

### 8.2 Chat Endpoints

```http
# Agent Chat Completion
POST /chat/completions/agent
Content-Type: application/json

{
  "agentId": "guid",
  "conversationHistory": [...],
  "userMessage": "string",
  "conversationId": "guid",
  "chatSessionId": "guid"
}

# Agent Streamed Chat Completion
POST /chat/completions/agent/streamed
Content-Type: application/json
Accept: application/x-ndjson

# Returns: IAsyncEnumerable<AesirChatStreamedResult>
```

### 8.3 Configuration Endpoints

```http
# CRUD for Agents
GET    /configuration/agents
GET    /configuration/agents/{id}
POST   /configuration/agents
PUT    /configuration/agents/{id}
DELETE /configuration/agents/{id}
GET    /configuration/agents/{id}/tools
PUT    /configuration/agents/{id}/tools

# CRUD for Tools
GET    /configuration/tools
GET    /configuration/tools/{id}
POST   /configuration/tools
PUT    /configuration/tools/{id}
DELETE /configuration/tools/{id}

# CRUD for Inference Engines
GET    /configuration/inferenceengines
GET    /configuration/inferenceengines/{id}
POST   /configuration/inferenceengines
PUT    /configuration/inferenceengines/{id}
DELETE /configuration/inferenceengines/{id}

# CRUD for MCP Servers
GET    /configuration/mcpservers
GET    /configuration/mcpservers/{id}
POST   /configuration/mcpservers
PUT    /configuration/mcpservers/{id}
DELETE /configuration/mcpservers/{id}
POST   /configuration/mcpservers/from-config
GET    /configuration/mcpservers/{id}/tools

# General Settings
GET    /configuration/generalsettings
PUT    /configuration/generalsettings

# System Status
GET    /configuration/systemready
GET    /configuration/databaseconfigurationmode
```

### 8.4 Document Endpoints

```http
# File Operations
GET /document/collections/file/{filename}/content
GET /document/collections/file/{id}/{filename}

# Global Category Files
POST   /document/collections/globals/{categoryId}/upload/file
GET    /document/collections/globals/{categoryId}/files
GET    /document/collections/globals/{categoryId}/files/{filename}/content
DELETE /document/collections/globals/{categoryId}/files/{filename}

# Conversation Files
POST   /document/collections/conversations/{conversationId}/upload/file
GET    /document/collections/conversations/{conversationId}/files
GET    /document/collections/conversations/files
GET    /document/collections/conversations/{conversationId}/files/{filename}/content
DELETE /document/collections/conversations/{conversationId}/files/{filename}
```

### 8.5 SignalR Hubs

```javascript
// TTS Hub - Text-to-Speech
Hub URL: /ttshub
Method: GenerateAudio(text: string, speed: float) → IAsyncEnumerable<byte[]>

// STT Hub - Speech-to-Text
Hub URL: /stthub
Method: ProcessAudioStream(audioFrames: IAsyncEnumerable<byte[]>) → IAsyncEnumerable<string>
```

### 8.6 Health Check

```http
GET /healthz

Response (200 OK):
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy"
  }
}
```

---

## 9. Client Applications

### 9.1 Desktop Client (Avalonia)

**Target Framework:** .NET 10.0
**UI Framework:** Avalonia 11.3.8
**Pattern:** MVVM with CommunityToolkit.Mvvm

#### Key Features

| Feature | Description |
|---------|-------------|
| **Chat Interface** | Real-time streaming chat with markdown support |
| **Document Management** | Upload, view, and manage document collections |
| **Voice Control** | Hands-free operation with STT/TTS |
| **Citation Viewer** | PDF/image viewing with zoom controls |
| **Agent Selection** | Switch between configured AI agents |
| **Configuration UI** | Manage agents, tools, inference engines |

#### Desktop-Specific Services

| Service | Purpose |
|---------|---------|
| `AudioPlaybackService` | MiniAudioEx-based audio playback |
| `AudioRecordingService` | SoundFlow-based microphone capture |
| `SpeechService` | SignalR bridge for TTS/STT |
| `CitationViewerService` | Document display in modal drawer |

#### Views

```
MainDesktopWindow
├── ChatView
│   ├── ChatHistoryView (sidebar)
│   └── ConversationView (main)
├── DocumentsView
├── ToolsView
├── AgentsView
├── InferenceEnginesView
├── GeneralSettingsView
└── LogsView
```

### 9.2 Browser Client (WebAssembly)

**Target Framework:** .NET 10.0-browser
**UI Framework:** Avalonia 11.3.8 (WASM)

- Shared codebase with desktop client via `Aesir.Client`
- No plugin installation required
- Limited audio capabilities (browser restrictions)

### 9.3 Shared Client Library

**Project:** `Aesir.Client`

Contains shared components used by both desktop and browser clients:
- ViewModels (MainWindowViewModel, ChatViewViewModel, etc.)
- Views (AXAML files)
- Services (ChatService, ChatHistoryService, etc.)
- Models (DTOs)
- Converters and Validators

---

## 10. Deployment & Infrastructure

### 10.1 Container Architecture

```yaml
services:
  aesir-api:           # ASP.NET Core API
  aesir-client-desktop: # Avalonia desktop (optional)
  pgdb:                 # PostgreSQL 16 with pgvector
  qdrant:               # Vector database
  reverse-proxy:        # Traefik for HTTPS/routing
```

### 10.2 Docker Compose Files

| File | Purpose |
|------|---------|
| `docker-compose-api-dev.yml` | Development environment |
| `docker-compose-aesir-all.yml` | Full stack including desktop |
| `docker-compose-aesir-all.override.yml` | Local overrides |

### 10.3 Environment Configuration

**Development Endpoints:**
```
API Base URL: https://aesir.localhost
TTS Hub: https://aesir.localhost/ttshub
STT Hub: https://aesir.localhost/stthub
Qdrant: https://qdrant.localhost:6333
PostgreSQL: localhost:5432
```

### 10.4 Required Infrastructure

| Component | Version | Purpose |
|-----------|---------|---------|
| PostgreSQL | 16+ | Primary database |
| pgvector | Latest | Vector extension |
| Qdrant | Latest | Vector search |
| Traefik | Latest | Reverse proxy |
| Ollama | Latest | Local AI (optional) |

### 10.5 Hardware Requirements

| Tier | RAM | CPU | Storage | GPU |
|------|-----|-----|---------|-----|
| Minimum | 16GB | 4 cores | 50GB | Optional |
| Recommended | 100GB+ | 8+ cores | 1TB+ | CUDA-compatible |
| Edge | 8GB | 4 cores | 32GB | Optional |

---

## 11. Technology Stack

### 11.1 Backend Technologies

| Category | Technology | Version |
|----------|------------|---------|
| **Framework** | .NET | 10.0 |
| **Language** | C# | 13 |
| **API** | ASP.NET Core | 10.0 |
| **AI Orchestration** | Microsoft Semantic Kernel | 1.67.1 |
| **ORM** | Dapper | 2.1.66 |
| **ORM Extensions** | Dapper.Contrib | 2.0.78 |
| **Migrations** | FluentMigrator | 7.1.0 |
| **Logging** | NLog | 6.0.6 |
| **Health Checks** | AspNetCore.HealthChecks.NpgSql | 9.0.0 |

### 11.2 Frontend Technologies

| Category | Technology | Version |
|----------|------------|---------|
| **UI Framework** | Avalonia | 11.3.8 |
| **MVVM** | CommunityToolkit.Mvvm | 8.4.0 |
| **HTTP Client** | Flurl.Http | 4.0.2 |
| **Resilience** | Polly | 8.6.4 |
| **SignalR Client** | Microsoft.AspNetCore.SignalR.Client | 10.0.0 |
| **Theme** | Semi.Avalonia | 11.3.7.1 |
| **Icons** | Material.Icons.Avalonia | 2.4.1 |

### 11.3 Audio/Speech Technologies

| Category | Technology | Version |
|----------|------------|---------|
| **Audio Playback** | MiniAudioEx | 2.6.5 |
| **Audio Recording** | SoundFlow | 1.2.1 |
| **STT Runtime** | SherpaOnnx | Various |
| **TTS Models** | VITS-Piper | Various |
| **Image Processing** | SixLabors.ImageSharp | 3.1.12 |
| **PDF Processing** | PDFtoImage | 5.2.0 |

### 11.4 Infrastructure Technologies

| Category | Technology | Version |
|----------|------------|---------|
| **Container Runtime** | Docker | Latest |
| **Orchestration** | Docker Compose / K3s | Latest |
| **Reverse Proxy** | Traefik | Latest |
| **Database** | PostgreSQL | 16+ |
| **Vector Extension** | pgvector | Latest |
| **Vector Database** | Qdrant | Latest |

---

## 12. Module System

### 12.1 Module Interface

```csharp
public interface IModule
{
    string Name { get; }
    string Version { get; }
    string Description { get; }

    Task RegisterServicesAsync(IServiceCollection services);
    void Initialize(IApplicationBuilder app);
}
```

### 12.2 Creating a New Module

1. Create project: `Aesir.Modules.{ModuleName}`
2. Implement `IModule` interface
3. Add `CopyModuleToApiServer` MSBuild target
4. Create migrations in `Migrations/` folder
5. Build Api.Server → Module auto-discovered

### 12.3 Module Discovery

- **Build Time:** MSBuild discovers `Modules/Aesir.Modules.*/*.csproj`
- **Runtime:** `ModuleDiscovery` scans for `Aesir.Modules.*.dll`
- **No Manual Registration Required**

### 12.4 Module Project Template

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Aesir.Infrastructure\Aesir.Infrastructure.csproj" />
    <PackageReference Include="FluentMigrator" Version="7.1.0" />
  </ItemGroup>

  <Target Name="CopyModuleToApiServer" AfterTargets="Build">
    <Copy SourceFiles="$(TargetPath)"
          DestinationFolder="$(ProjectDir)../Aesir.Api.Server/bin/$(Configuration)/$(TargetFramework)/"
          SkipUnchangedFiles="true" />
  </Target>
</Project>
```

---

## 13. Security Requirements

### 13.1 Current Security Posture

| Aspect | Status | Details |
|--------|--------|---------|
| **Transport Security** | Implemented | HTTPS via Traefik |
| **Authentication** | Planned | JWT Bearer tokens |
| **Authorization** | Planned | Role-based access |
| **API Key Management** | Implemented | For Qdrant, Google APIs |
| **Container Security** | Implemented | Non-root user execution |
| **Secrets Management** | Partial | Environment variables |

### 13.2 Security Features to Implement

- [ ] JWT Authentication middleware
- [ ] User registration and login endpoints
- [ ] Role-based authorization on endpoints
- [ ] API rate limiting
- [ ] Input validation and sanitization
- [ ] Audit logging for security events
- [ ] Secret management via vault

### 13.3 Air-Gapped Security

- All processing can occur locally with Ollama
- No external data transmission required
- Complete data sovereignty
- OPSEC compliance for classified environments

---

## 14. Future Roadmap

### 14.1 Planned Features

| Feature | Priority | Status |
|---------|----------|--------|
| User Authentication (JWT) | P0 | Planned |
| Role-Based Authorization | P0 | Planned |
| Multi-tenant Support | P1 | Planned |
| Advanced RAG Pipelines | P1 | In Progress |
| Custom Plugin Development | P2 | Planned |
| Mobile Client | P3 | Planned |

### 14.2 Technical Debt

| Item | Priority | Description |
|------|----------|-------------|
| Complete Auth Implementation | P0 | JWT + user management |
| API Documentation | P1 | OpenAPI/Swagger annotations |
| Unit Test Coverage | P1 | Target 80%+ coverage |
| Performance Optimization | P2 | Caching, connection pooling |
| Error Handling Standardization | P2 | Consistent error responses |

### 14.3 Integration Roadmap

- [ ] SAP Integration Plugin
- [ ] Dynamics 365 Connector
- [ ] Salesforce Integration
- [ ] HubSpot Integration
- [ ] Custom CRM Adapters

---

## Appendix A: Tested AI Models

| Category | Model | Purpose |
|----------|-------|---------|
| **Chat (Non-Reasoning)** | Cogito | General conversation |
| **Chat (Reasoning)** | Qwen3 | Complex reasoning tasks |
| **Vision** | Gemma3 | Image analysis |
| **Embeddings** | MxbAI | Vector generation |
| **Embeddings** | Nomic | Vector generation |
| **TTS** | Piper (VITS) | Voice synthesis |
| **STT** | SenseVoice | Speech recognition |

## Appendix B: API Error Codes

| HTTP Code | Meaning |
|-----------|---------|
| 200 | Success |
| 201 | Created |
| 204 | No Content |
| 400 | Bad Request |
| 401 | Unauthorized |
| 404 | Not Found |
| 500 | Internal Server Error |

## Appendix C: Configuration Examples

### Agent Configuration
```json
{
  "id": "guid",
  "name": "Business Assistant",
  "description": "Corporate document assistant",
  "chatInferenceEngineId": "guid",
  "chatModel": "cogito:latest",
  "chatTemperature": 0.7,
  "chatTopP": 0.9,
  "chatMaxTokens": 2048,
  "chatPromptPersona": 0,
  "allowThinking": false
}
```

### Inference Engine Configuration
```json
{
  "id": "guid",
  "name": "Local Ollama",
  "description": "Local AI backend",
  "type": 0,
  "configuration": {
    "endpoint": "http://localhost:11434"
  }
}
```

---

**Document End**

*This PRD was reverse-engineered from the AESIR codebase and represents the current state of the system as of January 2026.*
