import { apiFetch } from "@/lib/api-fetch";
import type { IssueDto, PagedResult } from "@/lib/types";
import { StatusBadge, PriorityBadge } from "@/components/badges";
import Link from "next/link";
import { Plus, MessageSquare } from "lucide-react";
import { IssuesViewToggle } from "@/components/issues/issues-view-toggle";
import { KanbanBoardWrapper } from "@/components/issues/kanban-board-wrapper";
import { Suspense } from "react";

interface SearchParams {
  page?: string;
  pageSize?: string;
  status?: string;
  priority?: string;
  search?: string;
  view?: string;
}

async function getIssues(params: SearchParams) {
  const isKanban = params.view === "kanban";
  const qs = new URLSearchParams({
    pageNumber: params.page ?? "1",
    pageSize: isKanban ? "100" : (params.pageSize ?? "15"),
    ...(params.status && { status: params.status }),
    ...(params.priority && { priority: params.priority }),
    ...(params.search && { searchTerm: params.search }),
  });

  return apiFetch<PagedResult<IssueDto>>(`/api/issues?${qs}`);
}

export const metadata = { title: "Issues" };

export default async function IssuesPage({
  searchParams,
}: {
  searchParams: Promise<SearchParams>;
}) {
  const params = await searchParams;
  const isKanban = params.view === "kanban";
  const data = await getIssues(params).catch(() => null);

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-(--text-primary)">Issues</h1>
          <p className="text-(--text-secondary) text-sm mt-0.5">
            {data ? `${data.totalCount} issues encontradas` : "Carregando..."}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Suspense>
            <IssuesViewToggle />
          </Suspense>
          <Link
            href="/issues/new"
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-500 transition-colors"
          >
            <Plus size={16} />
            Nova Issue
          </Link>
        </div>
      </div>

      <IssueFilters current={params} />

      {isKanban ? (
        !data ? (
          <div className="bg-(--bg-surface) rounded-xl border border-(--border) p-8 text-center text-(--text-secondary)">
            Erro ao carregar issues.
          </div>
        ) : (
          <KanbanBoardWrapper issues={data.items} />
        )
      ) : !data ? (
        <div className="bg-(--bg-surface) rounded-xl border border-(--border) p-8 text-center text-(--text-secondary)">
          Erro ao carregar issues. Verifique se o servidor está rodando.
        </div>
      ) : data.items.length === 0 ? (
        <div className="bg-(--bg-surface) rounded-xl border border-(--border) p-12 text-center">
          <p className="text-(--text-secondary)">Nenhuma issue encontrada.</p>
        </div>
      ) : (
        <div className="bg-(--bg-surface) rounded-xl border border-(--border) overflow-hidden shadow-sm">
          <table className="w-full text-sm">
            <thead className="bg-(--bg-subtle) border-b border-(--border)">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">Título</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">Status</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">Prioridade</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">Responsável</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">Criada em</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-(--border)">
              {data.items.map((issue) => (
                <tr key={issue.id} className="hover:bg-(--bg-subtle) transition-colors">
                  <td className="px-4 py-3 font-medium text-(--text-primary) max-w-xs truncate">
                    {issue.title}
                  </td>
                  <td className="px-4 py-3"><StatusBadge status={issue.status} /></td>
                  <td className="px-4 py-3"><PriorityBadge priority={issue.priority} /></td>
                  <td className="px-4 py-3 text-(--text-secondary)">{issue.assigneeName ?? "—"}</td>
                  <td className="px-4 py-3 text-(--text-secondary)">
                    {new Date(issue.createdAt).toLocaleDateString("pt-BR")}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex items-center justify-end gap-3">
                      {issue.commentsCount > 0 && (
                        <span className="flex items-center gap-1 text-(--text-muted) text-xs">
                          <MessageSquare size={13} />
                          {issue.commentsCount}
                        </span>
                      )}
                      <Link
                        href={`/issues/${issue.id}`}
                        className="text-blue-600 hover:text-blue-500 font-medium"
                      >
                        Ver
                      </Link>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="px-4 py-3 border-t border-(--border) flex items-center justify-between text-sm text-(--text-secondary)">
            <span>Página {data.pageNumber} de {data.totalPages}</span>
            <div className="flex gap-2">
              {data.pageNumber > 1 && (
                <Link href={`?page=${data.pageNumber - 1}`} className="px-3 py-1 rounded border border-(--border-input) text-(--text-primary) hover:bg-(--bg-subtle)">
                  Anterior
                </Link>
              )}
              {data.pageNumber < data.totalPages && (
                <Link href={`?page=${data.pageNumber + 1}`} className="px-3 py-1 rounded border border-(--border-input) text-(--text-primary) hover:bg-(--bg-subtle)">
                  Próxima
                </Link>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function IssueFilters({ current }: { current: SearchParams }) {
  const statuses = ["Open", "InProgress", "Resolved", "Closed"];
  const priorities = ["Low", "Medium", "High", "Critical"];

  return (
    <form className="flex flex-wrap gap-3 mb-4">
      <input
        name="search"
        defaultValue={current.search}
        placeholder="Buscar issues..."
        className="flex-1 min-w-48 px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
      <select
        name="status"
        defaultValue={current.status ?? ""}
        className="px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        <option value="">Todos os status</option>
        {statuses.map((s) => <option key={s} value={s}>{s}</option>)}
      </select>
      <select
        name="priority"
        defaultValue={current.priority ?? ""}
        className="px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        <option value="">Todas as prioridades</option>
        {priorities.map((p) => <option key={p} value={p}>{p}</option>)}
      </select>
      {current.view && <input type="hidden" name="view" value={current.view} />}
      <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-500 transition-colors">
        Filtrar
      </button>
    </form>
  );
}
