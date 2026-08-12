//! Catalog stock rules island (`RemoveStock` / `AddStock`).
//!
//! Absorbed from the former top-level `native/catalog_stock` crate. Port
//! characterized semantics here when the Catalog stock unit runs; keep
//! `cargo test` green. This crate already declares `cdylib` + `rlib` for a
//! later .NET `LibraryImport` cutover.
//!
//! .NET source of truth today: `src/Catalog.API/Model/CatalogItem.cs`.

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
