# Migration validate: Catalog.API / stock mutations (RemoveStock / AddStock)

Ticket: ME-1. Branch: `demo/me-1-catalog-stock-migration`. Unit 1 only.
Evidence level: ran-real-tests (floor for keep/merge). Implementer self-report in `plan.md` was not used as proof.

## Claim
`CatalogItem.RemoveStock` / `AddStock` match pre-migration behavior for the characterized cases, and those methods call the Rust `catalog` cdylib on the live path.

## Blast-radius safety fact
- Fact: `RemoveStock` / `AddStock` are pure in-memory mutations (throw `CatalogDomainException` or update `AvailableStock` / `OnReorder` only) and can be characterized without a database or broker. Rust is on that path, not a dead crate.
- Status: proven
- Evidence: `dotnet test tests/Catalog.UnitTests/Catalog.UnitTests.csproj` exit 0, 19/19 (no Docker). Negative proof: move `tests/Catalog.UnitTests/bin/Debug/net10.0/libcatalog.dylib` aside, `dotnet test ... --no-build` exit 2, 19 failed, inner `DllNotFoundException` from `NativeStock` static ctor (`NativeStock.cs:25`). File restored; `--no-build` exit 0, 19/19. `nm` shows `_catalog_remove_stock` and `_catalog_add_stock` in `native/target/release/libcatalog.dylib` (mtime 18:45, after `stock.rs` 18:43).

## Artifact ladder
- [x] Harness: `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh` — exit 0. Paths `R:cargo-test-workspace` exit 0 (catalog 8 passed), `R:cargo-build-release-workspace` exit 0, `A:Catalog.UnitTests` exit 0 (19 passed). Functional path B was not taken because the unit project exists.
- [x] Rust island + parity: `native/crates/catalog` module `stock`. `cargo test --manifest-path native/Cargo.toml -p catalog` exit 0, 8 passed. Wired from .NET: yes (`LibraryImport("catalog")` in `src/Catalog.API/Model/NativeStock.cs`; `CatalogItem.RemoveStock` / `AddStock` delegate to it).
- [x] `dotnet build eShop.Web.slnf` — exit 0, 0 errors (44 pre-existing MessagePack NU1902/NU1903 warnings).
- [ ] Optional runtime: N/A — Aspire was not already running; not started for this gate.
- [ ] Functional suite: waived (see below). Not run.

## Parity
Wired-path tests in `tests/Catalog.UnitTests/CatalogItemStockTests.cs` (pass against the loaded dylib) and matching Rust tests in `native/crates/catalog/src/stock.rs`:

| Case | C# test | Rust test |
|------|---------|-----------|
| `AvailableStock == 0` throws `CatalogDomainException` "Empty stock, product item {Name} is sold out" | `RemoveStock_EmptyStock_ThrowsSoldOut` | `remove_empty_stock_is_sold_out_regardless_of_quantity` |
| qty <= 0 throws "Item units desired should be greater than zero" | `RemoveStock_NonPositiveQuantity_Throws` | `remove_non_positive_quantity_is_rejected` |
| qty <= stock returns qty and decrements | `RemoveStock_SufficientStock_RemovesDesired` | `remove_sufficient_stock_removes_desired` |
| qty > stock returns available and zeroes stock | `RemoveStock_InsufficientStock_RemovesAvailableAndZeroes` | `remove_insufficient_stock_removes_available_and_zeroes` |
| Add under max increments and sets `OnReorder=false` | `AddStock_UnderMax_AddsQuantityAndClearsOnReorder` | `add_under_max_adds_quantity` (clear stays in C#; asserted on the wired path) |
| Add over max clamps to `MaxStockThreshold` and returns actual added | `AddStock_ExceedsMax_ClampsToMaxThreshold` | `add_exceeding_max_clamps_to_threshold` |

Original `CatalogItem` (`git show HEAD:.../CatalogItem.cs`) uses the same order (empty before non-positive), the same messages, `Math.Min` decrement, and clamp-then-`OnReorder = false`.

## Blast radius
Pass. Implementer files only:

- `eShop.slnx` (adds `tests/Catalog.UnitTests`)
- `native/crates/catalog/Cargo.toml`, `src/lib.rs`, `src/stock.rs`
- `scripts/check-catalog.sh` (prepends `~/.cargo/bin` to PATH)
- `src/Catalog.API/Catalog.API.csproj`
- `src/Catalog.API/Model/CatalogItem.cs`, `NativeStock.cs`
- `tests/Catalog.UnitTests/*`
- `plan.md`

No Ordering, Basket, WebApp, RabbitMQ, `.cursor/`, or Makefile edits. `validate.md` is this gate's artifact, written after that inventory.

## Waivers
- `tests/Catalog.FunctionalTests`: not run. Operator waiver for this postflight — slow Docker suite with 2 pre-existing RabbitMQ failures unrelated to the stock unit. The harness also skips them when `tests/Catalog.UnitTests` exists (path A). Owner: migration operator for ME-1.

## Fix-forward attempts
- Count: 0
- Stopped because: N/A (no corrections)

## Rollback
- Trigger: `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh` or `Catalog.UnitTests` red on mainline CI; `DllNotFoundException` / `TypeInitializationException` from `NativeStock` on a host that should have the cdylib; stock results diverge from the parity table.
- Action: revert the unit PR. There is no feature flag. `SkipNativeCatalogBuild=true` skips the cargo build but does not restore the old C# methods.

## Verdict
- [x] Keep / merge — harness, `cargo test -p catalog`, unit tests, live-path negative proof, web solution filter, and blast radius all passed. Rust is on the `RemoveStock` / `AddStock` path.
- [ ] Do not merge
- [ ] Inconclusive
