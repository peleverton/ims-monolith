/**
 * US-074: Feature Flags hook
 * Fetches /api/features and caches the result.
 */

import { useQuery } from "@tanstack/react-query";

export type FeatureFlags = {
  EnableKanbanView: boolean;
  EnableWebhooks: boolean;
  EnableFullTextSearch: boolean;
};

async function fetchFeatureFlags(): Promise<FeatureFlags> {
  const res = await fetch("/api/proxy/features");
  if (!res.ok) throw new Error("Failed to fetch feature flags");
  return res.json();
}

export function useFeatureFlags() {
  return useQuery<FeatureFlags>({
    queryKey: ["feature-flags"],
    queryFn: fetchFeatureFlags,
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: false,
  });
}

export function useFeatureFlag(flag: keyof FeatureFlags): boolean {
  const { data } = useFeatureFlags();
  return data?.[flag] ?? false;
}
