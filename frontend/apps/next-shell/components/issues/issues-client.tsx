"use client";

/**
 * IssuesClient — US-068
 *
 * Client-side issues list powered by TanStack React Query.
 * Features:
 *  - Instant filter/pagination without full page reloads
 *  - keepPreviousData for smooth page transitions
 *  - Inline status change with optimistic update
 *  - Loading skeleton and error state
 */

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Plus, MessageSquare, RefreshCw, Loader2 } from "lucide-react";
import { StatusBadge, PriorityBadge } from "@/components/badges";
import { useIssues, useUpdateIssueStatus, useDeleteIssue } from "@/lib/hooks/use-issues";
import type { IssueDto, IssueStatus, IssuePriority } from "@/lib/types";
import type { GetIssuesParams } from "@/lib/api/issues";

const STATUSES: IssueStatus[] = ["Open", "InProgress", "Resolved", "Closed"];
const PRIORITIES: IssuePriority[] = ["Low", "Medium", "High", "Critical"];

interface Props {
  initialParams?: GetIssuesParams;
}

export function IssuesClient({ initialParams = {} }: Props) {
  const router = useRouter();
  const [params, setParams] = useState<GetIssuesParams>({
    page: 1,
    pageSize: 15,
    ...initialParams,
  });

  const { data, isFetching, isError, refetch } = useIssues(params);
  const deleteMutation = useDeleteIssue();

  function setFilter(patch: Partial<GetIssuesParams>) {
    setParams((prev) => ({ ...prev, ...patch, page: 1 }));
  }

  function setPage(page: number) {
    setParams((prev) => ({ ...prev, page }));
  }

  return (
    <div>
      {/* ── Header ──────────────────────────────────────────────── */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-(--text-primary)">Issues</h1>
          <p className="text-(--text-secondary) text-sm mt-0.5">
            {data
              ? `${data.totalCount} issue${data.totalCount !== 1 ? "s" : ""} encontrada${data.totalCount !== 1 ? "s" : ""}`
              : isFetching
              ? "Carregando..."
              : "—"}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={() => refetch()}
            disabled={isFetching}
            title="Atualizar"
            className="p-2 rounded-lg border border-(--border-input) text-(--text-secondary) hover:text-(--text-primary) hover:bg-(--bg-subtle) disabled:opacity-50 transition-colors"
          >
            <RefreshCw size={16} className={isFetching ? "animate-spin" : ""} />
          </button>
          <Link
            href="/issues/new"
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-500 transition-colors"
          >
            <Plus size={16} />
            Nova Issue
          </Link>
        </div>
      </div>

      {/* ── Filters ─────────────────────────────────────────────── */}
      <div className="flex flex-wrap gap-3 mb-4">
        <input
          value={params.search ?? ""}
          onChange={(e) => setFilter({ search: e.target.value || undefined })}
          placeholder="Buscar issues..."
          className="flex-1 min-w-48 px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <select
          value={params.status ?? ""}
          onChange={(e) => setFilter({ status: (e.target.value as IssueStatus) || undefined })}
          className="px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">Todos os status</option>
          {STATUSES.map((s) => (
            <option key={s} value={s}>{s === "InProgress" ? "Em Progresso" : s}</option>
          ))}
        </select>
        <select
          value={params.priority ?? ""}
          onChange={(e) => setFilter({ priority: (e.target.value as IssuePriority) || undefined })}
          className="px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">Todas as prioridades</option>
          {PRIORITIES.map((p) => (
            <option key={p} value={p}>{p}</option>
          ))}
        </select>
      </div>

      {/* ── Content ─────────────────────────────────────────────── */}
      {isError ? (
        <div className="bg-(--bg-surface) rounded-xl border border-(--border) p-8 text-center text-(--text-secondary)">
          Erro ao carregar issues.{" "}
          <button onClick={() => refetch()} className="text-blue-600 hover:underline">
            Tentar novamente
          </button>
        </div>
      ) : !data ? (
        <IssuesSkeleton />
      ) : data.items.length === 0 ? (
        <div className="bg-(--bg-surface) rounded-xl border border-(--border) p-12 text-center">
          <p className="text-(--text-secondary)">Nenhuma issue encontrada.</p>
        </div>
      ) : (
        <div className={`bg-(--bg-surface) rounded-xl border border-(--border) overflow-hidden shadow-sm transition-opacity ${isFetching ? "opacity-60" : "opacity-100"}`}>
          {isFetching && (
            <div className="flex items-center gap-2 px-4 py-2 bg-blue-50 dark:bg-blue-950/30 text-blue-600 text-xs border-b border-(--border)">
              <Loader2 size={12} className="animate-spin" />
              Atualizando...
            </div>
          )}
          <table className="w-full text-sm">
            <thead className="bg-(--bg-subtle) border-b border-(--border)">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">Título</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">Status</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">Prioridade</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide hidden md:table-cell">Resolvida em</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide hidden lg:table-cell">Criada em</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-(--border)">
              {data.items.map((issue) => (
                <IssueRow
                  key={issue.id}
                  issue={issue}
                  onDelete={() => deleteMutation.mutate(issue.id)}
                />
              ))}
            </tbody>
          </table>

          {/* ── Pagination ─────────────────────────────────────── */}
          <div className="px-4 py-3 border-t border-(--border) flex items-center justify-between text-sm text-(--text-secondary)">
            <span>
              Página {data.pageNumber} de {data.totalPages}
              {" "}· {data.totalCount} issue{data.totalCount !== 1 ? "s" : ""}
            </span>
            <div className="flex gap-2">
              {data.pageNumber > 1 && (
                <button
                  onClick={() => setPage(data.pageNumber - 1)}
                  className="px-3 py-1 rounded border border-(--border-input) text-(--text-primary) hover:bg-(--bg-subtle) transition-colors"
                >
                  Anterior
                </button>
              )}
              {data.pageNumber < data.totalPages && (
                <button
                  onClick={() => setPage(data.pageNumber + 1)}
                  className="px-3 py-1 rounded border border-(--border-input) text-(--text-primary) hover:bg-(--bg-subtle) transition-colors"
                >
                  Próxima
                </button>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Sub-components ─────────────────────────────────────────────────────────────

function IssueRow({ issue, onDelete }: { issue: IssueDto; onDelete: () => void }) {
  const statusMutation = useUpdateIssueStatus(issue.id);

  return (
    <tr className="hover:bg-(--bg-subtle) transition-colors">
      <td className="px-4 py-3 font-medium text-(--text-primary) max-w-xs truncate">
        {issue.title}
      </td>
      <td className="px-4 py-3">
        <StatusBadge status={issue.status} />
      </td>
      <td className="px-4 py-3">
        <PriorityBadge priority={issue.priority} />
      </td>
      <td className="px-4 py-3 text-(--text-secondary) hidden md:table-cell">
        {issue.resolvedAt
          ? new Date(issue.resolvedAt).toLocaleDateString("pt-BR")
          : "—"}
      </td>
      <td className="px-4 py-3 text-(--text-secondary) hidden lg:table-cell">
        {new Date(issue.createdAt).toLocaleDateString("pt-BR")}
      </td>
      <td className="px-4 py-3 text-right">
        <div className="flex items-center justify-end gap-3">
          {Array.isArray(issue.comments) && issue.comments.length > 0 && (
            <span className="flex items-center gap-1 text-(--text-muted) text-xs">
              <MessageSquare size={13} />
              {issue.comments.length}
            </span>
          )}
          <Link
            href={`/issues/${issue.id}`}
            className="text-blue-600 hover:text-blue-500 font-medium text-sm"
          >
            Ver
          </Link>
        </div>
      </td>
    </tr>
  );
}

function IssuesSkeleton() {
  return (
    <div className="bg-(--bg-surface) rounded-xl border border-(--border) overflow-hidden shadow-sm animate-pulse">
      <div className="divide-y divide-(--border)">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="flex items-center gap-4 px-4 py-3">
            <div className="h-4 bg-(--bg-subtle) rounded w-48" />
            <div className="h-5 bg-(--bg-subtle) rounded w-20" />
            <div className="h-5 bg-(--bg-subtle) rounded w-16" />
            <div className="h-4 bg-(--bg-subtle) rounded w-24 ml-auto" />
          </div>
        ))}
      </div>
    </div>
  );
}
