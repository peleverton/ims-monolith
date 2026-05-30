"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { CachedProductApi } from "@/lib/cached-product-api";
import type { ProductListDto } from "@/lib/types";

// ─── useDebounce Hook ─────────────────────────────────────────────────────────

/**
 * Custom hook: delays value updates by the specified milliseconds.
 * Only fires after user stops typing for `delay` ms.
 */
export function useDebounce<T>(value: T, delay: number = 300): T {
  const [debouncedValue, setDebouncedValue] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedValue(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);

  return debouncedValue;
}

// ─── useProductSearch Hook ────────────────────────────────────────────────────

interface ProductSearchState {
  results: ProductListDto[];
  totalCount: number;
  isLoading: boolean;
  isFromCache: boolean;
  cacheSize: number;
  error: string | null;
}

/**
 * Combines useDebounce + CachedProductApiProxy for autocomplete.
 * Only hits the API after 300ms of inactivity.
 */
export function useProductSearch(searchTerm: string) {
  const [state, setState] = useState<ProductSearchState>({
    results: [],
    totalCount: 0,
    isLoading: false,
    isFromCache: false,
    cacheSize: 0,
    error: null,
  });

  const debouncedTerm = useDebounce(searchTerm, 300);
  const abortRef = useRef<AbortController | null>(null);

  /* eslint-disable react-hooks/set-state-in-effect */
  // Data-fetching effect: setState in async callbacks is the standard pattern
  useEffect(() => {
    const term = debouncedTerm;

    if (!term.trim()) {
      setState((s) => ({
        ...s,
        results: [],
        totalCount: 0,
        isLoading: false,
        error: null,
      }));
      return;
    }

    // Cancel previous in-flight request
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setState((s) => ({ ...s, isLoading: true, error: null }));

    const cacheSizeBefore = CachedProductApi.cacheSize;

    CachedProductApi.search(term, { pageSize: 10 })
      .then((result) => {
        if (controller.signal.aborted) return;
        setState({
          results: result.items,
          totalCount: result.totalCount,
          isLoading: false,
          isFromCache: CachedProductApi.cacheSize === cacheSizeBefore,
          cacheSize: CachedProductApi.cacheSize,
          error: null,
        });
      })
      .catch((err) => {
        if (controller.signal.aborted) return;
        if (err instanceof Error && err.name === "AbortError") return;
        setState((s) => ({
          ...s,
          isLoading: false,
          error: "Erro ao buscar produtos",
        }));
      });

    return () => { controller.abort(); };
  }, [debouncedTerm]);
  /* eslint-enable react-hooks/set-state-in-effect */

  const clearCache = useCallback(() => {
    CachedProductApi.clearCache();
    setState((s) => ({ ...s, cacheSize: 0 }));
  }, []);

  return { ...state, clearCache };
}
