//! Shared building blocks for eShop Rust service crates.
//!
//! Corresponds loosely to cross-cutting .NET types used across Catalog, Basket,
//! and Ordering — not a 1:1 port of any single project.
//!
//! **Intentionally stubbed:** real shared IDs, money/quantity newtypes, and
//! error taxonomy land here as units are migrated. No .NET FFI surface lives
//! in this crate (see `eshop-ffi`).

#![deny(unsafe_code)]

/// Placeholder so the workspace compiles before shared types land.
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
