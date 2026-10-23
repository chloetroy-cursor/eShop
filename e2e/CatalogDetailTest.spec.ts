import { test, expect } from '@playwright/test';

test('Catalog grid renders in the browser', async ({ page }) => {
  const missingAssets: string[] = [];
  page.on('response', (response) => {
    if (response.status() === 404) {
      missingAssets.push(response.url());
    }
  });

  await page.goto('/');

  await expect(page.locator('.catalog-item').first()).toBeVisible();
  await expect(page.locator('.catalog-item')).toHaveCount(9);
  await expect(page.getByText('Loading...')).toHaveCount(0);
  await expect(page.locator('.catalog-search-tag').first()).toBeVisible();

  expect(missingAssets).toEqual([]);
});

test('Catalog grid needs the Blazor script to render', async ({ page }) => {
  await page.route('**/_framework/blazor.web.js', (route) => route.fulfill({ status: 404 }));

  await page.goto('/');

  await expect(page.getByText('Loading...')).toBeVisible();
  await expect(page.locator('.catalog-item')).toHaveCount(0);
});

test('Catalog grid shows stock and description', async ({ page }) => {
  await page.goto('/');

  const card = page.locator('.catalog-item').first();
  await expect(card.locator('.name')).toHaveText('Adventurer GPS Watch');
  await expect(card.locator('.price')).toHaveText('$199.99');
  await expect(card.locator('.stock')).toHaveText(/^\d+ in stock$/);
  await expect(card.locator('.description')).not.toBeEmpty();
});

test('Item page shows stock, brand and type', async ({ page }) => {
  await page.goto('/item/99');

  await expect(page.getByRole('heading', { name: 'Adventurer GPS Watch' })).toBeVisible();
  await expect(page.locator('.stock')).toHaveText(/^\d+ in stock$/);
  await expect(page.getByText('Solstix', { exact: true })).toBeVisible();
  await expect(page.getByText('Navigation', { exact: true })).toBeVisible();
});
