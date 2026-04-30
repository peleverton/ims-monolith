'use client';

import { useEffect, useRef, useState } from 'react';
import Script from 'next/script';

interface BlazorHostProps {
  mountDelay?: number;
}

declare global {
  interface Window {
    imsAuth: {
      getToken: () => string | null;
    };
    Blazor?: {
      start: (options?: Record<string, unknown>) => Promise<void>;
    };
  }
}

export function BlazorHost({ mountDelay = 100 }: BlazorHostProps) {
  const initialized = useRef(false);
  const [blazorAvailable, setBlazorAvailable] = useState<boolean | null>(null);

  useEffect(() => {
    if (initialized.current) return;
    initialized.current = true;

    window.imsAuth = {
      getToken: () => {
        const match = document.cookie.match(/ims_public_token=([^;]+)/);
        return match ? decodeURIComponent(match[1]) : null;
      },
    };

    // If Blazor already started in a previous render (SPA navigation), skip probe.
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    if ((window as any).__blazorStarted) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setBlazorAvailable(true);
      return;
    }

    // Probe using the rewrite path (/_framework/*) — if rewrite is working the
    // file will be served from public/_blazor/_framework/.
    fetch('/_framework/blazor.webassembly.js', { method: 'HEAD' })
      .then((r) => setBlazorAvailable(r.ok))
      .catch(() => setBlazorAvailable(false));
  }, []);

  if (blazorAvailable === false) return null;
  if (blazorAvailable === null) return null;

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  if ((window as any).__blazorStarted) return null;

  return (
    <>
      <link
        rel="stylesheet"
        href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap"
      />
      {/* Use /_content/ rewrite path so MudBlazor CSS sub-resources resolve correctly */}
      <link rel="stylesheet" href="/_content/MudBlazor/MudBlazor.min.css" />

      {/*
        Load via /_framework/ (rewritten by next.config to /_blazor/_framework/).
        This ensures import.meta.url = /_framework/blazor.webassembly.js so all
        internal dynamic imports (dotnet.js, dotnet.native.*.js, etc.) resolve to
        /_framework/* — which next.config already rewrites to /_blazor/_framework/*.
        The tempBase trick does NOT work for ES module import() resolution.
      */}
      <Script
        src="/_framework/blazor.webassembly.js"
        strategy="lazyOnload"
        data-no-auto-start="true"
        onLoad={() => {
          setTimeout(async () => {
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            if ((window as any).__blazorStarted) return;
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            (window as any).__blazorStarted = true;

            try {
              await window.Blazor?.start({});
            } catch (err: unknown) {
              console.warn('[BlazorHost] Blazor start failed:', err);
              // eslint-disable-next-line @typescript-eslint/no-explicit-any
              (window as any).__blazorStarted = false;
            }
          }, mountDelay);
        }}
        onError={() => {
          console.warn('[BlazorHost] blazor.webassembly.js failed to load.');
        }}
      />

      <Script
        src="/_content/MudBlazor/MudBlazor.min.js"
        strategy="lazyOnload"
      />
    </>
  );
}
