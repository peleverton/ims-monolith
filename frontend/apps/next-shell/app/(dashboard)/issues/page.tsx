/**
 * Issues Page — US-068
 *
 * Server component shell. The list is rendered by IssuesClient (React Query).
 * Kanban view keeps its server-side fetch unchanged.
 */

import { IssuesClient } from "@/components/issues/issues-client";
import { IssuesViewToggle } from "@/components/issues/issues-view-toggle";
import { KanbanBoardWrapper } from "@/components/issues/kanban-board-wrapper";
import { apiFetch } from "@/lib/api-fetch";
import type { IssueDto, PagedResult } from "@/lib/types";
import { Suspense } from "react";

interface SearchParams {
  view?: string;
}

export const metadata = { title: "Issues" };

export default async function IssuesPage({
  searchParams,
}: {
  searchParams: Promise<SearchParams>;
}) {
  const params = await searchParams;
  const isKanban = params.view === "kanban";

  return (
    <div>
      <div className="flex justify-end mb-4">
        <Suspense>
          <IssuesViewToggle />
        </Suspense>
      </div>

      {isKanban ? (
        <KanbanFallback />
      ) : (
        <IssuesClient />
      )}
    </div>
  );
}

async function KanbanFallback() {
  const data = await apiFetch<PagedResult<IssueDto>>(
    "/api/issues?pageNumber=1&pageSize=100"
  ).catch(() => null);

  if (!data) {
    return (
      <div className="bg-(--bg-surface) rounded-xl border border-(--border) p-8 text-center text-(--text-secondary)">
        Erro ao carregar issues.
      </div>
    );
  }

  return <KanbanBoardWrapper issues={data.items} />;
}
