//! Catalog.API Rust landing zone (`src/Catalog.API`).
//!
//! Stock mutations live in [`stock`]. Later units add their own modules.

mod stock;

pub use stock::{add_stock, remove_stock, StockDecision};

#[no_mangle]
pub extern "C" fn catalog_remove_stock(
    available_stock: i32,
    quantity_desired: i32,
) -> StockDecision {
    remove_stock(available_stock, quantity_desired)
}

#[no_mangle]
pub extern "C" fn catalog_add_stock(
    available_stock: i32,
    quantity: i32,
    max_stock_threshold: i32,
) -> StockDecision {
    add_stock(available_stock, quantity, max_stock_threshold)
}
