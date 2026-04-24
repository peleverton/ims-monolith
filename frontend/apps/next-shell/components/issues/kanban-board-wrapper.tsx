"use client";

/**
 * kanban-board-wrapper.tsx — US-062
 * Client wrapper that connects the KanbanBoard to the API.
 */

import { useCallback } from "react";
import { KanbanBoard } from "./kanban-board";
import type { IssueDto, IssueStatus } from "@/lib/types";

interface KanbanBoardWrapperProps {
  issues: IssueDto[];
}

export function KanbanBoardWrapper({ issues }: KanbanBoardWrapperProps) {
  const handleStatusChange = useCallback(
    async (issueId: string, newStatus: IssueStatus) => {
      const res = await fetch(`/api/proxy/issues/${issueId}/status`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status: newStatus }),
      });
      if (!res.ok) throw new Error(`Failed to update status: ${res.status}`);
    },
    []
  );

  return <KanbanBoard issues={issues} onStatusChange={handleStatusChange} />;
}
