# Quickstart: Typed Factory Priority for Service Creation

**Feature**: 001-typed-factory-priority
**Date**: 2026-02-22

---

## Scenario

You have a custom background service `MyService` that requires dependencies or
initialization logic that the default reflection-based factory cannot supply.
Register a typed factory to take control of `MyService` creation.

---

## Step 1 — Implement `IServiceFactory<MyService>`

The interface is in `Juice.BgService.Abstractions` — no reference to the
management package required in your service project.

```csharp
using Juice.BgService;   // namespace of IServiceFactory<T> in Abstractions

public class MyServiceFactory : IServiceFactory<MyService>
{
    private readonly IMySpecialDependency _dep;

    public MyServiceFactory(IMySpecialDependency dep)
    {
        _dep = dep;
    }

    public IManagedService? CreateService<TModel>()
        where TModel : class, IServiceModel
    {
        // Return null to fall back to the default factory.
        return new MyService(_dep);
    }
}
```

---

## Step 2 — Register in DI

```csharp
// In Program.cs / Startup.cs, alongside AddBgService:
builder.Services
    .AddBgService(configuration.GetSection("BackgroundService"))
    .UseFileStore(configuration.GetSection("BackgroundService:Store"));

// Register the typed factory:
builder.Services.AddSingleton<IServiceFactory<MyService>, MyServiceFactory>();
```

No changes to `AddBgService` or `ServiceManager` are required.

---

## Step 3 — Verify

When `ServiceManager` initialises and encounters `MyService` by its
assembly-qualified name, `ServiceFactory` will:

1. Resolve `MyService` type from `Type.GetType(...)`.
2. Find `IServiceFactory<MyService>` in DI.
3. Call `MyServiceFactory.CreateService<ServiceModel>()`.
4. Use the returned instance — no reflection-based instantiation occurs.

If `MyServiceFactory.CreateService` returns `null`, the default reflection
path is used as a fallback and a Debug-level log entry is emitted.

---

## Plugin scenario

For a service type loaded from a plugin, register the typed factory in the
plugin's service collection:

```csharp
// Inside plugin's ConfigureSharedServices or plugin startup:
pluginServices.AddSingleton<IServiceFactory<PluginService>, PluginServiceFactory>();
```

The host `ServiceFactory` will find the typed factory in the plugin's
`ServiceProvider` when iterating plugins.

---

## Validation checklist

- [ ] `MyServiceFactory` implements `IServiceFactory<MyService>`.
- [ ] `IServiceFactory<MyService>` is registered in DI before the host starts.
- [ ] `ServiceManager` log shows the service was started (no error about missing
  type or failed factory).
- [ ] No regression: services without a typed factory continue to start
  normally.
