import { apiFetch } from "@/lib/api-fetch";
import type { IssueDto } from "@/lib/types";
import { StatusBadge, PriorityBadge } from "@/components/badges";
import { notFound } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, MessageSquare, Tag } from "lucide-react";

export async function generateMetadata({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const issue = await apiFetch<IssueDto>(`/api/issues/${id}`).catch(() => null);
  return { title: issue?.title ?? "Issue" };
}

export default async function IssueDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const issue = await apiFetch<IssueDto>(`/api/issues/${id}`).catch(() => null);

  if (!issue) notFound();

  return (
    <div className="max-w-3xl">
      <Link
        href="/issues"
        className="flex items-center gap-2 text-sm text-(--text-secondary) hover:text-(--text-primary) mb-6"
      >
        <ArrowLeft size={15} />
        Voltar para Issues
      </Link>

      <div className="bg-(--bg-surface) rounded-xl border border-(--border) shadow-sm p-6">
        <div className="flex items-start justify-between gap-4 mb-4">
          <h1 className="text-xl font-bold text-(--text-primary)">{issue.title}</h1>
          <div className="flex items-center gap-2 shrink-0">
            <StatusBadge status={issue.status} />
            <PriorityBadge priority={issue.priority} />
          </div>
        </div>

        <p className="text-(--text-secondary) mb-6 leading-relaxed">{issue.description}</p>

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
          <div>
            <p className="text-(--text-muted) text-xs uppercase tracking-wide mb-1">Atualizada em</p>
            <p className="font-medium text-(--text-primary)">
              {new Date(issue.updatedAt).toLocaleString("pt-BR")}
            </p>
          </div>
          <div>
            <p className="text-(--text-muted) text-xs uppercase tracking-wide mb-1">Comentários</p>
            <p className="flex items-center gap-1 font-medium text-(--text-primary)">
              <MessageSquare size={14} className="text-(--text-muted)" />
              {issue.commentsCount}
            </p>
          </div>
        </div>

        {issue.tags.length > 0 && (
          <div className="mt-4 flex items-center gap-2 flex-wrap">
            <Tag size={14} className="text-(--text-muted)" />
            {issue.tags.map((tag) => (
              <span key={tag} className="px-2 py-0.5 bg-(--bg-subtle) text-(--text-secondary) rounded text-xs border border-(--border)">
                {tag}
              </span>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
