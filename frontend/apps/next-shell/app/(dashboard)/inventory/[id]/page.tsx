/**
 * Inventory Product Detail — US-038
 *
 * Exibe detalhes do produto e histórico de movimentações de estoque.
 */

import { apiFetch } from "@/lib/api-fetch";
import type { ProductDto, PagedResult, StockMovementDto } from "@/lib/types";
import { StockStatusBadge } from "@/components/inventory/stock-status-badge";
import Link from "next/link";
import { ArrowLeft, Package } from "lucide-react";

export const metadata = { title: "Produto — Inventário" };

interface Props {
  params: Promise<{ id: string }>;
}

async function fetchProduct(id: string) {
  return apiFetch<ProductDto>(`/api/inventory/products/${id}`).catch(() => null);
}

async function fetchMovements(productId: string) {
  return apiFetch<PagedResult<StockMovementDto>>(
    `/api/inventory/stock-movements?productId=${productId}&pageSize=20`
  ).catch(() => null);
}

export default async function InventoryProductPage({ params }: Props) {
  const { id } = await params;
  const [product, movements] = await Promise.all([
    fetchProduct(id),
    fetchMovements(id),
  ]);

  if (!product) {
    return (
      <div className="py-12 text-center text-gray-500">
        Produto não encontrado.
      </div>
    );
  }

  return (
    <div>
      {/* Breadcrumb */}
      <div className="mb-6">
        <Link
          href="/dashboard/inventory"
          className="inline-flex items-center gap-1.5 text-sm text-gray-500 hover:text-gray-700 mb-4"
        >
          <ArrowLeft size={15} />
          Voltar ao inventário
        </Link>

        <div className="flex items-start gap-4">
          <div className="w-12 h-12 rounded-xl bg-blue-100 flex items-center justify-center shrink-0">
            <Package size={22} className="text-blue-600" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">{product.name}</h1>
            <div className="flex items-center gap-3 mt-1">
              <span className="text-sm text-gray-400 font-mono">{product.sku}</span>
              <StockStatusBadge status={product.stockStatus} />
              {!product.isActive && (
                <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-500 border border-gray-200">
                  Descontinuado
                </span>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Info Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
        <Card label="Estoque atual" value={String(product.currentStock)} unit={product.unit} />
        <Card label="Estoque mínimo" value={String(product.minimumStockLevel)} unit={product.unit} />
        <Card label="Estoque máximo" value={String(product.maximumStockLevel)} unit={product.unit} />
        <Card
          label="Preço de venda"
          value={product.unitPrice.toLocaleString("pt-BR", {
            style: "currency",
            currency: product.currency,
          })}
        />
      </div>

      {/* Details */}
      <div className="bg-white rounded-xl border border-gray-200 p-5 mb-6 shadow-sm">
        <h2 className="text-sm font-semibold text-gray-700 mb-3">Detalhes</h2>
        <dl className="grid grid-cols-2 sm:grid-cols-3 gap-x-6 gap-y-3 text-sm">
          <Detail label="Categoria" value={product.category} />
          <Detail label="Unidade" value={product.unit} />
          <Detail label="Moeda" value={product.currency} />
          <Detail
            label="Preço de custo"
            value={product.costPrice.toLocaleString("pt-BR", {
              style: "currency",
              currency: product.currency,
            })}
          />
          {product.barcode && <Detail label="Código de barras" value={product.barcode} />}
          {product.expiryDate && (
            <Detail
              label="Validade"
              value={new Date(product.expiryDate).toLocaleDateString("pt-BR")}
            />
          )}
          {product.description && (
            <div className="col-span-2 sm:col-span-3">
              <dt className="text-gray-500">Descrição</dt>
              <dd className="text-gray-800 mt-0.5">{product.description}</dd>
            </div>
          )}
        </dl>
      </div>

      {/* Stock Movements */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden shadow-sm">
        <div className="px-5 py-4 border-b border-gray-200">
          <h2 className="text-sm font-semibold text-gray-700">
            Histórico de movimentações
          </h2>
        </div>

        {!movements || movements.items.length === 0 ? (
          <div className="p-8 text-center text-gray-500 text-sm">
            Nenhuma movimentação registrada.
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">
                  Tipo
                </th>
                <th className="px-4 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">
                  Qtd
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">
                  Referência
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">
                  Observações
                </th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">
                  Data
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {movements.items.map((m) => (
                <tr key={m.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3">
                    <MovementTypeBadge type={m.movementType} />
                  </td>
                  <td className="px-4 py-3 text-right font-mono text-gray-700">
                    {m.quantity}
                  </td>
                  <td className="px-4 py-3 text-gray-600">{m.reference ?? "—"}</td>
                  <td className="px-4 py-3 text-gray-500 max-w-xs truncate">
                    {m.notes ?? "—"}
                  </td>
                  <td className="px-4 py-3 text-gray-500">
                    {new Date(m.movementDate).toLocaleString("pt-BR")}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

function Card({
  label,
  value,
  unit,
}: {
  label: string;
  value: string;
  unit?: string;
}) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 p-4 shadow-sm">
      <p className="text-xs text-gray-500">{label}</p>
      <p className="text-xl font-bold text-gray-900 mt-1">
        {value}
        {unit && <span className="text-sm font-normal text-gray-400 ml-1">{unit}</span>}
      </p>
    </div>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-gray-500">{label}</dt>
      <dd className="text-gray-800 font-medium mt-0.5">{value}</dd>
    </div>
  );
}

const MOVEMENT_COLORS: Record<string, string> = {
  In: "bg-green-100 text-green-700",
  Out: "bg-red-100 text-red-700",
  Adjustment: "bg-blue-100 text-blue-700",
  Transfer: "bg-purple-100 text-purple-700",
  Return: "bg-yellow-100 text-yellow-700",
  Damage: "bg-orange-100 text-orange-700",
  Loss: "bg-gray-100 text-gray-600",
};

const MOVEMENT_LABELS: Record<string, string> = {
  In: "Entrada",
  Out: "Saída",
  Adjustment: "Ajuste",
  Transfer: "Transferência",
  Return: "Devolução",
  Damage: "Avaria",
  Loss: "Perda",
};

function MovementTypeBadge({ type }: { type: string }) {
  const cls = MOVEMENT_COLORS[type] ?? "bg-gray-100 text-gray-600";
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${cls}`}>
      {MOVEMENT_LABELS[type] ?? type}
    </span>
  );
}
