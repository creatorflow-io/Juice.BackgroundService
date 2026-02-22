# Feature Specification: Typed Factory Priority for Service Creation

**Feature Branch**: `001-typed-factory-priority`
**Created**: 2026-02-22
**Status**: Draft
**Input**: User description: "The service creation should be scanned IServiceFactory<T> for specified service type before fallback to default ServiceFactory (currently)"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Register a Custom Factory for a Specific Service Type (Priority: P1)

A developer wants full control over how a particular background service type is
instantiated — for example, to inject a scoped dependency, provide custom
initialization parameters, or wrap creation with additional logic. They register
a typed factory `IServiceFactory<TService>` in the DI container. When
`ServiceFactory` resolves a service type (either from the application or from a
plugin), it checks for a registered `IServiceFactory<TService>` and delegates
instantiation to it before falling back to its own reflection-based creation
logic.

**Why this priority**: This is the core feature. Without it, the typed factory
concept has no value. All other stories depend on this working correctly.

**Independent Test**: Register `IServiceFactory<MyService>` returning a tracked
stub. Invoke `ServiceFactory.CreateService<TModel>("MyService, Assembly...")`.
Verify the stub's `CreateService` method was called and the default reflection
path was NOT taken.

**Acceptance Scenarios**:

1. **Given** a typed factory `IServiceFactory<MyService>` is registered in DI
   and `MyService` resolves successfully from its assembly-qualified name,
   **When** `ServiceFactory.CreateService<TModel>` is called for `MyService`,
   **Then** the typed factory's `CreateService` method is invoked and the
   reflection-based instantiation path inside `ServiceFactory` is bypassed.

2. **Given** a typed factory `IServiceFactory<MyService>` is registered,
   **When** `ServiceFactory.CreateService<TModel>` is called for a different
   type `OtherService`,
   **Then** the typed factory is NOT consulted and the default reflection path
   is used for `OtherService`.

3. **Given** `MyService` is resolved from a loaded plugin (not the main
   application assembly),
   **When** `ServiceFactory.CreateService<TModel>` is called for `MyService`,
   **Then** the typed factory is checked after the plugin resolves the type, and
   takes priority over the plugin's own reflection-based instantiation.

4. **Given** the typed factory's `CreateService` returns `null`,
   **When** `ServiceFactory` processes the result,
   **Then** it falls back to its own default instantiation logic and logs a
   diagnostic message indicating the typed factory yielded no instance.

---

### User Story 2 - No Typed Factory Registered (Backward Compatibility) (Priority: P2)

An existing application that has not registered any `IServiceFactory<T>` must
continue to work exactly as before. `ServiceFactory` falls through to its
existing logic without error or performance change.

**Why this priority**: Ensures the change is non-breaking. Existing deployments
must not require code changes.

**Independent Test**: Run the existing test suite with no typed factories
registered. All services MUST be created and behave identically to the current
release.

**Acceptance Scenarios**:

1. **Given** no `IServiceFactory<T>` is registered for any service type,
   **When** `ServiceFactory.CreateService<TModel>` is called,
   **Then** service creation behaves identically to the pre-feature behaviour.

---

### User Story 3 - Multiple Typed Factories for Different Service Types (Priority: P3)

A developer registers distinct typed factories for several independent service
types. Each typed factory is invoked only for its matching type, regardless of
whether that type comes from the main application or a plugin.

**Why this priority**: Validates that the resolution mechanism correctly scopes
typed factories to their declared type without cross-contamination.

**Independent Test**: Register two typed factories for two different service
types. Invoke `ServiceFactory.CreateService<TModel>` for each type. Verify each
factory was called exactly once for its own type and zero times for the other.

**Acceptance Scenarios**:

1. **Given** `IServiceFactory<ServiceA>` and `IServiceFactory<ServiceB>` are
   both registered,
   **When** `ServiceFactory` creates instances of `ServiceA` and `ServiceB`,
   **Then** factory A is called for `ServiceA` and factory B is called for
   `ServiceB`, with no cross-invocation.

---

### Edge Cases

- What happens when a typed factory throws an exception during `CreateService`?
  `ServiceFactory` MUST catch the error, log it, and fall back to the default
  reflection-based instantiation rather than propagating the exception.
- What happens when two registrations of `IServiceFactory<T>` exist for the
  same `T`? Standard DI last-wins behaviour applies; `ServiceFactory` MUST NOT
  throw.
- What happens when type resolution from the assembly-qualified name fails
  entirely (neither app nor any plugin contains the type)? The typed factory
  lookup is skipped; the existing error path is unchanged.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `IServiceFactory<T>` MUST be declared in `Juice.BgService.Abstractions`
  so that background service implementation projects can implement it without
  depending on `Juice.BgService` (the management layer).
- **FR-002**: Inside `ServiceFactory`, after the concrete service type `T` is
  resolved (via `Type.GetType` or from a plugin), the factory MUST check whether
  an `IServiceFactory<T>` is registered in the DI container before performing
  reflection-based instantiation.
- **FR-003**: If a matching typed `IServiceFactory<T>` is found, `ServiceFactory`
  MUST delegate `CreateService<TModel>` to it and return the result directly,
  bypassing the reflection path.
- **FR-004**: If the typed factory returns `null`, `ServiceFactory` MUST fall
  back to its own reflection-based instantiation and MUST log a diagnostic
  message.
- **FR-005**: If no typed factory is registered for the resolved type, the
  behaviour MUST be identical to the current pre-feature implementation.
- **FR-006**: The typed factory check MUST occur at the point where the type is
  known — both in the main-assembly resolution branch and in the plugin
  resolution branch of `ServiceFactory`.
- **FR-007**: Errors thrown by a typed factory MUST be caught inside
  `ServiceFactory`, logged, and treated as a `null` return (triggering
  fallback).
- **FR-008**: `IsServiceExists` inside `ServiceFactory` MUST return `true` when
  an `IServiceFactory<T>` is registered for the resolved type, even if the type
  would otherwise fail the `IsAssignableTo(typeof(IManagedService))` check.
- **FR-009**: `IServiceFactory` (non-generic) MUST be removed. `ServiceManager`
  MUST reference `ServiceFactory` directly. This is a **MAJOR** breaking change
  and requires a major version bump on `Juice.BgService`.

### Key Entities

- **IServiceFactory\<T\>** (new generic interface, in `Juice.BgService.Abstractions`):
  Typed factory for a specific service type `T : IManagedService`. Placed in
  Abstractions so background service implementation projects can depend on it
  without referencing the management layer.
- **ServiceFactory** (existing, modified, in `Juice.BgService`): Concrete factory
  used directly by `ServiceManager`. After resolving the concrete type by name,
  it checks for a registered `IServiceFactory<T>` and delegates to it; only
  falls back to its own reflection path when no typed factory is found or when
  the typed factory returns `null`.
- **IServiceFactory** (existing non-generic interface, in `Juice.BgService`):
  Removed — `ServiceManager` now uses `ServiceFactory` directly. Removing this
  interface is a **MAJOR** breaking change; version bump required.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All existing test suites pass without modification when no typed
  factories are registered (zero regression).
- **SC-002**: A typed factory registered for service type `T` is invoked for
  every `CreateService` call targeting `T`, with 100% fidelity (verified by a
  unit test using a counted stub).
- **SC-003**: When a typed factory returns `null` or throws, the default
  reflection path produces the service and a diagnostic log entry is present —
  verifiable in integration tests examining log output.
- **SC-004**: Registering typed factories for N independent service types
  results in zero cross-invocations (each factory called exactly for its own
  type).

## Assumptions

- The typed factory lookup uses `IServiceProvider.GetService<IServiceFactory<T>>()`
  after `T` is resolved. If `T` cannot be resolved at all, the existing error
  path applies unchanged.
- The same DI container (`_serviceProvider`) used by `ServiceFactory` today
  supplies the typed factory lookup for app-hosted types. No additional provider
  reference is needed.
- Plugin `ServiceProvider` is used when the type originates from a plugin; the
  typed factory lookup in the plugin branch uses the plugin's own
  `ServiceProvider`.
- `IsServiceExists` returning `true` for a registered typed factory is
  sufficient — no additional existence probe of the service type itself is
  required in that path.
- Removing `IServiceFactory` (non-generic) is in scope for this feature.
  Consumers who provided custom `IServiceFactory` implementations are expected
  to migrate to `IServiceFactory<T>` per type.
