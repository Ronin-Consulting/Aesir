# VA-001: Setup LibVLCSharp Dependencies

**Epic**: VISUAL_AGENT_UX
**Phase**: 1 - Foundation
**Priority**: High

## Description
Add LibVLCSharp NuGet packages and platform-specific native libraries to the Aesir.Client.Desktop project to enable video playback capabilities.

## Acceptance Criteria
- [ ] LibVLCSharp package (v3.8.5+) added to Aesir.Client.Desktop.csproj
- [ ] Platform-specific LibVLC packages added with conditional references:
  - [ ] VideoLAN.LibVLC.Windows (Windows only)
  - [ ] VideoLAN.LibVLC.Mac (macOS only)
  - [ ] VideoLAN.LibVLC.Linux (Linux only - if available)
- [ ] LibVLC native libraries copy to output directory correctly
- [ ] Basic LibVLC initialization test passes (smoke test)

## Technical Details

### NuGet Package References
```xml
<PackageReference Include="LibVLCSharp" Version="3.8.5" />
<PackageReference Include="VideoLAN.LibVLC.Windows" Version="3.0.20"
                  Condition="$([MSBuild]::IsOSPlatform('Windows'))" />
<PackageReference Include="VideoLAN.LibVLC.Mac" Version="3.0.20"
                  Condition="$([MSBuild]::IsOSPlatform('OSX'))" />
```

### Basic Smoke Test
Create a simple test to verify LibVLC can be initialized:
```csharp
using LibVLCSharp.Shared;

// In a test or Program.cs initialization
Core.Initialize(); // Should not throw
var libVLC = new LibVLC(); // Should create successfully
```

## Files to Modify
- `Aesir.Client/Aesir.Client.Desktop/Aesir.Client.Desktop.csproj`

## Dependencies
None

## Testing
- [ ] Build succeeds on Windows
- [ ] Build succeeds on macOS (if available)
- [ ] Build succeeds on Linux (if available)
- [ ] Native LibVLC libraries present in output directory
- [ ] Application starts without LibVLC-related errors

## Notes
- LibVLC native libraries are large (~50MB per platform)
- Consider adding to .gitignore if not using NuGet package restore
- Verify licensing compatibility (LGPL 2.1+)