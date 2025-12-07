#!/bin/bash
# run-tauri.sh - Start the Tauri desktop application
# Usage: ./run-tauri.sh
#
# Note: This script assumes the Blazor dev server is running on port 5173.
# Run ./run-blazor.sh first in a separate terminal.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TAURI_DIR="$SCRIPT_DIR/Client/Aesir.Client.Web"
BLAZOR_PORT=5173

echo "=== Tauri Desktop App ==="

# Check if Blazor dev server is running
if ! lsof -ti:$BLAZOR_PORT > /dev/null 2>&1; then
    echo "WARNING: Blazor dev server is not running on port $BLAZOR_PORT"
    echo "Run ./run-blazor.sh in a separate terminal first."
    echo ""
    read -p "Continue anyway? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Kill any existing Tauri dev processes
if pgrep -f "cargo-tauri" > /dev/null 2>&1; then
    echo "Stopping existing Tauri dev process..."
    pkill -f "cargo-tauri" 2>/dev/null || true
    sleep 1
fi

if pgrep -f "aesir-client-web" > /dev/null 2>&1; then
    echo "Stopping existing Tauri app..."
    pkill -f "aesir-client-web" 2>/dev/null || true
    sleep 1
fi

# Navigate to Tauri directory
cd "$TAURI_DIR"

echo "Starting Tauri desktop app..."
echo "Press Ctrl+C to stop"
echo ""

# Start Tauri in dev mode
cargo tauri dev
