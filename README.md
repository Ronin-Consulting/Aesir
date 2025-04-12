# How to run
  
1. #### API Server
   1. **IF** Rider IDE then right click the file "docker-compose-api.dev.yml" and select "Debug ..." or "Run..." from menu that's it,
   2. **ELSE** while in the "~Aesir/Aesir.Api.Server" folder run the following command "dotnet build"
   3. **THEN** change directory to "~/Aesir" folder and run the following command "docker compose docker-compose-api-dev.yml".
2. #### Client
   1. **IF** Rider IDE right click the Aesir.Client.Desktop project and select "Debug ..." or "Run..." from menu
   2. **ELSE** while in the "~Aesir/Aesir.Client/Aesir.Client.Desktop" folder run the following command "dotnet build && dotnet run".
   3. **NOTE:** the client will eventually be moved to a container but not yet.


## AESIR client things left to do

- [ ] Add message controls like chatgpt
  - [ ] Copy message
  - [ ] Regenerate assistant message
  - [ ] Edit user message
  - [ ] Play message
- [ ] Implement RAG
  - [ ] Upload
  - [ ] Download
  - [ ] CRUD
- [ ] Citations
  - [ ] RAG
  - [ ] Other?
- [ ] Implement Microphone
  - [ ] Record
  - [ ] Stop
  - [ ] Play
- [ ] Implement Chat History
  - [ ] CRUD
  - [ ] Search
- [ ] Implement User Settings
  - [ ] Change name
  - [ ] Change profile picture
  - [ ] Change theme
- [ ] Implement User Authentication
  - [ ] Login
  - [ ] Register
  - [ ] Logout
- [ ] Implement Model Switching