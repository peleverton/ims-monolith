/**
 * US-090: Keycloak PKCE login initiator.
 *
 * GET /api/auth/keycloak-login
 *
 * 1. Generates PKCE code_verifier + code_challenge (S256)
 * 2. Stores code_verifier in a short-lived HttpOnly cookie (60s TTL)
 * 3. Redirects the browser to the Keycloak authorization endpoint
 */
import { NextRequest, NextResponse } from "next/server";
import {
  getKeycloakConfig,
  generateCodeVerifier,
  generateCodeChallenge,
  buildAuthorizationUrl,
} from "@/lib/keycloak";

const PKCE_COOKIE = "ims_pkce_verifier";
const STATE_COOKIE = "ims_pkce_state";

export async function GET(request: NextRequest) {
  const config = getKeycloakConfig();

  const codeVerifier = generateCodeVerifier();
  const codeChallenge = await generateCodeChallenge(codeVerifier);

  // Random state to prevent CSRF
  const stateBytes = new Uint8Array(16);
  crypto.getRandomValues(stateBytes);
  const state = Buffer.from(stateBytes).toString("hex");

  // callbackUrl to redirect after successful auth
  const callbackUrl = request.nextUrl.searchParams.get("callbackUrl") ?? "/issues";

  const authUrl = await buildAuthorizationUrl(config, codeChallenge, state);

  const response = NextResponse.redirect(authUrl);

  const isSecure = config.redirectUri.startsWith("https://");
  const cookieOpts = `HttpOnly; Path=/; SameSite=Lax; Max-Age=120${isSecure ? "; Secure" : ""}`;

  // Store verifier + callbackUrl in cookie (120s TTL — enough for user to authenticate)
  response.headers.append("Set-Cookie", `${PKCE_COOKIE}=${codeVerifier}; ${cookieOpts}`);
  response.headers.append("Set-Cookie", `${STATE_COOKIE}=${state}:${encodeURIComponent(callbackUrl)}; ${cookieOpts}`);

  return response;
}
