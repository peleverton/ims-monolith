import { apiFetch } from "@/lib/api-fetch";
import type { IssueDto, PagedResult } from "@/lib/types";
import { StatusBadge, PriorityBadge } from "@/components/badges";
import Link from "next/link";
import { Plus, MessageSquare } from "lucide-react";

interface SearchParams {
  page?: string;
  pageSize?: string;
  status?: string;
  priority?: string;
  search?: string;
}

async function getIssues(params: SearchParams) {
  const qs = new URLSearchParams({
    pageNumber: params.page ?? "1",
    pageSize: params.pageSize ?? "15",
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
  const data = await getIssues(params).catch(() => null);

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Issues</h1>
          <p className="text-gray-500 text-sm mt-0.5">
            {data ? `${data.totalCount} issues encontradas` : "Carregando..."}
          </p>
        </div>
        <Link
          href="/dashboard/issues/new"
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-500 transition-colors"
        >
          <Plus size={16} />
          Nova Issue
        </Link>
      </div>

      {/* Filtros */}
      <IssueFilters current={params} />

      {/* Tabela */}
      {!data ? (
        <div className="bg-white rounded-xl border border-gray-200 p-8 text-center text-gray-500">
          Erro ao carregar issues. Verifique se o servidor está rodando.
        </div>
      ) : data.items.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center">
          <p className="text-gray-500">Nenhuma issue encontrada.</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 overflow-hidden shadow-sm">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Título</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Status</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Prioridade</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Responsável</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Criada em</th>
                <th className="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {data.items.map((issue) => (
                <tr key={issue.id} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3 font-medium text-gray-900 max-w-xs truncate">
                    {issue.title}
                  </td>
                  <td className="px-4 py-3"><StatusBadge status={issue.status} /></td>
                  <td className="px-4 py-3"><PriorityBadge priority={issue.priority} /></td>
                  <td className="px-4 py-3 text-gray-500">{issue.assigneeName ?? "—"}</td>
                  <td className="px-4 py-3 text-gray-500">
                    {new Date(issue.createdAt).toLocaleDateString("pt-BR")}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex items-center justify-end gap-3">
                      {issue.commentsCount > 0 && (
                        <span className="flex items-center gap-1 text-gray-400 text-xs">
                          <MessageSquare size={13} />
                          {issue.commentsCount}
                        </span>
                      )}
                      <Link
                        href={`/dashboard/issues/${issue.id}`}
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

          {/* Paginação */}
          <div className="px-4 py-3 border-t border-gray-200 flex items-center justify-between text-sm text-gray-500">
            <span>
              Página {data.pageNumber} de {data.totalPages}
            </span>
            <div className="flex gap-2">
              {data.pageNumber > 1 && (
                <Link
                  href={`?page=${data.pageNumber - 1}`}
                  className="px-3 py-1 rounded border border-gray-300 hover:bg-gray-50"
                >
                  Anterior
                </Link>
              )}
              {data.pageNumber < data.totalPages && (
                <Link
                  href={`?page=${data.pageNumber + 1}`}
                  className="px-3 py-1 rounded border border-gray-300 hover:bg-gray-50"
                >
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
        className="flex-1 min-w-48 px-3 py-2 rounded-lg border border-gray-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
      <select
        name="status"
        defaultValue={current.status ?? ""}
        className="px-3 py-2 rounded-lg border border-gray-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        <option value="">Todos os status</option>
        {statuses.map((s) => <option key={s} value={s}>{s}</option>)}
      </select>
      <select
        name="priority"
        defaultValue={current.priority ?? ""}
        className="px-3 py-2 rounded-lg border border-gray-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      >
        <option value="">Todas as prioridades</option>
        {priorities.map((p) => <option key={p} value={p}>{p}</option>)}
      </select>
      <button
        type="submit"
        className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-500 transition-colors"
      >
        Filtrar
      </button>
    </form>
  );
}
