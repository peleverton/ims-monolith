import { NextRequest, NextResponse } from "next/server";
import { checkRateLimit, RATE_LIMIT_CONFIGS } from "@/lib/rate-limiter";

// Rotas públicas — não requerem autenticação
const PUBLIC_PATHS = ["/login", "/register", "/api/auth", "/api/health"];

function isPublicPath(pathname: string): boolean {
  return PUBLIC_PATHS.some(
    (p) => pathname === p || pathname.startsWith(p + "/")
  );
}

function getClientIp(request: NextRequest): string {
  return (
    request.headers.get("x-forwarded-for")?.split(",")[0].trim() ??
    request.headers.get("x-real-ip") ??
    "unknown"
  );
}

function rateLimitResponse(resetAt: number, limit: number): NextResponse {
  const retryAfter = Math.ceil((resetAt - Date.now()) / 1000);
  return NextResponse.json(
    { error: "Too Many Requests", retryAfter },
    {
      status: 429,
      headers: {
        "X-RateLimit-Limit": String(limit),
        "X-RateLimit-Remaining": "0",
        "Retry-After": String(retryAfter),
      },
    }
  );
}

export default async function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Static files and passthrough paths — skip rate limiting
  if (
    pathname.startsWith("/_next") ||
    pathname.startsWith("/hubs/") ||
    pathname.startsWith("/_framework/") ||
    pathname.startsWith("/_content/") ||
    pathname.includes(".")
  ) {
    return NextResponse.next();
  }

  const ip = getClientIp(request);

  // ── US-076: Rate Limiting ──────────────────────────────────
  // Auth endpoints: strictest limit (10 req/min per IP)
  if (pathname.startsWith("/api/auth")) {
    const result = checkRateLimit(`auth:${ip}`, RATE_LIMIT_CONFIGS.auth);
    if (!result.allowed) return rateLimitResponse(result.resetAt, result.limit);
    const response = NextResponse.next();
    response.headers.set("X-RateLimit-Limit", String(result.limit));
    response.headers.set("X-RateLimit-Remaining", String(result.remaining));
    return response;
  }

  // Authenticated users: generous limit (500 req/min per userId)
  const token = request.cookies.get("ims_access_token")?.value;
  if (token) {
    const userKey = `user:${token.slice(0, 32)}`;
    const result = checkRateLimit(userKey, RATE_LIMIT_CONFIGS.user);
    if (!result.allowed) return rateLimitResponse(result.resetAt, result.limit);
    const response = NextResponse.next();
    response.headers.set("X-RateLimit-Limit", String(result.limit));
    response.headers.set("X-RateLimit-Remaining", String(result.remaining));
    return response;
  }

  // Public (unauthenticated) requests: 100 req/min per IP
  const result = checkRateLimit(`public:${ip}`, RATE_LIMIT_CONFIGS.public);
  if (!result.allowed) return rateLimitResponse(result.resetAt, result.limit);

  // ── Auth check (page routes only) ─────────────────────────
  if (!pathname.startsWith("/api/") && !isPublicPath(pathname)) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("callbackUrl", pathname);
    return NextResponse.redirect(loginUrl);
  }

  const response = NextResponse.next();
  response.headers.set("X-RateLimit-Limit", String(result.limit));
  response.headers.set("X-RateLimit-Remaining", String(result.remaining));
  return response;
}

export const config = {
  matcher: [
    "/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)",
  ],
};
