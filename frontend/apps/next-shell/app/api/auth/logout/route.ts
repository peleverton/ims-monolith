import { NextResponse } from "next/server";

const AUTH_COOKIE = "ims_access_token";
const REFRESH_COOKIE = "ims_refresh_token";

export async function POST() {
  const response = NextResponse.json({ message: "Logged out" });
  response.cookies.delete(AUTH_COOKIE);
  response.cookies.delete(REFRESH_COOKIE);
  return response;
}
