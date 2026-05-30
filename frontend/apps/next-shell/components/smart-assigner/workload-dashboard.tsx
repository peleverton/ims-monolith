"use client";

import { useWorkloadDashboard, type AutoAssignmentEvent } from "@/lib/hooks/use-workload-dashboard";
import { useEffect, useState } from "react";
import { apiFetch } from "@/lib/api-fetch";
import type { WorkloadEntry } from "@/lib/hooks/use-workload-dashboard";

// ─── Workload Table ───────────────────────────────────────────────────────────

function WorkloadTable({ candidates }: { candidates: WorkloadEntry[] }) {
  return (
    <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
      <table className="min-w-full text-sm">
        <thead className="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th className="px-4 py-3 text-left font-medium text-gray-600 dark:text-gray-300">Usuário</th>
            <th className="px-4 py-3 text-center font-medium text-gray-600 dark:text-gray-300">Ativas</th>
            <th className="px-4 py-3 text-center font-medium text-gray-600 dark:text-gray-300">Atrasadas</th>
            <th className="px-4 py-3 text-center font-medium text-gray-600 dark:text-gray-300">Tempo Médio</th>
            <th className="px-4 py-3 text-center font-medium text-gray-600 dark:text-gray-300">Score</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600 dark:text-gray-300">Skills</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100 dark:divide-gray-700">
          {candidates.map((c) => (
            <tr key={c.userId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
              <td className="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{c.username}</td>
              <td className="px-4 py-3 text-center">
                <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                  c.activeIssues >= 10 ? "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400" :
                  c.activeIssues >= 5 ? "bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400" :
                  "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400"
                }`}>
                  {c.activeIssues}
                </span>
              </td>
              <td className="px-4 py-3 text-center">
                {c.overdueIssues > 0 ? (
                  <span className="text-red-600 dark:text-red-400 font-semibold">{c.overdueIssues}</span>
                ) : (
                  <span className="text-gray-400">0</span>
                )}
              </td>
              <td className="px-4 py-3 text-center text-gray-600 dark:text-gray-400">
                {c.avgResolutionHours.toFixed(1)}h
              </td>
              <td className="px-4 py-3 text-center">
                <span className="font-mono text-xs bg-gray-100 dark:bg-gray-700 px-2 py-0.5 rounded">
                  {c.score.toFixed(1)}
                </span>
              </td>
              <td className="px-4 py-3">
                <div className="flex flex-wrap gap-1">
                  {c.skills.slice(0, 3).map((skill) => (
                    <span key={skill} className="inline-flex items-center rounded bg-blue-50 dark:bg-blue-900/30 px-1.5 py-0.5 text-xs text-blue-700 dark:text-blue-300">
                      {skill}
                    </span>
                  ))}
                  {c.skills.length > 3 && (
                    <span className="text-xs text-gray-400">+{c.skills.length - 3}</span>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// ─── Activity Feed ────────────────────────────────────────────────────────────

function ActivityFeed({ events }: { events: AutoAssignmentEvent[] }) {
  if (events.length === 0) {
    return (
      <div className="flex items-center justify-center h-48 text-gray-400 dark:text-gray-500 text-sm">
        Aguardando atribuições automáticas...
      </div>
    );
  }

  return (
    <div className="space-y-2 max-h-96 overflow-y-auto">
      {events.map((event) => (
        <div
          key={event.id}
          className="flex items-start gap-3 rounded-lg border border-gray-100 dark:border-gray-700 p-3 bg-white dark:bg-gray-800 animate-in slide-in-from-top-2 duration-300"
        >
          <div className="flex-shrink-0 mt-0.5">
            <span className={`inline-block w-2 h-2 rounded-full ${
              event.priority === "Critical" ? "bg-red-500" :
              event.priority === "High" ? "bg-orange-500" :
              event.priority === "Medium" ? "bg-yellow-500" : "bg-green-500"
            }`} />
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
              {event.issueTitle}
            </p>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
              {event.reason}
            </p>
            <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">
              {new Date(event.occurredOn).toLocaleTimeString("pt-BR")}
            </p>
          </div>
          <span className={`flex-shrink-0 inline-flex items-center rounded px-1.5 py-0.5 text-xs font-medium ${
            event.priority === "Critical" ? "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400" :
            event.priority === "High" ? "bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400" :
            "bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300"
          }`}>
            {event.priority}
          </span>
        </div>
      ))}
    </div>
  );
}

// ─── Connection Status ────────────────────────────────────────────────────────

function ConnectionBadge({ connected }: { connected: boolean }) {
  return (
    <span className={`inline-flex items-center gap-1.5 text-xs font-medium ${
      connected ? "text-green-600 dark:text-green-400" : "text-red-600 dark:text-red-400"
    }`}>
      <span className={`w-1.5 h-1.5 rounded-full ${
        connected ? "bg-green-500 animate-pulse" : "bg-red-500"
      }`} />
      {connected ? "Conectado" : "Desconectado"}
    </span>
  );
}

// ─── Main Dashboard Component ─────────────────────────────────────────────────

export function WorkloadDashboardClient() {
  const { recentAssignments, isConnected, totalAutoAssigned } = useWorkloadDashboard();
  const [candidates, setCandidates] = useState<WorkloadEntry[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function fetchCandidates() {
      try {
        const data = await apiFetch<WorkloadEntry[]>("/api/smart-assigner/candidates");
        setCandidates(data);
      } catch {
        // Silent fail — will show empty state
      } finally {
        setLoading(false);
      }
    }
    fetchCandidates();

    // Refresh every 30s
    const interval = setInterval(fetchCandidates, 30_000);
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            Smart Assigner — Dashboard
          </h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
            Distribuição automática de issues em tempo real
          </p>
        </div>
        <ConnectionBadge connected={isConnected} />
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4">
          <p className="text-sm text-gray-500 dark:text-gray-400">Total Auto-Atribuídas</p>
          <p className="text-3xl font-bold text-gray-900 dark:text-gray-100 mt-1">{totalAutoAssigned}</p>
        </div>
        <div className="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4">
          <p className="text-sm text-gray-500 dark:text-gray-400">Candidatos Ativos</p>
          <p className="text-3xl font-bold text-gray-900 dark:text-gray-100 mt-1">{candidates.length}</p>
        </div>
        <div className="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-4">
          <p className="text-sm text-gray-500 dark:text-gray-400">Eventos Recentes</p>
          <p className="text-3xl font-bold text-gray-900 dark:text-gray-100 mt-1">{recentAssignments.length}</p>
        </div>
      </div>

      {/* Two columns: Table + Feed */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Workload Table */}
        <div className="lg:col-span-2">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-3">
            Ranking de Carga de Trabalho
          </h2>
          {loading ? (
            <div className="animate-pulse space-y-3">
              {[...Array(5)].map((_, i) => (
                <div key={i} className="h-12 bg-gray-100 dark:bg-gray-800 rounded" />
              ))}
            </div>
          ) : (
            <WorkloadTable candidates={candidates} />
          )}
        </div>

        {/* Activity Feed */}
        <div>
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-3">
            Feed de Atribuições
          </h2>
          <ActivityFeed events={recentAssignments} />
        </div>
      </div>
    </div>
  );
}
