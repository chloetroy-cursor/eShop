# .NET → Rust migration scope: Catalog.API

Ticket: ME-1 — Orchestrate Catalog.API .NET→Rust migration (stock unit)
Branch: demo/me-1-catalog-stock-migration
Site: https://chloe-fe-demo.atlassian.net/browse/ME-1

## Definition of done
- [x] Whole-service inventory (public surface, domain, adapters, events, deps) complete
- [x] Blast radius for migrating the service documented (local vs cross-cutting)
- [x] Recommended sequence covers the service with sequenced verifiable units
- [x] Each scheduled domain island includes required Rust implementation + .NET→Rust wire + parity
- How to check pass/fail: every unit in **Recommended sequence** names characterize → Rust → wire → parity and a check command; the first unit's harness is `./scripts/check-catalog.sh`.

## Inventory

### Assemblies / csproj / TFM
- `src/Catalog.API/Catalog.API.csproj` — SDK-style web project, net10.0, `Nullable` enabled
- Linked shared compiles: `src/Shared/ActivityExtensions.cs`, `src/Shared/MigrateDbContextExtensions.cs`
- Project refs: `EventBusRabbitMQ`, `IntegrationEventLogEF`, `eShop.ServiceDefaults`
- Tests: `tests/Catalog.FunctionalTests` (Aspire + Docker, `WebApplicationFactory`). `tests/Catalog.UnitTests` does not exist at scoping time.
- `InternalsVisibleTo`: `Catalog.FunctionalTests`

### NuGet / build
- Central Package Management via `Directory.Packages.props`
- Notable: Asp.Versioning.Http, Aspire.Npgsql.EntityFrameworkCore.PostgreSQL, Pgvector (+ EF), Aspire.Azure.AI.OpenAI, CommunityToolkit.Aspire.OllamaSharp
- Aspire AppHost registers `catalog-api` with `catalogdb` and RabbitMQ

### Hosting
- Kestrel minimal API (`Program.cs` → `MapCatalogApi()`), health at `/health`
- YARP mobile-bff routes under `/catalog-api/...`; WebApp forwards product images to `catalog-api`
- Integration event handlers run in-process via the RabbitMQ bus; no gRPC or separate workers

### Public surface (HTTP, versioned `api/catalog`)
| Area | Routes |
|------|--------|
| List / filter | GET `/items`, `/items/by`, `/items/{id}`, `/items/by/{name}`, `/items/type/{typeId}/brand/{brandId?}`, `/items/type/all/brand/{brandId?}` |
| Search | GET `/items/withsemanticrelevance/{text}` (v1), `/items/withsemanticrelevance` (v2) |
| Types / brands | GET `/catalogtypes`, `/catalogbrands` |
| Pictures | GET `/items/{id}/pic` |
| Mutations | PUT `/items` (v1), PUT `/items/{id}` (v2), POST `/items`, DELETE `/items/{id}` |

No HTTP stock endpoint; stock mutates via integration events and entity methods.

### Domain vs adapters
- **Domain:** `CatalogItem` (`AvailableStock`, `RestockThreshold`, `MaxStockThreshold`, `OnReorder`, `RemoveStock`, `AddStock`), `CatalogBrand`, `CatalogType`, `CatalogDomainException`
- **Adapters:** `CatalogContext` (Npgsql + pgvector + outbox), `CatalogContextSeed`, `CatalogAI` embeddings, `CatalogIntegrationEventService`, RabbitMQ subscriptions, `Pics` static files

### Events
| Direction | Event | Role |
|-----------|-------|------|
| Consumed | `OrderStatusChangedToAwaitingValidationIntegrationEvent` | `AvailableStock >= Units` → confirmed/rejected |
| Consumed | `OrderStatusChangedToPaidIntegrationEvent` | `RemoveStock` per line → `SaveChangesAsync` |
| Produced | `OrderStockConfirmedIntegrationEvent` / `OrderStockRejectedIntegrationEvent` | Stock check outcome |
| Produced | `ProductPriceChangedIntegrationEvent` | On PUT when price changes |

### Existing Rust crates (`native/`)
- Workspace `native/Cargo.toml`: `eshop-core`, `catalog`, `basket`, `ordering`
- Landing zone `native/crates/catalog` is an empty stub. Units land as modules (e.g. `catalog::stock`), not as separate crates.

## Dependencies / blast radius
- **Inbound:** Ordering → RabbitMQ → Catalog handlers; WebApp/YARP → HTTP; AppHost wiring; functional tests
- **Outbound:** Postgres `catalogdb` + outbox; RabbitMQ publishes; optional OpenAI/Ollama embeddings
- **Cross-cutting:** event contract types shared with Ordering/Webhooks; shared EventBus packages; Aspire `catalogdb`
- **Service-level notes:** pure domain islands first (stock, stock-check predicate, price-change detection). EF/HTTP host and AI adapters stay .NET until late units. Do not touch Ordering contracts in the stock unit.
- **Safety fact (first unit):** `CatalogItem.RemoveStock` / `AddStock` are pure in-memory mutations (throw `CatalogDomainException` or update `AvailableStock` / `OnReorder` only) and can be characterized without DB or broker.
  - Status: **proven**. `dotnet test tests/Catalog.UnitTests/Catalog.UnitTests.csproj` on the unchanged `CatalogItem` → exit 0, 17/17 passed (`tests/Catalog.UnitTests/CatalogItemStockTests.cs`; no DB, broker, or Docker). Baseline recorded before any Rust or `CatalogItem` change.
  - Evidence at scoping: source inspection of `src/Catalog.API/Model/CatalogItem.cs`; `cargo test --workspace` green; harness fell to functional tests (path B) because no unit test project existed.

## Recommended sequence (covers the service)

Each domain island: characterize → (extract if needed) → Rust port → wire .NET → Rust → parity → harness green.

1. **Stock mutations (`RemoveStock` / `AddStock`)** — ME-1 first unit
   - Characterize: empty stock throws; qty ≤ 0 throws; partial remove returns available and zeroes stock; add clamps at `MaxStockThreshold`; add clears `OnReorder`
   - Rust: `native/crates/catalog` module `stock` (`cdylib` + `rlib`)
   - Wire: `CatalogItem` delegates to Rust via `LibraryImport`
   - Check: `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh`
   - Green means: Rust on the live remove/add path; `cargo test`, `Catalog.UnitTests`, harness exit 0
2. **Stock availability predicate** (`OrderStatusChangedToAwaitingValidationIntegrationEventHandler`) — `AvailableStock >= Units` decision → Rust; publish stays .NET
3. **Price-change detection on UpdateItem** — when `ProductPriceChangedIntegrationEvent` fires → Rust decision; EF + outbox stay .NET
4. **Create/update field invariants** — pure validation → Rust + wire on create/update path
5. **Query/filter helpers (non-EF)** — pure paging/filter normalization, or document that no pure surface exists
6. **Semantic search orchestration (optional late)** — ranking/fallback rules only if thick enough
7. **Picture path / MIME mapping** — pure extension → MIME → Rust; file I/O stays .NET
8. **Types/brands + remaining HTTP host** — tracked so the surface is not forgotten; likely stays .NET until service cutover
9. **Integration outbox / bus adapter** — last mile; explicit dual-run strategy in that unit's ticket

## Risks
| Risk | Impact | Mitigation | Detection |
|------|--------|------------|-----------|
| No `Catalog.UnitTests` at start | Port without locked semantics | Characterize before Rust/wire | Harness path A missing |
| Rust toolchain missing on host | Harness exit 2 | Install via rustup; `~/.cargo/bin` on PATH | `check-catalog: path=R:rust-missing-toolchain` |
| Native library load (`cdylib`) | .NET fails to load Rust | Build and copy the dylib in the Catalog.API build; test the loaded path in unit tests | Unit test failure / harness |
| Behavioral drift | Wrong inventory / order flow | Mirror .NET cases in Rust tests; parity on wired path | Characterization + `cargo test` |
| Expanding into Ordering contracts | Multi-service PR | Stock unit stays inside Catalog.API + local crate | Diff review |
| `AddStock` has no production callers | Dead path still must match | Characterize and port both per ticket | Unit coverage |

## First unit (first vertical to implement)
- **Scope:** characterize `RemoveStock` / `AddStock` → Rust `catalog::stock` → wire `CatalogItem` to Rust → parity. Not extract-only. Not whole service.
- **Harness:** `./scripts/check-catalog.sh` (use `MIGRATION_REQUIRE_RUST=1` for cutover proof). Requires `cargo` on PATH.
- **Rust crate path:** `native/crates/catalog` (module `stock`)
- **Boundary:** `LibraryImport` to the `cdylib`. Rust CLI only as a fallback for parity proof if FFI is blocked.
- **Acceptance:**
  - [x] `tests/Catalog.UnitTests` locks `RemoveStock` / `AddStock`; green on baseline before the port — `dotnet test tests/Catalog.UnitTests/Catalog.UnitTests.csproj` exit 0, 17/17
  - [x] `catalog::stock` implements the same semantics; `cargo test -p catalog` green — `cargo test --manifest-path native/Cargo.toml -p catalog` exit 0, 8/8 (`native/crates/catalog/src/stock.rs`)
  - [x] `CatalogItem` live path calls Rust for both methods — `CatalogItem.RemoveStock`/`AddStock` delegate to `NativeStock` (`LibraryImport("catalog")`); with `libcatalog.dylib` removed from the test output all 19 tests fail with `DllNotFoundException` (exit 2), restored → 19/19
  - [x] Parity: the same characterization cases pass against the Rust-wired path — `dotnet test tests/Catalog.UnitTests/Catalog.UnitTests.csproj` exit 0, 19/19 (17 characterization + 2 native-load); `dotnet build eShop.Web.slnf` exit 0
  - [x] `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh` exit 0 — path `R:cargo-test-workspace` → `R:cargo-build-release-workspace` → `A:Catalog.UnitTests`, all exit_code=0
  - [ ] Draft PR for ME-1; do not merge
- **Native library:** `Catalog.API.csproj` target `BuildNativeCatalog` runs `cargo build --release -p catalog --target-dir native/target` before `AssignTargetPaths` and adds the cdylib as a copy-to-output item, so it lands in `artifacts/bin/Catalog.API/<config>/` and in every referencing project's output. Set `SkipNativeCatalogBuild=true` to bypass.
- **Next autonomous unit:** stock availability predicate (unit 2)
