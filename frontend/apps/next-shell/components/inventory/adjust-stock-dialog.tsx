"use client";

/**
 * AdjustStockDialog — US-038
 *
 * Dialog para ajuste de estoque de um produto.
 */

import { useEffect, useRef } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { X } from "lucide-react";

const MOVEMENT_TYPES = [
  { value: "StockIn", label: "Entrada" },
  { value: "StockOut", label: "Saída" },
  { value: "Adjustment", label: "Ajuste" },
  { value: "Return", label: "Devolução" },
  { value: "Damage", label: "Avaria" },
  { value: "Loss", label: "Perda" },
  { value: "Purchase", label: "Compra" },
  { value: "Sale", label: "Venda" },
] as const;

const schema = z.object({
  quantity: z.preprocess(Number, z.number().int().min(1, "Quantidade ≥ 1")),
  movementType: z.enum(["StockIn", "StockOut", "Adjustment", "Return", "Damage", "Loss", "Purchase", "Sale"]),
  reference: z.string().optional(),
  notes: z.string().optional(),
});

type FormValues = {
  quantity: number;
  movementType: "StockIn" | "StockOut" | "Adjustment" | "Return" | "Damage" | "Loss" | "Purchase" | "Sale";
  reference?: string;
  notes?: string;
};

interface Props {
  open: boolean;
  productName?: string;
  currentStock?: number;
  onClose: () => void;
  onSubmit: (data: FormValues) => Promise<void>;
}

export function AdjustStockDialog({
  open,
  productName,
  currentStock,
  onClose,
  onSubmit,
}: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema) as import("react-hook-form").Resolver<FormValues>,
    defaultValues: { movementType: "StockIn", quantity: 1 },
  });

  useEffect(() => {
    if (open) {
      reset({ movementType: "StockIn", quantity: 1 });
      dialogRef.current?.showModal();
    } else {
      dialogRef.current?.close();
    }
  }, [open, reset]);

  return (
    <dialog
      ref={dialogRef}
      onClose={onClose}
      className="w-full max-w-md rounded-xl shadow-xl p-0 backdrop:bg-black/40 open:flex open:flex-col"
    >
      <div className="flex items-center justify-between px-6 py-4 border-b border-(--border)">
        <div>
          <h2 className="text-lg font-semibold text-(--text-primary)">Ajustar Estoque</h2>
          {productName && (
            <p className="text-sm text-(--text-secondary) mt-0.5">
              {productName}
              {currentStock !== undefined && (
                <span className="ml-2 font-medium text-(--text-primary)">
                  (atual: {currentStock})
                </span>
              )}
            </p>
          )}
        </div>
        <button
          onClick={onClose}
          className="p-1 rounded-lg text-(--text-muted) hover:text-(--text-primary) hover:bg-(--bg-subtle) transition-colors"
          aria-label="Fechar"
        >
          <X size={20} />
        </button>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="px-6 py-5 space-y-4">
        <div>
          <label className="block text-sm font-medium text-(--text-primary) mb-1">
            Tipo de movimentação <span className="text-red-500">*</span>
          </label>
          <select
            {...register("movementType")}
            className="w-full px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            {MOVEMENT_TYPES.map((m) => (
              <option key={m.value} value={m.value}>
                {m.label}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-(--text-primary) mb-1">
            Quantidade <span className="text-red-500">*</span>
          </label>
          <input
            type="number"
            min={1}
            {...register("quantity")}
            className="w-full px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          {errors.quantity && (
            <p className="mt-1 text-xs text-red-500">{errors.quantity.message}</p>
          )}
        </div>

        <div>
          <label className="block text-sm font-medium text-(--text-primary) mb-1">
            Referência / NF
          </label>
          <input
            {...register("reference")}
            placeholder="NF-001, PO-002..."
            className="w-full px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-(--text-primary) mb-1">
            Observações
          </label>
          <textarea
            {...register("notes")}
            rows={2}
            className="w-full px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
          />
        </div>

        <div className="flex items-center justify-end gap-3 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-(--text-primary) bg-(--bg-surface) border border-(--border-input) rounded-lg hover:bg-(--bg-subtle) transition-colors"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-500 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSubmitting ? "Salvando..." : "Confirmar ajuste"}
          </button>
        </div>
      </form>
    </dialog>
  );
}
