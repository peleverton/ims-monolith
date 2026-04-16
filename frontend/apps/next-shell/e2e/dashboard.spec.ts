import { test, expect } from "@playwright/test";

/**
 * E2E — Dashboard pages (Inventory e Analytics)
 * Verifica que as páginas carregam corretamente.
 * Os custom elements Blazor exibem skeleton enquanto o WASM carrega —
 * o teste valida que o skeleton ou o componente completo está visível.
 */

test.describe("Inventory Page", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/inventory");
  });

  test("carrega a página de inventário", async ({ page }) => {
    await expect(page).toHaveURL(/\/inventory/);
    await expect(
      page.getByRole("heading", { name: /inventário|inventory/i })
    ).toBeVisible();
  });

  test("exibe skeleton ou grid do Blazor", async ({ page }) => {
    // O skeleton aparece enquanto o Blazor WASM carrega
    // ou o grid completo se o WASM já estiver cacheado
    const content = page
      .getByText(/carregando componente blazor|inventário/i)
      .or(page.locator("inventory-grid, [class*=skeleton], table"));

    await expect(content.first()).toBeVisible({ timeout: 15_000 });
  });

  test("sidebar exibe link ativo para inventory", async ({ page }) => {
    const nav = page.locator("nav, aside").first();
    const activeLink = nav.getByRole("link", { name: /inventário|inventory/i });
    await expect(activeLink).toBeVisible();
  });
});

test.describe("Analytics Page", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/analytics");
  });

  test("carrega a página de analytics", async ({ page }) => {
    await expect(page).toHaveURL(/\/analytics/);
    await expect(
      page.getByRole("heading", { name: /analytics/i })
    ).toBeVisible();
  });

  test("exibe skeleton ou dashboard do Blazor", async ({ page }) => {
    const content = page
      .getByText(/carregando componente blazor|kpi|analytics/i)
      .or(page.locator("analytics-dashboard, [class*=skeleton]"));

    await expect(content.first()).toBeVisible({ timeout: 15_000 });
  });
});

test.describe("Navegação entre páginas", () => {
  test("navega entre todas as seções via sidebar", async ({ page }) => {
    await page.goto("/issues");

    const nav = page.locator("nav, aside").first();

    // Issues → Inventory
    await nav.getByRole("link", { name: /inventário|inventory/i }).click();
    await expect(page).toHaveURL(/\/inventory/);

    // Inventory → Analytics
    await nav.getByRole("link", { name: /analytics/i }).click();
    await expect(page).toHaveURL(/\/analytics/);

    // Analytics → Issues
    await nav.getByRole("link", { name: /issues/i }).click();
    await expect(page).toHaveURL(/\/issues/);
  });

  test("título da página muda a cada navegação", async ({ page }) => {
    await page.goto("/issues");
    await expect(page).toHaveTitle(/issues/i);

    await page.goto("/inventory");
    await expect(page).toHaveTitle(/inventário|inventory/i);

    await page.goto("/analytics");
    await expect(page).toHaveTitle(/analytics/i);
  });
});
