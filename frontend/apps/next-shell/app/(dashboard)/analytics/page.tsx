export const metadata = { title: "Analytics" };

export default function AnalyticsPage() {
  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Analytics</h1>
        <p className="text-gray-500 text-sm mt-0.5">
          Dashboard de KPIs e gráficos — componente Blazor (US-033)
        </p>
      </div>

      {/* Placeholder — será substituído pelo Custom Element Blazor na US-034 */}
      <div className="bg-white rounded-xl border-2 border-dashed border-purple-200 p-12 text-center">
        <div className="text-4xl mb-3">📊</div>
        <p className="text-purple-700 font-semibold">Blazor Component: &lt;analytics-dashboard&gt;</p>
        <p className="text-gray-500 text-sm mt-1">
          Será integrado na US-031 / US-034 — MudChart com KPIs em tempo real
        </p>
        <div className="mt-4 px-4 py-2 bg-purple-50 rounded-lg inline-block text-xs text-purple-600 font-mono">
          {'<analytics-dashboard api-base-url="/api/proxy" />'}
        </div>
      </div>
    </div>
  );
}
