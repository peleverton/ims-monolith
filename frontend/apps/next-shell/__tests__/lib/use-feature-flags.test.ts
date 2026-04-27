import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useFeatureFlags, useFeatureFlag } from "@/lib/hooks/use-feature-flags";
import React from "react";

const mockFlags = {
  EnableKanbanView: true,
  EnableWebhooks: true,
  EnableFullTextSearch: false,
};

function createWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return ({ children }: { children: React.ReactNode }) =>
    React.createElement(QueryClientProvider, { client: qc }, children);
}

describe("useFeatureFlags", () => {
  beforeEach(() => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => mockFlags,
    } as Response);
  });

  it("fetches and returns feature flags", async () => {
    const { result } = renderHook(() => useFeatureFlags(), { wrapper: createWrapper() });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toEqual(mockFlags);
  });

  it("useFeatureFlag returns true for enabled flag", async () => {
    const { result } = renderHook(() => useFeatureFlag("EnableKanbanView"), {
      wrapper: createWrapper(),
    });
    await waitFor(() => expect(result.current).toBe(true));
  });

  it("useFeatureFlag returns false for disabled flag", async () => {
    const { result } = renderHook(() => useFeatureFlag("EnableFullTextSearch"), {
      wrapper: createWrapper(),
    });
    await waitFor(() => expect(result.current).toBe(false));
  });
});
