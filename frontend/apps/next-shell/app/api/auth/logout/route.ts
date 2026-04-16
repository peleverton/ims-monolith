import { NextRequest, NextResponse } from "next/server";

const AUTH_COOKIE = "ims_access_token";
const REFRESH_COOKIE = "ims_refresh_token";
const IMS_API = process.env.IMS_API_URL ?? "http://localhost:5049";

export async function POST(request: NextRequest) {
  const refreshToken = request.cookies.get(REFRESH_COOKIE)?.value;

  // Notificar backend para invalidar o refresh token (best-effort)
  if (refreshToken) {
    await fetch(`${IMS_API}/api/auth/logout`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    }).catch(() => {
      // Ignorar erros — o cookie será removido de qualquer forma
    });
  }

  const response = NextResponse.json({ message: "Logged out" });

  // Expirar ambos os cookies
  const expiredOpts = {
    httpOnly: true,
    path: "/",
    sameSite: "strict" as const,
    maxAge: 0,
    secure: process.env.NODE_ENV === "production",
  };
  response.cookies.set(AUTH_COOKIE, "", expiredOpts);
  response.cookies.set(REFRESH_COOKIE, "", expiredOpts);

  return response;
}
