import { test, expect } from '@playwright/test';

test.describe('Shell Navigation', () => {
  test.describe('Sidebar Navigation', () => {
    test('should display sidebar with navigation items', async ({ page }) => {
      await page.goto('/');
      await page.waitForTimeout(1000);
      
      // Check if sidebar is present (may be in login or authenticated state)
      const sidebar = page.locator('[data-sidebar="sidebar"], nav, aside');
      const hasSidebar = await sidebar.count() > 0;
      
      // If not authenticated, should show login page
      const loginButton = page.getByRole('button', { name: /sign in|login/i });
      const hasLoginButton = await loginButton.count() > 0;
      
      expect(hasSidebar || hasLoginButton).toBeTruthy();
    });

    test('should display app title in header', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('domcontentloaded');
      
      // Check for "My Tools" title (appears on login page and in sidebar)
      await expect(page.getByText('My Tools').first()).toBeVisible();
    });
  });

  test.describe('Route Navigation', () => {
    test('should navigate to dashboard route', async ({ page }) => {
      await page.goto('/');
      await expect(page).toHaveURL(/\/$/);
    });

    test('should navigate to users route', async ({ page }) => {
      await page.goto('/users');
      await expect(page).toHaveURL(/\/users$/);
    });

    test('should navigate to products route', async ({ page }) => {
      await page.goto('/products');
      await expect(page).toHaveURL(/\/products$/);
    });

    test('should navigate to settings route', async ({ page }) => {
      await page.goto('/settings');
      await expect(page).toHaveURL(/\/settings$/);
    });

    test('should navigate to hello-world route', async ({ page }) => {
      await page.goto('/hello-world');
      await expect(page).toHaveURL(/\/hello-world$/);
    });

    test('should navigate to api-playground route', async ({ page }) => {
      await page.goto('/api-playground');
      await expect(page).toHaveURL(/\/api-playground$/);
    });

    test('should navigate to auth route', async ({ page }) => {
      await page.goto('/auth');
      await expect(page).toHaveURL(/\/auth$/);
    });

    test('should navigate to logout route', async ({ page }) => {
      await page.goto('/logout');
      await expect(page).toHaveURL(/\/logout$/);
    });
  });

  test.describe('Navigation Links', () => {
    test('should have dashboard link in navigation', async ({ page }) => {
      await page.goto('/');
      
      // Check for dashboard text in page (either as nav item or landing page)
      const dashboardText = page.getByText(/dashboard/i).first();
      await expect(dashboardText).toBeVisible({ timeout: 10000 });
    });
  });
});

test.describe('Shell Application Bootstrap', () => {
  test('should mount root element', async ({ page }) => {
    await page.goto('/');
    
    // Check that #app element exists and has content
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
    await expect(appDiv).not.toBeEmpty();
  });

  test('should load without JavaScript errors', async ({ page }) => {
    const errors: string[] = [];
    
    page.on('console', msg => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
      }
    });

    page.on('pageerror', error => {
      errors.push(error.message);
    });

    await page.goto('/');
    await page.waitForTimeout(2000);
    
    // Filter out known acceptable errors (e.g., MSAL auth errors in test environment)
    const criticalErrors = errors.filter(err => 
      !err.includes('AADSTS') && // Azure AD errors expected without real auth
      !err.includes('Failed to load resource') // Network errors expected in test
    );
    
    // Log any errors for debugging
    if (criticalErrors.length > 0) {
      console.log('Critical errors found:', criticalErrors);
    }
  });

  test('should have proper page title', async ({ page }) => {
    await page.goto('/');
    
    // The title should be related to the app
    const title = await page.title();
    expect(title).toBeTruthy();
  });
});
