import { BlazorElement } from "@/components/blazor-element";

export const metadata = { title: "Inventário" };

export default function InventoryPage() {
  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Inventário</h1>
        <p className="text-gray-500 text-sm mt-0.5">
          Grid interativo com MudBlazor
        </p>
      </div>
      {/* Custom Element Blazor — <inventory-grid> registrado via RegisterCustomElement */}
      <BlazorElement tag="inventory-grid" apiBaseUrl="/api/proxy" />
    </div>
  );
}
