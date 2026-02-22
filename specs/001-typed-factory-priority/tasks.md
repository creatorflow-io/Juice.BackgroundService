# Tasks: Typed Factory Priority for Service Creation

**Input**: Design documents from `specs/001-typed-factory-priority/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Organization**: Tasks are grouped by user story to enable independent
implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label — US1/US2/US3 (setup/foundational phases have no label)

## Path Conventions

- Source: `src/` at repository root
- Tests: `test/` at repository root

---

## Phase 1: Setup — New Interface

**Purpose**: Create the `IServiceFactory<T>` contract in `Abstractions` and
remove the obsolete `IServiceFactory` (non-generic). These two tasks have no
dependency on each other and can run in parallel.

- [x] T001 [P] Create `src/Juice.BgService.Abstractions/IServiceFactory`1.cs` — declare `IServiceFactory<T> where T : class, IManagedService` with method `IManagedService? CreateService<TModel>() where TModel : class, IServiceModel` in namespace `Juice.BgService`
- [x] T002 [P] Delete `src/Juice.BgService/Management/IServiceFactory.cs` — remove the non-generic interface entirely

**Checkpoint**: Solution must compile after both T001 and T002 — the only
consumer of `IServiceFactory` is `ServiceManager`, which is fixed in Phase 2.

---

## Phase 2: Foundational — Repair `ServiceManager` and DI Registration

**Purpose**: Fix the two compilation breaks introduced by deleting `IServiceFactory`.
T003 and T004 are in different files and can run in parallel after T001+T002.

**⚠️ CRITICAL**: Do not begin Phase 3 or later until this phase is complete and the solution builds.

- [x] T003 [P] Update `src/Juice.BgService/Management/ServiceManager.cs` — change private field `IServiceFactory _serviceFactory` to `ServiceFactory _serviceFactory` and update the constructor parameter type from `IServiceFactory serviceFactory` to `ServiceFactory serviceFactory`
- [x] T004 [P] Update `src/Juice.BgService/Management/Extensions/ServiceManagerSeviceCollectionExtensions.cs` — remove the `services.AddSingleton<IServiceFactory, ServiceFactory>()` line; `ServiceFactory` is now registered directly as a singleton without an interface (`services.AddSingleton<ServiceFactory>()`) and injected by concrete type

**Checkpoint**: Foundation complete — solution compiles, all pre-existing tests
pass, no behaviour change yet.

---

## Phase 3: User Story 1 — Typed Factory Priority (Priority: P1) 🎯 MVP

**Goal**: When `IServiceFactory<T>` is registered for service type `T`,
`ServiceFactory` delegates instantiation to it before using the reflection path.

**Independent Test**: Invoke `ServiceFactory.CreateService<TModel>` for a type
that has a registered stub `IServiceFactory<T>`. Assert the stub was called and
the reflection path was NOT taken. Assert null opt-out triggers fallback.

- [x] T005 [US1] Modify `src/Juice.BgService/Management/ServiceFactory.cs` — in `CreateService<TModel>`, after each point where `type` or `t` (plugin) is resolved and verified as `IManagedService`, add: resolve `typeof(IServiceFactory<>).MakeGenericType(resolvedType)` from the relevant `IServiceProvider`; if non-null, call `CreateService<TModel>()` via reflection; if result is non-null return it; if result is null log Debug and fall through; wrap in try/catch and log Warning on exception before falling through
- [x] T006 [US1] Modify `src/Juice.BgService/Management/ServiceFactory.cs` — update `IsServiceExists` to also return `true` when `serviceProvider.GetService(typeof(IServiceFactory<>).MakeGenericType(type))` is non-null (for both app-assembly and plugin branches)
- [x] T007 [P][US1] Create `test/Juice.BgService.Tests.XUnit/TypedFactoryTests.cs` — write xUnit tests: (a) typed factory is invoked when registered and result is returned; (b) typed factory returning null falls back to reflection path; (c) typed factory throwing exception falls back and emits Warning log; (d) unrelated service type does not invoke the typed factory

**Checkpoint**: User Story 1 fully functional and independently testable.

---

## Phase 4: User Story 2 — Backward Compatibility (Priority: P2)

**Goal**: No `IServiceFactory<T>` registered → behaviour identical to
pre-feature release. No source changes required in this phase; validation only.

**Independent Test**: Run existing `PluginsTests.cs` and `ScheduleTests.cs`.
All tests must pass without modification.

- [x] T008 [P][US2] Add backward-compatibility test cases to `test/Juice.BgService.Tests.XUnit/TypedFactoryTests.cs` — verify that when no `IServiceFactory<T>` is registered, `ServiceFactory.CreateService<TModel>` produces an instance via the reflection path and `IsServiceExists` returns the same result as before this feature

**Checkpoint**: Existing test suite passes. User Story 2 independently verified.

---

## Phase 5: User Story 3 — Multiple Typed Factories (Priority: P3)

**Goal**: Registering `IServiceFactory<ServiceA>` and `IServiceFactory<ServiceB>`
results in each factory being called exactly for its own type with zero
cross-invocation.

**Independent Test**: Register two stub factories with counters. Invoke
`ServiceFactory.CreateService<TModel>` for each type. Assert each counter is
exactly 1 and the opposing counter is exactly 0.

- [x] T009 [P][US3] Add multi-factory test cases to `test/Juice.BgService.Tests.XUnit/TypedFactoryTests.cs` — register two distinct counted stub factories for two different service types; call `CreateService` for each type; assert each factory was called exactly once for its own type and zero times for the other type

**Checkpoint**: All three user stories independently testable and passing.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: XML documentation and convenience registration API.

- [x] T010 [P] Add XML doc comments to `IServiceFactory<T>` in `src/Juice.BgService.Abstractions/IServiceFactory`1.cs` — document the interface, the type parameter `T`, the method, the `TModel` parameter, and the null-return opt-out behaviour (see contracts/IServiceFactory-T.md for wording)
- [x] T011 [P] Add `AddServiceFactory<TService, TFactory>` extension method to `src/Juice.BgService/Management/Extensions/ServiceManagerSeviceCollectionExtensions.cs` — registers `TFactory` as `IServiceFactory<TService>` singleton; constraints: `TService : class, IManagedService`, `TFactory : class, IServiceFactory<TService>`
- [x] T012 Run all tests in `test/Juice.BgService.Tests.XUnit/` and confirm full suite passes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (T001, T002)**: Independent of each other — start both immediately
- **Phase 2 (T003, T004)**: Both depend on T002 completing; run in parallel with each other
- **Phase 3 (T005, T006)**: T005 depends on T001+T004 (interface exists, DI fixed); T006 depends on T005; T007 [P] with T005 (different file)
- **Phase 4 (T008)**: Depends on Phase 3 completion
- **Phase 5 (T009)**: Depends on Phase 3 completion; can run in parallel with Phase 4
- **Phase 6 (T010, T011)**: Independent of each other; both depend on Phase 3 completion; T012 last

### Within Phase 3

- T005 before T006 (same file, sequential edits)
- T007 can start in parallel with T005 (different file — test file vs source file)

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 1 + Phase 2 — no dependency on US2 or US3
- **US2 (P2)**: Depends on Phase 1 + Phase 2 — no dependency on US1 or US3 (backward compat is inherent)
- **US3 (P3)**: Depends on Phase 3 US1 completion (needs typed factory mechanism working)

---

## Parallel Opportunities

### Phase 1 (run together)

```
Task: T001 — Create IServiceFactory`1.cs in Abstractions
Task: T002 — Delete IServiceFactory.cs from Juice.BgService
```

### Phase 2 (run together after T002)

```
Task: T003 — Update ServiceManager.cs field type
Task: T004 — Update ServiceManagerSeviceCollectionExtensions.cs DI registration
```

### Phase 3 (T007 alongside T005)

```
Task: T005 — Modify ServiceFactory.cs (typed lookup logic)
Task: T007 — Create TypedFactoryTests.cs (US1 test cases)   ← different file, parallel
```

### Phase 4 + Phase 5 (run together after Phase 3)

```
Task: T008 — Add US2 backward-compat tests to TypedFactoryTests.cs
Task: T009 — Add US3 multi-factory tests to TypedFactoryTests.cs
```

### Phase 6 (run together after Phase 3)

```
Task: T010 — XML doc on IServiceFactory<T>
Task: T011 — Add AddServiceFactory extension method
```

---

## Implementation Strategy

### MVP (User Story 1 Only)

1. Complete Phase 1 (T001, T002) in parallel
2. Complete Phase 2 (T003, T004) in parallel
3. Complete Phase 3 (T005 → T006, T007 parallel)
4. **STOP and VALIDATE**: run `TypedFactoryTests` — stub factory called, fallback works
5. Merge — MVP delivers typed factory priority with no regression

### Incremental Delivery

1. MVP above → typed factory priority working
2. Add Phase 4 (T008) → backward-compat verified by tests
3. Add Phase 5 (T009) → multi-factory isolation verified
4. Add Phase 6 (T010, T011, T012) → polish and convenience API

---

## Notes

- [P] tasks = different files, no dependencies between them
- T002 + T003 must be completed before the solution compiles again — do not leave the repo in a broken state between T002 and T003
- `ServiceFactory.cs` has two independent lookup points (app-assembly branch and plugin branch) — T005 covers both in one task; they are closely coupled in the same method
- The `IServiceFactory<>` open-generic type lookup (`MakeGenericType` + `GetService`) is the only runtime-reflection step added; no `MethodInfo` caching is required for this feature scope
- Version bump reminder: `Juice.BgService` → MAJOR bump; `Juice.BgService.Abstractions` → MINOR bump
