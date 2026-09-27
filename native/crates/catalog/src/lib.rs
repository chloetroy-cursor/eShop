//! Catalog.API Rust landing zone (`src/Catalog.API`).
//!
//! Migration units land as modules in this crate. Currently:
//! - [`stock`]: `CatalogItem.RemoveStock` / `AddStock` rules plus their C ABI
//!   exports, called from .NET via `LibraryImport("catalog")`.

pub mod stock;
