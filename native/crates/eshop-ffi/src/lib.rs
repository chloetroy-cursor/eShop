//! Thin FFI helpers for calling Rust islands from .NET (`LibraryImport` / P/Invoke).
//!
//! Not tied to one service. Service crates (`catalog`, `basket`, `ordering`) own
//! domain logic; this crate will hold ABI-stable helpers (string marshalling,
//! status codes) when a cutover needs them.
//!
//! **Intentionally stubbed:** no `extern "C"` exports yet — demos add them per
//! unit alongside `cdylib` on the service crate that owns the island.

use eshop_core;

/// Placeholder so the workspace compiles before real FFI helpers land.
pub fn skeleton_ok() -> bool {
    eshop_core::skeleton_ok()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn skeleton_builds() {
        assert!(skeleton_ok());
    }
}
