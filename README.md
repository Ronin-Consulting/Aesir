![AESIR](logo.png)
# How to run
  
1. #### API Server
   1. Update your local "hosts" file with a line "127.0.0.1 aesir.localhost",
      1. On Windows, path "C:\Windows\System32\drivers\etc\hosts"
      2. On Unix, path "/etc/hosts"
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

## Supported AI Backends

### Ollama (Default)
Ollama is the default backend used by AESIR. No additional configuration is required beyond installing Ollama locally.

### Random Notes
- Test Vision Models with this repo... https://github.com/JensWalter/my-receipts