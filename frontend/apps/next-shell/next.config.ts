import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin("./i18n/request.ts");

const IMS_API_URL = process.env.IMS_API_URL ?? "http://localhost:5049";

const nextConfig: NextConfig = {
  // Habilita output standalone para Docker (copia apenas os arquivos necessários)
  output: "standalone",
  async rewrites() {
    return {
      // beforeFiles: run BEFORE static files are checked.
      // Required so that /_framework/* and /_content/* resolve to the Blazor
      // artifacts in public/_blazor/ before Next.js returns 404 for those paths.
      // Blazor WASM uses document.baseURI (not import.meta.url) to construct
      // resource URLs, so all internal fetches come in as /_framework/* and
      // /_content/* — these rewrites map them back to the correct public path.
      beforeFiles: [
        // Absolute paths (Blazor loaded from /_framework/)
        {
          source: "/_framework/:path*",
          destination: "/_blazor/_framework/:path*",
        },
        {
          source: "/_content/:path*",
          destination: "/_blazor/_content/:path*",
        },
        // Relative paths: Blazor resolves _content/* and _framework/* using
        // document.baseURI (the current page URL). When the user is on /analytics,
        // /issues, etc., those requests arrive as /:page*/_content/* — catch them all.
        {
          source: "/:prefix*/_framework/:path*",
          destination: "/_blazor/_framework/:path*",
        },
        {
          source: "/:prefix*/_content/:path*",
          destination: "/_blazor/_content/:path*",
        },
      ],
      afterFiles: [
        // /api/proxy/** is handled by app/api/proxy/[...path]/route.ts (authenticated BFF)
        // Proxy SignalR hubs to backend
        {
          source: "/hubs/:path*",
          destination: `${IMS_API_URL}/hubs/:path*`,
        },
      ],
      fallback: [],
    };
  },
  async headers() {
    return [
      {
        source: "/(.*)",
        headers: [
          { key: "X-Frame-Options", value: "DENY" },
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
        ],
      },
      // US-072: Cache-Control immutable for Blazor WASM assets (fingerprinted, never change)
      {
        source: "/_blazor/:path*",
        headers: [
          { key: "Cache-Control", value: "public, max-age=31536000, immutable" },
        ],
      },
      // US-072: Serve .br files with correct Content-Encoding so browsers decompress them
      {
        source: "/_blazor/:path*.wasm.br",
        headers: [
          { key: "Content-Encoding", value: "br" },
          { key: "Content-Type", value: "application/wasm" },
        ],
      },
      {
        source: "/_blazor/:path*.dll.br",
        headers: [
          { key: "Content-Encoding", value: "br" },
          { key: "Content-Type", value: "application/octet-stream" },
        ],
      },
      {
        source: "/_blazor/:path*.js.br",
        headers: [
          { key: "Content-Encoding", value: "br" },
          { key: "Content-Type", value: "application/javascript" },
        ],
      },
    ];
  },
};

export default withNextIntl(nextConfig);
