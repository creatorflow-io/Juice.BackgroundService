# Contract: `IServiceFactory<T>`

**Package**: `Juice.BgService.Abstractions`
**Namespace**: `Juice.BgService`
**Kind**: Public generic interface (new)

---

## Interface Declaration

```csharp
namespace Juice.BgService
{
    /// <summary>
    /// Type-specific factory for creating instances of <typeparamref name="T"/>.
    /// Register an implementation in DI to override the default reflection-based
    /// instantiation inside <see cref="ServiceFactory"/> for the service type
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Declared in <c>Juice.BgService.Abstractions</c> so background service
    /// implementation projects can implement this interface without depending on
    /// the management layer (<c>Juice.BgService</c>).
    /// </remarks>
    /// <typeparam name="T">
    /// The concrete <see cref="IManagedService"/> type this factory handles.
    /// </typeparam>
    public interface IServiceFactory<T>
        where T : class, IManagedService
    {
        /// <summary>
        /// Creates an instance of <typeparamref name="T"/> configured for use
        /// with model type <typeparamref name="TModel"/>.
        /// </summary>
        /// <typeparam name="TModel">
        /// The service model type used to configure the created service.
        /// </typeparam>
        /// <returns>
        /// A new <see cref="IManagedService"/> instance, or <c>null</c> to
        /// signal that this factory opts out and the default creation path
        /// should be used.
        /// </returns>
        IManagedService? CreateService<TModel>()
            where TModel : class, IServiceModel;
    }
}
```

---

## Consumer Registration

```csharp
// In the host application or plugin startup:
services.AddSingleton<IServiceFactory<MyService>, MyServiceFactory>();

// Or via the convenience extension (if provided):
services.AddServiceFactory<MyService, MyServiceFactory>();
```

Implementing projects need only reference `Juice.BgService.Abstractions`:

```xml
<PackageReference Include="Juice.BgService.Abstractions" Version="x.y.z" />
```

---

## Caller Contract (`ServiceFactory` internal)

`ServiceFactory` (in `Juice.BgService`) invokes this interface as follows:

1. After resolving the concrete type `T` (via `Type.GetType` or plugin scan).
2. Lookup: `serviceProvider.GetService(typeof(IServiceFactory<>).MakeGenericType(T))`
3. If non-null: call `CreateService<TModel>()` via reflection on the result.
4. If result is non-null: return it directly (reflection path skipped).
5. If result is null or lookup returns null: fall through to reflection path.
6. If call throws: log Warning, fall through to reflection path.

---

## Removed Contract: `IServiceFactory` (non-generic)

The non-generic `IServiceFactory` is **removed** in this feature. `ServiceManager`
now uses `ServiceFactory` directly. Consumers providing a custom global factory
must migrate to per-type `IServiceFactory<T>` registrations.

---

## Invariants

| Invariant | Description |
|-----------|-------------|
| Non-breaking (no factory) | No `IServiceFactory<T>` registered → identical behaviour to pre-feature |
| Null opt-out | Returning `null` from `CreateService<TModel>` triggers fallback |
| Exception safety | Exceptions caught; default path always available as fallback |
| Type scoping | A factory for type `A` is NEVER invoked when creating type `B` |
| Plugin scoping | Plugin factories resolved from plugin `ServiceProvider`, not host |
| No management dependency | Implementing projects need only `Abstractions` reference |
