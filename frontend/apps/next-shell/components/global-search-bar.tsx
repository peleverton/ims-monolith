"use client";

import { useState, useRef, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useSearch, type SearchResultItem } from "@/lib/hooks/use-search";
import { useTranslations } from "next-intl";

export function GlobalSearchBar() {
  const t = useTranslations("search");
  const router = useRouter();
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const { data, isFetching } = useSearch({ q: query });

  // Close on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (!containerRef.current?.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  const handleSelect = useCallback(
    (item: SearchResultItem) => {
      setOpen(false);
      setQuery("");
      const path = item.module === "issues"
        ? `/issues/${item.id}`
        : `/inventory/${item.id}`;
      router.push(path);
    },
    [router]
  );

  return (
    <div ref={containerRef} className="relative w-full max-w-sm">
      <div className="relative">
        <svg
          className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <input
          type="text"
          value={query}
          onChange={(e) => {
            setQuery(e.target.value);
            setOpen(true);
          }}
          onFocus={() => query.length >= 2 && setOpen(true)}
          placeholder={t("placeholder")}
          className="w-full pl-9 pr-4 py-2 text-sm rounded-md border border-input bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
          aria-label={t("placeholder")}
        />
        {isFetching && (
          <div className="absolute right-3 top-1/2 -translate-y-1/2 h-3 w-3 animate-spin rounded-full border border-muted-foreground border-t-transparent" />
        )}
      </div>

      {open && query.length >= 2 && (
        <div className="absolute top-full mt-1 z-50 w-full min-w-[320px] rounded-md border bg-popover shadow-lg overflow-hidden">
          {!data || data.results.length === 0 ? (
            <p className="p-3 text-sm text-muted-foreground">
              {isFetching ? t("searching") : t("noResults")}
            </p>
          ) : (
            <ul role="listbox">
              {data.results.map((item) => (
                <li key={`${item.module}-${item.id}`}>
                  <button
                    onClick={() => handleSelect(item)}
                    className="w-full text-left flex items-start gap-3 px-3 py-2.5 hover:bg-accent transition-colors"
                  >
                    <span className="mt-0.5 text-[10px] font-semibold uppercase tracking-wide px-1.5 py-0.5 rounded bg-muted text-muted-foreground shrink-0">
                      {item.module}
                    </span>
                    <div className="min-w-0">
                      <p className="text-sm font-medium truncate">{item.title}</p>
                      {item.description && (
                        <p className="text-xs text-muted-foreground truncate">{item.description}</p>
                      )}
                    </div>
                  </button>
                </li>
              ))}
              {data.total > data.results.length && (
                <li className="px-3 py-2 text-xs text-muted-foreground border-t">
                  {t("andMore", { count: data.total - data.results.length })}
                </li>
              )}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
