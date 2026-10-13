# Sign in and out

Sign-in sends the visitor to the Identity service's login page, accepts a seeded
username and password, and returns them to the storefront with their name in the
header; signing out from the user menu makes them anonymous again. Buying anything
requires it.

## Sub-features

- `auth-login` completes the Identity login and lands back on the page the user left.
- `auth-bad-password` keeps the user on the login page with an error.
- `auth-purchase-redirect` sends an anonymous `Log in to purchase` click to login.
- `auth-menu` exposes `My orders` and `Log out` under the user icon when signed in.
- `auth-logout` posts the logout form and restores the `Sign in` link.

## How to get to it (user POV)

- Click the user icon (`Sign in`) at the top right of any page.
- Click `Log in to purchase` on a product page while anonymous.
- Visit `/cart`, `/checkout` or `/user/orders` while anonymous (all redirect to login).
- Hover the user icon when signed in to open the menu; click `Log out`.

## Driving it with Playwright

Preconditions:

- Doctor exits 0. Fresh browser context (Playwright default).
- Recipe: `scripts/verify.sh sign-in`.

- **Open login.** From `/`, `page.getByLabel('Sign in').click()`. Heading `Login` appears and the URL host is now `localhost:5223` (Identity.API).
- **Credentials.** `getByPlaceholder('Username').fill('bob')`, `getByPlaceholder('Password').fill('Pass123$')`, `getByRole('button', { name: 'Login' }).click()`. Back on `/` with heading `Ready for a new adventure?`, `getByRole('heading', { name: 'bob' })` visible, and `getByLabel('Sign in')` count 0.
- **Bad password.** Same form with `definitely-wrong`. Heading `Login` stays; `getByText(/invalid/i)` is visible.
- **Purchase redirect.** `page.goto('/item/99')`, `getByRole('button', { name: 'Log in to purchase' }).click()`. Heading `Login` appears.
- **Log out.** After `signIn(page, user)`: `page.locator('.dropdown-button').hover()`, `getByRole('button', { name: 'Log out' }).click()`. `getByLabel('Sign in')` visible and the `bob` heading gone.
- **Proof.** Screenshots of the Identity form and the signed-in home; ARIA snapshot of the signed-in header; note with the login URL.

## Gotchas

- The login page belongs to Identity.API, served on `5223` under the http launch
  profile. If Identity is down the click lands on a connection error, not a 500 from
  the storefront. Doctor checks its discovery document.
- The username heading is an `h3` inside the header; `getByText('bob')` also matches
  the login hint on the Identity page, so use the heading role.
- The user menu opens on hover only; clicking the icon does nothing.
- Logout is a real POST with an antiforgery token. Driving `GET /user/logout` is not
  the user path.
- Playwright's default context is fresh per test, so there is no shared storage state
  here; the repo's root `e2e/login.setup.ts` approach is for CI, not for this skill.
