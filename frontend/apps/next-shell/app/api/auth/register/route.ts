import { NextRequest, NextResponse } from "next/server";
import type { AuthResponse } from "@/lib/types";

const IMS_API = process.env.IMS_API_URL ?? "http://localhost:5049";

export async function POST(request: NextRequest) {
  const body = await request.json();

  const upstream = await fetch(`${IMS_API}/api/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (!upstream.ok) {
    const err = await upstream.json().catch(() => ({ message: "Registration failed" }));
    return NextResponse.json(err, { status: upstream.status });
  }

  const data: AuthResponse = await upstream.json();
  return NextResponse.json(data, { status: 201 });
}
