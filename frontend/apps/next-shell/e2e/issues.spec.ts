import { test, expect } from "@playwright/test";

/**
 * E2E — Issues Dashboard
 * Requer storageState (usuário autenticado via auth.setup.ts).
 */

test.describe("Issues — Listagem", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/issues");
  });

  test("exibe a página de issues com título correto", async ({ page }) => {
    await expect(page).toHaveURL(/\/issues/);
    await expect(
      page.getByRole("heading", { name: /issues/i })
    ).toBeVisible();
  });

  test("exibe a sidebar de navegação", async ({ page }) => {
    // Sidebar deve estar visível e ter links principais
    const nav = page.locator("nav, aside").first();
    await expect(nav).toBeVisible();
    await expect(nav.getByRole("link", { name: /issues/i })).toBeVisible();
  });

  test("exibe tabela ou lista de issues", async ({ page }) => {
    // Espera a lista ou mensagem de vazio
    const list = page
      .locator("table, [role=list], [role=grid]")
      .or(page.getByText(/nenhuma issue|no issues|vazio|empty/i));
    await expect(list.first()).toBeVisible({ timeout: 10_000 });
  });

  test("botão 'Nova Issue' está visível", async ({ page }) => {
    await expect(
      page.getByRole("link", { name: /nova issue|new issue|criar/i })
        .or(page.getByRole("button", { name: /nova issue|new issue|criar/i }))
    ).toBeVisible();
  });
});

test.describe("Issues — Criação", () => {
  test("navega para a página de nova issue", async ({ page }) => {
    await page.goto("/issues/new");
    await expect(page).toHaveURL(/\/issues\/new/);
    await expect(
      page.getByRole("heading", { name: /nova issue|new issue|criar/i })
    ).toBeVisible();
  });

  test("exibe campos do formulário de nova issue", async ({ page }) => {
    await page.goto("/issues/new");

    await expect(page.getByLabel(/título|title/i)).toBeVisible();
    await expect(page.getByLabel(/descrição|description/i)).toBeVisible();
  });

  test("exibe erro ao submeter formulário vazio", async ({ page }) => {
    await page.goto("/issues/new");

    await page.getByRole("button", { name: /salvar|criar|submit|enviar/i }).click();

    // Deve exibir mensagem de validação
    await expect(
      page.getByText(/obrigatório|required|preencha|inválido/i)
    ).toBeVisible({ timeout: 3_000 });
  });
});

test.describe("Issues — Detalhes", () => {
  test("navega para detalhe de uma issue ao clicar", async ({ page }) => {
    await page.goto("/issues");

    // Clica no primeiro link de issue na tabela (se houver)
    const firstLink = page.getByRole("link").filter({ hasText: /^(?!nova|new|criar).+/i }).first();

    const count = await firstLink.count();
    if (count > 0) {
      await firstLink.click();
      await expect(page).toHaveURL(/\/issues\/.+/);
    } else {
      // Nenhuma issue cadastrada — apenas valida que a página carregou
      await expect(page.getByText(/nenhuma|empty|vazio/i)).toBeVisible();
    }
  });
});
