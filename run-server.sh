#!/bin/bash
# run-server.sh - Build and start the AESIR API server via Docker Compose
# Usage: ./run-server.sh [--rebuild]
#
# Options:
#   --rebuild    Force rebuild of the API image (no cache)
#
# The script will automatically tail the API logs after startup.
# Press Ctrl+C to stop following logs (containers keep running).

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/docker-compose-api-dev.yml"

# Parse arguments
REBUILD=false
for arg in "$@"; do
    case $arg in
        --rebuild)
            REBUILD=true
            ;;
    esac
done

echo "=== AESIR API Server (Docker) ==="

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "ERROR: Docker is not running. Please start Docker Desktop first."
    exit 1
fi

# Navigate to project root (where docker-compose file is)
cd "$SCRIPT_DIR"

# Stop existing containers
echo "Stopping existing containers..."
docker compose -f "$COMPOSE_FILE" down 2>/dev/null || true

# Build and start
if [ "$REBUILD" = true ]; then
    echo "Rebuilding API image (--rebuild specified)..."
    docker compose -f "$COMPOSE_FILE" build --no-cache aesir-api
else
    echo "Building API image (use --rebuild to force full rebuild)..."
    docker compose -f "$COMPOSE_FILE" build aesir-api
fi

echo "Starting services..."
docker compose -f "$COMPOSE_FILE" up -d

echo ""
echo "Waiting for services to be healthy..."
sleep 5

# Check health status
echo ""
echo "Service Status:"
docker compose -f "$COMPOSE_FILE" ps

echo ""
echo "=== Services Started ==="
echo "API:      https://aesir.localhost"
echo "Qdrant:   https://qdrant.localhost (or http://localhost:6333)"
echo "Traefik:  http://localhost:8080 (dashboard)"
echo "Postgres: localhost:5432"
echo ""
echo "Following API logs (Ctrl+C to stop)..."
echo "==========================================="
docker compose -f "$COMPOSE_FILE" logs -f aesir-api
