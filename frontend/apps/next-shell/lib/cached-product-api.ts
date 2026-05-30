/**
 * Epic 6: Proxy/Decorator Pattern — wraps the product API with LRU caching.
 * The component doesn't know if data comes from cache or network.
 */

import { LRUCache } from "@/lib/lru-cache";
import { getProducts, type GetProductsParams } from "@/lib/api/inventory";
import type { ProductListDto, PagedResult } from "@/lib/types";

const CACHE_CAPACITY = 50;

// Singleton LRU cache instance for product search results
const searchCache = new LRUCache<string, PagedResult<ProductListDto>>(CACHE_CAPACITY);

/**
 * CachedProductApiProxy — Proxy Pattern.
 * Intercepts search calls and serves from LRU cache when possible.
 */
export const CachedProductApi = {
  /**
   * Search products with LRU caching.
   * Cache key is the search term (lowercase, trimmed).
   */
  async search(
    term: string,
    params: Omit<GetProductsParams, "search"> = {}
  ): Promise<PagedResult<ProductListDto>> {
    const normalizedTerm = term.toLowerCase().trim();

    if (!normalizedTerm) {
      return getProducts(params);
    }

    // Check exact cache hit
    const cached = searchCache.get(normalizedTerm);
    if (cached) {
      return cached;
    }

    // Check prefix-aware cache: "Mou" might be served by cached "Mouse" results
    const prefixHit = searchCache.getByPrefix(normalizedTerm);
    if (prefixHit) {
      // Filter cached results client-side for the more specific prefix
      const filtered = {
        ...prefixHit,
        items: prefixHit.items.filter(
          (item) =>
            item.name.toLowerCase().includes(normalizedTerm) ||
            item.sku.toLowerCase().includes(normalizedTerm)
        ),
        totalCount: prefixHit.items.filter(
          (item) =>
            item.name.toLowerCase().includes(normalizedTerm) ||
            item.sku.toLowerCase().includes(normalizedTerm)
        ).length,
      };

      // Only use prefix cache if it still has results
      if (filtered.items.length > 0) {
        return filtered;
      }
    }

    // Cache miss — fetch from API
    const result = await getProducts({ ...params, search: normalizedTerm });

    // Store in LRU cache
    searchCache.set(normalizedTerm, result);

    return result;
  },

  /** Clear the search cache */
  clearCache(): void {
    searchCache.clear();
  },

  /** Current cache size */
  get cacheSize(): number {
    return searchCache.size;
  },
};
