# Browse the catalog

Browsing lets any visitor, signed in or not, see the product grid on the home page,
narrow it by brand or type, page through it, and open a product to read its details,
price and stock.

## Sub-features

- `browse-grid` streams nine product cards (image, name, description, price, `N in stock`) under the hero heading.
- `browse-filter-brand` narrows the grid to one brand and marks that tag active.
- `browse-filter-type` narrows the grid to one type; brand and type combine.
- `browse-page` moves between pages of nine with the numbered links.
- `browse-item` opens a product page with description, brand, type, stock and price.

## How to get to it (user POV)

- Open `http://localhost:5045`.
- Click the AdventureWorks logo from any page.
- Click a product card or its name from the grid.
- Visit `http://localhost:5045/item/<id>` directly (item 99 is the canonical fixture).
- Click a tag under `Brand` or `Type` in the left filter panel.
- Click a page number under the grid.

## Driving it with Playwright

Preconditions:

- Doctor exits 0. No sign-in needed.
- Recipe: `scripts/verify.sh catalog-browse`.

- **Grid.** Open `/`. `page.goto('/')`; wait `expect(page.locator('.catalog-item')).toHaveCount(9)` and `getByText('Loading...')` count 0. First card `.name` is `Adventurer GPS Watch`, `.price` is `$199.99`, `.stock` matches `/^\d+ in stock$/`. Register a `page.on('response')` 404 collector; it must stay empty.
- **Brand filter.** Click the second tag in the `Brand` group. `page.locator('.catalog-search-group').filter({ hasText: 'Brand' }).locator('.catalog-search-tag').nth(1).click()`. URL gains `?brand=<id>`, that tag has class `active`, grid count is between 1 and 9.
- **Page 2.** Click `2`. `page.locator('.page-links').getByRole('link', { name: '2', exact: true }).click()`. Wait for the first `.catalog-item .name` to differ from the page 1 value. URL gains `?page=2`.
- **Item page.** Click `getByRole('link', { name: 'Adventurer GPS Watch' })`. URL ends `/item/99`; heading `Adventurer GPS Watch`; `Brand: Solstix`, `Type: Navigation`, `.stock` matches `/^\d+ in stock$/`; anonymous sees `getByRole('button', { name: 'Log in to purchase' })`.
- **Proof.** `evidence.shot` and `evidence.aria` of the grid and of item 99; `evidence.note` of the 404 list and the pagination names.

## Gotchas

- The home shell returns 200 and the heading renders before the grid streams in. A
  missing `/_framework/blazor.web.js` leaves `Loading...` forever while every server
  check passes. Assert the cards, not the heading.
- Enhanced navigation changes the URL before the new grid replaces the old DOM.
  `toHaveURL` then `first().toBeVisible()` passes against stale content.
- Every page link carries `active-page` and `aria-current="page"` (NavLink ignores the
  query string since .NET 9), so the active page is not identifiable from the DOM and
  the pagination looks all-selected to users. Do not use `.active-page` as a wait
  signal; treat it as a known product gap.
- Brand and type filter tags use the same class; scope by the group heading.
- Stock numbers change after a verified checkout (`StockConfirmed` decrements), so
  assert the shape, not the value.
