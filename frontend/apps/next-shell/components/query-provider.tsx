"use client";

/**
 * QueryProvider — US-068
 *
 * Wraps the app with TanStack React Query's QueryClientProvider.
 * Configured with sensible defaults for an IMS dashboard:
 *  - staleTime: 30s (data considered fresh for 30 seconds)
 *  - gcTime: 5min (cache retained for 5 minutes after unmount)
 *  - retry: 1 (one retry on failure)
 *  - refetchOnWindowFocus: true (refresh stale data when user returns)
 */

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState } from "react";

export function QueryProvider({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30 * 1000,        // 30 seconds
            gcTime: 5 * 60 * 1000,       // 5 minutes
            retry: 1,
            refetchOnWindowFocus: true,
          },
          mutations: {
            retry: 0,
          },
        },
      })
  );

  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}
