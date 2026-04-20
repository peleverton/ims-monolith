import { SignJWT } from "jose";
import { cookies } from "next/headers";

const AUTH_COOKIE = "ims_access_token";
const REFRESH_COOKIE = "ims_refresh_token";

export interface SessionPayload {
  userId: string;
  username: string;
  email: string;
  roles: string[];
  exp?: number;
}

/** Lê e valida o token JWT do cookie HttpOnly */
export async function getSession(): Promise<SessionPayload | null> {
  const cookieStore = await cookies();
  const token = cookieStore.get(AUTH_COOKIE)?.value;
  if (!token) return null;

  try {
    // Decode JWT payload without verifying signature
    // (signature is verified by the backend on every API request)
    const parts = token.split(".");
    if (parts.length !== 3) return null;
    const payloadJson = Buffer.from(parts[1], "base64url").toString("utf-8");
    const p = JSON.parse(payloadJson) as Record<string, unknown>;

    // Check expiry
    const exp = p["exp"] as number | undefined;
    if (exp && Date.now() / 1000 > exp) return null;
    const userId =
      (p["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] as string) ??
      (p["sub"] as string) ?? "";
    const username =
      (p["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] as string) ??
      (p["name"] as string) ?? "";
    const email =
      (p["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] as string) ??
      (p["email"] as string) ?? "";
    const rawRoles =
      p["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ??
      p["role"] ?? [];
    const roles = Array.isArray(rawRoles) ? (rawRoles as string[]) : [rawRoles as string];

    return { userId, username, email, roles, exp: p["exp"] as number | undefined };
  } catch {
    return null;
  }
}

/** Retorna o access token raw para repassar ao IMS API */
export async function getAccessToken(): Promise<string | null> {
  const cookieStore = await cookies();
  return cookieStore.get(AUTH_COOKIE)?.value ?? null;
}

/** Seta os cookies de sessão (chamado após login bem-sucedido) */
export function setSessionCookies(
  accessToken: string,
  refreshToken: string,
  response: Response
): void {
  const isProduction = process.env.NODE_ENV === "production";
  const cookieOpts = [
    "HttpOnly",
    "Path=/",
    "SameSite=Strict",
    isProduction ? "Secure" : "",
    `Max-Age=${60 * 60 * 24}`, // 24h
  ]
    .filter(Boolean)
    .join("; ");

  response.headers.append("Set-Cookie", `${AUTH_COOKIE}=${accessToken}; ${cookieOpts}`);
  response.headers.append("Set-Cookie", `${REFRESH_COOKIE}=${refreshToken}; ${cookieOpts}`);
}

export { AUTH_COOKIE, REFRESH_COOKIE };
