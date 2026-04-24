"use client";

/**
 * kanban-board.tsx — US-062: Issues Kanban Board view
 *
 * Drag-and-drop board with columns per status.
 * Uses @dnd-kit/core for accessible DnD.
 */

import { useState, useCallback } from "react";
import {
  DndContext,
  DragOverlay,
  PointerSensor,
  KeyboardSensor,
  useSensor,
  useSensors,
  closestCorners,
  type DragStartEvent,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  verticalListSortingStrategy,
  sortableKeyboardCoordinates,
} from "@dnd-kit/sortable";
import { KanbanColumn } from "./kanban-column";
import { KanbanCard } from "./kanban-card";
import type { IssueDto, IssueStatus } from "@/lib/types";

export const KANBAN_COLUMNS: { id: IssueStatus; label: string; color: string }[] = [
  { id: "Open", label: "Aberto", color: "border-t-blue-500" },
  { id: "InProgress", label: "Em Andamento", color: "border-t-yellow-500" },
  { id: "Resolved", label: "Resolvido", color: "border-t-green-500" },
  { id: "Closed", label: "Fechado", color: "border-t-gray-400" },
];

interface KanbanBoardProps {
  issues: IssueDto[];
  onStatusChange: (issueId: string, newStatus: IssueStatus) => Promise<void>;
}

export function KanbanBoard({ issues, onStatusChange }: KanbanBoardProps) {
  const [activeIssue, setActiveIssue] = useState<IssueDto | null>(null);
  const [localIssues, setLocalIssues] = useState<IssueDto[]>(issues);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

  const getIssuesByStatus = useCallback(
    (status: IssueStatus) => localIssues.filter((i) => i.status === status),
    [localIssues]
  );

  function handleDragStart(event: DragStartEvent) {
    const issue = localIssues.find((i) => i.id === event.active.id);
    setActiveIssue(issue ?? null);
  }

  async function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    setActiveIssue(null);

    if (!over) return;

    const draggedIssue = localIssues.find((i) => i.id === active.id);
    if (!draggedIssue) return;

    // over.id pode ser um column id ou um card id
    const targetStatus = KANBAN_COLUMNS.find(
      (col) => col.id === over.id || localIssues.find((i) => i.id === over.id)?.status === col.id
    );

    const newStatus: IssueStatus =
      (KANBAN_COLUMNS.find((c) => c.id === over.id)?.id as IssueStatus) ??
      (localIssues.find((i) => i.id === over.id)?.status as IssueStatus);

    if (!newStatus || newStatus === draggedIssue.status) return;

    // Optimistic update
    setLocalIssues((prev) =>
      prev.map((i) => (i.id === draggedIssue.id ? { ...i, status: newStatus } : i))
    );

    try {
      await onStatusChange(draggedIssue.id, newStatus);
    } catch {
      // Rollback
      setLocalIssues((prev) =>
        prev.map((i) =>
          i.id === draggedIssue.id ? { ...i, status: draggedIssue.status } : i
        )
      );
    }
  }

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCorners}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
    >
      <div
        className="grid gap-4"
        style={{ gridTemplateColumns: `repeat(${KANBAN_COLUMNS.length}, minmax(0, 1fr))` }}
        role="region"
        aria-label="Kanban board"
      >
        {KANBAN_COLUMNS.map((col) => {
          const colIssues = getIssuesByStatus(col.id);
          return (
            <KanbanColumn
              key={col.id}
              id={col.id}
              label={col.label}
              colorClass={col.color}
              count={colIssues.length}
            >
              <SortableContext
                items={colIssues.map((i) => i.id)}
                strategy={verticalListSortingStrategy}
              >
                {colIssues.map((issue) => (
                  <KanbanCard key={issue.id} issue={issue} />
                ))}
              </SortableContext>
            </KanbanColumn>
          );
        })}
      </div>

      <DragOverlay>
        {activeIssue && <KanbanCard issue={activeIssue} isOverlay />}
      </DragOverlay>
    </DndContext>
  );
}
