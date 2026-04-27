/**
 * lib/hooks/use-issues.ts — US-068
 *
 * TanStack React Query hooks for the Issues module.
 *
 * Query keys follow the pattern:
 *   ["issues"]                   — all issues list
 *   ["issues", params]           — filtered/paged list
 *   ["issue", id]                — single issue detail
 */

import {
  useQuery,
  useMutation,
  useQueryClient,
  keepPreviousData,
} from "@tanstack/react-query";
import { toast } from "sonner";
import {
  getIssues,
  getIssueById,
  createIssue,
  updateIssue,
  updateIssueStatus,
  assignIssue,
  addComment,
  deleteIssue,
  type GetIssuesParams,
} from "@/lib/api/issues";
import type {
  CreateIssueRequest,
  UpdateIssueRequest,
  IssueStatus,
} from "@/lib/types";

// ── Query Keys ────────────────────────────────────────────────────────────────

export const issueKeys = {
  all: ["issues"] as const,
  list: (params: GetIssuesParams) => ["issues", params] as const,
  detail: (id: string) => ["issue", id] as const,
};

// ── Queries ───────────────────────────────────────────────────────────────────

/**
 * Fetches a paginated, filtered list of issues.
 * Uses `keepPreviousData` so the current page stays visible while fetching next.
 */
export function useIssues(params: GetIssuesParams = {}) {
  return useQuery({
    queryKey: issueKeys.list(params),
    queryFn: () => getIssues(params),
    placeholderData: keepPreviousData,
  });
}

/** Fetches a single issue by ID. */
export function useIssue(id: string) {
  return useQuery({
    queryKey: issueKeys.detail(id),
    queryFn: () => getIssueById(id),
    enabled: Boolean(id),
  });
}

// ── Mutations ─────────────────────────────────────────────────────────────────

/** Creates a new issue and invalidates the list cache. */
export function useCreateIssue() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateIssueRequest) => createIssue(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: issueKeys.all });
      toast.success("Issue criada com sucesso!");
    },
    onError: () => toast.error("Erro ao criar issue."),
  });
}

/** Updates issue fields and refreshes list + detail caches. */
export function useUpdateIssue(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateIssueRequest) => updateIssue(id, data),
    onSuccess: (updated) => {
      qc.setQueryData(issueKeys.detail(id), updated);
      qc.invalidateQueries({ queryKey: issueKeys.all });
      toast.success("Issue atualizada!");
    },
    onError: () => toast.error("Erro ao atualizar issue."),
  });
}

/** Changes the status of an issue with optimistic update. */
export function useUpdateIssueStatus(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (status: IssueStatus) => updateIssueStatus(id, status),
    onMutate: async (newStatus) => {
      // Cancel in-flight queries for this issue
      await qc.cancelQueries({ queryKey: issueKeys.detail(id) });
      const previous = qc.getQueryData(issueKeys.detail(id));
      // Optimistically update detail cache
      qc.setQueryData(issueKeys.detail(id), (old: unknown) => {
        if (!old || typeof old !== "object") return old;
        return { ...(old as object), status: newStatus };
      });
      return { previous };
    },
    onError: (_err, _vars, context) => {
      // Rollback on error
      if (context?.previous) {
        qc.setQueryData(issueKeys.detail(id), context.previous);
      }
      toast.error("Erro ao atualizar status.");
    },
    onSuccess: (updated) => {
      qc.setQueryData(issueKeys.detail(id), updated);
      qc.invalidateQueries({ queryKey: issueKeys.all });
      toast.success("Status atualizado!");
    },
  });
}

/** Assigns an issue to a user. */
export function useAssignIssue(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (assigneeId: string) => assignIssue(id, assigneeId),
    onSuccess: (updated) => {
      qc.setQueryData(issueKeys.detail(id), updated);
      qc.invalidateQueries({ queryKey: issueKeys.all });
      toast.success("Issue atribuída!");
    },
    onError: () => toast.error("Erro ao atribuir issue."),
  });
}

/** Adds a comment to an issue. */
export function useAddComment(issueId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (content: string) => addComment(issueId, content),
    onSuccess: (updated) => {
      qc.setQueryData(issueKeys.detail(issueId), updated);
      toast.success("Comentário adicionado!");
    },
    onError: () => toast.error("Erro ao adicionar comentário."),
  });
}

/** Deletes an issue and invalidates the list. */
export function useDeleteIssue() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteIssue(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: issueKeys.all });
      toast.success("Issue removida!");
    },
    onError: () => toast.error("Erro ao remover issue."),
  });
}
