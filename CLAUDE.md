# Claude Code Guidelines

This document contains guidelines and instructions for Claude Code when working in this project.

## Project Configuration

- **Target Framework:** .NET 9.0
- **C# Version:** C# 13
- All projects must target `net9.0` framework

## Code Generation

When generating code that involves external libraries, frameworks, or technologies, you MUST:

1. **Always fetch up-to-date documentation** before writing code
    - Use the `mcp__context7__resolve-library-id` tool to find the library ID
    - Use the `mcp__context7__get-library-docs` tool to retrieve current documentation
    - Never rely solely on training data for library-specific implementations

2. **Required for context7 usage:**
    - Any time you need to implement features using specific libraries (e.g., React, Next.js, TensorFlow, PyTorch, DeepStream, GStreamer)
    - When the user mentions a specific version or wants the latest API patterns
    - Before generating boilerplate or starter code for frameworks
    - When troubleshooting library-specific errors or deprecations

3. **Workflow:**
   ```
   User Request → Identify Libraries → Resolve Library ID → Fetch Docs → Generate Code
   ```

4. **Examples of when to use context7:**
    - "Create a React component with hooks" → Fetch React docs first
    - "Set up a DeepStream pipeline" → Fetch DeepStream docs first
    - "Write a FastAPI endpoint" → Fetch FastAPI docs first
    - "Configure GStreamer elements" → Fetch GStreamer docs first

5. **What to include in context7 queries:**
    - Specify the `topic` parameter to focus on relevant sections (e.g., "hooks", "routing", "pipeline configuration")
    - Adjust `tokens` parameter based on complexity (default: 5000, complex topics: 10000+)

## Enforcement

Do not generate library-specific code without first consulting context7 documentation. If documentation is unavailable for a library, inform the user and proceed with caution, clearly noting the limitations.

## Code Style Guidelines
- **Naming**: PascalCase for classes/interfaces/public methods, camelCase for local variables
- **Interfaces**: Prefix with "I" (e.g., IChatService, IModelsService)
- **Organization**: Group implementations in dedicated folders (e.g., Standard, Platform)
- **Nullable**: Enable nullable reference types (`<Nullable>enable</Nullable>`)
- **Async**: Use async/await consistently with Task<T> return types
- **Dependency Injection**: Use constructor injection and register services with proper lifetimes
- **MVVM Pattern**: ViewModels should derive from ViewModelBase, use CommunityToolkit.Mvvm
- **Error Handling**: Use try/catch with specific exception types, avoid general Exception catches

## Plan Creation
- Always ask any clarifying questions, if needed, before creating code change plans.