import { defineConfig, devices } from '@playwright/test';
import path from 'path';
import { evidenceRoot } from './recipes/fixtures';

// Recipes live in ./recipes. ESHOP_VERIFY_SPEC_DIR points at an ad-hoc directory
// (for example a one-off spec under /tmp) that still gets the same evidence fixtures.
const specDir = process.env.ESHOP_VERIFY_SPEC_DIR ?? path.join(__dirname, 'recipes');

export default defineConfig({
  testDir: specDir,
  // Recipes within one feature mutate shared state (one Redis basket per user), so
  // they run serially. The stack is a single shared instance; parallel workers only
  // race each other.
  fullyParallel: false,
  workers: 1,
  retries: 0,
  forbidOnly: true,
  // Checkout waits on the order processor's grace period (~1-2 minutes).
  timeout: 4 * 60_000,
  expect: { timeout: 15_000 },
  reporter: [
    ['list'],
    ['json', { outputFile: path.join(evidenceRoot(), 'report.json') }],
  ],
  // Traces, failure screenshots, and videos land next to the recipe evidence, never
  // in the repository.
  outputDir: path.join(evidenceRoot(), 'playwright-output'),
  use: {
    baseURL: process.env.ESHOP_VERIFY_URL ?? 'http://localhost:5045',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    ...devices['Desktop Chrome'],
  },
  // No webServer block on purpose: launching is scripts/launch.sh, which refuses to
  // start a second AppHost on the fixed ports.
});
