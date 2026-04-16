"use client";

/**
 * ThemeProvider — US-041
 *
 * Wrapper de next-themes para light/dark mode.
 * Aplica a classe `dark` no <html> com transição suave.
 *
 * @see https://github.com/pacocoursey/next-themes
 */

import { ThemeProvider as NextThemesProvider } from "next-themes";
import type { ReactNode } from "react";

interface Props {
  children: ReactNode;
}

export function ThemeProvider({ children }: Props) {
  return (
    <NextThemesProvider
      attribute="class"
      defaultTheme="system"
      enableSystem
      disableTransitionOnChange={false}
    >
      {children}
    </NextThemesProvider>
  );
}
