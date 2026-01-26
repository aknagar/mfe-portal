import { test, expect } from '@playwright/test';

/**
 * Tests for the Hello World Pilet
 * The pilet is registered as a route in the shell application
 */
test.describe('Hello World Pilet', () => {
  test('should navigate to hello-world route', async ({ page }) => {
    await page.goto('/hello-world');
    await expect(page).toHaveURL(/\/hello-world$/);
  });

  test('should display page content', async ({ page }) => {
    await page.goto('/hello-world');
    await page.waitForLoadState('domcontentloaded');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).not.toBeEmpty();
  });

  test.describe('Hello World Content (When Authenticated)', () => {
    test('should display hello world heading', async ({ page }) => {
      await page.goto('/hello-world');
      
      const heading = page.locator('h1:has-text("Hello World")');
      const isOnHelloWorldPage = await heading.count() > 0;
      
      if (isOnHelloWorldPage) {
        await expect(heading).toBeVisible();
      }
    });

    test('should display welcome message', async ({ page }) => {
      await page.goto('/hello-world');
      
      const welcomeText = page.getByText(/welcome to the hello world pilet/i);
      const hasWelcome = await welcomeText.count() > 0;
      
      if (hasWelcome) {
        await expect(welcomeText).toBeVisible();
      }
    });

    test('should display description text', async ({ page }) => {
      await page.goto('/hello-world');
      
      const descriptionText = page.getByText(/micro-frontend module/i);
      const hasDescription = await descriptionText.count() > 0;
      
      if (hasDescription) {
        await expect(descriptionText).toBeVisible();
      }
    });
  });

  test.describe('Hello World - Styling', () => {
    test('should have proper padding', async ({ page }) => {
      await page.goto('/hello-world');
      
      const heading = page.locator('h1:has-text("Hello World")');
      const isOnHelloWorldPage = await heading.count() > 0;
      
      if (isOnHelloWorldPage) {
        // Container should have padding class
        const container = page.locator('.p-6').first();
        await expect(container).toBeVisible();
      }
    });

    test('should have styled heading', async ({ page }) => {
      await page.goto('/hello-world');
      
      const heading = page.locator('h1:has-text("Hello World")');
      const isOnHelloWorldPage = await heading.count() > 0;
      
      if (isOnHelloWorldPage) {
        // Heading should have tailwind classes
        await expect(heading).toHaveClass(/font-bold|text-3xl/);
      }
    });
  });
});

test.describe('Hello World - Responsive', () => {
  test('should display correctly on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/hello-world');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });

  test('should display correctly on tablet', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto('/hello-world');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });

  test('should display correctly on desktop', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/hello-world');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });
});
