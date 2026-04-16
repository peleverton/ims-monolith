"use client";

/**
 * ThemeToggle — US-041
 *
 * Botão que alterna entre light, dark e system.
 * Exibe ícone correspondente ao tema atual.
 */

import { useTheme } from "next-themes";
import { useEffect, useState } from "react";
import { Sun, Moon, Monitor } from "lucide-react";
import { cn } from "@/lib/utils";

export function ThemeToggle({ className }: { className?: string }) {
  const { theme, setTheme } = useTheme();
  const [mounted, setMounted] = useState(false);

  // Evita hidration mismatch
  useEffect(() => setMounted(true), []);

  if (!mounted) {
    return (
      <div className={cn("w-8 h-8 rounded-lg", className)} />
    );
  }

  const cycles: Array<{ value: string; icon: React.ReactNode; label: string }> = [
    { value: "light", icon: <Sun size={16} />, label: "Tema claro" },
    { value: "dark", icon: <Moon size={16} />, label: "Tema escuro" },
    { value: "system", icon: <Monitor size={16} />, label: "Tema do sistema" },
  ];

  const current = cycles.find((c) => c.value === theme) ?? cycles[2];
  const next = cycles[(cycles.indexOf(current) + 1) % cycles.length];

  return (
    <button
      onClick={() => setTheme(next.value)}
      title={`Alternar para: ${next.label}`}
      aria-label={`Tema atual: ${current.label}. Clique para ${next.label}`}
      className={cn(
        "flex items-center justify-center w-8 h-8 rounded-lg transition-colors",
        "text-slate-400 hover:text-white hover:bg-slate-700",
        className
      )}
    >
      {current.icon}
    </button>
  );
}
