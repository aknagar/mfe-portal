import { test, expect } from '@playwright/test';

test.describe('Shell Application', () => {
  test('should load the application', async ({ page }) => {
    // Listen to console messages
    const messages: string[] = [];
    page.on('console', msg => messages.push(`${msg.type()}: ${msg.text()}`));
    
    // Navigate to the app
    await page.goto('http://localhost:1234/');
    
    // Wait for the app to load
    await page.waitForTimeout(2000);
    
    // Print console messages
    console.log('Browser Console Messages:');
    messages.forEach(msg => console.log(msg));
    
    // Take a screenshot
    await page.screenshot({ path: 'debug-screenshot.png', fullPage: true });
    
    // Check if the app div exists
    const appDiv = await page.locator('#app');
    await expect(appDiv).toBeVisible();
    
    // Get the page content
    const content = await page.content();
    console.log('\nPage HTML:', content.substring(0, 500));
  });
});
