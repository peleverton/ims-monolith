/**
 * lib/api/inventory.ts — US-038
 *
 * Funções client-side para o módulo de Inventário.
 * Usa apiFetch (api-client.ts) com interceptor de refresh token.
 * Todas as chamadas passam pelo BFF proxy: /api/proxy/inventory/*
 */

import { apiFetch } from "@/lib/api-client";
import type {
  ProductDto,
  ProductListDto,
  CreateProductRequest,
  UpdateProductRequest,
  AdjustStockRequest,
  StockMovementDto,
  SupplierListDto,
  LocationListDto,
  PagedResult,
} from "@/lib/types";

const BASE = "/api/proxy/inventory";

// ── Products ────────────────────────────────────────────────────────────────

export interface GetProductsParams {
  page?: number;
  pageSize?: number;
  category?: string;
  stockStatus?: string;
  search?: string;
  locationId?: string;
  supplierId?: string;
}

export function getProducts(params: GetProductsParams = {}) {
  const qs = new URLSearchParams();
  if (params.page) qs.set("page", String(params.page));
  if (params.pageSize) qs.set("pageSize", String(params.pageSize));
  if (params.category) qs.set("category", params.category);
  if (params.stockStatus) qs.set("stockStatus", params.stockStatus);
  if (params.search) qs.set("search", params.search);
  if (params.locationId) qs.set("locationId", params.locationId);
  if (params.supplierId) qs.set("supplierId", params.supplierId);
  return apiFetch<PagedResult<ProductListDto>>(`${BASE}/products?${qs}`);
}

export function getProductById(id: string) {
  return apiFetch<ProductDto>(`${BASE}/products/${id}`);
}

export function createProduct(data: CreateProductRequest) {
  return apiFetch<ProductDto>(`${BASE}/products`, {
    method: "POST",
    body: JSON.stringify(data),
  });
}

export function updateProduct(id: string, data: UpdateProductRequest) {
  return apiFetch<ProductDto>(`${BASE}/products/${id}`, {
    method: "PUT",
    body: JSON.stringify(data),
  });
}

export function adjustStock(id: string, data: AdjustStockRequest) {
  return apiFetch<void>(`${BASE}/products/${id}/stock/adjust`, {
    method: "PATCH",
    body: JSON.stringify(data),
  });
}

export function discontinueProduct(id: string) {
  return apiFetch<void>(`${BASE}/products/${id}/discontinue`, {
    method: "PATCH",
  });
}

export function deleteProduct(id: string) {
  return apiFetch<void>(`${BASE}/products/${id}`, {
    method: "DELETE",
  });
}

// ── Stock Movements ─────────────────────────────────────────────────────────

export interface GetMovementsParams {
  page?: number;
  pageSize?: number;
  productId?: string;
  movementType?: string;
}

export function getStockMovements(params: GetMovementsParams = {}) {
  const qs = new URLSearchParams();
  if (params.page) qs.set("page", String(params.page));
  if (params.pageSize) qs.set("pageSize", String(params.pageSize));
  if (params.productId) qs.set("productId", params.productId);
  if (params.movementType) qs.set("movementType", params.movementType);
  return apiFetch<PagedResult<StockMovementDto>>(`${BASE}/stock-movements?${qs}`);
}

// ── Suppliers ───────────────────────────────────────────────────────────────

export function getSuppliers(page = 1, pageSize = 100) {
  return apiFetch<PagedResult<SupplierListDto>>(
    `${BASE}/suppliers?page=${page}&pageSize=${pageSize}`
  );
}

// ── Locations ───────────────────────────────────────────────────────────────

export function getLocations(page = 1, pageSize = 100) {
  return apiFetch<PagedResult<LocationListDto>>(
    `${BASE}/locations?page=${page}&pageSize=${pageSize}`
  );
}
