/**
 * ui/card.tsx — US-041
 * Card base genérico com suporte a dark mode via CSS vars.
 */

import { cn } from "@/lib/utils";
import type { ReactNode } from "react";

interface CardProps {
  children: ReactNode;
  className?: string;
  padding?: boolean;
}

/** Card surface com bordas e sombra, responde ao tema dark/light automaticamente. */
export function Card({ children, className, padding = true }: CardProps) {
  return (
    <div
      className={cn(
        "bg-(--bg-surface) border border-(--border) rounded-xl shadow-sm",
        padding && "p-5",
        className
      )}
    >
      {children}
    </div>
  );
}

/** Cabeçalho de Card com título e slot opcional de ação */
export function CardHeader({
  title,
  action,
  className,
}: {
  title: string;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <div className={cn("flex items-center justify-between mb-4", className)}>
      <h2 className="text-sm font-semibold text-(--text-primary)">{title}</h2>
      {action}
    </div>
  );
}
