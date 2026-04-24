"use client";

/**
 * kanban-column.tsx — US-062
 * Droppable column container for the Kanban board.
 */

import { useDroppable } from "@dnd-kit/core";
import type { IssueStatus } from "@/lib/types";

interface KanbanColumnProps {
  id: IssueStatus;
  label: string;
  colorClass: string;
  count: number;
  children: React.ReactNode;
}

export function KanbanColumn({ id, label, colorClass, count, children }: KanbanColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id });

  return (
    <div
      ref={setNodeRef}
      className={`
        flex flex-col rounded-xl border border-(--border) bg-(--bg-subtle)
        border-t-4 ${colorClass} min-h-32
        transition-colors ${isOver ? "bg-blue-50 dark:bg-blue-950/20" : ""}
      `}
      aria-label={`Coluna ${label}`}
    >
      {/* Column header */}
      <div className="flex items-center justify-between px-3 py-2.5 border-b border-(--border)">
        <span className="text-sm font-semibold text-(--text-primary)">{label}</span>
        <span className="text-xs font-medium px-1.5 py-0.5 rounded-full bg-(--bg-surface) text-(--text-secondary) border border-(--border)">
          {count}
        </span>
      </div>

      {/* Cards */}
      <div className="flex flex-col gap-2 p-2 flex-1">
        {children}
      </div>
    </div>
  );
}
