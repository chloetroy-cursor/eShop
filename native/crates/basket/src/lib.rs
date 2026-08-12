//! Basket service crate — Rust home for **Basket.API** migration units.
//!
//! Corresponds to `src/Basket.API`. Unit modules (e.g. [`cart`]) are where
//! agents land sequenced islands from a whole-service plan.
//!
//! **Intentionally stubbed:** Redis/identity adapters, gRPC surface, and cart
//! mutation rules — structure only so demos have a credible landing place.

#![deny(unsafe_code)]

pub mod buyer;
pub mod cart;

/// Workspace-level smoke hook for `cargo test --workspace`.
pub fn skeleton_ok() -> bool {
    cart::skeleton_ok() && buyer::skeleton_ok()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn skeleton_builds() {
        assert!(skeleton_ok());
    }
}
