import { test, expect } from './fixtures';

test('home grid streams nine items with filters and pages', async ({ page, evidence }) => {
  const missing: string[] = [];
  page.on('response', (r) => {
    if (r.status() === 404) missing.push(r.url());
  });

  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();
  // The grid streams in after the shell, so the heading alone proves nothing.
  await expect(page.locator('.catalog-item')).toHaveCount(9);
  await expect(page.getByText('Loading...')).toHaveCount(0);
  await expect(page.locator('.catalog-search-tag').first()).toBeVisible();

  const first = page.locator('.catalog-item').first();
  await expect(first.locator('.name')).toHaveText('Adventurer GPS Watch');
  await expect(first.locator('.price')).toHaveText('$199.99');
  await expect(first.locator('.stock')).toHaveText(/^\d+ in stock$/);

  await evidence.shot('home-grid');
  await evidence.aria('home-grid');
  await evidence.note('missing-assets', { missing });
  expect(missing).toEqual([]);
});

test('brand filter narrows the grid and marks the active tag', async ({ page, evidence }) => {
  await page.goto('/');
  await expect(page.locator('.catalog-item')).toHaveCount(9);

  const brandGroup = page.locator('.catalog-search-group').filter({ hasText: 'Brand' });
  const brand = brandGroup.locator('.catalog-search-tag').nth(1);
  const brandName = (await brand.textContent())?.trim() ?? '';
  await brand.click();

  await expect(page).toHaveURL(/[?&]brand=\d+/);
  await expect(brandGroup.locator('.catalog-search-tag.active')).toHaveText(brandName);
  const items = page.locator('.catalog-item');
  await expect.poll(() => items.count()).toBeGreaterThan(0);
  const count = await items.count();
  expect(count).toBeLessThanOrEqual(9);

  await evidence.shot(`brand-${brandName}`);
  await evidence.note('brand-filter', { brandName, url: page.url(), count });
});

test('page 2 shows a different first item', async ({ page, evidence }) => {
  await page.goto('/');
  const firstOnPage1 = await page.locator('.catalog-item .name').first().textContent();

  await page.locator('.page-links').getByRole('link', { name: '2', exact: true }).click();
  await expect(page).toHaveURL(/[?&]page=2/);
  // Enhanced navigation swaps the URL before the streamed grid replaces the old DOM, so
  // "an item is visible" is not a wait signal. Neither is `.active-page`: NavLink ignores
  // the query string, so every page link carries it (see features/catalog-browse.md).
  await expect(page.locator('.catalog-item .name').first()).not.toHaveText(firstOnPage1 ?? '');
  const firstOnPage2 = await page.locator('.catalog-item .name').first().textContent();
  const activeLinks = await page.locator('.page-links .active-page').count();

  await evidence.shot('page-2');
  await evidence.note('pagination', { firstOnPage1, firstOnPage2, url: page.url(), activeLinks });
});

test('item page shows description, brand, type and stock', async ({ page, evidence }) => {
  await page.goto('/');
  await page.getByRole('link', { name: 'Adventurer GPS Watch' }).click();

  await expect(page).toHaveURL(/\/item\/\d+$/);
  await expect(page.getByRole('heading', { name: 'Adventurer GPS Watch' })).toBeVisible();
  await expect(page.locator('.stock')).toHaveText(/^\d+ in stock$/);
  await expect(page.getByText('Brand: Solstix')).toBeVisible();
  await expect(page.getByText('Type: Navigation')).toBeVisible();
  // Anonymous visitors get the login call to action, not the add button.
  await expect(page.getByRole('button', { name: 'Log in to purchase' })).toBeVisible();

  await evidence.shot('item-99');
  await evidence.aria('item-99');
});
