# Refactoring Guidelines for android-photo-booth

This document outlines the approach and instructions for the ongoing refactor of the android-photo-booth project to modernize it to .NET Core 10 with improved architecture and stability.

## Project Overview
- **Language**: C#
- **Target Framework**: .NET Core 10
- **Type**: Windows Forms Desktop Application (may support Mac/Linux)
- **Main Components**: 
  - `android-photo-booth-app`: Main UI application
  - `android-photo-booth-camera`: Camera control via ADB
  - `android-photo-booth-app.tests`: Unit tests using MSTest

## Primary Refactoring Goals
1. **Migrate to modern libraries** and improve stability
2. **Implement dependency injection** where useful
3. **Upgrade to .NET Core 10** and remove .NET Framework dependencies
4. **Improve cross-platform support** (Windows primary, but Mac/Linux if feasible)
5. **Enhance testability** with modern MSTest architecture
6. **Reduce technical debt** and improve code maintainability

## Code Style & Standards

### Naming Conventions
- **Private fields**: Use `_camelCase` prefix (e.g., `_fieldName`)
- **Properties**: Use `PascalCase` (e.g., `PropertyName`)
- **Methods**: Use `PascalCase` (e.g., `MethodName()`)
- **Constants**: Use `UPPER_CASE` (e.g., `MAX_RETRIES`)
- Follow **Microsoft C# Coding Conventions** as the standard

### Comments & Documentation
- **Minimal comments**: Code should be self-explanatory through clear naming and structure
- **Use XML documentation** on public classes, methods, and properties
- **Comments for "why"** not "what": Explain non-obvious logic or business decisions
- **No commented-out code**: Delete or handle with version control

### File Organization
- **One class per file** (with rare exceptions for tiny helper classes)
- **File name matches class name** exactly (case-sensitive on non-Windows)
- **Folder structure follows namespace structure**
  - Example: `Services/Camera/` folder → `MagnusAkselvoll.AndroidPhotoBooth.Services.Camera` namespace
- **Logical grouping**: Related classes grouped in folders that reflect their domain

## Architectural Approach

### Dependency Injection
- Use built-in .NET Core `IServiceCollection` and `IServiceProvider`
- Wire up DI at application startup (in `Program.cs`)
- Constructor injection for all dependencies
- Useful for: Camera control, settings management, UI services, logging

### Design Patterns
- **Separation of Concerns**: UI, Business Logic, and Infrastructure clearly separated
- **Interface-based design**: Define interfaces for key services, making them mockable
- **Service Layer Pattern**: Business logic in dedicated service classes
- **Repository Pattern**: For data access (if applicable)

### Testing Strategy
- **MSTest framework** with modern architecture
- **Unit tests** for business logic (services, controllers, utilities)
- **Arrange-Act-Assert** pattern for test structure
- **Mock dependencies** using Moq or similar
- **Test project structure** mirrors source project structure
- **One test file per class** being tested
- **Aim for high coverage** of critical paths, especially camera control logic

## Breaking Changes
- **Allowed**: We can make breaking changes if they improve architecture
- **Rationale**: This is a refactor, not a maintenance release
- **Goal**: End result should be cleaner and more maintainable than before

## Dependencies
- **NuGet packages**: Can add free, well-supported packages with active maintenance
- **Selection criteria**: 
  - Active community/official support
  - Regular updates
  - Good download numbers on NuGet
  - License: MIT, Apache 2.0, or similar permissive licenses preferred
- **Current candidates to evaluate**:
  - Dependency injection: Built-in .NET Core DI (Microsoft.Extensions.DependencyInjection)
  - Logging: Serilog or built-in .NET Core logging
  - Configuration: Built-in .NET Core Configuration
  - Testing: MSTest, Moq for mocking

## Communication Protocol

### Before Any Changes
1. **Present the big picture**: I will outline my entire proposed approach
2. **Show scope**: Explain which files/components will be affected
3. **Explain rationale**: Why these changes improve the codebase
4. **Wait for approval**: You review and give feedback before I make changes

### Making Changes
- Changes are made incrementally and logically grouped
- Each logical change is commit-ready (you handle the commits)
- You will commit often between phases

### Working Incrementally
- **One piece at a time**: We complete each phase before moving to the next
- **Iterative**: If a phase needs adjustment, we adjust and continue
- **Flexible**: We can pause, review, and adjust the overall strategy anytime

## Current Status
- [ ] Phase 1: Project structure and tooling assessment
- [ ] Phase 2: Upgrade to .NET Core 10 and fix compilation issues
- [ ] Phase 3: Implement dependency injection
- [ ] Phase 4: Refactor core services
- [ ] Phase 5: Improve testing infrastructure
- [ ] Phase 6: Cross-platform considerations
- [ ] Phase 7: Final cleanup and optimization

## Notes
- The app currently runs on Windows; cross-platform support would require testing on Mac/Linux
- Focus on stability and maintainability over adding new features
- The ADB controller is critical—extra care needed when refactoring camera control logic
