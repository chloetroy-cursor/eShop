---
name: verify-eshop
description: Drives the real eShop storefront (Blazor WebApp on the Aspire AppHost at http://localhost:5045) with Playwright and captures proof. Use when a change touches WebApp, Basket/Catalog/Ordering/Identity APIs, the RabbitMQ event flow, or whenever "does the shop still work" needs evidence rather than a build.
---

# Verify eShop

The surface a user touches is the AdventureWorks storefront: `src/WebApp` (Blazor,
server-rendered with streaming and enhanced navigation) fronting Catalog, Basket,
Ordering and Identity APIs, wired by `src/eShop.AppHost` with Postgres, Redis and
RabbitMQ in Docker. Other surfaces exist and are out of scope here: the MAUI
`HybridApp`/`ClientApp`, the `mobile-bff` YARP proxy, `WebhookClient`, and the Aspire
dashboard at `http://localhost:18848`.

Everything below runs from the repository root. All paths are relative to
`.cursor/skills/verify-eshop/`.

## Launch

```bash
.cursor/skills/verify-eshop/scripts/launch.sh            # default 300 s wait
.cursor/skills/verify-eshop/scripts/launch.sh --timeout 900
```

Wraps the repo's own launcher, `python3 .cursor/skills/demo-prep/scripts/prep.py start`,
which runs `ESHOP_USE_HTTP_ENDPOINTS=1 dotnet run --project src/eShop.AppHost` and
waits until `http://localhost:5045` returns a page containing `catalog-items` *and*
serves `/_framework/blazor.web.js` (the grid is streamed and only reaches the DOM once
that script loads). Ready looks like `ESHOP DEMO READY` plus the storefront, dashboard
and log URLs. First cold start pulls images and builds; 2-6 minutes. A warm start with
persistent containers takes about a minute. Also installs Playwright deps (`npm ci`) if
`node_modules/@playwright/test` is missing.

**Isolation: there is exactly one stack per machine.** WebApp (5045), Identity (5223),
the dashboard (18848) and the DCP-managed containers use fixed ports and names, and
Postgres/RabbitMQ/Redis have `ContainerLifetime.Persistent`. Two AppHosts cannot
coexist; `prep.py start` detects a live storefront and adopts it instead. When it
adopts, `launch.sh` says so and cleanup will leave the stack up. If the storefront is
up but demo-prep does not own it (see Doctor), drive it read-only and do not stop it.

Teardown: `scripts/cleanup.sh` (below). Never `kill` by process name.

## Doctor

```bash
python3 .cursor/skills/verify-eshop/scripts/doctor.py
```

Read-only. Exit 0 means drive it; exit 1 means fix the named line first. It checks:
.NET 10 SDK and Docker daemon; storefront renders catalog and Blazor script; Identity
discovery document at `http://localhost:5223/.well-known/openid-configuration`; the
AppHost runner is one demo-prep tracks (tmux session `eshop-fe-demo` or the pid file
under `~/.cursor/demo-logs/`); `postgres-*`, `eventbus-*`, `redis-*` containers with
the DCP label; Playwright with chromium installed; no Aspire service exited non-zero
(read from DCP, since service stderr never reaches the AppHost log). Run it before the
first drive and again after any drive that failed for a reason you cannot name.

## Drive

Harness: Playwright (`@playwright/test` 1.63, already the repo's e2e tool; see
`playwright.config.ts` and `e2e/` at the root for the CI suite). This skill ships its
own config and recipes so evidence lands outside the repo and nothing starts a second
AppHost.

```bash
.cursor/skills/verify-eshop/scripts/verify.sh                       # all recipes
.cursor/skills/verify-eshop/scripts/verify.sh catalog-browse        # one feature
.cursor/skills/verify-eshop/scripts/verify.sh shopping-bag --headed
.cursor/skills/verify-eshop/scripts/verify.sh checkout-orders --grep Paid
.cursor/skills/verify-eshop/scripts/verify.sh --spec-dir /tmp/my-recipes   # ad hoc
```

`verify.sh` runs doctor, creates a fresh evidence directory, runs the named recipes
from `recipes/` serially (one worker: the basket is shared state), and prints the
evidence path first and last. Recipes are the feature map made executable; read
`features/<feature>.md` for the user-POV walkthrough before changing one.

Ad hoc drives: write `/tmp/x/anything.spec.ts` that imports from
`<repo>/.cursor/skills/verify-eshop/recipes/fixtures` (absolute path) and run with
`--spec-dir /tmp/x`. The fixture gives you `page`, `user`, and `evidence`
(`shot`, `aria`, `note`), plus `signIn(page, user)` and `emptyBag(page)`.

Stable handles that exist in this app (prefer these over CSS):

| Thing | Handle |
| --- | --- |
| Home | `getByRole('heading', { name: 'Ready for a new adventure?' })`; `.catalog-item` x9 |
| Product link | `getByRole('link', { name: 'Adventurer GPS Watch' })` (item 99, $199.99, Solstix / Navigation) |
| Sign in | `getByLabel('Sign in')` → Identity page `getByRole('heading', { name: 'Login' })`, `getByPlaceholder('Username'|'Password')`, `getByRole('button', { name: 'Login' })` |
| Signed-in marker | `getByRole('heading', { name: '<username>' })` (an `h3` in the header) |
| User menu | hover `.dropdown-button`, then `getByRole('link', { name: 'My orders' })` / `getByRole('button', { name: 'Log out' })` |
| Add to bag | `getByRole('button', { name: 'Add to shopping bag' })`; anonymous shows `'Log in to purchase'` |
| Bag icon | `getByLabel('cart')`, badge `.cart-badge` |
| Bag page | heading `Shopping bag`; `getByLabel('product quantity')`; `getByRole('button', { name: 'Update' })`; `getByRole('link', { name: 'Check out' })`; empty text `Your shopping bag is empty` |
| Checkout | heading `Checkout`; `getByLabel('Address'|'City'|'State'|'Zip code'|'Country')`; `getByRole('button', { name: 'Place order' })` |
| Orders | heading `Orders`; rows `.orders-item:not(.orders-header)` with `.order-number`, `.order-status` |

Credentials: `ESHOP_VERIFY_USER` / `ESHOP_VERIFY_PASSWORD`, default `bob` / `Pass123$`
(seeded by `src/Identity.API/UsersSeed.cs`; `alice` is the other). Use bob so a
presenter signed in as alice is undisturbed.

## Evidence

Root: `~/.cursor/verify-evidence/eshop/<UTC run id>/` (override the root with
`ESHOP_VERIFY_EVIDENCE_ROOT`, or the exact dir with `ESHOP_VERIFY_EVIDENCE_DIR`).
Inside: `doctor.txt`, `report.json` (Playwright JSON), `<feature>/<test title>/NN-*.png
| .aria.yml | .json` written by the recipe, and `playwright-output/` with traces and
failure screenshots. Open a trace with `npx playwright show-trace <trace.zip>`.

Proof standards for this app:

- Drive the real user path: Identity login form, the item page button, the cart form.
  Never call Basket/Ordering APIs directly as a substitute for the UI.
- Capture the action and the state it produced: badge count after add, bag line after
  navigation, order row after Place order, not just a final screen.
- Verify side effects beneath the UI with the read-only helper:

  ```bash
  .cursor/skills/verify-eshop/scripts/side-effects.sh orders [bob]   # ordering.orders rows
  .cursor/skills/verify-eshop/scripts/side-effects.sh queues         # RabbitMQ depth/consumers
  .cursor/skills/verify-eshop/scripts/side-effects.sh basket         # raw Redis baskets
  ```

  Containers are found by the DCP label, Postgres uses the container's own
  `POSTGRES_PASSWORD`, Redis the container's `REDIS_PASSWORD`. `orders <name>` matches
  the Identity display name case-insensitively (`Bob`, not the login).
- Streaming and enhanced navigation mean a 200 and a changed URL prove nothing. Wait
  on content (`toHaveText`, `toHaveCount`), never on "something is visible".
- No mocks. The only external boundary (OpenAI/Ollama) is disabled in `AppHost/Program.cs`.
- A screenshot without the `bob` header or an ARIA snapshot is weak proof; capture both
  where identity matters.

## Cleanup

```bash
.cursor/skills/verify-eshop/scripts/cleanup.sh               # drain bag; stop stack if we started it
.cursor/skills/verify-eshop/scripts/cleanup.sh --keep-stack  # drain bag only
```

Drains the verification user's bag through the real cart form (`scripts/cleanup/`),
then stops the AppHost via `prep.py stop` **only if** `launch.sh` wrote
`~/.cursor/verify-evidence/eshop/.state/started-apphost`. An adopted stack stays up.
Persistent containers stay by design. Orders cannot be deleted from the UI and are
left in `orderingdb`; they are harmless and numbered, so recipes count rows before and
after rather than assuming an empty table. Evidence directories are never touched.

## Helpers

| Script | Purpose |
| --- | --- |
| `scripts/launch.sh [--timeout N]` | start or adopt the stack, record ownership |
| `scripts/doctor.py` | read-only health gate, exit 0/1 |
| `scripts/verify.sh [recipe...] [--headed] [--grep X] [--spec-dir D] [--skip-doctor]` | drive + collect evidence |
| `scripts/side-effects.sh orders|queues|basket` | state behind the UI |
| `scripts/cleanup.sh [--keep-stack]` | residue and instance teardown |
| `scripts/env.sh` | sourced; drops a temp-dir `PLAYWRIGHT_BROWSERS_PATH` that agent sandboxes export |
| `recipes/fixtures.ts` | `test` with `evidence`/`user`, `signIn`, `emptyBag` |

## Feature map

`features/README.md` indexes one file per user-facing feature. Keep it honest with
`/maintain-verification-skill`.
