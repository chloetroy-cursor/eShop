//! `CatalogItem` stock mutations.
//!
//! `i32` arithmetic wraps, matching unchecked C# `int` math.
//! Empty stock is reported before a non-positive quantity.

/// Rust returned the new stock and the applied delta.
pub const OK: i32 = 0;
/// `AvailableStock == 0`, including when the requested quantity is not positive.
pub const EMPTY_STOCK: i32 = 1;
/// Stock is present and the requested quantity is not positive.
pub const NON_POSITIVE_QUANTITY: i32 = 2;

/// Discriminant plus the stock write the .NET wrapper applies.
#[repr(C)]
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct StockDecision {
    pub code: i32,
    pub available_stock: i32,
    pub applied_delta: i32,
}

pub fn remove_stock(available_stock: i32, quantity_desired: i32) -> StockDecision {
    if available_stock == 0 {
        return StockDecision {
            code: EMPTY_STOCK,
            available_stock,
            applied_delta: 0,
        };
    }

    if quantity_desired <= 0 {
        return StockDecision {
            code: NON_POSITIVE_QUANTITY,
            available_stock,
            applied_delta: 0,
        };
    }

    let removed = quantity_desired.min(available_stock);
    StockDecision {
        code: OK,
        available_stock: available_stock.wrapping_sub(removed),
        applied_delta: removed,
    }
}

pub fn add_stock(available_stock: i32, quantity: i32, max_stock_threshold: i32) -> StockDecision {
    let original = available_stock;
    let sum = available_stock.wrapping_add(quantity);
    let available_stock = if sum > max_stock_threshold {
        original.wrapping_add(max_stock_threshold.wrapping_sub(original))
    } else {
        original.wrapping_add(quantity)
    };

    StockDecision {
        code: OK,
        available_stock,
        applied_delta: available_stock.wrapping_sub(original),
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn remove_empty_stock_positive_quantity() {
        let decision = remove_stock(0, 2);
        assert_eq!(decision.code, EMPTY_STOCK);
        assert_eq!(decision.available_stock, 0);
        assert_eq!(decision.applied_delta, 0);
    }

    #[test]
    fn remove_empty_stock_and_zero_quantity_is_empty_stock() {
        let decision = remove_stock(0, 0);
        assert_eq!(decision.code, EMPTY_STOCK);
        assert_eq!(decision.available_stock, 0);
        assert_eq!(decision.applied_delta, 0);
    }

    #[test]
    fn remove_empty_stock_and_negative_quantity_is_empty_stock() {
        let decision = remove_stock(0, -3);
        assert_eq!(decision.code, EMPTY_STOCK);
        assert_eq!(decision.available_stock, 0);
        assert_eq!(decision.applied_delta, 0);
    }

    #[test]
    fn remove_zero_quantity_is_non_positive() {
        let decision = remove_stock(8, 0);
        assert_eq!(decision.code, NON_POSITIVE_QUANTITY);
        assert_eq!(decision.available_stock, 8);
        assert_eq!(decision.applied_delta, 0);
    }

    #[test]
    fn remove_negative_quantity_is_non_positive() {
        let decision = remove_stock(8, -4);
        assert_eq!(decision.code, NON_POSITIVE_QUANTITY);
        assert_eq!(decision.available_stock, 8);
        assert_eq!(decision.applied_delta, 0);
    }

    #[test]
    fn remove_partial_returns_previous_stock() {
        let decision = remove_stock(3, 10);
        assert_eq!(decision.code, OK);
        assert_eq!(decision.applied_delta, 3);
        assert_eq!(decision.available_stock, 0);
    }

    #[test]
    fn remove_exact_returns_requested_quantity() {
        let decision = remove_stock(6, 6);
        assert_eq!(decision.code, OK);
        assert_eq!(decision.applied_delta, 6);
        assert_eq!(decision.available_stock, 0);
    }

    #[test]
    fn remove_within_stock_returns_requested_quantity() {
        let decision = remove_stock(9, 4);
        assert_eq!(decision.code, OK);
        assert_eq!(decision.applied_delta, 4);
        assert_eq!(decision.available_stock, 5);
    }

    #[test]
    fn add_under_threshold_returns_quantity() {
        let decision = add_stock(10, 5, 100);
        assert_eq!(decision.code, OK);
        assert_eq!(decision.applied_delta, 5);
        assert_eq!(decision.available_stock, 15);
    }

    #[test]
    fn add_equal_to_threshold_returns_quantity() {
        let decision = add_stock(90, 10, 100);
        assert_eq!(decision.code, OK);
        assert_eq!(decision.applied_delta, 10);
        assert_eq!(decision.available_stock, 100);
    }

    #[test]
    fn add_over_threshold_caps_at_max() {
        let decision = add_stock(90, 50, 100);
        assert_eq!(decision.code, OK);
        assert_eq!(decision.applied_delta, 10);
        assert_eq!(decision.available_stock, 100);
    }

    #[test]
    fn add_already_above_threshold_returns_negative_delta() {
        let decision = add_stock(150, 1, 100);
        assert_eq!(decision.code, OK);
        assert_eq!(decision.applied_delta, -50);
        assert_eq!(decision.available_stock, 100);
    }

    #[test]
    fn add_already_at_max_returns_zero() {
        let decision = add_stock(100, 5, 100);
        assert_eq!(decision.code, OK);
        assert_eq!(decision.applied_delta, 0);
        assert_eq!(decision.available_stock, 100);
    }

    #[test]
    fn add_zero_quantity_returns_zero() {
        let decision = add_stock(25, 0, 100);
        assert_eq!(decision.code, OK);
        assert_eq!(decision.applied_delta, 0);
        assert_eq!(decision.available_stock, 25);
    }

    #[test]
    fn add_negative_quantity_decreases_stock() {
        let decision = add_stock(25, -7, 100);
        assert_eq!(decision.code, OK);
        assert_eq!(decision.applied_delta, -7);
        assert_eq!(decision.available_stock, 18);
    }

    #[test]
    fn add_unchecked_overflow_wraps_to_min_value() {
        let decision = add_stock(i32::MAX, 1, 100);
        assert_eq!(decision.code, OK);
        assert_eq!(decision.available_stock, i32::MIN);
        assert_eq!(decision.applied_delta, 1);
    }

    #[test]
    fn exports_match_pure_functions_for_characterized_inputs() {
        let removes = [(0, 2), (0, 0), (0, -3), (8, 0), (8, -4), (3, 10), (6, 6), (9, 4)];
        for (available, quantity) in removes {
            assert_eq!(
                crate::catalog_remove_stock(available, quantity),
                remove_stock(available, quantity)
            );
        }

        let adds = [
            (10, 5, 100),
            (90, 10, 100),
            (90, 50, 100),
            (150, 1, 100),
            (100, 5, 100),
            (25, 0, 100),
            (25, -7, 100),
            (i32::MAX, 1, 100),
        ];
        for (available, quantity, max) in adds {
            assert_eq!(
                crate::catalog_add_stock(available, quantity, max),
                add_stock(available, quantity, max)
            );
        }
    }
}
