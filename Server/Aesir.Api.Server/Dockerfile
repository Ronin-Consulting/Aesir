# Global args if needed, but we'll scope per stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    libgdiplus \
    libc6-dev \
    libstdc++6 \
    gnupg \
    ca-certificates \
    cabextract \
    libgomp1 \
    libopenblas0 \
    libatlas3-base \
    && ln -s /usr/lib/libgdiplus.so /usr/lib/gdiplus.dll \
    && sed -i 's/Components: main/Components: main contrib/' /etc/apt/sources.list.d/debian.sources \
    && apt-get update \
    && echo "ttf-mscorefonts-installer msttcorefonts/accepted-mscorefonts-eula select true" | debconf-set-selections \
    && apt-get install -y --no-install-recommends \
    ttf-mscorefonts-installer \
    fontconfig \
    && fc-cache -fv \
    # Install Docker CLI in case of optional use of MCP Docker Toolkit
    && curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian bookworm stable" > /etc/apt/sources.list.d/docker.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends docker-ce-cli \
    # Cleanup
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
# Default RID; override with --build-arg RID=linux-x64
ARG RID=linux-arm64
WORKDIR /src

# Copy all source files (includes all modules, infrastructure, etc.)
# This allows the BuildAllModules target in Api.Server.csproj to discover and build all Aesir.Modules.* projects
COPY . .

# Restore dependencies for entire solution (includes all modules)
# This ensures module projects have their NuGet packages restored before BuildAllModules runs
RUN dotnet restore "Aesir.sln"

WORKDIR "/src/Server/Aesir.Api.Server"
# Build Api.Server - the BuildAllModules target will automatically discover and build all module projects
RUN dotnet build "Aesir.Api.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
# Re-declare for this stage
ARG RID=linux-arm64 
RUN dotnet publish "Aesir.Api.Server.csproj" -c $BUILD_CONFIGURATION -r $RID --self-contained true -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aesir.Api.Server.dll"]