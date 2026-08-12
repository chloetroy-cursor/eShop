# native/ — eShop Rust backend workspace

Demo-ready Cargo workspace for migrating eShop backend services **.NET → Rust**.
Frontend (React) is out of scope here.

Glance this tree in ~30 seconds; extend it one unit at a time.

## Tree

```text
native/
  Cargo.toml                 # workspace root
  README.md                  # this file (talk-track)
  crates/
    eshop-core/              # shared types / helpers (no FFI)
    eshop-ffi/               # thin P/Invoke / LibraryImport helpers (stub)
    catalog/                 # ← Catalog.API  (primary demo)
      src/stock.rs           #   catalog::stock  (RemoveStock / AddStock island)
      src/items.rs           #   stub
      src/queries.rs         #   stub
    basket/                  # ← Basket.API
      src/cart.rs            #   stub
      src/buyer.rs           #   stub
    ordering/                # ← Ordering.API / Ordering.Domain
      src/orders.rs          #   stub
      src/aggregate.rs       #   stub
```

| .NET project | Rust crate | First unit module |
|--------------|------------|-------------------|
| `src/Catalog.API` | `crates/catalog` | `catalog::stock` |
| `src/Basket.API` | `crates/basket` | `basket::cart` |
| `src/Ordering.API` (+ Domain) | `crates/ordering` | `ordering::orders` |

## Convention (agents & demos)

**One crate per service, one module per migration unit.**

- Path shape: `native/crates/<service>/src/<unit>.rs`
- Rust path: `<service>::<unit>` (example: `catalog::stock`)
- Do **not** add a new top-level `native/<service>_<unit>/` crate for each island — that was the old ad-hoc layout (`native/catalog_stock/`), now absorbed into `catalog::stock`.

Skills under `.cursor/skills/` (Scope / Migrate / Validate) land work in these crates. Catalog stock remains the default first vertical.

## How demos extend this

1. **Scope** a service → whole-service `plan.md` with sequenced units.
2. **Migrate** the next unit into the matching module (characterize → extract if needed → Rust → wire → parity).
3. Keep `cdylib` + `rlib` on the service crate that .NET will P/Invoke (Catalog already declares both).
4. **Validate** with real commands — no fake metrics.

Shared helpers go in `eshop-core`; ABI glue in `eshop-ffi` when a cutover needs it.

## Checks

From repo root:

```bash
# Full Rust workspace (preferred for this scaffolding)
./scripts/check-native.sh

# Equivalent:
cargo test --manifest-path native/Cargo.toml --workspace
```

Catalog migration lever (unchanged role — .NET tests + Catalog Rust crate):

```bash
./scripts/check-catalog.sh
# Strict: MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh
```

| Script | What it covers |
|--------|----------------|
| `scripts/check-native.sh` | `cargo test --workspace` under `native/` |
| `scripts/check-catalog.sh` | Catalog Rust crate (`native/crates/catalog`) + Catalog .NET tests |

`check-catalog.sh` does **not** replace `check-native.sh`; use native for the whole workspace, catalog when working a Catalog unit end-to-end with .NET.
