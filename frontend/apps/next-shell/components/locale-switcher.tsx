"use client";

import { useLocale, useTranslations } from "next-intl";
import { useRouter, usePathname } from "next/navigation";
import { Globe } from "lucide-react";
import { routing, type Locale } from "@/i18n/routing";

/**
 * LocaleSwitcher — seletor de idioma PT-BR / EN-US.
 * Persiste a preferência via cookie do next-intl e faz navegação
 * para a mesma rota no novo locale.
 */
export function LocaleSwitcher() {
  const t = useTranslations("locale");
  const locale = useLocale() as Locale;
  const router = useRouter();
  const pathname = usePathname();

  const handleChange = (nextLocale: Locale) => {
    if (nextLocale === locale) return;

    // Remove prefixo de locale atual do pathname, se houver
    const cleanPath = pathname.replace(/^\/(pt|en)/, "") || "/";

    // Navega para a mesma rota no novo locale
    if (nextLocale === routing.defaultLocale) {
      router.push(cleanPath);
    } else {
      router.push(`/${nextLocale}${cleanPath}`);
    }

    router.refresh();
  };

  return (
    <div className="flex items-center gap-1.5 px-2">
      <Globe size={14} className="text-slate-400 shrink-0" />
      <div className="flex rounded-md overflow-hidden border border-slate-600">
        {routing.locales.map((l) => (
          <button
            key={l}
            onClick={() => handleChange(l)}
            className={[
              "px-2 py-0.5 text-xs font-medium transition-colors",
              l === locale
                ? "bg-blue-600 text-white"
                : "text-slate-400 hover:text-white hover:bg-slate-700",
            ].join(" ")}
            title={t(l)}
          >
            {l.toUpperCase()}
          </button>
        ))}
      </div>
    </div>
  );
}
