import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { jwtVerify } from "jose";
import createMiddleware from "next-intl/middleware";
import { routing } from "./i18n/routing";

const AUTH_COOKIE = "ims_access_token";
const secret = new TextEncoder().encode(
  process.env.AUTH_SECRET ?? "fallback-secret-change-me"
);

const PUBLIC_ROUTES = ["/login", "/register", "/api/auth", "/api/health"];

// Middleware de i18n do next-intl
const intlMiddleware = createMiddleware(routing);

export async function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Rotas de API e assets — apenas next-intl não é necessário
  if (pathname.startsWith("/api/") || pathname.startsWith("/_blazor")) {
    return NextResponse.next();
  }

  // Rotas públicas — aplicar i18n sem verificação de auth
  const isPublic = PUBLIC_ROUTES.some((r) =>
    pathname.replace(/^\/(pt|en)/, "").startsWith(r) || pathname.startsWith(r)
  );
  if (isPublic) return intlMiddleware(request);

  // Verificar autenticação
  const token = request.cookies.get(AUTH_COOKIE)?.value;
  if (!token) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("callbackUrl", pathname);
    return NextResponse.redirect(loginUrl);
  }

  try {
    await jwtVerify(token, secret);
    // Auth OK — aplicar i18n
    return intlMiddleware(request);
  } catch {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("callbackUrl", pathname);
    const response = NextResponse.redirect(loginUrl);
    response.cookies.delete(AUTH_COOKIE);
    return response;
  }
}

export const config = {
  matcher: [
    "/((?!_next/static|_next/image|favicon.ico|_blazor).*)",
  ],
};
