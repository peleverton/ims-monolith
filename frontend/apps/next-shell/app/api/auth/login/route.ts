import { NextRequest, NextResponse } from "next/server";
import type { AuthResponse } from "@/lib/types";

const IMS_API = process.env.IMS_API_URL ?? "http://localhost:5049";
const AUTH_COOKIE = "ims_access_token";
const REFRESH_COOKIE = "ims_refresh_token";

const cookieOpts = (maxAge: number) =>
  `HttpOnly; Path=/; SameSite=Strict; Max-Age=${maxAge}${
    process.env.NODE_ENV === "production" ? "; Secure" : ""
  }`;

export async function POST(request: NextRequest) {
  const body = await request.json();

  const upstream = await fetch(`${IMS_API}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (!upstream.ok) {
    const err = await upstream.json().catch(() => ({ message: "Unauthorized" }));
    return NextResponse.json(err, { status: upstream.status });
  }

  const data: AuthResponse = await upstream.json();

  const response = NextResponse.json({
    username: data.username,
    email: data.email,
    roles: data.roles,
    expiresIn: data.expiresIn,
  });

  response.headers.append(
    "Set-Cookie",
    `${AUTH_COOKIE}=${data.accessToken}; ${cookieOpts(data.expiresIn)}`
  );
  response.headers.append(
    "Set-Cookie",
    `${REFRESH_COOKIE}=${data.refreshToken}; ${cookieOpts(60 * 60 * 24 * 7)}`
  );

  return response;
}
