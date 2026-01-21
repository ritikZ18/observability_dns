import { test, expect } from '@playwright/test';

test.describe('Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
  });

  test('should load dashboard', async ({ page }) => {
    await expect(page).toHaveTitle(/DNS & TLS Observatory/);
    await expect(page.locator('h1')).toContainText('DNS & TLS Observatory');
  });

  test('should display domain form', async ({ page }) => {
    await expect(page.locator('input[placeholder*="Domain"]')).toBeVisible();
    await expect(page.locator('button:has-text("ADD DOMAIN")')).toBeVisible();
  });

  test('should add a domain', async ({ page }) => {
    const domainInput = page.locator('input[placeholder*="Domain"]');
    await domainInput.fill('example.com');
    await page.locator('button:has-text("ADD DOMAIN")').click();
    
    // Wait for domain to appear in table
    await expect(page.locator('text=example.com')).toBeVisible({ timeout: 10000 });
  });

  test('should display observability table', async ({ page }) => {
    await expect(page.locator('table')).toBeVisible();
  });
});
