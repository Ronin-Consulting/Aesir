FROM mcr.microsoft.com/dotnet/runtime:9.0-jammy AS base
# Install comprehensive dependencies for Avalonia and SkiaSharp on ARM64
RUN apt-get update && apt-get install -y \
    libx11-6 libfontconfig1 libice6 libsm6 libxext6 libxrender1 libxi6 \
    libfreetype6 libunwind8 libxfixes3 libgl1 libdbus-1-3 \
    libuuid1 uuid-dev uuid-runtime \
    libharfbuzz0b libglib2.0-0 libgraphite2-3 \
    libexpat1 libpng16-16 libjpeg8 \
    fontconfig fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

# Update font cache to fix fontconfig warnings
RUN fc-cache -fv

# Create non-root user
RUN groupadd -r avalonia && useradd -r -g avalonia -m avalonia
# Set working directory
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
# Copy project files
COPY ["Aesir.Client/Aesir.Client.Desktop/Aesir.Client.Desktop.csproj", "Aesir.Client/Aesir.Client.Desktop/"]
COPY ["Aesir.Client/Aesir.Client/Aesir.Client.csproj", "Aesir.Client/Aesir.Client/"]
COPY ["Aesir.Common/Aesir.Common.csproj", "Aesir.Common/"]
COPY ["Aesir.Client/Directory.Build.props", "Aesir.Client/"]
# Restore dependencies
RUN dotnet restore "Aesir.Client/Aesir.Client.Desktop/Aesir.Client.Desktop.csproj"
# Copy source code
COPY . .
# Build and publish
WORKDIR "/src/Aesir.Client/Aesir.Client.Desktop"
RUN dotnet publish "Aesir.Client.Desktop.csproj" -c Release -o /app/publish \
    --no-restore --self-contained false

FROM base AS final
WORKDIR /app
# Copy published application
COPY --from=build /app/publish .
# Change ownership to non-root user
RUN chown -R avalonia:avalonia /app

# Switch to non-root user
USER avalonia

# Entry point
ENTRYPOINT ["dotnet", "Aesir.Client.Desktop.dll"]