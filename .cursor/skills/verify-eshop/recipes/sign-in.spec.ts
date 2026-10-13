import { test, expect, signIn } from './fixtures';

test('sign in through Identity and come back signed in', async ({ page, evidence, user }) => {
  await page.goto('/');
  await page.getByLabel('Sign in').click();

  // The login form is served by Identity.API (port 5223 under the http profile),
  // not the storefront. Proving the hostname changed is part of the proof.
  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
  const loginUrl = page.url();
  await evidence.shot('identity-login-form');

  await page.getByPlaceholder('Username').fill(user.username);
  await page.getByPlaceholder('Password').fill(user.password);
  await page.getByRole('button', { name: 'Login' }).click();

  await expect(page.getByRole('heading', { name: 'Ready for a new adventure?' })).toBeVisible();
  await expect(page.getByRole('heading', { name: user.username })).toBeVisible();
  await expect(page.getByLabel('Sign in')).toHaveCount(0);

  await evidence.shot('signed-in-home');
  await evidence.aria('signed-in-home');
  await evidence.note('sign-in', { loginUrl, afterUrl: page.url(), username: user.username });
});

test('wrong password stays on the login page with an error', async ({ page, evidence, user }) => {
  await page.goto('/');
  await page.getByLabel('Sign in').click();
  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();

  await page.getByPlaceholder('Username').fill(user.username);
  await page.getByPlaceholder('Password').fill('definitely-wrong');
  await page.getByRole('button', { name: 'Login' }).click();

  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
  await expect(page.getByText(/invalid/i)).toBeVisible();
  await evidence.shot('invalid-credentials');
});

test('anonymous add-to-bag redirects to login', async ({ page, evidence }) => {
  await page.goto('/item/99');
  await page.getByRole('button', { name: 'Log in to purchase' }).click();
  await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible();
  await evidence.shot('redirected-to-login');
});

test('log out from the user menu returns to anonymous', async ({ page, evidence, user }) => {
  await signIn(page, user);

  // The menu opens on hover; the form inside it is a real POST to user/logout.
  await page.locator('.dropdown-button').hover();
  await page.getByRole('button', { name: 'Log out' }).click();

  await expect(page.getByLabel('Sign in')).toBeVisible();
  await expect(page.getByRole('heading', { name: user.username })).toHaveCount(0);
  await evidence.shot('logged-out-home');
});
