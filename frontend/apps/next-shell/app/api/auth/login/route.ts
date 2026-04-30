import { NextRequest, NextResponse } from "next/server";
import type { AuthResponse } from "@/lib/types";

const IMS_API = process.env.IMS_API_URL ?? "http://localhost:5049";
const AUTH_COOKIE = "ims_access_token";
const REFRESH_COOKIE = "ims_refresh_token";

// Only set Secure flag when the public URL is HTTPS
const isSecure =
  (process.env.NEXTAUTH_URL ?? process.env.NEXT_PUBLIC_APP_URL ?? "http://localhost:3000").startsWith(
    "https://"
  );

const cookieOpts = (maxAge: number) =>
  `HttpOnly; Path=/; SameSite=Lax; Max-Age=${maxAge}${isSecure ? "; Secure" : ""}`;

export async function POST(request: NextRequest) {
  const body = await request.json();

  let upstream: Response;
  try {
    upstream = await fetch(`${IMS_API}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
  } catch {
    return NextResponse.json({ message: "Serviço indisponível" }, { status: 503 });
  }

  if (!upstream.ok) {
    const err = await upstream.json().catch(() => ({ message: "Unauthorized" }));
    return NextResponse.json(err, { status: upstream.status });
  }

  const data: AuthResponse = await upstream.json();

  // Compute seconds until expiry (backend returns expiresAt datetime string)
  const expiresAt = (data as unknown as { expiresAt?: string }).expiresAt;
  const accessTokenMaxAge = expiresAt
    ? Math.max(0, Math.floor((new Date(expiresAt).getTime() - Date.now()) / 1000))
    : 3600;

  const response = NextResponse.json({
    username: data.username,
    email: data.email,
    roles: data.roles,
    expiresIn: accessTokenMaxAge,
  });

  response.headers.append(
    "Set-Cookie",
    `${AUTH_COOKIE}=${data.accessToken}; ${cookieOpts(accessTokenMaxAge)}`
  );
  response.headers.append(
    "Set-Cookie",
    `${REFRESH_COOKIE}=${data.refreshToken}; ${cookieOpts(60 * 60 * 24 * 7)}`
  );

  return response;
}
