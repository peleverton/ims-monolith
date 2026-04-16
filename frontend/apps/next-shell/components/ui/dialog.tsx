/**
 * ui/dialog.tsx — US-041
 * Dialog base genérico com suporte a dark mode via CSS vars.
 */

"use client";

import { useEffect, useRef, type ReactNode } from "react";
import { X } from "lucide-react";
import { cn } from "@/lib/utils";

interface DialogProps {
  open: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  children: ReactNode;
  footer?: ReactNode;
  size?: "sm" | "md" | "lg";
}

const sizeClass = {
  sm: "max-w-sm",
  md: "max-w-2xl",
  lg: "max-w-4xl",
};

/**
 * Dialog base reutilizável com suporte a dark mode.
 * Usa `<dialog>` nativo para acessibilidade (foco trap, Esc para fechar).
 *
 * @example
 * <Dialog open={open} onClose={close} title="Novo item">
 *   <p>Conteúdo</p>
 * </Dialog>
 */
export function Dialog({
  open,
  onClose,
  title,
  description,
  children,
  footer,
  size = "md",
}: DialogProps) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    if (open) ref.current?.showModal();
    else ref.current?.close();
  }, [open]);

  return (
    <dialog
      ref={ref}
      onClose={onClose}
      className={cn(
        "w-full rounded-xl shadow-xl p-0 bg-(--bg-surface) text-(--text-primary)",
        "backdrop:bg-black/50",
        "open:flex open:flex-col",
        sizeClass[size]
      )}
    >
      {/* Header */}
      <div className="flex items-start justify-between px-6 py-4 border-b border-(--border) shrink-0">
        <div>
          <h2 className="text-lg font-semibold text-(--text-primary)">{title}</h2>
          {description && (
            <p className="text-sm text-(--text-secondary) mt-0.5">{description}</p>
          )}
        </div>
        <button
          onClick={onClose}
          aria-label="Fechar"
          className="p-1.5 rounded-lg text-(--text-muted) hover:text-(--text-primary) hover:bg-(--bg-subtle) transition-colors"
        >
          <X size={18} />
        </button>
      </div>

      {/* Body */}
      <div className="overflow-y-auto max-h-[75vh] px-6 py-5">{children}</div>

      {/* Footer */}
      {footer && (
        <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-(--border) bg-(--bg-subtle) shrink-0">
          {footer}
        </div>
      )}
    </dialog>
  );
}
