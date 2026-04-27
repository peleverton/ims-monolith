/**
 * US-076: In-memory rate limiter for the BFF Next.js middleware.
 *
 * Uses a sliding-window counter stored in-process (Map).
 * For multi-replica deployments, swap the store with Redis via ioredis.
 *
 * Limits:
 *  - Auth endpoints (/api/auth/*):   10 req/min per IP
 *  - Authenticated users:           500 req/min per userId
 *  - All other public requests:     100 req/min per IP
 */

interface WindowEntry {
  count: number;
  resetAt: number; // epoch ms
}

const store = new Map<string, WindowEntry>();

export interface RateLimitConfig {
  limit: number;
  windowMs: number;
}

export interface RateLimitResult {
  allowed: boolean;
  limit: number;
  remaining: number;
  resetAt: number;
}

export function checkRateLimit(key: string, config: RateLimitConfig): RateLimitResult {
  const now = Date.now();
  const entry = store.get(key);

  if (!entry || now >= entry.resetAt) {
    // New window
    store.set(key, { count: 1, resetAt: now + config.windowMs });
    return { allowed: true, limit: config.limit, remaining: config.limit - 1, resetAt: now + config.windowMs };
  }

  if (entry.count >= config.limit) {
    return { allowed: false, limit: config.limit, remaining: 0, resetAt: entry.resetAt };
  }

  entry.count++;
  return { allowed: true, limit: config.limit, remaining: config.limit - entry.count, resetAt: entry.resetAt };
}

// Periodically clean up expired entries (every 5 minutes)
if (typeof setInterval !== "undefined") {
  setInterval(() => {
    const now = Date.now();
    for (const [key, entry] of store.entries()) {
      if (now >= entry.resetAt) store.delete(key);
    }
  }, 5 * 60 * 1000);
}

export const RATE_LIMIT_CONFIGS = {
  auth: { limit: 10, windowMs: 60_000 },      // 10 req/min
  public: { limit: 100, windowMs: 60_000 },   // 100 req/min
  user: { limit: 500, windowMs: 60_000 },     // 500 req/min
} satisfies Record<string, RateLimitConfig>;
