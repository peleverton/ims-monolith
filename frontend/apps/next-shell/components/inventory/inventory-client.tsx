"use client";

/**
 * InventoryClient — US-038
 *
 * Componente client-side que gerencia todo o CRUD de produtos de inventário.
 * Usa apiFetch (api-client.ts) com refresh token automático.
 */

import { useState, useCallback, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Plus, Pencil, Trash2, RefreshCw, BarChart3 } from "lucide-react";
import { toast } from "sonner";
import { StockStatusBadge } from "@/components/inventory/stock-status-badge";
import { ProductFormDialog } from "@/components/inventory/product-form-dialog";
import { AdjustStockDialog } from "@/components/inventory/adjust-stock-dialog";
import { DeleteConfirmDialog } from "@/components/inventory/delete-confirm-dialog";
import {
  getProducts,
  createProduct,
  updateProduct,
  adjustStock,
  deleteProduct,
  getProductById,
} from "@/lib/api/inventory";
import type {
  ProductListDto,
  ProductDto,
  PagedResult,
  CreateProductRequest,
  UpdateProductRequest,
  AdjustStockRequest,
} from "@/lib/types";

interface Props {
  initialData: PagedResult<ProductListDto> | null;
  searchParams: {
    page?: string;
    category?: string;
    stockStatus?: string;
    search?: string;
  };
}

export function InventoryClient({ initialData, searchParams }: Props) {
  const router = useRouter();
  const [data, setData] = useState(initialData);
  const [isPending, startTransition] = useTransition();

  // Dialog state
  const [formOpen, setFormOpen] = useState(false);
  const [adjustOpen, setAdjustOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const [selectedProduct, setSelectedProduct] = useState<ProductDto | null>(null);
  const [selectedListItem, setSelectedListItem] = useState<ProductListDto | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  // ── Refresh ────────────────────────────────────────────────────────────
  const refreshData = useCallback(async () => {
    try {
      const fresh = await getProducts({
        page: Number(searchParams.page ?? 1),
        pageSize: 15,
        category: searchParams.category,
        stockStatus: searchParams.stockStatus,
        search: searchParams.search,
      });
      setData(fresh);
    } catch {
      toast.error("Erro ao atualizar lista de produtos");
    }
  }, [searchParams]);

  // ── Create ─────────────────────────────────────────────────────────────
  const handleCreate = async (values: CreateProductRequest) => {
    await createProduct(values);
    toast.success("Produto criado com sucesso!");
    setFormOpen(false);
    await refreshData();
  };

  // ── Edit ───────────────────────────────────────────────────────────────
  const openEdit = async (item: ProductListDto) => {
    try {
      const full = await getProductById(item.id);
      setSelectedProduct(full);
      setFormOpen(true);
    } catch {
      toast.error("Erro ao carregar produto");
    }
  };

  const handleUpdate = async (values: UpdateProductRequest) => {
    if (!selectedProduct) return;
    await updateProduct(selectedProduct.id, values);
    toast.success("Produto atualizado com sucesso!");
    setFormOpen(false);
    setSelectedProduct(null);
    await refreshData();
  };

  // ── Adjust Stock ───────────────────────────────────────────────────────
  const openAdjust = (item: ProductListDto) => {
    setSelectedListItem(item);
    setAdjustOpen(true);
  };

  const handleAdjust = async (values: AdjustStockRequest) => {
    if (!selectedListItem) return;
    await adjustStock(selectedListItem.id, values);
    toast.success("Estoque ajustado com sucesso!");
    setAdjustOpen(false);
    setSelectedListItem(null);
    await refreshData();
  };

  // ── Delete ─────────────────────────────────────────────────────────────
  const openDelete = (item: ProductListDto) => {
    setSelectedListItem(item);
    setDeleteOpen(true);
  };

  const handleDelete = async () => {
    if (!selectedListItem) return;
    setIsDeleting(true);
    try {
      await deleteProduct(selectedListItem.id);
      toast.success("Produto excluído com sucesso!");
      setDeleteOpen(false);
      setSelectedListItem(null);
      await refreshData();
    } finally {
      setIsDeleting(false);
    }
  };

  // ── Navigation ─────────────────────────────────────────────────────────
  const navigate = (params: Record<string, string | undefined>) => {
    const qs = new URLSearchParams();
    const merged = { ...searchParams, ...params };
    Object.entries(merged).forEach(([k, v]) => {
      if (v) qs.set(k, v);
    });
    startTransition(() => router.push(`/inventory?${qs}`));
  };

  const CATEGORIES = [
    "Electronics",
    "Food",
    "Beverages",
    "Clothing",
    "Furniture",
    "Books",
    "Toys",
    "Sports",
    "Tools",
    "Automotive",
    "Health",
    "Medical",
    "Beauty",
    "Home",
    "Garden",
    "Office",
    "Pet",
    "Baby",
    "Other",
  ];
  const STOCK_STATUSES = ["InStock", "LowStock", "OutOfStock", "Overstock", "Discontinued"];

  return (
    <>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-(--text-primary)">Inventário</h1>
          <p className="text-(--text-secondary) text-sm mt-0.5">
            {data ? `${data.totalCount} produtos` : "Carregando..."}
          </p>
        </div>
        <button
          onClick={() => {
            setSelectedProduct(null);
            setFormOpen(true);
          }}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-500 transition-colors"
        >
          <Plus size={16} />
          Novo Produto
        </button>
      </div>

      {/* Filtros */}
      <form
        className="flex flex-wrap gap-3 mb-4"
        onSubmit={(e) => {
          e.preventDefault();
          const fd = new FormData(e.currentTarget);
          navigate({
            search: (fd.get("search") as string) || undefined,
            category: (fd.get("category") as string) || undefined,
            stockStatus: (fd.get("stockStatus") as string) || undefined,
            page: "1",
          });
        }}
      >
        <input
          name="search"
          defaultValue={searchParams.search}
          placeholder="Buscar por nome, SKU..."
          className="flex-1 min-w-48 px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <select
          name="category"
          defaultValue={searchParams.category ?? ""}
          className="px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">Todas as categorias</option>
          {CATEGORIES.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </select>
        <select
          name="stockStatus"
          defaultValue={searchParams.stockStatus ?? ""}
          className="px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <option value="">Todos os status</option>
          {STOCK_STATUSES.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
        <button
          type="submit"
          className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-500 transition-colors"
        >
          Filtrar
        </button>
      </form>

      {/* Tabela */}
      {!data ? (
        <div className="bg-(--bg-surface) rounded-xl border border-(--border) p-8 text-center text-(--text-secondary)">
          Erro ao carregar produtos. Verifique se o servidor está rodando.
        </div>
      ) : data.items.length === 0 ? (
        <div className="bg-(--bg-surface) rounded-xl border border-(--border) p-12 text-center">
          <p className="text-(--text-secondary)">Nenhum produto encontrado.</p>
        </div>
      ) : (
        <div className="bg-(--bg-surface) rounded-xl border border-(--border) overflow-hidden shadow-sm">
          <table className="w-full text-sm">
            <thead className="bg-(--bg-subtle) border-b border-(--border)">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">
                  Nome / SKU
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">
                  Categoria
                </th>
                <th className="px-4 py-3 text-right text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">
                  Estoque
                </th>
                <th className="px-4 py-3 text-right text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">
                  Preço
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-(--text-secondary) uppercase tracking-wide">
                  Status
                </th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className={`divide-y divide-(--border) ${isPending ? "opacity-50" : ""}`}>
              {data.items.map((item) => (
                <tr key={item.id} className="hover:bg-(--bg-subtle) transition-colors">
                  <td className="px-4 py-3">
                    <div className="font-medium text-(--text-primary) truncate max-w-xs">
                      {item.name}
                    </div>
                    <div className="text-xs text-(--text-muted) font-mono">{item.sku}</div>
                  </td>
                  <td className="px-4 py-3 text-(--text-secondary)">{item.category}</td>
                  <td className="px-4 py-3 text-right font-mono text-(--text-primary)">
                    {item.currentStock}
                  </td>
                  <td className="px-4 py-3 text-right text-(--text-primary)">
                    {item.unitPrice.toLocaleString("pt-BR", {
                      style: "currency",
                      currency: "BRL",
                    })}
                  </td>
                  <td className="px-4 py-3">
                    <StockStatusBadge status={item.stockStatus} />
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center justify-end gap-1">
                      <button
                        onClick={() => openAdjust(item)}
                        title="Ajustar estoque"
                        className="p-1.5 rounded-lg text-(--text-muted) hover:text-blue-600 hover:bg-blue-50 transition-colors"
                      >
                        <RefreshCw size={15} />
                      </button>
                      <button
                        onClick={() => router.push(`/inventory/${item.id}`)}
                        title="Ver movimentações"
                        className="p-1.5 rounded-lg text-(--text-muted) hover:text-purple-600 hover:bg-purple-50 transition-colors"
                      >
                        <BarChart3 size={15} />
                      </button>
                      <button
                        onClick={() => openEdit(item)}
                        title="Editar"
                        className="p-1.5 rounded-lg text-(--text-muted) hover:text-green-600 hover:bg-green-50 transition-colors"
                      >
                        <Pencil size={15} />
                      </button>
                      <button
                        onClick={() => openDelete(item)}
                        title="Excluir"
                        className="p-1.5 rounded-lg text-(--text-muted) hover:text-red-600 hover:bg-red-50 transition-colors"
                      >
                        <Trash2 size={15} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Paginação */}
          <div className="px-4 py-3 border-t border-(--border) flex items-center justify-between text-sm text-(--text-secondary)">
            <span>
              Página {data.pageNumber} de {data.totalPages}
            </span>
            <div className="flex gap-2">
              {data.pageNumber > 1 && (
                <button
                  onClick={() => navigate({ page: String(data.pageNumber - 1) })}
                  className="px-3 py-1 rounded border border-(--border-input) text-(--text-primary) hover:bg-(--bg-subtle)"
                >
                  Anterior
                </button>
              )}
              {data.pageNumber < data.totalPages && (
                <button
                  onClick={() => navigate({ page: String(data.pageNumber + 1) })}
                  className="px-3 py-1 rounded border border-(--border-input) text-(--text-primary) hover:bg-(--bg-subtle)"
                >
                  Próxima
                </button>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Dialogs */}
      <ProductFormDialog
        open={formOpen}
        product={selectedProduct}
        onClose={() => {
          setFormOpen(false);
          setSelectedProduct(null);
        }}
        onSubmit={selectedProduct ? handleUpdate : handleCreate}
      />

      <AdjustStockDialog
        open={adjustOpen}
        productName={selectedListItem?.name}
        currentStock={selectedListItem?.currentStock}
        onClose={() => {
          setAdjustOpen(false);
          setSelectedListItem(null);
        }}
        onSubmit={handleAdjust}
      />

      <DeleteConfirmDialog
        open={deleteOpen}
        productName={selectedListItem?.name}
        isDeleting={isDeleting}
        onClose={() => {
          setDeleteOpen(false);
          setSelectedListItem(null);
        }}
        onConfirm={handleDelete}
      />
    </>
  );
}
