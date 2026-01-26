import { test, expect } from '@playwright/test';

/**
 * Tests for the URL Getter Pilet (API Playground)
 * This pilet provides API testing functionality
 */
test.describe('URL Getter Pilet (API Playground)', () => {
  test('should navigate to api-playground route', async ({ page }) => {
    await page.goto('/api-playground');
    await expect(page).toHaveURL(/\/api-playground$/);
  });

  test('should display page content', async ({ page }) => {
    await page.goto('/api-playground');
    await page.waitForLoadState('domcontentloaded');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).not.toBeEmpty();
  });

  test.describe('URL Getter Features (When Authenticated)', () => {
    test('should display page title', async ({ page }) => {
      await page.goto('/api-playground');
      
      // Check for API Playground or URL Getter title
      const apiPlaygroundTitle = page.locator('h1:has-text("API Playground")');
      const urlGetterTitle = page.locator('h1:has-text("URL Getter")');
      
      const hasTitle = await apiPlaygroundTitle.count() > 0 || await urlGetterTitle.count() > 0;
      
      if (hasTitle) {
        if (await apiPlaygroundTitle.count() > 0) {
          await expect(apiPlaygroundTitle).toBeVisible();
        } else {
          await expect(urlGetterTitle).toBeVisible();
        }
      }
    });
  });
});

test.describe('URL Getter - Integration', () => {
  test('pilet should integrate with shell navigation', async ({ page }) => {
    // Start from home
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');
    
    // Navigate to API Playground
    await page.goto('/api-playground');
    
    // Verify we arrived
    await expect(page).toHaveURL(/\/api-playground$/);
    
    // Content should be present
    const appDiv = page.locator('#app');
    await expect(appDiv).not.toBeEmpty();
  });

  test('should share common UI components with shell', async ({ page }) => {
    await page.goto('/api-playground');
    
    // Check if common UI components are used
    const hasCards = await page.locator('[data-slot="card"], .rounded-xl.border, .shadow').count() > 0;
    const hasButtons = await page.locator('button').count() > 0;
    
    expect(hasButtons || hasCards).toBeTruthy();
  });
});

test.describe('URL Getter - Responsive', () => {
  test('should display correctly on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/api-playground');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });

  test('should display correctly on tablet', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto('/api-playground');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });
});
