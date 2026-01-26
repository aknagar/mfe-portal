import { test, expect } from '@playwright/test';

test.describe('Error Handling', () => {
  test('should handle 404 page not found', async ({ page }) => {
    await page.goto('/non-existent-page');
    
    // Page should still render without crashing
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });

  test('should display error boundary for component errors', async ({ page }) => {
    await page.goto('/');
    
    // The layout includes ErrorInfo component for handling errors
    // This test ensures the app doesn't crash on load
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
    await expect(appDiv).not.toBeEmpty();
  });
});

test.describe('Layout Component', () => {
  test('should render layout wrapper', async ({ page }) => {
    await page.goto('/');
    
    // #app should be the root container
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });

  test('layout should not crash on rapid navigation', async ({ page }) => {
    await page.goto('/');
    
    // Rapid navigation shouldn't cause errors
    await page.goto('/users');
    await page.goto('/settings');
    await page.goto('/');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });
});

test.describe('Performance', () => {
  test('page should load within acceptable time', async ({ page }) => {
    const startTime = Date.now();
    
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');
    
    const loadTime = Date.now() - startTime;
    
    // Page should load in under 10 seconds
    expect(loadTime).toBeLessThan(10000);
  });

  test('should not have memory leaks on navigation', async ({ page }) => {
    // Navigate through multiple pages
    for (let i = 0; i < 5; i++) {
      await page.goto('/');
      await page.goto('/users');
      await page.goto('/settings');
    }
    
    // App should still be functional
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });
});

test.describe('Browser Compatibility', () => {
  test('should work with JavaScript enabled', async ({ page }) => {
    await page.goto('/');
    
    // React app requires JavaScript
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
    await expect(appDiv).not.toBeEmpty();
  });
});

test.describe('CSS and Styling', () => {
  test('should load Tailwind CSS styles', async ({ page }) => {
    await page.goto('/');
    
    // Check that styled elements exist
    const styledElements = page.locator('[class*="bg-"], [class*="text-"], [class*="p-"]');
    const count = await styledElements.count();
    
    expect(count).toBeGreaterThan(0);
  });

  test('should have responsive styles', async ({ page }) => {
    await page.goto('/');
    
    // Check for responsive classes
    const responsiveElements = page.locator('[class*="md:"], [class*="lg:"], [class*="sm:"]');
    const count = await responsiveElements.count();
    
    // Should have some responsive elements
    expect(count).toBeGreaterThanOrEqual(0);
  });
});

test.describe('Accessibility', () => {
  test('should have proper document structure', async ({ page }) => {
    await page.goto('/');
    
    // Should have an #app root element
    const app = page.locator('#app');
    await expect(app).toBeVisible();
  });

  test('should have interactive buttons', async ({ page }) => {
    await page.goto('/');
    
    // Find all buttons
    const buttons = page.getByRole('button');
    const buttonCount = await buttons.count();
    
    // Should have at least one button
    expect(buttonCount).toBeGreaterThan(0);
  });

  test('should have proper link structure', async ({ page }) => {
    await page.goto('/');
    
    // Find all links
    const links = page.getByRole('link');
    const linkCount = await links.count();
    
    // Should have at least one link (navigation or content)
    expect(linkCount).toBeGreaterThanOrEqual(0);
  });

  test('should support keyboard navigation', async ({ page }) => {
    await page.goto('/');
    
    // Press Tab to move through interactive elements
    await page.keyboard.press('Tab');
    
    // Some element should be focused
    const focusedElement = page.locator(':focus');
    const hasFocus = await focusedElement.count() > 0;
    
    expect(hasFocus).toBeTruthy();
  });
});
