# Checkout and orders

Checkout turns the bag into an order: the signed-in user confirms a shipping address
prefilled from their profile, places the order, and lands on their orders list where
the new order appears as `Submitted` and advances on its own to `Paid` as the
back-end services validate stock and take payment. This is the path that exercises
Ordering.API publishing through RabbitMQ, the subject of INC-001.

## Sub-features

- `checkout-prefill` shows address, city, state, zip and country from the user's Identity claims.
- `checkout-place` submits the order, drains the bag, and navigates to `/user/orders`.
- `orders-list` shows number, date, total and a status pill per order.
- `orders-progress` moves the new order `Submitted` → `AwaitingValidation` → `StockConfirmed` → `Paid` and the page refreshes itself on each change.
- `checkout-empty` rejects Place order with `Your cart is empty` when the bag is empty.

## How to get to it (user POV)

- Click `Check out` in the bag summary.
- Visit `/checkout` directly while signed in.
- Open the user menu and click `My orders`, or visit `/user/orders`.

## Driving it with Playwright

Preconditions:

- Doctor exits 0, including `eventbus-*` (RabbitMQ) and `no crashed Aspire services`;
  `scripts/side-effects.sh queues` lists `Ordering`, `OrderProcessor`, `Catalog`,
  `PaymentProcessor` each with 1 consumer.
- Signed in as bob with an empty bag. Record `scripts/side-effects.sh orders bob` first.
- Recipe: `scripts/verify.sh checkout-orders` (about 2 minutes; the Paid test waits on
  the order processor's 1 minute grace period polled every 30 seconds).

- **Reach checkout.** Add item 99, `getByLabel('cart').click()`, `getByRole('link', { name: 'Check out' }).click()`. Heading `Checkout`; `getByLabel('Address')` is non-empty (`15703 NE 61st Ct` for bob).
- **Place.** `getByRole('button', { name: 'Place order' }).click()`. URL ends `/user/orders`; heading `Orders`; `.orders-item:not(.orders-header)` count is previous + 1; the new row's `.order-number` is one not present before.
- **Row exists.** `scripts/side-effects.sh orders bob` returns one more row, id equal to the new order number, status `Submitted`, street equal to the form value.
- **Progress.** Poll the new row's `.order-status` (interval 2 s, up to 3 min) until `Paid`; record every distinct value seen. Expected sequence `Submitted`, `AwaitingValidation`, `StockConfirmed`, `Paid`.
- **Confirm.** `scripts/side-effects.sh orders bob` now shows `Paid` for that id. `getByLabel('cart').click()` shows `Your shopping bag is empty`.
- **Empty rejection.** With an empty bag, `page.goto('/checkout')`, click `Place order`. `Your cart is empty` visible and URL still `/checkout`.
- **Proof.** Screenshots of the prefilled form, the orders page right after placing, and at `Paid`; ARIA snapshot of the orders list; notes with DB rows before/after, the status sequence, and queue listings before/after.

## Gotchas

- Orders are listed oldest first and never deleted, so "the first row" is the oldest
  order. Identify the new one by the number you had not seen.
- `buyers."Name"` is the Identity display name (`Bob`), not the login; the helper
  matches case-insensitively.
- A status stuck at `Submitted` for more than ~2 minutes means OrderProcessor or
  RabbitMQ is unhealthy; stuck at `AwaitingValidation` points at Catalog.API, at
  `StockConfirmed` at PaymentProcessor. Check `queues` for growing `messages` and
  doctor for crashed services before blaming the UI.
- The orders page refreshes via an interactive-server circuit
  (`OrdersRefreshOnStatusChange`). A plain `page.reload()` also works if the circuit
  dropped, but the self-refresh is itself a behavior worth proving.
- Checkout posts `CardTypeId = 1` and no card fields; there is no payment form to fill.
- The checkout `EditForm` validates server-side; errors render in a
  `ValidationSummary` under the buttons, not inline next to the empty-bag message.
- Each `Paid` order decrements catalog stock; repeated runs lower `N in stock` for item 99.
