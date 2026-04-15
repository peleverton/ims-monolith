import { BlazorElement } from "@/components/blazor-element";

export const metadata = { title: "Analytics" };

export default function AnalyticsPage() {
  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Analytics</h1>
        <p className="text-gray-500 text-sm mt-0.5">
          Dashboard de KPIs e gráficos com MudBlazor
        </p>
      </div>
      {/* Custom Element Blazor — <analytics-dashboard> */}
      <BlazorElement tag="analytics-dashboard" apiBaseUrl="/api/proxy" />
    </div>
  );
}
