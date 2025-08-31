#!/bin/bash

# Detect OS
if [[ "$OSTYPE" == "darwin"* ]]; then
    OS="macOS"
elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
    OS="Linux"
else
    echo "Unsupported OS: $OSTYPE"
    exit 1
fi

echo "Optimizing Ollama for $OS (edge-focused: low concurrency, memory savings)..."

# Base optimized env vars
export OLLAMA_HOST="0.0.0.0:11434"          # Allow remote access (e.g., from AESIR client)
export OLLAMA_ORIGINS="*"                   # CORS for web/browser access
export OLLAMA_FLASH_ATTENTION="1"           # Faster attention, less VRAM
# export OLLAMA_KV_CACHE_TYPE="q8_0"        # Quantize KV for ~50% memory save; also causes freakout
export OLLAMA_CONTEXT_LENGTH="102400"       # Default context length; assumes gpt-oss
export OLLAMA_KEEP_ALIVE="5m"               # Models loaded 5 min for quick reuse
export OLLAMA_MAX_LOADED_MODELS="3"         # Limit to fit edge RAM
export OLLAMA_NUM_PARALLEL="2"              # Parallel reqs per model
export OLLAMA_MAX_QUEUE="256"               # Queued reqs before overload
# export OLLAMA_DEBUG="1"                   # Debug for troubleshooting

# OS-specific tweaks
if [[ "$OS" == "macOS" ]]; then
    # Metal auto-enabled; no extra needed
    echo "Using Metal acceleration if available..."
elif [[ "$OS" == "Linux" ]]; then
    # Assume NVIDIA; set for CUDA (comment if no GPU or default to use ALL)
    # export CUDA_VISIBLE_DEVICES="0" # Use first GPU
    echo "Enabling CUDA if available..."
fi

# Launch ollama serve in background, log to file
OLLAMA_BIN="ollama"  # Assume in PATH; or set full path e.g., /usr/local/bin/ollama
LOG_FILE="./ollama_aesir.log"

nohup $OLLAMA_BIN serve > $LOG_FILE 2>&1 &
PID=$!

if [ $? -eq 0 ]; then
    echo "Ollama launched in background. PID: $PID"
    echo "Logs: $LOG_FILE"
    echo "Monitor with: tail -f $LOG_FILE"
    echo "Kill with: kill $PID"
else
    echo "Launch failed. Check $LOG_FILE for errors."
fi
