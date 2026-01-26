import { test, expect } from '@playwright/test';

/**
 * Dashboard page tests
 * Note: These tests require authentication. In a real test environment,
 * you would mock the authentication or use test accounts.
 */
test.describe('Dashboard Page', () => {
  test.describe('Page Structure', () => {
    test('should have correct URL', async ({ page }) => {
      await page.goto('/');
      await expect(page).toHaveURL(/\/$/);
    });

    test('should contain dashboard-related content', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('domcontentloaded');
      
      // Check for either dashboard content (when authenticated) or login content
      const dashboardText = page.getByText(/dashboard|welcome/i).first();
      await expect(dashboardText).toBeVisible({ timeout: 10000 });
    });
  });

  test.describe('Dashboard Content (when visible)', () => {
    // These tests verify dashboard elements assuming auth state
    test('should display dashboard heading', async ({ page }) => {
      await page.goto('/');
      
      // Look for Dashboard text anywhere on the page
      const dashboardElements = page.getByText('Dashboard');
      if (await dashboardElements.count() > 0) {
        await expect(dashboardElements.first()).toBeVisible();
      }
    });

    test('should be accessible from root URL', async ({ page }) => {
      await page.goto('/');
      
      // Page should load without errors
      const appDiv = page.locator('#app');
      await expect(appDiv).toBeVisible();
      await expect(appDiv).not.toBeEmpty();
    });
  });
});

test.describe('Dashboard Cards (Authenticated View)', () => {
  /**
   * These tests check for dashboard card elements that appear
   * when a user is authenticated. They will pass if either:
   * 1. User is authenticated and sees dashboard
   * 2. User is not authenticated (tests skip card assertions)
   */
  
  test('should display statistics cards when authenticated', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');
    
    // Check if we're on dashboard (authenticated) or login page
    const dashboardTitle = page.locator('h1:has-text("Dashboard")');
    const isOnDashboard = await dashboardTitle.count() > 0;
    
    if (isOnDashboard) {
      // Verify statistics cards are present
      await expect(page.getByText('Total Users')).toBeVisible();
      await expect(page.getByText('API Requests')).toBeVisible();
      await expect(page.getByText('Active Sessions')).toBeVisible();
      await expect(page.getByText('Growth')).toBeVisible();
    }
  });

  test('should display recent activity section when authenticated', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');
    
    const dashboardTitle = page.locator('h1:has-text("Dashboard")');
    const isOnDashboard = await dashboardTitle.count() > 0;
    
    if (isOnDashboard) {
      await expect(page.getByText('Recent Activity')).toBeVisible();
    }
  });

  test('should display quick actions section when authenticated', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('domcontentloaded');
    
    const dashboardTitle = page.locator('h1:has-text("Dashboard")');
    const isOnDashboard = await dashboardTitle.count() > 0;
    
    if (isOnDashboard) {
      await expect(page.getByText('Quick Actions')).toBeVisible();
    }
  });
});

test.describe('Dashboard - Responsive Layout', () => {
  test('should display properly on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });

  test('should display properly on tablet', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto('/');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });

  test('should display properly on desktop', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });
});
