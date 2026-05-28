/**
 * US-090: Keycloak PKCE callback handler.
 *
 * GET /api/auth/keycloak-callback?code=...&state=...
 *
 * 1. Validates state (CSRF protection)
 * 2. Retrieves code_verifier from HttpOnly cookie
 * 3. Exchanges authorization code for tokens via Keycloak token endpoint
 * 4. Stores access + refresh tokens as HttpOnly cookies (same model as custom JWT)
 * 5. Redirects to callbackUrl (default: /issues)
 */
import { NextRequest, NextResponse } from "next/server";
import { getKeycloakConfig, exchangeCodeForTokens } from "@/lib/keycloak";

const PKCE_COOKIE  = "ims_pkce_verifier";
const STATE_COOKIE = "ims_pkce_state";
const AUTH_COOKIE  = "ims_access_token";
const REFRESH_COOKIE = "ims_refresh_token";

export async function GET(request: NextRequest) {
  const { searchParams } = request.nextUrl;
  const code  = searchParams.get("code");
  const state = searchParams.get("state");
  const error = searchParams.get("error");

  // Handle Keycloak errors (e.g., user cancelled login)
  if (error) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("reason", "keycloak_error");
    loginUrl.searchParams.set("error", error);
    return NextResponse.redirect(loginUrl);
  }

  if (!code || !state) {
    return NextResponse.json({ message: "Missing code or state" }, { status: 400 });
  }

  // Validate PKCE verifier cookie
  const codeVerifier = request.cookies.get(PKCE_COOKIE)?.value;
  if (!codeVerifier) {
    return NextResponse.json({ message: "PKCE verifier missing or expired" }, { status: 400 });
  }

  // Validate state + extract callbackUrl
  const stateCookie = request.cookies.get(STATE_COOKIE)?.value;
  const [storedState, encodedCallbackUrl] = (stateCookie ?? ":").split(":", 2);
  if (state !== storedState) {
    return NextResponse.json({ message: "State mismatch — possible CSRF" }, { status: 400 });
  }
  const callbackUrl = encodedCallbackUrl ? decodeURIComponent(encodedCallbackUrl) : "/issues";

  const config = getKeycloakConfig();

  let tokens;
  try {
    tokens = await exchangeCodeForTokens(config, code, codeVerifier);
  } catch (err) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("reason", "token_exchange_failed");
    return NextResponse.redirect(loginUrl);
  }

  const isSecure = config.redirectUri.startsWith("https://");
  const accessMaxAge = tokens.expires_in ?? 3600;
  const refreshMaxAge = 60 * 60 * 24 * 7; // 7 days default
  const cookieOpts = (maxAge: number) =>
    `HttpOnly; Path=/; SameSite=Lax; Max-Age=${maxAge}${isSecure ? "; Secure" : ""}`;

  const response = NextResponse.redirect(new URL(callbackUrl, request.url));

  // Set auth cookies — same format the rest of the app reads
  response.headers.append("Set-Cookie", `${AUTH_COOKIE}=${tokens.access_token}; ${cookieOpts(accessMaxAge)}`);
  if (tokens.refresh_token) {
    response.headers.append("Set-Cookie", `${REFRESH_COOKIE}=${tokens.refresh_token}; ${cookieOpts(refreshMaxAge)}`);
  }

  // Clear PKCE cookies (single-use)
  const clearOpts = "HttpOnly; Path=/; Max-Age=0";
  response.headers.append("Set-Cookie", `${PKCE_COOKIE}=; ${clearOpts}`);
  response.headers.append("Set-Cookie", `${STATE_COOKIE}=; ${clearOpts}`);

  return response;
}
