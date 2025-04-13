# Services Directory

This directory contains service interfaces and implementations for the Aesir API Server.

## Interfaces

- `IChatService.cs`: Interface for chat-related functionality
- `IModelsService.cs`: Interface for AI model management
- `IChatHistoryService.cs`: Interface for chat history management
- `IPdfDataLoader.cs`: Interface for loading PDF documents

## Implementations

Service implementations are located in the `Implementations` directory and organized by provider.

## Adding New Service Providers

When implementing new service providers:
1. Create a new directory under `Implementations` with the provider name
2. Implement the required interfaces
3. Register the implementation in the IoC container
