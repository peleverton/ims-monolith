/**
 * US-090: Keycloak PKCE (Proof Key for Code Exchange) utilities for the BFF.
 *
 * Flow:
 *  1. /api/auth/keycloak-login   — generates code_verifier + challenge, redirects to Keycloak
 *  2. Keycloak redirects back to /api/auth/keycloak-callback?code=...&state=...
 *  3. BFF exchanges code for tokens (using PKCE verifier stored in a short-lived cookie)
 *  4. Tokens stored as HttpOnly cookies — same session model as custom JWT auth
 */

export interface KeycloakConfig {
  authority: string;    // http://localhost:8180/realms/ims
  clientId: string;     // ims-frontend
  redirectUri: string;  // http://localhost:3000/api/auth/keycloak-callback
}

export interface KeycloakTokenResponse {
  access_token: string;
  refresh_token?: string;
  expires_in: number;
  token_type: string;
  id_token?: string;
}

/** Returns Keycloak config from environment variables (server-side only). */
export function getKeycloakConfig(): KeycloakConfig {
  const authority =
    process.env.KEYCLOAK_AUTHORITY ??
    process.env.NEXT_PUBLIC_KEYCLOAK_AUTHORITY ??
    "http://localhost:8180/realms/ims";
  const clientId =
    process.env.KEYCLOAK_CLIENT_ID ??
    process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID ??
    "ims-frontend";
  const appUrl =
    process.env.NEXTAUTH_URL ?? process.env.NEXT_PUBLIC_APP_URL ?? "http://localhost:3000";

  return {
    authority,
    clientId,
    redirectUri: `${appUrl}/api/auth/keycloak-callback`,
  };
}

/** Returns true when the Keycloak feature flag is active (env var). */
export function isKeycloakEnabled(): boolean {
  return (
    process.env.NEXT_PUBLIC_USE_KEYCLOAK === "true" ||
    process.env.USE_KEYCLOAK === "true"
  );
}

// ── PKCE helpers ──────────────────────────────────────────────────────────────

/** Generates a cryptographically random code verifier (RFC 7636). */
export function generateCodeVerifier(): string {
  const array = new Uint8Array(32);
  crypto.getRandomValues(array);
  return base64UrlEncode(array);
}

/** Derives the code challenge from the verifier (S256 method). */
export async function generateCodeChallenge(verifier: string): Promise<string> {
  const data = new TextEncoder().encode(verifier);
  const digest = await crypto.subtle.digest("SHA-256", data);
  return base64UrlEncode(new Uint8Array(digest));
}

function base64UrlEncode(buffer: Uint8Array): string {
  return btoa(String.fromCharCode(...buffer))
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/, "");
}

/** Builds the Keycloak authorization URL for the PKCE flow. */
export async function buildAuthorizationUrl(
  config: KeycloakConfig,
  codeChallenge: string,
  state: string,
  scope = "openid profile email"
): Promise<string> {
  const params = new URLSearchParams({
    response_type: "code",
    client_id: config.clientId,
    redirect_uri: config.redirectUri,
    scope,
    state,
    code_challenge: codeChallenge,
    code_challenge_method: "S256",
  });
  return `${config.authority}/protocol/openid-connect/auth?${params.toString()}`;
}

/** Exchanges the authorization code for tokens via the token endpoint. */
export async function exchangeCodeForTokens(
  config: KeycloakConfig,
  code: string,
  codeVerifier: string
): Promise<KeycloakTokenResponse> {
  const tokenEndpoint = `${config.authority}/protocol/openid-connect/token`;

  const body = new URLSearchParams({
    grant_type: "authorization_code",
    client_id: config.clientId,
    redirect_uri: config.redirectUri,
    code,
    code_verifier: codeVerifier,
  });

  const response = await fetch(tokenEndpoint, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: body.toString(),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(`Token exchange failed: ${response.status} — ${error}`);
  }

  return response.json() as Promise<KeycloakTokenResponse>;
}

/** Refreshes tokens using a Keycloak refresh token. */
export async function refreshKeycloakTokens(
  config: KeycloakConfig,
  refreshToken: string
): Promise<KeycloakTokenResponse> {
  const tokenEndpoint = `${config.authority}/protocol/openid-connect/token`;

  const body = new URLSearchParams({
    grant_type: "refresh_token",
    client_id: config.clientId,
    refresh_token: refreshToken,
  });

  const response = await fetch(tokenEndpoint, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: body.toString(),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(`Token refresh failed: ${response.status} — ${error}`);
  }

  return response.json() as Promise<KeycloakTokenResponse>;
}
