import { test, expect, signIn, emptyBag } from './fixtures';

test.beforeEach(async ({ page, user }) => {
  await signIn(page, user);
  await emptyBag(page);
});

test.afterEach(async ({ page }) => {
  // Basket state is one Redis entry per user and survives the run; leave it empty.
  await emptyBag(page);
});

test('add an item, see the badge, see the line in the bag', async ({ page, evidence }) => {
  await page.goto('/');
  await page.getByRole('link', { name: 'Adventurer GPS Watch' }).click();
  await expect(page.getByRole('heading', { name: 'Adventurer GPS Watch' })).toBeVisible();
  await expect(page.getByLabel('cart').locator('.cart-badge')).toHaveCount(0);

  await page.getByRole('button', { name: 'Add to shopping bag' }).click();

  // Header badge re-renders in the same request; the number is the side effect.
  await expect(page.getByLabel('cart').locator('.cart-badge')).toHaveText('1');
  await evidence.shot('item-added-badge');

  await page.getByLabel('cart').click();
  await expect(page.getByRole('heading', { name: 'Shopping bag' })).toBeVisible();
  const line = page.locator('.cart-item');
  await expect(line).toHaveCount(1);
  await expect(line.locator('.name')).toHaveText('Adventurer GPS Watch');
  await expect(page.getByLabel('product quantity')).toHaveValue('1');
  await expect(page.locator('.cart-summary-total')).toContainText('$199.99');

  await evidence.shot('bag-one-line');
  await evidence.aria('bag-one-line');
});

test('update quantity recomputes the line and summary totals', async ({ page, evidence }) => {
  await page.goto('/item/99');
  await page.getByRole('button', { name: 'Add to shopping bag' }).click();
  await page.getByLabel('cart').click();
  await expect(page.getByLabel('product quantity')).toHaveValue('1');

  await page.getByLabel('product quantity').fill('3');
  await page.getByRole('button', { name: 'Update' }).click();

  await expect(page.getByLabel('product quantity')).toHaveValue('3');
  await expect(page.locator('.cart-item .catalog-item-total')).toHaveText(/\$599\.97/);
  await expect(page.locator('.cart-summary-total')).toContainText('$599.97');
  await expect(page.locator('.cart-summary-header .filter-badge')).toHaveText('3');

  // Reload proves the quantity is stored in Basket.API, not just pending in the form.
  await page.reload();
  await expect(page.getByLabel('product quantity')).toHaveValue('3');
  await evidence.shot('bag-quantity-3-after-reload');
  await evidence.note('totals', { quantity: 3, expectedTotal: '599.97' });
});

test('quantity zero removes the line and shows the empty state', async ({ page, evidence }) => {
  await page.goto('/item/99');
  await page.getByRole('button', { name: 'Add to shopping bag' }).click();
  await page.getByLabel('cart').click();
  await expect(page.getByLabel('product quantity')).toHaveValue('1');

  await page.getByLabel('product quantity').fill('0');
  await page.getByRole('button', { name: 'Update' }).click();

  await expect(page.getByText('Your shopping bag is empty')).toBeVisible();
  await expect(page.getByLabel('cart').locator('.cart-badge')).toHaveCount(0);
  await evidence.shot('bag-empty');
});
