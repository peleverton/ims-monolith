"use client";

/**
 * IssueDetailClient — US-068
 *
 * Client-side issue detail panel powered by React Query.
 * Features:
 *  - Inline status change with optimistic update
 *  - Add comment form
 *  - resolvedAt display
 *  - Auto-refresh on window focus
 */

import { useState } from "react";
import Link from "next/link";
import { ArrowLeft, MessageSquare, Tag, Clock, CheckCircle2, Loader2 } from "lucide-react";
import { StatusBadge, PriorityBadge } from "@/components/badges";
import { useIssue, useUpdateIssueStatus, useAddComment } from "@/lib/hooks/use-issues";
import type { IssueStatus } from "@/lib/types";

const STATUSES: IssueStatus[] = ["Open", "InProgress", "Resolved", "Closed"];

interface Props {
  id: string;
}

export function IssueDetailClient({ id }: Props) {
  const { data: issue, isLoading, isError, refetch } = useIssue(id);
  const statusMutation = useUpdateIssueStatus(id);
  const commentMutation = useAddComment(id);

  const [comment, setComment] = useState("");

  if (isLoading) {
    return <IssueDetailSkeleton />;
  }

  if (isError || !issue) {
    return (
      <div className="max-w-3xl">
        <Link
          href="/issues"
          className="flex items-center gap-2 text-sm text-(--text-secondary) hover:text-(--text-primary) mb-6"
        >
          <ArrowLeft size={15} />
          Voltar para Issues
        </Link>
        <div className="bg-(--bg-surface) rounded-xl border border-(--border) p-12 text-center text-(--text-secondary)">
          Erro ao carregar issue.{" "}
          <button onClick={() => refetch()} className="text-blue-600 hover:underline">
            Tentar novamente
          </button>
        </div>
      </div>
    );
  }

  const handleAddComment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!comment.trim()) return;
    await commentMutation.mutateAsync(comment.trim());
    setComment("");
  };

  return (
    <div className="max-w-3xl">
      <Link
        href="/issues"
        className="flex items-center gap-2 text-sm text-(--text-secondary) hover:text-(--text-primary) mb-6"
      >
        <ArrowLeft size={15} />
        Voltar para Issues
      </Link>

      {/* ── Main card ──────────────────────────────────────────── */}
      <div className="bg-(--bg-surface) rounded-xl border border-(--border) shadow-sm p-6 mb-4">
        <div className="flex items-start justify-between gap-4 mb-4">
          <h1 className="text-xl font-bold text-(--text-primary)">{issue.title}</h1>
          <div className="flex items-center gap-2 shrink-0">
            <StatusBadge status={issue.status} />
            <PriorityBadge priority={issue.priority} />
          </div>
        </div>

        <p className="text-(--text-secondary) mb-6 leading-relaxed">{issue.description}</p>

        {/* ── Metadata grid ──────────────────────────────────── */}
        <div className="grid grid-cols-2 gap-4 text-sm border-t border-(--border) pt-4">
          <div>
            <p className="text-(--text-muted) text-xs uppercase tracking-wide mb-1">Responsável</p>
            <p className="font-medium text-(--text-primary)">{issue.assigneeName ?? "Não atribuído"}</p>
          </div>
          <div>
            <p className="text-(--text-muted) text-xs uppercase tracking-wide mb-1">Criada em</p>
            <p className="font-medium text-(--text-primary)">
              {new Date(issue.createdAt).toLocaleString("pt-BR")}
            </p>
          </div>
          {issue.updatedAt && (
            <div>
              <p className="text-(--text-muted) text-xs uppercase tracking-wide mb-1">Atualizada em</p>
              <p className="font-medium text-(--text-primary)">
                {new Date(issue.updatedAt).toLocaleString("pt-BR")}
              </p>
            </div>
          )}
          {issue.dueDate && (
            <div>
              <p className="text-(--text-muted) text-xs uppercase tracking-wide mb-1 flex items-center gap-1">
                <Clock size={10} /> Prazo
              </p>
              <p className="font-medium text-(--text-primary)">
                {new Date(issue.dueDate).toLocaleDateString("pt-BR")}
              </p>
            </div>
          )}
          {issue.resolvedAt && (
            <div>
              <p className="text-green-600 text-xs uppercase tracking-wide mb-1 flex items-center gap-1">
                <CheckCircle2 size={10} /> Resolvida em
              </p>
              <p className="font-medium text-green-600">
                {new Date(issue.resolvedAt).toLocaleString("pt-BR")}
              </p>
            </div>
          )}
        </div>

        {/* ── Tags ───────────────────────────────────────────── */}
        {issue.tags && issue.tags.length > 0 && (
          <div className="mt-4 flex items-center gap-2 flex-wrap">
            <Tag size={14} className="text-(--text-muted)" />
            {issue.tags.map((tag) => (
              <span
                key={tag.name}
                style={{ backgroundColor: tag.color + "22", borderColor: tag.color + "55", color: tag.color }}
                className="px-2 py-0.5 rounded text-xs border font-medium"
              >
                {tag.name}
              </span>
            ))}
          </div>
        )}
      </div>

      {/* ── Change Status ──────────────────────────────────────── */}
      <div className="bg-(--bg-surface) rounded-xl border border-(--border) shadow-sm p-4 mb-4">
        <h2 className="text-sm font-semibold text-(--text-secondary) uppercase tracking-wide mb-3">
          Alterar Status
        </h2>
        <div className="flex flex-wrap gap-2">
          {STATUSES.map((s) => (
            <button
              key={s}
              disabled={statusMutation.isPending || issue.status === s}
              onClick={() => statusMutation.mutate(s)}
              className={`px-3 py-1.5 rounded-lg text-sm font-medium border transition-colors
                ${issue.status === s
                  ? "bg-blue-600 text-white border-blue-600"
                  : "bg-(--bg-subtle) text-(--text-primary) border-(--border-input) hover:bg-(--bg-app) disabled:opacity-50"
                }`}
            >
              {statusMutation.isPending && issue.status !== s ? (
                <Loader2 size={12} className="animate-spin inline mr-1" />
              ) : null}
              {s === "InProgress" ? "Em Progresso" : s}
            </button>
          ))}
        </div>
      </div>

      {/* ── Comments ───────────────────────────────────────────── */}
      <div className="bg-(--bg-surface) rounded-xl border border-(--border) shadow-sm p-4">
        <h2 className="text-sm font-semibold text-(--text-secondary) uppercase tracking-wide mb-3 flex items-center gap-2">
          <MessageSquare size={14} />
          Comentários ({issue.comments?.length ?? 0})
        </h2>

        {issue.comments && issue.comments.length > 0 ? (
          <div className="space-y-3 mb-4">
            {issue.comments.map((c) => (
              <div key={c.id} className="bg-(--bg-subtle) rounded-lg p-3">
                <p className="text-sm text-(--text-primary)">{c.content}</p>
                <p className="text-xs text-(--text-muted) mt-1">
                  {new Date(c.createdAt).toLocaleString("pt-BR")}
                </p>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-sm text-(--text-muted) mb-4">Nenhum comentário ainda.</p>
        )}

        <form onSubmit={handleAddComment} className="flex gap-2">
          <input
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder="Adicionar comentário..."
            className="flex-1 px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-app) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          <button
            type="submit"
            disabled={!comment.trim() || commentMutation.isPending}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-500 disabled:opacity-50 transition-colors flex items-center gap-1"
          >
            {commentMutation.isPending ? <Loader2 size={14} className="animate-spin" /> : null}
            Enviar
          </button>
        </form>
      </div>
    </div>
  );
}

function IssueDetailSkeleton() {
  return (
    <div className="max-w-3xl animate-pulse space-y-4">
      <div className="h-4 w-32 bg-(--bg-subtle) rounded" />
      <div className="bg-(--bg-surface) rounded-xl border border-(--border) p-6 space-y-4">
        <div className="h-6 bg-(--bg-subtle) rounded w-2/3" />
        <div className="h-4 bg-(--bg-subtle) rounded w-full" />
        <div className="h-4 bg-(--bg-subtle) rounded w-3/4" />
        <div className="grid grid-cols-2 gap-4 pt-4 border-t border-(--border)">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="space-y-1">
              <div className="h-3 bg-(--bg-subtle) rounded w-20" />
              <div className="h-4 bg-(--bg-subtle) rounded w-32" />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
