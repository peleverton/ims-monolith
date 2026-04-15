import { NextRequest, NextResponse } from "next/server";
import type { AuthResponse } from "@/lib/types";

const IMS_API = process.env.IMS_API_URL ?? "http://localhost:5049";
const REFRESH_COOKIE = "ims_refresh_token";
const AUTH_COOKIE = "ims_access_token";

const cookieOpts = (maxAge: number) =>
  `HttpOnly; Path=/; SameSite=Strict; Max-Age=${maxAge}${
    process.env.NODE_ENV === "production" ? "; Secure" : ""
  }`;

export async function POST(request: NextRequest) {
  const refreshToken = request.cookies.get(REFRESH_COOKIE)?.value;
  if (!refreshToken) {
    return NextResponse.json({ message: "No refresh token" }, { status: 401 });
  }

  const upstream = await fetch(`${IMS_API}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });

  if (!upstream.ok) {
    return NextResponse.json({ message: "Refresh failed" }, { status: 401 });
  }

  const data: AuthResponse = await upstream.json();

  const response = NextResponse.json({ expiresIn: data.expiresIn });
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
