import { Page, Locator, expect } from '@playwright/test';

/**
 * Navigation helper functions for E2E tests
 */
export class NavigationHelpers {
  constructor(private page: Page) {}

  /**
   * Navigate to a page via sidebar link
   */
  async navigateViaLink(linkText: string) {
    await this.page.getByRole('link', { name: linkText }).click();
    await this.page.waitForLoadState('networkidle');
  }

  /**
   * Get the sidebar navigation menu
   */
  getSidebar(): Locator {
    return this.page.locator('[data-sidebar="sidebar"]');
  }

  /**
   * Get a navigation link by name
   */
  getNavLink(name: string): Locator {
    return this.page.getByRole('link', { name });
  }

  /**
   * Verify current page URL
   */
  async expectPath(path: string) {
    await expect(this.page).toHaveURL(new RegExp(`${path}$`));
  }
}

/**
 * Card component helper functions
 */
export class CardHelpers {
  constructor(private page: Page) {}

  /**
   * Get all cards on the page
   */
  getCards(): Locator {
    return this.page.locator('[data-slot="card"], .rounded-xl.border');
  }

  /**
   * Get a card by its title
   */
  getCardByTitle(title: string): Locator {
    return this.page.locator(`text=${title}`).locator('xpath=ancestor::*[contains(@class, "rounded")]').first();
  }
}

/**
 * Form helper functions
 */
export class FormHelpers {
  constructor(private page: Page) {}

  /**
   * Fill an input by label
   */
  async fillByLabel(label: string, value: string) {
    await this.page.getByLabel(label).fill(value);
  }

  /**
   * Select an option from a dropdown
   */
  async selectOption(selector: string, value: string) {
    await this.page.locator(selector).selectOption(value);
  }

  /**
   * Click a button by text
   */
  async clickButton(text: string) {
    await this.page.getByRole('button', { name: text }).click();
  }
}

/**
 * Wait helpers for asynchronous operations
 */
export class WaitHelpers {
  constructor(private page: Page) {}

  /**
   * Wait for loading to complete
   */
  async waitForLoading() {
    // Wait for any loading indicators to disappear
    const loadingIndicators = this.page.locator('text=Loading');
    if (await loadingIndicators.count() > 0) {
      await loadingIndicators.first().waitFor({ state: 'hidden', timeout: 10000 });
    }
  }

  /**
   * Wait for text to appear on page
   */
  async waitForText(text: string, timeout = 5000) {
    await this.page.getByText(text).waitFor({ timeout });
  }

  /**
   * Wait for network requests to complete
   */
  async waitForNetworkIdle() {
    await this.page.waitForLoadState('networkidle');
  }
}

/**
 * Create all helper instances for a page
 */
export function createHelpers(page: Page) {
  return {
    navigation: new NavigationHelpers(page),
    cards: new CardHelpers(page),
    forms: new FormHelpers(page),
    wait: new WaitHelpers(page)
  };
}
