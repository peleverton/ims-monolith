'use client';

import React, { useEffect, useRef, useState } from 'react';

interface BlazorElementProps {
  tag: 'inventory-grid' | 'analytics-dashboard';
  apiBaseUrl?: string;
  className?: string;
}

// Declaração para TypeScript reconhecer os custom elements como JSX válido
declare global {
  namespace JSX {
    interface IntrinsicElements {
      'inventory-grid': React.DetailedHTMLProps<
        React.HTMLAttributes<HTMLElement> & { 'api-base-url'?: string },
        HTMLElement
      >;
      'analytics-dashboard': React.DetailedHTMLProps<
        React.HTMLAttributes<HTMLElement> & { 'api-base-url'?: string },
        HTMLElement
      >;
    }
  }
}

/**
 * BlazorElement — wrapper React para Custom Elements Blazor.
 * Usa ssr:false implícito (é 'use client' e verifica window).
 * Mostra skeleton enquanto o WASM carrega.
 */
export function BlazorElement({
  tag,
  apiBaseUrl = '/api/proxy',
  className,
}: BlazorElementProps) {
  const [blazorReady, setBlazorReady] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    // Inicializa o token público antes de montar o custom element
    fetch('/api/auth/me', { credentials: 'include' }).catch(() => {});

    // Polling para saber quando o Blazor WASM terminou de carregar
    const check = setInterval(() => {
      if (typeof window !== 'undefined' && window.Blazor) {
        setBlazorReady(true);
        clearInterval(check);
      }
    }, 200);

    // Timeout de segurança: mostrar mesmo sem confirmação após 8s
    const timeout = setTimeout(() => {
      setBlazorReady(true);
      clearInterval(check);
    }, 8000);

    return () => {
      clearInterval(check);
      clearTimeout(timeout);
    };
  }, []);

  if (!blazorReady) {
    return <BlazorSkeleton />;
  }

  return (
    <div ref={containerRef} className={className}>
      {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
      {React.createElement(tag as any, { 'api-base-url': apiBaseUrl })}
    </div>
  );
}

function BlazorSkeleton() {
  return (
    <div className="bg-white rounded-xl border border-gray-200 p-8 animate-pulse">
      <div className="flex items-center gap-3 mb-6">
        <div className="h-6 w-6 bg-gray-200 rounded" />
        <div className="h-6 w-48 bg-gray-200 rounded" />
        <div className="ml-auto h-9 w-28 bg-gray-200 rounded-lg" />
      </div>
      <div className="space-y-3">
        {[...Array(6)].map((_, i) => (
          <div key={i} className="flex gap-4">
            <div className="h-4 flex-1 bg-gray-100 rounded" />
            <div className="h-4 w-20 bg-gray-100 rounded" />
            <div className="h-4 w-16 bg-gray-100 rounded" />
            <div className="h-4 w-24 bg-gray-100 rounded" />
          </div>
        ))}
      </div>
      <div className="mt-4 text-center text-xs text-gray-400">
        Carregando componente Blazor...
      </div>
    </div>
  );
}
