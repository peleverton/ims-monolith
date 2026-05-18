/**
 * push-store.ts
 * In-memory store for Web Push subscriptions.
 * In production, replace with a persistent database.
 */
export interface PushSubscriptionData {
  endpoint: string;
  keys: { auth: string; p256dh: string };
}

// Module-level singleton — survives hot reload in dev via globalThis
const g = globalThis as typeof globalThis & { __pushSubscriptions?: Map<string, PushSubscriptionData> };

if (!g.__pushSubscriptions) {
  g.__pushSubscriptions = new Map<string, PushSubscriptionData>();
}

export const pushSubscriptions: Map<string, PushSubscriptionData> = g.__pushSubscriptions;
