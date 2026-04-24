"use client";

/**
 * US-063: Export analytics data as JSON or CSV.
 *
 * Calls GET /api/proxy/analytics/export via the BFF (authenticated).
 * The response is a file blob — triggers a browser download directly.
 */

import { useState } from "react";
import { Download, Loader2, ChevronDown } from "lucide-react";

type ExportFormat = "json" | "csv";
type ExportModule = "issues" | "inventory" | "users" | undefined;

interface ExportButtonProps {
  /** Pre-select a module to export. Omit to export all. */
  module?: ExportModule;
}

export function ExportButton({ module }: ExportButtonProps) {
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleExport(format: ExportFormat) {
    setOpen(false);
    setLoading(true);
    setError(null);

    try {
      const params = new URLSearchParams({ format });
      if (module) params.set("module", module);

      const res = await fetch(`/api/proxy/analytics/export?${params}`);

      if (!res.ok) {
        const text = await res.text().catch(() => res.statusText);
        throw new Error(`Erro ${res.status}: ${text}`);
      }

      // Derive filename from Content-Disposition or fallback
      const disposition = res.headers.get("Content-Disposition") ?? "";
      const match = disposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
      const filename =
        match?.[1]?.replace(/['"]/g, "") ??
        `ims-analytics-export.${format}`;

      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      a.remove();
      URL.revokeObjectURL(url);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao exportar");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="relative inline-block">
      {/* Trigger button */}
      <button
        onClick={() => setOpen((o) => !o)}
        disabled={loading}
        className="inline-flex items-center gap-1.5 rounded-md border border-gray-300 bg-white px-3 py-1.5 text-sm font-medium text-gray-700 shadow-sm transition hover:bg-gray-50 disabled:opacity-50 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-200 dark:hover:bg-gray-700"
        aria-haspopup="true"
        aria-expanded={open}
      >
        {loading ? (
          <Loader2 className="h-4 w-4 animate-spin" />
        ) : (
          <Download className="h-4 w-4" />
        )}
        Exportar
        <ChevronDown className="h-3.5 w-3.5 opacity-60" />
      </button>

      {/* Dropdown */}
      {open && (
        <>
          {/* Backdrop to close on click-outside */}
          <div
            className="fixed inset-0 z-10"
            onClick={() => setOpen(false)}
            aria-hidden="true"
          />
          <div className="absolute right-0 z-20 mt-1 w-36 rounded-md border border-gray-200 bg-white py-1 shadow-lg dark:border-gray-700 dark:bg-gray-800">
            <button
              onClick={() => handleExport("json")}
              className="flex w-full items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 dark:text-gray-200 dark:hover:bg-gray-700"
            >
              <span className="font-mono text-xs text-gray-400">{"{}"}</span>
              JSON
            </button>
            <button
              onClick={() => handleExport("csv")}
              className="flex w-full items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 dark:text-gray-200 dark:hover:bg-gray-700"
            >
              <span className="font-mono text-xs text-gray-400">CSV</span>
              CSV
            </button>
          </div>
        </>
      )}

      {/* Inline error */}
      {error && (
        <p className="absolute left-0 top-full mt-1 whitespace-nowrap text-xs text-red-600 dark:text-red-400">
          {error}
        </p>
      )}
    </div>
  );
}
