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
        className="flex items-center gap-2 text-sm text-gray-500 hover:text-gray-700 mb-6"
      >
        <ArrowLeft size={15} />
        Voltar para Issues
      </Link>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <div className="flex items-start justify-between gap-4 mb-4">
          <h1 className="text-xl font-bold text-gray-900">{issue.title}</h1>
          <div className="flex items-center gap-2 shrink-0">
            <StatusBadge status={issue.status} />
            <PriorityBadge priority={issue.priority} />
          </div>
        </div>

        <p className="text-gray-600 mb-6 leading-relaxed">{issue.description}</p>

        <div className="grid grid-cols-2 gap-4 text-sm border-t border-gray-100 pt-4">
          <div>
            <p className="text-gray-400 text-xs uppercase tracking-wide mb-1">Responsável</p>
            <p className="font-medium">{issue.assigneeName ?? "Não atribuído"}</p>
          </div>
          <div>
            <p className="text-gray-400 text-xs uppercase tracking-wide mb-1">Criada em</p>
            <p className="font-medium">
              {new Date(issue.createdAt).toLocaleString("pt-BR")}
            </p>
          </div>
          <div>
            <p className="text-gray-400 text-xs uppercase tracking-wide mb-1">Atualizada em</p>
            <p className="font-medium">
              {new Date(issue.updatedAt).toLocaleString("pt-BR")}
            </p>
          </div>
          <div>
            <p className="text-gray-400 text-xs uppercase tracking-wide mb-1">Comentários</p>
            <p className="flex items-center gap-1 font-medium">
              <MessageSquare size={14} className="text-gray-400" />
              {issue.commentsCount}
            </p>
          </div>
        </div>

        {issue.tags.length > 0 && (
          <div className="mt-4 flex items-center gap-2 flex-wrap">
            <Tag size={14} className="text-gray-400" />
            {issue.tags.map((tag) => (
              <span key={tag} className="px-2 py-0.5 bg-gray-100 text-gray-600 rounded text-xs">
                {tag}
              </span>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
