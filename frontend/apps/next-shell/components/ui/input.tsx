/**
 * ui/input.tsx — US-041
 * Input e Textarea base com suporte a dark mode.
 */

import { cn } from "@/lib/utils";
import type { InputHTMLAttributes, TextareaHTMLAttributes } from "react";

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
}

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string;
  error?: string;
}

const baseInputClass =
  "w-full px-3 py-2 rounded-lg border border-(--border-input) bg-(--bg-surface) text-(--text-primary) " +
  "text-sm placeholder:text-(--text-muted) " +
  "focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 " +
  "disabled:opacity-50 disabled:cursor-not-allowed transition-colors";

/** Input base com label e mensagem de erro. */
export function Input({ label, error, className, id, ...props }: InputProps) {
  return (
    <div className="space-y-1">
      {label && (
        <label htmlFor={id} className="block text-sm font-medium text-(--text-primary)">
          {label}
        </label>
      )}
      <input id={id} className={cn(baseInputClass, className)} {...props} />
      {error && <p className="text-xs text-red-500">{error}</p>}
    </div>
  );
}

/** Textarea base com label e mensagem de erro. */
export function Textarea({ label, error, className, id, ...props }: TextareaProps) {
  return (
    <div className="space-y-1">
      {label && (
        <label htmlFor={id} className="block text-sm font-medium text-(--text-primary)">
          {label}
        </label>
      )}
      <textarea
        id={id}
        className={cn(baseInputClass, "resize-none", className)}
        {...props}
      />
      {error && <p className="text-xs text-red-500">{error}</p>}
    </div>
  );
}

/** Select base com label e mensagem de erro. */
export function Select({
  label,
  error,
  className,
  id,
  children,
  ...props
}: InputHTMLAttributes<HTMLSelectElement> & {
  label?: string;
  error?: string;
}) {
  return (
    <div className="space-y-1">
      {label && (
        <label htmlFor={id} className="block text-sm font-medium text-(--text-primary)">
          {label}
        </label>
      )}
      <select
        id={id}
        className={cn(
          baseInputClass,
          "appearance-none cursor-pointer",
          className
        )}
        {...props}
      >
        {children}
      </select>
      {error && <p className="text-xs text-red-500">{error}</p>}
    </div>
  );
}
