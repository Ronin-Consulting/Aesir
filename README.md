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
  - [ ] Regenerate assistant message
  - [ ] Edit user message
  - [ ] Play message
- [ ] Implement RAG
  - [x] Upload
  - [ ] Download
  - [x] CRUD
  - [X] Citations
  - [X] Citation Viewer
  - [ ] Other?
- [ ] Implement Microphone
  - [ ] Record
  - [ ] Stop
  - [ ] Play
- [x] Implement Chat History
  - [x] CRUD
  - [x] Search
- [ ] Implement User Settings
  - [ ] Change name
  - [ ] Change profile picture
  - [ ] Change theme
- [ ] Implement User Authentication
  - [ ] Login
  - [ ] Register
  - [ ] Logout
- [ ] Implement Model Switching - WIP

## Supported AI Backends

### Ollama (Default)
Ollama is the default backend used by AESIR. No additional configuration is required beyond installing Ollama locally.

### OpenAI
To use OpenAI as the backend:
1. Set `"Inference:UseOpenAICompatible": true` in appsettings.Development.json
2. Add your API key to `"Inference:OpenAI:ApiKey"`
3. Optionally configure organization ID and preferred models

### Models Tested
1. qwen2.5:14b-instruct-q6_K
2. qwen2.5:32b
3. cogito:32b - This one is the best