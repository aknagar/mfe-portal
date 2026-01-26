import { test as base, expect, Page } from '@playwright/test';

/**
 * Authentication fixture for E2E tests.
 * Since MSAL authentication requires interactive login, tests will
 * mock the authentication state or work with the login page flow.
 */
export interface AuthFixture {
  authenticatedPage: Page;
  loginPage: Page;
}

/**
 * Mock MSAL accounts for testing
 */
export const mockAccounts = {
  authenticated: {
    name: 'Test User',
    username: 'testuser@outlook.com',
    localAccountId: 'test-local-id',
    homeAccountId: 'test-home-id',
    environment: 'login.microsoftonline.com',
    tenantId: 'test-tenant-id'
  }
};

/**
 * Test fixture that provides authenticated and non-authenticated page contexts
 */
export const test = base.extend<AuthFixture>({
  authenticatedPage: async ({ page }, use) => {
    // Navigate to the app first
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    
    // Inject mock MSAL state into localStorage/sessionStorage for testing
    // This simulates an authenticated user without actually going through the OAuth flow
    await page.evaluate((accounts) => {
      const mockAccount = {
        homeAccountId: accounts.authenticated.homeAccountId,
        environment: accounts.authenticated.environment,
        tenantId: accounts.authenticated.tenantId,
        username: accounts.authenticated.username,
        localAccountId: accounts.authenticated.localAccountId,
        name: accounts.authenticated.name,
        idTokenClaims: {
          aud: 'test-client-id',
          iss: `https://login.microsoftonline.com/${accounts.authenticated.tenantId}/v2.0`,
          iat: Math.floor(Date.now() / 1000),
          nbf: Math.floor(Date.now() / 1000),
          exp: Math.floor(Date.now() / 1000) + 3600,
          name: accounts.authenticated.name,
          preferred_username: accounts.authenticated.username,
          oid: accounts.authenticated.localAccountId,
          tid: accounts.authenticated.tenantId,
        }
      };

      // Mock the MSAL cache structure
      const accountKey = `${mockAccount.homeAccountId}-${mockAccount.environment}-${mockAccount.tenantId}`;
      const msalCache: Record<string, string> = {};
      msalCache[`${accountKey}-account`] = JSON.stringify(mockAccount);
      
      // Store in sessionStorage (MSAL uses sessionStorage by default)
      for (const [key, value] of Object.entries(msalCache)) {
        sessionStorage.setItem(key, value);
      }
    }, mockAccounts);

    await use(page);
  },
  
  loginPage: async ({ page }, use) => {
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    await use(page);
  }
});

export { expect };
