# Use multi-stage build for smaller final image
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
ARG TARGETPLATFORM
ARG BUILDPLATFORM

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

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0-jammy AS runtime

# Install X11 and graphics libraries for Avalonia + PDF support
RUN apt-get update && apt-get install -y \
    # X11 and graphics libraries
    libx11-6 \
    libx11-dev \
    libxrandr2 \
    libxinerama1 \
    libxcursor1 \
    libxi6 \
    libxext6 \
    libxfixes3 \
    libxrender1 \
    libxdamage1 \
    libxcomposite1 \
    libgl1-mesa-glx \
    libglu1-mesa \
    libdrm2 \
    # Graphics and PDF processing libraries
    libgdiplus \
    libfontconfig1 \
    libfreetype6 \
    libharfbuzz0b \
    libcairo2 \
    libpango-1.0-0 \
    libpangocairo-1.0-0 \
    libgdk-pixbuf-2.0-0 \
    libgtk-3-0 \
    # Fonts for markdown rendering
    fonts-liberation \
    fonts-dejavu-core \
    fontconfig \
    # Audio (optional, for potential future features)
    libasound2 \
    libpulse0 \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r avalonia && useradd -r -g avalonia -m avalonia

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R avalonia:avalonia /app

# Switch to non-root user
USER avalonia

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Expose the application (if needed for future features)
EXPOSE 8080

# Entry point
ENTRYPOINT ["dotnet", "Aesir.Client.Desktop.dll"]