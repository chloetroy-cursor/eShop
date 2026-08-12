//! Ordering service crate — Rust home for **Ordering** migration units.
//!
//! Corresponds primarily to `src/Ordering.API` / `src/Ordering.Domain` (and
//! related Ordering.* projects as units are scoped). Prefer modules per
//! domain island (e.g. [`orders`], [`aggregate`]).
//!
//! **Intentionally stubbed:** order aggregate invariants, integration events,
//! and API handlers — no behavioral port yet.

#![deny(unsafe_code)]

pub mod aggregate;
pub mod orders;

/// Workspace-level smoke hook for `cargo test --workspace`.
pub fn skeleton_ok() -> bool {
    orders::skeleton_ok() && aggregate::skeleton_ok()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn skeleton_builds() {
        assert!(skeleton_ok());
    }
}
