# Migration validate: Catalog.API / ME-1 stock

## Claim

`CatalogItem.RemoveStock` and `CatalogItem.AddStock` keep the pre-migration stock rules when both methods call `native/crates/catalog` module `stock` through `LibraryImport`.

## Blast-radius safety fact

- Fact: `RemoveStock` and `AddStock` only read and write in-memory `CatalogItem` fields and throw `CatalogDomainException`. They do not open a database, broker, file, or HTTP call.
- Status: proven
- Evidence: `tests/Catalog.UnitTests/CatalogItemStockTests.cs` constructs `new CatalogItem("Road Bike")` and sets `AvailableStock`, `MaxStockThreshold`, and `OnReorder`. No test mentions `CatalogContext`. The same 16 tests passed after the cdylib was restored: `dotnet test tests/Catalog.UnitTests/Catalog.UnitTests.csproj --no-build` exit 0.

## Evidence level

ran-real-tests

Floor for keep/merge is ran-real-tests. The pre-wire baseline run is self-report only. The wired path, the Rust tests, and the missing-library run were executed in this validation.

## Baseline

Only claimed.

`git diff HEAD -- src/Catalog.API/Model/CatalogItem.cs` shows the old C# arithmetic and the Rust calls in one uncommitted change. `tests/Catalog.UnitTests/` is untracked. There is no commit and no saved harness log of path A against the old method bodies. This validation cannot re-run that past baseline.

The new tests do call the public methods. Every case uses `item.RemoveStock` or `item.AddStock`. That shows the characterization surface. It does not prove those tests passed before the bodies changed.

The deleted C# matches the locked rules in `plan.md`: empty stock is checked before a non-positive quantity, remove uses `Math.Min`, add compares `AvailableStock + quantity` with `MaxStockThreshold`, and `AddStock` sets `OnReorder = false`. That comparison is pointed at the diff, not a recorded run.

## Artifact ladder

- [x] Characterization/unit: `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh` — exit 0. It printed `check-catalog: path=R:cargo-test-workspace exit_code=0`, `check-catalog: path=R:cargo-build-release-workspace exit_code=0`, and `check-catalog: path=A:Catalog.UnitTests exit_code=0`. Path A ran 16 tests, 16 succeeded, 0 failed.
- [x] Rust island + parity: crate `native/crates/catalog`, module `stock`. `cargo test --manifest-path native/Cargo.toml -p catalog` exit 0, 17 passed, 0 failed. Wired from .NET: yes. `CatalogItem.RemoveStock` and `CatalogItem.AddStock` call `NativeCatalog`, which uses `LibraryImport("catalog")` for `catalog_remove_stock` and `catalog_add_stock`. `BuildCatalogNative` in `src/Catalog.API/Catalog.API.csproj` release-builds the crate and copies `libcatalog.dylib` into the output.
- [x] Service client-style evidence: waived. See Waiver.
- [ ] Optional runtime: N/A. The stack was not already running, and this unit does not need it.

Direct Rust command, with `PATH` prefixed by `$HOME/.cargo/bin`:

```text
cargo test --manifest-path native/Cargo.toml -p catalog
```

Exit 0. 17 passed, 0 failed, 0 ignored. The overflow test `add_unchecked_overflow_wraps_to_min_value` passed.

## Negative proof

Copied library found at `tests/Catalog.UnitTests/bin/Debug/net10.0/libcatalog.dylib`. Release copies also exist at `native/target/release/libcatalog.dylib` and `native/target/release/deps/libcatalog.dylib`. No `libcatalog.dylib` was under `src/Catalog.API/bin`.

Moved the unit-test copy to `libcatalog.dylib.validator-aside` in the same directory. Confirmed the original path was absent. Then:

```text
dotnet test tests/Catalog.UnitTests/Catalog.UnitTests.csproj --no-build
```

Exit 2. 16 failed, 0 succeeded. `--no-build` did not rebuild and did not copy the library back. The file was still absent after the run.

Failure shape: `System.TypeInitializationException` for `eShop.Catalog.API.Model.NativeCatalog`, inner exception `System.DllNotFoundException: Unable to load shared library 'catalog'`. Stack points at `NativeCatalog.cs` line 12, `NativeLibrary.Load`. `dlopen` tried `tests/Catalog.UnitTests/bin/Debug/net10.0/libcatalog.dylib` and reported no such file. Tests that expect `CatalogDomainException` failed `Assert.ThrowsExactly` because the actual exception was that `TypeInitializationException`.

Restored the aside file onto `libcatalog.dylib`. No aside file remained. Re-ran:

```text
dotnet test tests/Catalog.UnitTests/Catalog.UnitTests.csproj --no-build
```

Exit 0. 16 passed, 0 failed.

## Code check

- C# stock arithmetic is gone from both methods. `RemoveStock` and `AddStock` call `NativeCatalog.RemoveStock` and `NativeCatalog.AddStock`.
- Exception text stays in C#. Empty stock throws `CatalogDomainException` with `Empty stock, product item {Name} is sold out`. Non-positive quantity throws `CatalogDomainException` with `Item units desired should be greater than zero`. `AddStock` sets `OnReorder = false`. `RemoveStock` does not assign `OnReorder`. The 16 passing tests lock those outcomes, including the name in the sold-out message.
- `native/crates/catalog/src/stock.rs` uses `wrapping_add` and `wrapping_sub`. `AvailableStock == int.MaxValue` and `quantity == 1` with `MaxStockThreshold == 100` takes the else branch after the wrapped sum and yields stock `int.MinValue` with return value 1. The Rust test and `AddStock_unchecked_overflow_wraps_to_min_value` both passed.
- `git diff` for `OrderStatusChangedToPaidIntegrationEventHandler.cs` and `CatalogApi.cs` is empty. Neither file is modified or untracked.
- Units 2–6 are not in this tree. `native/crates/catalog/src/lib.rs` only adds module `stock` and the two C exports. The working tree changes are `eShop.slnx`, `native/crates/catalog/*`, `src/Catalog.API/Catalog.API.csproj`, `src/Catalog.API/Model/CatalogItem.cs`, `src/Catalog.API/Model/NativeCatalog.cs`, `tests/Catalog.UnitTests/`, and `plan.md`.

## Parity

- [ ] Characterization tests exist and pass on baseline. Tests exist and call the public methods. The pre-wire path A run is only claimed.
- [x] Same tests pass on the Rust-wired path. Path A exit 0, 16 passed. Restored `--no-build` run exit 0, 16 passed.
- [x] Contract check for this unit: paid handler and `CatalogApi.cs` are unchanged. No HTTP or event payload change is in the diff.

## Waiver

Path B, `tests/Catalog.FunctionalTests`, is waived for this verdict.

- Owner: the preflight gate.
- Reason: stock is an in-process domain method, and path A is the committed lever.

## Fix-forward attempts

- Count: 0
- Stopped because: N/A
- Notes for human: validation did not edit code.

## Structure encoding

None. No repeated failure class showed up in this run.

## Rollback

- Trigger: path A prints an exit code other than 0, a normal run throws `DllNotFoundException` or panics in `catalog::stock`, or a stock result disagrees with the cases locked in `plan.md`.
- Action: revert the ME-1 files listed under Code check to the current `HEAD` versions and drop the untracked stock, wrapper, and unit-test files. Do not deploy this unit.

## Verdict

- [x] Keep / merge — path A exit 0 with 16 .NET tests, `cargo test -p catalog` exit 0 with 17 tests, and the unit-test cdylib removal failed all 16 tests on `DllNotFoundException`. Restoring the library made the 16 tests pass again. The safety fact is proven. Rust is on the live path. The pre-wire baseline run remains only claimed.
- [ ] Do not merge
- [ ] Inconclusive

Inconclusive is not keep/merge. This verdict is keep / merge.
