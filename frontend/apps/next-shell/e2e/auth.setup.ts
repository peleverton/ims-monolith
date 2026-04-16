import { test as setup, expect } from "@playwright/test";
import path from "path";

/**
 * Setup de autenticação — executado uma vez antes dos testes que precisam de sessão.
 * Salva o estado de autenticação em e2e/.auth/user.json para reutilização.
 */

const AUTH_FILE = path.join(__dirname, ".auth/user.json");

setup("autenticar usuário de teste", async ({ page }) => {
  await page.goto("/login");

  // Preencher credenciais do usuário de teste
  await page.getByLabel(/email/i).fill(
    process.env.E2E_USER_EMAIL ?? "admin@ims.local"
  );
  await page.getByLabel(/senha|password/i).fill(
    process.env.E2E_USER_PASSWORD ?? "Admin@123"
  );

  await page.getByRole("button", { name: /entrar|login/i }).click();

  // Aguardar redirect para o dashboard
  await expect(page).toHaveURL(/\/(issues|inventory|analytics)/);

  // Salvar estado de autenticação
  await page.context().storageState({ path: AUTH_FILE });
});
