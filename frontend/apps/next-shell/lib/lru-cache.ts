/**
 * Epic 6: LRU Cache — Least Recently Used cache implementation.
 * Uses a Map (insertion-ordered in JS) for O(1) get/set operations.
 * When capacity is reached, the least recently used entry is evicted.
 */

export class LRUCache<K, V> {
  private cache: Map<K, V>;
  private readonly capacity: number;

  constructor(capacity: number = 50) {
    if (capacity < 1) throw new Error("LRU capacity must be >= 1");
    this.cache = new Map();
    this.capacity = capacity;
  }

  /**
   * Get a value. Moves the entry to "most recently used" position.
   * Returns undefined if not found.
   */
  get(key: K): V | undefined {
    if (!this.cache.has(key)) return undefined;

    // Move to end (most recently used) by re-inserting
    const value = this.cache.get(key)!;
    this.cache.delete(key);
    this.cache.set(key, value);
    return value;
  }

  /**
   * Set a value. Evicts the LRU entry if at capacity.
   */
  set(key: K, value: V): void {
    // If key exists, delete it first (so re-insert moves it to end)
    if (this.cache.has(key)) {
      this.cache.delete(key);
    } else if (this.cache.size >= this.capacity) {
      // Evict the first entry (least recently used)
      const lruKey = this.cache.keys().next().value;
      if (lruKey !== undefined) this.cache.delete(lruKey);
    }

    this.cache.set(key, value);
  }

  /**
   * Check if key exists without affecting LRU order.
   */
  has(key: K): boolean {
    return this.cache.has(key);
  }

  /**
   * Prefix-aware lookup: returns cached value for a key that starts with the given prefix.
   * This enables "Mou" to serve results cached from "Mouse" queries.
   */
  getByPrefix(prefix: string): V | undefined {
    // Check exact match first
    const exact = this.get(prefix as unknown as K);
    if (exact !== undefined) return exact;

    // Check if any cached key starts with this prefix (or prefix starts with cached key)
    for (const [key] of this.cache) {
      const keyStr = String(key);
      if (keyStr.startsWith(prefix) || prefix.startsWith(keyStr)) {
        return this.get(key);
      }
    }

    return undefined;
  }

  /** Current number of entries */
  get size(): number {
    return this.cache.size;
  }

  /** Clear all entries */
  clear(): void {
    this.cache.clear();
  }

  /** Delete a specific key */
  delete(key: K): boolean {
    return this.cache.delete(key);
  }
}
