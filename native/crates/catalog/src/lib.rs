//! Catalog.API Rust landing zone (`src/Catalog.API`).
//!
//! Units land as modules. [`stock`] is `CatalogItem.RemoveStock` / `AddStock`
//! plus the C ABI consumed by `LibraryImport("catalog")`.

pub mod stock;
