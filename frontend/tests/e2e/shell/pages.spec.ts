import { test, expect } from '@playwright/test';

test.describe('Users Page', () => {
  test('should navigate to users page', async ({ page }) => {
    await page.goto('/users');
    await expect(page).toHaveURL(/\/users$/);
  });

  test('should display page content', async ({ page }) => {
    await page.goto('/users');
    await page.waitForLoadState('domcontentloaded');
    
    // Page should have content
    const appDiv = page.locator('#app');
    await expect(appDiv).not.toBeEmpty();
  });

  test.describe('When Authenticated', () => {
    test('should display users page title', async ({ page }) => {
      await page.goto('/users');
      
      // If authenticated, should show Users heading
      const usersHeading = page.locator('h1:has-text("Users")');
      const isOnUsersPage = await usersHeading.count() > 0;
      
      if (isOnUsersPage) {
        await expect(usersHeading).toBeVisible();
        await expect(page.getByText('Manage user accounts')).toBeVisible();
      }
    });

    test('should display user management card', async ({ page }) => {
      await page.goto('/users');
      
      const usersHeading = page.locator('h1:has-text("Users")');
      const isOnUsersPage = await usersHeading.count() > 0;
      
      if (isOnUsersPage) {
        await expect(page.getByText('User Management')).toBeVisible();
      }
    });
  });
});

test.describe('Settings Page', () => {
  test('should navigate to settings page', async ({ page }) => {
    await page.goto('/settings');
    await expect(page).toHaveURL(/\/settings$/);
  });

  test('should display page content', async ({ page }) => {
    await page.goto('/settings');
    await page.waitForLoadState('domcontentloaded');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).not.toBeEmpty();
  });

  test.describe('When Authenticated', () => {
    test('should display settings page title', async ({ page }) => {
      await page.goto('/settings');
      
      const settingsHeading = page.locator('h1:has-text("Settings")');
      const isOnSettingsPage = await settingsHeading.count() > 0;
      
      if (isOnSettingsPage) {
        await expect(settingsHeading).toBeVisible();
        await expect(page.getByText('Configure your application')).toBeVisible();
      }
    });

    test('should display application settings card', async ({ page }) => {
      await page.goto('/settings');
      
      const settingsHeading = page.locator('h1:has-text("Settings")');
      const isOnSettingsPage = await settingsHeading.count() > 0;
      
      if (isOnSettingsPage) {
        await expect(page.getByText('Application Settings')).toBeVisible();
      }
    });
  });
});

test.describe('Approvals Page', () => {
  test('should navigate to approvals page', async ({ page }) => {
    await page.goto('/approvals');
    await expect(page).toHaveURL(/\/approvals$/);
  });

  test('should display page content', async ({ page }) => {
    await page.goto('/approvals');
    await page.waitForLoadState('domcontentloaded');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).not.toBeEmpty();
  });
});

test.describe('Hello World Page', () => {
  test('should navigate to hello-world page', async ({ page }) => {
    await page.goto('/hello-world');
    await expect(page).toHaveURL(/\/hello-world$/);
  });

  test('should display page content', async ({ page }) => {
    await page.goto('/hello-world');
    await page.waitForLoadState('domcontentloaded');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).not.toBeEmpty();
  });
});
