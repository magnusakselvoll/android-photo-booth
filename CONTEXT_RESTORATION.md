# Context Restoration Guide

**Purpose**: This document preserves critical context for resuming work if the conversation context needs to be reset.

**Last Updated**: November 6, 2025  
**Phase Completion**: Phase 1 & 2 Complete, Phase 3 Ready  
**Build Status**: ✅ 0 Errors, 0 Warnings

---

## ⚠️ IMPORTANT: Keep This Document Updated

**To Future Self (and current maintainers)**:

This document is **critical** for context continuity. After each phase completion:

1. **Update the timestamp** at the top (date completed)
2. **Update "Phase Completion"** line
3. **Update "Build Status"** line
4. **Add a new section** documenting what was done in the completed phase
5. **Update Phase N Plan section** with any new discoveries or gotchas
6. **Commit this file** to the dev/copilot-refactor branch

This is your "single source of truth" for resuming work. A few minutes updating this document saves hours of re-analysis if context resets.

**Sections to update after each phase:**
- ✅ After Phase 3: "Settings Migration (Phase 3 Preparation)" → "Settings Migration (Phase 3 Completed)"
- ✅ After Phase 4: "Joystick Refactoring (Phase 4 Preparation)" → "Joystick Refactoring (Phase 4 Completed)"
- And so on for Phases 5, 6, 7...

---

---

## 🎯 Current Project State

### Completed Phases

**Phase 1: Project Setup** ✅
- Migrated from .NET Framework 4.7.2 → .NET 9.0 (net9.0-windows)
- Converted all 3 projects to modern SDK-style .csproj format
- Created modern Program.cs with dependency injection setup
- Folder structure reorganization ready (not yet executed)

**Phase 2: Nullability & Warnings** ✅
- Fixed 72 compiler warnings (CS8618, CS8622, CS8625, CS8603, CS8602, CS8604, CS8600, CS0618)
- Enabled nullable reference types across entire codebase
- Updated event handler signatures to accept nullable `sender` parameters
- Replaced obsolete `TimeZone` with modern `TimeZoneInfo`
- Build status: **0 errors, 0 warnings**

### Project Structure

```
android-photo-booth/
├── android-photo-booth-app/          (WinExe, net9.0-windows)
│   ├── MainForm.cs
│   ├── PictureForm.cs
│   ├── SlideshowControl.cs
│   ├── HistoryQueue.cs
│   └── Properties/
│       └── Settings.settings        (TO BE MIGRATED - Phase 3)
├── android-photo-booth-camera/       (Library, net9.0-windows)
│   ├── CameraForm.cs
│   ├── AdbController.cs
│   ├── AndroidDevice.cs
│   ├── JoystickObserver.cs           (Uses SharpDX - TO BE REPLACED - Phase 4)
│   ├── Countdown.cs
│   └── Logging/
│       ├── Logger.cs
│       ├── LogMessage.cs
│       └── LogMessageLevel.cs
└── android-photo-booth-app.tests/    (TestProject, net9.0-windows)
    └── HistoryQueueTests.cs
```

### Build Command

```powershell
dotnet build d:\code\android-photo-booth\android-photo-booth.sln --configuration Release
```

Expected output: 0 errors, 0 warnings, build succeeds in ~2 seconds

---

## 🏗️ Architecture Decisions

### Framework Choice: .NET 9.0 (not 10.0)
- **Reason**: .NET 10.0 SDK not available in local environment
- **Decision**: Use .NET 9.0 (still modern, LTS-track compatible)
- **Target Framework Specifier**: `net9.0-windows` (required for Windows Forms)

### Nullable Reference Types Pattern
- **Decision**: Make types nullable (`Type?`) rather than suppress warnings
- **Pattern**: 
  ```csharp
  // ✅ CORRECT - Explicit about nullability
  private AdbController? _adbController;
  public event EventHandler<LogMessage>? MessageLogged;
  
  // ❌ WRONG - Would suppress real issues
  #pragma warning disable CS8618
  ```

### Event Handler Sender Parameter
- **Pattern**: All event handlers accept `object? sender` (nullable)
- **Reason**: UI framework events don't guarantee non-null sender
- **Example**:
  ```csharp
  private void OnMessageLogged(object? sender, LogMessage message)
  private void OnCountdownTick(object? sender, int secondsRemaining)
  ```

### Testing Framework
- **Decision**: Use MSTest v3.2.2 (built-in, no external Moq)
- **Reason**: Modern .NET testing should use native capabilities
- **Approach**: Use MSTest's built-in mocking and assertion features

### Dependency Injection
- **Framework**: `Microsoft.Extensions.DependencyInjection` v9.0.0
- **Setup Location**: Both `Program.cs` files (app and camera projects)
- **ConfigureServices**: Method placeholder created, ready for Phase 3

---

## 🔧 Key Code Patterns Applied

### Null-Coalescing Pattern
```csharp
// Safe default values
_deviceTextBox.Text = connected ? device?.ToString() ?? "Unknown device" : errorMessage ?? "Unknown error";
Message = message ?? throw new ArgumentNullException(nameof(message));
```

### Null-Conditional Access
```csharp
// Safe member access on nullable types
_timer?.Stop();
_timer?.Dispose();
CancellationTokenSource?.Cancel();
```

### Safe Collection Iteration
```csharp
// Handle nullable collections
foreach (var prop in image.PropertyItems ?? [])
foreach (string extension in Settings.FilenameExtensions ?? [])
{
    if (!string.IsNullOrEmpty(extension))
    {
        hashSet.Add(extension);
    }
}
```

### Constructor Initialization vs Lazy
```csharp
// ✅ Initialize in constructor (not in separate method)
public HistoryQueue(int capacity)
{
    Capacity = capacity;
    _buffer = new T[Capacity];  // Direct initialization
    _firstElement = -1;
    _lastElement = -1;
}
```

---

## 📋 Settings Migration (Phase 3 Preparation)

### Current Settings Locations

**Files using `Properties.Settings.Default`:**

1. **android-photo-booth-app**:
   - `MainForm.cs`: ~5 references (PictureFolder, ShowFileNames, etc.)
   - `PictureForm.cs`: 1-2 references
   - `SlideshowControl.cs`: 3-4 references (FilenameExtensions, Recursive, etc.)

2. **android-photo-booth-camera**:
   - `CameraForm.cs`: ~10 references (AdbPath, UseNfcScreenApi, etc.)
   - `CameraSettingsForm.cs`: Multiple settings UI bindings
   - `JoystickInfo.cs`: 1 reference (Joystick GUID setting)

### Phase 3 Plan

1. Create `Settings.cs` POCO class:
   ```csharp
   public class AppSettings
   {
       public string PictureFolder { get; set; }
       public bool ShowFileNames { get; set; }
       public string[] FilenameExtensions { get; set; }
       public string AdbPath { get; set; }
       public bool UseNfcScreenApi { get; set; }
       public bool Recursive { get; set; }
       // ... etc
   }
   ```

2. Create `ISettingsService` interface:
   ```csharp
   public interface ISettingsService
   {
       AppSettings Load();
       void Save(AppSettings settings);
   }
   ```

3. Create `appsettings.json` with default values

4. Register in DI: `services.AddScoped<ISettingsService, SettingsService>`

5. Migrate all `Properties.Settings.Default.X` → `_settingsService.Settings.X`

### Expected Changes
- Files modified: 6-8
- Lines changed: 100-150
- Warnings introduced: 0 (settings service will be fully typed)

---

## 🎮 Joystick Refactoring (Phase 4 Preparation)

### Current Implementation
- **Library**: SharpDX 4.2.0 (DirectInput)
- **Location**: `android-photo-booth-camera/JoystickObserver.cs` and `JoystickInfo.cs`
- **References**: Lines 44, 76 in JoystickObserver.cs; method calls in CameraForm.cs

### Planned Replacement
- **Library**: Windows.Gaming.Input (modern UWP API, built-in)
- **Key Differences**:
  - SharpDX uses COM/DirectInput
  - Windows.Gaming.Input is modern, WinRT-based
  - Simpler API, better for Windows 10+

### Migration Steps
1. Replace `using SharpDX.DirectInput` with Windows.Gaming.Input
2. Update `JoystickInfo` class to use Gamepad/RawGameController
3. Simplify event handling (Windows.Gaming.Input is event-driven)
4. Update dependencies in .csproj

### Affected Files
- `JoystickInfo.cs` (property/method updates)
- `JoystickObserver.cs` (main refactor)
- `CameraForm.cs` (event handler updates)
- `android-photo-booth-camera.csproj` (remove SharpDX, add Windows.Gaming.Input reference)

---

## 🔌 P/Invoke & Windows-specific Code (Phase 5)

### Current P/Invoke Usage

**Location**: `android-photo-booth-camera/NativeMethods.cs` and `SlideshowControl.cs`

Common P/Invoke calls:
- `SetThreadExecutionState` (prevent screen lock during slideshow)
- `ShowWindow` / `SetForegroundWindow` (window management)
- User32 API calls for window handling

### Verification Needed
- All P/Invoke signatures are Windows 10+ compatible
- No deprecated APIs being used
- Platform targeting (net9.0-windows) handles P/Invoke correctly
- No issues with 64-bit executables

### Files to Verify
- `NativeMethods.cs` - P/Invoke signatures
- `SlideshowControl.cs` - SetThreadExecutionState usage
- Any window/form management code in CameraForm.cs

---

## 🧪 Testing (Phase 6)

### Current Test Status
- **Framework**: MSTest v3.2.2
- **Test File**: `HistoryQueueTests.cs`
- **Coverage**: Limited (only HistoryQueue)

### Phase 6 Goals
1. Add service/integration tests
2. Test AdbController methods
3. Test settings service (once created)
4. Use MSTest built-in capabilities (no Moq)

### Modern Testing Patterns (No Moq)
- Use `TestContext` for test setup/teardown
- Create minimal fake implementations for dependencies
- Use `Assert` fluently
- Leverage records and tuples for test data

---

## 📚 Documentation Files

Key reference documents:
- `CLAUDE.md` - Refactoring guidelines and C# preferences
- `DOTNET10_UPGRADE_ANALYSIS.md` - Original upgrade analysis
- `UPGRADE_SUMMARY.md` - Phase breakdown
- `PHASE2_COMPLETION_SUMMARY.md` - What was just completed
- `CONTEXT_RESTORATION.md` - **This file**

---

## ⚡ Quick Reference: Modern C# Patterns Used

| Pattern | Example | Why |
|---------|---------|-----|
| File-scoped namespaces | `namespace X.Y.Z;` | Cleaner, less nesting |
| Implicit usings | Enabled in .csproj | Less boilerplate |
| Nullable reference types | `string?` | Type safety |
| Null coalescing | `?? default` | Safe defaults |
| Null conditional | `?.` | Safe access |
| Records (future) | For settings POCOs | Immutable by default |
| Tuples | Return multiple values | Cleaner than out params |

---

## 🚀 Next Phase Checklist

### Before Starting Phase 3
- [ ] Review current Settings usage in all files
- [ ] Create Settings.cs POCO class
- [ ] Create ISettingsService interface
- [ ] Create SettingsService implementation
- [ ] Create appsettings.json template
- [ ] Run build test (should still be 0 warnings)

### Commands to Know
```powershell
# Build without restore (fast)
dotnet build --configuration Release --no-restore

# Build with restore (clean)
dotnet build --configuration Release

# Check for warnings only
dotnet build --configuration Release 2>&1 | Select-String "warning"

# Run specific test
dotnet test android-photo-booth-app.tests
```

---

## ⚠️ Known Gotchas

1. **Settings in UI Designer Bindings**: CameraSettingsForm.cs binds directly to Properties.Settings. This binding will need refactoring to use ISettingsService.

2. **FileInfo Nullability**: `image.PropertyItems` can be null on some images. Always use `?? []` pattern.

3. **Event Handler Sender**: Never assume sender is non-null in event handlers. Always use `object? sender`.

4. **JoystickInfo.ConfiguredJoystick**: Returns nullable, callers must null-check before use.

5. **Task vs System.Threading.Tasks.Task**: Ambiguous in some contexts. Use fully-qualified names when needed.

---

## 📞 Quick Escalation

If context resets and you need to continue:

1. Read this file first (you're reading it!)
2. Check `PHASE2_COMPLETION_SUMMARY.md` for what was just done
3. Review `.csproj` files to confirm target frameworks are `net9.0-windows`
4. Run `dotnet build` to verify 0 errors, 0 warnings
5. Check the todo list in this repository
6. Proceed with Phase 3 (Settings migration)

---

## 🔄 Maintenance Instructions for Future Self

**After completing each phase, immediately update this document:**

```markdown
# Completion Checklist

□ Update "Last Updated" timestamp
□ Update "Phase Completion" status line
□ Update "Build Status" with current results
□ Add new "Phase X Completed" section documenting:
  - What was changed
  - Files modified (count and names)
  - New build status
  - Any new gotchas discovered
  - Links to new documentation created
□ Update relevant Phase Preparation section
□ Run full build and verify status
□ Commit changes to dev/copilot-refactor branch with message:
  "docs: Update context restoration for Phase X completion"
```

**Why this matters:**
- Conversation context has token limits (~200k per session)
- If context resets mid-phase, this document is your lifeline
- 5 minutes of documentation saves 1-2 hours of re-analysis
- Each phase builds on previous ones - accurate tracking is critical

**Quick test**: Can someone new to this project:
1. Read CONTEXT_RESTORATION.md
2. Run `dotnet build`
3. Know exactly what was done and what comes next?

If yes ✅, you've kept it up to date.
If no ❌, update it before moving to the next phase.

---

**End of Context Restoration Guide**
