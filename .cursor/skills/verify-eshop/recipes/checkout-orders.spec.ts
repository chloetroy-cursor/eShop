import { execFileSync } from 'child_process';
import path from 'path';
import { test, expect, signIn, emptyBag } from './fixtures';

const sideEffects = path.join(__dirname, '..', 'scripts', 'side-effects.sh');

function run(args: readonly string[]): string {
  return execFileSync(sideEffects, [...args], { encoding: 'utf8' });
}

type OrderRow = { readonly id: number; readonly status: string; readonly date: string };

function parseOrders(tsv: string): OrderRow[] {
  return tsv
    .split('\n')
    .filter((line) => line.trim().length > 0)
    .map((line) => {
      const [id, status, date] = line.split('\t');
      return { id: Number(id), status, date };
    });
}

test.beforeEach(async ({ page, user }) => {
  await signIn(page, user);
  await emptyBag(page);
});

test('place an order and watch it reach Paid', async ({ page, evidence, user }) => {
  const before = parseOrders(run(['orders', user.username]));
  const queuesBefore = run(['queues']);

  await page.goto('/item/99');
  await page.getByRole('button', { name: 'Add to shopping bag' }).click();
  await page.getByLabel('cart').click();
  await page.getByRole('link', { name: 'Check out' }).click();

  // Address fields are prefilled from the signed-in user's Identity claims.
  await expect(page.getByRole('heading', { name: 'Checkout' })).toBeVisible();
  const street = page.getByLabel('Address');
  await expect(street).not.toHaveValue('');
  const prefilledStreet = await street.inputValue();
  await evidence.shot('checkout-prefilled');

  await page.getByRole('button', { name: 'Place order' }).click();

  await expect(page).toHaveURL(/\/user\/orders$/);
  await expect(page.getByRole('heading', { name: 'Orders' })).toBeVisible();
  const rows = page.locator('.orders-item:not(.orders-header)');
  await expect(rows).toHaveCount(before.length + 1);
  // The list is not sorted newest-first; the new order is the number we had not seen.
  const knownIds = new Set(before.map((o) => String(o.id)));
  const numbers = (await rows.locator('.order-number').allTextContents()).map((t) => t.trim());
  const fresh = numbers.filter((n) => !knownIds.has(n));
  expect(fresh).toHaveLength(1);
  const orderNumber = fresh[0];
  expect(orderNumber).toMatch(/^\d+$/);
  const newest = rows.filter({ has: page.locator('.order-number', { hasText: new RegExp(`^\\s*${orderNumber}\\s*$`) }) });
  await evidence.shot('orders-after-place');
  await evidence.aria('orders-after-place');

  // The DB row exists before any status progression; its street matches the form.
  const justPlaced = parseOrders(run(['orders', user.username]));
  expect(justPlaced.length).toBe(before.length + 1);
  expect(justPlaced[0].id).toBe(Number(orderNumber));
  await evidence.note('db-after-place', { rows: justPlaced, prefilledStreet });

  // OrderProcessor releases the grace period after 1 minute on a 30 second poll, then
  // Catalog confirms stock and PaymentProcessor pays, each via RabbitMQ. The orders
  // page re-renders itself when the status changes (OrdersRefreshOnStatusChange).
  const seen: string[] = [];
  await expect
    .poll(
      async () => {
        const status = (await newest.locator('.order-status').textContent())?.trim() ?? '';
        if (seen[seen.length - 1] !== status) seen.push(status);
        return status;
      },
      { timeout: 3 * 60_000, intervals: [2_000] },
    )
    .toBe('Paid');
  await evidence.shot('orders-paid');

  const final = parseOrders(run(['orders', user.username]));
  expect(final[0].id).toBe(Number(orderNumber));
  expect(final[0].status).toBe('Paid');
  await evidence.note('status-progression', { orderNumber, seen, db: final[0] });
  await evidence.note('queues', { before: queuesBefore, after: run(['queues']) });

  // Checkout drains the bag; proving that is part of the order path.
  await page.getByLabel('cart').click();
  await expect(page.getByText('Your shopping bag is empty')).toBeVisible();
});

test('placing an order with an empty bag is rejected', async ({ page, evidence }) => {
  await page.goto('/checkout');
  await expect(page.getByRole('heading', { name: 'Checkout' })).toBeVisible();
  await page.getByRole('button', { name: 'Place order' }).click();

  await expect(page.getByText('Your cart is empty')).toBeVisible();
  await expect(page).toHaveURL(/\/checkout$/);
  await evidence.shot('checkout-empty-rejected');
});
