import { test, expect, signIn, emptyBag } from '../../recipes/fixtures';

test('drain the verification user bag', async ({ page, user, evidence }) => {
  await signIn(page, user);
  await emptyBag(page);
  await expect(page.getByText('Your shopping bag is empty')).toBeVisible();
  await evidence.shot('bag-empty-after-cleanup');
});
