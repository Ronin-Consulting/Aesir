# Tauri Desktop Application

This folder contains the Tauri 2.x configuration for wrapping the Aesir Blazor WebAssembly application as a native desktop app.

> **Note:** Visual Studio solution files cannot display nested folder structures. Use your IDE's file explorer to browse the full directory structure shown below.

## Directory Structure

```
src-tauri/
├── Cargo.toml              # Rust dependencies and package config
├── Cargo.lock              # Generated - locked dependency versions
├── build.rs                # Tauri build script
├── tauri.conf.json         # Main Tauri configuration
├── .gitignore              # Git ignore rules
├── README.md               # This file
│
├── src/                    # Rust source code
│   ├── lib.rs              # Main Tauri library - plugin initialization
│   └── main.rs             # Application entry point
│
├── capabilities/           # Tauri 2.x permission system
│   └── default.json        # Default permissions for the main window
│
├── icons/                  # Application icons (all platforms)
│   ├── icon.icns           # macOS app icon
│   ├── icon.ico            # Windows app icon
│   ├── icon.png            # Base icon (512x512)
│   ├── 32x32.png           # Small icon
│   ├── 128x128.png         # Medium icon
│   ├── 128x128@2x.png      # Retina medium icon
│   ├── Square*.png         # Windows tile icons
│   ├── StoreLogo.png       # Windows Store logo
│   ├── aesir-icon-blue-1024.png  # High-res source icon
│   ├── android/            # Android icons (if mobile support added)
│   └── ios/                # iOS icons (if mobile support added)
│
└── gen/                    # Generated files (gitignored)
    └── schemas/            # Auto-generated JSON schemas
```

## Key Files

### `tauri.conf.json`
Main configuration file containing:
- **App settings**: Window size, title, security settings
- **Build settings**: Dev URL, build commands, frontend dist path
- **Bundle settings**: Icons, app metadata, platform-specific options
- **Plugin settings**: Permissions for shell, dialog, fs, os, opener

### `src/lib.rs`
Initializes Tauri plugins:
- `tauri_plugin_shell` - Open URLs in default browser
- `tauri_plugin_dialog` - Native save/open file dialogs
- `tauri_plugin_fs` - File system access (scoped to temp/downloads)
- `tauri_plugin_os` - Platform detection
- `tauri_plugin_opener` - Open files with system default apps
- `tauri_plugin_log` - Debug logging (dev builds only)

### `capabilities/default.json`
Defines what the app is allowed to do:
- Core Tauri APIs
- Shell operations (open URLs)
- Dialog operations (save/open dialogs)
- File system access (temp directory)
- OS information access
- File opener access

## Development

### Prerequisites
- Rust toolchain (install via https://rustup.rs)
- Tauri CLI: `cargo install tauri-cli`

### Commands

```bash
# Development (connects to Blazor dev server at localhost:5173)
# First, start the Blazor app:
cd ../Aesir.Client.Web.App
dotnet watch run --urls "http://localhost:5173"

# Then, in another terminal:
cd src-tauri
cargo tauri dev

# Production build (creates native installer)
cargo tauri build
```

### Build Output
Production builds are placed in:
- **macOS**: `target/release/bundle/macos/Aesir.app`
- **Windows**: `target/release/bundle/msi/Aesir_x.x.x_x64.msi`
- **Linux**: `target/release/bundle/appimage/Aesir_x.x.x_amd64.AppImage`

## Native Features

The Blazor app detects Tauri at runtime via `window.__TAURI__` and provides:
- Native file save dialogs instead of browser downloads
- Open files with system default applications
- Platform-specific UI adaptations

See `wwwroot/js/platform-interop.js` in the Blazor app for the JS interop layer.
