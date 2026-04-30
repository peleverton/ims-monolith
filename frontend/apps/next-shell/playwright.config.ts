import { defineConfig, devices } from "@playwright/test";

/**
 * Playwright E2E config — IMS Next.js Shell
 * Docs: https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,

  reporter: [
    ["html", { outputFolder: "playwright-report", open: "never" }],
    ["list"],
  ],

  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3000",
    trace: "on-first-retry",
    screenshot: "only-on-failure",
    video: "on-first-retry",
  },

  projects: [
    // ── Setup: cria o estado de autenticação ─────────────────
    {
      name: "setup",
      testMatch: /.*\.setup\.ts/,
    },

    // ── Chrome (autenticado) ─────────────────────────────────
    {
      name: "chromium",
      use: {
        ...devices["Desktop Chrome"],
        storageState: "e2e/.auth/user.json",
      },
      dependencies: ["setup"],
    },

    // ── Firefox (autenticado) ────────────────────────────────
    {
      name: "firefox",
      use: {
        ...devices["Desktop Firefox"],
        storageState: "e2e/.auth/user.json",
      },
      dependencies: ["setup"],
    },

    // ── Auth flow (sem storageState — testa login/logout) ─────
    {
      name: "auth-flow",
      testMatch: /.*auth\.spec\.ts/,
      use: { ...devices["Desktop Chrome"] },
    },
  ],

  // Inicia o servidor Next.js antes dos testes (em CI usa servidor já rodando)
  webServer: {
    command: "node .next/standalone/server.js",
    url: "http://localhost:3000",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    env: {
      NEXTAUTH_SECRET: process.env.NEXTAUTH_SECRET ?? "e2e-test-secret-min-32-chars-ok",
      NEXTAUTH_URL: "http://localhost:3000",
      NEXT_PUBLIC_API_URL: process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5049",
    },
  },
});
