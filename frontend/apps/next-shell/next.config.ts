import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin("./i18n/request.ts");

const IMS_API_URL = process.env.IMS_API_URL ?? "http://localhost:5049";

const nextConfig: NextConfig = {
  // Habilita output standalone para Docker (copia apenas os arquivos necessários)
  output: "standalone",
  async rewrites() {
    return [
      // /api/proxy/** is handled by app/api/proxy/[...path]/route.ts (authenticated BFF)
      // Proxy SignalR hubs to backend
      {
        source: "/hubs/:path*",
        destination: `${IMS_API_URL}/hubs/:path*`,
      },
      // NOTE: Blazor WASM static files are served directly from public/_blazor/
      // Do NOT proxy /_blazor/ to backend — Next.js serves them as static files.
    ];
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
    ];
  },
};

export default withNextIntl(nextConfig);
