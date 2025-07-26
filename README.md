![AESIR](Transparent%20Logo.png)
# How to run
  
1. #### API Server
   1. Update your local "hosts" file with a line "127.0.0.1 aesir.localhost",
   2. **IF** Rider IDE then right click the file "docker-compose-api.dev.yml" and select "Debug ..." or "Run..." from menu that's it,
   3. **ELSE** change directory to "~/Aesir" folder and run the following command "docker compose -f docker-compose-api-dev.yml up".
   4. **NOTE:** To use OpenAI instead of Ollama, set `"Inference:UseOpenAICompatible": true` in appsettings.Development.json and add your API key to `"Inference:OpenAI:ApiKey"`.
2. #### Client
   1. **IF** Rider IDE from the run menu edit the run configuration and add "ASPNETCORE_ENVIRONMENT=Development" to environment variables.
   2. **THEN** from the menu "Debug ..." or "Run..." the Aesir.Client.Desktop project.
   3. **ELSE** while in the "~Aesir/Aesir.Client/Aesir.Client.Desktop" folder run the following command "dotnet build && dotnet run".
   4. **NOTE:** the client will eventually be moved to a container but not yet.

## AESIR client things left to do

- [ ] Add message controls like chatgpt
  - [x] Copy message
  - [x] Regenerate assistant message
  - [x] Edit user message
  - [ ] Play message
- [X] Support "Thinking" In Chat 
- [X] Implement RAG
  - [x] Upload - Global Documents
  - [x] Upload - Conversation Documents
  - [ ] ~~Download (should we do this?)~~
  - [x] CRUD
  - [X] Citations
  - [X] Citation Viewer
  - [X] Handle image based PDFs
    - [X] Vison Model Backended OCR
    - [ ] ~~Tesseract .NET? Is it cross platformed?~~
    - [X] Other - ASPOSE?
    - [X] Other - Handle pages with images and text.
  - [ ] RBAC RAG
  - [X] Other?
    - [X] Smarter Chunking - Use Semantic Kernel Chunker (we could make this configurable?)
    - [ ] ~~Re-ranker~~
    - [X] Lexical and Semantic Search
  - [X] Implement Computer Vision
    - [X] Support for Local Vision Models
    - [X] Support for Hosted Vision Models
- [X] Implement Image Chat (using computer vision)
- [x] Implement Chat History
  - [x] CRUD
  - [x] Search
- [ ] Tools/MCP Integration
- [ ] AI Agents
- [ ] Implement Microphone
  - [ ] Record
  - [ ] Stop
  - [ ] Play
- [ ] Implement User Settings
  - [ ] Change name
  - [ ] Change profile picture
  - [ ] Change theme
- [ ] Implement User Authentication
  - [ ] Login
  - [ ] Register
  - [ ] Logout
- [ ] Implement Model Switching

## Supported AI Backends

### Ollama (Default)
Ollama is the default backend used by AESIR. No additional configuration is required beyond installing Ollama locally.

### OpenAI
To use OpenAI as the backend:
1. Set `"Inference:UseOpenAICompatible": true` in appsettings.Development.json
2. Add your API key to `"Inference:OpenAI:ApiKey"`
3. Optionally configure organization ID and preferred models

### LLM Models Tested
1. **cogito:32b-v1-preview-qwen-q4_K_M - This one is the best non-reasoning**
2. **qwen3:32b-q4_K_M - The best reasoning/thinking model**

Note: 
1. Tried deepseek-r1:32b-qwen-distill-q4_K_M its great! But sad with tools. Looks like a bug in Ollama.

### Embedding Models Tested
1. mxbai-embed-large:latest
2. nomic-embed-text:latest

### Vision Models Tested
1. gemma3:12b (works pretty good)