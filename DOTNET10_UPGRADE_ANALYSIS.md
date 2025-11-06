# .NET Core 10 Upgrade Analysis & Plan

**Date**: November 6, 2025  
**Status**: Pre-Upgrade Analysis  
**Target Framework**: .NET Core 10 (net10.0)

---

## Executive Summary

This document provides a comprehensive analysis of the android-photo-booth codebase and a detailed plan to upgrade from **.NET Framework 4.7.2** to **.NET Core 10**. The project is a Windows Forms desktop application with good separation of concerns and minimal external dependencies, which makes it a strong candidate for migration.

**Key Finding**: This migration is **highly achievable** with an estimated effort of **4-6 phases**, assuming no major architectural changes are needed.

---

## Part 1: Current State Analysis

### 1.1 Project Structure

**Solution Layout**:
```
android-photo-booth.sln
├── android-photo-booth-app (WinForms Executable)
│   ├── Namespaces: MagnusAkselvoll.AndroidPhotoBooth.App
│   ├── Main components: Forms (MainForm, PictureForm)
│   └── Utilities: HistoryQueue, SlideshowControl, NativeMethods
├── android-photo-booth-camera (Library)
│   ├── Namespaces: MagnusAkselvoll.AndroidPhotoBooth.Camera
│   ├── Main components: AdbController, CameraForm, JoystickObserver
│   ├── Forms: CameraForm, CameraSettingsForm
│   └── Logging: Custom logging framework
└── android-photo-booth-app.tests (MSTest Unit Tests)
    ├── Framework: MSTest 1.2.0
    └── Current tests: HistoryQueueTests
```

### 1.2 Current Framework & Dependencies

**Target Frameworks**:
- `android-photo-booth-app`: .NET Framework 4.7.2 (WinExe)
- `android-photo-booth-camera`: .NET Framework 4.7.2 (Library, net45)
- `android-photo-booth-app.tests`: .NET Framework 4.7.2 (net461)

**NuGet Dependencies**:
| Package | Version | Current Usage | .NET Core Compatible |
|---------|---------|------|--------|
| SharpDX | 4.2.0 | DirectInput for joystick | ❌ No (abandoned) |
| SharpDX.DirectInput | 4.2.0 | Joystick input handling | ❌ No (abandoned) |
| MSTest.TestFramework | 1.2.0 | Unit testing framework | ⚠️ Old version |
| MSTest.TestAdapter | 1.2.0 | Test runner | ⚠️ Old version |

### 1.3 Code Analysis

**Windows Forms Usage**:
- ✅ All WinForms code uses standard APIs (controls, events, dialogs)
- ✅ No deprecated patterns detected
- ✅ Compatible forms: MainForm, PictureForm, CameraForm, CameraSettingsForm

**P/Invoke Usage**:
```csharp
// NativeMethods.cs - SetThreadExecutionState (kernel32.dll)
[DllImport("kernel32.dll")]
public static extern uint SetThreadExecutionState(uint esFlags);
```
- ✅ Standard Windows P/Invoke
- ✅ Fully compatible with .NET Core on Windows
- ⚠️ Will require `<RuntimeIdentifier>` configuration

**Settings & Configuration**:
- Uses `Properties.Settings.Default` pattern
- `App.config` with legacy configuration
- Will need migration to `appsettings.json` with `IConfiguration`

**Async/Await**:
- ✅ Already uses async/await in AdbController
- ✅ Task-based async patterns
- ✅ Compatible with .NET Core

**Process Execution**:
- Uses `Process.Start()` with `ProcessStartInfo`
- ✅ Fully compatible with .NET Core

**File I/O & Path Handling**:
- ✅ Standard `System.IO` APIs
- ✅ Compatible with .NET Core (cross-platform potential)

**Threading**:
- Uses basic `lock()` statements
- ✅ Compatible with .NET Core

**Logging**:
- Custom logging framework in `Logging/` folder
- ✅ No external dependencies, easy to modernize

### 1.4 Breaking Changes & Compatibility Issues

**Issue 1: SharpDX Library (CRITICAL)**
- SharpDX 4.2.0 is outdated and not maintained for .NET Core
- **Solution**: Replace with `Windows.Gaming.Input` (built-in, Windows-only) or investigate alternatives
- **Alternative**: Use `System.Device.Gpio` or find cross-platform joystick library

**Issue 2: Settings & Configuration (IMPORTANT)**
- `Properties.Settings.Default` pattern deprecated
- **Solution**: Migrate to `IConfiguration` + `appsettings.json`
- **Migration Strategy**: Create abstraction layer for settings

**Issue 3: App.config (IMPORTANT)**
- App.config is .NET Framework specific
- **Solution**: Replace with `appsettings.json`

**Issue 4: MSTest Version (LOW)**
- MSTest.TestFramework 1.2.0 is outdated
- **Solution**: Update to latest MSTest version (v2.x or v3.x)

**Issue 5: P/Invoke Complexity (MEDIUM)**
- Windows-only P/Invoke requires runtime identifier
- **Solution**: Windows-only build configuration or conditional compilation
- **Impact**: Cannot run on Linux/Mac without refactoring

**Issue 6: Windows Forms Designer (MEDIUM)**
- Designer.cs files are auto-generated
- **Risk**: Potential compatibility with modern tooling
- **Mitigation**: Test thoroughly after upgrade

---

## Part 2: Dependency Migration Strategy

### 2.1 SharpDX/DirectInput Replacement

**Current Usage**:
- Joystick input detection and monitoring
- Located in `JoystickObserver.cs` and `JoystickInfo.cs`

**Options**:

**Option A: Windows.Gaming.Input (RECOMMENDED)**
- ✅ Official Microsoft library for gaming input
- ✅ Built-in to Windows
- ✅ Modern, actively maintained
- ✅ First-class .NET Core support
- ❌ Windows-only
- NuGet: None needed (part of Windows SDK)

**Option B: Silk.NET**
- ✅ Cross-platform, modern
- ✅ Active community
- ⚠️ Requires additional setup
- NuGet: `Silk.NET.SDL` or `Silk.NET.GLFW`

**Option C: Custom P/Invoke**
- ✅ Full control
- ✅ No external dependencies
- ❌ More complex, error-prone
- Requires: Windows.Gaming.Input P/Invoke or similar

**Recommendation**: Use **Windows.Gaming.Input** - it's battle-tested, officially supported, and requires zero external dependencies.

**Migration Effort**: Medium (1-2 hours for refactoring)

### 2.2 Configuration & Settings

**Current**:
```csharp
Properties.Settings.Default.PictureFolder
Properties.Settings.Default.UseNfcScreenApi
```

**Target**:
```csharp
IConfiguration["Settings:PictureFolder"]
// or
IOptions<AppSettings> appSettings
```

**Implementation Strategy**:
1. Create `AppSettings` POCO class
2. Configure in `Startup.cs` or `Program.cs`
3. Create `ISettingsService` interface
4. Implement `SettingsService` with dependency injection
5. Replace all `Properties.Settings.Default.*` calls

**Files Affected**:
- `AdbController.cs` (10+ references)
- `CameraForm.cs` (3+ references)
- `MainForm.cs` (4+ references)

**Migration Effort**: Medium-High (2-3 hours)

### 2.3 MSTest Framework Update

**Current**: MSTest.TestFramework 1.2.0  
**Target**: MSTest v3.x (latest) + Moq 4.x

**Additional Packages**:
- `MSTest.TestFramework` (v3.x)
- `MSTest.TestAdapter` (v3.x)
- `Moq` (v4.x) - for mocking
- `Shouldly` (optional) - for assertions

**Migration Effort**: Low (30 minutes, mostly automated)

---

## Part 3: Project File Structure Refactoring

### 3.1 New Project File Format

**Current**: Legacy `.csproj` format (2015-era)  
**Target**: Modern SDK-style `.csproj` (net10.0)

**Automatic via Visual Studio**: When upgrading, Visual Studio can auto-generate the new format.

**Example Modern Format**:
```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <RootNamespace>MagnusAkselvoll.AndroidPhotoBooth.App</RootNamespace>
    <AssemblyName>android-photo-booth-app</AssemblyName>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

### 3.2 File Organization Best Practices

**Current**: Some loose organization, mostly flat file structure  
**Target**: Organized by domain/feature

**Proposed Structure**:

```
android-photo-booth-app/
├── Program.cs (entry point)
├── UI/
│   ├── Forms/
│   │   ├── MainForm.cs
│   │   ├── MainForm.Designer.cs
│   │   └── MainForm.resx
│   │   ├── PictureForm.cs
│   │   ├── PictureForm.Designer.cs
│   │   └── PictureForm.resx
│   └── Controls/
│       └── SlideshowControl.cs
├── Services/
│   └── NativeWindowsServices.cs (for P/Invoke)
├── Models/
│   ├── HistoryQueue.cs
│   └── ImageChosenEventArgs.cs
├── Enums/
│   └── InterruptReason.cs
├── Config/
│   ├── AppSettings.cs
│   └── appsettings.json
└── Properties/
    └── AssemblyInfo.cs (may be auto-generated)

android-photo-booth-camera/
├── Program.cs
├── Services/
│   ├── AdbController.cs
│   ├── AndroidDevice.cs
│   └── JoystickObserver.cs
├── UI/
│   ├── Forms/
│   │   ├── CameraForm.cs
│   │   ├── CameraForm.Designer.cs
│   │   └── CameraForm.resx
│   │   ├── CameraSettingsForm.cs
│   │   ├── CameraSettingsForm.Designer.cs
│   │   └── CameraSettingsForm.resx
├── Models/
│   ├── Countdown.cs
│   ├── CameraType.cs
│   ├── JoystickInfo.cs
│   └── AndroidDevice.cs
├── Logging/
│   ├── Logger.cs
│   ├── LogMessage.cs
│   └── LogMessageLevel.cs
├── Config/
│   ├── CameraSettings.cs
│   └── appsettings.json
└── Properties/
    └── AssemblyInfo.cs

android-photo-booth-app.tests/
├── Unit/
│   ├── Models/
│   │   └── HistoryQueueTests.cs
│   ├── Services/
│   │   └── AdbControllerTests.cs (new)
│   └── UI/
│       └── SlideshowControlTests.cs (new)
└── appsettings.json
```

---

## Part 4: Detailed Upgrade Plan

### Phase 1: Assessment & Project Setup (Estimated: 2-3 hours)

**Goal**: Set up the development environment and create modern project files

**Tasks**:
1. ✅ Deep analysis of codebase (THIS DOCUMENT)
2. Create new `.csproj` files in modern SDK format
3. Create `Program.cs` entry point (replaces `Properties/AssemblyInfo.cs`)
4. Migrate project structure to organized folders
5. Verify Visual Studio project loading
6. Create migration checklist document

**Outputs**:
- Modern `.csproj` files
- New folder structure in place
- Project loads without errors in Visual Studio

**Commits**:
- "chore: setup net10.0 project structure"

---

### Phase 2: NuGet & Dependency Updates (Estimated: 2-4 hours)

**Goal**: Update all NuGet dependencies and resolve compatibility issues

**Tasks**:
1. Update MSTest framework (v1.2 → v3.x)
2. Add Moq for mocking
3. Replace SharpDX with Windows.Gaming.Input
4. Review all assembly bindings (if any)
5. Run initial build (expect many errors)
6. Document breaking changes found

**Key Changes**:
- Remove `packages.config`
- Add PackageReference entries to `.csproj`
- Update test project dependencies

**Outputs**:
- All NuGet packages updated
- `packages.config` replaced
- Project structure accepts PackageReferences

**Expected Issues**:
- Compilation errors from SharpDX removal
- Missing type errors from changed APIs
- Test framework API changes

**Commits**:
- "chore: update dependencies to net10.0 compatible versions"
- "chore: remove SharpDX, replace with Windows.Gaming.Input"
- "chore: update MSTest framework to v3.x"

---

### Phase 3: Configuration & Settings Migration (Estimated: 3-4 hours)

**Goal**: Migrate from `App.config`/`Properties.Settings` to `appsettings.json`/`IConfiguration`

**Tasks**:
1. Create `appsettings.json` template
2. Create `AppSettings` POCO class
3. Create `ISettingsService` interface
4. Implement `SettingsService`
5. Set up dependency injection in `Program.cs`
6. Replace all `Properties.Settings.Default` references
7. Update `CameraForm`, `MainForm`, `AdbController`
8. Remove `App.config` and `Settings.settings`

**Key Components**:
```csharp
// AppSettings.cs - POCO for configuration
public class AppSettings
{
    public string PictureFolder { get; set; }
    public string DeviceImageFolder { get; set; }
    public bool UseNfcScreenApi { get; set; }
    // ... other settings
}

// appsettings.json
{
  "AppSettings": {
    "PictureFolder": "",
    "DeviceImageFolder": "/sdcard/DCIM/Camera/",
    "UseNfcScreenApi": false
  }
}
```

**Files Modified**:
- AdbController.cs (~15 changes)
- CameraForm.cs (~5 changes)
- CameraSettingsForm.cs (~5 changes)
- MainForm.cs (~8 changes)
- All references to Properties.Settings.Default

**Commits**:
- "refactor: migrate to IConfiguration-based settings"
- "refactor: update AdbController to use ISettingsService"
- "refactor: update UI forms to use ISettingsService"

---

### Phase 4: Joystick Input Refactoring (Estimated: 2-3 hours)

**Goal**: Replace SharpDX/DirectInput with Windows.Gaming.Input

**Key Components**:
- `JoystickObserver.cs` - needs complete rewrite
- `JoystickInfo.cs` - may need updates
- `CameraForm.cs` - integration points

**Current SharpDX Usage**:
```csharp
using SharpDX.DirectInput;
private DirectInput _directInput;
private Joystick _joystick;
```

**New Windows.Gaming.Input Approach**:
```csharp
using Windows.Gaming.Input;
var gamepads = Gamepad.Gamepads;
var gamepad = gamepads.FirstOrDefault();
```

**Tasks**:
1. Rewrite `JoystickObserver.cs` using Windows.Gaming.Input
2. Test joystick detection and button mapping
3. Update `CameraForm.cs` integration
4. Add unit tests for joystick observer

**Outputs**:
- Fully functional joystick input using native Windows APIs
- Backward-compatible button mapping
- Unit tests for joystick detection

**Commits**:
- "refactor: replace SharpDX with Windows.Gaming.Input"
- "test: add joystick observer tests"

---

### Phase 5: P/Invoke & Windows-Specific Code (Estimated: 1-2 hours)

**Goal**: Ensure all Windows-specific code (P/Invoke, native calls) works correctly

**Current Usage**:
- `NativeMethods.cs` - `SetThreadExecutionState` for preventing sleep
- Various process/file operations

**Tasks**:
1. Review all P/Invoke declarations
2. Add RuntimeIdentifier for Windows-only targeting
3. Test P/Invoke calls work correctly
4. Optionally: Refactor NativeMethods into service

**Project File Configuration**:
```xml
<PropertyGroup>
  <TargetFramework>net10.0-windows</TargetFramework>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <!-- or for cross-platform support with optional Windows features -->
  <SelfContained>true</SelfContained>
</PropertyGroup>
```

**Commits**:
- "chore: configure project for Windows P/Invoke support"

---

### Phase 6: Testing & Modernization (Estimated: 3-4 hours)

**Goal**: Modernize test infrastructure and improve coverage

**Tasks**:
1. Update test framework decorators to modern MSTest
2. Replace old MSTest assertions with modern patterns
3. Add tests for new services (SettingsService, etc.)
4. Add tests for JoystickObserver
5. Add basic integration tests
6. Verify all tests pass

**Current Test Structure**:
- `HistoryQueueTests.cs` (basic unit tests)

**New Test Structure**:
```
Tests/
├── Unit/
│   ├── Models/HistoryQueueTests.cs
│   ├── Services/AdbControllerTests.cs (new)
│   ├── Services/SettingsServiceTests.cs (new)
│   └── Services/JoystickObserverTests.cs (new)
└── Integration/
    └── AdbControllerIntegrationTests.cs (new)
```

**Modern MSTest Example**:
```csharp
[TestClass]
public class HistoryQueueTests
{
    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Constructor_NegativeCapacity_ThrowsArgumentException()
    {
        // Arrange & Act
        var queue = new HistoryQueue<int>(-1);
        
        // Assert - exception should be thrown
    }
}
```

**Commits**:
- "test: modernize test framework and add service tests"
- "test: improve test coverage for critical components"

---

### Phase 7: Final Integration & Cross-Platform Considerations (Estimated: 2-3 hours)

**Goal**: Test full application functionality and evaluate cross-platform support

**Tasks**:
1. Build and run application
2. Test all forms load correctly
3. Test settings persistence
4. Test joystick input
5. Test camera control (if ADB available)
6. Evaluate Mac/Linux support possibilities
7. Create deployment configuration

**Cross-Platform Analysis**:
- ✅ Joystick input: Windows-only (cannot easily cross-platform)
- ✅ P/Invoke (SetThreadExecutionState): Windows-only
- ⚠️ ADB Process execution: Cross-platform capable
- ⚠️ File I/O: Cross-platform capable
- ❌ Windows Forms: Windows-only

**Recommendation**: Keep as Windows-only application. Refactoring for cross-platform would require:
1. Replacing Windows Forms with WPF or cross-platform UI (significant work)
2. Creating platform-specific input handlers
3. Moving to web UI (React/Angular) - major redesign

**For now**: Configure as Windows-only `net10.0-windows`

**Commits**:
- "chore: finalize net10.0 migration"
- "docs: add platform support documentation"

---

## Part 5: Risk Assessment & Mitigation

### Risks Identified

| Risk | Severity | Probability | Mitigation |
|------|----------|-------------|-----------|
| SharpDX removal breaks joystick | HIGH | MEDIUM | Test Windows.Gaming.Input thoroughly; create branch for fallback |
| Settings migration loses data | MEDIUM | LOW | Create settings converter; test with real config files |
| Windows Forms designer compatibility | MEDIUM | LOW | Test form loading; use preview in VS2022 |
| P/Invoke breaks on different Windows versions | LOW | LOW | Target Windows 10+; test on multiple systems |
| Async/await issues | LOW | MEDIUM | Code review AdbController async patterns |
| Build performance regression | LOW | LOW | Monitor build times; profile if needed |

### Mitigation Strategies

1. **Branch Strategy**: Create feature branch `dev/copilot-refactor` for all changes
2. **Testing**: Run tests after each phase; keep integration tests passing
3. **Documentation**: Update README.md with new build/run instructions
4. **Rollback Plan**: Keep `.NET Framework 4.7.2` version on separate branch
5. **Communication**: Document breaking changes in CLAUDE.md

---

## Part 6: Build & Deployment Considerations

### 6.1 Build Configuration

**Modern .csproj Configuration**:
```xml
<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishReadyToRun>true</PublishReadyToRun>
    
    <!-- Nullable reference types for better code quality -->
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    
    <!-- Versioning -->
    <Version>2.0.0</Version>
    <InformationalVersion>2.0.0</InformationalVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MSTest.TestFramework" Version="3.x" />
    <PackageReference Include="Windows.Gaming.Input" Version="..." />
    <PackageReference Include="Moq" Version="4.x" />
  </ItemGroup>
</Project>
```

### 6.2 Deployment

**Self-Contained Deployment**:
```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

**Output**: Standalone executable that doesn't require .NET Core installed

### 6.3 GitHub Actions / CI/CD

**Recommended Build Script**:
```yaml
- name: Build
  run: dotnet build --configuration Release

- name: Test
  run: dotnet test --configuration Release --no-build

- name: Publish
  run: dotnet publish -c Release -r win-x64 --self-contained
```

---

## Part 7: Recommended Implementation Order

**Recommended Sequence** (follows dependency chain):

1. **Phase 1**: Project setup (2-3h)
2. **Phase 2**: Dependencies (2-4h)
3. **Phase 3**: Settings migration (3-4h) [Before joystick changes]
4. **Phase 4**: Joystick refactor (2-3h)
5. **Phase 5**: P/Invoke verification (1-2h)
6. **Phase 6**: Testing improvements (3-4h)
7. **Phase 7**: Final integration (2-3h)

**Total Estimated Effort**: 15-23 hours of focused work

**Per-Phase Commits**: 8-12 commits total (logical, reviewable changes)

---

## Part 8: Success Criteria

✅ **Phase Complete When**:

1. **Phase 1**: Projects load without errors, folder structure reorganized
2. **Phase 2**: All NuGet dependencies updated, no reference errors
3. **Phase 3**: Settings fully migrated, `Properties.Settings` references gone
4. **Phase 4**: Joystick input functional with Windows.Gaming.Input
5. **Phase 5**: All P/Invoke calls work, no platform-specific compilation errors
6. **Phase 6**: All tests pass, coverage ≥ 70% of critical paths
7. **Phase 7**: Full application tested, documentation updated

**Final Success**: Application builds, runs, and functions identically to .NET Framework 4.7.2 version, but on .NET Core 10.

---

## Part 9: Appendix - Key Files Involved

### Files That Will Be Heavily Modified

| File | Changes | Difficulty |
|------|---------|------------|
| android-photo-booth-app.csproj | Complete rewrite to SDK format | Easy |
| android-photo-booth-camera.csproj | Complete rewrite to SDK format | Easy |
| Program.cs (both projects) | Entry point setup, DI configuration | Medium |
| AdbController.cs | Settings migration (~15 changes) | Medium |
| MainForm.cs | Settings, dependency injection | Medium |
| JoystickObserver.cs | Complete rewrite for Windows.Gaming.Input | Hard |
| NativeMethods.cs | Minor updates for P/Invoke targeting | Easy |
| App.config | Remove, replace with appsettings.json | Easy |
| packages.config | Remove, migrate to SDK style | Easy |

### Files That Will Be Created

| File | Purpose |
|------|---------|
| AppSettings.cs | Configuration POCO |
| ISettingsService.cs | Settings service interface |
| SettingsService.cs | Settings service implementation |
| appsettings.json (both projects) | Configuration file |
| DirectoryStructure folders | Organized code structure |

### Files That Will Be Deleted

| File | Reason |
|------|--------|
| packages.config | SDK style doesn't use this |
| App.config | Replaced by appsettings.json |
| Properties/AssemblyInfo.cs | Auto-generated in modern .csproj |
| Properties/Settings.settings | Replaced by IConfiguration |
| Properties/Settings.Designer.cs | Auto-generated, no longer needed |

---

## Part 10: Open Questions & Decision Points

**For User to Decide**:

1. **Cross-Platform Support**: Worth investigating Mac/Linux support, or stay Windows-only?
   - Recommendation: Stay Windows-only (WinForms + P/Invoke + SharpDX are fundamentally Windows)

2. **Deployment Model**: 
   - Self-contained executable (larger, no runtime dependency)
   - Framework-dependent (smaller, requires .NET Core installed)
   - Recommendation: Self-contained for easier distribution

3. **Nullable Reference Types**:
   - Enable for better null-safety checks?
   - Recommendation: Enable (`<Nullable>enable</Nullable>`)

4. **Version Bumping**:
   - Should version go 1.x → 2.0.0 to signal major changes?
   - Recommendation: Yes, 2.0.0 marks the .NET Core migration

5. **Backward Compatibility**:
   - Keep .NET Framework 4.7.2 branch alive?
   - Recommendation: Maintain separate `master-net472` branch for historical reference

---

## Summary

The migration from **.NET Framework 4.7.2** to **.NET Core 10** is **well-scoped and achievable**. The codebase has:

✅ **Advantages**:
- Minimal external dependencies (only SharpDX problematic)
- Modern async/await code already in use
- Good separation of concerns
- Small, focused project size
- No database/EF dependencies

⚠️ **Challenges**:
- SharpDX library needs replacement
- Settings system needs migration
- Windows Forms tooling compatibility unknown
- P/Invoke requires Windows-only targeting

✅ **Estimated Timeline**: 15-23 hours over 7 phases, with clear deliverables for each phase

**Next Step**: Proceed with Phase 1 (Project Setup) when ready.

---

**Document Version**: 1.0  
**Last Updated**: November 6, 2025  
**Status**: Ready for Phase 1
