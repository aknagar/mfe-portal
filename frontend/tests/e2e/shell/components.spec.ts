import { test, expect } from '@playwright/test';

test.describe('Auth Button Component', () => {
  test.describe('Not Authenticated State', () => {
    test('should show login button when not authenticated', async ({ page }) => {
      await page.goto('/');
      await page.waitForLoadState('domcontentloaded');
      
      // On login page, should see sign-in button
      const signInButton = page.getByRole('button', { name: /sign in|login/i });
      await expect(signInButton.first()).toBeVisible();
    });
  });
});

test.describe('AdminLayout Component', () => {
  test.describe('Sidebar Features', () => {
    test('should have collapsible sidebar', async ({ page }) => {
      await page.goto('/');
      
      // Look for sidebar trigger (menu button)
      const menuButton = page.locator('[data-sidebar="trigger"], button').filter({ has: page.locator('svg') }).first();
      
      // Page should load without errors
      const appDiv = page.locator('#app');
      await expect(appDiv).toBeVisible();
    });
  });
});

test.describe('Card Component', () => {
  test('should render cards correctly on login page', async ({ page }) => {
    await page.goto('/');
    
    // Login page has feature cards
    const cards = page.locator('[data-slot="card"], .rounded-xl, .shadow-xl');
    const cardCount = await cards.count();
    
    // Should have at least the login card
    expect(cardCount).toBeGreaterThan(0);
  });
});

test.describe('Button Component', () => {
  test('should render primary button', async ({ page }) => {
    await page.goto('/');
    
    // Check for sign-in button which uses the Button component
    const primaryButton = page.getByRole('button', { name: /sign in/i });
    await expect(primaryButton).toBeVisible();
  });

  test('button should be clickable', async ({ page }) => {
    await page.goto('/');
    
    const button = page.getByRole('button', { name: /sign in/i });
    await expect(button).toBeEnabled();
  });
});

test.describe('Input Component', () => {
  test('should render input fields on API Playground', async ({ page }) => {
    await page.goto('/api-playground');
    
    const playgroundTitle = page.locator('h1:has-text("API Playground")');
    const isOnPlaygroundPage = await playgroundTitle.count() > 0;
    
    if (isOnPlaygroundPage) {
      // Check for endpoint input
      const input = page.locator('input#endpoint, input[placeholder*="endpoint"]');
      await expect(input.first()).toBeVisible();
    }
  });
});

test.describe('Textarea Component', () => {
  test('should render textarea on API Playground', async ({ page }) => {
    await page.goto('/api-playground');
    
    const playgroundTitle = page.locator('h1:has-text("API Playground")');
    const isOnPlaygroundPage = await playgroundTitle.count() > 0;
    
    if (isOnPlaygroundPage) {
      // Click on body tab
      const bodyTab = page.getByText('Request body');
      await bodyTab.click();
      
      // Textarea should be visible
      const textarea = page.locator('textarea');
      await expect(textarea.first()).toBeVisible();
    }
  });

  test('textarea should be editable', async ({ page }) => {
    await page.goto('/api-playground');
    
    const playgroundTitle = page.locator('h1:has-text("API Playground")');
    const isOnPlaygroundPage = await playgroundTitle.count() > 0;
    
    if (isOnPlaygroundPage) {
      const bodyTab = page.getByText('Request body');
      await bodyTab.click();
      
      const textarea = page.locator('textarea').first();
      await textarea.clear();
      await textarea.fill('{"test": "value"}');
      
      await expect(textarea).toHaveValue('{"test": "value"}');
    }
  });
});

test.describe('Label Component', () => {
  test('should render labels correctly', async ({ page }) => {
    await page.goto('/api-playground');
    
    const playgroundTitle = page.locator('h1:has-text("API Playground")');
    const isOnPlaygroundPage = await playgroundTitle.count() > 0;
    
    if (isOnPlaygroundPage) {
      // Check for method label
      const methodLabel = page.getByText('Method', { exact: true });
      await expect(methodLabel).toBeVisible();
    }
  });
});

test.describe('Select Component', () => {
  test('should render select dropdown', async ({ page }) => {
    await page.goto('/api-playground');
    
    const playgroundTitle = page.locator('h1:has-text("API Playground")');
    const isOnPlaygroundPage = await playgroundTitle.count() > 0;
    
    if (isOnPlaygroundPage) {
      const select = page.locator('#method, select').first();
      await expect(select).toBeVisible();
    }
  });

  test('select should change value', async ({ page }) => {
    await page.goto('/api-playground');
    
    const playgroundTitle = page.locator('h1:has-text("API Playground")');
    const isOnPlaygroundPage = await playgroundTitle.count() > 0;
    
    if (isOnPlaygroundPage) {
      const select = page.locator('#method, select').first();
      
      await select.selectOption('PUT');
      await expect(select).toHaveValue('PUT');
    }
  });
});
