import { NextRequest, NextResponse } from "next/server";

// Rotas públicas — não requerem autenticação
const PUBLIC_PATHS = ["/login", "/register", "/api/auth", "/api/health"];

function isPublicPath(pathname: string): boolean {
  return PUBLIC_PATHS.some(
    (p) => pathname === p || pathname.startsWith(p + "/")
  );
}

export default async function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Static files and API routes — pass through immediately
  if (
    pathname.startsWith("/_next") ||
    pathname.startsWith("/api/") ||
    pathname.startsWith("/hubs/") ||
    pathname.startsWith("/_framework/") ||
    pathname.startsWith("/_content/") ||
    pathname.includes(".")
  ) {
    return NextResponse.next();
  }

  // Public routes — pass through without any redirect
  if (isPublicPath(pathname)) {
    return NextResponse.next();
  }

  // Check authentication via cookie
  const token = request.cookies.get("ims_access_token")?.value;

  if (!token) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("callbackUrl", pathname);
    return NextResponse.redirect(loginUrl);
  }

  // Authenticated — pass through, let Next.js handle routing normally
  return NextResponse.next();
}

export const config = {
  matcher: [
    "/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)",
  ],
};
