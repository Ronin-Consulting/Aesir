![AESIR](logo.png)
# How to run
  
1. #### API Server
   1. Update your local "hosts" file with a line "127.0.0.1 aesir.localhost",
      1. On Windows, path "C:\Windows\System32\drivers\etc\hosts"
      2. On Unux, path "/etc/hosts"
   2. **IF** Rider IDE then right click the file "docker-compose-api.dev.yml" and select "Debug ..." or "Run..." from menu that's it,
   3. **ELSE** change directory to "~/Aesir" folder and run the following command "docker compose -f docker-compose-api-dev.yml up".
   4. **NOTE:** To use OpenAI instead of Ollama, set `"Inference:UseOpenAICompatible": true` in appsettings.Development.json and add your API key to `"Inference:OpenAI:ApiKey"`.
2. #### Desktop Client
   1. **IF** Rider IDE from the run menu edit the run configuration and add "ASPNETCORE_ENVIRONMENT=Development" to environment variables.
   2. **THEN** from the menu "Debug ..." or "Run..." the Aesir.Client.Desktop project.
   3. **ELSE** while in the "~Aesir/Aesir.Client/Aesir.Client.Desktop" folder run the following command "dotnet build && dotnet run".
   4. **NOTE:** the client will eventually be moved to a container but not yet.
3. ### Browser Client
   1. Add the aesir.localhost.crt certificate to your OS trust store and make sure it's trusted.
   2. **IF** Rider IDE from the run menu edit the run configuration and set Open Browser to Chrome.
   3. **THEN** from the menu "Debug ..." or "Run..." the Aesir.Client.Browser project.

## OUTSTANDING FEATURES

- [X] Add message controls like chatgpt
  - [x] Copy message
  - [X] Regenerate assistant message
  - [X] Edit user message
  - [X] Play message
- [X] Support "Thinking" In Chat
- [X] Support Showing Execution Of Tools In Chat Thoughts
- [X] Implement RAG
  - [X] Implement Document Management
  - [X] Upload - Global Documents
  - [X] Upload - Conversation Documents
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
- [X] AI Agents
- [X] Hands Free Mode
  - [X] hands free UI/UX
  - [X] speech-to-text
  - [X] text-to-speech
- [ ] Implement User Settings
  - [ ] Change name
  - [ ] Change profile picture
  - [ ] Change theme
- [ ] Implement User Authentication
  - [ ] Login
  - [ ] Register
  - [ ] Logout
- [X] Works in web browser (ish)
- [ ] Optimize Backend
  - [ ] Custom tuned llama.cpp 
    - X86,Apple Silicon and ARM64
    - CUDA, Metal and Onnx
## Supported AI Backends

### Ollama (Default)
Ollama is the default backend used by AESIR. No additional configuration is required beyond installing Ollama locally.

### Random Notes
- Test Vision Models with this repo... https://github.com/JensWalter/my-receipts