import { BlazorElement } from "@/components/blazor-element";
import { ExportButton } from "@/components/analytics/export-button";

export const metadata = { title: "Analytics" };

export default function AnalyticsPage() {
  return (
    <div>
      <div className="mb-6 flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            Analytics
          </h1>
          <p className="text-gray-500 dark:text-gray-400 text-sm mt-0.5">
            Dashboard de KPIs e gráficos com MudBlazor
          </p>
        </div>

        {/* US-063: Export analytics data as JSON or CSV */}
        <ExportButton />
      </div>

      {/* Custom Element Blazor — <analytics-dashboard> */}
      <BlazorElement tag="analytics-dashboard" apiBaseUrl="/api/proxy" />
    </div>
  );
}
