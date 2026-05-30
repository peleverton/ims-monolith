"use client";

import { useState, useRef, useEffect } from "react";
import { useProductSearch } from "@/lib/hooks/use-product-search";
import type { ProductListDto } from "@/lib/types";

/**
 * Epic 6: Autocomplete Component — product search with LRU cache.
 * Shows cached/network indicator and cache stats.
 */

interface ProductAutocompleteProps {
  onSelect?: (product: ProductListDto) => void;
  placeholder?: string;
}

export function ProductAutocomplete({
  onSelect,
  placeholder = "Buscar produto por nome ou SKU...",
}: ProductAutocompleteProps) {
  const [inputValue, setInputValue] = useState("");
  const [isOpen, setIsOpen] = useState(false);
  const wrapperRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const { results, totalCount, isLoading, isFromCache, cacheSize, clearCache } =
    useProductSearch(inputValue);

  // Close dropdown on outside click
  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (wrapperRef.current && !wrapperRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, []);

  // Open dropdown when results arrive
  const isDropdownVisible = isOpen && results.length > 0 && inputValue.trim().length > 0;

  const handleSelect = (product: ProductListDto) => {
    setInputValue(product.name);
    setIsOpen(false);
    onSelect?.(product);
  };

  return (
    <div ref={wrapperRef} className="relative w-full max-w-lg">
      {/* Input */}
      <div className="relative">
        <input
          ref={inputRef}
          type="text"
          value={inputValue}
          onChange={(e) => { setInputValue(e.target.value); setIsOpen(true); }}
          onFocus={() => setIsOpen(true)}
          placeholder={placeholder}
          className="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-4 py-2.5 pr-20 text-sm text-gray-900 dark:text-gray-100 placeholder-gray-400 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none"
        />

        {/* Status indicators */}
        <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-2">
          {isLoading && (
            <div className="w-4 h-4 border-2 border-blue-500 border-t-transparent rounded-full animate-spin" />
          )}
          {!isLoading && isFromCache && inputValue.trim() && (
            <span className="text-[10px] font-medium bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 px-1.5 py-0.5 rounded">
              CACHE
            </span>
          )}
          {!isLoading && !isFromCache && inputValue.trim() && results.length > 0 && (
            <span className="text-[10px] font-medium bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 px-1.5 py-0.5 rounded">
              API
            </span>
          )}
        </div>
      </div>

      {/* Dropdown */}
      {isDropdownVisible && (
        <div className="absolute z-50 mt-1 w-full rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 shadow-lg max-h-80 overflow-y-auto">
          {results.length > 0 ? (
            <>
              {results.map((product) => (
                <button
                  key={product.id}
                  onClick={() => handleSelect(product)}
                  className="w-full text-left px-4 py-3 hover:bg-gray-50 dark:hover:bg-gray-700/50 border-b border-gray-100 dark:border-gray-700 last:border-0 transition-colors"
                >
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
                        {product.name}
                      </p>
                      <p className="text-xs text-gray-500 dark:text-gray-400">
                        SKU: {product.sku} • {product.category}
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm font-medium text-gray-700 dark:text-gray-300">
                        {product.currentStock} un
                      </p>
                      <span
                        className={`text-xs font-medium px-1.5 py-0.5 rounded ${
                          product.stockStatus === "InStock"
                            ? "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400"
                            : product.stockStatus === "LowStock"
                            ? "bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400"
                            : "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400"
                        }`}
                      >
                        {product.stockStatus}
                      </span>
                    </div>
                  </div>
                </button>
              ))}

              {/* Footer with stats */}
              <div className="px-4 py-2 bg-gray-50 dark:bg-gray-900/50 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between text-xs text-gray-400 dark:text-gray-500">
                <span>{totalCount} resultado(s)</span>
                <span className="flex items-center gap-2">
                  Cache: {cacheSize}/50
                  {cacheSize > 0 && (
                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        clearCache();
                      }}
                      className="text-red-400 hover:text-red-500 underline"
                    >
                      limpar
                    </button>
                  )}
                </span>
              </div>
            </>
          ) : (
            <div className="px-4 py-6 text-center text-sm text-gray-400 dark:text-gray-500">
              Nenhum produto encontrado para &quot;{inputValue}&quot;
            </div>
          )}
        </div>
      )}
    </div>
  );
}
