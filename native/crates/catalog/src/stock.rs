//! Stock mutation rules ported from `CatalogItem.RemoveStock` / `AddStock`.
//!
//! Arithmetic is wrapping to match C#'s default unchecked `int` semantics.

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum StockError {
    /// `AvailableStock == 0`; checked before the quantity.
    Empty,
    /// `quantityDesired <= 0`.
    NonPositiveQuantity,
}

impl StockError {
    pub const fn code(self) -> i32 {
        match self {
            StockError::Empty => 1,
            StockError::NonPositiveQuantity => 2,
        }
    }
}

/// Returns `(new_available, removed)`.
pub fn remove_stock(available: i32, desired: i32) -> Result<(i32, i32), StockError> {
    if available == 0 {
        return Err(StockError::Empty);
    }
    if desired <= 0 {
        return Err(StockError::NonPositiveQuantity);
    }
    let removed = desired.min(available);
    Ok((available.wrapping_sub(removed), removed))
}

/// Returns `(new_available, added)`. Caller clears `OnReorder`.
pub fn add_stock(available: i32, max: i32, quantity: i32) -> (i32, i32) {
    let new_available = if available.wrapping_add(quantity) > max {
        max
    } else {
        available.wrapping_add(quantity)
    };
    (new_available, new_available.wrapping_sub(available))
}

pub const STOCK_OK: i32 = 0;

/// C ABI: returns `STOCK_OK` or a [`StockError::code`]. Out-pointers are only
/// written on success. Null out-pointers are ignored.
#[no_mangle]
pub extern "C" fn catalog_remove_stock(
    available: i32,
    desired: i32,
    new_available: *mut i32,
    removed: *mut i32,
) -> i32 {
    match remove_stock(available, desired) {
        Ok((a, r)) => {
            write_out(new_available, a);
            write_out(removed, r);
            STOCK_OK
        }
        Err(e) => e.code(),
    }
}

/// C ABI: returns the quantity actually added and writes the new stock level.
#[no_mangle]
pub extern "C" fn catalog_add_stock(
    available: i32,
    max: i32,
    quantity: i32,
    new_available: *mut i32,
) -> i32 {
    let (a, added) = add_stock(available, max, quantity);
    write_out(new_available, a);
    added
}

fn write_out(ptr: *mut i32, value: i32) {
    if !ptr.is_null() {
        // SAFETY: caller passes a valid, writable i32 pointer (or null, handled above).
        unsafe { *ptr = value };
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn remove_empty_stock_is_sold_out_regardless_of_quantity() {
        for desired in [1, 0, -1] {
            assert_eq!(remove_stock(0, desired), Err(StockError::Empty));
        }
    }

    #[test]
    fn remove_non_positive_quantity_is_rejected() {
        for desired in [0, -1, i32::MIN] {
            assert_eq!(remove_stock(5, desired), Err(StockError::NonPositiveQuantity));
        }
    }

    #[test]
    fn remove_sufficient_stock_removes_desired() {
        assert_eq!(remove_stock(10, 1), Ok((9, 1)));
        assert_eq!(remove_stock(10, 10), Ok((0, 10)));
        assert_eq!(remove_stock(10, 4), Ok((6, 4)));
    }

    #[test]
    fn remove_insufficient_stock_removes_available_and_zeroes() {
        assert_eq!(remove_stock(3, 4), Ok((0, 3)));
        assert_eq!(remove_stock(3, i32::MAX), Ok((0, 3)));
    }

    #[test]
    fn add_under_max_adds_quantity() {
        assert_eq!(add_stock(0, 100, 10), (10, 10));
        assert_eq!(add_stock(5, 100, 95), (100, 95));
        assert_eq!(add_stock(5, 100, 0), (5, 0));
    }

    #[test]
    fn add_exceeding_max_clamps_to_threshold() {
        assert_eq!(add_stock(90, 100, 20), (100, 10));
        assert_eq!(add_stock(100, 100, 1), (100, 0));
        assert_eq!(add_stock(0, 100, i32::MAX), (100, 100));
    }

    #[test]
    fn ffi_remove_reports_codes_and_writes_outputs() {
        let (mut a, mut r) = (-1, -1);
        assert_eq!(catalog_remove_stock(10, 4, &mut a, &mut r), STOCK_OK);
        assert_eq!((a, r), (6, 4));

        let (mut a, mut r) = (-1, -1);
        assert_eq!(catalog_remove_stock(0, 1, &mut a, &mut r), 1);
        assert_eq!(catalog_remove_stock(5, 0, &mut a, &mut r), 2);
        assert_eq!((a, r), (-1, -1));

        assert_eq!(catalog_remove_stock(10, 4, std::ptr::null_mut(), std::ptr::null_mut()), STOCK_OK);
    }

    #[test]
    fn ffi_add_returns_added_and_writes_new_available() {
        let mut a = -1;
        assert_eq!(catalog_add_stock(90, 100, 20, &mut a), 10);
        assert_eq!(a, 100);
        assert_eq!(catalog_add_stock(0, 100, 10, std::ptr::null_mut()), 10);
    }
}
