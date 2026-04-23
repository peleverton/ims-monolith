/**
 * US-058: Unit tests for lib/api-fetch.ts
 * Verifies Authorization header injection, error throwing, 204 handling,
 * and correct URL construction.
 */

import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";

// ── Mock next/headers & lib/auth before importing api-fetch ──────────────

vi.mock("next/headers", () => ({
  cookies: vi.fn().mockResolvedValue({
    get: vi.fn().mockReturnValue({ value: "mock-access-token" }),
  }),
}));

vi.mock("@/lib/auth", () => ({
  getAccessToken: vi.fn().mockResolvedValue("mock-access-token"),
}));

import { apiFetch } from "@/lib/api-fetch";

// ── Helpers ───────────────────────────────────────────────────────────────

function mockFetch(status: number, body: unknown, ok = status < 400) {
  globalThis.fetch = vi.fn().mockResolvedValue({
    ok,
    status,
    statusText: ok ? "OK" : "Error",
    json: () => Promise.resolve(body),
    text: () => Promise.resolve(JSON.stringify(body)),
  } as unknown as Response);
}

afterEach(() => {
  vi.restoreAllMocks();
});

// ── Tests ─────────────────────────────────────────────────────────────────

describe("apiFetch", () => {
  it("sends GET request to correct URL", async () => {
    mockFetch(200, { id: 1 });
    await apiFetch("/api/test");
    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining("/api/test"),
      expect.any(Object)
    );
  });

  it("injects Authorization Bearer header when auth=true (default)", async () => {
    mockFetch(200, {});
    await apiFetch("/api/secure");

    const [, options] = (globalThis.fetch as ReturnType<typeof vi.fn>).mock.calls[0];
    expect(options.headers["Authorization"]).toBe("Bearer mock-access-token");
  });

  it("does NOT inject Authorization header when auth=false", async () => {
    mockFetch(200, {});
    await apiFetch("/api/public", { auth: false });

    const [, options] = (globalThis.fetch as ReturnType<typeof vi.fn>).mock.calls[0];
    expect(options.headers["Authorization"]).toBeUndefined();
  });

  it("sets Content-Type to application/json by default", async () => {
    mockFetch(200, {});
    await apiFetch("/api/data");

    const [, options] = (globalThis.fetch as ReturnType<typeof vi.fn>).mock.calls[0];
    expect(options.headers["Content-Type"]).toBe("application/json");
  });

  it("returns parsed JSON on 200", async () => {
    const payload = { id: "abc", name: "test" };
    mockFetch(200, payload);
    const result = await apiFetch<typeof payload>("/api/data");
    expect(result).toEqual(payload);
  });

  it("returns undefined on 204 No Content", async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 204,
      json: () => Promise.reject(new Error("no body")),
      text: () => Promise.resolve(""),
    } as unknown as Response);

    const result = await apiFetch("/api/delete");
    expect(result).toBeUndefined();
  });

  it("throws on 401 Unauthorized", async () => {
    mockFetch(401, { message: "Unauthorized" }, false);
    await expect(apiFetch("/api/secret")).rejects.toThrow("401");
  });

  it("throws on 404 Not Found", async () => {
    mockFetch(404, { message: "Not Found" }, false);
    await expect(apiFetch("/api/missing")).rejects.toThrow("404");
  });

  it("throws on 500 Server Error", async () => {
    mockFetch(500, "Internal Server Error", false);
    await expect(apiFetch("/api/broken")).rejects.toThrow("500");
  });

  it("passes custom headers alongside defaults", async () => {
    mockFetch(200, {});
    await apiFetch("/api/custom", {
      headers: { "X-Custom-Header": "value" },
    });

    const [, options] = (globalThis.fetch as ReturnType<typeof vi.fn>).mock.calls[0];
    expect(options.headers["X-Custom-Header"]).toBe("value");
    expect(options.headers["Content-Type"]).toBe("application/json");
  });

  it("passes method and body for POST requests", async () => {
    mockFetch(201, { id: "new" });
    await apiFetch("/api/create", {
      method: "POST",
      body: JSON.stringify({ name: "item" }),
    });

    const [, options] = (globalThis.fetch as ReturnType<typeof vi.fn>).mock.calls[0];
    expect(options.method).toBe("POST");
    expect(options.body).toBe(JSON.stringify({ name: "item" }));
  });
});
