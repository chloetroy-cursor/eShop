//! Catalog service crate — Rust home for **Catalog.API** migration units.
//!
//! Corresponds to `src/Catalog.API` (primary demo service). Unit modules map
//! 1:1 to migration islands agents land under this crate, e.g. [`stock`] for
//! `RemoveStock` / `AddStock`.
//!
//! **Convention:** `native/crates/<service>/` + module `<unit>` (was historically
//! a top-level `native/catalog_stock/` crate; absorbed here as [`stock`]).
//!
//! **Intentionally stubbed:** item/query surfaces, EF/adapters, and HTTP — only
//! the stock island skeleton is present so `cargo test` and
//! `scripts/check-catalog.sh` have a concrete path.

#![deny(unsafe_code)]

pub mod items;
pub mod queries;
pub mod stock;

/// Workspace-level smoke hook for `cargo test --workspace`.
pub fn skeleton_ok() -> bool {
    stock::skeleton_ok() && items::skeleton_ok() && queries::skeleton_ok()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn skeleton_builds() {
        assert!(skeleton_ok());
    }
}
