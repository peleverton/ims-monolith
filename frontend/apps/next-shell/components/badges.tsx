import { cn } from "@/lib/utils";
import type { IssueStatus, IssuePriority } from "@/lib/types";

const statusStyles: Record<IssueStatus, string> = {
  Open: "bg-blue-100 text-blue-700",
  InProgress: "bg-yellow-100 text-yellow-700",
  Resolved: "bg-green-100 text-green-700",
  Closed: "bg-gray-100 text-gray-600",
};

const priorityStyles: Record<IssuePriority, string> = {
  Low: "bg-slate-100 text-slate-600",
  Medium: "bg-orange-100 text-orange-600",
  High: "bg-red-100 text-red-700",
  Critical: "bg-red-200 text-red-800 font-semibold",
};

export function StatusBadge({ status }: { status: IssueStatus }) {
  return (
    <span className={cn("inline-flex px-2 py-0.5 rounded-full text-xs font-medium", statusStyles[status])}>
      {status === "InProgress" ? "Em Progresso" : status}
    </span>
  );
}

export function PriorityBadge({ priority }: { priority: IssuePriority }) {
  return (
    <span className={cn("inline-flex px-2 py-0.5 rounded-full text-xs font-medium", priorityStyles[priority])}>
      {priority}
    </span>
  );
}
