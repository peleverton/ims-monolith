/**
 * StockStatusBadge — US-038
 * Exibe o status de estoque do produto com cores semânticas.
 */

import type { StockStatus } from "@/lib/types";

const CONFIG: Record<StockStatus, { label: string; className: string }> = {
  InStock: {
    label: "Normal",
    className: "bg-green-100 text-green-700 border-green-200",
  },
  LowStock: {
    label: "Baixo",
    className: "bg-yellow-100 text-yellow-700 border-yellow-200",
  },
  OutOfStock: {
    label: "Sem estoque",
    className: "bg-red-100 text-red-700 border-red-200",
  },
  Overstock: {
    label: "Excesso",
    className: "bg-blue-100 text-blue-700 border-blue-200",
  },
  Discontinued: {
    label: "Descontinuado",
    className: "bg-gray-100 text-gray-600 border-gray-200",
  },
};

interface Props {
  status: string;
}

export function StockStatusBadge({ status }: Props) {
  const cfg = CONFIG[status as StockStatus] ?? {
    label: status,
    className: "bg-gray-100 text-gray-600 border-gray-200",
  };
  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${cfg.className}`}
    >
      {cfg.label}
    </span>
  );
}
