import { NextRequest, NextResponse } from "next/server";

const BACKEND_URL = process.env.IMS_API_URL ?? "http://localhost:5049";

export async function POST(request: NextRequest) {
  const body = await request.json();

  // Honeypot check: bots fill hidden fields that humans never touch
  if (body.hp_field) {
    return NextResponse.json({ message: "Bad request." }, { status: 400 });
  }

  const { orgName, email, password, plan } = body;

  const upstream = await fetch(`${BACKEND_URL}/api/tenants/signup`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ orgName, email, password, plan }),
  });

  if (!upstream.ok) {
    const err = await upstream.json().catch(() => ({ message: "Signup failed." }));
    return NextResponse.json(err, { status: upstream.status });
  }

  const data = await upstream.json();
  return NextResponse.json(data, { status: 202 });
}
