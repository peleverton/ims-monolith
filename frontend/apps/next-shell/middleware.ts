import createMiddleware from "next-intl/middleware";
import { NextRequest, NextResponse } from "next/server";
import { routing } from "./i18n/routing";

const intlMiddleware = createMiddleware(routing);

// Rotas públicas — não requerem autenticação
const PUBLIC_PATHS = ["/login", "/register", "/api/auth"];

function isPublicPath(pathname: string): boolean {
  // Remove o prefixo de locale se presente (/en/login → /login)
  const stripped = pathname.replace(/^\/(pt|en)/, "") || "/";
  return PUBLIC_PATHS.some(
    (p) => stripped === p || stripped.startsWith(p + "/")
  );
}

export default async function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Deixa arquivos estáticos e rotas de API passarem
  if (
    pathname.startsWith("/_next") ||
    pathname.startsWith("/api/") ||
    pathname.includes(".")
  ) {
    return NextResponse.next();
  }

  // Se é uma rota pública, aplica apenas i18n sem checar autenticação
  if (isPublicPath(pathname)) {
    return intlMiddleware(request) ?? NextResponse.next();
  }

  // Verifica autenticação via cookie ANTES do i18n redirect
  const token = request.cookies.get("ims_access_token")?.value;

  if (!token) {
    // Redireciona direto para /login sem prefixo de locale
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("callbackUrl", pathname);
    return NextResponse.redirect(loginUrl);
  }

  // Autenticado — aplica middleware de i18n normalmente
  return intlMiddleware(request) ?? NextResponse.next();
}

export const config = {
  matcher: [
    // Aplica em todas as rotas exceto arquivos estáticos e internos do Next.js
    "/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)",
  ],
};
