//! Catalog stock rules island (`RemoveStock` / `AddStock`).
//!
//! Absorbed from the former top-level `native/catalog_stock` crate. Agents
//! implementing **Migrate to Rust** should port characterized semantics here,
//! keep `cargo test` green with parity cases, and expose a `cdylib` surface
//! (this crate already declares `cdylib` + `rlib`) for .NET `LibraryImport`.
//!
//! .NET source of truth today: `src/Catalog.API/Model/CatalogItem.cs` (or an
//! extracted `CatalogStock` type).

/// Placeholder so `cargo test` / `cargo build` succeed before the real port lands.
pub fn skeleton_ok() -> bool {
    true
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn skeleton_builds() {
        assert!(skeleton_ok());
    }
}
