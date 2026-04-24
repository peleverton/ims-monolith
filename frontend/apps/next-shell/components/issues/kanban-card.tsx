"use client";

/**
 * kanban-card.tsx — US-062
 * Draggable issue card for the Kanban board.
 */

import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import Link from "next/link";
import { PriorityBadge } from "@/components/badges";
import type { IssueDto } from "@/lib/types";

const PRIORITY_ORDER = { Critical: 0, High: 1, Medium: 2, Low: 3 };

interface KanbanCardProps {
  issue: IssueDto;
  isOverlay?: boolean;
}

export function KanbanCard({ issue, isOverlay = false }: KanbanCardProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: issue.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      aria-label={`Issue: ${issue.title}`}
      className={`
        bg-(--bg-surface) rounded-lg border border-(--border) p-3 cursor-grab active:cursor-grabbing
        shadow-sm hover:shadow-md transition-shadow select-none
        ${isDragging ? "opacity-40 ring-2 ring-blue-500" : ""}
        ${isOverlay ? "shadow-xl rotate-1 opacity-95" : ""}
      `}
    >
      <div className="flex items-start justify-between gap-2 mb-2">
        <p className="text-xs font-semibold text-(--text-primary) line-clamp-2 flex-1">
          {issue.title}
        </p>
        <PriorityBadge priority={issue.priority} />
      </div>

      {issue.assigneeName && (
        <p className="text-xs text-(--text-muted) mt-1 truncate">
          👤 {issue.assigneeName}
        </p>
      )}

      <div className="flex items-center justify-between mt-2">
        <span className="text-[10px] text-(--text-muted)">
          {new Date(issue.createdAt).toLocaleDateString("pt-BR")}
        </span>
        <Link
          href={`/issues/${issue.id}`}
          onClick={(e) => e.stopPropagation()}
          className="text-[10px] text-blue-600 hover:text-blue-500 font-medium"
          aria-label={`Ver issue ${issue.title}`}
        >
          Ver →
        </Link>
      </div>
    </div>
  );
}
