# native/ — Catalog Rust scaffold

Minimal Cargo workspace for the **Catalog.API** .NET → Rust demo. Landing zone
only — not a behavioral port.

## Tree

```text
native/
  Cargo.toml
  README.md
  crates/
    catalog/                 # ← Catalog.API
      src/stock.rs           # catalog::stock (RemoveStock / AddStock placeholder)
      src/items.rs           # thin stub
      src/queries.rs         # thin stub
```

## Convention

Add migration units as modules under `native/crates/catalog/` (e.g. `catalog::stock`).
Other services (Basket, Ordering, …) get crates under `native/crates/` when those
tickets start — do not pre-create them here.

`cdylib` + `rlib` are declared on `catalog` so a later .NET `LibraryImport` cutover
can land without reshaping the tree. FFI helpers can wait until wiring.

## Checks

```bash
./scripts/check-native.sh
# same as: cargo test --manifest-path native/Cargo.toml --workspace

./scripts/check-catalog.sh          # Catalog Rust crate + Catalog .NET tests
# MIGRATION_REQUIRE_RUST=1 ./scripts/check-catalog.sh
```

| Script | Role |
|--------|------|
| `scripts/check-native.sh` | `cargo test --workspace` in `native/` |
| `scripts/check-catalog.sh` | `native/crates/catalog` + Catalog .NET tests |
