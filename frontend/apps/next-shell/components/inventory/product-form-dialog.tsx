"use client";

/**
 * ProductFormDialog — US-038
 *
 * Dialog para criar ou editar um produto de inventário.
 * Usa react-hook-form + zod para validação.
 */

import { useEffect, useRef } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { X } from "lucide-react";
import type { ProductDto } from "@/lib/types";

const CATEGORIES = [
  "Electronics",
  "Machinery",
  "RawMaterial",
  "Consumable",
  "Furniture",
  "Tool",
  "Spare",
  "Other",
] as const;

const schema = z.object({
  name: z.string().min(2, "Nome obrigatório"),
  sku: z.string().min(1, "SKU obrigatório"),
  category: z.enum(CATEGORIES),
  description: z.string().optional(),
  barcode: z.string().optional(),
  minimumStockLevel: z.preprocess(Number, z.number().int().min(0, "Mínimo ≥ 0")),
  maximumStockLevel: z.preprocess(Number, z.number().int().min(1, "Máximo ≥ 1")),
  unitPrice: z.preprocess(Number, z.number().min(0, "Preço ≥ 0")),
  costPrice: z.preprocess(Number, z.number().min(0, "Custo ≥ 0")),
  unit: z.string().min(1, "Unidade obrigatória"),
  currency: z.string().min(1, "Moeda obrigatória"),
});

type FormValues = {
  name: string;
  sku: string;
  category: (typeof CATEGORIES)[number];
  description?: string;
  barcode?: string;
  minimumStockLevel: number;
  maximumStockLevel: number;
  unitPrice: number;
  costPrice: number;
  unit: string;
  currency: string;
};

interface Props {
  open: boolean;
  product?: ProductDto | null;
  onClose: () => void;
  onSubmit: (data: FormValues) => Promise<void>;
}

export function ProductFormDialog({ open, product, onClose, onSubmit }: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema) as import("react-hook-form").Resolver<FormValues>,
    defaultValues: {
      unit: "un",
      currency: "BRL",
      minimumStockLevel: 0,
      maximumStockLevel: 100,
      unitPrice: 0,
      costPrice: 0,
    },
  });

  // Sincroniza com produto existente ao editar
  useEffect(() => {
    if (product) {
      reset({
        name: product.name,
        sku: product.sku,
        category: product.category as (typeof CATEGORIES)[number],
        description: product.description ?? "",
        barcode: product.barcode ?? "",
        minimumStockLevel: product.minimumStockLevel,
        maximumStockLevel: product.maximumStockLevel,
        unitPrice: product.unitPrice,
        costPrice: product.costPrice,
        unit: product.unit,
        currency: product.currency,
      });
    } else {
      reset({
        unit: "un",
        currency: "BRL",
        minimumStockLevel: 0,
        maximumStockLevel: 100,
        unitPrice: 0,
        costPrice: 0,
      });
    }
  }, [product, reset]);

  useEffect(() => {
    if (open) dialogRef.current?.showModal();
    else dialogRef.current?.close();
  }, [open]);

  const handleClose = () => {
    onClose();
  };

  const handleFormSubmit = async (values: FormValues) => {
    await onSubmit(values);
  };

  return (
    <dialog
      ref={dialogRef}
      onClose={handleClose}
      className="w-full max-w-2xl rounded-xl shadow-xl p-0 backdrop:bg-black/40 open:flex open:flex-col"
    >
      {/* Header */}
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
        <h2 className="text-lg font-semibold text-gray-900">
          {product ? "Editar Produto" : "Novo Produto"}
        </h2>
        <button
          onClick={handleClose}
          className="p-1 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors"
          aria-label="Fechar"
        >
          <X size={20} />
        </button>
      </div>

      {/* Form */}
      <form
        onSubmit={handleSubmit(handleFormSubmit)}
        className="overflow-y-auto max-h-[80vh]"
      >
        <div className="px-6 py-5 grid grid-cols-1 sm:grid-cols-2 gap-4">
          {/* Nome */}
          <div className="sm:col-span-2">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Nome <span className="text-red-500">*</span>
            </label>
            <input
              {...register("name")}
              className="w-full px-3 py-2 rounded-lg border border-gray-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Nome do produto"
            />
            {errors.name && (
              <p className="mt-1 text-xs text-red-500">{errors.name.message}</p>
            )}
          </div>

          {/* SKU */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              SKU <span className="text-red-500">*</span>
            </label>
            <input
              {...register("sku")}
              disabled={!!product}
              className="w-full px-3 py-2 rounded-lg border border-gray-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-50 disabled:text-gray-500"
              placeholder="EX-0001"
            />
            {errors.sku && (
              <p className="mt-1 text-xs text-red-500">{errors.sku.message}</p>
            )}
          </div>

          {/* Categoria */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Categoria <span className="text-red-500">*</span>
            </label>
            <select
              {...register("category")}
              className="w-full px-3 py-2 rounded-lg border border-gray-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              {CATEGORIES.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
            {errors.category && (
              <p className="mt-1 text-xs text-red-500">
                {errors.category.message}
              </p>
            )}
          </div>

          {/* Barcode */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Código de barras
            </label>
            <input
              {...register("barcode")}
              className="w-full px-3 py-2 rounded-lg border border-gray-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="123456789"
            />
          </div>

          {/* Unidade */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Unidade <span className="text-red-500">*</span>
            </label>
            <input
              {...register("unit")}
              className="w-full px-3 py-2 rounded-lg border border-gray-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="un, kg, l..."
            />
          </div>

          {/* Estoque mínimo */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Estoque mínimo <span className="text-red-500">*</span>
            </label>
            <input
              type="number"
              {...register("minimumStockLevel")}
              className="w-full px-3 py-2 rounded-lg border border-gray-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            {errors.minimumStockLevel && (
              <p className="mt-1 text-xs text-red-500">
                {errors.minimumStockLevel.message}
              </p>
            )}
          </div>

          {/* Estoque máximo */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Estoque máximo <span className="text-red-500">*</span>
            </label>
            <input
              type="number"
              {...register("maximumStockLevel")}
              className="w-full px-3 py-2 rounded-lg border border-gray-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            {errors.maximumStockLevel && (
              <p className="mt-1 text-xs text-red-500">
                {errors.maximumStockLevel.message}
              </p>
            )}
          </div>

          {/* Preço de venda */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Preço de venda (R$) <span className="text-red-500">*</span>
            </label>
            <input
              type="number"
              step="0.01"
              {...register("unitPrice")}
              className="w-full px-3 py-2 rounded-lg border border-gray-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            {errors.unitPrice && (
              <p className="mt-1 text-xs text-red-500">
                {errors.unitPrice.message}
              </p>
            )}
          </div>

          {/* Preço de custo */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Preço de custo (R$) <span className="text-red-500">*</span>
            </label>
            <input
              type="number"
              step="0.01"
              {...register("costPrice")}
              className="w-full px-3 py-2 rounded-lg border border-gray-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
            {errors.costPrice && (
              <p className="mt-1 text-xs text-red-500">
                {errors.costPrice.message}
              </p>
            )}
          </div>

          {/* Descrição */}
          <div className="sm:col-span-2">
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Descrição
            </label>
            <textarea
              {...register("description")}
              rows={3}
              className="w-full px-3 py-2 rounded-lg border border-gray-300 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
              placeholder="Descrição opcional do produto..."
            />
          </div>
        </div>

        {/* Footer */}
        <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-gray-200 bg-gray-50">
          <button
            type="button"
            onClick={handleClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-500 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSubmitting ? "Salvando..." : product ? "Salvar alterações" : "Criar produto"}
          </button>
        </div>
      </form>
    </dialog>
  );
}
