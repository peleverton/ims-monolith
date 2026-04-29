/**
 * Home Dashboard — US-060
 *
 * Server Component: busca KPIs reais do backend em paralelo no request,
 * sem loading spinner (zero client-side fetch para o conteúdo principal).
 */

import Link from "next/link";
import {
  AlertCircle,
  Package,
  CheckCircle2,
  TrendingDown,
  ChevronRight,
  Plus,
} from "lucide-react";
import { Card, CardHeader } from "@/components/ui/card";
import { StatusBadge, PriorityBadge } from "@/components/badges";
import { apiFetch } from "@/lib/api-fetch";
import type {
  PagedResult,
  IssueDto,
  AnalyticsSummaryDto,
  ProductListDto,
} from "@/lib/types";

export const metadata = { title: "Dashboard — IMS" };

// ── Data fetching (parallel, server-side) ─────────────────────────────────────

async function fetchDashboardData() {
  const [analyticsResult, recentIssuesResult, lowStockResult] =
    await Promise.allSettled([
      apiFetch<AnalyticsSummaryDto>("/api/analytics/summary"),
      apiFetch<PagedResult<IssueDto>>(
        "/api/issues?pageNumber=1&pageSize=5&sort=updatedAt"
      ),
      apiFetch<PagedResult<ProductListDto>>(
        "/api/inventory/products?pageSize=5&stockStatus=LowStock"
      ),
    ]);

  return {
    analytics:
      analyticsResult.status === "fulfilled" ? analyticsResult.value : null,
    recentIssues:
      recentIssuesResult.status === "fulfilled"
        ? recentIssuesResult.value
        : null,
    lowStock:
      lowStockResult.status === "fulfilled" ? lowStockResult.value : null,
  };
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default async function DashboardPage() {
  const { analytics, recentIssues, lowStock } = await fetchDashboardData();

  const kpis = [
    {
      label: "Total de Issues",
      value: analytics?.totalIssues ?? "—",
      icon: AlertCircle,
      iconColor: "text-blue-500",
      bg: "bg-blue-50 dark:bg-blue-950/40",
      href: "/issues",
    },
    {
      label: "Issues Abertas",
      value: analytics?.openIssues ?? "—",
      icon: AlertCircle,
      iconColor: "text-yellow-500",
      bg: "bg-yellow-50 dark:bg-yellow-950/40",
      href: "/issues?status=Open",
    },
    {
      label: "Issues Resolvidas",
      value: analytics?.resolvedIssues ?? "—",
      icon: CheckCircle2,
      iconColor: "text-green-500",
      bg: "bg-green-50 dark:bg-green-950/40",
      href: "/issues?status=Resolved",
    },
    {
      label: "Produtos em Estoque",
      value: analytics?.totalInventoryItems ?? "—",
      icon: Package,
      iconColor: "text-purple-500",
      bg: "bg-purple-50 dark:bg-purple-950/40",
      href: "/inventory",
    },
  ];

  return (
    <div className="space-y-6">
      {/* ── Header ── */}
      <div>
        <h1 className="text-2xl font-bold text-(--text-primary)">Dashboard</h1>
        <p className="text-sm text-(--text-secondary) mt-0.5">
          Visão consolidada do sistema
        </p>
      </div>

      {/* ── KPI Cards ── */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {kpis.map(({ label, value, icon: Icon, iconColor, bg, href }) => (
          <Link key={label} href={href} className="group">
            <Card className="flex items-center gap-4 hover:shadow-md transition-shadow cursor-pointer">
              <div className={`p-3 rounded-lg ${bg} shrink-0`}>
                <Icon size={22} className={iconColor} />
              </div>
              <div className="min-w-0">
                <p className="text-xs text-(--text-secondary) truncate">{label}</p>
                <p className="text-2xl font-bold text-(--text-primary) leading-tight">
                  {value}
                </p>
              </div>
            </Card>
          </Link>
        ))}
      </div>

      {/* ── Bottom Row ── */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Recent Issues */}
        <Card padding={false}>
          <div className="p-5">
            <CardHeader
              title="Issues Recentes"
              action={
                <Link
                  href="/issues"
                  className="text-xs text-blue-500 hover:underline flex items-center gap-0.5"
                >
                  Ver todas <ChevronRight size={12} />
                </Link>
              }
            />
          </div>
          {recentIssues && recentIssues.items.length > 0 ? (
            <ul className="divide-y divide-(--border)">
              {recentIssues.items.map((issue) => (
                <li key={issue.id}>
                  <Link
                    href={`/issues/${issue.id}`}
                    className="flex items-start justify-between gap-3 px-5 py-3 hover:bg-(--bg-app) transition-colors"
                  >
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-(--text-primary) truncate">
                        {issue.title}
                      </p>
                      <p className="text-xs text-(--text-secondary) mt-0.5">
                        {new Date(issue.updatedAt ?? issue.createdAt).toLocaleDateString("pt-BR")}
                      </p>
                    </div>
                    <div className="flex items-center gap-2 shrink-0">
                      <PriorityBadge priority={issue.priority} />
                      <StatusBadge status={issue.status} />
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          ) : (
            <p className="px-5 pb-5 text-sm text-(--text-secondary)">
              Nenhuma issue recente.
            </p>
          )}
        </Card>

        {/* Low Stock Alerts */}
        <Card padding={false}>
          <div className="p-5">
            <CardHeader
              title="Alertas de Estoque Baixo"
              action={
                <Link
                  href="/inventory?stockStatus=LowStock"
                  className="text-xs text-blue-500 hover:underline flex items-center gap-0.5"
                >
                  Ver todos <ChevronRight size={12} />
                </Link>
              }
            />
          </div>
          {lowStock && lowStock.items.length > 0 ? (
            <ul className="divide-y divide-(--border)">
              {lowStock.items.map((product) => (
                <li key={product.id}>
                  <Link
                    href={`/inventory/${product.id}`}
                    className="flex items-center justify-between gap-3 px-5 py-3 hover:bg-(--bg-app) transition-colors"
                  >
                    <div className="min-w-0">
                      <p className="text-sm font-medium text-(--text-primary) truncate">
                        {product.name}
                      </p>
                      <p className="text-xs text-(--text-secondary) mt-0.5">
                        SKU: {product.sku}
                      </p>
                    </div>
                    <div className="flex items-center gap-2 shrink-0">
                      <TrendingDown size={14} className="text-red-500" />
                      <span className="text-sm font-semibold text-red-600">
                        {product.currentStock} un
                      </span>
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          ) : (
            <p className="px-5 pb-5 text-sm text-(--text-secondary)">
              Nenhum alerta de estoque.
            </p>
          )}
        </Card>
      </div>

      {/* ── Quick Actions ── */}
      <div className="flex flex-wrap gap-3">
        <Link
          href="/issues/new"
          className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-colors"
        >
          <Plus size={15} />
          Nova Issue
        </Link>
        <Link
          href="/inventory"
          className="inline-flex items-center gap-2 px-4 py-2 bg-(--bg-surface) border border-(--border) hover:bg-(--bg-app) text-(--text-primary) text-sm font-medium rounded-lg transition-colors"
        >
          <Package size={15} />
          Ajustar Estoque
        </Link>
        <Link
          href="/analytics"
          className="inline-flex items-center gap-2 px-4 py-2 bg-(--bg-surface) border border-(--border) hover:bg-(--bg-app) text-(--text-primary) text-sm font-medium rounded-lg transition-colors"
        >
          <CheckCircle2 size={15} />
          Ver Analytics
        </Link>
      </div>
    </div>
  );
}
