//! Catalog service crate — Rust landing zone for **Catalog.API**.
//!
//! Corresponds to `src/Catalog.API`. Unit modules map to migration islands,
//! e.g. [`stock`] for `RemoveStock` / `AddStock`.
//!
//! Former top-level `native/catalog_stock/` is absorbed here as [`stock`].
//!
//! **Intentionally stubbed:** item/query surfaces, EF/adapters, and HTTP — only
//! thin module placeholders so `cargo test` and `scripts/check-catalog.sh` have
//! a concrete path. Other backend services get their own crates when those
//! tickets start — not pre-created here.

#![deny(unsafe_code)]

pub mod items;
pub mod queries;
pub mod stock;

/// Workspace smoke hook for `cargo test --workspace`.
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
