# eShop verification map

This directory is the maintained source for verifying the user-facing behavior of the
AdventureWorks storefront. Read this index before driving the app, then use the matching
feature file as the recipe. Each feature has an executable twin in `../recipes/<feature>.spec.ts`.

## Baseline preconditions

- The Aspire stack is up via `scripts/launch.sh` and `scripts/doctor.py` exits 0.
- Storefront at `http://localhost:5045`; Identity login at `http://localhost:5223`.
- Catalog is the seeded set: 101 items, 9 per page, item 99 is `Adventurer GPS Watch`
  at `$199.99`, brand `Solstix`, type `Navigation`.
- Verification user is `bob` / `Pass123$` (`ESHOP_VERIFY_USER`, `ESHOP_VERIFY_PASSWORD`),
  with address claims `15703 NE 61st Ct, Redmond, WA 98052, U.S.`.
- bob's shopping bag is empty (`scripts/cleanup.sh --keep-stack` drains it).
- Never drive a storefront that doctor reports as untracked by demo-prep, except read-only.

## Driving conventions

- Start every recipe from the baseline unless its preconditions say otherwise.
- Prefer ARIA roles and accessible names; the stable CSS handles are `.catalog-item`,
  `.cart-item`, `.cart-badge`, `.orders-item`, `.order-number`, `.order-status`.
- Pages stream-render and navigate with enhanced navigation. Wait on content
  assertions, never on URL change or first-visible.
- One worker. The basket is one Redis entry per user and every recipe shares it.
- Restore the bag after a mutation. Orders persist; count them, do not expect zero.

## Proof and skip reporting

- Capture the user action and the resulting state, not only the final screen.
- UI proof includes a screenshot with the header (logo, `bob`, bag icon) visible and,
  for state-bearing pages, an ARIA snapshot.
- Mutation proof includes a second view of the stored value: a reload, a navigation
  back, or `scripts/side-effects.sh`.
- Record the feature ID and entry point with each artifact (the fixture names the
  directory after the recipe and test title).
- Report an unreachable path with the attempted command and the unmet precondition.
  Do not report a skipped entry point as verified through a different path.

## Feature entry contract

Each feature file starts with an H1 and one paragraph describing the user-visible
behavior, then exactly four H2 sections in this order:

1. `Sub-features` lists short IDs with one line for each behavior.
2. `How to get to it (user POV)` lists every user entry point.
3. `Driving it with Playwright` starts with `Preconditions:` and pairs each user action
   with an exact locator/command and the observable result.
4. `Gotchas` lists traps that waste or invalidate a run.

## Features

- [Browse the catalog](./catalog-browse.md) covers the streamed home grid, brand and
  type filters, pagination, and the item detail page.
- [Sign in and out](./sign-in.md) covers the Identity login round trip, bad
  credentials, the anonymous purchase redirect, and logging out.
- [Shopping bag](./shopping-bag.md) covers add to bag, header badge, quantity update,
  totals, and removal.
- [Checkout and orders](./checkout-orders.md) covers the prefilled checkout form, Place
  order, the orders list, the Submitted → Paid progression through RabbitMQ, and the
  empty-bag rejection.
