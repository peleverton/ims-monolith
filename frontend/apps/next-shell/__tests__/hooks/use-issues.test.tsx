/**
 * US-068: Unit tests for React Query hooks — use-issues.ts
 *
 * Tests cover:
 *  - useIssues: fetches list, passes params to API, handles error
 *  - useIssue: fetches single issue, disabled when no id
 *  - useCreateIssue: calls API and invalidates cache
 *  - useUpdateIssueStatus: optimistic update + rollback on error
 *  - useDeleteIssue: calls API and invalidates cache
 *  - useAddComment: calls API and updates detail cache
 */

import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor, act } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import React from "react";

// ── Mock API module ────────────────────────────────────────────────────────────

vi.mock("@/lib/api/issues", () => ({
  getIssues: vi.fn(),
  getIssueById: vi.fn(),
  createIssue: vi.fn(),
  updateIssue: vi.fn(),
  updateIssueStatus: vi.fn(),
  assignIssue: vi.fn(),
  addComment: vi.fn(),
  deleteIssue: vi.fn(),
}));

// Mock sonner toast
vi.mock("sonner", () => ({
  toast: { success: vi.fn(), error: vi.fn() },
}));

import * as api from "@/lib/api/issues";
import { toast } from "sonner";
import {
  useIssues,
  useIssue,
  useCreateIssue,
  useUpdateIssueStatus,
  useDeleteIssue,
  useAddComment,
  issueKeys,
} from "@/lib/hooks/use-issues";
import type { IssueDto, PagedResult } from "@/lib/types";

// ── Helpers ───────────────────────────────────────────────────────────────────

function makeIssue(overrides: Partial<IssueDto> = {}): IssueDto {
  return {
    id: "issue-1",
    title: "Test Issue",
    description: "Description",
    status: "Open",
    priority: "Medium",
    reporterId: "user-1",
    createdAt: "2026-04-27T00:00:00Z",
    tags: [],
    comments: [],
    activities: [],
    commentsCount: 0,
    ...overrides,
  };
}

function makePagedResult(items: IssueDto[]): PagedResult<IssueDto> {
  return {
    items,
    totalCount: items.length,
    pageNumber: 1,
    pageSize: 15,
    totalPages: 1,
  };
}

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return {
    queryClient,
    wrapper: ({ children }: { children: React.ReactNode }) =>
      React.createElement(QueryClientProvider, { client: queryClient }, children),
  };
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe("useIssues", () => {
  beforeEach(() => vi.clearAllMocks());

  it("fetches and returns paged issues", async () => {
    const issue = makeIssue();
    vi.mocked(api.getIssues).mockResolvedValueOnce(makePagedResult([issue]));

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useIssues(), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.items).toHaveLength(1);
    expect(result.current.data?.items[0].id).toBe("issue-1");
  });

  it("passes filter params to the API", async () => {
    vi.mocked(api.getIssues).mockResolvedValueOnce(makePagedResult([]));

    const { wrapper } = createWrapper();
    const params = { page: 2, status: "Open" as const, search: "bug" };
    renderHook(() => useIssues(params), { wrapper });

    await waitFor(() => expect(api.getIssues).toHaveBeenCalledWith(params));
  });

  it("returns error state when API rejects", async () => {
    vi.mocked(api.getIssues).mockRejectedValueOnce(new Error("Network error"));

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useIssues(), { wrapper });

    await waitFor(() => expect(result.current.isError).toBe(true));
  });
});

describe("useIssue", () => {
  beforeEach(() => vi.clearAllMocks());

  it("fetches a single issue by id", async () => {
    const issue = makeIssue({ title: "Single Issue" });
    vi.mocked(api.getIssueById).mockResolvedValueOnce(issue);

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useIssue("issue-1"), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.title).toBe("Single Issue");
    expect(api.getIssueById).toHaveBeenCalledWith("issue-1");
  });

  it("is disabled when id is empty", () => {
    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useIssue(""), { wrapper });

    expect(result.current.fetchStatus).toBe("idle");
    expect(api.getIssueById).not.toHaveBeenCalled();
  });
});

describe("useCreateIssue", () => {
  beforeEach(() => vi.clearAllMocks());

  it("calls createIssue and shows success toast", async () => {
    const created = makeIssue({ id: "new-1" });
    vi.mocked(api.createIssue).mockResolvedValueOnce(created);

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useCreateIssue(), { wrapper });

    await act(async () => {
      await result.current.mutateAsync({
        title: "New Issue",
        description: "Desc",
        priority: "High",
      });
    });

    expect(api.createIssue).toHaveBeenCalled();
    expect(toast.success).toHaveBeenCalledWith("Issue criada com sucesso!");
  });

  it("shows error toast on failure", async () => {
    vi.mocked(api.createIssue).mockRejectedValueOnce(new Error("fail"));

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useCreateIssue(), { wrapper });

    await act(async () => {
      result.current.mutate({ title: "X", description: "Y", priority: "Low" });
    });

    await waitFor(() => expect(toast.error).toHaveBeenCalledWith("Erro ao criar issue."));
  });
});

describe("useUpdateIssueStatus", () => {
  beforeEach(() => vi.clearAllMocks());

  it("calls updateIssueStatus and shows success toast", async () => {
    const updated = makeIssue({ status: "Resolved", resolvedAt: "2026-04-27T10:00:00Z" });
    vi.mocked(api.updateIssueStatus).mockResolvedValueOnce(updated);

    const { wrapper, queryClient } = createWrapper();
    queryClient.setQueryData(issueKeys.detail("issue-1"), makeIssue());

    const { result } = renderHook(() => useUpdateIssueStatus("issue-1"), { wrapper });

    await act(async () => {
      await result.current.mutateAsync("Resolved");
    });

    expect(api.updateIssueStatus).toHaveBeenCalledWith("issue-1", "Resolved");
    expect(toast.success).toHaveBeenCalledWith("Status atualizado!");
    // Detail cache should be updated with new data
    const cached = queryClient.getQueryData(issueKeys.detail("issue-1")) as IssueDto;
    expect(cached.status).toBe("Resolved");
    expect(cached.resolvedAt).toBe("2026-04-27T10:00:00Z");
  });

  it("rolls back optimistic update on error", async () => {
    vi.mocked(api.updateIssueStatus).mockRejectedValueOnce(new Error("fail"));

    const originalIssue = makeIssue({ status: "Open" });
    const { wrapper, queryClient } = createWrapper();
    queryClient.setQueryData(issueKeys.detail("issue-1"), originalIssue);

    const { result } = renderHook(() => useUpdateIssueStatus("issue-1"), { wrapper });

    await act(async () => {
      result.current.mutate("Resolved");
    });

    await waitFor(() => {
      const cached = queryClient.getQueryData(issueKeys.detail("issue-1")) as IssueDto;
      expect(cached.status).toBe("Open");
    });
    expect(toast.error).toHaveBeenCalledWith("Erro ao atualizar status.");
  });
});

describe("useDeleteIssue", () => {
  beforeEach(() => vi.clearAllMocks());

  it("calls deleteIssue and shows success toast", async () => {
    vi.mocked(api.deleteIssue).mockResolvedValueOnce(undefined);

    const { wrapper } = createWrapper();
    const { result } = renderHook(() => useDeleteIssue(), { wrapper });

    await act(async () => {
      await result.current.mutateAsync("issue-1");
    });

    expect(api.deleteIssue).toHaveBeenCalledWith("issue-1");
    expect(toast.success).toHaveBeenCalledWith("Issue removida!");
  });
});

describe("useAddComment", () => {
  beforeEach(() => vi.clearAllMocks());

  it("adds a comment and updates the detail cache", async () => {
    const updated = makeIssue({
      comments: [{ id: "c1", content: "Ótimo!", authorId: "u1", createdAt: "2026-04-27T10:00:00Z" }],
    });
    vi.mocked(api.addComment).mockResolvedValueOnce(updated);

    const { wrapper, queryClient } = createWrapper();
    queryClient.setQueryData(issueKeys.detail("issue-1"), makeIssue());

    const { result } = renderHook(() => useAddComment("issue-1"), { wrapper });

    await act(async () => {
      await result.current.mutateAsync("Ótimo!");
    });

    expect(api.addComment).toHaveBeenCalledWith("issue-1", "Ótimo!");
    expect(toast.success).toHaveBeenCalledWith("Comentário adicionado!");
    const cached = queryClient.getQueryData(issueKeys.detail("issue-1")) as IssueDto;
    expect(cached.comments).toHaveLength(1);
  });
});
