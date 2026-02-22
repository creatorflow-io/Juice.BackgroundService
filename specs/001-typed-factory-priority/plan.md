# Implementation Plan: Typed Factory Priority for Service Creation

**Branch**: `001-typed-factory-priority` | **Date**: 2026-02-22 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-typed-factory-priority/spec.md`

## Summary

Add a generic `IServiceFactory<T>` interface to `Juice.BgService.Abstractions`
so service implementation projects can provide a typed factory without depending
on the management layer. Modify `ServiceFactory` to check DI for a registered
`IServiceFactory<T>` after resolving the service type, delegating instantiation
before falling back to reflection. Remove the non-generic `IServiceFactory` and
update `ServiceManager` to use `ServiceFactory` directly (MAJOR breaking change).

## Technical Context

**Language/Version**: C# 13 / .NET 8.0 + .NET 9.0
**Primary Dependencies**: `Microsoft.Extensions.DependencyInjection.Abstractions`, `Juice.Plugins.Management` (existing)
**Storage**: N/A
**Testing**: xUnit, FluentAssertions, `Juice.XUnit` — existing projects under `test/`
**Target Platform**: .NET 8.0 and .NET 9.0 (multi-targeted, Windows + Linux)
**Project Type**: Library (NuGet packages)
**Performance Goals**: Zero overhead when no typed factory is registered (single `GetService` null-return, no extra work)
**Constraints**: `IServiceFactory` (non-generic) removed — MAJOR break; `ServiceManager` changes constructor dependency; all net8.0/net9.0 targets maintained
**Scale/Scope**: 1 new file in `Abstractions`; 2 modified files in `Juice.BgService` (`ServiceFactory`, `ServiceManager`); 1 deleted file; 1 new test file

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Status |
|-----------|-------|--------|
| I. Abstractions-First | `IServiceFactory<T>` placed in `Juice.BgService.Abstractions` — the correct layer for contracts consumed by service implementation projects. | ✅ Pass |
| II. Managed Service Lifecycle | No lifecycle changes. Factory creates services; `StartAsync` entry point unchanged. | ✅ Pass |
| III. Extension via ServiceBase | No change to base classes. Feature is factory-layer only. | ✅ Pass |
| IV. Observability & State Transparency | Debug/Warning log entries added for null-return and exceptions via existing `ILogger<ServiceFactory>`. | ✅ Pass |
| V. Versioning & Multi-Target Compatibility | Removing `IServiceFactory` = **MAJOR** version bump on `Juice.BgService`. Adding `IServiceFactory<T>` to `Abstractions` = **MINOR** bump on `Abstractions`. Both net8.0 and net9.0 unaffected. | ✅ Pass — MAJOR bump on `Juice.BgService`, MINOR on `Abstractions` |

**Post-design re-check**: ✅ All gates pass. No complexity violations.

## Project Structure

### Documentation (this feature)

```text
specs/001-typed-factory-priority/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── IServiceFactory-T.md
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Juice.BgService.Abstractions/
│   └── IServiceFactory`1.cs            # NEW — IServiceFactory<T>, namespace Juice.BgService
│
└── Juice.BgService/
    └── Management/
        ├── IServiceFactory.cs          # DELETED — non-generic interface removed
        ├── ServiceFactory.cs           # MODIFIED — typed factory lookup; no longer implements IServiceFactory
        ├── ServiceManager.cs           # MODIFIED — field type IServiceFactory → ServiceFactory
        └── Extensions/
            └── ServiceManagerSeviceCollectionExtensions.cs  # MODIFIED — remove IServiceFactory registration

test/
└── Juice.BgService.Tests.XUnit/
    └── TypedFactoryTests.cs            # NEW — xUnit tests for typed factory priority
```

**Structure Decision**: Minimal cross-project change. `Abstractions` gains one
new file; `Juice.BgService` loses one file (`IServiceFactory.cs`) and has two
modified. Tests extend the existing `Juice.BgService.Tests.XUnit` project.
