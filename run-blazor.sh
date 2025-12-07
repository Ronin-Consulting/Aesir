#!/bin/bash
# run-blazor.sh - Start the Blazor WebAssembly development server
# Usage: ./run-blazor.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_DIR="$SCRIPT_DIR/Client/Aesir.Client.Web/Aesir.Client.Web.App"
PORT=5173

echo "=== Blazor WebAssembly Dev Server ==="

# Check if port 5173 is in use and kill the process
if lsof -ti:$PORT > /dev/null 2>&1; then
    echo "Port $PORT is in use. Stopping existing process..."
    lsof -ti:$PORT | xargs kill -9 2>/dev/null || true
    sleep 2
    echo "Existing process stopped."
fi

# Also kill any dotnet watch processes for this app
if pgrep -f "dotnet.*watch.*Aesir.Client.Web.App" > /dev/null 2>&1; then
    echo "Stopping existing dotnet watch process..."
    pkill -f "dotnet.*watch.*Aesir.Client.Web.App" 2>/dev/null || true
    sleep 1
fi

# Navigate to app directory
cd "$APP_DIR"

echo "Starting Blazor dev server on http://localhost:$PORT"
echo "Access via: https://aesir.localhost (reverse proxy)"
echo "Press Ctrl+C to stop"
echo ""

# Start the dev server with hot reload
dotnet watch run --urls "http://localhost:$PORT"
