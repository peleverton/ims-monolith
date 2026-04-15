import { SignJWT, jwtVerify } from "jose";
import { cookies } from "next/headers";

const AUTH_COOKIE = "ims_access_token";
const REFRESH_COOKIE = "ims_refresh_token";
const secret = new TextEncoder().encode(
  process.env.AUTH_SECRET ?? "fallback-secret-change-me"
);

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
    const { payload } = await jwtVerify(token, secret);
    return payload as unknown as SessionPayload;
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
