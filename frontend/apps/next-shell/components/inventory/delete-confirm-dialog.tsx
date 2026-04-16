"use client";

/**
 * DeleteConfirmDialog — US-038
 *
 * Dialog de confirmação para deletar um produto.
 */

import { useEffect, useRef } from "react";
import { AlertTriangle, X } from "lucide-react";

interface Props {
  open: boolean;
  productName?: string;
  isDeleting?: boolean;
  onClose: () => void;
  onConfirm: () => Promise<void>;
}

export function DeleteConfirmDialog({
  open,
  productName,
  isDeleting,
  onClose,
  onConfirm,
}: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    if (open) dialogRef.current?.showModal();
    else dialogRef.current?.close();
  }, [open]);

  return (
    <dialog
      ref={dialogRef}
      onClose={onClose}
      className="w-full max-w-sm rounded-xl shadow-xl p-0 backdrop:bg-black/40 open:flex open:flex-col"
    >
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
        <h2 className="text-lg font-semibold text-gray-900">Excluir produto</h2>
        <button
          onClick={onClose}
          className="p-1 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors"
          aria-label="Fechar"
        >
          <X size={20} />
        </button>
      </div>

      <div className="px-6 py-5">
        <div className="flex items-start gap-4">
          <div className="shrink-0 w-10 h-10 rounded-full bg-red-100 flex items-center justify-center">
            <AlertTriangle size={20} className="text-red-600" />
          </div>
          <div>
            <p className="text-sm text-gray-700">
              Tem certeza que deseja excluir{" "}
              <span className="font-semibold">{productName ?? "este produto"}</span>?
            </p>
            <p className="mt-1 text-xs text-gray-500">
              Esta ação não pode ser desfeita.
            </p>
          </div>
        </div>
      </div>

      <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-gray-200 bg-gray-50">
        <button
          onClick={onClose}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
        >
          Cancelar
        </button>
        <button
          onClick={onConfirm}
          disabled={isDeleting}
          className="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-lg hover:bg-red-500 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isDeleting ? "Excluindo..." : "Excluir"}
        </button>
      </div>
    </dialog>
  );
}
