import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-fetch";

export interface SearchResultItem {
  module: string;
  type: string;
  id: string;
  title: string;
  description?: string;
  score: number;
}

export interface SearchResponse {
  results: SearchResultItem[];
  total: number;
}

interface SearchParams {
  q: string;
  modules?: string[];
  page?: number;
  pageSize?: number;
}

export function useSearch(params: SearchParams) {
  const { q, modules = [], page = 1, pageSize = 20 } = params;
  const modulesParam = modules.join(",");

  return useQuery<SearchResponse>({
    queryKey: ["search", q, modulesParam, page, pageSize],
    queryFn: async () => {
      const url = new URL("/api/proxy/search", window.location.origin);
      url.searchParams.set("q", q);
      if (modulesParam) url.searchParams.set("modules", modulesParam);
      url.searchParams.set("page", String(page));
      url.searchParams.set("pageSize", String(pageSize));
      return apiFetch<SearchResponse>(url.pathname + url.search);
    },
    enabled: q.trim().length >= 2,
    staleTime: 30_000,
    placeholderData: (prev) => prev,
  });
}
