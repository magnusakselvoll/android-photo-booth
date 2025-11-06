# .NET Core 10 Upgrade - Implementation Checklist

## Phase 1: Project Setup & Modern Tooling ⚙️

### Pre-Flight Checklist
- [ ] Backup current solution or ensure git branch is clean
- [ ] Install Visual Studio 2022 with .NET 10 SDK
- [ ] Verify .NET 10 SDK installed: `dotnet --version` → should show 10.0.x
- [ ] Close solution in Visual Studio before starting

### Project File Modernization
- [ ] Create new `.csproj` files in SDK style for all 3 projects
- [ ] Verify all 3 projects load without errors
- [ ] Remove `App.config` references from .csproj (for app project)
- [ ] Update project GUIDs if needed (should stay same for GitHub)
- [ ] Test: `dotnet build` should work

### Folder Structure Reorganization
- [ ] Create `UI/Forms` folder for android-photo-booth-app
- [ ] Create `UI/Controls` folder for android-photo-booth-app
- [ ] Create `Models` folder for android-photo-booth-app
- [ ] Create `Services` folder for android-photo-booth-app
- [ ] Create `Config` folder for both projects
- [ ] Create `Enums` folder for android-photo-booth-camera
- [ ] Move files to appropriate folders
- [ ] Update namespace declarations in moved files
- [ ] Test: Solution should still compile

### Modern Program.cs Entry Point
- [ ] Create new `Program.cs` for android-photo-booth-app (entry point)
- [ ] Create new `Program.cs` for android-photo-booth-camera (if still needed)
- [ ] Replace WinForms.Application.Run() with modern DI setup
- [ ] Set up ServiceCollection for dependency injection
- [ ] Test: Application should still start

### Completion Criteria
- ✅ All 3 projects build without warnings
- ✅ Project structure is organized by domain
- ✅ Namespaces match folder structure
- ✅ Visual Studio shows no errors

**Estimated Time**: 2-3 hours  
**Commit Message**: `chore: setup net10.0 project structure`

---

## Phase 2: Dependencies & NuGet Updates 📦

### NuGet Package Assessment
- [ ] List all current packages: `dotnet list package --outdated`
- [ ] Document current versions in migration checklist
- [ ] Verify no transitive dependency conflicts

### MSTest Framework Upgrade
- [ ] Update `MSTest.TestFramework` from 1.2.0 to 3.2.2+ (latest)
- [ ] Update `MSTest.TestAdapter` from 1.2.0 to 3.2.2+
- [ ] Remove `packages.config` from test project
- [ ] Add PackageReferences to `.csproj` for test project
- [ ] Add `Moq` v4.x for mocking: `dotnet add package Moq`
- [ ] Test: `dotnet restore` should work

### SharpDX Removal Planning
- [ ] Document all SharpDX usages:
  - [ ] `JoystickObserver.cs` - DirectInput initialization
  - [ ] `JoystickInfo.cs` - data structures (may not change)
  - [ ] Joystick.GetCapabilities()
  - [ ] Joystick state polling
- [ ] Create backup of `JoystickObserver.cs` (save as `.bak` or note changes)
- [ ] Do NOT remove SharpDX references yet (wait for Phase 4)

### Build Verification
- [ ] Run: `dotnet build` on each project
- [ ] Expect errors related to:
  - [ ] SharpDX references (normal, will fix in Phase 4)
  - [ ] Configuration class changes (will fix in Phase 3)
  - [ ] Test framework API changes (will fix in Phase 6)
- [ ] Document all errors for reference

### NuGet Cleanup
- [ ] Verify Windows.Gaming.Input is available (built-in, no install needed)
- [ ] Check for any deprecated NuGet source URLs
- [ ] Update NuGet.config if needed

### Completion Criteria
- ✅ MSTest framework updated to v3.x
- ✅ SharpDX references documented but not yet replaced
- ✅ No missing package dependencies
- ✅ New PackageReferences in `.csproj` files

**Estimated Time**: 2-4 hours  
**Commits**:
- `chore: update MSTest framework to v3.x`
- `chore: update NuGet dependencies for net10.0`

---

## Phase 3: Settings & Configuration Migration 🔧

### Create Configuration Infrastructure

#### Step 1: Create AppSettings POCO
- [ ] Create `Config/AppSettings.cs` in both projects
- [ ] Define all settings from current `Properties/Settings.settings`
- [ ] Properties to include:
  - For android-photo-booth-app:
    - [ ] PictureFolder
    - [ ] MinimumDisplaySeconds
    - [ ] MaximumDisplaySeconds
    - [ ] ShowFileNames
    - [ ] FilenameExtensions
  - For android-photo-booth-camera:
    - [ ] UseNfcScreenApi
    - [ ] CameraApp
    - [ ] DeleteAfterDownload
    - [ ] DeviceImageFolder
    - [ ] PublishFolder
    - [ ] WorkingFolder
    - [ ] etc. (all from current Settings.settings)

#### Step 2: Create ISettingsService Interface
- [ ] Create `Services/ISettingsService.cs`
- [ ] Define methods for all settings get/set operations
- [ ] Use property injection pattern for consistency

#### Step 3: Implement SettingsService
- [ ] Create `Services/SettingsService.cs`
- [ ] Inject `IConfiguration`
- [ ] Implement all methods from interface
- [ ] Add automatic persistence logic

#### Step 4: Create appsettings.json Templates
- [ ] Create `Config/appsettings.json` for android-photo-booth-app
- [ ] Create `Config/appsettings.json` for android-photo-booth-camera
- [ ] Populate with current default values
- [ ] Add comments for each setting

#### Step 5: Update Program.cs (both projects)
- [ ] Set up IConfiguration from appsettings.json
- [ ] Register ISettingsService in ServiceCollection
- [ ] Example:
```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

services.AddSingleton(typeof(IConfiguration), configuration);
services.AddSingleton<ISettingsService, SettingsService>();
```

### Update Code to Use New Settings Service

#### android-photo-booth-app/MainForm.cs
- [ ] Inject ISettingsService via constructor
- [ ] Replace all `Properties.Settings.Default.*` with service calls
- [ ] Count replacements: ~8 changes expected
- [ ] Replace `Settings.Default.Save()` with service method

#### android-photo-booth-camera/AdbController.cs
- [ ] Inject ISettingsService via constructor
- [ ] Replace all `Properties.Settings.Default.*` with service calls
- [ ] Count replacements: ~15 changes expected
- [ ] Review: 
  - [ ] UseNfcScreenApi
  - [ ] CameraApp
  - [ ] DeleteAfterDownload
  - [ ] DeviceImageFolder
  - [ ] WorkingFolder
  - [ ] PublishFolder
  - [ ] etc.

#### android-photo-booth-camera/CameraForm.cs
- [ ] Inject ISettingsService
- [ ] Replace all settings references: ~5 changes expected

#### android-photo-booth-camera/CameraSettingsForm.cs
- [ ] Inject ISettingsService
- [ ] Replace all settings references: ~5 changes expected

### Remove Legacy Configuration Files
- [ ] Delete or archive `Properties/Settings.settings`
- [ ] Delete `Properties/Settings.Designer.cs`
- [ ] Delete `App.config` (for WinForms app)
- [ ] Update `.csproj` to remove references to these files

### Validation & Testing
- [ ] Build all projects: `dotnet build`
- [ ] Verify no compilation errors
- [ ] Application should start without errors
- [ ] Settings should load from appsettings.json
- [ ] Settings should persist (if UI allows saving)

### Completion Criteria
- ✅ All settings migrated to IConfiguration
- ✅ All `Properties.Settings.Default` references removed
- ✅ appsettings.json files created with correct defaults
- ✅ ISettingsService working correctly
- ✅ Application loads settings on startup

**Estimated Time**: 3-4 hours  
**Commits**:
- `refactor: create ISettingsService infrastructure`
- `refactor: migrate android-photo-booth-app settings`
- `refactor: migrate android-photo-booth-camera settings`
- `chore: remove legacy configuration files`

---

## Phase 4: Joystick Input Refactoring 🎮

### SharpDX → Windows.Gaming.Input Migration

#### Analysis Phase
- [ ] Review current `JoystickObserver.cs` implementation
- [ ] Document:
  - [ ] How joystick detection works
  - [ ] Button mapping (which button does what)
  - [ ] Events triggered on button press
  - [ ] State polling mechanism
- [ ] Review `JoystickInfo.cs` data structure
- [ ] Create mapping document: SharpDX method → Windows.Gaming.Input equivalent

#### Create Windows.Gaming.Input Wrapper
- [ ] Create `Services/IGamepadService.cs` interface
- [ ] Create `Services/GamepadService.cs` implementation
- [ ] Methods needed:
  - [ ] DetectGamepads()
  - [ ] GetGamepadState()
  - [ ] IsButtonPressed(GamepadButtons button)
  - [ ] Poll for state changes

#### Rewrite JoystickObserver.cs
- [ ] Replace all `SharpDX.DirectInput` imports with Windows.Gaming.Input
- [ ] Remove `DirectInput _directInput` initialization
- [ ] Remove `Joystick _joystick` member
- [ ] Replace with `Gamepad _gamepad`
- [ ] Rewrite detection logic:
  - Before: `DirectInput.GetDevices(DeviceClass.GameControl)`
  - After: `Gamepad.Gamepads.FirstOrDefault()`
- [ ] Rewrite polling logic:
  - Before: `_joystick.Poll(); _joystick.GetCurrentState()`
  - After: `_gamepad.GetCurrentReading()`
- [ ] Map buttons:
  - Before: `Joystick.ButtonCapabilities`
  - After: `GamepadButtons` enum
- [ ] Create mapping table for button events

#### Update CameraForm.cs Integration
- [ ] Verify CameraForm correctly receives joystick events
- [ ] Test button mappings still work
- [ ] Adjust if button IDs changed

#### Testing
- [ ] Connect physical gamepad/joystick
- [ ] Test detection: should find device
- [ ] Test button presses: events should fire
- [ ] Test multiple buttons
- [ ] Test disconnection/reconnection

### Remove SharpDX References
- [ ] Delete or comment out SharpDX NuGet packages
- [ ] Remove `using SharpDX.DirectInput;`
- [ ] Remove SharpDX from `.csproj` files
- [ ] Verify no other SharpDX references remain

### Completion Criteria
- ✅ Joystick detection works with Windows.Gaming.Input
- ✅ Button presses detected correctly
- ✅ No SharpDX references remain
- ✅ CameraForm integration working
- ✅ Physical testing passed

**Estimated Time**: 2-3 hours  
**Commits**:
- `refactor: replace SharpDX with Windows.Gaming.Input`
- `test: verify gamepad detection and button mapping`

---

## Phase 5: P/Invoke & Windows-Specific Code 🪟

### Windows P/Invoke Audit
- [ ] Review `NativeMethods.cs`:
  - [ ] SetThreadExecutionState (kernel32.dll) - for preventing sleep
  - [ ] Any other P/Invoke calls?
- [ ] Verify all P/Invoke declarations are correct
- [ ] Test P/Invoke calls work on Windows 10+

### Project Configuration for Windows-Only
- [ ] Update `.csproj` files to target Windows only:
```xml
<TargetFramework>net10.0-windows</TargetFramework>
<UseWindowsForms>true</UseWindowsForms>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<SelfContained>true</SelfContained>
```

### P/Invoke Testing
- [ ] Build project: should compile without platform errors
- [ ] Test SetThreadExecutionState:
  - [ ] Application should prevent sleep when running
  - [ ] Lock/Sleep should work when app closed
- [ ] Test on Windows 10 and Windows 11

### Documentation
- [ ] Add comments to NativeMethods.cs explaining Windows-only support
- [ ] Update README.md: "Windows 10+ required"
- [ ] Document why Mac/Linux support is not feasible (WinForms + P/Invoke)

### Completion Criteria
- ✅ All P/Invoke calls work correctly
- ✅ Project compiles with Windows-only targeting
- ✅ No platform-specific errors
- ✅ Sleep prevention working

**Estimated Time**: 1-2 hours  
**Commits**:
- `chore: configure Windows-only P/Invoke support`

---

## Phase 6: Testing & Quality Improvements 📊

### Modernize Existing Tests
- [ ] Update `HistoryQueueTests.cs`:
  - [ ] Replace old MSTest attributes with modern equivalents
  - [ ] Update assertion methods to modern API
  - [ ] Example: `Assert.ThrowsException<T>` instead of `ExpectedException`
- [ ] Test should pass with new MSTest v3.x

### Create Service Tests
- [ ] Create `Unit/Services/SettingsServiceTests.cs`
  - [ ] Test loading settings from configuration
  - [ ] Test settings persistence
  - [ ] Test default values
  
- [ ] Create `Unit/Services/GamepadServiceTests.cs` (if refactored)
  - [ ] Mock gamepad detection
  - [ ] Test button state reading
  - [ ] Test connection/disconnection

### Create Form Tests (Optional)
- [ ] Consider testing form initialization
- [ ] Test button click handlers (if possible with mocking)

### Run All Tests
- [ ] Command: `dotnet test`
- [ ] Verify all tests pass
- [ ] Check code coverage (if coverage tool enabled)
- [ ] Target: ≥70% coverage on critical paths

### Test Infrastructure Setup (Optional)
- [ ] Consider adding `Shouldly` for better assertions
- [ ] Configure code coverage reporting
- [ ] Set up CI/CD test running

### Completion Criteria
- ✅ All tests pass with MSTest v3.x
- ✅ New service tests created
- ✅ Code coverage ≥70% on critical paths
- ✅ `dotnet test` runs successfully

**Estimated Time**: 3-4 hours  
**Commits**:
- `test: modernize test framework to MSTest v3.x`
- `test: add service layer tests`
- `test: improve code coverage`

---

## Phase 7: Final Integration & Validation ✅

### Full Application Testing
- [ ] Build solution: `dotnet build`
- [ ] Run application: Should start without errors
- [ ] MainForm loads correctly
- [ ] Settings load from appsettings.json
- [ ] Test all buttons and forms
- [ ] Test camera controls (if ADB available)
- [ ] Test joystick input (if gamepad available)
- [ ] Test settings saving
- [ ] Test configuration reload on app restart

### Performance Verification
- [ ] Application startup time (compare to old version if noted)
- [ ] Memory usage (compare to old version if available)
- [ ] No memory leaks (light profiling)
- [ ] No unexpected exceptions in logs

### Deployment & Distribution
- [ ] Create self-contained executable:
  ```powershell
  dotnet publish -c Release -r win-x64 --self-contained
  ```
- [ ] Test executable on clean Windows machine (no .NET installed)
- [ ] Verify size is reasonable (~200-300 MB for self-contained)
- [ ] Test installation and basic functionality

### Documentation Updates
- [ ] Update `README.md`:
  - [ ] Change "Requires .NET Framework 4.7.2" to ".NET 10"
  - [ ] Update build instructions
  - [ ] Update system requirements (Windows 10+)
- [ ] Update `CLAUDE.md` with migration completion
- [ ] Create `MIGRATION_NOTES.md` for future reference
- [ ] Document any breaking changes

### Final Code Review
- [ ] Review all Phase 1-6 changes
- [ ] Check for any TODOs or temporary fixes
- [ ] Verify code style consistency
- [ ] Check XML documentation is present

### Create Release Notes
- [ ] Version: 2.0.0 (major version for .NET migration)
- [ ] Highlight: Upgraded to .NET Core 10
- [ ] Note: Windows 10+ required
- [ ] Note: Improved architecture with DI

### Completion Criteria
- ✅ Application fully functional
- ✅ All tests pass
- ✅ Self-contained executable works
- ✅ Documentation updated
- ✅ No known issues

**Estimated Time**: 2-3 hours  
**Commits**:
- `chore: finalize net10.0 migration`
- `docs: update documentation for net10.0`
- `release: version 2.0.0 - .NET Core 10 migration`

---

## Post-Migration Follow-Up

### Optional Improvements (After Core Migration)
- [ ] Nullable reference types: Enable for better null safety
- [ ] Code analysis: Enable Roslyn analyzers
- [ ] Performance: Profile and optimize if needed
- [ ] Testing: Increase coverage beyond 70%
- [ ] Architecture: Consider further DI refactoring

### Maintenance
- [ ] Keep dependencies updated quarterly
- [ ] Monitor .NET 10 updates and patch
- [ ] Watch for deprecated APIs in used libraries
- [ ] Plan .NET 11+ upgrades as released

### GitHub & CI/CD
- [ ] Update GitHub Actions workflows for net10.0
- [ ] Ensure CI/CD pipeline tests on .NET 10 SDK
- [ ] Set up automated testing on PRs
- [ ] Tag release on GitHub

---

## Summary Statistics

| Phase | Estimated Hours | Risk Level | Priority |
|-------|-----------------|-----------|----------|
| 1: Project Setup | 2-3 | Low | 🔴 HIGH |
| 2: Dependencies | 2-4 | Medium | 🔴 HIGH |
| 3: Settings | 3-4 | Medium | 🔴 HIGH |
| 4: Joystick | 2-3 | High | 🟠 MEDIUM |
| 5: P/Invoke | 1-2 | Low | 🟢 LOW |
| 6: Testing | 3-4 | Low | 🟢 LOW |
| 7: Integration | 2-3 | Low | 🟢 LOW |
| **TOTAL** | **15-23** | - | - |

---

## Troubleshooting Quick Reference

### Build Errors
- **"SharpDX not found"**: Expected in Phase 2, fixed in Phase 4
- **"App.config not allowed"**: Normal, fixed in Phase 3
- **"Properties.Settings not found"**: Expected, fixed in Phase 3

### Runtime Errors
- **"File not found"**: Check appsettings.json path
- **"Joystick not detected"**: Verify gamepad connection, test code in Phase 4
- **"Thread execution state error"**: P/Invoke issue, address in Phase 5

### Test Failures
- **"TestClass not found"**: Update MSTest attributes in Phase 6
- **"Assert method not recognized"**: Update assertions to MSTest v3.x API

---

**Checklist Version**: 1.0  
**Last Updated**: November 6, 2025  
**Status**: Ready for Phase 1 implementation
