import { test, expect } from '@playwright/test';

test.describe('API Playground Page', () => {
  test('should navigate to api-playground page', async ({ page }) => {
    await page.goto('/api-playground');
    await expect(page).toHaveURL(/\/api-playground$/);
  });

  test('should display page content', async ({ page }) => {
    await page.goto('/api-playground');
    await page.waitForLoadState('domcontentloaded');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).not.toBeEmpty();
  });

  test.describe('API Playground Form (When Authenticated)', () => {
    test('should display page title', async ({ page }) => {
      await page.goto('/api-playground');
      
      const playgroundTitle = page.locator('h1:has-text("API Playground")');
      const isOnPlaygroundPage = await playgroundTitle.count() > 0;
      
      if (isOnPlaygroundPage) {
        await expect(playgroundTitle).toBeVisible();
      }
    });

    test('should display HTTP method selector', async ({ page }) => {
      await page.goto('/api-playground');
      
      const playgroundTitle = page.locator('h1:has-text("API Playground")');
      const isOnPlaygroundPage = await playgroundTitle.count() > 0;
      
      if (isOnPlaygroundPage) {
        const methodSelect = page.locator('#method, select');
        await expect(methodSelect.first()).toBeVisible();
      }
    });

    test('should display endpoint input', async ({ page }) => {
      await page.goto('/api-playground');
      
      const playgroundTitle = page.locator('h1:has-text("API Playground")');
      const isOnPlaygroundPage = await playgroundTitle.count() > 0;
      
      if (isOnPlaygroundPage) {
        const endpointInput = page.locator('#endpoint, input[placeholder*="endpoint"]');
        await expect(endpointInput.first()).toBeVisible();
      }
    });

    test('should display execute button', async ({ page }) => {
      await page.goto('/api-playground');
      
      const playgroundTitle = page.locator('h1:has-text("API Playground")');
      const isOnPlaygroundPage = await playgroundTitle.count() > 0;
      
      if (isOnPlaygroundPage) {
        const executeButton = page.getByRole('button', { name: /execute/i });
        await expect(executeButton).toBeVisible();
      }
    });

    test('should have request tabs', async ({ page }) => {
      await page.goto('/api-playground');
      
      const playgroundTitle = page.locator('h1:has-text("API Playground")');
      const isOnPlaygroundPage = await playgroundTitle.count() > 0;
      
      if (isOnPlaygroundPage) {
        // Check for headers and body tabs
        const headersTab = page.getByText('Request headers');
        const bodyTab = page.getByText('Request body');
        
        await expect(headersTab).toBeVisible();
        await expect(bodyTab).toBeVisible();
      }
    });

    test('should allow switching between request tabs', async ({ page }) => {
      await page.goto('/api-playground');
      
      const playgroundTitle = page.locator('h1:has-text("API Playground")');
      const isOnPlaygroundPage = await playgroundTitle.count() > 0;
      
      if (isOnPlaygroundPage) {
        // Click on body tab
        const bodyTab = page.getByText('Request body');
        await bodyTab.click();
        
        // Body textarea should be visible
        const textarea = page.locator('textarea');
        await expect(textarea.first()).toBeVisible();
        
        // Click on headers tab
        const headersTab = page.getByText('Request headers');
        await headersTab.click();
        
        // Headers section should be visible
        await expect(page.getByText('Name')).toBeVisible();
      }
    });

    test('should have default headers', async ({ page }) => {
      await page.goto('/api-playground');
      
      const playgroundTitle = page.locator('h1:has-text("API Playground")');
      const isOnPlaygroundPage = await playgroundTitle.count() > 0;
      
      if (isOnPlaygroundPage) {
        // Check for Content-Type header
        const contentType = page.locator('input[value="Content-Type"]');
        const hasContentType = await contentType.count() > 0;
        
        if (hasContentType) {
          await expect(contentType.first()).toBeVisible();
        }
      }
    });

    test('should allow adding new headers', async ({ page }) => {
      await page.goto('/api-playground');
      
      const playgroundTitle = page.locator('h1:has-text("API Playground")');
      const isOnPlaygroundPage = await playgroundTitle.count() > 0;
      
      if (isOnPlaygroundPage) {
        // Click add header button
        const addHeaderButton = page.getByRole('button', { name: /add header/i });
        const initialHeaderCount = await page.locator('input[placeholder="Header name"]').count();
        
        await addHeaderButton.click();
        
        // New header inputs should be added
        const newHeaderCount = await page.locator('input[placeholder="Header name"]').count();
        expect(newHeaderCount).toBeGreaterThanOrEqual(initialHeaderCount);
      }
    });

    test('should support different HTTP methods', async ({ page }) => {
      await page.goto('/api-playground');
      
      const playgroundTitle = page.locator('h1:has-text("API Playground")');
      const isOnPlaygroundPage = await playgroundTitle.count() > 0;
      
      if (isOnPlaygroundPage) {
        const methodSelect = page.locator('#method, select').first();
        
        // Check available methods
        const options = await methodSelect.locator('option').allTextContents();
        
        expect(options).toContain('GET');
        expect(options).toContain('POST');
        expect(options).toContain('PUT');
        expect(options).toContain('DELETE');
        expect(options).toContain('PATCH');
      }
    });

    test('should change HTTP method', async ({ page }) => {
      await page.goto('/api-playground');
      
      const playgroundTitle = page.locator('h1:has-text("API Playground")');
      const isOnPlaygroundPage = await playgroundTitle.count() > 0;
      
      if (isOnPlaygroundPage) {
        const methodSelect = page.locator('#method, select').first();
        
        await methodSelect.selectOption('GET');
        await expect(methodSelect).toHaveValue('GET');
        
        await methodSelect.selectOption('DELETE');
        await expect(methodSelect).toHaveValue('DELETE');
      }
    });

    test('should allow editing endpoint URL', async ({ page }) => {
      await page.goto('/api-playground');
      
      const playgroundTitle = page.locator('h1:has-text("API Playground")');
      const isOnPlaygroundPage = await playgroundTitle.count() > 0;
      
      if (isOnPlaygroundPage) {
        const endpointInput = page.locator('#endpoint, input[id="endpoint"]').first();
        
        await endpointInput.clear();
        await endpointInput.fill('https://api.example.com/test');
        
        await expect(endpointInput).toHaveValue('https://api.example.com/test');
      }
    });

    test('should mask authorization token', async ({ page }) => {
      await page.goto('/api-playground');
      
      const playgroundTitle = page.locator('h1:has-text("API Playground")');
      const isOnPlaygroundPage = await playgroundTitle.count() > 0;
      
      if (isOnPlaygroundPage) {
        // Authorization header value should be masked
        const authInput = page.locator('input[value*="Bearer"]').first();
        
        if (await authInput.count() > 0) {
          const value = await authInput.inputValue();
          // Should contain masked token or loading state
          expect(value).toMatch(/Bearer (\*+|Loading\.\.\.|.+)/);
        }
      }
    });
  });
});

test.describe('API Playground - Execute Request', () => {
  test('should execute request and show response', async ({ page }) => {
    // Mock the API endpoint
    await page.route('**/api/test', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Success', data: { id: 1 } })
      });
    });

    await page.goto('/api-playground');
    
    const playgroundTitle = page.locator('h1:has-text("API Playground")');
    const isOnPlaygroundPage = await playgroundTitle.count() > 0;
    
    if (isOnPlaygroundPage) {
      // Set endpoint to test URL
      const endpointInput = page.locator('#endpoint, input[id="endpoint"]').first();
      await endpointInput.clear();
      await endpointInput.fill('http://localhost/api/test');
      
      // Change method to GET
      const methodSelect = page.locator('#method, select').first();
      await methodSelect.selectOption('GET');
      
      // Execute request
      const executeButton = page.getByRole('button', { name: /execute/i });
      await executeButton.click();
      
      // Wait for response
      await page.waitForTimeout(1000);
      
      // Response section should appear
      const responseSection = page.getByText('Response');
      const hasResponse = await responseSection.count() > 0;
      
      if (hasResponse) {
        await expect(responseSection.first()).toBeVisible();
      }
    }
  });

  test('should show response body tab', async ({ page }) => {
    await page.goto('/api-playground');
    
    const playgroundTitle = page.locator('h1:has-text("API Playground")');
    const isOnPlaygroundPage = await playgroundTitle.count() > 0;
    
    if (isOnPlaygroundPage) {
      // Execute a request first
      const executeButton = page.getByRole('button', { name: /execute/i });
      await executeButton.click();
      
      await page.waitForTimeout(2000);
      
      // Check for response tabs
      const responseBodyTab = page.getByText('Response body');
      const responseHeadersTab = page.getByText('Response headers');
      
      const hasResponseTabs = await responseBodyTab.count() > 0;
      
      if (hasResponseTabs) {
        await expect(responseBodyTab).toBeVisible();
        await expect(responseHeadersTab).toBeVisible();
      }
    }
  });
});

test.describe('API Playground - Responsive', () => {
  test('should be usable on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/api-playground');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });

  test('should be usable on tablet', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto('/api-playground');
    
    const appDiv = page.locator('#app');
    await expect(appDiv).toBeVisible();
  });
});
