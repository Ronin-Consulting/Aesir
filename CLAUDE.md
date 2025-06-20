# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands
- Build solution: `dotnet build Aesir.sln`
- Run API server: `docker compose -f docker-compose-api-dev.yml up`
- Run desktop client: `dotnet run --project Aesir.Client/Aesir.Client.Desktop/Aesir.Client.Desktop.csproj`

## Test Commands
- Only run tests when asked.

## Code Style Guidelines
- **Naming**: PascalCase for classes/interfaces/public methods, camelCase for local variables
- **Interfaces**: Prefix with "I" (e.g., IChatService, IModelsService)
- **Organization**: Group implementations in dedicated folders (Standard, Ollama)
- **Nullable**: Enable nullable reference types (`<Nullable>enable</Nullable>`)
- **Async**: Use async/await consistently with Task<T> return types
- **Dependency Injection**: Use constructor injection and register services in App.axaml.cs
- **MVVM Pattern**: ViewModels should derive from ViewModelBase, use CommunityToolkit.Mvvm
- **Error Handling**: Use try/catch with specific exception types, avoid general Exception catches