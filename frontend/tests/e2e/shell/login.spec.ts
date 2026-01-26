import { test, expect } from '@playwright/test';

test.describe('Login Page', () => {
  test.beforeEach(async ({ page }) => {
    // Clear any existing auth state
    await page.goto('/');
    await page.evaluate(() => {
      sessionStorage.clear();
      localStorage.clear();
    });
    await page.reload();
  });

  test('should display login page when not authenticated', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');
    
    // Login page should show app name and sign-in button
    await expect(page.getByText('My Tools').first()).toBeVisible();
    await expect(page.getByRole('button', { name: /sign in with microsoft/i })).toBeVisible();
  });

  test('should display welcome message', async ({ page }) => {
    await page.goto('/');
    
    await expect(page.getByText('Welcome!')).toBeVisible();
  });

  test('should display feature cards', async ({ page }) => {
    await page.goto('/');
    
    // Check for feature descriptions
    await expect(page.getByText('Secure Access')).toBeVisible();
    await expect(page.getByText('Fast & Efficient')).toBeVisible();
    await expect(page.getByText('Personalized')).toBeVisible();
  });

  test('should display login description', async ({ page }) => {
    await page.goto('/');
    
    await expect(page.getByText(/sign in with your personal microsoft account/i)).toBeVisible();
  });

  test('should display supported account types', async ({ page }) => {
    await page.goto('/');
    
    await expect(page.getByText(/@outlook.com|@hotmail.com|@live.com/i)).toBeVisible();
  });

  test('should have Microsoft sign-in button', async ({ page }) => {
    await page.goto('/');
    
    const signInButton = page.getByRole('button', { name: /sign in with microsoft/i });
    await expect(signInButton).toBeVisible();
    await expect(signInButton).toBeEnabled();
  });

  test('should display logo icon', async ({ page }) => {
    await page.goto('/');
    
    // The My Tools logo with wrench icon should be visible
    const title = page.getByText('My Tools').first();
    await expect(title).toBeVisible();
  });
});

test.describe('Login Page - Responsive Design', () => {
  test('should be responsive on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');
    
    // Main elements should still be visible
    await expect(page.getByText('My Tools').first()).toBeVisible();
    await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible();
  });

  test('should be responsive on tablet viewport', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto('/');
    
    await expect(page.getByText('My Tools').first()).toBeVisible();
    await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible();
  });

  test('should display feature grid on larger screens', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.goto('/');
    
    // All three feature cards should be visible
    await expect(page.getByText('Secure Access')).toBeVisible();
    await expect(page.getByText('Fast & Efficient')).toBeVisible();
    await expect(page.getByText('Personalized')).toBeVisible();
  });
});

test.describe('Login Page - Accessibility', () => {
  test('should have proper heading structure', async ({ page }) => {
    await page.goto('/');
    
    // Check for h1 heading (My Tools)
    const h1 = page.locator('h1');
    await expect(h1.first()).toBeVisible();
  });

  test('should have interactive elements accessible', async ({ page }) => {
    await page.goto('/');
    
    // Sign in button should be keyboard accessible
    const signInButton = page.getByRole('button', { name: /sign in with microsoft/i });
    await expect(signInButton).toBeVisible();
    
    // Focus the button
    await signInButton.focus();
    await expect(signInButton).toBeFocused();
  });
});
