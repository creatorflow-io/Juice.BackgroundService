# Data Model: Typed Factory Priority for Service Creation

**Feature**: 001-typed-factory-priority
**Date**: 2026-02-22

---

## New Interface: `IServiceFactory<T>`

**Package**: `Juice.BgService.Abstractions`
**Namespace**: `Juice.BgService`

```
IServiceFactory<T>
  where T : class, IManagedService
──────────────────────────────────
+ CreateService<TModel>() : IManagedService?
    where TModel : class, IServiceModel
```

**Purpose**: Allows consumers to register a type-specific factory for a
particular `IManagedService` implementation. Declared in `Abstractions` so
background service implementation projects (plugins, service libraries) can
implement it without depending on `Juice.BgService` (the management layer).
When registered in DI, `ServiceFactory` delegates to this interface instead of
using reflection-based instantiation.

**Constraints**:
- `T` MUST be a concrete class implementing `IManagedService`.
- `CreateService<TModel>` MAY return `null` to signal opt-out (triggers
  fallback to default creation logic in `ServiceFactory`).
- Implementations receive their own dependencies via constructor injection.

---

## Removed Interface: `IServiceFactory` (non-generic)

**Package**: `Juice.BgService`
**Change**: **REMOVED** — breaking change, MAJOR version bump required.

`ServiceManager<TModel>` previously depended on `IServiceFactory`. It now
references `ServiceFactory` directly. The per-type extension point
(`IServiceFactory<T>`) replaces the need for a swappable global factory.

**Migration**: Consumers who registered a custom `IServiceFactory` must migrate
to one or more `IServiceFactory<T>` registrations per service type.

---

## Modified Class: `ServiceFactory`

**Package**: `Juice.BgService`
**Change**: No longer implements `IServiceFactory`. Used directly by
`ServiceManager<TModel>`. Internal typed-factory lookup added.

### Resolution flow (updated)

```
CreateService<TModel>(typeAssemblyQualifiedName)
│
├─ 1. type = Type.GetType(assemblyQualifiedName)
│       if type != null AND IsAssignableTo(IManagedService):
│         ├─ 2a. typedFactory = _serviceProvider
│         │         .GetService(typeof(IServiceFactory<>).MakeGenericType(type))
│         │       if typedFactory != null:
│         │         result = typedFactory.CreateService<TModel>()
│         │         if result != null → return result
│         │         else → log Debug, fall through to 2b
│         │       on exception → log Warning, fall through to 2b
│         └─ 2b. return CreateService<TModel>(type, _serviceProvider)
│                       (existing reflection path — unchanged)
│
└─ 3. plugins scan (existing loop)
        for each loaded plugin p:
          t = p.GetType(assemblyQualifiedName)
          if t != null AND IsAssignableTo(IManagedService):
            ├─ 4a. typedFactory = p.ServiceProvider
            │         .GetService(typeof(IServiceFactory<>).MakeGenericType(t))
            │       if typedFactory != null:
            │         result = typedFactory.CreateService<TModel>()
            │         if result != null → return result
            │         else → log Debug, fall through to 4b
            │       on exception → log Warning, fall through to 4b
            └─ 4b. return CreateService<TModel>(t, p.ServiceProvider)
                          (existing reflection path — unchanged)
```

### IsServiceExists flow (updated)

```
IsServiceExists(typeAssemblyQualifiedName)
│
├─ type = Type.GetType(assemblyQualifiedName)
│   if type != null:
│     if IsAssignableTo(IManagedService) → return true
│     if GetService(IServiceFactory<type>) != null → return true
│
└─ plugins scan:
    t = p.GetType(assemblyQualifiedName)
    if t != null:
      if IsAssignableTo(IManagedService) → return true
      if p.ServiceProvider.GetService(IServiceFactory<t>) != null → return true
```

---

## Modified Class: `ServiceManager<TModel>`

**Package**: `Juice.BgService`
**Change**: Field type changed from `IServiceFactory` to `ServiceFactory`;
constructor parameter updated accordingly. No behavioural change.

```
Before: private readonly IServiceFactory _serviceFactory;
After:  private readonly ServiceFactory _serviceFactory;
```

---

## Registration Helper (optional extension method)

A convenience extension on `IServiceCollection` to simplify typed factory
registration, added to `ServiceManagerSeviceCollectionExtensions`:

```
AddServiceFactory<TService, TFactory>(IServiceCollection)
  where TService : class, IManagedService
  where TFactory : class, IServiceFactory<TService>
→ registers TFactory as IServiceFactory<TService> (Singleton)
```

---

## State Transitions

No new `ServiceState` values introduced. Services created by a typed factory
transition through identical states as those created by the reflection path.

---

## Entity Relationships (updated)

```
Juice.BgService.Abstractions
  └─ IServiceFactory<T>  ← NEW (implemented by consumer per service type)

Juice.BgService
  ├─ ServiceFactory  (modified — performs typed factory lookup)
  │    └─ resolves IServiceFactory<T> from DI at runtime
  └─ ServiceManager<TModel>
       └─ uses ServiceFactory directly (IServiceFactory removed)
```
