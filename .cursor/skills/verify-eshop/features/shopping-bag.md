# Shopping bag

The bag lets a signed-in user add a product from its page, see the running count in
the header, review lines with quantity and totals, change a quantity, and remove a line
by setting it to zero. Contents persist across page loads and sessions for that user.

## Sub-features

- `bag-add` adds one unit from the product page and increments the header badge.
- `bag-view` lists each line with image, name, unit price, quantity input and line total, plus a summary total and count.
- `bag-update` changes a quantity and recomputes line and summary totals.
- `bag-persist` keeps the contents across a reload (stored in Basket.API / Redis).
- `bag-remove` removes a line at quantity zero and shows the empty state.

## How to get to it (user POV)

- Click `Add to shopping bag` on a product page while signed in.
- Click the bag icon in the header.
- Visit `/cart` directly while signed in.
- Click `Continue shopping` or the logo to leave.

## Driving it with Playwright

Preconditions:

- Doctor exits 0. Signed in as bob (`signIn(page, user)`); bag empty (`emptyBag(page)`).
- Recipe: `scripts/verify.sh shopping-bag`.

- **Add.** `page.goto('/item/99')`; `getByLabel('cart').locator('.cart-badge')` count 0; `getByRole('button', { name: 'Add to shopping bag' }).click()`. Badge text becomes `1`.
- **View.** `getByLabel('cart').click()`. Heading `Shopping bag`; `.cart-item` count 1 with `.name` `Adventurer GPS Watch`; `getByLabel('product quantity')` value `1`; `.cart-summary-total` contains `$199.99`.
- **Update.** `getByLabel('product quantity').fill('3')`, `getByRole('button', { name: 'Update' }).click()`. Quantity `3`, `.cart-item .catalog-item-total` `$599.97`, summary `$599.97`, `.cart-summary-header .filter-badge` `3`.
- **Persist.** `page.reload()`. Quantity still `3`.
- **Remove.** `fill('0')` then `Update`. `Your shopping bag is empty` visible; header badge gone.
- **Proof.** Screenshot after add (badge visible), screenshot plus ARIA of the one-line bag, screenshot after reload at quantity 3, screenshot of the empty state. Optionally `scripts/side-effects.sh basket` for the raw Redis entry.

## Gotchas

- Basket state is one Redis key per user (keyed by identity GUID) and outlives the run.
  Every recipe here starts and ends with `emptyBag`; skipping that makes counts wrong
  for the next run and for a human using bob.
- The header badge re-renders within the same request after add. If it does not
  change, Basket.API is unreachable and the UI silently logs the user out (CartMenu
  catches 401 by calling logout). Look for the `Sign in` link reappearing.
- `Update` with the same quantity posts and re-renders without visible change.
- Anonymous `/cart` redirects to login; the empty state text only appears signed in.
- Prices come from the catalog at add time; a price edit does not reprice existing lines.
