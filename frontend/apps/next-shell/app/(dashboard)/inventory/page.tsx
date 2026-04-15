export const metadata = { title: "Inventário" };

export default function InventoryPage() {
  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Inventário</h1>
        <p className="text-gray-500 text-sm mt-0.5">
          Grid interativo — componente Blazor (US-032)
        </p>
      </div>

      {/* Placeholder — será substituído pelo Custom Element Blazor na US-034 */}
      <div className="bg-white rounded-xl border-2 border-dashed border-blue-200 p-12 text-center">
        <div className="text-4xl mb-3">🔷</div>
        <p className="text-blue-700 font-semibold">Blazor Component: &lt;inventory-grid&gt;</p>
        <p className="text-gray-500 text-sm mt-1">
          Será integrado na US-031 / US-034 — MudDataGrid com CRUD completo
        </p>
        <div className="mt-4 px-4 py-2 bg-blue-50 rounded-lg inline-block text-xs text-blue-600 font-mono">
          {'<inventory-grid api-base-url="/api/proxy" />'}
        </div>
      </div>
    </div>
  );
}
