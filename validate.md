# Migration validate: Catalog.API / U1 stock rules

Branch `me-1/catalog-stock-rust` (HEAD `e9fb581`), validated 2026-09-28 against `plan.md` "Contract the port must hold" and "Unit 1". Every check below was rerun by the validator; nothing is taken from the implementer's report.

## Verdict: keep / merge

The claim holds, the safety fact is proven, Rust is on the live path, parity is proven by the unchanged characterization suite plus an independent differential, and no rollback trigger is open. Merge still waits for an explicit merge request per repo policy (draft PR only).

## Claim
`CatalogItem.RemoveStock`/`AddStock` behave exactly as the pre-migration C# (unchecked int, check order, messages, stock unchanged on throw, `OnReorder`) when routed through Rust `catalog::stock` via `LibraryImport`.

## Blast-radius safety fact
- Fact: `RemoveStock`/`AddStock` are pure int arithmetic on `AvailableStock`, `MaxStockThreshold`, `OnReorder` with no I/O, EF, or DI.
- Status: **proven**. The 27-case characterization suite constructs `CatalogItem` with no DbContext, DI, or host and passes on unchanged C# at `0ade9b5` (row 1). `git diff main 0ade9b5 -- src` is empty (0 lines).

## Evidence level
ran-real-tests (floor met). No runtime/deploy probe (stack not running).

## Evidence

| # | Check | Command | Exit | Result |
|---|---|---|---|---|
| 1 | Baseline pin, unchanged C# | `git worktree add /tmp/me1-base 0ade9b5`; `dotnet test tests/Catalog.UnitTests/Catalog.UnitTests.csproj` | 0 | 27/27 passed; no `libcatalog` in output dir; worktree removed (`git worktree remove --force`, exit 0) |
| 2 | Characterization assertions unchanged | `git diff 0ade9b5 HEAD -- tests/Catalog.UnitTests/CatalogItemStockTests.cs` | 0 | empty (0 lines) |
| 3 | Contract coverage (read `CatalogItemStockTests.cs`) | manual | n/a | all contract items covered; see below |
| 4 | Rust unit tests | `cargo test --manifest-path native/Cargo.toml -p catalog` | 0 | 15 passed, 0 failed |
| 5 | Harness | `MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh` | 0 | `R:cargo-test-workspace exit_code=0`, `R:cargo-build-release-workspace exit_code=0`, `A:Catalog.UnitTests exit_code=0`, 30/30 passed (27 characterization + 3 `NativeStockTests`) |
| 6a | Negative proof | move `tests/Catalog.UnitTests/bin/Debug/net10.0/libcatalog.dylib` aside; `dotnet test ... --no-build` | 2 | 30/30 failed; `TypeInitializationException ---> System.DllNotFoundException: Rust library 'catalog' was not found` |
| 6b | Restore | restore dylib; `dotnet test ... --no-build` | 0 | 30/30 passed |
| 6c | Artifact present | `ls artifacts/bin/Catalog.API/debug/libcatalog.dylib` | 0 | present (16832 B, same as `native/target/release`). Unit-test output is `tests/Catalog.UnitTests/bin/Debug/net10.0/` (MSTest.Sdk project, not under `artifacts/`); only those two copies exist |
| 7 | Independent differential, original C# (`git show main:...CatalogItem.cs`, wrapping ops) vs `remove_stock`/`add_stock` and `catalog_remove_stock`/`catalog_add_stock` | `/tmp/me1-diff`, `cargo run --release` and `cargo run` (debug, overflow checks on) | 0 / 0 | grid {MIN, MIN+1, -100, -1, 0, 1, 100, MAX-1, MAX}: 81 remove + 729 add cases; plus 1,000,000 random triples each. **0 mismatches**, no panics. FFI out-params verified untouched on error statuses |
| 8a | Web solution build | `dotnet build eShop.Web.slnf` | 0 | 0 errors; 44 warnings, all NU1902/NU1903 NuGet audit on untouched packages |
| 8b | Functional tests build | `dotnet build tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj` | 0 | 0 errors |
| 9 | Scope | `git diff main --stat -- src/Catalog.API/Infrastructure/Migrations src/Catalog.API/Apis src/Ordering.API` | 0 | empty. 12 files changed, all within U1 (crate, `CatalogItem`, `NativeStock`, csproj wiring, unit tests, slnf/slnx) |

## Contract coverage (row 3)
- Unchecked arithmetic: `MinValue` remove wraps to 0; `MaxValue + 1` wraps instead of clamping; `MinValue - 1` wraps then clamps; clamp-from-`MaxValue` return wraps; partial remove with `int.MaxValue`.
- Empty check before qty check: available 0 with qty 1/0/-1 all throw the sold-out message.
- Exact messages including `{Name}` interpolation (`Widget`, `Alpine Skis`).
- Stock and `OnReorder` unchanged on both throws.
- Negative stock not empty: -5, qty 3 returns -5, leaves 0.
- `AddStock` negative qty, wrapped-sum clamp (including already-above-max), `new - original` return, `OnReorder = false` asserted in every add case.
- Gaps: none against the contract. Negative/extreme `MaxStockThreshold` is not in the C# suite; the differential (row 7) covers it.

## Parity
- [x] Characterization tests exist and pass on baseline (row 1)
- [x] Same tests pass on the Rust-wired path with no assertion changes (rows 2, 5)
- [x] Rust is live, not dead code (row 6a)
- [x] Contract/API: no HTTP, event, or EF surface touched (row 9)

## Waivers
- `Catalog.FunctionalTests` execution: waived (Docker down). Owner: operator. Reason: they never call the unit; grep for `RemoveStock|AddStock|NativeStock` finds 0 matches under `tests/Catalog.FunctionalTests`. Project builds (row 8b).
- Optional runtime (Aspire): N/A, stack not running.

## Fix-forward attempts
0. Validator changed no source, tests, or commits.

## Rollback
- Trigger: characterization/parity suite red on mainline CI; `DllNotFoundException`/`TypeInitializationException` in Catalog.API; any stock divergence in `OrderStatusChangedToPaid` handling.
- Action: revert `e9fb581` (restores the C# bodies; crate and tests can stay).

## Residual risks
- Missing library fails on first stock call, not at startup. `NativeStock`'s static ctor throws only when `OrderStatusChangedToPaidIntegrationEventHandler` first calls `RemoveStock`, so a host shipped without the dylib starts healthy and then fails paid-order events. Suggest a startup probe in U2 or before deploy.
- cargo is now a hard build dependency of Catalog.API and everything referencing it (AppHost, slnf). The `pr-validation` CI run on the draft PR has not been observed by the validator.
- Container/publish: host-OS library only; RID-aware build is a plan-listed follow-up. Publish not tested.
