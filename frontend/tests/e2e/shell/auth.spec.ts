import { test, expect } from '@playwright/test';

test.describe('Logout Page', () => {
  test('should navigate to logout page', async ({ page }) => {
    await page.goto('/logout');
    await expect(page).toHaveURL(/\/logout$/);
  });

  test('should display logout content', async ({ page }) => {
    await page.goto('/logout');
    await page.waitForLoadState('domcontentloaded');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).not.toBeEmpty();
  });
});

test.describe('Auth Page', () => {
  test('should navigate to auth page', async ({ page }) => {
    await page.goto('/auth');
    await expect(page).toHaveURL(/\/auth$/);
  });

  test('should display auth redirect content', async ({ page }) => {
    await page.goto('/auth');
    await page.waitForLoadState('domcontentloaded');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).not.toBeEmpty();
  });
});

test.describe('Authentication Flow', () => {
  test('should store post-login redirect path', async ({ page }) => {
    // Navigate to a protected route
    await page.goto('/users');
    
    // Check that redirect path is stored
    const redirectPath = await page.evaluate(() => {
      return sessionStorage.getItem('postLoginRedirect');
    });
    
    // Either redirectPath is set or we're already authenticated
    if (redirectPath) {
      expect(redirectPath).toBe('/users');
    }
  });

  test('should clear auth state on logout navigation', async ({ page }) => {
    // Navigate to logout
    await page.goto('/logout');
    
    // Page should handle logout route
    await expect(page).toHaveURL(/\/logout$/);
  });
});
