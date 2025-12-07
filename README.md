<p align="center">
  <img src="logo.png" alt="AESIR Logo" width="800"/>
</p>

<p align="center">
  <strong><em>A pragmatic AI agent platform designed for air-gapped and edge devices</em></strong>
</p>

<br/>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 10"/>
  &nbsp;&nbsp;
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="MIT License"/>
  &nbsp;&nbsp;
  <img src="https://img.shields.io/badge/Platform-x86%20%7C%20ARM64%20%7C%20Jetson-blue?style=for-the-badge" alt="Platforms"/>
</p>

<p align="center">
  <a href="#features">Features</a> &nbsp;•&nbsp;
  <a href="#quick-start">Quick Start</a> &nbsp;•&nbsp;
  <a href="#architecture">Architecture</a> &nbsp;•&nbsp;
  <a href="#configuration">Configuration</a> &nbsp;•&nbsp;
  <a href="#documentation">Documentation</a> &nbsp;•&nbsp;
  <a href="#contributing">Contributing</a>
</p>

<br/>

---

## Overview

AESIR is an open-source AI agent framework built with .NET that enables deployment of intelligent systems on offline networks and resource-constrained hardware. It prioritizes **security**, **privacy**, and **operational independence**.

Perfect for:
- **Air-gapped environments** - Classified, medical, and sensitive networks
- **Edge devices** - NVIDIA Jetson, Raspberry Pi, and embedded systems
- **Privacy-focused deployments** - All processing happens locally

## Features

### Multi-Engine Support
Connect to multiple AI providers simultaneously:
- **Ollama** - Run models locally with full offline capability
- **OpenAI** - GPT-4, GPT-4o, and other OpenAI models
- **Groq** - Ultra-fast inference with Groq hardware
- **Grok** - xAI's Grok models
- **Any OpenAI-compatible API** - LM Studio, vLLM, etc.

### RAG & Document Intelligence
- **Semantic search** with vector embeddings (Qdrant)
- **Multi-format support** - PDF, DOCX, images, and more
- **OCR capabilities** for scanned documents
- **Per-conversation and global document collections**

### MCP Server Integration
- **Model Context Protocol** support for tool integration
- **Local and remote MCP servers**
- **Dynamic tool discovery** from connected servers

### Modular Architecture
- **Plugin-based design** - Enable only what you need
- **Hot-swappable inference engines**
- **Extensible tool system**
- **Custom module development**

### Multi-Modal Processing
- **Vision models** - Image understanding and analysis
- **Speech-to-Text** - Whisper-based transcription
- **Text-to-Speech** - VITS/Piper voice synthesis

### Hardware Acceleration
- **NVIDIA CUDA** - Full GPU acceleration
- **Apple Metal** - Native macOS performance
- **NVIDIA Jetson** - Edge AI deployment
- **ONNX Runtime** - Cross-platform inference

## Quick Start

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (for development)
- [Ollama](https://ollama.ai/) (optional, for local models)

### 1. Clone the Repository

```bash
git clone https://github.com/ronin-consulting/Aesir.git
cd Aesir
```

### 2. Configure Local DNS

Add the following to your hosts file:

**Windows:** `C:\Windows\System32\drivers\etc\hosts`
**macOS/Linux:** `/etc/hosts`

```
127.0.0.1 aesir.localhost
127.0.0.1 qdrant.localhost
```

### 3. Start the AESIR Server

```bash
docker compose -f docker-compose-api-dev.yml up -d
```

This starts:
- **AESIR API Server** - Core backend services
- **PostgreSQL** - Configuration and chat history storage
- **Qdrant** - Vector database for RAG
- **Traefik** - Reverse proxy with TLS

### 4. Launch the Web Client

```bash
cd Client/Aesir.Client.Web/Aesir.Client.Web.App
dotnet watch run --urls "http://localhost:5173"
```

### 5. Access AESIR

- **Web Client:** http://localhost:5173
- **Setup Wizard:** http://localhost:5173/setup
- **API Swagger:** https://aesir.localhost/swagger

On first launch, the **Setup Wizard** will guide you through:
1. Connecting an inference engine (Ollama, OpenAI, etc.)
2. Configuring RAG embedding models
3. Creating your first AI agent

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        AESIR Platform                           │
├─────────────────────────────────────────────────────────────────┤
│  ┌───────────────────────────────────┐  ┌───────────────────┐   │
│  │      Reference Client (Blazor)    │  │  Mobile (Future)  │   │
│  │  ┌─────────────┐ ┌─────────────┐  │  │                   │   │
│  │  │   Browser   │ │   Desktop   │  │  │                   │   │
│  │  │    (WASM)   │ │   (Tauri)   │  │  │                   │   │
│  │  └─────────────┘ └─────────────┘  │  │                   │   │
│  └────────────────┬──────────────────┘  └─────────┬─────────┘   │
│                   │                               │             │
│                   └───────────────┬───────────────┘             │
│                                   ▼                             │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    AESIR API Server                       │  │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────────────┐  │  │
│  │  │  Chat   │ │ Config  │ │   RAG   │ │    Inference    │  │  │
│  │  │ Module  │ │ Module  │ │ Module  │ │     Module      │  │  │
│  │  └─────────┘ └─────────┘ └─────────┘ └─────────────────┘  │  │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────────────┐  │  │
│  │  │   MCP   │ │ Speech  │ │ Storage │ │     Logging     │  │  │
│  │  │ Module  │ │ Module  │ │ Module  │ │     Module      │  │  │
│  │  └─────────┘ └─────────┘ └─────────┘ └─────────────────┘  │  │
│  └───────────────────────────────────────────────────────────┘  │
│                          │                                      │
│         ┌────────────────┼────────────────┐                     │
│         ▼                ▼                ▼                     │
│  ┌────────────┐  ┌─────────────┐  ┌─────────────┐               │
│  │ PostgreSQL │  │   Qdrant    │  │   Ollama    │               │
│  │   (Data)   │  │  (Vectors)  │  │  (Models)   │               │
│  └────────────┘  └─────────────┘  └─────────────┘               │
└─────────────────────────────────────────────────────────────────┘
```

## Configuration

### Inference Engines

Configure inference engines in `appsettings.Development.json`:

```json
{
  "InferenceEngines": [
    {
      "Name": "Local Ollama",
      "Type": "Ollama",
      "Configuration": {
        "Endpoint": "http://localhost:11434"
      }
    },
    {
      "Name": "OpenAI",
      "Type": "OpenAICompatible",
      "Configuration": {
        "Endpoint": "https://api.openai.com/v1",
        "ApiKey": "your-api-key"
      }
    }
  ]
}
```

### RAG Settings

```json
{
  "GeneralSettings": {
    "RagEmbeddingInferenceEngineName": "Local Ollama",
    "RagEmbeddingModel": "nomic-embed-text:latest",
    "RagVisionInferenceEngineName": "Local Ollama",
    "RagVisionModel": "llava:latest"
  }
}
```

### Database Connection

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=5432;Database=aesir;User Id=postgres;Password=your-password;"
  }
}
```

## Development

### Project Structure

```
Aesir/
├── Server/
│   ├── Aesir.Api.Server/        # Main API entry point
│   ├── Aesir.Infrastructure/    # Core infrastructure
│   ├── Aesir.Orchestration/     # AI orchestration logic
│   └── Modules/
│       ├── Aesir.Modules.Chat/          # Chat functionality
│       ├── Aesir.Modules.Configuration/ # Settings management
│       ├── Aesir.Modules.Documents/     # RAG & documents
│       ├── Aesir.Modules.Inference/     # Model inference
│       ├── Aesir.Modules.Mcp/           # MCP integration
│       └── Aesir.Modules.Speech/        # TTS/STT
├── Client/
│   └── Aesir.Client.Web/        # Reference Client
│       ├── Aesir.Client.Web.App/           # Blazor WASM application
│       ├── Aesir.Client.Web.Infrastructure/ # Shared client services
│       ├── Modules/                         # Feature modules
│       └── src-tauri/                       # Tauri desktop wrapper
└── Common/
    └── Aesir.Common/            # Shared models and utilities
```

### Building from Source

```bash
# Build the server
dotnet build Server/Aesir.Api.Server

# Build the web client
dotnet build Client/Aesir.Client.Web/Aesir.Client.Web.App

# Run tests
dotnet test
```

### Running Tests

```bash
# All tests
dotnet test

# Web client tests only
dotnet test Client/Aesir.Client.Web/Aesir.Client.Web.Tests
```

## Deployment

### Docker Compose (Recommended)

```bash
# Development
docker compose -f docker-compose-api-dev.yml up -d

# View logs
docker compose -f docker-compose-api-dev.yml logs -f aesir-api
```

### NVIDIA Jetson

AESIR supports deployment on NVIDIA Jetson devices (Nano, Xavier, Orin) for edge AI applications. See the [Jetson Deployment Guide](docs/JETSON.md) for details.

### Kubernetes

Kubernetes manifests are available in the `k8s/` directory for production deployments.

## Documentation

- **[Full Documentation](https://ronin-consulting.github.io/Aesir/)** - Comprehensive guides and API reference
- **[API Reference](https://aesir.localhost/swagger)** - Interactive API documentation (when running locally)
- **[Architecture Guide](docs/ARCHITECTURE.md)** - Deep dive into system design
- **[Module Development](docs/MODULES.md)** - Creating custom modules

## Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- **[Ollama](https://ollama.ai/)** - Local model inference
- **[Qdrant](https://qdrant.tech/)** - Vector database
- **[MudBlazor](https://mudblazor.com/)** - Blazor component library
- **[Tauri](https://tauri.app/)** - Desktop application framework

---

<p align="center">
  <strong>Built with ❤️ by <a href="https://ronin-consulting.github.io/">Ronin Consulting</a></strong>
</p>
