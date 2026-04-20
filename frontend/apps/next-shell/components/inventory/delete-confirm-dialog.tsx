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
      <div className="flex items-center justify-between px-6 py-4 border-b border-(--border)">
        <h2 className="text-lg font-semibold text-(--text-primary)">Excluir produto</h2>
        <button
          onClick={onClose}
          className="p-1 rounded-lg text-(--text-muted) hover:text-(--text-primary) hover:bg-(--bg-subtle) transition-colors"
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
            <p className="text-sm text-(--text-secondary)">
              Tem certeza que deseja excluir{" "}
              <span className="font-semibold text-(--text-primary)">{productName ?? "este produto"}</span>?
            </p>
            <p className="mt-1 text-xs text-(--text-muted)">
              Esta ação não pode ser desfeita.
            </p>
          </div>
        </div>
      </div>

      <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-(--border) bg-(--bg-subtle)">
        <button
          onClick={onClose}
          className="px-4 py-2 text-sm font-medium text-(--text-primary) bg-(--bg-surface) border border-(--border-input) rounded-lg hover:bg-(--bg-subtle) transition-colors"
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
