import { test as base, expect, type Page } from '@playwright/test';
import fs from 'fs';
import os from 'os';
import path from 'path';

// verify.sh always sets ESHOP_VERIFY_EVIDENCE_DIR to a fresh run directory. The
// fallback only exists so a bare `npx playwright test -c <config>` still writes
// somewhere predictable instead of into the repository.
export function evidenceRoot(): string {
  return (
    process.env.ESHOP_VERIFY_EVIDENCE_DIR ??
    path.join(os.homedir(), '.cursor', 'verify-evidence', 'eshop', 'latest')
  );
}

export type Credentials = { readonly username: string; readonly password: string };

// bob is the default so verification never disturbs alice, the account a presenter
// is most likely signed in as. Both are seeded by Identity.API/UsersSeed.cs.
export function verificationUser(): Credentials {
  return {
    username: process.env.ESHOP_VERIFY_USER ?? 'bob',
    password: process.env.ESHOP_VERIFY_PASSWORD ?? 'Pass123$',
  };
}

export type Evidence = {
  readonly dir: string;
  /** Full-page screenshot. Returns the written path. */
  shot(name: string): Promise<string>;
  /** ARIA snapshot of the whole page body: the structural proof a screenshot cannot give. */
  aria(name: string): Promise<string>;
  /** JSON side-note (ids, counts, timings, URLs) kept beside the captures. */
  note(name: string, data: unknown): Promise<string>;
};

function safeName(name: string): string {
  return name.replace(/[^a-zA-Z0-9._-]+/g, '-');
}

export const test = base.extend<{ evidence: Evidence; user: Credentials }>({
  user: async ({}, use) => {
    await use(verificationUser());
  },
  evidence: async ({ page }, use, testInfo) => {
    const feature = path.basename(testInfo.file).replace(/\.spec\.ts$/, '');
    const dir = path.join(evidenceRoot(), feature, safeName(testInfo.title));
    fs.mkdirSync(dir, { recursive: true });
    let seq = 0;
    const next = (name: string, ext: string) =>
      path.join(dir, `${String(++seq).padStart(2, '0')}-${safeName(name)}.${ext}`);

    const evidence: Evidence = {
      dir,
      async shot(name) {
        const file = next(name, 'png');
        await page.screenshot({ path: file, fullPage: true });
        return file;
      },
      async aria(name) {
        const file = next(name, 'aria.yml');
        fs.writeFileSync(file, await page.locator('body').ariaSnapshot());
        return file;
      },
      async note(name, data) {
        const file = next(name, 'json');
        fs.writeFileSync(file, JSON.stringify(data, null, 2));
        return file;
      },
    };
    await use(evidence);
  },
});

export { expect };

/** Sign in through the real Identity.API login page, the way a user does. */
export async function signIn(page: Page, user: Credentials): Promise<void> {
  await page.goto('/');
  await page.getByLabel('Sign in').click();
  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
  await page.getByPlaceholder('Username').fill(user.username);
  await page.getByPlaceholder('Password').fill(user.password);
  await page.getByRole('button', { name: 'Login' }).click();
  await expect(page.getByRole('heading', { name: user.username })).toBeVisible();
}

/** Set every line in the signed-in user's bag to quantity 0 via the real cart form. */
export async function emptyBag(page: Page): Promise<void> {
  await page.goto('/cart');
  await expect(page.getByRole('heading', { name: 'Shopping bag' })).toBeVisible();
  for (;;) {
    const quantities = page.getByLabel('product quantity');
    const before = await quantities.count();
    if (before === 0) break;
    await quantities.first().fill('0');
    await page.getByRole('button', { name: 'Update' }).first().click();
    await expect
      .poll(() => page.getByLabel('product quantity').count(), { timeout: 15_000 })
      .toBeLessThan(before);
  }
  await expect(page.getByText('Your shopping bag is empty')).toBeVisible();
}
