"use client";

/**
 * issues-view-toggle.tsx — US-062
 * Client component that switches between List and Kanban views.
 */

import { useRouter, useSearchParams } from "next/navigation";
import { LayoutList, LayoutGrid } from "lucide-react";

export function IssuesViewToggle() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const currentView = searchParams.get("view") ?? "list";

  function setView(view: "list" | "kanban") {
    const params = new URLSearchParams(searchParams.toString());
    params.set("view", view);
    router.push(`?${params.toString()}`);
  }

  return (
    <div className="flex items-center gap-1 rounded-lg border border-(--border) p-1 bg-(--bg-surface)">
      <button
        onClick={() => setView("list")}
        aria-label="Visualização em lista"
        aria-pressed={currentView === "list"}
        className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
          currentView === "list"
            ? "bg-blue-600 text-white"
            : "text-(--text-secondary) hover:bg-(--bg-subtle)"
        }`}
      >
        <LayoutList size={14} />
        Lista
      </button>
      <button
        onClick={() => setView("kanban")}
        aria-label="Visualização Kanban"
        aria-pressed={currentView === "kanban"}
        className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
          currentView === "kanban"
            ? "bg-blue-600 text-white"
            : "text-(--text-secondary) hover:bg-(--bg-subtle)"
        }`}
      >
        <LayoutGrid size={14} />
        Kanban
      </button>
    </div>
  );
}
