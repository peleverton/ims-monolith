import { test, expect } from "@playwright/test";

/**
 * E2E — Fluxos de Autenticação
 * Testa login, logout e registro sem storageState (sem sessão prévia).
 * Projeto: auth-flow (ver playwright.config.ts)
 */

test.describe("Login", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/login");
  });

  test("exibe o formulário de login", async ({ page }) => {
    await expect(page.getByRole("heading", { name: /entrar|login/i })).toBeVisible();
    await expect(page.getByLabel(/email/i)).toBeVisible();
    await expect(page.getByLabel(/senha|password/i)).toBeVisible();
    await expect(page.getByRole("button", { name: /entrar|login/i })).toBeVisible();
  });

  test("redireciona para /login ao tentar acessar rota protegida", async ({ page }) => {
    await page.goto("/issues");
    await expect(page).toHaveURL(/\/login/);
  });

  test("exibe erro para credenciais inválidas", async ({ page }) => {
    await page.getByLabel(/email/i).fill("invalido@test.com");
    await page.getByLabel(/senha|password/i).fill("senhaerrada");
    await page.getByRole("button", { name: /entrar|login/i }).click();

    await expect(
      page.getByText(/credenciais inválidas|invalid|unauthorized|erro/i)
    ).toBeVisible({ timeout: 5_000 });

    // Deve permanecer na página de login
    await expect(page).toHaveURL(/\/login/);
  });

  test("realiza login com sucesso e redireciona para o dashboard", async ({ page }) => {
    await page.getByLabel(/email/i).fill(
      process.env.E2E_USER_EMAIL ?? "admin@ims.local"
    );
    await page.getByLabel(/senha|password/i).fill(
      process.env.E2E_USER_PASSWORD ?? "Admin@123"
    );
    await page.getByRole("button", { name: /entrar|login/i }).click();

    // Deve redirecionar para o dashboard
    await expect(page).toHaveURL(/\/(issues|inventory|analytics)/, {
      timeout: 10_000,
    });

    // Sidebar deve estar visível
    await expect(page.locator("nav, aside")).toBeVisible();
  });
});

test.describe("Logout", () => {
  test("realiza logout e redireciona para /login", async ({ page }) => {
    // Primeiro faz login
    await page.goto("/login");
    await page.getByLabel(/email/i).fill(
      process.env.E2E_USER_EMAIL ?? "admin@ims.local"
    );
    await page.getByLabel(/senha|password/i).fill(
      process.env.E2E_USER_PASSWORD ?? "Admin@123"
    );
    await page.getByRole("button", { name: /entrar|login/i }).click();
    await expect(page).toHaveURL(/\/(issues|inventory|analytics)/, { timeout: 10_000 });

    // Clica no botão de logout
    const logoutBtn = page.getByRole("button", { name: /sair|logout/i });
    if (await logoutBtn.isVisible()) {
      await logoutBtn.click();
    } else {
      // Fallback: rota de logout direta
      await page.goto("/api/auth/logout");
    }

    await expect(page).toHaveURL(/\/login/, { timeout: 5_000 });
  });
});

test.describe("Registro", () => {
  test("exibe o formulário de registro", async ({ page }) => {
    await page.goto("/register");
    await expect(page.getByLabel(/nome|name/i)).toBeVisible();
    await expect(page.getByLabel(/email/i)).toBeVisible();
    await expect(page.getByLabel(/senha|password/i)).toBeVisible();
  });

  test("exibe erro para email já cadastrado", async ({ page }) => {
    await page.goto("/register");
    await page.getByLabel(/nome|name/i).fill("Teste");
    await page.getByLabel(/email/i).fill(
      process.env.E2E_USER_EMAIL ?? "admin@ims.local"
    );
    await page.getByLabel(/senha|password/i).first().fill("Admin@123");

    const confirmField = page.getByLabel(/confirmar|confirm/i);
    if (await confirmField.isVisible()) {
      await confirmField.fill("Admin@123");
    }

    await page.getByRole("button", { name: /registrar|criar|cadastrar/i }).click();

    await expect(
      page.getByText(/já cadastrado|already exists|conflict|erro/i)
    ).toBeVisible({ timeout: 5_000 });
  });
});
