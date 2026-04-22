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
    if ((window as any).__blazorStarted) {
      setBlazorAvailable(true);
      return;
    }

    // Probe before loading any Blazor script — avoids the uncaught promise
    // error from blazor.webassembly.js that freezes input event handlers.
    fetch('/_blazor/_framework/blazor.webassembly.js', { method: 'HEAD' })
      .then((r) => setBlazorAvailable(r.ok))
      .catch(() => setBlazorAvailable(false));
  }, []);

  // Don't render Blazor scripts at all when the framework files are absent.
  if (blazorAvailable === false) return null;
  if (blazorAvailable === null) return null; // still probing

  // If already started, no need to inject scripts again.
  if ((window as any).__blazorStarted) return null;

  return (
    <>
      <link
        rel="stylesheet"
        href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap"
      />
      <link rel="stylesheet" href="/_blazor/_content/MudBlazor/MudBlazor.min.css" />

      <Script
        src="/_blazor/_framework/blazor.webassembly.js"
        strategy="lazyOnload"
        data-no-auto-start="true"
        onLoad={() => {
          setTimeout(async () => {
            if ((window as any).__blazorStarted) return;
            (window as any).__blazorStarted = true;
            window.Blazor?.start({
              loadBootResource: (
                _type: string,
                filename: string,
                defaultUri: string,
                _integrity: string
              ) => {
                // Rewrite relative _framework/* URLs to use the correct /_blazor/ prefix
                if (defaultUri.includes('_framework/')) {
                  return `/_blazor/_framework/${filename}`;
                }
                return defaultUri;
              },
            }).catch((err: unknown) => {
              console.warn('[BlazorHost] Blazor start failed:', err);
              (window as any).__blazorStarted = false;
            });
          }, mountDelay);
        }}
        onError={() => {
          console.warn('[BlazorHost] blazor.webassembly.js failed to load.');
        }}
      />

      <Script
        src="/_blazor/_content/MudBlazor/MudBlazor.min.js"
        strategy="lazyOnload"
      />
    </>
  );
}
