/**
 * app/api/proxy/[...path]/route.ts
 *
 * Authenticated BFF proxy: repassa todas as requisições /api/proxy/** ao IMS backend
 * injetando o Bearer token do cookie HttpOnly no header Authorization.
 * Suporta GET, POST, PUT, PATCH, DELETE.
 */

import { NextRequest, NextResponse } from "next/server";
import { cookies } from "next/headers";

const IMS_API = process.env.IMS_API_URL ?? "http://localhost:5049";
const AUTH_COOKIE = "ims_access_token";

async function proxyRequest(
  request: NextRequest,
  params: { path: string[] }
): Promise<NextResponse> {
  const cookieStore = await cookies();
  const token = cookieStore.get(AUTH_COOKIE)?.value;

  if (!token) {
    console.warn(`[proxy] No auth token in cookie for ${request.method} ${request.nextUrl.pathname}`);
  }

  // Monta URL destino: /api/proxy/issues/123 → http://app:8080/api/issues/123
  const path = params.path.join("/");
  const search = request.nextUrl.search;
  const targetUrl = `${IMS_API}/api/${path}${search}`;

  // Headers para repassar ao backend
  const headers: Record<string, string> = {
    "Content-Type": request.headers.get("Content-Type") ?? "application/json",
  };
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  // Repassar correlation id se existir
  const correlationId = request.headers.get("X-Correlation-Id");
  if (correlationId) headers["X-Correlation-Id"] = correlationId;

  // US-043: propagar W3C Trace Context para correlação frontend ↔ backend no Jaeger
  const traceparent = request.headers.get("traceparent");
  if (traceparent) headers["traceparent"] = traceparent;
  const tracestate = request.headers.get("tracestate");
  if (tracestate) headers["tracestate"] = tracestate;

  // Body para métodos que enviam payload
  let body: BodyInit | null = null;
  const method = request.method;
  if (method !== "GET" && method !== "HEAD" && method !== "DELETE") {
    body = await request.text();
  }

  let upstream: Response;
  try {
    upstream = await fetch(targetUrl, { method, headers, body });
  } catch (err) {
    console.error(`[proxy] Upstream fetch failed: ${targetUrl}`, err);
    return NextResponse.json(
      { error: "Upstream unavailable" },
      { status: 503 }
    );
  }

  // Repassar resposta
  const responseHeaders = new Headers();
  const contentType = upstream.headers.get("Content-Type");
  if (contentType) responseHeaders.set("Content-Type", contentType);

  if (upstream.status === 204) {
    return new NextResponse(null, { status: 204, headers: responseHeaders });
  }

  const responseBody = await upstream.text();
  return new NextResponse(responseBody, {
    status: upstream.status,
    headers: responseHeaders,
  });
}

export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  return proxyRequest(request, await params);
}

export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  return proxyRequest(request, await params);
}

export async function PUT(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  return proxyRequest(request, await params);
}

export async function PATCH(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  return proxyRequest(request, await params);
}

export async function DELETE(
  request: NextRequest,
  { params }: { params: Promise<{ path: string[] }> }
) {
  return proxyRequest(request, await params);
}
