# .NET → Rust migration scope: Catalog.API

Ticket: [ME-1](https://chloe-fe-demo.atlassian.net/browse/ME-1). First unit only. Draft PR; do not merge.

## Definition of done
- [x] Whole-service inventory (routes, domain, adapters, events, deps)
- [x] Blast radius documented (local vs cross-cutting)
- [x] Sequence covers the service; each domain island is characterize → Rust → wire → parity
- [x] Unit 1 harness `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh` exits 0 via path `A:Catalog.UnitTests` (see `validate.md`)

## Inventory
- Project: `src/Catalog.API/Catalog.API.csproj`, `Microsoft.NET.Sdk.Web`, net10.0, Nullable on. Root `Directory.Build.props` sets `TreatWarningsAsErrors=true` and `UseArtifactsOutput=true` (output under `artifacts/bin/<Project>/<config>/`). Central Package Management.
- Project refs: `EventBusRabbitMQ`, `IntegrationEventLogEF`, `eShop.ServiceDefaults`.
- Hosting: Aspire `AddProject<Projects.Catalog_API>("catalog-api")` (`src/eShop.AppHost/Program.cs:36`) with `catalogdb` (Postgres + pgvector) and RabbitMQ. YARP mobile BFF routes. No Dockerfiles.
- HTTP surface (`Apis/CatalogApi.cs`, `api/catalog`, v1/v2): GET `items`, `items/by`, `items/{id}`, `items/by/{name}`, `items/{id}/pic`, `items/withsemanticrelevance[/{text}]`, `items/type/{typeId}/brand/{brandId?}`, `items/type/all/brand/{brandId?}`, `catalogtypes`, `catalogbrands`; PUT `items`, `items/{id}`; POST `items`; DELETE `items/{id}`.
- Domain: `CatalogItem` (`RemoveStock`, `AddStock` are the only behavior), `CatalogBrand`, `CatalogType`, `CatalogDomainException`.
- Adapters: `CatalogContext` (EF/Npgsql, pgvector), `CatalogContextSeed`, `CatalogAI` embeddings, `CatalogIntegrationEventService` (outbox).
- Events consumed: `OrderStatusChangedToAwaitingValidation` (stock check → `OrderStockConfirmed` | `OrderStockRejected`), `OrderStatusChangedToPaid` (`RemoveStock` per line).
- Events published: `ProductPriceChangedIntegrationEvent`, `OrderStockConfirmed`, `OrderStockRejected`.
- Tests: `tests/Catalog.FunctionalTests` (Aspire Postgres, needs Docker). No `Catalog.UnitTests` project at scoping time.
- Rust: `native/Cargo.toml` workspace; `native/crates/catalog` is an empty landing zone.

## Blast radius
- Inbound: WebApp/WebAppComponents over HTTP (JSON mirrors `AvailableStock`, `OnReorder`), mobile BFF via YARP, Ordering via RabbitMQ events. All cross-cutting contracts; unit 1 changes none of them.
- Outbound: Postgres `catalogdb`, RabbitMQ, OpenAI/Ollama.
- Unit 1 callers: `RemoveStock` has one caller, `IntegrationEvents/EventHandling/OrderStatusChangedToPaidIntegrationEventHandler.cs:17`. `AddStock` has no production caller.
- **Safety fact (unit 1):** `RemoveStock`/`AddStock` are pure int arithmetic on `AvailableStock`, `MaxStockThreshold`, and `OnReorder` with no I/O, EF, or DI.
  - Status: **proven**. 27 characterization tests construct `CatalogItem` with no DbContext, DI, or host and pass on unchanged C# at `0ade9b5`.

## Contract the port must hold
- C# int arithmetic is unchecked (no `CheckForOverflowUnderflow`); Rust uses `wrapping_*` everywhere.
- `RemoveStock`: the `AvailableStock == 0` check runs before the `quantityDesired <= 0` check. Messages are exact: `Empty stock, product item {Name} is sold out` and `Item units desired should be greater than zero`. Stock is unchanged on throw. Negative stock is not "empty": available -5, qty 3 returns -5 and leaves 0.
- `AddStock`: no guard on negative quantity. If `available + qty` (wrapping) exceeds max, stock becomes max. Returns `new - original` (wrapping). Always sets `OnReorder = false`.

## Recommended sequence
1. **U1 Stock rules** (`RemoveStock`/`AddStock`). Characterize → Rust `catalog::stock` → `LibraryImport` wire → parity. Check: `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh` (path A).
2. **U2 Stock availability** in the AwaitingValidation handler (`AvailableStock >= Units`, confirm/reject aggregation). Extract a pure function → Rust → wire; publishing stays .NET.
3. **U3 Pagination and filter shaping** (`PaginationRequest` defaults, brand/type predicate building). Extract → Rust → wire; parity against functional tests.
4. **U4 Price-change detection** on UpdateItem. Rust decision; outbox stays .NET.
5. **U5 Create/update field mapping** and the v1 id check.
6. **U6 Seed parsing** (`Setup/catalog.json`).
7. **U7 Adapters.** Per-adapter decision (EF/pgvector, RabbitMQ consumer, outbox, embeddings); a Rust service behind the same routes, dual-run via YARP, contract tests on HTTP and event JSON.
8. **U8 HTTP cutover.** Rust serves `api/catalog` v1/v2; retire the Catalog.API host.

## Risks
| Risk | Mitigation | Detection |
|---|---|---|
| Overflow drift (Rust debug panics, C# wraps) | `wrapping_*`; `int.MaxValue`/`int.MinValue` cases in both suites | Parity tests |
| Native library missing at runtime | Build and copy the cdylib from `Catalog.API.csproj`; fail fast on load | Negative proof: remove lib → `DllNotFoundException` |
| Container images get a host-OS library | Out of scope for U1; follow-up for a RID-aware cargo target | Publish smoke (follow-up) |
| cargo becomes a Catalog.API build dependency | GitHub `ubuntu-latest` ships cargo; the draft PR CI run proves it | `pr-validation` on the draft PR |
| Harness path A skips functional tests | Build `Catalog.FunctionalTests`; run them when Docker is up | Harness `path=` line |

## Unit 1
- Boundary: `cdylib` + `[LibraryImport]` over a C ABI of blittable ints and a status code. Messages, exceptions, and `OnReorder` stay in C#.
- Harness: `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh` must exit 0 via `A:Catalog.UnitTests`.
- Acceptance:
  - [x] `tests/Catalog.UnitTests` characterizes both methods; green on unchanged C# (`0ade9b5`, 27/27)
  - [x] `catalog::stock` with Rust tests; `cargo test -p catalog` green (15/15)
  - [x] `CatalogItem` delegates to Rust; same characterization suite green with no assertion changes (30/30)
  - [x] Negative proof: removing the cdylib from test output fails the suite with `DllNotFoundException`
  - [x] `libcatalog` present in `artifacts/bin/Catalog.API/debug/`
  - [x] `dotnet build eShop.Web.slnf` exits 0 with `Catalog.UnitTests` in the filter
- Next autonomous unit: U2.
