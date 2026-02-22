# Research: Typed Factory Priority for Service Creation

**Feature**: 001-typed-factory-priority
**Date**: 2026-02-22

---

## Decision 1: Where does `IServiceFactory<T>` live?

**Decision**: Declare `IServiceFactory<T>` in `Juice.BgService.Abstractions`.

**Rationale**: Background service implementation projects (plugins, service
libraries) only reference `Abstractions`. Placing `IServiceFactory<T>` there
lets those projects implement a typed factory without taking a dependency on
`Juice.BgService` (the management layer). The namespace `Juice.BgService`
(already the root namespace of `Abstractions`) ensures no naming disruption.

**Alternatives considered**:
- Place in `Juice.BgService` alongside `ServiceFactory` — rejected: forces
  every plugin or service library that wants to provide a typed factory to
  reference the management package, defeating the abstraction boundary.
- Place in `Juice.BgService.ServiceBase` — rejected: ServiceBase is for
  base-class helpers; a factory interface is not a base-class concern, and not
  every service project uses ServiceBase.

---

## Decision 2: How is `IServiceFactory<T>` resolved inside `ServiceFactory`?

**Decision**: Use `IServiceProvider.GetService<IServiceFactory<T>>()` after the
concrete type `T` is resolved. The call is made via reflection:
`typeof(IServiceFactory<>).MakeGenericType(resolvedType)` followed by
`serviceProvider.GetService(genericFactoryType)`.

**Rationale**: `ServiceFactory` receives the type only as a `string` at runtime,
so the generic type argument cannot be expressed at compile time. Constructing
the closed generic via `MakeGenericType` and calling `GetService` with the
`Type` overload is the standard .NET DI pattern for runtime-generic resolution.

**Alternatives considered**:
- Keyed services (net8+): `GetKeyedService<IServiceFactory>(typeName)` —
  rejected: forces a different registration API; loses the type-safety benefit
  of the generic interface.
- Dictionary of factories registered at startup: rejected — adds configuration
  overhead for consumers; DI-based lookup is idiomatic and zero-overhead if no
  typed factory is registered.

---

## Decision 3: Plugin branch — which `IServiceProvider` is used for lookup?

**Decision**: When the type is resolved from a plugin, the typed factory lookup
uses the **plugin's own `ServiceProvider`** (`p.ServiceProvider`), not the host
`_serviceProvider`.

**Rationale**: The plugin's `ServiceProvider` is the composition root for
plugin-specific types. A typed factory for a plugin service is meaningfully
registered there. The host container does not know about plugin-specific types
and cannot meaningfully host a factory for them.

**Alternatives considered**:
- Always use host `_serviceProvider`: rejected — host DI cannot resolve
  plugin-scoped `IServiceFactory<T>` registrations that reference plugin types.

---

## Decision 4: `IsServiceExists` with typed factory

**Decision**: `IsServiceExists` returns `true` if the resolved type passes
`IsAssignableTo(typeof(IManagedService))` **OR** if a typed
`IServiceFactory<T>` is registered for it in the relevant provider. This covers
the case where a typed factory is registered for a type that might not directly
satisfy the `IsAssignableTo` check under edge conditions.

**Rationale**: Consistent with FR-007 in the spec. In practice, any type
wrapped by a typed factory should already be an `IManagedService`; the check
prevents a silent skip of a validly registered factory.

---

## Decision 5: Error handling in typed factory delegation

**Decision**: Wrap the typed factory call in a `try/catch(Exception)`. On
exception, log at `Warning` level with the exception details and fall through
to the default reflection path. On `null` return, log at `Debug` level and fall
through.

**Rationale**: Factory errors should never prevent service startup when a
working default path exists. `Warning` for exceptions (unexpected), `Debug` for
intentional null (factory opted out).

---

## Decision 6a: Fate of `IServiceFactory` (non-generic)

**Decision**: Remove `IServiceFactory` (non-generic). `ServiceManager` is
updated to reference `ServiceFactory` directly rather than through the interface.

**Rationale**: `IServiceFactory` existed solely to allow injection of the
factory into `ServiceManager`. With `IServiceFactory<T>` as the per-type
extension point, there is no meaningful reason to swap the entire factory at the
host level. Keeping the non-generic interface alongside the generic one would
confuse consumers about which to implement. Removing it simplifies the API
surface and removes the ambiguity.

**Impact**: This is a **MAJOR** breaking change for `Juice.BgService`. Any
consumer who registered a custom `IServiceFactory` implementation must migrate.
In practice, the only registered implementation has always been `ServiceFactory`
itself (via `AddBgService`), so real-world migration impact is low.

**Alternatives considered**:
- Keep `IServiceFactory` as a deprecated alias — rejected: adds noise; the
  deprecation path is unclear since there is no replacement at the same
  abstraction level.
- Keep `IServiceFactory` and make `ServiceFactory` implement both — rejected:
  perpetuates the confusion the user explicitly wants to eliminate.

---

## Decision 6: `IServiceFactory<T>` interface shape

**Decision**: Mirror the non-generic `IServiceFactory` interface but constrain
to type `T`:

```csharp
public interface IServiceFactory<T> where T : class, IManagedService
{
    IManagedService? CreateService<TModel>(T? hint = default)
        where TModel : class, IServiceModel;
}
```

After further consideration: the factory receives the `IServiceProvider`
implicitly through DI construction. The method signature only needs to match the
call site in `ServiceFactory` — which needs a `TModel` parameter to call
`CreateService<TModel>`. No additional parameters are required; the type `T` is
already known from the interface binding.

**Final shape**:

```csharp
public interface IServiceFactory<T> where T : class, IManagedService
{
    IManagedService? CreateService<TModel>()
        where TModel : class, IServiceModel;
}
```

**Alternatives considered**:
- Pass `IServiceProvider` as method parameter: rejected — DI injection into the
  factory class itself is the idiomatic approach; method-level provider
  threading is an anti-pattern.
