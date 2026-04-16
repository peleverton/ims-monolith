/**
 * Inventory Page — US-038
 *
 * Server Component: busca dados iniciais via api-fetch (server-side com token),
 * repassa ao InventoryClient para CRUD interativo.
 */

import { apiFetch } from "@/lib/api-fetch";
import type { PagedResult, ProductListDto } from "@/lib/types";
import { InventoryClient } from "@/components/inventory/inventory-client";

export const metadata = { title: "Inventário" };

interface SearchParams {
  page?: string;
  category?: string;
  stockStatus?: string;
  search?: string;
}

async function fetchProducts(params: SearchParams) {
  const qs = new URLSearchParams({ page: params.page ?? "1", pageSize: "15" });
  if (params.category) qs.set("category", params.category);
  if (params.stockStatus) qs.set("stockStatus", params.stockStatus);
  if (params.search) qs.set("search", params.search);
  return apiFetch<PagedResult<ProductListDto>>(
    `/api/inventory/products?${qs}`
  ).catch(() => null);
}

export default async function InventoryPage({
  searchParams,
}: {
  searchParams: Promise<SearchParams>;
}) {
  const params = await searchParams;
  const data = await fetchProducts(params);

  return (
    <div>
      <InventoryClient initialData={data} searchParams={params} />
    </div>
  );
}
