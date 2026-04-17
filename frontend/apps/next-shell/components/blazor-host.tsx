'use client';

import { useEffect, useRef, useState } from 'react';
import Script from 'next/script';

interface BlazorHostProps {
  /** Esperar este tempo (ms) antes de montar custom elements */
  mountDelay?: number;
}

declare global {    interface Window {
      imsAuth: {
        getToken: () => string | null;
      };
      Blazor?: {
        start: (options?: Record<string, unknown>) => Promise<void>;
      };
    }
}

/**
 * BlazorHost — carrega o runtime Blazor WASM de forma lazy.
 * Deve ser montado uma única vez no layout do dashboard.
 * Expõe window.imsAuth.getToken() para que os Custom Elements
 * Blazor possam autenticar chamadas ao BFF.
 */
export function BlazorHost({ mountDelay = 100 }: BlazorHostProps) {
  const initialized = useRef(false);

  useEffect(() => {
    if (initialized.current) return;
    initialized.current = true;

    // Bridge: expõe o token do cookie para o Blazor via JS Interop
    window.imsAuth = {
      getToken: () => {
        // Lê o cookie ims_access_token (não HttpOnly — apenas o público)
        // Para tokens HttpOnly, o BFF retorna o token via endpoint /api/auth/me
        const match = document.cookie.match(/ims_public_token=([^;]+)/);
        return match ? decodeURIComponent(match[1]) : null;
      },
    };
  }, []);

  return (
    <>
      {/* MudBlazor CSS */}
      <link
        rel="stylesheet"
        href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap"
      />
      <link rel="stylesheet" href="/_blazor/_content/MudBlazor/MudBlazor.min.css" />

      {/* Blazor WASM runtime — lazy, só carrega quando o componente monta */}
      <Script
        src="/_blazor/_framework/blazor.webassembly.js"
        strategy="lazyOnload"
        data-no-auto-start="true"
        onLoad={() => {
          // Pequeno delay para garantir que o DOM está pronto para custom elements
          setTimeout(() => {
            // Evita "Blazor has already started" ao renavegar
            if ((window as any).__blazorStarted) return;
            (window as any).__blazorStarted = true;
            window.Blazor?.start().catch(console.error);
          }, mountDelay);
        }}
      />

      {/* MudBlazor JS */}
      <Script
        src="/_blazor/_content/MudBlazor/MudBlazor.min.js"
        strategy="lazyOnload"
      />
    </>
  );
}
