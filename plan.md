# .NET → Rust migration scope: Catalog.API

ME-1 implements unit 1 only. Later units stay planned until postflight accepts the stock unit.

```text
Service: Catalog.API
Why this first unit: RemoveStock / AddStock are in-memory field math plus two CatalogDomainException messages. The only production caller is OrderStatusChangedToPaidIntegrationEventHandler, and it can keep calling RemoveStock unchanged.
Boundary and blast radius: local to CatalogItem method bodies, a LibraryImport wrapper, native/crates/catalog module stock, and tests/Catalog.UnitTests. Persisted AvailableStock is cross-cutting because the paid handler saves it. HTTP, events, EF, and AI stay unchanged.
Harness command: ./scripts/check-catalog.sh then MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh. Green is path A:Catalog.UnitTests exit 0 after the Rust workspace test and release build.
Safety fact: unproven. The methods do not open a database, broker, file, or HTTP call. No characterization suite exists yet. The implementer proves it by running path A on the current C# bodies before replacing them.
Acceptance evidence: baseline path A exit 0 before the wire; cargo test -p catalog with stock tests running; same Catalog.UnitTests green through LibraryImport; removing the cdylib from the test output fails with DllNotFoundException.
Main risk: a false green. MIGRATION_REQUIRE_RUST=1 only requires native/Cargo.toml, which already tests clean with 0 tests. A suite that still passes after the cdylib is removed is a .NET-only path.
Next autonomous unit: availability decision in the awaiting-validation handler (has_stock plus confirmed-vs-rejected), after this unit is accepted.
```

## Definition of done

- [x] Whole-service inventory (public surface, domain, adapters, events, deps) complete
- [x] Blast radius for migrating the service documented (local vs cross-cutting)
- [x] Recommended sequence covers the service with sequenced verifiable units
- [x] Each scheduled domain island includes required Rust implementation + .NET→Rust wire + parity, or an explicit .NET adapter with a named check
- How to check pass/fail: this file names every Catalog.API surface below. Unit 1 is pass only when `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh` prints `check-catalog: path=A:Catalog.UnitTests exit_code=0` and the negative cdylib proof fails with `DllNotFoundException`. Units 2–6 are not done in ME-1.

## Inventory

- Assemblies / csproj / TFM: one SDK-style web project, `src/Catalog.API/Catalog.API.csproj` (`Microsoft.NET.Sdk.Web`, `net10.0`, nullable). No separate Domain or Infrastructure assembly. `Directory.Build.props` sets `TreatWarningsAsErrors`. No `CheckForOverflowUnderflow`, so `int` math is unchecked.
- NuGet / build: versions in `Directory.Packages.props`. Project references `EventBusRabbitMQ`, `IntegrationEventLogEF`, `eShop.ServiceDefaults`. Packages include `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`, `Pgvector`, `Pgvector.EntityFrameworkCore`, `Aspire.Azure.AI.OpenAI`, `CommunityToolkit.Aspire.OllamaSharp`, `Asp.Versioning.Http`. `InternalsVisibleTo` is `Catalog.FunctionalTests` only. `tests/Catalog.FunctionalTests` exists. `tests/Catalog.UnitTests/Catalog.UnitTests.csproj` does not. `eShop.slnx` does not list a Catalog unit-test project.
- Hosting (Aspire/Kestrel/workers): Kestrel in `src/Catalog.API/Program.cs` (`MapCatalogApi`, health via service defaults). Aspire resource `catalog-api` in `src/eShop.AppHost/Program.cs` references RabbitMQ `eventbus` and Postgres `catalogdb` (`ankane/pgvector`). No gRPC server and no separate worker. RabbitMQ consumers run in-process.
- Public surface (routes, events, CLI): HTTP group `api/catalog` in `src/Catalog.API/Apis/CatalogApi.cs`. v1 and v2: `GET /items/by`, `GET /items/{id}`, `GET /items/{id}/pic`, `GET /catalogtypes`, `GET /catalogbrands`, `POST /items`, `DELETE /items/{id}`. v1 only: `GET /items`, `GET /items/by/{name}`, `GET /items/withsemanticrelevance/{text}`, type/brand list routes, `PUT /items`. v2 only: filtered `GET /items`, `GET /items/withsemanticrelevance`, `PUT /items/{id}`. OpenAPI: `src/Catalog.API/Catalog.API.json`, `src/Catalog.API/Catalog.API_v2.json`.
- Domain vs adapters: stock mutations on `CatalogItem.RemoveStock` / `AddStock` (`src/Catalog.API/Model/CatalogItem.cs`). `RestockThreshold` is stored and never read. Availability comparison `AvailableStock >= Units` is in the awaiting-validation handler. Query filters, semantic-search branch, picture MIME switch, and create/update field copies live in `CatalogApi.cs`. Adapters: `CatalogContext` (table `Catalog`, pgvector `vector(384)`), EF migrations, `CatalogContextSeed` (`AvailableStock = 100`, `MaxStockThreshold = 200`, `RestockThreshold = 10`), `CatalogIntegrationEventService`, `CatalogAI`, files under `Pics/`.
- Events produced/consumed: consumed `OrderStatusChangedToAwaitingValidationIntegrationEvent` and `OrderStatusChangedToPaidIntegrationEvent` (subscriptions in `src/Catalog.API/Extensions/Extensions.cs`, client name `Catalog`). Produced `OrderStockConfirmedIntegrationEvent`, `OrderStockRejectedIntegrationEvent`, `ProductPriceChangedIntegrationEvent`. Record types are duplicated per service; there is no shared contract assembly.
- Dependencies: Ordering publishes the two consumed events and consumes stock confirmed/rejected. WebApp, HybridApp, and ClientApp call the HTTP catalog. Webhooks consumes `ProductPriceChangedIntegrationEvent`.
- Existing Rust crates (`native/`): workspace `native/Cargo.toml` members `eshop-core`, `catalog`, `basket`, `ordering`. `native/crates/catalog/src/lib.rs` is an empty landing zone. No `crate-type`. No `LibraryImport` in the repo. Baseline `cargo test --manifest-path native/Cargo.toml --workspace` exit 0 with 0 tests (cargo 1.98.1). `dotnet` 10.0.400.

Current stock behavior to preserve (`src/Catalog.API/Model/CatalogItem.cs`):

- `RemoveStock`: `AvailableStock == 0` throws `CatalogDomainException` (`Empty stock, product item {Name} is sold out`) before the non-positive check. Stock stays 0. `OnReorder` stays as it was.
- `RemoveStock`: stock not zero and `quantityDesired <= 0` throws `CatalogDomainException` (`Item units desired should be greater than zero`). Stock unchanged.
- `RemoveStock`: otherwise removes `min(quantityDesired, AvailableStock)`, returns that count, and does not change `OnReorder`. It does not read `RestockThreshold` or `MaxStockThreshold`.
- `AddStock`: if `AvailableStock + quantity` exceeds `MaxStockThreshold`, stock becomes the threshold and the return value is the amount actually added (possibly negative when stock was already above the threshold). Otherwise stock increases by `quantity`, including a negative quantity. Always sets `OnReorder = false`. Does not throw.
- The only production caller of `RemoveStock` is `src/Catalog.API/IntegrationEvents/EventHandling/OrderStatusChangedToPaidIntegrationEventHandler.cs`. It ignores the return value and calls `SaveChangesAsync` after the loop. `AddStock` has no production caller. `CreateItem` / `UpdateItem` assign stock fields directly and must keep doing so.

## Dependencies / blast radius

- Inbound: WebApp, HybridApp, ClientApp, and the YARP mobile BFF call catalog HTTP. None call `RemoveStock` or `AddStock`. Ordering publishes `OrderStatusChangedToPaidIntegrationEvent`.
- Outbound: Postgres `catalogdb` via EF. RabbitMQ via the integration-event log. Ordering consumes stock confirmed/rejected. Webhooks consumes price-changed.
- Cross-cutting: event record copies are per service. Shared projects are `EventBusRabbitMQ`, `IntegrationEventLogEF`, and `eShop.ServiceDefaults`. A drifted `RemoveStock` changes `AvailableStock` rows that HTTP queries and the awaiting-validation handler read.
- Service-level migration notes: unit 1's diff stays in Catalog.API, `native/crates/catalog`, `tests/Catalog.UnitTests`, and `eShop.slnx`. The paid handler source stays unchanged. HTTP routes, event payloads, EF, and AI stay unchanged.
- **Safety fact (first unit):** `RemoveStock` and `AddStock` only read and write in-memory `CatalogItem` fields and throw `CatalogDomainException`. They do not open a database, broker, file, or HTTP call.
  - Status: unproven
  - Evidence: assumption from reading `CatalogItem.cs`. No test calls these methods. Prove it with `./scripts/check-catalog.sh` path `A:Catalog.UnitTests` exit 0 on the current C# bodies, before those bodies delegate to Rust.

## Recommended sequence (covers the service)

1. Unit: stock mutations (`RemoveStock` / `AddStock`) — characterize on current C# → Rust `catalog::stock` → `LibraryImport` wire → parity. Check: `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh`. Green means workspace `cargo test` and release build exit 0, then `check-catalog: path=A:Catalog.UnitTests exit_code=0`, and removing the copied cdylib fails that suite with `DllNotFoundException`. This is the only unit ME-1 implements.
2. Unit: availability decision — port `AvailableStock >= Units` and the confirmed-vs-rejected choice from `OrderStatusChangedToAwaitingValidationIntegrationEventHandler` into `catalog::stock`. EF `Find`, the outbox, and RabbitMQ stay .NET. Check: `./scripts/check-catalog.sh` with new unit cases for the predicate and the event choice.
3. Unit: semantic-search mode — port the AI-disabled / null-embedding fallback vs cosine-distance branch from `GetItemsBySemanticRelevance`. Embedding generation and the pgvector query stay .NET. Check: unit cases for the branch, plus `dotnet test tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj`. Path A exits the harness before functional tests, so this command is separate.
4. Unit: picture MIME map — port `GetImageMimeTypeFromImageFileExtension` to a `catalog` module. File reads stay .NET. Check: unit cases for every extension, including unknown → `application/octet-stream`, plus the functional picture test.
5. Unit: HTTP reads, create, update, delete, and price publication stay .NET adapters. `UpdateItem` publishes `ProductPriceChangedIntegrationEvent` only when EF reports `Price` modified. `CreateItem` assigns stock fields directly and does not call `AddStock`. Check: `dotnet test tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj`.
6. Unit: persistence, seed, and the integration-event adapter stay .NET (`CatalogContext`, migrations, pgvector, `CatalogContextSeed`, `CatalogIntegrationEventService`, both RabbitMQ subscriptions). Check: the functional suite plus a host smoke that both subscriptions still register. No RabbitMQ rewrite in `native/crates/catalog`.

## Risks

| Risk | Impact | Mitigation | Detection |
|------|--------|------------|-----------|
| Check order, partial remove, cap, `OnReorder`, or unchecked wrap gets "corrected" | Paid-path stock diverges from current behavior | Lock the cases below on `CatalogItem`, including exception type and both message strings | `./scripts/check-catalog.sh` path A |
| Rust `+` / `-` panics in debug while C# wraps | Parity fails, or only release builds wrap | `wrapping_add` / `wrapping_sub` in `stock` | `cargo test` overflow case and the same case through `LibraryImport` |
| A new exception type or a panic crosses FFI | Paid handler skips `SaveChangesAsync` or takes down the process | Rust returns a discriminant. The wrapper throws `CatalogDomainException` | Unit tests expect that exception type and message |
| cdylib missing, or tests still pass without it | Rust is not on the live path | MSBuild copies the release cdylib into the test output. Static constructor fails fast | Negative proof: `dotnet test --no-build` shows `DllNotFoundException` |
| Path A hides functional regressions | `./scripts/check-catalog.sh` stops at unit tests | Later units name `dotnet test tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj` | Script output shows path A only |
| Create/update starts calling `AddStock` | Create would cap stock and clear `OnReorder` | Unit 1 leaves `CreateItem` and `UpdateItem` on field assign / `SetValues` | Diff does not touch those methods |

## First unit (first vertical to implement)

Status: implemented. Postflight verdict is keep / merge. Evidence is in `validate.md`. The pre-wire path A run is claimed by the implementer and was not re-run, because the old method bodies and the new tests are one change.

- Scope: characterize `RemoveStock` / `AddStock` → no separate .NET extract (the methods are already the pure surface) → Rust port → wire → parity. Not extract-only. Not the whole service.
- Why first: synchronous field math, empty crate landing zone, harness already prefers `tests/Catalog.UnitTests` when that csproj exists.
- **Harness:** `./scripts/check-catalog.sh` on the current C# bodies after the unit project exists, then `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh` after the wire. Run the first command before replacing the method bodies. Fails closed once path A exists.
- Rust crate path (proposed): `native/crates/catalog` module `stock`. `[lib] crate-type = ["cdylib", "rlib"]`.
- Boundary: `LibraryImport("catalog")` on a small `Native*` type. An MSBuild target in `src/Catalog.API/Catalog.API.csproj` runs `cargo build --release -p catalog --target-dir native/target` and copies `libcatalog.dylib` / `libcatalog.so` / `catalog.dll` into the output so referencing test projects load it. A static constructor fails fast when the library is missing. Rust returns a discriminant plus the new `AvailableStock` and the applied delta. The wrapper throws `CatalogDomainException` with the current messages (item name stays in C#) and assigns `AvailableStock`. `AddStock` sets `OnReorder = false`. `RemoveStock` leaves `OnReorder` unchanged. Delete the C# arithmetic. Do not edit the paid handler.
- Acceptance: Rust on the path; the cases below green against the wired public methods; harness path A exit 0; negative proof fails with `DllNotFoundException`.
- Characterization cases the baseline must lock:
  - Empty stock, including when `quantityDesired <= 0`: message `Empty stock, product item {Name} is sold out`. Stock stays 0. `OnReorder` unchanged.
  - Stock greater than 0 and `quantityDesired <= 0`: message `Item units desired should be greater than zero`. Stock unchanged. `OnReorder` unchanged.
  - Partial remove (`quantityDesired > AvailableStock > 0`): returns the previous stock, stock becomes 0, `OnReorder` unchanged.
  - Exact and within-stock removes: return the requested quantity, stock decreases by that amount, `OnReorder` unchanged.
  - `AddStock` under or equal to `MaxStockThreshold`: returns `quantity`, stock increases by `quantity`, `OnReorder` false.
  - `AddStock` over the threshold: stock becomes `MaxStockThreshold`, return value is `MaxStockThreshold - original`, `OnReorder` false. Already at max with a positive quantity returns 0.
  - `AddStock` does not reject `quantity <= 0`. A negative quantity under the max decreases stock and still clears `OnReorder`. Quantity 0 under the max returns 0 and clears `OnReorder`.
  - One unchecked overflow case: `AvailableStock == int.MaxValue` and `quantity == 1` with `MaxStockThreshold == 100` wraps the sum, takes the else branch, and yields stock `int.MinValue` and return value 1, with `OnReorder` false.
- Remaining service coverage: units 2–6 above. Do not implement them in this run.
- Suggested tickets: availability predicate; semantic-search mode; picture MIME map; functional parity for the HTTP and EF adapter surface.
