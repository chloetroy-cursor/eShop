//! Stock mutation rules ported from `CatalogItem.RemoveStock` / `AddStock`.
//!
//! Every add/sub uses `wrapping_*` so overflow matches C# unchecked `int`.

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum StockError {
    Empty,
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

pub const STOCK_OK: i32 = 0;

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

/// Returns `(new_available, added)`. `OnReorder` stays in C#.
pub fn add_stock(available: i32, max: i32, quantity: i32) -> (i32, i32) {
    let wrapped_sum = available.wrapping_add(quantity);
    let new_available = if wrapped_sum > max { max } else { wrapped_sum };
    (new_available, new_available.wrapping_sub(available))
}

/// Status: 0 ok, 1 empty stock, 2 non-positive qty. Out-pointers written only on success.
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
        // SAFETY: null rejected above; caller provides a writable i32 or null.
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
            assert_eq!(
                remove_stock(5, desired),
                Err(StockError::NonPositiveQuantity)
            );
        }
    }

    #[test]
    fn remove_negative_stock_is_not_empty() {
        assert_eq!(remove_stock(-5, 3), Ok((0, -5)));
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
    fn remove_min_value_wraps_subtract_to_zero() {
        assert_eq!(remove_stock(i32::MIN, 1), Ok((0, i32::MIN)));
    }

    #[test]
    fn add_under_max_adds_quantity() {
        assert_eq!(add_stock(0, 100, 10), (10, 10));
        assert_eq!(add_stock(5, 100, 95), (100, 95));
        assert_eq!(add_stock(5, 100, 0), (5, 0));
    }

    #[test]
    fn add_negative_quantity_subtracts() {
        assert_eq!(add_stock(10, 100, -3), (7, -3));
    }

    #[test]
    fn add_exceeding_max_clamps_to_threshold() {
        assert_eq!(add_stock(90, 100, 20), (100, 10));
        assert_eq!(add_stock(100, 100, 1), (100, 0));
        assert_eq!(add_stock(0, 100, i32::MAX), (100, 100));
    }

    #[test]
    fn add_already_above_max_clamps_only_when_wrapped_sum_exceeds() {
        assert_eq!(add_stock(150, 100, 1), (100, -50));
        assert_eq!(add_stock(150, 100, -10), (100, -50));
        assert_eq!(add_stock(150, 100, -60), (90, -60));
    }

    #[test]
    fn add_max_value_plus_one_wraps_instead_of_clamping() {
        assert_eq!(add_stock(i32::MAX, 100, 1), (i32::MIN, 1));
    }

    #[test]
    fn add_min_value_minus_one_wraps_then_clamps() {
        assert_eq!(add_stock(i32::MIN, 100, -1), (100, 100i32.wrapping_sub(i32::MIN)));
    }

    #[test]
    fn add_clamp_from_max_value_return_wraps() {
        assert_eq!(add_stock(i32::MAX, 100, 0), (100, 100i32.wrapping_sub(i32::MAX)));
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

        assert_eq!(
            catalog_remove_stock(10, 4, std::ptr::null_mut(), std::ptr::null_mut()),
            STOCK_OK
        );
    }

    #[test]
    fn ffi_add_returns_added_and_writes_new_available() {
        let mut a = -1;
        assert_eq!(catalog_add_stock(90, 100, 20, &mut a), 10);
        assert_eq!(a, 100);
        assert_eq!(catalog_add_stock(0, 100, 10, std::ptr::null_mut()), 10);
    }
}
