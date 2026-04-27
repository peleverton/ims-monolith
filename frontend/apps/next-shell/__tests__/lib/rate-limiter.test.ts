import { describe, it, expect, beforeEach } from "vitest";
import { checkRateLimit, RATE_LIMIT_CONFIGS } from "@/lib/rate-limiter";

describe("checkRateLimit", () => {
  const config = { limit: 3, windowMs: 60_000 };

  it("allows requests within the limit", () => {
    const key = `test:${Date.now()}:allow`;
    const r1 = checkRateLimit(key, config);
    const r2 = checkRateLimit(key, config);
    const r3 = checkRateLimit(key, config);

    expect(r1.allowed).toBe(true);
    expect(r1.remaining).toBe(2);
    expect(r2.allowed).toBe(true);
    expect(r2.remaining).toBe(1);
    expect(r3.allowed).toBe(true);
    expect(r3.remaining).toBe(0);
  });

  it("blocks requests over the limit", () => {
    const key = `test:${Date.now()}:block`;
    checkRateLimit(key, config);
    checkRateLimit(key, config);
    checkRateLimit(key, config);
    const r4 = checkRateLimit(key, config);

    expect(r4.allowed).toBe(false);
    expect(r4.remaining).toBe(0);
  });

  it("returns correct limit value", () => {
    const key = `test:${Date.now()}:limit`;
    const result = checkRateLimit(key, config);
    expect(result.limit).toBe(config.limit);
  });

  it("resets window after windowMs expires", () => {
    const expiredConfig = { limit: 1, windowMs: -1 }; // immediate expiry
    const key = `test:${Date.now()}:reset`;
    checkRateLimit(key, expiredConfig); // fills the window (already expired)
    const result = checkRateLimit(key, expiredConfig); // should start new window
    expect(result.allowed).toBe(true);
    expect(result.remaining).toBe(expiredConfig.limit - 1);
  });

  it("RATE_LIMIT_CONFIGS has correct auth limit", () => {
    expect(RATE_LIMIT_CONFIGS.auth.limit).toBe(10);
    expect(RATE_LIMIT_CONFIGS.public.limit).toBe(100);
    expect(RATE_LIMIT_CONFIGS.user.limit).toBe(500);
  });

  it("different keys are independent", () => {
    const key1 = `test:${Date.now()}:keyA`;
    const key2 = `test:${Date.now()}:keyB`;
    const strictConfig = { limit: 1, windowMs: 60_000 };

    checkRateLimit(key1, strictConfig); // exhausts key1
    const r = checkRateLimit(key2, strictConfig); // key2 should be fresh
    expect(r.allowed).toBe(true);
  });
});
