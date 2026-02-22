<!--
==============================================================================
SYNC IMPACT REPORT
==============================================================================
Version change: [TEMPLATE] → 1.0.0 (initial ratification)

Modified principles: N/A (initial creation from template placeholders)

Added sections:
  - Core Principles (I–V)
  - Technology Stack
  - Development Workflow
  - Governance

Removed sections: None (all were placeholders)

Templates requiring updates:
  - .specify/templates/plan-template.md  ✅ Constitution Check gates align
  - .specify/templates/spec-template.md  ✅ Scope and FR guidance aligns
  - .specify/templates/tasks-template.md ✅ Task phases align with principles
  - .specify/templates/agent-file-template.md  ✅ No outdated references found

Follow-up TODOs:
  - None — all placeholders resolved from user input and codebase inspection.
==============================================================================
-->

# Juice BgService Constitution

## Core Principles

### I. Abstractions-First

The `Juice.BgService.Abstractions` project MUST contain all public contracts
(`IManagedService`, `IServiceModel`, `ServiceState`, `ServiceEventArgs`).
No implementation detail may leak into the abstractions layer.
Consuming projects depend only on the abstractions package; concrete
implementations are resolved at the application composition root.

**Rationale**: Decoupling contracts from implementations allows consumers to
substitute service implementations without changing referencing code, and keeps
the public API surface stable across major versions.

### II. Managed Service Lifecycle (NON-NEGOTIABLE)

Every background service MUST implement `IManagedService` and honour the full
lifecycle contract:

- `StartAsync` / `StopAsync` / `RequestStopAsync` — explicit lifecycle control.
- `HealthCheckAsync` — returns `(Healthy, Message)` to support monitoring.
- `SetState(ServiceState, message, data)` — all state transitions MUST go
  through this method; direct field mutation is prohibited.
- `OnChanged` event MUST be raised on every state transition so that
  `ServiceManager` and API layers can react without polling.

`ServiceState` values (`Stopped`, `Starting`, `Running`, `Restarting`,
`RestartPending`, `StoppedUnexpectedly`) are exhaustive; adding new states
is a MINOR version bump to the Abstractions package.

**Rationale**: A consistent lifecycle contract enables `ServiceManager` to
dynamically init, start, stop, and monitor any registered service without
knowledge of its domain logic.

### III. Extension via Inheritance from ServiceBase

Domain-specific background services SHOULD extend one of the concrete base
classes provided by `Juice.BgService.ServiceBase` rather than implementing
`IManagedService` directly:

- `FileWatcherService` — for watch-folder / file-event–driven workloads.
- `ScheduledService` — for recurring / cron-like workloads.
- `BackgroundService` — generic long-running base when neither above fits.

New base classes introduced into ServiceBase MUST be independently usable and
MUST NOT carry hidden dependencies on higher-level packages (`Juice.BgService`,
`Juice.BgService.Api`).

**Rationale**: Providing well-tested base classes eliminates repetitive
boilerplate, enforces the lifecycle contract by default, and guides developers
toward proven patterns.

### IV. Observability & State Transparency

Services MUST expose their runtime state through the standard `IManagedService`
surface (`State`, `Message`, `HealthCheckAsync`). The API layer
(`Juice.BgService.Api`) MUST NOT invent out-of-band status channels.

Structured logging MUST use the `ILogger<T>` abstraction. Log events SHOULD be
defined as constants in `LogEvents.cs` to enable log filtering by event ID.
Service state changes MUST be logged at `Information` level or above.

**Rationale**: Uniform observability allows operators to monitor and diagnose
services without custom tooling. Defined log-event IDs enable filtering in
production log aggregators.

### V. Versioning & Multi-Target Compatibility

All NuGet packages in this solution MUST follow **Semantic Versioning**
(MAJOR.MINOR.PATCH):

- **MAJOR** — breaking changes to public contracts (abstractions, manager API).
- **MINOR** — additive, backwards-compatible public API additions.
- **PATCH** — bug fixes, performance improvements, documentation.

The solution MUST target all actively supported .NET LTS/STS versions declared
in project files. Currently: **net8.0** and **net9.0**.
Dropping a target framework is a MAJOR version bump.

**Rationale**: Library consumers pin to NuGet versions; predictable versioning
and multi-targeting minimise friction for upgrading host applications.

## Technology Stack

- **Language**: C# (latest LTS language version per target framework)
- **Frameworks**: .NET 8.0, .NET 9.0
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Hosting**: Microsoft.Extensions.Hosting (`IHostedService` integration)
- **API**: ASP.NET Core minimal-API / controller pattern (`Juice.BgService.Api`)
- **Persistence**: File-based store (`FileStore`) for service configurations;
  custom `IServiceRepository` implementations are pluggable.
- **Testing**: xUnit with `Microsoft.Extensions.Hosting.Testing` or equivalent
  in-process host; test projects under `test/`.
- **CI target**: Windows Server (primary), Linux (secondary).

## Development Workflow

- Feature branches MUST branch from `master`; release branches (`release/X.Y`)
  are cut from `master` when preparing a release.
- Every public API change MUST update the XML doc comment on the affected
  interface or class before the PR is merged.
- New base classes or service types introduced into `ServiceBase` MUST include
  at least one integration test in the `test/` directory demonstrating their
  lifecycle contract.
- The `Constitution Check` gate in `plan.md` MUST verify: abstraction layering,
  lifecycle compliance, observability hooks, and versioning impact before
  implementation begins.
- Complexity violations (e.g., adding a 5th project to the solution) MUST be
  justified in the `Complexity Tracking` table of `plan.md`.

## Governance

This constitution supersedes all other documented practices for this solution.
Amendments require:

1. A PR updating this file with an incremented `CONSTITUTION_VERSION`.
2. The Sync Impact Report (HTML comment at top) updated to reflect changes.
3. Any affected templates (`.specify/templates/`) updated in the same PR.
4. At least one reviewer approval before merge.

All PRs and code-reviews MUST verify compliance with the five core principles.
Unjustified complexity or abstraction layer violations MUST block merge.

For runtime development guidance consult `.specify/templates/` and the
`speckit.*` command set.

**Version**: 1.0.0 | **Ratified**: 2026-02-22 | **Last Amended**: 2026-02-22
