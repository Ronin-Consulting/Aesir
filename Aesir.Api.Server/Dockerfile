FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Aesir.Api.Server/Aesir.Api.Server.csproj", "Aesir.Api.Server/"]
RUN dotnet restore "Aesir.Api.Server/Aesir.Api.Server.csproj"
COPY . .
WORKDIR "/src/Aesir.Api.Server"
RUN dotnet build "Aesir.Api.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Aesir.Api.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aesir.Api.Server.dll"]
